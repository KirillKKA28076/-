using System;

namespace WarmBread
{
    [Serializable]
    public sealed class StockBatch
    {
        public string productId;
        public int quantity;
        public float bakedAt;
        public StockBatch(string id, int count, float time) { productId = id; quantity = count; bakedAt = time; }
    }
}
