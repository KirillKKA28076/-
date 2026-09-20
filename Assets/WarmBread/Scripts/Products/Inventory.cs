using System.Collections.Generic;
using System.Linq;

namespace WarmBread
{
    public sealed class Inventory
    {
        private readonly List<StockBatch> batches = new List<StockBatch>();

        public IReadOnlyList<StockBatch> Batches => batches;

        public int Count(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return 0;
            return batches
                .Where(batch => batch != null && batch.productId == id && batch.quantity > 0)
                .Sum(batch => batch.quantity);
        }

        public void Add(string id, int quantity, float time)
        {
            if (string.IsNullOrWhiteSpace(id) ||
                quantity <= 0 ||
                float.IsNaN(time) ||
                float.IsInfinity(time))
            {
                return;
            }

            batches.Add(new StockBatch(id, quantity, time));
        }

        public StockBatch Take(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            var batch = batches
                .Where(candidate =>
                    candidate != null &&
                    candidate.productId == id &&
                    candidate.quantity > 0)
                .OrderBy(candidate => candidate.bakedAt)
                .FirstOrDefault();

            if (batch == null) return null;

            batch.quantity--;
            var unit = new StockBatch(id, 1, batch.bakedAt);
            if (batch.quantity <= 0) batches.Remove(batch);
            return unit;
        }

        public void Return(IEnumerable<StockBatch> units)
        {
            if (units == null) return;

            foreach (var batch in units)
            {
                if (batch == null) continue;
                Add(batch.productId, batch.quantity, batch.bakedAt);
            }
        }

        public int Expire(ProductData[] products, float now)
        {
            var catalog = products ?? new ProductData[0];
            var lost = 0;

            for (var i = batches.Count - 1; i >= 0; i--)
            {
                var batch = batches[i];
                if (batch == null)
                {
                    batches.RemoveAt(i);
                    continue;
                }

                var product = catalog.FirstOrDefault(candidate =>
                    candidate != null && candidate.Id == batch.productId);

                if (product == null ||
                    (product.ShelfLifeHours > 0f && now - batch.bakedAt >= product.ShelfLifeHours))
                {
                    lost += System.Math.Max(0, batch.quantity);
                    batches.RemoveAt(i);
                }
            }

            return lost;
        }
    }
}
