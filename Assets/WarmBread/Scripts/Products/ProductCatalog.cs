using UnityEngine;

namespace WarmBread
{
    public static class ProductCatalog
    {
        public static ProductData[] CreateDefaults()
        {
            return new[] {
                Make("bread_white", "Батон нарезной", 8, 15, 48, "D59A50"),
                Make("bread_black", "Хлеб бородинский", 7, 14, 48, "714730"),
                Make("pirozhok_meat", "Пирожок с мясом", 5, 12, 6, "BA7438"),
                Make("pirozhok_potato", "Пирожок с картошкой", 4, 10, 6, "E4B16D"),
                Make("bulochka", "Булочка с маком", 4, 10, 24, "BD8652"),
                Make("sig_camel", "Сигареты «Верблюд»", 15, 25, 0, "C6AD78"),
                Make("sig_java", "Сигареты «Ява»", 12, 20, 0, "A34338"),
                Make("water", "Вода • 0,5 л", 3, 7, 0, "A2C1C8"),
                Make("lemonade", "Лимонад «Буратино»", 5, 12, 0, "BC8D3D"),
                Make("gum", "Жвачка", 1, 3, 0, "879979") };
        }
        private static ProductData Make(string id, string title, int cost, int price, float life, string hex)
        {
            var p = ScriptableObject.CreateInstance<ProductData>();
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            p.Configure(id, title, cost * 100, price * 100, life, color);
            p.name = id;
            return p;
        }
    }
}
