using System.Collections.Generic;
using System.Linq;

namespace WarmBread
{
    public sealed class Inventory
    {
        private readonly List<StockBatch> batches = new List<StockBatch>();
        public IReadOnlyList<StockBatch> Batches => batches;
        public int Count(string id) => batches.Where(b => b.productId == id).Sum(b => b.quantity);
        public void Add(string id, int quantity, float time)
        { if (quantity > 0) batches.Add(new StockBatch(id, quantity, time)); }
        public StockBatch Take(string id)
        {
            var b = batches.Where(x => x.productId == id && x.quantity > 0).OrderBy(x => x.bakedAt).FirstOrDefault();
            if (b == null) return null;
            b.quantity--;
            var unit = new StockBatch(id, 1, b.bakedAt);
            if (b.quantity == 0) batches.Remove(b);
            return unit;
        }
        public void Return(IEnumerable<StockBatch> units)
        { foreach (var b in units) Add(b.productId, b.quantity, b.bakedAt); }
        public int Expire(ProductData[] products, float now)
        {
            int lost = 0;
            for (int i = batches.Count - 1; i >= 0; i--)
            {
                var b = batches[i];
                var p = products.FirstOrDefault(x => x.Id == b.productId);
                if (p == null || (p.ShelfLifeHours > 0 && now - b.bakedAt >= p.ShelfLifeHours))
                { lost += b.quantity; batches.RemoveAt(i); }
            }
            return lost;
        }
    }
}
