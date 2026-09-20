using System;
using System.Collections.Generic;
using UnityEngine;

namespace WarmBread
{
    public enum MoneyOperation { Sale, Purchase, Rent, Penalty, Refund, Other }

    [Serializable]
    public sealed class MoneyEntry
    {
        public MoneyOperation operation;
        public int amount;
        public int balanceAfter;
        public string note;
        public long unixTime;
    }

    public sealed class EconomyLedger : MonoBehaviour
    {
        [SerializeField] private int startingBalance = 50000;
        [SerializeField] private int minimumBalance = -10000000;
        [SerializeField] private int maximumBalance = 100000000;

        private readonly List<MoneyEntry> entries = new List<MoneyEntry>();
        public int Balance { get; private set; }
        public IReadOnlyList<MoneyEntry> Entries => entries;

        private void Awake() { Balance = Mathf.Clamp(startingBalance, minimumBalance, maximumBalance); }

        public bool TryApply(MoneyOperation operation, int amount, string note = null)
        {
            if (amount == 0 || Math.Abs((long)amount) > maximumBalance) return false;
            long next = (long)Balance + amount;
            if (next < minimumBalance || next > maximumBalance) return false;

            Balance = (int)next;
            entries.Add(new MoneyEntry
            {
                operation = operation,
                amount = amount,
                balanceAfter = Balance,
                note = note ?? string.Empty,
                unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });
            GameEventBus.Publish(new MoneyChanged(Balance));
            return true;
        }

        public void ResetLedger()
        {
            entries.Clear();
            Balance = Mathf.Clamp(startingBalance, minimumBalance, maximumBalance);
            GameEventBus.Publish(new MoneyChanged(Balance));
        }
    }
}
