using UnityEngine;

namespace WarmBread
{
    [CreateAssetMenu(menuName = "Тёплый хлеб/Товар")]
    public sealed class ProductData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string title;
        [SerializeField, Min(0)] private int cost;
        [SerializeField, Min(1)] private int price;
        [SerializeField, Min(0)] private float shelfLifeHours;
        [SerializeField] private Color tint;
        public string Id => id;
        public string Title => title;
        // Все денежные величины в копейках, без ошибок float.
        public int Cost => cost;
        public int Price => price;
        public float ShelfLifeHours => shelfLifeHours;
        public Color Tint => tint;
        public void Configure(string key, string label, int buy, int sell, float hours, Color color)
        { id = key; title = label; cost = buy; price = sell; shelfLifeHours = hours; tint = color; }
    }
}
