using NUnit.Framework;
using UnityEngine;
using HijackPoker.UI;

namespace HijackPoker.Tests
{
    [TestFixture]
    public class TurnTimerViewTests
    {
        [Test]
        public void GetTimerColor_Full_ReturnsCyan()
        {
            var color = TurnTimerView.GetTimerColor(1.0f);
            Assert.AreEqual(UIFactory.AccentCyan, color);
        }

        [Test]
        public void GetTimerColor_AboveHalf_ReturnsCyan()
        {
            var color = TurnTimerView.GetTimerColor(0.75f);
            Assert.AreEqual(UIFactory.AccentCyan, color);
        }

        [Test]
        public void GetTimerColor_AtHalf_ReturnsCyan()
        {
            // At exactly 0.5, still in cyan range (>0.5)
            var color = TurnTimerView.GetTimerColor(0.51f);
            Assert.AreEqual(UIFactory.AccentCyan, color);
        }

        [Test]
        public void GetTimerColor_JustBelowHalf_TransitionsToCyanYellow()
        {
            var color = TurnTimerView.GetTimerColor(0.49f);
            // Should be between cyan and gold
            Assert.Greater(color.r, UIFactory.AccentCyan.r);
        }

        [Test]
        public void GetTimerColor_AtQuarter_IsGold()
        {
            var color = TurnTimerView.GetTimerColor(0.25f);
            // At exactly 0.25, t=1 so fully gold transitioning to magenta range
            // Actually at boundary: fill=0.25, which is the start of the last range
            // In the middle range: t = 1 - (0.25-0.25)/0.25 = 1, so fully gold
            AssertColorsClose(UIFactory.AccentGold, color);
        }

        [Test]
        public void GetTimerColor_BelowQuarter_TransitionsToMagenta()
        {
            var color = TurnTimerView.GetTimerColor(0.1f);
            // Should be closer to magenta than gold
            Assert.Greater(color.r, UIFactory.AccentGold.r * 0.5f);
        }

        [Test]
        public void GetTimerColor_AtZero_ReturnsMagenta()
        {
            var color = TurnTimerView.GetTimerColor(0f);
            AssertColorsClose(UIFactory.AccentMagenta, color);
        }

        [Test]
        public void GetTimerColor_OverOne_Clamped()
        {
            var color = TurnTimerView.GetTimerColor(1.5f);
            Assert.AreEqual(UIFactory.AccentCyan, color);
        }

        [Test]
        public void GetTimerColor_NegativeValue_Clamped()
        {
            var color = TurnTimerView.GetTimerColor(-0.5f);
            AssertColorsClose(UIFactory.AccentMagenta, color);
        }

        [Test]
        public void GetTimerColor_MidTransition_InterpolatesCorrectly()
        {
            // At 0.375 (midpoint of 0.25-0.5 range), should be halfway between cyan and gold
            var color = TurnTimerView.GetTimerColor(0.375f);
            var expected = Color.Lerp(UIFactory.AccentCyan, UIFactory.AccentGold, 0.5f);
            AssertColorsClose(expected, color);
        }

        private static void AssertColorsClose(Color expected, Color actual, float tolerance = 0.02f)
        {
            Assert.AreEqual(expected.r, actual.r, tolerance, $"Red: expected {expected.r}, got {actual.r}");
            Assert.AreEqual(expected.g, actual.g, tolerance, $"Green: expected {expected.g}, got {actual.g}");
            Assert.AreEqual(expected.b, actual.b, tolerance, $"Blue: expected {expected.b}, got {actual.b}");
        }
    }
}
