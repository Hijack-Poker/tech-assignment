using System;

namespace HijackPoker.Utils
{
    public struct ParsedCard
    {
        public string Rank;
        public string Suit;
        public string Symbol;
        public bool IsRed;

        public string Display => $"{Rank}{Symbol}";
    }

    public static class CardUtils
    {
        public static ParsedCard Parse(string card)
        {
            if (string.IsNullOrEmpty(card) || card.Length < 2)
                throw new ArgumentException($"Invalid card string: \"{card ?? "null"}\"");

            string suit = card.Substring(card.Length - 1);
            string rank = card.Substring(0, card.Length - 1);

            if (!IsValidSuit(suit))
                throw new ArgumentException($"Invalid suit: \"{suit}\" in card \"{card}\"");

            if (!IsValidRank(rank))
                throw new ArgumentException($"Invalid rank: \"{rank}\" in card \"{card}\"");

            return new ParsedCard
            {
                Rank = rank,
                Suit = suit,
                Symbol = GetSuitSymbol(suit),
                IsRed = IsSuitRed(suit)
            };
        }

        public static string GetSuitSymbol(string suit)
        {
            switch (suit)
            {
                case "H": return "\u2665";
                case "D": return "\u2666";
                case "C": return "\u2663";
                case "S": return "\u2660";
                default: return "?";
            }
        }

        public static bool IsSuitRed(string suit)
        {
            return suit == "H" || suit == "D";
        }

        private static bool IsValidSuit(string suit)
        {
            return suit == "H" || suit == "D" || suit == "C" || suit == "S";
        }

        private static bool IsValidRank(string rank)
        {
            switch (rank)
            {
                case "A": case "2": case "3": case "4": case "5":
                case "6": case "7": case "8": case "9": case "10":
                case "J": case "Q": case "K":
                    return true;
                default:
                    return false;
            }
        }
    }
}
