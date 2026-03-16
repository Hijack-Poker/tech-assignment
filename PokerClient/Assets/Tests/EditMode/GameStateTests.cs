using NUnit.Framework;
using Newtonsoft.Json;
using HijackPoker.Models;

namespace HijackPoker.Tests
{
    public class GameStateTests
    {
        private const string SampleTableJson = @"{
            ""game"": {
                ""id"": 1, ""tableId"": 1, ""tableName"": ""Starter Table"",
                ""gameNo"": 3, ""handStep"": 6, ""stepName"": ""DEAL_FLOP"",
                ""dealerSeat"": 2, ""smallBlindSeat"": 3, ""bigBlindSeat"": 4,
                ""communityCards"": [""JH"", ""7D"", ""2C""],
                ""pot"": 3.00, ""sidePots"": [], ""move"": 0, ""status"": ""in_progress"",
                ""smallBlind"": 1.00, ""bigBlind"": 2.00, ""maxSeats"": 6,
                ""currentBet"": 0, ""winners"": []
            },
            ""players"": [
                {
                    ""playerId"": 1, ""username"": ""Alice"", ""seat"": 1,
                    ""stack"": 150.00, ""bet"": 0, ""totalBet"": 0,
                    ""status"": ""1"", ""action"": """", ""cards"": [""AH"", ""KD""],
                    ""handRank"": """", ""winnings"": 0
                }
            ]
        }";

        [Test]
        public void IsShowdown_True_WhenHandStepIs12()
        {
            var game = new GameState { HandStep = 12 };
            Assert.IsTrue(game.IsShowdown);
        }

        [Test]
        public void IsShowdown_True_WhenHandStepAbove12()
        {
            var game = new GameState { HandStep = 15 };
            Assert.IsTrue(game.IsShowdown);
        }

        [Test]
        public void IsShowdown_False_WhenHandStepIs11()
        {
            var game = new GameState { HandStep = 11 };
            Assert.IsFalse(game.IsShowdown);
        }

        [Test]
        public void IsHandComplete_True_WhenStepNameIsRecordStats()
        {
            var game = new GameState { StepName = "RECORD_STATS_AND_NEW_HAND" };
            Assert.IsTrue(game.IsHandComplete);
        }

        [Test]
        public void IsHandComplete_False_WhenStepNameIsDealFlop()
        {
            var game = new GameState { StepName = "DEAL_FLOP" };
            Assert.IsFalse(game.IsHandComplete);
        }

        [Test]
        public void CommunityCards_ParsesFromJsonArray()
        {
            var table = JsonConvert.DeserializeObject<TableResponse>(SampleTableJson);
            Assert.AreEqual(3, table.Game.CommunityCards.Count);
            Assert.AreEqual("JH", table.Game.CommunityCards[0]);
            Assert.AreEqual("7D", table.Game.CommunityCards[1]);
            Assert.AreEqual("2C", table.Game.CommunityCards[2]);
        }

        [Test]
        public void SidePots_ParsesEmptyArray()
        {
            var table = JsonConvert.DeserializeObject<TableResponse>(SampleTableJson);
            Assert.IsNotNull(table.Game.SidePots);
            Assert.AreEqual(0, table.Game.SidePots.Count);
        }

        [Test]
        public void PlayerState_IsFolded_WhenStatus11()
        {
            var player = new PlayerState { Status = "11" };
            Assert.IsTrue(player.IsFolded);
        }

        [Test]
        public void PlayerState_IsAllIn_WhenStatus12()
        {
            var player = new PlayerState { Status = "12" };
            Assert.IsTrue(player.IsAllIn);
        }

        [Test]
        public void PlayerState_IsWinner_WhenWinningsPositive()
        {
            var player = new PlayerState { Winnings = 50f };
            Assert.IsTrue(player.IsWinner);
        }

        [Test]
        public void PlayerState_IsNotWinner_WhenWinningsZero()
        {
            var player = new PlayerState { Winnings = 0f };
            Assert.IsFalse(player.IsWinner);
        }

        [Test]
        public void PlayerState_HasCards_WhenCardsHasTwoEntries()
        {
            var table = JsonConvert.DeserializeObject<TableResponse>(SampleTableJson);
            Assert.IsTrue(table.Players[0].HasCards);
            Assert.AreEqual(2, table.Players[0].Cards.Count);
        }

        [Test]
        public void PlayerState_HasCards_False_WhenCardsNull()
        {
            var player = new PlayerState { Cards = null };
            Assert.IsFalse(player.HasCards);
        }

        [Test]
        public void FullTableResponse_DeserializesAllFields()
        {
            var table = JsonConvert.DeserializeObject<TableResponse>(SampleTableJson);

            Assert.IsNotNull(table);
            Assert.IsNotNull(table.Game);
            Assert.IsNotNull(table.Players);

            Assert.AreEqual(1, table.Game.Id);
            Assert.AreEqual(1, table.Game.TableId);
            Assert.AreEqual("Starter Table", table.Game.TableName);
            Assert.AreEqual(3, table.Game.GameNo);
            Assert.AreEqual(6, table.Game.HandStep);
            Assert.AreEqual("DEAL_FLOP", table.Game.StepName);
            Assert.AreEqual(2, table.Game.DealerSeat);
            Assert.AreEqual(3, table.Game.SmallBlindSeat);
            Assert.AreEqual(4, table.Game.BigBlindSeat);
            Assert.AreEqual(3.00f, table.Game.Pot);
            Assert.AreEqual(0, table.Game.Move);
            Assert.AreEqual("in_progress", table.Game.Status);
            Assert.AreEqual(1.00f, table.Game.SmallBlind);
            Assert.AreEqual(2.00f, table.Game.BigBlind);
            Assert.AreEqual(6, table.Game.MaxSeats);
            Assert.AreEqual(0f, table.Game.CurrentBet);

            Assert.AreEqual(1, table.Players.Count);
            var alice = table.Players[0];
            Assert.AreEqual(1, alice.PlayerId);
            Assert.AreEqual("Alice", alice.Username);
            Assert.AreEqual(1, alice.Seat);
            Assert.AreEqual(150.00f, alice.Stack);
            Assert.AreEqual("1", alice.Status);
            Assert.IsTrue(alice.IsActive);
        }

        [Test]
        public void SidePots_ParsesWithData()
        {
            var json = @"{
                ""game"": {
                    ""id"": 1, ""tableId"": 1, ""tableName"": ""T"",
                    ""gameNo"": 1, ""handStep"": 12, ""stepName"": ""SHOWDOWN"",
                    ""dealerSeat"": 1, ""smallBlindSeat"": 2, ""bigBlindSeat"": 3,
                    ""communityCards"": [], ""pot"": 100,
                    ""sidePots"": [{""amount"": 50, ""eligibleSeats"": [1,2]}],
                    ""move"": 0, ""status"": ""in_progress"",
                    ""smallBlind"": 1, ""bigBlind"": 2, ""maxSeats"": 6,
                    ""currentBet"": 0, ""winners"": []
                },
                ""players"": []
            }";
            var table = JsonConvert.DeserializeObject<TableResponse>(json);
            Assert.AreEqual(1, table.Game.SidePots.Count);
            Assert.AreEqual(50f, table.Game.SidePots[0].Amount);
            Assert.AreEqual(2, table.Game.SidePots[0].EligibleSeats.Count);
        }

        [Test]
        public void Winners_ParsesEmptyArray()
        {
            var table = JsonConvert.DeserializeObject<TableResponse>(SampleTableJson);
            Assert.IsNotNull(table.Game.Winners);
            Assert.AreEqual(0, table.Game.Winners.Count);
        }
    }
}
