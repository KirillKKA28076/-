using UnityEngine;

namespace WarmBread
{
    [CreateAssetMenu(menuName = "Тёплый хлеб/Покупатель")]
    public sealed class CustomerData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string greeting;
        [SerializeField, TextArea] private string story;
        [SerializeField] private Color coat;
        [SerializeField] private bool child;
        [SerializeField] private bool lateVisitor;

        public string Id
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(id)) return id;
                if (!string.IsNullOrWhiteSpace(name)) return name;
                return displayName ?? string.Empty;
            }
        }

        public string DisplayName => displayName;
        public string Greeting => greeting;
        public string Story => story;
        public Color Coat => coat;
        public bool Child => child;
        public bool LateVisitor => lateVisitor || displayName == "Незнакомец";

        public void Configure(string key, string n, string hello, string tale, Color color, bool isChild = false, bool isLateVisitor = false)
        {
            id = key;
            displayName = n;
            greeting = hello;
            story = tale;
            coat = color;
            child = isChild;
            lateVisitor = isLateVisitor;
        }

        public static CustomerData[] Defaults()
        {
            string[] ids = { "nina", "sergey", "mishka", "marina", "viktor", "stranger" };
            string[] names = { "Нина Петровна", "Сергей • после смены", "Мишка • 6 «Б»", "Марина", "Виктор Ильич", "Незнакомец" };
            string[] hello = { "Деточка, а хлеб сегодняшний?", "С ночной. Дайте чего-нибудь тёплого.", "А можно на мелочь? Я посчитал!", "Домой бегу. Там суп остывает.", "Доброго дня. Пахнет как в детстве.", "Один батон. Если ещё остался." };
            string[] tales = {
                "У Нины Петровны муж всегда покупал горбушку. Теперь она берёт две — по привычке.",
                "Сергей чинит трамваи. Говорит, последний рейс пахнет мокрыми пальто и железом.",
                "Мишка копит на кассету. Сегодня решил: пусть лучше будет лимонад на двоих.",
                "Марина обещала сыну море. Пока получается только синяя чашка на кухне.",
                "Виктор Ильич когда-то преподавал музыку. Каждую сдачу называет маленькой паузой.",
                "Незнакомец посмотрел на часы: «А у вас всё ещё лето». На улице стоял сентябрь." };
            var result = new CustomerData[names.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = CreateInstance<CustomerData>();
                result[i].Configure(ids[i], names[i], hello[i], tales[i], Color.HSVToRGB(.06f + i * .095f, .25f, .37f + .035f * i), i == 2, i == 5);
                result[i].name = ids[i];
            }
            return result;
        }
    }
}
