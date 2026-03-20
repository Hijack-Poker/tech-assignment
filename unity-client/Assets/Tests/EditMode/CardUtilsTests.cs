using System;
using NUnit.Framework;
using HijackPoker.Utils;

namespace HijackPoker.Tests
{
    [TestFixture]
    public class CardUtilsTests
    {
        // ── Parse rank and suit ──

        [Test]
        public void Parse_AceOfHearts()
        {
            var card = CardUtils.Parse("AH");
            Assert.AreEqual("A", card.Rank);
            Assert.AreEqual("H", card.Suit);
        }

        [Test]
        public void Parse_TenOfDiamonds()
        {
            var card = CardUtils.Parse("10D");
            Assert.AreEqual("10", card.Rank);
            Assert.AreEqual("D", card.Suit);
        }

        [Test]
        public void Parse_TwoOfClubs()
        {
            var card = CardUtils.Parse("2C");
            Assert.AreEqual("2", card.Rank);
            Assert.AreEqual("C", card.Suit);
        }

        [Test]
        public void Parse_KingOfSpades()
        {
            var card = CardUtils.Parse("KS");
            Assert.AreEqual("K", card.Rank);
            Assert.AreEqual("S", card.Suit);
        }

        [Test]
        public void Parse_QueenOfHearts()
        {
            var card = CardUtils.Parse("QH");
            Assert.AreEqual("Q", card.Rank);
            Assert.AreEqual("H", card.Suit);
        }

        [Test]
        public void Parse_JackOfClubs()
        {
            var card = CardUtils.Parse("JC");
            Assert.AreEqual("J", card.Rank);
            Assert.AreEqual("C", card.Suit);
        }

        // ── Suit colors ──

        [Test]
        public void Parse_Hearts_IsRed()
        {
            var card = CardUtils.Parse("AH");
            Assert.IsTrue(card.IsRed);
        }

        [Test]
        public void Parse_Diamonds_IsRed()
        {
            var card = CardUtils.Parse("10D");
            Assert.IsTrue(card.IsRed);
        }

        [Test]
        public void Parse_Clubs_IsNotRed()
        {
            var card = CardUtils.Parse("2C");
            Assert.IsFalse(card.IsRed);
        }

        [Test]
        public void Parse_Spades_IsNotRed()
        {
            var card = CardUtils.Parse("KS");
            Assert.IsFalse(card.IsRed);
        }

        // ── Unicode symbols ──

        [Test]
        public void Parse_Hearts_Symbol()
        {
            var card = CardUtils.Parse("AH");
            Assert.AreEqual("\u2665", card.Symbol);
        }

        [Test]
        public void Parse_Diamonds_Symbol()
        {
            var card = CardUtils.Parse("10D");
            Assert.AreEqual("\u2666", card.Symbol);
        }

        [Test]
        public void Parse_Clubs_Symbol()
        {
            var card = CardUtils.Parse("2C");
            Assert.AreEqual("\u2663", card.Symbol);
        }

        [Test]
        public void Parse_Spades_Symbol()
        {
            var card = CardUtils.Parse("KS");
            Assert.AreEqual("\u2660", card.Symbol);
        }

        // ── Display string ──

        [Test]
        public void Display_AceOfHearts()
        {
            var card = CardUtils.Parse("AH");
            Assert.AreEqual("A\u2665", card.Display);
        }

        [Test]
        public void Display_TenOfDiamonds()
        {
            var card = CardUtils.Parse("10D");
            Assert.AreEqual("10\u2666", card.Display);
        }

        // ── GetSuitSymbol static method ──

        [TestCase("H", "\u2665")]
        [TestCase("D", "\u2666")]
        [TestCase("C", "\u2663")]
        [TestCase("S", "\u2660")]
        [TestCase("X", "?")]
        public void GetSuitSymbol_AllSuits(string suit, string expected)
        {
            Assert.AreEqual(expected, CardUtils.GetSuitSymbol(suit));
        }

        // ── IsSuitRed static method ──

        [TestCase("H", true)]
        [TestCase("D", true)]
        [TestCase("C", false)]
        [TestCase("S", false)]
        public void IsSuitRed_AllSuits(string suit, bool expected)
        {
            Assert.AreEqual(expected, CardUtils.IsSuitRed(suit));
        }

        // ── Edge cases ──

        [Test]
        public void Parse_NullString_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CardUtils.Parse(null));
        }

        [Test]
        public void Parse_EmptyString_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CardUtils.Parse(""));
        }

        [Test]
        public void Parse_SingleChar_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CardUtils.Parse("A"));
        }

        [Test]
        public void Parse_InvalidSuit_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CardUtils.Parse("AX"));
        }

        [Test]
        public void Parse_InvalidRank_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CardUtils.Parse("XH"));
        }

        // ── All ranks ──

        [TestCase("AH", "A")]
        [TestCase("2H", "2")]
        [TestCase("3H", "3")]
        [TestCase("4H", "4")]
        [TestCase("5H", "5")]
        [TestCase("6H", "6")]
        [TestCase("7H", "7")]
        [TestCase("8H", "8")]
        [TestCase("9H", "9")]
        [TestCase("10H", "10")]
        [TestCase("JH", "J")]
        [TestCase("QH", "Q")]
        [TestCase("KH", "K")]
        public void Parse_AllRanks(string card, string expectedRank)
        {
            var parsed = CardUtils.Parse(card);
            Assert.AreEqual(expectedRank, parsed.Rank);
        }
    }
}
