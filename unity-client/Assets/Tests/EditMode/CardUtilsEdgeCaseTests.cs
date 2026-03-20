using System;
using NUnit.Framework;
using HijackPoker.Utils;

namespace HijackPoker.Tests
{
    [TestFixture]
    public class CardUtilsEdgeCaseTests
    {
        // ── Malformed input ──

        [Test]
        public void Parse_WhitespaceOnly_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CardUtils.Parse("  "));
        }

        [Test]
        public void Parse_LowercaseSuit_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CardUtils.Parse("Ah"));
        }

        [Test]
        public void Parse_LowercaseRank_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CardUtils.Parse("aH"));
        }

        [Test]
        public void Parse_NumbersOnly_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CardUtils.Parse("11"));
        }

        [Test]
        public void Parse_ReversedCardString_ThrowsArgumentException()
        {
            // "HA" instead of "AH"
            Assert.Throws<ArgumentException>(() => CardUtils.Parse("HA"));
        }

        [Test]
        public void Parse_ExtraCharacters_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CardUtils.Parse("AHX"));
        }

        [Test]
        public void Parse_SpecialCharacters_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CardUtils.Parse("@H"));
        }

        [Test]
        public void Parse_UnicodeInput_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CardUtils.Parse("\u2665H"));
        }

        [Test]
        public void Parse_ZeroRank_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CardUtils.Parse("0H"));
        }

        [Test]
        public void Parse_ElevenRank_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CardUtils.Parse("11H"));
        }

        [Test]
        public void Parse_OneRank_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CardUtils.Parse("1H"));
        }

        // ── GetSuitSymbol edge cases ──

        [Test]
        public void GetSuitSymbol_EmptyString_ReturnsQuestionMark()
        {
            Assert.AreEqual("?", CardUtils.GetSuitSymbol(""));
        }

        [Test]
        public void GetSuitSymbol_Null_ReturnsQuestionMark()
        {
            Assert.AreEqual("?", CardUtils.GetSuitSymbol(null));
        }

        [Test]
        public void GetSuitSymbol_Lowercase_ReturnsQuestionMark()
        {
            Assert.AreEqual("?", CardUtils.GetSuitSymbol("h"));
        }

        // ── IsSuitRed edge cases ──

        [Test]
        public void IsSuitRed_Null_ReturnsFalse()
        {
            Assert.IsFalse(CardUtils.IsSuitRed(null));
        }

        [Test]
        public void IsSuitRed_EmptyString_ReturnsFalse()
        {
            Assert.IsFalse(CardUtils.IsSuitRed(""));
        }

        [Test]
        public void IsSuitRed_Lowercase_ReturnsFalse()
        {
            Assert.IsFalse(CardUtils.IsSuitRed("h"));
        }

        // ── Display formatting ──

        [Test]
        public void Display_AllSuits_CorrectSymbols()
        {
            Assert.AreEqual("A\u2665", CardUtils.Parse("AH").Display);  // Hearts
            Assert.AreEqual("A\u2666", CardUtils.Parse("AD").Display);  // Diamonds
            Assert.AreEqual("A\u2663", CardUtils.Parse("AC").Display);  // Clubs
            Assert.AreEqual("A\u2660", CardUtils.Parse("AS").Display);  // Spades
        }

        [Test]
        public void Display_TenHasCorrectWidth()
        {
            // "10" is the only two-char rank, verify display handles it
            var card = CardUtils.Parse("10S");
            Assert.AreEqual("10\u2660", card.Display);
            Assert.AreEqual("10", card.Rank);
            Assert.AreEqual("S", card.Suit);
        }

        // ── Full deck validation ──

        [Test]
        public void Parse_FullDeck_AllValid()
        {
            string[] ranks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
            string[] suits = { "H", "D", "C", "S" };

            int count = 0;
            foreach (var rank in ranks)
            {
                foreach (var suit in suits)
                {
                    var card = CardUtils.Parse(rank + suit);
                    Assert.AreEqual(rank, card.Rank);
                    Assert.AreEqual(suit, card.Suit);
                    Assert.IsNotNull(card.Symbol);
                    Assert.IsNotEmpty(card.Display);
                    count++;
                }
            }

            Assert.AreEqual(52, count, "Should validate all 52 cards in a standard deck");
        }
    }
}
