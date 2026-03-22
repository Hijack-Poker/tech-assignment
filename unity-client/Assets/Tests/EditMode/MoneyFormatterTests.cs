using NUnit.Framework;
using HijackPoker.Utils;

namespace HijackPoker.Tests
{
    [TestFixture]
    public class MoneyFormatterTests
    {
        [Test]
        public void Format_WholeNumber()
        {
            Assert.AreEqual("$150.00", MoneyFormatter.Format(150.0f));
        }

        [Test]
        public void Format_WithCents()
        {
            Assert.AreEqual("$0.50", MoneyFormatter.Format(0.5f));
        }

        [Test]
        public void Format_WithThousandsSeparator()
        {
            Assert.AreEqual("$1,234.56", MoneyFormatter.Format(1234.56f));
        }

        [Test]
        public void Format_Zero()
        {
            Assert.AreEqual("$0.00", MoneyFormatter.Format(0));
        }

        [Test]
        public void Format_Negative()
        {
            Assert.AreEqual("-$5.00", MoneyFormatter.Format(-5.0f));
        }

        [Test]
        public void Format_SmallAmount()
        {
            Assert.AreEqual("$1.00", MoneyFormatter.Format(1.0f));
        }

        [Test]
        public void Format_LargeAmount()
        {
            Assert.AreEqual("$10,000.00", MoneyFormatter.Format(10000.0f));
        }

        [Test]
        public void Format_TwoCents()
        {
            Assert.AreEqual("$2.00", MoneyFormatter.Format(2.0f));
        }

        [Test]
        public void Format_FractionalCents_Rounds()
        {
            // float precision: 1234.5f should format fine
            Assert.AreEqual("$1,234.50", MoneyFormatter.Format(1234.5f));
        }

        [Test]
        public void Format_NegativeLargeAmount()
        {
            Assert.AreEqual("-$1,000.00", MoneyFormatter.Format(-1000.0f));
        }
    }
}
