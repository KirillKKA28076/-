using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace WarmBread
{
    public sealed class Order
    {
        private readonly Dictionary<string, int> items = new Dictionary<string, int>();
        private readonly IReadOnlyDictionary<string, int> readOnlyItems;

        public Order()
        {
            readOnlyItems = new ReadOnlyDictionary<string, int>(items);
        }

        public IReadOnlyDictionary<string, int> Items => readOnlyItems;
        public int Total { get; private set; }
        public int Paid => Money.Tender(Total);

        public void Add(ProductData product)
        {
            if (product == null || string.IsNullOrWhiteSpace(product.Id)) return;
            items[product.Id] = items.TryGetValue(product.Id, out var count) ? count + 1 : 1;
            Total += product.Price;
        }

        public bool Matches(IEnumerable<StockBatch> bag)
        {
            if (bag == null) return false;
            var counts = bag
                .Where(batch => batch != null && !string.IsNullOrWhiteSpace(batch.productId) && batch.quantity > 0)
                .GroupBy(batch => batch.productId)
                .ToDictionary(group => group.Key, group => group.Sum(batch => batch.quantity));

            return counts.Count == items.Count &&
                   items.All(pair => counts.TryGetValue(pair.Key, out var count) && count == pair.Value);
        }

        public string Describe(ProductData[] products)
        {
            var catalog = (products ?? new ProductData[0])
                .Where(product => product != null && !string.IsNullOrWhiteSpace(product.Id))
                .GroupBy(product => product.Id)
                .ToDictionary(group => group.Key, group => group.First());

            return string.Join("\n", items.Select(pair =>
            {
                var title = catalog.TryGetValue(pair.Key, out var product) ? product.Title : pair.Key;
                return title + "  × " + pair.Value;
            }));
        }
    }
}
