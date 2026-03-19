using NUnit.Framework;
using HijackPoker.UI;

namespace HijackPoker.Tests
{
    [TestFixture]
    public class ChipStackViewTests
    {
        [Test]
        public void DecomposeBet_Zero_ReturnsEmpty()
        {
            var result = ChipStackView.DecomposeBet(0);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void DecomposeBet_SingleDollar_ReturnsOneWhite()
        {
            var result = ChipStackView.DecomposeBet(1);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(1, result[0].count);
            Assert.AreEqual(3, result[0].denomIdx); // white = index 3
        }

        [Test]
        public void DecomposeBet_FiveDollars_ReturnsOneRed()
        {
            var result = ChipStackView.DecomposeBet(5);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(1, result[0].count);
            Assert.AreEqual(2, result[0].denomIdx); // red = index 2
        }

        [Test]
        public void DecomposeBet_TwentyFive_ReturnsOneGreen()
        {
            var result = ChipStackView.DecomposeBet(25);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(1, result[0].count);
            Assert.AreEqual(1, result[0].denomIdx); // green = index 1
        }

        [Test]
        public void DecomposeBet_Hundred_ReturnsOneBlack()
        {
            var result = ChipStackView.DecomposeBet(100);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(1, result[0].count);
            Assert.AreEqual(0, result[0].denomIdx); // black = index 0
        }

        [Test]
        public void DecomposeBet_MixedAmount_CorrectBreakdown()
        {
            // 137 = 1×100 + 1×25 + 2×5 + 2×1
            var result = ChipStackView.DecomposeBet(137);
            Assert.AreEqual(4, result.Count);

            Assert.AreEqual(1, result[0].count); // 100
            Assert.AreEqual(0, result[0].denomIdx);

            Assert.AreEqual(1, result[1].count); // 25
            Assert.AreEqual(1, result[1].denomIdx);

            Assert.AreEqual(2, result[2].count); // 5×2
            Assert.AreEqual(2, result[2].denomIdx);

            Assert.AreEqual(2, result[3].count); // 1×2
            Assert.AreEqual(3, result[3].denomIdx);
        }

        [Test]
        public void DecomposeBet_LargeAmount_HandlesMultipleHundreds()
        {
            // 350 = 3×100 + 2×25
            var result = ChipStackView.DecomposeBet(350);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(3, result[0].count);
            Assert.AreEqual(0, result[0].denomIdx);
            Assert.AreEqual(2, result[1].count);
            Assert.AreEqual(1, result[1].denomIdx);
        }

        [Test]
        public void DecomposeBet_FractionalAmount_RoundsToNearest()
        {
            // 5.50 rounds to 6 = 1×5 + 1×1
            var result = ChipStackView.DecomposeBet(5.5f);
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void DecomposeBet_SmallFraction_RoundsDown()
        {
            // 0.4 rounds to 0
            var result = ChipStackView.DecomposeBet(0.4f);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void DecomposeBet_ExactDenomination_NoRemainder()
        {
            // 50 = 2×25
            var result = ChipStackView.DecomposeBet(50);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(2, result[0].count);
            Assert.AreEqual(1, result[0].denomIdx);
        }
    }
}
