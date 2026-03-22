using NUnit.Framework;
using HijackPoker.Utils;

namespace HijackPoker.Tests
{
    public class BettingCalculatorTests
    {
        [Test]
        public void NoBet_ToCallIsZero()
        {
            var ctx = BettingCalculator.Calculate(currentBet: 0f, actorBet: 0f, actorStack: 100f, bigBlind: 2f);
            Assert.AreEqual(0f, ctx.ToCall);
        }

        [Test]
        public void BetToCall_CorrectAmount()
        {
            var ctx = BettingCalculator.Calculate(currentBet: 10f, actorBet: 2f, actorStack: 100f, bigBlind: 2f);
            Assert.AreEqual(8f, ctx.ToCall);
        }

        [Test]
        public void AllInCall_WhenCallExceedsStack()
        {
            var ctx = BettingCalculator.Calculate(currentBet: 200f, actorBet: 0f, actorStack: 50f, bigBlind: 2f);
            Assert.IsTrue(ctx.IsCallAllIn);
        }

        [Test]
        public void NotAllInCall_WhenStackCoversCall()
        {
            var ctx = BettingCalculator.Calculate(currentBet: 10f, actorBet: 0f, actorStack: 100f, bigBlind: 2f);
            Assert.IsFalse(ctx.IsCallAllIn);
        }

        [Test]
        public void CanRaise_WhenStackExceedsCall()
        {
            var ctx = BettingCalculator.Calculate(currentBet: 10f, actorBet: 0f, actorStack: 100f, bigBlind: 2f);
            Assert.IsTrue(ctx.CanRaise);
        }

        [Test]
        public void CannotRaise_WhenStackEqualsCall()
        {
            var ctx = BettingCalculator.Calculate(currentBet: 10f, actorBet: 0f, actorStack: 10f, bigBlind: 2f);
            Assert.IsFalse(ctx.CanRaise);
        }

        [Test]
        public void MinRaise_IsDoubleCurrentBet_WhenLarger()
        {
            // currentBet=10, bigBlind=2: max(10+2, 10*2) = max(12, 20) = 20
            var ctx = BettingCalculator.Calculate(currentBet: 10f, actorBet: 0f, actorStack: 100f, bigBlind: 2f);
            Assert.AreEqual(20f, ctx.MinRaise);
        }

        [Test]
        public void MinRaise_IsCurrentBetPlusBigBlind_WhenLarger()
        {
            // currentBet=2, bigBlind=5: max(2+5, 2*2) = max(7, 4) = 7
            var ctx = BettingCalculator.Calculate(currentBet: 2f, actorBet: 0f, actorStack: 100f, bigBlind: 5f);
            Assert.AreEqual(7f, ctx.MinRaise);
        }

        [Test]
        public void ZeroBet_ZeroStack_NoAllIn()
        {
            var ctx = BettingCalculator.Calculate(currentBet: 0f, actorBet: 0f, actorStack: 0f, bigBlind: 2f);
            Assert.IsFalse(ctx.IsCallAllIn);
            Assert.IsFalse(ctx.CanRaise);
        }
    }
}
