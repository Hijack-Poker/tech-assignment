using NUnit.Framework;
using HijackPoker.Utils;
using HijackPoker.Models;

namespace HijackPoker.Tests
{
    [TestFixture]
    public class ShowdownLogicEdgeCaseTests
    {
        // ── All-fold scenario: only one player remains active ──

        [Test]
        public void AllFold_LastPlayerActive_BeforeShowdown_HideCards()
        {
            // Even if everyone else folded, the last active player's cards stay hidden pre-showdown
            Assert.IsFalse(ShowdownLogic.ShouldShowCards(5, PlayerStatusCode.Active, 0));
        }

        [Test]
        public void AllFold_WinnerByDefault_ShowCards()
        {
            // Winner by default (everyone else folded) — winnings > 0 reveals cards
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(5, PlayerStatusCode.Active, 24.0f));
        }

        [Test]
        public void AllFold_FoldedPlayers_StayHidden()
        {
            Assert.IsFalse(ShowdownLogic.ShouldShowCards(5, PlayerStatusCode.Folded, 0));
        }

        // ── Heads-up scenarios ──

        [Test]
        public void HeadsUp_BothActive_AtShowdown_ShowCards()
        {
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(12, PlayerStatusCode.Active, 0));
        }

        [Test]
        public void HeadsUp_OneAllIn_OneActive_AtShowdown_BothShow()
        {
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(12, PlayerStatusCode.AllIn, 0));
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(12, PlayerStatusCode.Active, 0));
        }

        [Test]
        public void HeadsUp_OneFolds_WinnerRevealed()
        {
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(9, PlayerStatusCode.Active, 30.0f));
            Assert.IsFalse(ShowdownLogic.ShouldShowCards(9, PlayerStatusCode.Folded, 0));
        }

        // ── Side pot scenarios (multiple all-ins) ──

        [Test]
        public void MultipleSidePots_AllInPlayers_AtShowdown_AllShow()
        {
            // Three all-in players at showdown all reveal
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(12, PlayerStatusCode.AllIn, 0));
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(13, PlayerStatusCode.AllIn, 0));
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(14, PlayerStatusCode.AllIn, 0));
        }

        [Test]
        public void MultipleSidePots_AllInWinner_ShowCards()
        {
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(14, PlayerStatusCode.AllIn, 60.0f));
        }

        [Test]
        public void MultipleSidePots_AllInLoser_AtShowdown_ShowCards()
        {
            // Even losing all-in players show at showdown (step >= 12, not folded)
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(14, PlayerStatusCode.AllIn, 0));
        }

        // ── Edge: winnings exactly 0 vs small positive ──

        [Test]
        public void Winnings_ExactlyZero_NoReveal_BeforeShowdown()
        {
            Assert.IsFalse(ShowdownLogic.ShouldShowCards(5, PlayerStatusCode.Active, 0f));
        }

        [Test]
        public void Winnings_SmallPositive_Reveals()
        {
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(5, PlayerStatusCode.Active, 0.01f));
        }

        [Test]
        public void Winnings_VeryLarge_Reveals()
        {
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(3, PlayerStatusCode.Active, 999999.99f));
        }

        // ── Edge: step boundaries ──

        [Test]
        public void Step0_NoOneRevealed()
        {
            Assert.IsFalse(ShowdownLogic.ShouldShowCards(0, PlayerStatusCode.Active, 0));
            Assert.IsFalse(ShowdownLogic.ShouldShowCards(0, PlayerStatusCode.AllIn, 0));
            Assert.IsFalse(ShowdownLogic.ShouldShowCards(0, PlayerStatusCode.Folded, 0));
        }

        [Test]
        public void ShowCards_Status_OverridesEverything()
        {
            // ShowCards status reveals at any step, even step 0
            for (int step = 0; step <= 15; step++)
            {
                Assert.IsTrue(ShowdownLogic.ShouldShowCards(step, PlayerStatusCode.ShowCards, 0),
                    $"ShowCards status should reveal at step {step}");
            }
        }

        [Test]
        public void Folded_NeverRevealed_RegardlessOfStep()
        {
            // Folded players never show cards regardless of step (unless winnings > 0)
            for (int step = 0; step <= 15; step++)
            {
                Assert.IsFalse(ShowdownLogic.ShouldShowCards(step, PlayerStatusCode.Folded, 0),
                    $"Folded player should be hidden at step {step}");
            }
        }

        // ── Null/empty status edge cases ──

        [Test]
        public void NullStatus_BeforeShowdown_HideCards()
        {
            Assert.IsFalse(ShowdownLogic.ShouldShowCards(5, null, 0));
        }

        [Test]
        public void NullStatus_AtShowdown_ShowCards()
        {
            // null status is not "11" (folded), so showdown rule applies
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(12, null, 0));
        }

        [Test]
        public void EmptyStatus_AtShowdown_ShowCards()
        {
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(12, "", 0));
        }

        [Test]
        public void UnknownStatus_AtShowdown_ShowCards()
        {
            // Unknown status code is not explicitly folded, so showdown reveals
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(12, "99", 0));
        }
    }
}
