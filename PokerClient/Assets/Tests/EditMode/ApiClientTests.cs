using NUnit.Framework;
using Newtonsoft.Json;
using HijackPoker.Models;

namespace HijackPoker.Tests
{
    public class ApiClientTests
    {
        [Test]
        public void HealthResponse_Deserializes()
        {
            var json = @"{""service"": ""holdem-processor"", ""status"": ""ok"", ""timestamp"": ""2024-01-01T00:00:00Z""}";
            var health = JsonConvert.DeserializeObject<HealthResponse>(json);
            Assert.AreEqual("holdem-processor", health.Service);
            Assert.AreEqual("ok", health.Status);
            Assert.IsNotNull(health.Timestamp);
        }

        [Test]
        public void ProcessResponse_Deserializes()
        {
            var json = @"{
                ""success"": true,
                ""result"": {
                    ""status"": ""ok"",
                    ""tableId"": 1,
                    ""step"": 6,
                    ""stepName"": ""DEAL_FLOP""
                },
                ""error"": null
            }";
            var resp = JsonConvert.DeserializeObject<ProcessResponse>(json);
            Assert.IsTrue(resp.Success);
            Assert.AreEqual(6, resp.Result.Step);
            Assert.AreEqual("DEAL_FLOP", resp.Result.StepName);
            Assert.AreEqual(1, resp.Result.TableId);
            Assert.IsNull(resp.Error);
        }

        [Test]
        public void TableResponse_Deserializes()
        {
            var json = @"{
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
            var table = JsonConvert.DeserializeObject<TableResponse>(json);
            Assert.AreEqual(6, table.Game.HandStep);
            Assert.AreEqual("Alice", table.Players[0].Username);
        }

        [Test]
        public void EmptyCommunityCards_DeserializesToEmptyList()
        {
            var json = @"{
                ""game"": {
                    ""id"": 1, ""tableId"": 1, ""tableName"": ""T"",
                    ""gameNo"": 1, ""handStep"": 0, ""stepName"": ""PREP"",
                    ""dealerSeat"": 1, ""smallBlindSeat"": 2, ""bigBlindSeat"": 3,
                    ""communityCards"": [], ""pot"": 0,
                    ""sidePots"": [], ""move"": 0, ""status"": ""in_progress"",
                    ""smallBlind"": 1, ""bigBlind"": 2, ""maxSeats"": 6,
                    ""currentBet"": 0, ""winners"": []
                },
                ""players"": []
            }";
            var table = JsonConvert.DeserializeObject<TableResponse>(json);
            Assert.IsNotNull(table.Game.CommunityCards);
            Assert.AreEqual(0, table.Game.CommunityCards.Count);
        }

        [Test]
        public void EmptySidePots_DeserializesToEmptyList()
        {
            var json = @"{
                ""game"": {
                    ""id"": 1, ""tableId"": 1, ""tableName"": ""T"",
                    ""gameNo"": 1, ""handStep"": 0, ""stepName"": ""PREP"",
                    ""dealerSeat"": 1, ""smallBlindSeat"": 2, ""bigBlindSeat"": 3,
                    ""communityCards"": [], ""pot"": 0,
                    ""sidePots"": [], ""move"": 0, ""status"": ""in_progress"",
                    ""smallBlind"": 1, ""bigBlind"": 2, ""maxSeats"": 6,
                    ""currentBet"": 0, ""winners"": []
                },
                ""players"": []
            }";
            var table = JsonConvert.DeserializeObject<TableResponse>(json);
            Assert.IsNotNull(table.Game.SidePots);
            Assert.AreEqual(0, table.Game.SidePots.Count);
        }

        [Test]
        public void ProcessResponse_WithError_Deserializes()
        {
            var json = @"{
                ""success"": false,
                ""result"": null,
                ""error"": ""Table not found""
            }";
            var resp = JsonConvert.DeserializeObject<ProcessResponse>(json);
            Assert.IsFalse(resp.Success);
            Assert.IsNull(resp.Result);
            Assert.AreEqual("Table not found", resp.Error);
        }
    }
}
