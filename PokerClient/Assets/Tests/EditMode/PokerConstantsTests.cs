using NUnit.Framework;
using HijackPoker.Utils;

namespace HijackPoker.Tests
{
    public class PokerConstantsTests
    {
        [Test] public void IsBettingStep_Step0_False() => Assert.IsFalse(PokerConstants.IsBettingStep(0));
        [Test] public void IsBettingStep_Step1_False() => Assert.IsFalse(PokerConstants.IsBettingStep(1));
        [Test] public void IsBettingStep_Step2_False() => Assert.IsFalse(PokerConstants.IsBettingStep(2));
        [Test] public void IsBettingStep_Step3_False() => Assert.IsFalse(PokerConstants.IsBettingStep(3));
        [Test] public void IsBettingStep_Step4_False() => Assert.IsFalse(PokerConstants.IsBettingStep(4));
        [Test] public void IsBettingStep_Step5_True() => Assert.IsTrue(PokerConstants.IsBettingStep(5));
        [Test] public void IsBettingStep_Step6_False() => Assert.IsFalse(PokerConstants.IsBettingStep(6));
        [Test] public void IsBettingStep_Step7_True() => Assert.IsTrue(PokerConstants.IsBettingStep(7));
        [Test] public void IsBettingStep_Step8_False() => Assert.IsFalse(PokerConstants.IsBettingStep(8));
        [Test] public void IsBettingStep_Step9_True() => Assert.IsTrue(PokerConstants.IsBettingStep(9));
        [Test] public void IsBettingStep_Step10_False() => Assert.IsFalse(PokerConstants.IsBettingStep(10));
        [Test] public void IsBettingStep_Step11_True() => Assert.IsTrue(PokerConstants.IsBettingStep(11));
        [Test] public void IsBettingStep_Step12_False() => Assert.IsFalse(PokerConstants.IsBettingStep(12));
        [Test] public void IsBettingStep_Step13_False() => Assert.IsFalse(PokerConstants.IsBettingStep(13));
        [Test] public void IsBettingStep_Step14_False() => Assert.IsFalse(PokerConstants.IsBettingStep(14));
        [Test] public void IsBettingStep_Step15_False() => Assert.IsFalse(PokerConstants.IsBettingStep(15));

        [Test] public void TurnDuration_Is20() => Assert.AreEqual(20f, PokerConstants.TurnDurationSeconds);
        [Test] public void LowTimeWarning_Is5() => Assert.AreEqual(5f, PokerConstants.LowTimeWarningSeconds);
        [Test] public void MaxNameLength_Is20() => Assert.AreEqual(20, PokerConstants.MaxNameLength);
    }
}
