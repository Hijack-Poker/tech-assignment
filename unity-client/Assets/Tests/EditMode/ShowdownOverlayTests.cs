using System.Collections.Generic;
using NUnit.Framework;
using HijackPoker.Models;

namespace HijackPoker.Tests
{
    [TestFixture]
    public class ShowdownOverlayTests
    {
        // ── Filtering: skip folded players ────────────────────────────

        [Test]
        public void FilterPlayers_ExcludesFolded()
        {
            var players = new List<PlayerState>
            {
                CreatePlayer(1, "Alice", "1", 0, "Pair"),
                CreatePlayer(2, "Bob", "11", 0, ""), // Folded
                CreatePlayer(3, "Charlie", "1", 50, "Two Pair"),
            };

            var filtered = FilterForShowdown(players);

            Assert.AreEqual(2, filtered.Count);
            Assert.IsFalse(filtered.Exists(p => p.Username == "Bob"));
        }

        [Test]
        public void FilterPlayers_ExcludesNoCards()
        {
            var players = new List<PlayerState>
            {
                CreatePlayer(1, "Alice", "1", 0, "Pair"),
                CreatePlayerNoCards(4, "Dave", "1"),
            };

            var filtered = FilterForShowdown(players);

            Assert.AreEqual(1, filtered.Count);
            Assert.AreEqual("Alice", filtered[0].Username);
        }

        // ── Ordering: winners first ───────────────────────────────────

        [Test]
        public void Ordering_WinnersFirst()
        {
            var players = new List<PlayerState>
            {
                CreatePlayer(1, "Alice", "1", 0, "Pair"),
                CreatePlayer(3, "Charlie", "1", 50, "Full House"),
                CreatePlayer(5, "Eve", "1", 25, "Flush"),
            };

            var sorted = FilterForShowdown(players);
            sorted.Sort((a, b) =>
            {
                if (a.IsWinner && !b.IsWinner) return -1;
                if (!a.IsWinner && b.IsWinner) return 1;
                if (a.Winnings != b.Winnings) return b.Winnings.CompareTo(a.Winnings);
                return a.Seat.CompareTo(b.Seat);
            });

            Assert.AreEqual("Charlie", sorted[0].Username); // 50 winnings
            Assert.AreEqual("Eve", sorted[1].Username);     // 25 winnings
            Assert.AreEqual("Alice", sorted[2].Username);   // 0 winnings
        }

        [Test]
        public void Ordering_TiedWinners_SortBySeat()
        {
            var players = new List<PlayerState>
            {
                CreatePlayer(5, "Eve", "1", 25, "Flush"),
                CreatePlayer(2, "Bob", "1", 25, "Flush"),
            };

            var sorted = FilterForShowdown(players);
            sorted.Sort((a, b) =>
            {
                if (a.IsWinner && !b.IsWinner) return -1;
                if (!a.IsWinner && b.IsWinner) return 1;
                if (a.Winnings != b.Winnings) return b.Winnings.CompareTo(a.Winnings);
                return a.Seat.CompareTo(b.Seat);
            });

            Assert.AreEqual("Bob", sorted[0].Username);  // Seat 2
            Assert.AreEqual("Eve", sorted[1].Username);   // Seat 5
        }

        // ── Hand rank display ─────────────────────────────────────────

        [Test]
        public void HandRank_DisplayedForNonEmpty()
        {
            var player = CreatePlayer(1, "Alice", "1", 50, "Full House");
            Assert.AreEqual("Full House", player.HandRank);
        }

        [Test]
        public void HandRank_EmptyForNoRank()
        {
            var player = CreatePlayer(1, "Alice", "1", 0, "");
            Assert.AreEqual("", player.HandRank);
        }

        // ── Helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Mirrors the filtering logic used in ShowdownOverlay.Show().
        /// </summary>
        private static List<PlayerState> FilterForShowdown(List<PlayerState> players)
        {
            var result = new List<PlayerState>(players);
            result.RemoveAll(p => p.IsFolded || !p.HasCards);
            return result;
        }

        private static PlayerState CreatePlayer(int seat, string name, string status,
            float winnings, string handRank)
        {
            return new PlayerState
            {
                Seat = seat,
                Username = name,
                Status = status,
                Winnings = winnings,
                HandRank = handRank,
                PlayerId = seat,
                Stack = 100,
                Cards = new List<string> { "AH", "KD" }
            };
        }

        private static PlayerState CreatePlayerNoCards(int seat, string name, string status)
        {
            return new PlayerState
            {
                Seat = seat,
                Username = name,
                Status = status,
                Winnings = 0,
                HandRank = "",
                PlayerId = seat,
                Stack = 100,
                Cards = null
            };
        }
    }
}
