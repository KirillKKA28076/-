using System;
using System.Collections.Generic;

namespace WarmBread
{
    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;
        public int day = 1;
        public int cash = 50000;
        public int reputation = 70;
        public List<StockBatch> stock = new List<StockBatch>();
        public List<string> journal = new List<string>();
    }
}
