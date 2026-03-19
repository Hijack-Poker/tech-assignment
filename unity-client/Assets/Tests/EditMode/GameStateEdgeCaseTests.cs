using System.Collections.Generic;
using NUnit.Framework;
using Newtonsoft.Json;
using HijackPoker.Models;

namespace HijackPoker.Tests
{
    [TestFixture]
    public class GameStateEdgeCaseTests
    {
        // ── Partial JSON (missing optional fields) ──

        [Test]
        public void Deserialize_MinimalJson_DefaultsCorrectly()
        {
            string json = @"{ ""handStep"": 5, ""stepName"": ""PRE_FLOP_BETTING"" }";
            var game = JsonConvert.DeserializeObject<GameState>(json);

            Assert.AreEqual(5, game.HandStep);
            Assert.AreEqual("PRE_FLOP_BETTING", game.StepName);
            Assert.AreEqual(0, game.Id);
            Assert.AreEqual(0, game.TableId);
            Assert.IsNull(game.TableName);
            Assert.IsNull(game.CommunityCards);
            Assert.IsNull(game.SidePots);
            Assert.IsNull(game.Winners);
            Assert.AreEqual(0, game.Pot);
        }

        [Test]
        public void Deserialize_MissingCommunityCards_IsNull()
        {
            string json = @"{ ""handStep"": 0 }";
            var game = JsonConvert.DeserializeObject<GameState>(json);
            Assert.IsNull(game.CommunityCards);
        }

        [Test]
        public void Deserialize_EmptyCommunityCards()
        {
            string json = @"{ ""handStep"": 0, ""communityCards"": [] }";
            var game = JsonConvert.DeserializeObject<GameState>(json);
            Assert.IsNotNull(game.CommunityCards);
            Assert.AreEqual(0, game.CommunityCards.Count);
        }

        [Test]
        public void Deserialize_NullSidePots()
        {
            string json = @"{ ""handStep"": 5, ""sidePots"": null }";
            var game = JsonConvert.DeserializeObject<GameState>(json);
            Assert.IsNull(game.SidePots);
        }

        [Test]
        public void Deserialize_NullWinners()
        {
            string json = @"{ ""handStep"": 5, ""winners"": null }";
            var game = JsonConvert.DeserializeObject<GameState>(json);
            Assert.IsNull(game.Winners);
        }

        // ── IsShowdown with edge values ──

        [Test]
        public void IsShowdown_NegativeStep_ReturnsFalse()
        {
            var game = new GameState { HandStep = -1 };
            Assert.IsFalse(game.IsShowdown);
        }

        [Test]
        public void IsShowdown_VeryLargeStep_ReturnsTrue()
        {
            var game = new GameState { HandStep = 100 };
            Assert.IsTrue(game.IsShowdown);
        }

        // ── IsHandComplete edge cases ──

        [Test]
        public void IsHandComplete_NullStepName_ReturnsFalse()
        {
            var game = new GameState { StepName = null };
            Assert.IsFalse(game.IsHandComplete);
        }

        [Test]
        public void IsHandComplete_EmptyStepName_ReturnsFalse()
        {
            var game = new GameState { StepName = "" };
            Assert.IsFalse(game.IsHandComplete);
        }

        [Test]
        public void IsHandComplete_CaseSensitive()
        {
            var game = new GameState { StepName = "record_stats_and_new_hand" };
            Assert.IsFalse(game.IsHandComplete, "StepName comparison should be case-sensitive");
        }

        // ── TableResponse deserialization ──

        [Test]
        public void Deserialize_TableResponse_Complete()
        {
            string json = @"{
                ""game"": {
                    ""handStep"": 7,
                    ""stepName"": ""FLOP_BETTING"",
                    ""communityCards"": [""JH"", ""7D"", ""2C""],
                    ""pot"": 24.0,
                    ""sidePots"": [],
                    ""winners"": []
                },
                ""players"": [
                    {
                        ""playerId"": 1,
                        ""username"": ""Alice"",
                        ""seat"": 1,
                        ""status"": ""1"",
                        ""cards"": [""AH"", ""KD""],
                        ""winnings"": 0
                    }
                ]
            }";

            var table = JsonConvert.DeserializeObject<TableResponse>(json);

            Assert.IsNotNull(table.Game);
            Assert.AreEqual(7, table.Game.HandStep);
            Assert.AreEqual(3, table.Game.CommunityCards.Count);
            Assert.AreEqual(1, table.Players.Count);
            Assert.AreEqual("Alice", table.Players[0].Username);
        }

        [Test]
        public void Deserialize_TableResponse_NullGame()
        {
            string json = @"{ ""game"": null, ""players"": [] }";
            var table = JsonConvert.DeserializeObject<TableResponse>(json);
            Assert.IsNull(table.Game);
            Assert.AreEqual(0, table.Players.Count);
        }

        [Test]
        public void Deserialize_TableResponse_NullPlayers()
        {
            string json = @"{ ""game"": { ""handStep"": 0 }, ""players"": null }";
            var table = JsonConvert.DeserializeObject<TableResponse>(json);
            Assert.IsNotNull(table.Game);
            Assert.IsNull(table.Players);
        }

        // ── ProcessResponse deserialization ──

        [Test]
        public void Deserialize_ProcessResponse_Success()
        {
            string json = @"{
                ""success"": true,
                ""result"": {
                    ""status"": ""ok"",
                    ""tableId"": 1,
                    ""step"": 5,
                    ""stepName"": ""PRE_FLOP_BETTING""
                }
            }";

            var response = JsonConvert.DeserializeObject<ProcessResponse>(json);
            Assert.IsTrue(response.Success);
            Assert.AreEqual("ok", response.Result.Status);
            Assert.AreEqual(5, response.Result.Step);
        }

        [Test]
        public void Deserialize_ProcessResponse_Error()
        {
            string json = @"{
                ""success"": false,
                ""error"": ""Table not found""
            }";

            var response = JsonConvert.DeserializeObject<ProcessResponse>(json);
            Assert.IsFalse(response.Success);
            Assert.AreEqual("Table not found", response.Error);
            Assert.IsNull(response.Result);
        }

        // ── HealthResponse deserialization ──

        [Test]
        public void Deserialize_HealthResponse()
        {
            string json = @"{
                ""service"": ""holdem-processor"",
                ""status"": ""ok"",
                ""timestamp"": ""2024-01-15T10:30:00.000Z""
            }";

            var response = JsonConvert.DeserializeObject<HealthResponse>(json);
            Assert.AreEqual("holdem-processor", response.Service);
            Assert.AreEqual("ok", response.Status);
            Assert.IsNotNull(response.Timestamp);
        }

        // ── SidePot edge cases ──

        [Test]
        public void SidePot_EmptyEligibleSeats()
        {
            string json = @"{ ""amount"": 50, ""eligibleSeats"": [] }";
            var pot = JsonConvert.DeserializeObject<SidePot>(json);
            Assert.AreEqual(50, pot.Amount);
            Assert.AreEqual(0, pot.EligibleSeats.Count);
        }

        [Test]
        public void SidePot_SingleEligibleSeat()
        {
            string json = @"{ ""amount"": 10, ""eligibleSeats"": [3] }";
            var pot = JsonConvert.DeserializeObject<SidePot>(json);
            Assert.AreEqual(1, pot.EligibleSeats.Count);
            Assert.AreEqual(3, pot.EligibleSeats[0]);
        }
    }
}
