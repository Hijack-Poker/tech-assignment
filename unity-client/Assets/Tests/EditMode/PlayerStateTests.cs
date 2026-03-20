using NUnit.Framework;
using Newtonsoft.Json;
using HijackPoker.Models;
using System.Collections.Generic;

namespace HijackPoker.Tests
{
    [TestFixture]
    public class PlayerStateTests
    {
        private PlayerState CreatePlayer(
            string status = "1",
            float winnings = 0,
            List<string> cards = null,
            string action = "")
        {
            return new PlayerState
            {
                PlayerId = 1,
                Username = "TestPlayer",
                Seat = 1,
                Stack = 100.0f,
                Bet = 0,
                TotalBet = 0,
                Status = status,
                Action = action,
                Cards = cards ?? new List<string> { "AH", "KD" },
                HandRank = "",
                Winnings = winnings
            };
        }

        // ── IsActive ──

        [Test]
        public void IsActive_StatusActive_ReturnsTrue()
        {
            var player = CreatePlayer(status: PlayerStatusCode.Active);
            Assert.IsTrue(player.IsActive);
        }

        [Test]
        public void IsActive_StatusFolded_ReturnsFalse()
        {
            var player = CreatePlayer(status: PlayerStatusCode.Folded);
            Assert.IsFalse(player.IsActive);
        }

        // ── IsFolded ──

        [Test]
        public void IsFolded_StatusFolded_ReturnsTrue()
        {
            var player = CreatePlayer(status: PlayerStatusCode.Folded);
            Assert.IsTrue(player.IsFolded);
        }

        [Test]
        public void IsFolded_StatusActive_ReturnsFalse()
        {
            var player = CreatePlayer(status: PlayerStatusCode.Active);
            Assert.IsFalse(player.IsFolded);
        }

        // ── IsAllIn ──

        [Test]
        public void IsAllIn_StatusAllIn_ReturnsTrue()
        {
            var player = CreatePlayer(status: PlayerStatusCode.AllIn);
            Assert.IsTrue(player.IsAllIn);
        }

        [Test]
        public void IsAllIn_StatusActive_ReturnsFalse()
        {
            var player = CreatePlayer(status: PlayerStatusCode.Active);
            Assert.IsFalse(player.IsAllIn);
        }

        // ── IsWinner ──

        [Test]
        public void IsWinner_PositiveWinnings_ReturnsTrue()
        {
            var player = CreatePlayer(winnings: 24.0f);
            Assert.IsTrue(player.IsWinner);
        }

        [Test]
        public void IsWinner_ZeroWinnings_ReturnsFalse()
        {
            var player = CreatePlayer(winnings: 0);
            Assert.IsFalse(player.IsWinner);
        }

        // ── HasCards ──

        [Test]
        public void HasCards_TwoCards_ReturnsTrue()
        {
            var player = CreatePlayer(cards: new List<string> { "AH", "KD" });
            Assert.IsTrue(player.HasCards);
        }

        [Test]
        public void HasCards_EmptyList_ReturnsFalse()
        {
            var player = CreatePlayer(cards: new List<string>());
            Assert.IsFalse(player.HasCards);
        }

        [Test]
        public void HasCards_NullList_ReturnsFalse()
        {
            var player = CreatePlayer(cards: null);
            player.Cards = null;
            Assert.IsFalse(player.HasCards);
        }

        [Test]
        public void HasCards_OneCard_ReturnsTrue()
        {
            var player = CreatePlayer(cards: new List<string> { "AH" });
            Assert.IsTrue(player.HasCards);
        }

        // ── Status codes ──

        [Test]
        public void StatusCode_Constants_MatchExpectedValues()
        {
            Assert.AreEqual("1", PlayerStatusCode.Active);
            Assert.AreEqual("2", PlayerStatusCode.SittingOut);
            Assert.AreEqual("3", PlayerStatusCode.Leaving);
            Assert.AreEqual("4", PlayerStatusCode.ShowCards);
            Assert.AreEqual("5", PlayerStatusCode.PostBlind);
            Assert.AreEqual("6", PlayerStatusCode.WaitForBB);
            Assert.AreEqual("11", PlayerStatusCode.Folded);
            Assert.AreEqual("12", PlayerStatusCode.AllIn);
        }

        // ── JSON deserialization ──

        [Test]
        public void Deserialize_AllFields()
        {
            string json = @"{
                ""playerId"": 42,
                ""username"": ""Charlie"",
                ""seat"": 3,
                ""stack"": 250.50,
                ""bet"": 10.0,
                ""totalBet"": 20.0,
                ""status"": ""12"",
                ""action"": ""allin"",
                ""cards"": [""AH"", ""AS""],
                ""handRank"": ""Pair of Aces"",
                ""winnings"": 50.0
            }";

            var player = JsonConvert.DeserializeObject<PlayerState>(json);

            Assert.AreEqual(42, player.PlayerId);
            Assert.AreEqual("Charlie", player.Username);
            Assert.AreEqual(3, player.Seat);
            Assert.AreEqual(250.50f, player.Stack);
            Assert.AreEqual(10.0f, player.Bet);
            Assert.AreEqual(20.0f, player.TotalBet);
            Assert.AreEqual("12", player.Status);
            Assert.AreEqual("allin", player.Action);
            Assert.AreEqual(2, player.Cards.Count);
            Assert.AreEqual("AH", player.Cards[0]);
            Assert.AreEqual("AS", player.Cards[1]);
            Assert.AreEqual("Pair of Aces", player.HandRank);
            Assert.AreEqual(50.0f, player.Winnings);
            Assert.IsTrue(player.IsAllIn);
            Assert.IsTrue(player.IsWinner);
        }

        [Test]
        public void Deserialize_PlayerWithEmptyAction()
        {
            string json = @"{
                ""playerId"": 1, ""username"": ""Test"", ""seat"": 1,
                ""stack"": 100, ""bet"": 0, ""totalBet"": 0,
                ""status"": ""1"", ""action"": """",
                ""cards"": [], ""handRank"": """", ""winnings"": 0
            }";

            var player = JsonConvert.DeserializeObject<PlayerState>(json);

            Assert.AreEqual("", player.Action);
            Assert.IsTrue(player.IsActive);
            Assert.IsFalse(player.IsWinner);
            Assert.IsFalse(player.HasCards);
        }

        [TestCase("1", true, false, false)]
        [TestCase("11", false, true, false)]
        [TestCase("12", false, false, true)]
        [TestCase("2", false, false, false)]
        [TestCase("4", false, false, false)]
        public void ConvenienceProperties_ByStatus(string status, bool isActive, bool isFolded, bool isAllIn)
        {
            var player = CreatePlayer(status: status);
            Assert.AreEqual(isActive, player.IsActive);
            Assert.AreEqual(isFolded, player.IsFolded);
            Assert.AreEqual(isAllIn, player.IsAllIn);
        }
    }
}
