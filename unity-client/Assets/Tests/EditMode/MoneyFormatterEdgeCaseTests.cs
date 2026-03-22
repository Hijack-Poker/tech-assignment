using NUnit.Framework;
using HijackPoker.Utils;

namespace HijackPoker.Tests
{
    [TestFixture]
    public class MoneyFormatterEdgeCaseTests
    {
        // ── Zero and near-zero ──

        [Test]
        public void Format_NegativeZero_FormatsAsZero()
        {
            Assert.AreEqual("$0.00", MoneyFormatter.Format(-0.0f));
        }

        [Test]
        public void Format_VerySmallPositive()
        {
            Assert.AreEqual("$0.01", MoneyFormatter.Format(0.01f));
        }

        // ── Large numbers ──

        [Test]
        public void Format_HundredThousand()
        {
            Assert.AreEqual("$100,000.00", MoneyFormatter.Format(100000f));
        }

        [Test]
        public void Format_OneMillion()
        {
            Assert.AreEqual("$1,000,000.00", MoneyFormatter.Format(1000000f));
        }

        [Test]
        public void Format_TenMillion()
        {
            Assert.AreEqual("$10,000,000.00", MoneyFormatter.Format(10000000f));
        }

        // ── Negative values ──

        [Test]
        public void Format_NegativeSmall()
        {
            Assert.AreEqual("-$0.50", MoneyFormatter.Format(-0.5f));
        }

        [Test]
        public void Format_NegativeLargeWithCommas()
        {
            Assert.AreEqual("-$100,000.00", MoneyFormatter.Format(-100000f));
        }

        [Test]
        public void Format_NegativeOneCent()
        {
            Assert.AreEqual("-$0.01", MoneyFormatter.Format(-0.01f));
        }

        // ── Poker-specific amounts ──

        [Test]
        public void Format_SmallBlind()
        {
            Assert.AreEqual("$1.00", MoneyFormatter.Format(1.0f));
        }

        [Test]
        public void Format_BigBlind()
        {
            Assert.AreEqual("$2.00", MoneyFormatter.Format(2.0f));
        }

        [Test]
        public void Format_TypicalPot()
        {
            Assert.AreEqual("$24.00", MoneyFormatter.Format(24.0f));
        }

        [Test]
        public void Format_TypicalStack()
        {
            Assert.AreEqual("$200.00", MoneyFormatter.Format(200.0f));
        }

        [Test]
        public void Format_HighStakesPot()
        {
            Assert.AreEqual("$50,000.00", MoneyFormatter.Format(50000f));
        }

        // ── Consistency: format then compare ──

        [Test]
        public void Format_AlwaysHasDollarSign()
        {
            Assert.IsTrue(MoneyFormatter.Format(0).Contains("$"));
            Assert.IsTrue(MoneyFormatter.Format(100f).Contains("$"));
            Assert.IsTrue(MoneyFormatter.Format(-50f).Contains("$"));
        }

        [Test]
        public void Format_AlwaysHasTwoDecimalPlaces()
        {
            string result = MoneyFormatter.Format(100f);
            int dotIndex = result.IndexOf('.');
            Assert.AreEqual(result.Length - 3, dotIndex, "Should have exactly 2 decimal places");
        }
    }
}
