using System;
using System.Globalization;

namespace WarmBread
{
    public static class Money
    {
        public static readonly int[] Denominations =
        {
            10000,
            5000,
            1000,
            500,
            200,
            100,
            50,
            10
        };

        public static string Format(int kopecks)
        {
            return (kopecks / 100m).ToString("0.##", CultureInfo.GetCultureInfo("ru-RU")) + " ₽";
        }

        public static int Change(int total, int paid)
        {
            if (total < 0) throw new ArgumentOutOfRangeException(nameof(total));
            if (paid < total) throw new ArgumentOutOfRangeException(nameof(paid));
            return paid - total;
        }

        public static int Tender(int total)
        {
            if (total < 0) throw new ArgumentOutOfRangeException(nameof(total));
            if (total <= 5000) return 5000;
            if (total <= 10000) return 10000;
            return checked(((total + 9999) / 10000) * 10000);
        }
    }
}
