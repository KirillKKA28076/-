using System.Collections.Generic;
using System.Linq;

namespace WarmBread
{
    public sealed class Order
    {
        public readonly Dictionary<string, int> Items = new Dictionary<string, int>();
        public int Total { get; private set; }
        public int Paid => Money.Tender(Total);
        public void Add(ProductData product)
        { Items[product.Id] = Items.TryGetValue(product.Id, out var count) ? count + 1 : 1; Total += product.Price; }
        public bool Matches(IEnumerable<StockBatch> bag)
        {
            var counts = bag.GroupBy(b => b.productId).ToDictionary(g => g.Key, g => g.Sum(b => b.quantity));
            return counts.Count == Items.Count && Items.All(p => counts.TryGetValue(p.Key, out var n) && n == p.Value);
        }
        public string Describe(ProductData[] products) => string.Join("\n", Items.Select(p =>
            products.First(x => x.Id == p.Key).Title + "  × " + p.Value));
    }
}
