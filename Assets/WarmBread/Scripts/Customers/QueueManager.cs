using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WarmBread
{
    public sealed class QueueManager : MonoBehaviour
    {
        public GameSession Session { get; private set; }
        public CustomerAI Front => customers.Count > 0 && customers[0].CurrentState != CustomerAI.State.Arriving ? customers[0] : null;
        public int Count => customers.Count;
        public bool Conflict { get; private set; }
        private readonly List<CustomerAI> customers = new List<CustomerAI>();
        private float spawnTimer = 3, conflictTimer, conflictCooldown;
        private System.Random rng;
        public void Initialize(GameSession session) { Session = session; rng = new System.Random(2002); }
        private void Update()
        {
            if (Session == null || !Session.Running || Session.Paused) return;
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0)
            {
                spawnTimer = (Session.Hour > 8 && Session.Hour < 10) || (Session.Hour > 17 && Session.Hour < 19) ? 21 : 32;
                if (customers.Count < 6 && Session.Hour < 19.8f) Spawn();
            }
            conflictCooldown -= Time.deltaTime;
            if (!Conflict && conflictCooldown <= 0 && customers.Count >= 3 && customers[1].WaitSeconds > 45)
            { Conflict = true; conflictTimer = 25; EventBus.Say("«Мужчина, вы куда лезете?» — E у окна: успокоить очередь."); EventBus.Refresh(); }
            if (Conflict)
            {
                conflictTimer -= Time.deltaTime;
                if (conflictTimer <= 0) { Conflict = false; conflictCooldown = 45; if (customers.Count > 1) Abandon(customers[1]); }
            }
        }
        private void Spawn()
        {
            var data = Session.People[rng.Next(Session.People.Length - 1)];
            if (Session.Hour > 19 && rng.NextDouble() < .2) data = Session.People[Session.People.Length - 1];
            var available = Session.Products.Where(p => Session.Stock.Count(p.Id) > 0 && (!data.Child || !p.Id.StartsWith("sig_"))).ToArray();
            if (available.Length == 0) return;
            var order = new Order();
            int count = rng.Next(1, 4);
            for (int i = 0; i < count; i++)
            {
                var p = available[rng.Next(available.Length)];
                if (!order.Items.TryGetValue(p.Id, out var n) || n < Session.Stock.Count(p.Id)) order.Add(p);
            }
            var go = new GameObject("Покупатель • " + data.DisplayName);
            go.transform.SetParent(transform); go.transform.position = new Vector3(-17, .1f, 4);
            var person = go.AddComponent<CustomerAI>(); person.Initialize(data, order, this);
            customers.Add(person); Reposition(); EventBus.Refresh();
        }
        public void Calm()
        { if (!Conflict) return; Conflict = false; conflictCooldown = 45; Session.ChangeReputation(2); EventBus.Say("«Всем хватит свежего хлеба». Очередь успокоилась."); }
        public void Complete(CustomerAI person) { customers.Remove(person); person.Leave(); Reposition(); }
        public void Abandon(CustomerAI person)
        { Session.CustomerLost(person); customers.Remove(person); person.Leave(); Reposition(); }
        private void Reposition()
        {
            for (int i = 0; i < customers.Count; i++) customers[i].MoveTo(new Vector3(i == 0 ? 0 : .4f, .1f, 2.55f + i * .9f));
            EventBus.Refresh();
        }
        public void Clear()
        {
            foreach (Transform child in transform) Destroy(child.gameObject);
            customers.Clear(); spawnTimer = 4; Conflict = false; conflictCooldown = 45;
        }
    }
}
