using System.Collections.Generic;
using System.Linq;

namespace HijackPoker.Utils
{
    public struct BoardTexture
    {
        public bool IsPairedBoard;
        public int FlushDrawCount;
        public bool HasFlushDraw;
        public bool HasStraightDraw;
        public bool HasOvercards;
        public string Wetness; // "dry", "medium", "wet"
        public int HighCardRank;
        public string DominantSuit;
    }

    public static class BoardAnalyzer
    {
        public static BoardTexture Analyze(List<string> communityCards)
        {
            var tex = new BoardTexture { Wetness = "dry" };
            if (communityCards == null || communityCards.Count == 0) return tex;

            var suits = new Dictionary<string, int>();
            var ranks = new List<int>();

            foreach (var card in communityCards)
            {
                if (string.IsNullOrEmpty(card) || card.Length < 2) continue;
                string suit = card[^1..];
                string rank = card[..^1];

                suits.TryGetValue(suit, out int c);
                suits[suit] = c + 1;
                ranks.Add(RankToNumeric(rank));
            }

            if (ranks.Count == 0) return tex;

            // Suit analysis
            var topSuit = suits.OrderByDescending(kv => kv.Value).First();
            tex.FlushDrawCount = topSuit.Value;
            tex.DominantSuit = topSuit.Key;
            tex.HasFlushDraw = tex.FlushDrawCount >= 3;

            // Rank analysis
            ranks.Sort();
            var unique = ranks.Distinct().ToList();
            tex.IsPairedBoard = unique.Count < ranks.Count;
            tex.HighCardRank = ranks.Max();
            tex.HasOvercards = tex.HighCardRank >= 12;

            // Straight draw: 3+ unique ranks within a 5-rank window
            for (int i = 0; i < unique.Count; i++)
            {
                int end = unique[i] + 4;
                if (unique.Count(r => r >= unique[i] && r <= end) >= 3)
                { tex.HasStraightDraw = true; break; }
            }
            // Wheel check (A-low)
            if (unique.Contains(14) && unique.Count(r => r <= 5) >= 2)
                tex.HasStraightDraw = true;

            // Wetness
            int wet = 0;
            if (tex.HasFlushDraw) wet += 2;
            if (tex.HasStraightDraw) wet += 2;
            if (!tex.IsPairedBoard) wet++;
            tex.Wetness = wet >= 4 ? "wet" : wet >= 2 ? "medium" : "dry";

            return tex;
        }

        public static int RankToNumeric(string rank)
        {
            return rank switch
            {
                "A" => 14, "K" => 13, "Q" => 12, "J" => 11, "T" or "10" => 10,
                _ => int.TryParse(rank, out int n) ? n : 0,
            };
        }

        public static int HandRankTier(string handRank)
        {
            if (string.IsNullOrEmpty(handRank)) return 0;
            string h = handRank.ToLower();
            if (h.Contains("royal")) return 10;
            if (h.Contains("straight flush")) return 9;
            if (h.Contains("four") || h.Contains("quad")) return 8;
            if (h.Contains("full house") || h.Contains("full")) return 7;
            if (h.Contains("flush")) return 6;
            if (h.Contains("straight")) return 5;
            if (h.Contains("three") || h.Contains("trip") || h.Contains("set")) return 4;
            if (h.Contains("two pair")) return 3;
            if (h.Contains("pair")) return 2;
            return 1;
        }

        public static string SuitName(string suit)
        {
            return suit switch { "H" => "hearts", "D" => "diamonds", "C" => "clubs", "S" => "spades", _ => suit };
        }
    }
}
