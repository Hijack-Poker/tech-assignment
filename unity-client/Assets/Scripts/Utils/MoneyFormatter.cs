using System.Globalization;

namespace HijackPoker.Utils
{
    public static class MoneyFormatter
    {
        public static string Format(float amount)
        {
            if (amount < 0)
                return $"-${(-amount).ToString("#,##0.00", CultureInfo.InvariantCulture)}";

            return $"${amount.ToString("#,##0.00", CultureInfo.InvariantCulture)}";
        }
    }
}
