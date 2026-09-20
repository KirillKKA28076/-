using System;
using System.Collections.Generic;
using UnityEngine;

namespace WarmBread
{
    public enum DeliveryStatus { Ordered, InTransit, Arrived, Cancelled }

    [Serializable]
    public sealed class DeliveryOrder
    {
        public string id;
        public string productId;
        public int quantity;
        public int totalCost;
        public float arrivalTime;
        public DeliveryStatus status;
    }

    public sealed class DeliveryService : MonoBehaviour
    {
        [SerializeField] private float deliverySeconds = 25f;
        [SerializeField] private int maxActiveOrders = 4;
        private readonly List<DeliveryOrder> orders = new List<DeliveryOrder>();

        public IReadOnlyList<DeliveryOrder> Orders => orders;

        public bool TryOrder(string productId, int quantity, int totalCost, EconomyLedger ledger)
        {
            if (string.IsNullOrEmpty(productId) || quantity <= 0 || totalCost < 0 ||
                ledger == null || orders.Count >= maxActiveOrders) return false;
            if (!ledger.TryApply(MoneyOperation.Purchase, -totalCost, "delivery:" + productId)) return false;

            orders.Add(new DeliveryOrder
            {
                id = Guid.NewGuid().ToString("N"),
                productId = productId,
                quantity = quantity,
                totalCost = totalCost,
                arrivalTime = Time.time + Mathf.Max(0f, deliverySeconds),
                status = DeliveryStatus.InTransit
            });
            return true;
        }

        private void Update()
        {
            for (var i = orders.Count - 1; i >= 0; i--)
            {
                var order = orders[i];
                if (order.status != DeliveryStatus.InTransit || Time.time < order.arrivalTime) continue;
                order.status = DeliveryStatus.Arrived;
                orders[i] = order;
            }
        }

        public bool TryCancel(string orderId, EconomyLedger ledger)
        {
            if (ledger == null) return false;
            for (var i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                if (order.id != orderId || order.status != DeliveryStatus.InTransit) continue;
                order.status = DeliveryStatus.Cancelled;
                orders[i] = order;
                ledger.TryApply(MoneyOperation.Refund, order.totalCost, "cancel:" + order.productId);
                return true;
            }
            return false;
        }
    }
}
