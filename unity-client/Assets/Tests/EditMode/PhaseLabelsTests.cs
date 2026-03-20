using NUnit.Framework;
using HijackPoker.Utils;

namespace HijackPoker.Tests
{
    [TestFixture]
    public class PhaseLabelsTests
    {
        [TestCase(0, "Preparing Hand")]
        [TestCase(1, "Setting Up Dealer")]
        [TestCase(2, "Posting Small Blind")]
        [TestCase(3, "Posting Big Blind")]
        [TestCase(4, "Dealing Hole Cards")]
        [TestCase(5, "Pre-Flop Betting")]
        [TestCase(6, "Dealing Flop")]
        [TestCase(7, "Flop Betting")]
        [TestCase(8, "Dealing Turn")]
        [TestCase(9, "Turn Betting")]
        [TestCase(10, "Dealing River")]
        [TestCase(11, "River Betting")]
        [TestCase(12, "Showdown")]
        [TestCase(13, "Evaluating Hands")]
        [TestCase(14, "Paying Winners")]
        [TestCase(15, "Hand Complete")]
        public void GetLabel_AllSteps(int step, string expected)
        {
            Assert.AreEqual(expected, PhaseLabels.GetLabel(step));
        }

        [Test]
        public void GetLabel_NegativeStep_ReturnsFallback()
        {
            var label = PhaseLabels.GetLabel(-1);
            Assert.AreEqual("Unknown Step (-1)", label);
        }

        [Test]
        public void GetLabel_Step16_ReturnsFallback()
        {
            var label = PhaseLabels.GetLabel(16);
            Assert.AreEqual("Unknown Step (16)", label);
        }

        [Test]
        public void GetLabel_LargeStep_ReturnsFallback()
        {
            var label = PhaseLabels.GetLabel(100);
            Assert.AreEqual("Unknown Step (100)", label);
        }

        [Test]
        public void GetLabel_Step6_DealingFlop()
        {
            Assert.AreEqual("Dealing Flop", PhaseLabels.GetLabel(6));
        }
    }
}
