using System.Collections.Generic;
using UnityEngine;

namespace HijackPoker.Utils
{
    public static class CardSpriteLoader
    {
        public static readonly Dictionary<string, string> SuitNames = new()
        {
            { "H", "Hearts" }, { "D", "Diamonds" }, { "C", "Clubs" }, { "S", "Spades" }
        };

        public static Sprite LoadCardSprite(string cardCode)
        {
            var (rank, suit) = CardUtils.ParseCard(cardCode);
            if (string.IsNullOrEmpty(rank) || string.IsNullOrEmpty(suit))
                return null;

            string suitName = SuitNames.TryGetValue(suit, out var mapped) ? mapped : suit;
            return Resources.Load<Sprite>($"Cards/card{suitName}{rank}");
        }
    }
}
