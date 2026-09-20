using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WarmBread
{
    public sealed class GameSession : MonoBehaviour
    {
        private static readonly string[] CanonicalProductOrder =
        {
            "bread_white",
            "bread_black",
            "pirozhok_meat",
            "pirozhok_potato",
            "bulochka",
            "sig_camel",
            "sig_java",
            "water",
            "lemonade",
            "gum"
        };

        public ProductData[] Products { get; private set; } = new ProductData[0];
        public CustomerData[] People { get; private set; } = new CustomerData[0];
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
        public float Hour { get; private set; } = 6f;
        public float AbsoluteHour => (Day - 1) * 24f + Hour;
        public int ChangeInHand { get; private set; }
        public int PendingDeliveries => deliveries.Count;

        [Min(60f)]
        public float DayDurationSeconds = 900f;

        private float freshnessTimer;
        private readonly HashSet<string> journalIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<Delivery> deliveries = new List<Delivery>();

        private struct Delivery
        {
            public string Id;
            public float Due;
        }

        public void Initialize()
        {
            var catalog = Resources.LoadAll<ProductData>("Products")
                .Where(product => product != null && !string.IsNullOrWhiteSpace(product.Id))
                .GroupBy(product => product.Id, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList();

            var catalogIds = new HashSet<string>(catalog.Select(product => product.Id), StringComparer.Ordinal);
            foreach (var fallback in ProductCatalog.CreateDefaults())
            {
                if (catalogIds.Add(fallback.Id))
                {
                    catalog.Add(fallback);
                }
                else
                {
                    Destroy(fallback);
                }
            }

            Products = catalog
                .OrderBy(ProductSortIndex)
                .ThenBy(product => product.Id, StringComparer.Ordinal)
                .ToArray();

            People = Resources.LoadAll<CustomerData>("Customers")
                .Where(person => person != null)
                .ToArray();

            if (People.Length == 0) People = CustomerData.Defaults();
        }

        public ProductData FindProduct(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return Products.FirstOrDefault(product => product != null && product.Id == id);
        }

        public void Begin(bool resume)
        {
            var saved = resume ? SaveSystem.Read() : null;

            Day = saved != null ? saved.day : 1;
            Cash = saved != null ? saved.cash : 50000;
            Reputation = saved != null ? saved.reputation : 70;
            Hour = 6f;

            Stock = new Inventory();
            Journal.Clear();
            journalIds.Clear();
            Bag.Clear();
            deliveries.Clear();

            if (saved != null)
            {
                Stock.Return(saved.stock);
                Journal.AddRange(saved.journal);

                foreach (var id in saved.journalIds)
                {
                    if (!string.IsNullOrWhiteSpace(id)) journalIds.Add(id);
                }

                if (journalIds.Count == 0)
                {
                    foreach (var story in Journal)
                    {
                        var person = People.FirstOrDefault(candidate =>
                            candidate != null &&
                            string.Equals(candidate.Story, story, StringComparison.Ordinal));

                        journalIds.Add(person != null ? person.Id : "legacy:" + story);
                    }
                }
            }
            else
            {
                foreach (var product in Products)
                {
                    Stock.Add(product.Id, 10, AbsoluteHour);
                }
            }

            ResetShift();
            SaveCheckpoint();

            EventBus.Say(resume && saved == null
                ? "Сохранение не прочитано. Начинаем новую историю."
                : "Сентябрь, 2002. Чайник греется. Район просыпается.");
        }

        private void ResetShift()
        {
            Hour = 6f;
            Sales = 0;
            Revenue = 0;
            Expenses = 0;
            Complaints = 0;
            Waste = 0;
            Paying = false;
            Paused = false;
            Modal = false;
            Report = false;
            Running = true;
            ChangeInHand = 0;
            freshnessTimer = 0f;

            Time.timeScale = 1f;
            if (Queue != null) Queue.Clear();

            Waste += Stock.Expire(Products, AbsoluteHour);
            EventBus.Refresh();
        }

        private void Update()
        {
            if (!Running || Paused) return;

            Hour += Time.deltaTime * 14f / Mathf.Max(60f, DayDurationSeconds);

            for (var i = deliveries.Count - 1; i >= 0; i--)
            {
                if (Time.time < deliveries[i].Due) continue;

                Stock.Add(deliveries[i].Id, 10, AbsoluteHour);
                deliveries.RemoveAt(i);
                EventBus.Say("Поставщик оставил ящик свежего товара. Уже на полке.");
                EventBus.Sound(.8f);
                EventBus.Refresh();
            }

            freshnessTimer += Time.deltaTime;
            if (freshnessTimer >= 5f)
            {
                freshnessTimer = 0f;
                var expiredOnShelf = Stock.Expire(Products, AbsoluteHour);
                Waste += expiredOnShelf;

                if (!Paying && Bag.Any(IsExpired))
                {
                    ReturnBag();
                    Waste += Stock.Expire(Products, AbsoluteHour);
                    EventBus.Say("Выпечка в пакете остыла и списана. Соберите свежий заказ.");
                }
                else if (expiredOnShelf > 0)
                {
                    EventBus.Say("Списано несвежих товаров: " + expiredOnShelf + ". Позвоните поставщику.");
                    EventBus.Refresh();
                }
            }

            if (Hour >= 20f) CloseDay();
        }

        private bool IsExpired(StockBatch batch)
        {
            if (batch == null || string.IsNullOrWhiteSpace(batch.productId)) return true;
            var product = FindProduct(batch.productId);
            if (product == null) return true;
            return product.ShelfLifeHours > 0f &&
                   AbsoluteHour - batch.bakedAt >= product.ShelfLifeHours;
        }

        public void AddToBag(ProductData product)
        {
            if (!Running || Paying || product == null) return;

            if (Bag.Count >= 10)
            {
                EventBus.Say("Пакет полон. X — вернуть всё на полку.");
                return;
            }

            var item = Stock.Take(product.Id);
            if (item == null)
            {
                EventBus.Say("На полке пусто. Телефон или блокнот → поставщик.");
                return;
            }

            Bag.Add(item);
            EventBus.Sound(1.3f);
            EventBus.Refresh();
        }

        public void ReturnBag()
        {
            if (Paying) return;
            Stock.Return(Bag);
            Bag.Clear();
            EventBus.Refresh();
        }

        public void Serve()
        {
            if (!Running || Queue == null) return;

            if (Customer == null)
            {
                EventBus.Say("Пока никого. Можно выпить чаю.");
                return;
            }

            if (Queue.Conflict)
            {
                Queue.Calm();
                return;
            }

            if (Paying)
            {
                Modal = true;
                EventBus.Refresh();
                return;
            }

            if (!Customer.Order.Matches(Bag))
            {
                EventBus.Say("«Это не совсем мой заказ». Проверьте пакет; X — вернуть товары.");
                return;
            }

            Paying = true;
            ChangeInHand = 0;
            Customer.SetPaying();
            Modal = true;
            EventBus.Refresh();
        }

        public void AddChange(int denomination)
        {
            if (!Paying ||
                Customer == null ||
                Array.IndexOf(Money.Denominations, denomination) < 0 ||
                ChangeInHand + denomination > 20000)
            {
                return;
            }

            ChangeInHand += denomination;
            EventBus.Sound(1.7f);
            EventBus.Refresh();
        }

        public void ClearChange()
        {
            ChangeInHand = 0;
            EventBus.Refresh();
        }

        public void Checkout()
        {
            if (!Paying || Customer == null) return;

            var customer = Customer;
            if (ChangeInHand != Money.Change(customer.Order.Total, customer.Order.Paid))
            {
                ChangeReputation(-2);
                Complaints++;
                EventBus.Say("«Пересчитайте, пожалуйста». Сдача неточная; деньги ещё не переданы.");
                return;
            }

            Cash += customer.Order.Total;
            Revenue += customer.Order.Total;
            Sales++;
            ChangeReputation(1);

            Bag.Clear();
            Paying = false;
            Modal = false;
            ChangeInHand = 0;

            var data = customer.Data;
            if (data != null && !string.IsNullOrWhiteSpace(data.Story))
            {
                var storyId = string.IsNullOrWhiteSpace(data.Id) ? "story:" + data.Story : data.Id;
                if (journalIds.Add(storyId)) Journal.Add(data.Story);
            }

            var customerName = data != null ? data.DisplayName : "Покупатель";
            EventBus.Say(customerName + ": «Спасибо. До завтра». История записана в блокнот.");

            Queue.Complete(customer);
            EventBus.Sound(1.1f);
            EventBus.Refresh();
        }

        public void CustomerLost(CustomerAI customer)
        {
            if (customer == null) return;

            if (customer == Customer)
            {
                Paying = false;
                Modal = false;
                ChangeInHand = 0;
                ReturnBag();
            }

            Complaints++;
            ChangeReputation(-3);
            EventBus.Say("Покупатель не дождался. Району тоже нужно внимание.");
        }

        public void BuyStock(ProductData product)
        {
            if (!Running || product == null) return;

            var costLong = (long)product.Cost * 10L;
            if (costLong < 0L || costLong > int.MaxValue) return;
            var cost = (int)costLong;

            if (Cash < cost)
            {
                EventBus.Say("На этот ящик не хватает денег.");
                return;
            }

            if (deliveries.Count >= 4)
            {
                EventBus.Say("Газель полна: дождитесь доставки.");
                return;
            }

            Cash -= cost;
            Expenses += cost;
            deliveries.Add(new Delivery { Id = product.Id, Due = Time.time + 25f });

            EventBus.Say("Заказано: " + product.Title + " × 10. Привезут через 25 секунд.");
            EventBus.Refresh();
        }

        public void ChangeReputation(int amount)
        {
            Reputation = Mathf.Clamp(Reputation + amount, 0, 100);
            EventBus.Refresh();
        }

        public void CloseDay()
        {
            if (!Running) return;

            Paying = false;
            ReturnBag();
            if (Queue != null) Queue.Clear();

            foreach (var delivery in deliveries)
            {
                Stock.Add(delivery.Id, 10, AbsoluteHour);
            }

            deliveries.Clear();
            Cash -= 5000;
            Expenses += 5000;
            Running = false;
            Report = true;
            Modal = true;
            Hour = 20f;

            // Сохраняем уже следующий день: при аварийном выходе итог смены не теряется
            // и повторная аренда за закрытый день не списывается.
            SaveCheckpoint(Day + 1);
            EventBus.Refresh();
        }

        public void NextDay()
        {
            if (!Report) return;

            Day++;
            ResetShift();
            SaveCheckpoint();
            EventBus.Say("Новая смена. Пусть сегодня будет немного теплее.");
        }

        private void SaveCheckpoint(int? dayOverride = null)
        {
            var data = new SaveData
            {
                day = dayOverride ?? Day,
                cash = Cash,
                reputation = Reputation,
                stock = Stock.Batches
                    .Select(batch => new StockBatch(batch.productId, batch.quantity, batch.bakedAt))
                    .ToList(),
                journal = new List<string>(Journal),
                journalIds = journalIds.ToList()
            };

            if (!SaveSystem.Write(data))
            {
                EventBus.Say("Не удалось сохранить смену. Проверьте свободное место и права доступа.");
            }
        }

        private static int ProductSortIndex(ProductData product)
        {
            var index = Array.IndexOf(CanonicalProductOrder, product.Id);
            return index >= 0 ? index : int.MaxValue;
        }
    }
}
