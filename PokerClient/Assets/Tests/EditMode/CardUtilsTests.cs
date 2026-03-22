using NUnit.Framework;
using HijackPoker.Utils;

namespace HijackPoker.Tests
{
    public class CardUtilsTests
    {
        [Test] public void ParseCard_AH() { var (r, s) = CardUtils.ParseCard("AH"); Assert.AreEqual("A", r); Assert.AreEqual("H", s); }
        [Test] public void ParseCard_10D() { var (r, s) = CardUtils.ParseCard("10D"); Assert.AreEqual("10", r); Assert.AreEqual("D", s); }
        [Test] public void ParseCard_2C() { var (r, s) = CardUtils.ParseCard("2C"); Assert.AreEqual("2", r); Assert.AreEqual("C", s); }
        [Test] public void ParseCard_KS() { var (r, s) = CardUtils.ParseCard("KS"); Assert.AreEqual("K", r); Assert.AreEqual("S", s); }

        [Test] public void IsRedSuit_Hearts() => Assert.IsTrue(CardUtils.IsRedSuit("H"));
        [Test] public void IsRedSuit_Diamonds() => Assert.IsTrue(CardUtils.IsRedSuit("D"));
        [Test] public void IsBlackSuit_Clubs() => Assert.IsFalse(CardUtils.IsRedSuit("C"));
        [Test] public void IsBlackSuit_Spades() => Assert.IsFalse(CardUtils.IsRedSuit("S"));

        [Test] public void DisplayString_AH() => Assert.AreEqual("A\u2665", CardUtils.GetDisplayString("AH"));
        [Test] public void DisplayString_10D() => Assert.AreEqual("10\u2666", CardUtils.GetDisplayString("10D"));
        [Test] public void DisplayString_2C() => Assert.AreEqual("2\u2663", CardUtils.GetDisplayString("2C"));
        [Test] public void DisplayString_KS() => Assert.AreEqual("K\u2660", CardUtils.GetDisplayString("KS"));

        [Test] public void MoneyFormat_150() => Assert.AreEqual("$150.00", MoneyFormatter.Format(150f));
        [Test] public void MoneyFormat_Zero() => Assert.AreEqual("$0.00", MoneyFormatter.Format(0f));
        [Test] public void MoneyFormatGain_24() => Assert.AreEqual("+$24.00", MoneyFormatter.FormatGain(24f));
        [Test] public void MoneyFormatGain_Negative() => Assert.AreEqual("-$5.00", MoneyFormatter.FormatGain(-5f));

        [Test] public void StepLabel_6_IsDealingFlop()
        {
            var mgr = new UnityEngine.GameObject().AddComponent<HijackPoker.Managers.TableStateManager>();
            Assert.AreEqual("Dealing Flop", mgr.GetStepLabel(6));
            UnityEngine.Object.DestroyImmediate(mgr.gameObject);
        }
    }
}
