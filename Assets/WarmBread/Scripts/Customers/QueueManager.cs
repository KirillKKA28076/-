using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WarmBread
{
    public sealed class QueueManager : MonoBehaviour
    {
        public GameSession Session { get; private set; }
        public CustomerAI Front => customers.Count > 0 &&
                                   customers[0] != null &&
                                   customers[0].CurrentState != CustomerAI.State.Arriving
            ? customers[0]
            : null;
        public int Count => customers.Count;
        public bool Conflict { get; private set; }

        private readonly List<CustomerAI> customers = new List<CustomerAI>();
        private float spawnTimer = 3f;
        private float conflictTimer;
        private float conflictCooldown;
        private System.Random rng;
        private int seededDay;

        public void Initialize(GameSession session)
        {
            Session = session;
            Reseed();
        }

        private void Update()
        {
            if (Session == null || !Session.Running || Session.Paused) return;
            if (seededDay != Session.Day) Reseed();

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                var rushHour = (Session.Hour > 8f && Session.Hour < 10f) ||
                               (Session.Hour > 17f && Session.Hour < 19f);
                var baseInterval = rushHour ? 21f : 32f;
                var multiplier = Session.CurrentPlan != null
                    ? Session.CurrentPlan.CustomerIntervalMultiplier
                    : 1f;
                spawnTimer = Mathf.Max(7f, baseInterval * multiplier * RandomRange(.88f, 1.12f));

                var maxQueue = Mathf.Clamp(6 + (Session.Day - 1) / 3, 6, 9);
                if (customers.Count < maxQueue && Session.Hour < 19.8f) Spawn();
            }

            conflictCooldown = Mathf.Max(0f, conflictCooldown - Time.deltaTime);
            var conflictThreshold = Mathf.Max(28f, 48f - Session.Day * 2f);
            if (!Conflict &&
                conflictCooldown <= 0f &&
                customers.Count >= 3 &&
                customers[1] != null &&
                customers[1].WaitSeconds > conflictThreshold)
            {
                Conflict = true;
                conflictTimer = 25f;
                EventBus.Say("«Мужчина, вы куда лезете?» — E у окна: успокоить очередь.");
                EventBus.Refresh();
            }

            if (!Conflict) return;

            conflictTimer -= Time.deltaTime;
            if (conflictTimer > 0f) return;

            Conflict = false;
            conflictCooldown = 45f;
            if (customers.Count > 1) Abandon(customers[1]);
        }

        private void Spawn()
        {
            if (Session.People == null || Session.People.Length == 0 ||
                Session.Products == null || Session.Products.Length == 0 ||
                rng == null)
            {
                return;
            }

            var allPeople = Session.People.Where(person => person != null).ToArray();
            if (allPeople.Length == 0) return;

            var regularPeople = allPeople.Where(person => !person.LateVisitor).ToArray();
            var latePeople = allPeople.Where(person => person.LateVisitor).ToArray();

            CustomerData data;
            var lateChance = Session.Day >= CampaignRules.CampaignDays ? .34 : .2;
            if (Session.Hour > 19f && latePeople.Length > 0 && rng.NextDouble() < lateChance)
            {
                data = latePeople[rng.Next(latePeople.Length)];
            }
            else
            {
                var pool = regularPeople.Length > 0 ? regularPeople : allPeople;
                data = pool[rng.Next(pool.Length)];
            }

            var available = Session.Products
                .Where(product =>
                    product != null &&
                    Session.Stock.Count(product.Id) > 0 &&
                    (!data.Child || !product.Id.StartsWith("sig_", StringComparison.Ordinal)))
                .ToArray();

            if (available.Length == 0) return;

            var order = new Order();
            var maximumItems = Session.Day >= 6 ? 4 : 3;
            var itemCount = rng.Next(1, maximumItems + 1);
            for (var i = 0; i < itemCount; i++)
            {
                var eligible = available
                    .Where(product =>
                        !order.Items.TryGetValue(product.Id, out var ordered) ||
                        ordered < Session.Stock.Count(product.Id))
                    .ToArray();

                if (eligible.Length == 0) break;
                var selected = PickWeighted(eligible, Session.CurrentPlan);
                if (selected != null) order.Add(selected);
            }

            if (order.Items.Count == 0) return;

            var go = new GameObject("Покупатель • " + data.DisplayName);
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(-17f, .1f, 4f);

            var person = go.AddComponent<CustomerAI>();
            person.Initialize(data, order, this);
            customers.Add(person);
            Reposition();
            EventBus.Refresh();
        }

        private ProductData PickWeighted(ProductData[] products, DayPlan plan)
        {
            if (products == null || products.Length == 0) return null;
            if (plan == null) return products[rng.Next(products.Length)];

            var total = 0f;
            for (var i = 0; i < products.Length; i++)
            {
                total += Mathf.Max(.01f, plan.DemandWeight(products[i]));
            }

            var roll = (float)rng.NextDouble() * total;
            for (var i = 0; i < products.Length; i++)
            {
                roll -= Mathf.Max(.01f, plan.DemandWeight(products[i]));
                if (roll <= 0f) return products[i];
            }

            return products[products.Length - 1];
        }

        public void Calm()
        {
            if (!Conflict) return;
            Conflict = false;
            conflictCooldown = 45f;
            Session.ChangeReputation(2);
            EventBus.Say("«Всем хватит свежего хлеба». Очередь успокоилась.");
            EventBus.Refresh();
        }

        public void Complete(CustomerAI person)
        {
            if (person == null || !customers.Remove(person)) return;
            person.Leave();
            Reposition();
        }

        public void Abandon(CustomerAI person)
        {
            if (person == null || !customers.Remove(person)) return;
            Session.CustomerLost(person);
            person.Leave();
            Reposition();
        }

        private void Reposition()
        {
            customers.RemoveAll(person => person == null);
            for (var i = 0; i < customers.Count; i++)
            {
                customers[i].MoveTo(new Vector3(i == 0 ? 0f : .4f, .1f, 2.55f + i * .9f));
            }

            EventBus.Refresh();
        }

        public void Clear()
        {
            foreach (Transform child in transform)
            {
                if (child != null) Destroy(child.gameObject);
            }

            customers.Clear();
            spawnTimer = 4f;
            conflictTimer = 0f;
            conflictCooldown = 45f;
            Conflict = false;
            Reseed();
            EventBus.Refresh();
        }

        private void Reseed()
        {
            seededDay = Session != null ? Session.Day : 1;
            rng = new System.Random(2002 + seededDay * 7919);
        }

        private float RandomRange(float minimum, float maximum)
        {
            return minimum + (maximum - minimum) * (float)rng.NextDouble();
        }
    }
}
