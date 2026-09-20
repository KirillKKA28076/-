using System;
using System.Collections.Generic;

namespace WarmBread
{
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 3;

        public int version = CurrentVersion;
        public int day = 1;
        public int cash = 50000;
        public int reputation = 70;
        public int totalSales;
        public int totalRevenue;
        public int goalsCompleted;
        public bool campaignCompleted;
        public string endingId = string.Empty;
        public List<StockBatch> stock = new List<StockBatch>();
        public List<string> journal = new List<string>();
        public List<string> journalIds = new List<string>();
    }
}
