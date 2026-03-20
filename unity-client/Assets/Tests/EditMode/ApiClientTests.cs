using NUnit.Framework;
using Newtonsoft.Json;
using HijackPoker.Models;

namespace HijackPoker.Tests
{
    [TestFixture]
    public class ApiClientTests
    {
        private const string FullTableResponseJson = @"{
            ""game"": {
                ""id"": 1,
                ""tableId"": 1,
                ""tableName"": ""Starter Table"",
                ""gameNo"": 3,
                ""handStep"": 6,
                ""stepName"": ""DEAL_FLOP"",
                ""dealerSeat"": 2,
                ""smallBlindSeat"": 3,
                ""bigBlindSeat"": 4,
                ""communityCards"": [""JH"", ""7D"", ""2C""],
                ""pot"": 3.0,
                ""sidePots"": [],
                ""move"": 0,
                ""status"": ""in_progress"",
                ""smallBlind"": 1.0,
                ""bigBlind"": 2.0,
                ""maxSeats"": 6,
                ""currentBet"": 0,
                ""winners"": []
            },
            ""players"": [
                {
                    ""playerId"": 1,
                    ""username"": ""Alice"",
                    ""seat"": 1,
                    ""stack"": 150.0,
                    ""bet"": 0,
                    ""totalBet"": 2.0,
                    ""status"": ""1"",
                    ""action"": ""call"",
                    ""cards"": [""AH"", ""KD""],
                    ""handRank"": """",
                    ""winnings"": 0
                },
                {
                    ""playerId"": 2,
                    ""username"": ""Bob"",
                    ""seat"": 2,
                    ""stack"": 149.0,
                    ""bet"": 0,
                    ""totalBet"": 1.0,
                    ""status"": ""1"",
                    ""action"": ""call"",
                    ""cards"": [""QS"", ""JC""],
                    ""handRank"": """",
                    ""winnings"": 0
                }
            ]
        }";

        [Test]
        public void DeserializeTableResponse_ValidJson_GameFieldsParsed()
        {
            var response = JsonConvert.DeserializeObject<TableResponse>(FullTableResponseJson);

            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Game);
            Assert.AreEqual(1, response.Game.Id);
            Assert.AreEqual(1, response.Game.TableId);
            Assert.AreEqual("Starter Table", response.Game.TableName);
            Assert.AreEqual(3, response.Game.GameNo);
            Assert.AreEqual(6, response.Game.HandStep);
            Assert.AreEqual("DEAL_FLOP", response.Game.StepName);
            Assert.AreEqual(2, response.Game.DealerSeat);
            Assert.AreEqual(3, response.Game.SmallBlindSeat);
            Assert.AreEqual(4, response.Game.BigBlindSeat);
            Assert.AreEqual(3, response.Game.CommunityCards.Count);
            Assert.AreEqual("JH", response.Game.CommunityCards[0]);
            Assert.AreEqual("7D", response.Game.CommunityCards[1]);
            Assert.AreEqual("2C", response.Game.CommunityCards[2]);
            Assert.AreEqual(3.0f, response.Game.Pot);
            Assert.IsEmpty(response.Game.SidePots);
            Assert.AreEqual(0, response.Game.Move);
            Assert.AreEqual("in_progress", response.Game.Status);
            Assert.AreEqual(1.0f, response.Game.SmallBlind);
            Assert.AreEqual(2.0f, response.Game.BigBlind);
            Assert.AreEqual(6, response.Game.MaxSeats);
            Assert.AreEqual(0, response.Game.CurrentBet);
            Assert.IsEmpty(response.Game.Winners);
        }

        [Test]
        public void DeserializeTableResponse_ValidJson_PlayerFieldsParsed()
        {
            var response = JsonConvert.DeserializeObject<TableResponse>(FullTableResponseJson);

            Assert.AreEqual(2, response.Players.Count);

            var alice = response.Players[0];
            Assert.AreEqual(1, alice.PlayerId);
            Assert.AreEqual("Alice", alice.Username);
            Assert.AreEqual(1, alice.Seat);
            Assert.AreEqual(150.0f, alice.Stack);
            Assert.AreEqual(0, alice.Bet);
            Assert.AreEqual(2.0f, alice.TotalBet);
            Assert.AreEqual("1", alice.Status);
            Assert.AreEqual("call", alice.Action);
            Assert.AreEqual(2, alice.Cards.Count);
            Assert.AreEqual("AH", alice.Cards[0]);
            Assert.AreEqual("KD", alice.Cards[1]);
            Assert.AreEqual("", alice.HandRank);
            Assert.AreEqual(0, alice.Winnings);
        }

        [Test]
        public void DeserializeTableResponse_EmptyPlayersArray()
        {
            string json = @"{
                ""game"": {
                    ""id"": 1, ""tableId"": 1, ""tableName"": ""T"", ""gameNo"": 1,
                    ""handStep"": 0, ""stepName"": ""GAME_PREP"",
                    ""dealerSeat"": 0, ""smallBlindSeat"": 0, ""bigBlindSeat"": 0,
                    ""communityCards"": [], ""pot"": 0, ""sidePots"": [],
                    ""move"": 0, ""status"": ""waiting"",
                    ""smallBlind"": 1, ""bigBlind"": 2,
                    ""maxSeats"": 6, ""currentBet"": 0, ""winners"": []
                },
                ""players"": []
            }";

            var response = JsonConvert.DeserializeObject<TableResponse>(json);

            Assert.IsNotNull(response.Players);
            Assert.IsEmpty(response.Players);
        }

        [Test]
        public void DeserializeTableResponse_EmptyCommunityCards()
        {
            string json = @"{
                ""game"": {
                    ""id"": 1, ""tableId"": 1, ""tableName"": ""T"", ""gameNo"": 1,
                    ""handStep"": 4, ""stepName"": ""DEAL_CARDS"",
                    ""dealerSeat"": 1, ""smallBlindSeat"": 2, ""bigBlindSeat"": 3,
                    ""communityCards"": [], ""pot"": 3, ""sidePots"": [],
                    ""move"": 4, ""status"": ""in_progress"",
                    ""smallBlind"": 1, ""bigBlind"": 2,
                    ""maxSeats"": 6, ""currentBet"": 2, ""winners"": []
                },
                ""players"": []
            }";

            var response = JsonConvert.DeserializeObject<TableResponse>(json);

            Assert.IsNotNull(response.Game.CommunityCards);
            Assert.IsEmpty(response.Game.CommunityCards);
        }

        [Test]
        public void DeserializeTableResponse_SidePots()
        {
            string json = @"{
                ""game"": {
                    ""id"": 1, ""tableId"": 1, ""tableName"": ""T"", ""gameNo"": 1,
                    ""handStep"": 12, ""stepName"": ""AFTER_RIVER_BETTING_ROUND"",
                    ""dealerSeat"": 1, ""smallBlindSeat"": 2, ""bigBlindSeat"": 3,
                    ""communityCards"": [""AH"", ""KD"", ""QC"", ""JS"", ""10H""],
                    ""pot"": 50, ""sidePots"": [
                        { ""amount"": 30, ""eligibleSeats"": [1, 2, 3] },
                        { ""amount"": 20, ""eligibleSeats"": [1, 2] }
                    ],
                    ""move"": 0, ""status"": ""in_progress"",
                    ""smallBlind"": 1, ""bigBlind"": 2,
                    ""maxSeats"": 6, ""currentBet"": 0, ""winners"": []
                },
                ""players"": []
            }";

            var response = JsonConvert.DeserializeObject<TableResponse>(json);

            Assert.AreEqual(2, response.Game.SidePots.Count);
            Assert.AreEqual(30, response.Game.SidePots[0].Amount);
            Assert.AreEqual(3, response.Game.SidePots[0].EligibleSeats.Count);
            Assert.AreEqual(20, response.Game.SidePots[1].Amount);
            Assert.AreEqual(2, response.Game.SidePots[1].EligibleSeats.Count);
        }

        [Test]
        public void DeserializeTableResponse_Winners()
        {
            string json = @"{
                ""game"": {
                    ""id"": 1, ""tableId"": 1, ""tableName"": ""T"", ""gameNo"": 1,
                    ""handStep"": 13, ""stepName"": ""FIND_WINNERS"",
                    ""dealerSeat"": 1, ""smallBlindSeat"": 2, ""bigBlindSeat"": 3,
                    ""communityCards"": [""AH"", ""KD"", ""QC"", ""JS"", ""10H""],
                    ""pot"": 50, ""sidePots"": [],
                    ""move"": 0, ""status"": ""in_progress"",
                    ""smallBlind"": 1, ""bigBlind"": 2,
                    ""maxSeats"": 6, ""currentBet"": 0,
                    ""winners"": [
                        { ""seat"": 1, ""playerId"": 101 },
                        { ""seat"": 3, ""playerId"": 103 }
                    ]
                },
                ""players"": []
            }";

            var response = JsonConvert.DeserializeObject<TableResponse>(json);

            Assert.AreEqual(2, response.Game.Winners.Count);
            Assert.AreEqual(1, response.Game.Winners[0].Seat);
            Assert.AreEqual(101, response.Game.Winners[0].PlayerId);
            Assert.AreEqual(3, response.Game.Winners[1].Seat);
            Assert.AreEqual(103, response.Game.Winners[1].PlayerId);
        }

        [Test]
        public void DeserializeProcessResponse_ValidJson()
        {
            string json = @"{
                ""success"": true,
                ""result"": {
                    ""status"": ""processed"",
                    ""tableId"": 1,
                    ""step"": 6,
                    ""stepName"": ""DEAL_FLOP""
                }
            }";

            var response = JsonConvert.DeserializeObject<ProcessResponse>(json);

            Assert.IsTrue(response.Success);
            Assert.IsNotNull(response.Result);
            Assert.AreEqual("processed", response.Result.Status);
            Assert.AreEqual(1, response.Result.TableId);
            Assert.AreEqual(6, response.Result.Step);
            Assert.AreEqual("DEAL_FLOP", response.Result.StepName);
        }

        [Test]
        public void DeserializeProcessResponse_ErrorResponse()
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

        [Test]
        public void DeserializeHealthResponse_ValidJson()
        {
            string json = @"{
                ""service"": ""holdem-processor"",
                ""status"": ""ok"",
                ""timestamp"": ""2026-02-21T12:00:00.000Z""
            }";

            var response = JsonConvert.DeserializeObject<HealthResponse>(json);

            Assert.AreEqual("holdem-processor", response.Service);
            Assert.AreEqual("ok", response.Status);
            Assert.AreEqual("2026-02-21T12:00:00.000Z", response.Timestamp);
        }

        [Test]
        public void DeserializeTableResponse_NullOptionalFields()
        {
            string json = @"{
                ""game"": {
                    ""id"": 1, ""tableId"": 1, ""tableName"": ""T"", ""gameNo"": 1,
                    ""handStep"": 0, ""stepName"": ""GAME_PREP"",
                    ""dealerSeat"": 0, ""smallBlindSeat"": 0, ""bigBlindSeat"": 0,
                    ""communityCards"": [], ""pot"": 0,
                    ""move"": 0, ""status"": ""waiting"",
                    ""smallBlind"": 1, ""bigBlind"": 2,
                    ""maxSeats"": 6, ""currentBet"": 0
                },
                ""players"": [{
                    ""playerId"": 1, ""username"": ""Test"", ""seat"": 1,
                    ""stack"": 100, ""bet"": 0, ""totalBet"": 0,
                    ""status"": ""1"", ""action"": """",
                    ""cards"": [], ""handRank"": """", ""winnings"": 0
                }]
            }";

            var response = JsonConvert.DeserializeObject<TableResponse>(json);

            Assert.IsNull(response.Game.SidePots);
            Assert.IsNull(response.Game.Winners);
            Assert.IsNotNull(response.Game.CommunityCards);
        }

        [Test]
        public void DeserializeTableResponse_MalformedJson_ThrowsException()
        {
            string json = @"{ this is not valid json }";

            Assert.Throws<JsonReaderException>(() =>
                JsonConvert.DeserializeObject<TableResponse>(json));
        }

        [Test]
        public void DeserializeTableResponse_PlayerWithNoCards()
        {
            string json = @"{
                ""game"": {
                    ""id"": 1, ""tableId"": 1, ""tableName"": ""T"", ""gameNo"": 1,
                    ""handStep"": 0, ""stepName"": ""GAME_PREP"",
                    ""dealerSeat"": 0, ""smallBlindSeat"": 0, ""bigBlindSeat"": 0,
                    ""communityCards"": [], ""pot"": 0, ""sidePots"": [],
                    ""move"": 0, ""status"": ""waiting"",
                    ""smallBlind"": 1, ""bigBlind"": 2,
                    ""maxSeats"": 6, ""currentBet"": 0, ""winners"": []
                },
                ""players"": [{
                    ""playerId"": 1, ""username"": ""Test"", ""seat"": 1,
                    ""stack"": 100, ""bet"": 0, ""totalBet"": 0,
                    ""status"": ""1"", ""action"": """",
                    ""cards"": [], ""handRank"": """", ""winnings"": 0
                }]
            }";

            var response = JsonConvert.DeserializeObject<TableResponse>(json);
            var player = response.Players[0];

            Assert.IsNotNull(player.Cards);
            Assert.IsEmpty(player.Cards);
            Assert.IsFalse(player.HasCards);
        }
    }
}
