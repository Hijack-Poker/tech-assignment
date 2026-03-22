using System.Collections.Generic;
using NUnit.Framework;
using HijackPoker.Analytics;

namespace HijackPoker.Tests
{
    [TestFixture]
    public class BoardTextureAnalyzerTests
    {
        [Test]
        public void Analyze_NullCards_ReturnsEmpty()
        {
            var tex = BoardTextureAnalyzer.Analyze(null);
            Assert.AreEqual("", tex.Description);
            Assert.IsFalse(tex.IsPaired);
        }

        [Test]
        public void Analyze_EmptyList_ReturnsEmpty()
        {
            var tex = BoardTextureAnalyzer.Analyze(new List<string>());
            Assert.AreEqual("", tex.Description);
        }

        [Test]
        public void Analyze_MonotoneBoard_Detected()
        {
            var cards = new List<string> { "AH", "KH", "9H" };
            var tex = BoardTextureAnalyzer.Analyze(cards);

            Assert.IsTrue(tex.IsMonotone);
            Assert.IsTrue(tex.HasFlushDraw);
            Assert.IsFalse(tex.IsRainbow);
            Assert.AreEqual("A", tex.HighCard);
        }

        [Test]
        public void Analyze_TwoToneBoard_Detected()
        {
            var cards = new List<string> { "AH", "KH", "9C" };
            var tex = BoardTextureAnalyzer.Analyze(cards);

            Assert.IsTrue(tex.IsTwoTone);
            Assert.IsFalse(tex.IsMonotone);
            Assert.IsFalse(tex.IsRainbow);
        }

        [Test]
        public void Analyze_RainbowBoard_Detected()
        {
            var cards = new List<string> { "AH", "KD", "9C" };
            var tex = BoardTextureAnalyzer.Analyze(cards);

            Assert.IsTrue(tex.IsRainbow);
            Assert.IsFalse(tex.IsTwoTone);
            Assert.IsFalse(tex.IsMonotone);
        }

        [Test]
        public void Analyze_PairedBoard_Detected()
        {
            var cards = new List<string> { "AH", "AD", "7C" };
            var tex = BoardTextureAnalyzer.Analyze(cards);

            Assert.IsTrue(tex.IsPaired);
            Assert.IsFalse(tex.IsTrips);
        }

        [Test]
        public void Analyze_TripsBoard_Detected()
        {
            var cards = new List<string> { "AH", "AD", "AC" };
            var tex = BoardTextureAnalyzer.Analyze(cards);

            Assert.IsTrue(tex.IsTrips);
            Assert.IsTrue(tex.IsPaired);
        }

        [Test]
        public void Analyze_ConnectedCards_StraightDraw()
        {
            var cards = new List<string> { "9H", "10D", "JC" };
            var tex = BoardTextureAnalyzer.Analyze(cards);

            Assert.IsTrue(tex.HasStraightDraw);
        }

        [Test]
        public void Analyze_DisconnectedCards_NoStraightDraw()
        {
            var cards = new List<string> { "2H", "7D", "KC" };
            var tex = BoardTextureAnalyzer.Analyze(cards);

            Assert.IsFalse(tex.HasStraightDraw);
        }

        [Test]
        public void Analyze_WetBoard_HighWetness()
        {
            // Monotone + connected = very wet
            var cards = new List<string> { "9H", "10H", "JH" };
            var tex = BoardTextureAnalyzer.Analyze(cards);

            Assert.Greater(tex.WetnessRating, 7f);
        }

        [Test]
        public void Analyze_DryBoard_LowWetness()
        {
            // Rainbow, disconnected, unpaired
            var cards = new List<string> { "2H", "7D", "KC" };
            var tex = BoardTextureAnalyzer.Analyze(cards);

            Assert.Less(tex.WetnessRating, 5f);
        }

        [Test]
        public void Analyze_FiveCards_WorksCorrectly()
        {
            var cards = new List<string> { "AH", "KH", "QH", "JD", "10C" };
            var tex = BoardTextureAnalyzer.Analyze(cards);

            Assert.AreEqual("A", tex.HighCard);
            Assert.IsTrue(tex.HasStraightDraw);
        }

        [Test]
        public void Analyze_FourCards_WorksCorrectly()
        {
            var cards = new List<string> { "AH", "KD", "9C", "3S" };
            var tex = BoardTextureAnalyzer.Analyze(cards);

            Assert.AreEqual("A", tex.HighCard);
            Assert.IsTrue(tex.IsRainbow);
        }

        [Test]
        public void Analyze_WheelDraw_DetectedAsStraightDraw()
        {
            // A-2-3 should detect wheel draw potential
            var cards = new List<string> { "AH", "2D", "3C" };
            var tex = BoardTextureAnalyzer.Analyze(cards);

            Assert.IsTrue(tex.HasStraightDraw);
        }

        [Test]
        public void Analyze_Description_ContainsRelevantInfo()
        {
            var cards = new List<string> { "AH", "AD", "9C" };
            var tex = BoardTextureAnalyzer.Analyze(cards);

            Assert.IsTrue(tex.Description.Contains("paired"));
        }

        [Test]
        public void RankToInt_AllRanks_CorrectValues()
        {
            Assert.AreEqual(14, BoardTextureAnalyzer.RankToInt("A"));
            Assert.AreEqual(13, BoardTextureAnalyzer.RankToInt("K"));
            Assert.AreEqual(12, BoardTextureAnalyzer.RankToInt("Q"));
            Assert.AreEqual(11, BoardTextureAnalyzer.RankToInt("J"));
            Assert.AreEqual(10, BoardTextureAnalyzer.RankToInt("10"));
            Assert.AreEqual(2, BoardTextureAnalyzer.RankToInt("2"));
        }

        [Test]
        public void WetnessRating_ClampedBetween0And10()
        {
            // Even extreme boards should be clamped
            var cards = new List<string> { "9H", "10H", "JH", "QH", "KH" };
            var tex = BoardTextureAnalyzer.Analyze(cards);

            Assert.GreaterOrEqual(tex.WetnessRating, 0f);
            Assert.LessOrEqual(tex.WetnessRating, 10f);
        }
    }
}
