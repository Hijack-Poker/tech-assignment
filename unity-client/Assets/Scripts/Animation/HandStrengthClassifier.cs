namespace HijackPoker.Animation
{
    /// <summary>
    /// Classifies hand rank strings into tiers 0-4 for effect scaling.
    /// </summary>
    public static class HandStrengthClassifier
    {
        /// <summary>
        /// Returns tier 0-4 based on hand rank string.
        /// 0: High Card, 1: Pair, 2: Two Pair/Three of a Kind,
        /// 3: Straight/Flush/Full House, 4: Four of a Kind/Straight Flush/Royal Flush
        /// </summary>
        public static int GetTier(string handRank)
        {
            if (string.IsNullOrEmpty(handRank)) return 0;
            string lower = handRank.ToLower();

            if (lower.Contains("royal") || lower.Contains("straight flush"))
                return 4;
            if (lower.Contains("four"))
                return 4;
            if (lower.Contains("full house") || lower.Contains("flush") || lower.Contains("straight"))
                return 3;
            if (lower.Contains("three") || lower.Contains("two pair"))
                return 2;
            if (lower.Contains("pair"))
                return 1;

            return 0;
        }
    }
}
