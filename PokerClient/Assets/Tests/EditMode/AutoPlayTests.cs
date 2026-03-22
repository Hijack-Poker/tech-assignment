using NUnit.Framework;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.Utils;

namespace HijackPoker.Tests
{
    public class AutoPlayTests
    {
        private PlayerState MakeActor(float stack = 100f, float bet = 0f)
        {
            return new PlayerState { Stack = stack, Bet = bet, Status = "1" };
        }

        // ── Safe style ──

        [Test]
        public void Safe_NoBet_Checks()
        {
            var ctx = BettingCalculator.Calculate(0f, 0f, 100f, 2f);
            var result = AutoPlayDecision.Decide(AutoPlayStyle.Safe, ctx, MakeActor(), false, 0.5f);
            Assert.AreEqual("check", result.Action);
            Assert.AreEqual(0f, result.Amount);
        }

        [Test]
        public void Safe_BetExists_Calls()
        {
            var ctx = BettingCalculator.Calculate(10f, 0f, 100f, 2f);
            var result = AutoPlayDecision.Decide(AutoPlayStyle.Safe, ctx, MakeActor(), false, 0.5f);
            Assert.AreEqual("call", result.Action);
            Assert.AreEqual(10f, result.Amount);
        }

        // ── SmallRandom style ──

        [Test]
        public void SmallRandom_LowRoll_Raises()
        {
            var ctx = BettingCalculator.Calculate(10f, 0f, 100f, 2f);
            var result = AutoPlayDecision.Decide(AutoPlayStyle.SmallRandom, ctx, MakeActor(), false, 0.1f);
            Assert.AreEqual("raise", result.Action);
        }

        [Test]
        public void SmallRandom_AlreadyRaised_Calls()
        {
            var ctx = BettingCalculator.Calculate(10f, 0f, 100f, 2f);
            var result = AutoPlayDecision.Decide(AutoPlayStyle.SmallRandom, ctx, MakeActor(), true, 0.1f);
            Assert.AreEqual("call", result.Action);
        }

        [Test]
        public void SmallRandom_NoBet_LowRoll_Bets()
        {
            var ctx = BettingCalculator.Calculate(0f, 0f, 100f, 2f);
            var result = AutoPlayDecision.Decide(AutoPlayStyle.SmallRandom, ctx, MakeActor(), false, 0.1f);
            Assert.AreEqual("bet", result.Action);
        }

        // ── Hard style ──

        [Test]
        public void Hard_VeryLowRoll_Folds()
        {
            var ctx = BettingCalculator.Calculate(10f, 0f, 100f, 2f);
            var result = AutoPlayDecision.Decide(AutoPlayStyle.Hard, ctx, MakeActor(), false, 0.05f);
            Assert.AreEqual("fold", result.Action);
        }

        [Test]
        public void Hard_MidRoll_CallsOrChecks()
        {
            var ctx = BettingCalculator.Calculate(10f, 0f, 100f, 2f);
            var result = AutoPlayDecision.Decide(AutoPlayStyle.Hard, ctx, MakeActor(), false, 0.3f);
            Assert.AreEqual("call", result.Action);
        }

        [Test]
        public void Hard_HighRoll_CannotRaise_Calls()
        {
            // Stack equals toCall, so canRaise is false
            var ctx = BettingCalculator.Calculate(100f, 0f, 100f, 2f);
            var result = AutoPlayDecision.Decide(AutoPlayStyle.Hard, ctx, MakeActor(stack: 100f), false, 0.85f);
            Assert.AreEqual("call", result.Action);
        }
    }
}
