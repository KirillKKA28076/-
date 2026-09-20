using System;
using UnityEngine;

namespace WarmBread
{
    public enum WeatherKind
    {
        Drizzle,
        Rain,
        Fog,
        Clear,
        Wind
    }

    public sealed class DayPlan
    {
        private readonly string[] focusProducts;

        public int Day { get; }
        public string Title { get; }
        public string Description { get; }
        public int SalesGoal { get; }
        public int RevenueGoal { get; }
        public int Rent { get; }
        public int GoalBonus { get; }
        public float CustomerIntervalMultiplier { get; }
        public float DeliverySeconds { get; }
        public WeatherKind Weather { get; }
        public string WeatherLabel { get; }

        public DayPlan(
            int day,
            string title,
            string description,
            int salesGoal,
            int revenueGoal,
            int rent,
            int goalBonus,
            float customerIntervalMultiplier,
            float deliverySeconds,
            WeatherKind weather,
            string weatherLabel,
            params string[] focusProducts)
        {
            Day = Mathf.Max(1, day);
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            SalesGoal = Mathf.Max(1, salesGoal);
            RevenueGoal = Mathf.Max(0, revenueGoal);
            Rent = Mathf.Max(0, rent);
            GoalBonus = Mathf.Max(0, goalBonus);
            CustomerIntervalMultiplier = Mathf.Clamp(customerIntervalMultiplier, .45f, 2f);
            DeliverySeconds = Mathf.Clamp(deliverySeconds, 8f, 90f);
            Weather = weather;
            WeatherLabel = weatherLabel ?? string.Empty;
            this.focusProducts = focusProducts ?? new string[0];
        }

        public float DemandWeight(ProductData product)
        {
            if (product == null || string.IsNullOrWhiteSpace(product.Id)) return 0f;
            for (var index = 0; index < focusProducts.Length; index++)
            {
                if (string.Equals(focusProducts[index], product.Id, StringComparison.Ordinal))
                {
                    return 2.35f;
                }
            }

            if (product.Id.StartsWith("bread_", StringComparison.Ordinal)) return 1.2f;
            return 1f;
        }
    }

    public static class CampaignRules
    {
        public const int CampaignDays = 7;

        private static readonly DayPlan[] Plans =
        {
            new DayPlan(
                1,
                "Первое утро",
                "Район присматривается к новому продавцу. Главное — не торопиться и считать сдачу.",
                5,
                7500,
                5000,
                1500,
                1f,
                25f,
                WeatherKind.Drizzle,
                "мелкий дождь",
                "bread_white",
                "bread_black"),
            new DayPlan(
                2,
                "Холодный дождь",
                "Дождь усилился. Горячие пирожки разбирают быстрее, а мокрая очередь ждёт меньше.",
                7,
                11000,
                5000,
                2000,
                .9f,
                24f,
                WeatherKind.Rain,
                "сильный дождь",
                "pirozhok_meat",
                "pirozhok_potato"),
            new DayPlan(
                3,
                "После школы",
                "К обеду у остановки станет шумнее. Дети чаще берут лимонад, булочки и жвачку.",
                8,
                14000,
                5000,
                2500,
                .88f,
                23f,
                WeatherKind.Clear,
                "светло после дождя",
                "gum",
                "lemonade",
                "bulochka"),
            new DayPlan(
                4,
                "День получки",
                "Во дворе больше людей и меньше терпения. Запас лучше заказать заранее.",
                10,
                19000,
                5500,
                3000,
                .72f,
                20f,
                WeatherKind.Clear,
                "холодное ясное утро",
                "bread_white",
                "sig_camel",
                "sig_java"),
            new DayPlan(
                5,
                "Туманное утро",
                "Город звучит тише. Постоянные покупатели ждут знакомого света в окошке.",
                10,
                20000,
                5500,
                3500,
                .82f,
                22f,
                WeatherKind.Fog,
                "густой туман",
                "bread_black",
                "water"),
            new DayPlan(
                6,
                "Ветер с рынка",
                "Порывы гонят листья вдоль остановки. Поток быстрый, спрос меняется, поставщик приезжает раньше.",
                12,
                23000,
                6000,
                4500,
                .65f,
                18f,
                WeatherKind.Wind,
                "сильный ветер",
                "bulochka",
                "pirozhok_potato",
                "lemonade"),
            new DayPlan(
                7,
                "Воскресенье двора",
                "За семь дней ларёк стал частью двора. Сегодня решится, каким его запомнят.",
                12,
                26000,
                6000,
                6000,
                .62f,
                18f,
                WeatherKind.Drizzle,
                "тёплый вечерний дождь",
                "bread_white",
                "bread_black",
                "bulochka")
        };

        public static DayPlan Get(int day)
        {
            day = Mathf.Max(1, day);
            if (day <= Plans.Length) return Plans[day - 1];

            var extra = day - CampaignDays;
            var weatherKinds = Enum.GetValues(typeof(WeatherKind)).Length;
            return new DayPlan(
                day,
                "Обычный день во дворе",
                "История завершена, но ларёк продолжает жить в свободном режиме.",
                Mathf.Min(20, 12 + extra / 2),
                Mathf.Min(50000, 24000 + extra * 1200),
                6000,
                Mathf.Min(8000, 3500 + extra * 250),
                Mathf.Max(.55f, .78f - extra * .015f),
                Mathf.Max(14f, 21f - extra * .25f),
                (WeatherKind)(extra % weatherKinds),
                "переменчивая погода",
                extra % 2 == 0 ? "bread_white" : "bread_black");
        }

        public static string EndingId(int reputation, int cash, int totalSales)
        {
            if (reputation >= 85 && cash >= 80000 && totalSales >= 55) return "home";
            if (reputation >= 60 && cash >= 35000) return "warm-light";
            return "hard-autumn";
        }

        public static string EndingTitle(string endingId)
        {
            switch (endingId)
            {
                case "home": return "Ларёк, который стал домом";
                case "warm-light": return "Тёплый свет в окне";
                default: return "Осень продолжается";
            }
        }

        public static string EndingText(string endingId)
        {
            switch (endingId)
            {
                case "home":
                    return "К концу недели люди уже здороваются издалека. Нина Петровна знает, когда привезут бородинский, Мишка оставляет мелочь ровной стопкой, а Плюш считает подоконник своим. Ларёк остаётся маленьким, но район теперь держится и за этот тёплый свет.";
                case "warm-light":
                    return "Неделя была неровной, но окошко открывалось каждое утро. У двора появилась привычка заходить за хлебом и задерживаться ещё на минуту. Иногда этого достаточно, чтобы место стало своим.";
                default:
                    return "Денег едва хватает, люди всё ещё присматриваются, а дождь не заканчивается. Но утром можно снова включить лампу, поставить чайник и попробовать ещё раз. История не закрывается одной трудной неделей.";
            }
        }
    }
}
