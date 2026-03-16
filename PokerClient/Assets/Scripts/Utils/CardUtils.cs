namespace HijackPoker.Utils
{
    public static class CardUtils
    {
        public static (string rank, string suit) ParseCard(string card)
        {
            if (string.IsNullOrEmpty(card) || card.Length < 2)
                return ("?", "?");
            string suit = card[^1..];
            string rank = card[..^1];
            return (rank, suit);
        }

        public static bool IsRedSuit(string suit) =>
            suit == "H" || suit == "D";

        public static string GetSuitSymbol(string suit) => suit switch
        {
            "H" => "\u2665",
            "D" => "\u2666",
            "C" => "\u2663",
            "S" => "\u2660",
            _   => suit
        };

        public static string GetDisplayString(string card)
        {
            var (rank, suit) = ParseCard(card);
            return rank + GetSuitSymbol(suit);
        }
    }
}
