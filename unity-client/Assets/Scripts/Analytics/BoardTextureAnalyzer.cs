using System.Collections.Generic;
using HijackPoker.Utils;

namespace HijackPoker.Analytics
{
    public struct BoardTexture
    {
        public bool IsPaired;
        public bool IsTrips;
        public bool IsMonotone;
        public bool IsTwoTone;
        public bool IsRainbow;
        public bool HasFlushDraw;
        public bool HasStraightDraw;
        public float WetnessRating;
        public string HighCard;
        public string Description;
    }

    public static class BoardTextureAnalyzer
    {
        public static BoardTexture Analyze(List<string> communityCards)
        {
            var tex = new BoardTexture();
            if (communityCards == null || communityCards.Count == 0)
            {
                tex.Description = "";
                return tex;
            }

            var ranks = new List<int>();
            var suits = new Dictionary<string, int>();
            var rankCounts = new Dictionary<int, int>();

            foreach (var card in communityCards)
            {
                if (string.IsNullOrEmpty(card)) continue;
                try
                {
                    var parsed = CardUtils.Parse(card);
                    int rank = RankToInt(parsed.Rank);
                    ranks.Add(rank);

                    if (!suits.ContainsKey(parsed.Suit))
                        suits[parsed.Suit] = 0;
                    suits[parsed.Suit]++;

                    if (!rankCounts.ContainsKey(rank))
                        rankCounts[rank] = 0;
                    rankCounts[rank]++;
                }
                catch { /* skip invalid cards */ }
            }

            if (ranks.Count == 0)
            {
                tex.Description = "";
                return tex;
            }

            ranks.Sort((a, b) => b.CompareTo(a));
            tex.HighCard = IntToRank(ranks[0]);

            // Pairing
            foreach (var kvp in rankCounts)
            {
                if (kvp.Value >= 3) { tex.IsTrips = true; tex.IsPaired = true; }
                else if (kvp.Value >= 2) tex.IsPaired = true;
            }

            // Suits
            int maxSuit = 0;
            foreach (var kvp in suits)
            {
                if (kvp.Value > maxSuit)
                    maxSuit = kvp.Value;
            }

            tex.IsMonotone = maxSuit >= 3 && suits.Count == 1;
            tex.IsTwoTone = maxSuit >= 2 && !tex.IsMonotone;
            tex.IsRainbow = maxSuit <= 1;
            tex.HasFlushDraw = maxSuit >= 3;

            // Straight draw detection
            tex.HasStraightDraw = DetectStraightDraw(ranks);

            // Wetness rating (0-10)
            tex.WetnessRating = CalculateWetness(tex, ranks);

            // Description
            tex.Description = BuildDescription(tex);

            return tex;
        }

        private static bool DetectStraightDraw(List<int> sortedRanks)
        {
            if (sortedRanks.Count < 3) return false;

            var unique = new HashSet<int>(sortedRanks);
            // Add low-ace for wheel draws
            if (unique.Contains(14))
                unique.Add(1);

            var sorted = new List<int>(unique);
            sorted.Sort();

            // Check for 3+ cards within a 5-card window
            for (int i = 0; i < sorted.Count; i++)
            {
                int connected = 1;
                for (int j = i + 1; j < sorted.Count; j++)
                {
                    if (sorted[j] - sorted[i] <= 4)
                        connected++;
                }
                if (connected >= 3) return true;
            }
            return false;
        }

        private static float CalculateWetness(BoardTexture tex, List<int> ranks)
        {
            float wetness = 5f; // baseline

            if (tex.HasFlushDraw) wetness += 2f;
            if (tex.HasStraightDraw) wetness += 2f;
            if (tex.IsTwoTone) wetness += 1f;
            if (tex.IsPaired) wetness -= 1f;
            if (tex.IsRainbow) wetness -= 1.5f;
            if (tex.IsMonotone) wetness += 1f;

            // Connected cards increase wetness
            if (ranks.Count >= 2)
            {
                int gaps = 0;
                for (int i = 0; i < ranks.Count - 1; i++)
                    gaps += ranks[i] - ranks[i + 1] - 1;
                if (gaps <= 2) wetness += 1f;
                if (gaps >= 6) wetness -= 1f;
            }

            if (wetness < 0f) wetness = 0f;
            if (wetness > 10f) wetness = 10f;

            return wetness;
        }

        private static string BuildDescription(BoardTexture tex)
        {
            var parts = new List<string>();

            if (tex.WetnessRating >= 7f) parts.Add("Wet");
            else if (tex.WetnessRating <= 3f) parts.Add("Dry");

            if (tex.IsTrips) parts.Add("trips on board");
            else if (tex.IsPaired) parts.Add("paired");

            if (tex.IsMonotone) parts.Add("monotone");
            else if (tex.IsTwoTone) parts.Add("two-tone");
            else if (tex.IsRainbow) parts.Add("rainbow");

            if (tex.HasFlushDraw && !tex.IsMonotone) parts.Add("flush draw possible");
            if (tex.HasStraightDraw) parts.Add("straight draw possible");

            if (parts.Count == 0) return "Neutral board";
            return string.Join(", ", parts) + " board";
        }

        internal static int RankToInt(string rank)
        {
            switch (rank)
            {
                case "A": return 14;
                case "K": return 13;
                case "Q": return 12;
                case "J": return 11;
                case "10": return 10;
                case "9": return 9;
                case "8": return 8;
                case "7": return 7;
                case "6": return 6;
                case "5": return 5;
                case "4": return 4;
                case "3": return 3;
                case "2": return 2;
                default: return 0;
            }
        }

        private static string IntToRank(int rank)
        {
            switch (rank)
            {
                case 14: return "A";
                case 13: return "K";
                case 12: return "Q";
                case 11: return "J";
                default: return rank.ToString();
            }
        }
    }
}
