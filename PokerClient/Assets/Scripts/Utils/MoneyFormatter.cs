namespace HijackPoker.Utils
{
    public static class MoneyFormatter
    {
        public static string Format(float amount) =>
            $"${amount:F2}";

        public static string FormatGain(float amount) =>
            amount >= 0 ? $"+${amount:F2}" : $"-${System.Math.Abs(amount):F2}";
    }
}
