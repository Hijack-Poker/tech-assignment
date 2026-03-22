using NUnit.Framework;
using HijackPoker.Utils;
using HijackPoker.Models;

namespace HijackPoker.Tests
{
    [TestFixture]
    public class ShowdownLogicTests
    {
        // ── Active player (status "1") ──

        [Test]
        public void Active_BeforeShowdown_HideCards()
        {
            Assert.IsFalse(ShowdownLogic.ShouldShowCards(5, PlayerStatusCode.Active, 0));
        }

        [Test]
        public void Active_AtShowdown_ShowCards()
        {
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(12, PlayerStatusCode.Active, 0));
        }

        [Test]
        public void Active_AfterShowdown_ShowCards()
        {
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(14, PlayerStatusCode.Active, 0));
        }

        // ── Folded player (status "11") ──

        [Test]
        public void Folded_BeforeShowdown_HideCards()
        {
            Assert.IsFalse(ShowdownLogic.ShouldShowCards(5, PlayerStatusCode.Folded, 0));
        }

        [Test]
        public void Folded_AtShowdown_HideCards()
        {
            Assert.IsFalse(ShowdownLogic.ShouldShowCards(12, PlayerStatusCode.Folded, 0));
        }

        [Test]
        public void Folded_AtShowdown_Step15_HideCards()
        {
            Assert.IsFalse(ShowdownLogic.ShouldShowCards(15, PlayerStatusCode.Folded, 0));
        }

        // ── All-In player (status "12") ──

        [Test]
        public void AllIn_BeforeShowdown_HideCards()
        {
            Assert.IsFalse(ShowdownLogic.ShouldShowCards(5, PlayerStatusCode.AllIn, 0));
        }

        [Test]
        public void AllIn_AtShowdown_ShowCards()
        {
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(12, PlayerStatusCode.AllIn, 0));
        }

        [Test]
        public void AllIn_DuringBetting_HideCards()
        {
            Assert.IsFalse(ShowdownLogic.ShouldShowCards(9, PlayerStatusCode.AllIn, 0));
        }

        // ── Show Cards status ("4") ──

        [Test]
        public void ShowCards_BeforeShowdown_ShowCards()
        {
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(3, PlayerStatusCode.ShowCards, 0));
        }

        [Test]
        public void ShowCards_AtShowdown_ShowCards()
        {
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(12, PlayerStatusCode.ShowCards, 0));
        }

        [Test]
        public void ShowCards_Step0_ShowCards()
        {
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(0, PlayerStatusCode.ShowCards, 0));
        }

        // ── Winner override (winnings > 0) ──

        [Test]
        public void Winner_ActiveStatus_ShowCards()
        {
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(14, PlayerStatusCode.Active, 24.0f));
        }

        [Test]
        public void Winner_FoldedStatus_ShowCards()
        {
            // Edge case: a folded player with winnings > 0 (shouldn't happen normally, but winnings override)
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(14, PlayerStatusCode.Folded, 10.0f));
        }

        [Test]
        public void Winner_BeforeShowdown_ShowCards()
        {
            // Winnings override even before showdown
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(5, PlayerStatusCode.Active, 50.0f));
        }

        [Test]
        public void Winner_AllInStatus_ShowCards()
        {
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(14, PlayerStatusCode.AllIn, 100.0f));
        }

        // ── Other statuses ──

        [Test]
        public void SittingOut_BeforeShowdown_HideCards()
        {
            Assert.IsFalse(ShowdownLogic.ShouldShowCards(5, PlayerStatusCode.SittingOut, 0));
        }

        [Test]
        public void SittingOut_AtShowdown_ShowCards()
        {
            // Status "2" is not explicitly folded, so showdown reveals
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(12, PlayerStatusCode.SittingOut, 0));
        }

        [Test]
        public void PostBlind_BeforeShowdown_HideCards()
        {
            Assert.IsFalse(ShowdownLogic.ShouldShowCards(3, PlayerStatusCode.PostBlind, 0));
        }

        [Test]
        public void PostBlind_AtShowdown_ShowCards()
        {
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(12, PlayerStatusCode.PostBlind, 0));
        }

        // ── Boundary: step 11 vs 12 ──

        [Test]
        public void Active_Step11_HideCards()
        {
            Assert.IsFalse(ShowdownLogic.ShouldShowCards(11, PlayerStatusCode.Active, 0));
        }

        [Test]
        public void Active_Step12_ShowCards()
        {
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(12, PlayerStatusCode.Active, 0));
        }

        // ── All steps for active player ──

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(3, false)]
        [TestCase(4, false)]
        [TestCase(5, false)]
        [TestCase(6, false)]
        [TestCase(7, false)]
        [TestCase(8, false)]
        [TestCase(9, false)]
        [TestCase(10, false)]
        [TestCase(11, false)]
        [TestCase(12, true)]
        [TestCase(13, true)]
        [TestCase(14, true)]
        [TestCase(15, true)]
        public void Active_AllSteps(int step, bool expected)
        {
            Assert.AreEqual(expected, ShowdownLogic.ShouldShowCards(step, PlayerStatusCode.Active, 0));
        }
    }
}
