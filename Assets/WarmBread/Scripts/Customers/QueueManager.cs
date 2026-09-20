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

        public void Initialize(GameSession session)
        {
            Session = session;
            rng = new System.Random(2002);
        }

        private void Update()
        {
            if (Session == null || !Session.Running || Session.Paused) return;

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = (Session.Hour > 8f && Session.Hour < 10f) ||
                             (Session.Hour > 17f && Session.Hour < 19f)
                    ? 21f
                    : 32f;

                if (customers.Count < 6 && Session.Hour < 19.8f) Spawn();
            }

            conflictCooldown = Mathf.Max(0f, conflictCooldown - Time.deltaTime);
            if (!Conflict &&
                conflictCooldown <= 0f &&
                customers.Count >= 3 &&
                customers[1] != null &&
                customers[1].WaitSeconds > 45f)
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
            if (Session.Hour > 19f && latePeople.Length > 0 && rng.NextDouble() < .2)
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
            var itemCount = rng.Next(1, 4);
            for (var i = 0; i < itemCount; i++)
            {
                var eligible = available
                    .Where(product =>
                        !order.Items.TryGetValue(product.Id, out var ordered) ||
                        ordered < Session.Stock.Count(product.Id))
                    .ToArray();

                if (eligible.Length == 0) break;
                order.Add(eligible[rng.Next(eligible.Length)]);
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
            EventBus.Refresh();
        }
    }
}
