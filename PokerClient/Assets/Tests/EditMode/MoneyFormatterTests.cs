using NUnit.Framework;
using HijackPoker.Utils;

namespace HijackPoker.Tests
{
    public class MoneyFormatterTests
    {
        [Test] public void Format_WholeNumber() => Assert.AreEqual("$100.00", MoneyFormatter.Format(100f));
        [Test] public void Format_Decimal() => Assert.AreEqual("$99.50", MoneyFormatter.Format(99.5f));
        [Test] public void Format_Zero() => Assert.AreEqual("$0.00", MoneyFormatter.Format(0f));
        [Test] public void Format_SmallAmount() => Assert.AreEqual("$0.25", MoneyFormatter.Format(0.25f));

        [Test] public void FormatGain_Positive() => Assert.AreEqual("+$50.00", MoneyFormatter.FormatGain(50f));
        [Test] public void FormatGain_Zero() => Assert.AreEqual("+$0.00", MoneyFormatter.FormatGain(0f));
        [Test] public void FormatGain_Negative() => Assert.AreEqual("-$10.00", MoneyFormatter.FormatGain(-10f));
    }
}
