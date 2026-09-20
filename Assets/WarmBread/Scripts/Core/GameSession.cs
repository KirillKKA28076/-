using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WarmBread
{
    public sealed class GameSession : MonoBehaviour
    {
        public ProductData[] Products { get; private set; }
        public CustomerData[] People { get; private set; }
        public Inventory Stock { get; private set; } = new Inventory();
        public List<StockBatch> Bag { get; } = new List<StockBatch>();
        public List<string> Journal { get; } = new List<string>();
        public QueueManager Queue { get; set; }
        public CustomerAI Customer => Queue != null ? Queue.Front : null;
        public int Day { get; private set; } = 1;
        public int Cash { get; private set; } = 50000;
        public int Reputation { get; private set; } = 70;
        public int Sales { get; private set; }
        public int Revenue { get; private set; }
        public int Expenses { get; private set; }
        public int Complaints { get; private set; }
        public int Waste { get; private set; }
        public bool Running { get; private set; }
        public bool Report { get; private set; }
        public bool Paying { get; private set; }
        public bool Paused { get; set; }
        public bool Modal { get; set; } = true;
        public float Hour { get; private set; } = 6;
        public float AbsoluteHour => (Day - 1) * 24 + Hour;
        public int ChangeInHand { get; private set; }
        public int PendingDeliveries => deliveries.Count;
        public float DayDurationSeconds = 900;
        private float freshnessTimer;
        private readonly List<Delivery> deliveries = new List<Delivery>();
        private struct Delivery { public string Id; public float Due; }

        public void Initialize()
        {
            Products = Resources.LoadAll<ProductData>("Products");
            if (Products.Length == 0) Products = ProductCatalog.CreateDefaults();
            // Порядок каталога не зависит от порядка загрузки Resources.
            var order = new[] { "bread_white", "bread_black", "pirozhok_meat", "pirozhok_potato", "bulochka", "sig_camel", "sig_java", "water", "lemonade", "gum" };
            Products = Products.OrderBy(p => System.Array.IndexOf(order, p.Id)).ToArray();
            People = Resources.LoadAll<CustomerData>("Customers");
            if (People.Length == 0) People = CustomerData.Defaults();
        }
        public void Begin(bool resume)
        {
            var saved = resume ? SaveSystem.Read() : null;
            Day = saved?.day ?? 1; Cash = saved?.cash ?? 50000; Reputation = saved?.reputation ?? 70;
            Stock = new Inventory(); Journal.Clear(); Bag.Clear(); deliveries.Clear();
            if (saved != null) { Stock.Return(saved.stock); Journal.AddRange(saved.journal); }
            else foreach (var p in Products) Stock.Add(p.Id, 10, AbsoluteHour);
            ResetShift(); SaveCheckpoint();
            EventBus.Say(resume && saved == null ? "Сохранение не прочитано. Начинаем новую историю." : "Сентябрь, 2002. Чайник греется. Район просыпается.");
        }
        private void ResetShift()
        {
            Hour = 6; Sales = Revenue = Expenses = Complaints = Waste = 0;
            Paying = Paused = Modal = Report = false; Running = true; ChangeInHand = 0;
            Time.timeScale = 1;
            Queue.Clear(); Stock.Expire(Products, AbsoluteHour); EventBus.Refresh();
        }
        private void Update()
        {
            if (!Running || Paused) return;
            Hour += Time.deltaTime * 14 / DayDurationSeconds;
            for (int i = deliveries.Count - 1; i >= 0; i--)
                if (Time.time >= deliveries[i].Due)
                {
                    Stock.Add(deliveries[i].Id, 10, AbsoluteHour); deliveries.RemoveAt(i);
                    EventBus.Say("Поставщик оставил ящик свежего товара. Уже на полке."); EventBus.Sound(.8f); EventBus.Refresh();
                }
            freshnessTimer += Time.deltaTime;
            if (freshnessTimer >= 5)
            {
                freshnessTimer = 0; int count = Stock.Expire(Products, AbsoluteHour); Waste += count;
                // Собранный пакет не должен сохранять просроченную выпечку.
                if (!Paying && Bag.Any(b => IsExpired(b)))
                { ReturnBag(); Waste += Stock.Expire(Products, AbsoluteHour); EventBus.Say("Выпечка в пакете остыла и списана. Соберите свежий заказ."); }
                if (count > 0) { EventBus.Say("Списано несвежих товаров: " + count + ". Позвоните поставщику."); EventBus.Refresh(); }
            }
            if (Hour >= 20) CloseDay();
        }
        private bool IsExpired(StockBatch b)
        { var p = Products.First(x => x.Id == b.productId); return p.ShelfLifeHours > 0 && AbsoluteHour - b.bakedAt >= p.ShelfLifeHours; }
        public void AddToBag(ProductData p)
        {
            if (!Running || Paying) return;
            if (Bag.Count >= 10) { EventBus.Say("Пакет полон. X — вернуть всё на полку."); return; }
            var item = Stock.Take(p.Id);
            if (item == null) { EventBus.Say("На полке пусто. Телефон или блокнот → поставщик."); return; }
            Bag.Add(item); EventBus.Sound(1.3f); EventBus.Refresh();
        }
        public void ReturnBag()
        { if (Paying) return; Stock.Return(Bag); Bag.Clear(); EventBus.Refresh(); }
        public void Serve()
        {
            if (Customer == null) { EventBus.Say("Пока никого. Можно выпить чаю."); return; }
            if (Queue.Conflict) { Queue.Calm(); return; }
            if (Paying) { Modal = true; EventBus.Refresh(); return; }
            if (!Customer.Order.Matches(Bag))
            { EventBus.Say("«Это не совсем мой заказ». Проверьте пакет; X — вернуть товары."); return; }
            Paying = true; ChangeInHand = 0; Customer.SetPaying(); Modal = true; EventBus.Refresh();
        }
        public void AddChange(int denomination)
        {
            if (!Paying || Customer == null || denomination < 0 || ChangeInHand + denomination > 20000) return;
            ChangeInHand += denomination; EventBus.Sound(1.7f); EventBus.Refresh();
        }
        public void ClearChange() { ChangeInHand = 0; EventBus.Refresh(); }
        public void Checkout()
        {
            if (!Paying || Customer == null) return;
            var customer = Customer;
            if (ChangeInHand != Money.Change(customer.Order.Total, customer.Order.Paid))
            { ChangeReputation(-2); Complaints++; EventBus.Say("«Пересчитайте, пожалуйста». Сдача неточная; деньги ещё не переданы."); return; }
            Cash += customer.Order.Total; Revenue += customer.Order.Total; Sales++;
            ChangeReputation(1); Bag.Clear(); Paying = Modal = false; ChangeInHand = 0;
            if (!Journal.Contains(customer.Data.Story)) Journal.Add(customer.Data.Story);
            EventBus.Say(customer.Data.DisplayName + ": «Спасибо. До завтра». История записана в блокнот.");
            Queue.Complete(customer); EventBus.Sound(1.1f); EventBus.Refresh();
        }
        public void CustomerLost(CustomerAI customer)
        {
            if (customer == Customer)
            { Paying = Modal = false; ReturnBag(); ChangeInHand = 0; }
            Complaints++; ChangeReputation(-3); EventBus.Say("Покупатель не дождался. Району тоже нужно внимание.");
        }
        public void BuyStock(ProductData p)
        {
            if (!Running) return;
            int cost = p.Cost * 10;
            if (Cash < cost) { EventBus.Say("На этот ящик не хватает денег."); return; }
            if (deliveries.Count >= 4) { EventBus.Say("Газель полна: дождитесь доставки."); return; }
            Cash -= cost; Expenses += cost; deliveries.Add(new Delivery { Id = p.Id, Due = Time.time + 25 });
            EventBus.Say("Заказано: " + p.Title + " × 10. Привезут через 25 секунд."); EventBus.Refresh();
        }
        public void ChangeReputation(int amount) { Reputation = Mathf.Clamp(Reputation + amount, 0, 100); EventBus.Refresh(); }
        public void CloseDay()
        {
            if (!Running) return;
            Paying = false; ReturnBag(); Queue.Clear();
            // Оплаченные доставки не исчезают при закрытии.
            foreach (var d in deliveries) Stock.Add(d.Id, 10, AbsoluteHour);
            deliveries.Clear(); Cash -= 5000; Expenses += 5000;
            Running = false; Report = Modal = true; Hour = 20; EventBus.Refresh();
        }
        public void NextDay() { Day++; ResetShift(); SaveCheckpoint(); EventBus.Say("Новая смена. Пусть сегодня будет немного теплее."); }
        private void SaveCheckpoint()
        {
            var data = new SaveData { day = Day, cash = Cash, reputation = Reputation,
                stock = Stock.Batches.Select(b => new StockBatch(b.productId, b.quantity, b.bakedAt)).ToList(), journal = new List<string>(Journal) };
            if (!SaveSystem.Write(data)) EventBus.Say("Не удалось сохранить смену. Проверьте свободное место и права доступа.");
        }
    }
}
