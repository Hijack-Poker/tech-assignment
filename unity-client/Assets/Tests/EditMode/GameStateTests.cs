using NUnit.Framework;
using Newtonsoft.Json;
using HijackPoker.Models;

namespace HijackPoker.Tests
{
    [TestFixture]
    public class GameStateTests
    {
        private GameState CreateGameState(int handStep = 0, string stepName = "GAME_PREP")
        {
            return new GameState
            {
                Id = 1,
                TableId = 1,
                TableName = "Test Table",
                GameNo = 1,
                HandStep = handStep,
                StepName = stepName,
                DealerSeat = 1,
                SmallBlindSeat = 2,
                BigBlindSeat = 3,
                CommunityCards = new System.Collections.Generic.List<string>(),
                Pot = 0,
                SidePots = new System.Collections.Generic.List<SidePot>(),
                Move = 0,
                Status = "in_progress",
                SmallBlind = 1.0f,
                BigBlind = 2.0f,
                MaxSeats = 6,
                CurrentBet = 0,
                Winners = new System.Collections.Generic.List<Winner>()
            };
        }

        // ── IsShowdown ──

        [Test]
        public void IsShowdown_Step11_ReturnsFalse()
        {
            var state = CreateGameState(handStep: 11);
            Assert.IsFalse(state.IsShowdown);
        }

        [Test]
        public void IsShowdown_Step12_ReturnsTrue()
        {
            var state = CreateGameState(handStep: 12);
            Assert.IsTrue(state.IsShowdown);
        }

        [Test]
        public void IsShowdown_Step13_ReturnsTrue()
        {
            var state = CreateGameState(handStep: 13);
            Assert.IsTrue(state.IsShowdown);
        }

        [Test]
        public void IsShowdown_Step15_ReturnsTrue()
        {
            var state = CreateGameState(handStep: 15);
            Assert.IsTrue(state.IsShowdown);
        }

        [Test]
        public void IsShowdown_Step0_ReturnsFalse()
        {
            var state = CreateGameState(handStep: 0);
            Assert.IsFalse(state.IsShowdown);
        }

        [Test]
        public void IsShowdown_Step5_PreFlopBetting_ReturnsFalse()
        {
            var state = CreateGameState(handStep: 5);
            Assert.IsFalse(state.IsShowdown);
        }

        // ── IsHandComplete ──

        [Test]
        public void IsHandComplete_RecordStatsStepName_ReturnsTrue()
        {
            var state = CreateGameState(stepName: "RECORD_STATS_AND_NEW_HAND");
            Assert.IsTrue(state.IsHandComplete);
        }

        [Test]
        public void IsHandComplete_OtherStepName_ReturnsFalse()
        {
            var state = CreateGameState(stepName: "DEAL_FLOP");
            Assert.IsFalse(state.IsHandComplete);
        }

        [Test]
        public void IsHandComplete_GamePrep_ReturnsFalse()
        {
            var state = CreateGameState(stepName: "GAME_PREP");
            Assert.IsFalse(state.IsHandComplete);
        }

        // ── JSON deserialization edge cases ──

        [Test]
        public void Deserialize_MultipleSidePots()
        {
            string json = @"{
                ""id"": 1, ""tableId"": 1, ""tableName"": ""T"", ""gameNo"": 1,
                ""handStep"": 14, ""stepName"": ""PAY_WINNERS"",
                ""dealerSeat"": 1, ""smallBlindSeat"": 2, ""bigBlindSeat"": 3,
                ""communityCards"": [""AH"", ""KD"", ""QC"", ""JS"", ""10H""],
                ""pot"": 100,
                ""sidePots"": [
                    { ""amount"": 60, ""eligibleSeats"": [1, 2, 3] },
                    { ""amount"": 30, ""eligibleSeats"": [1, 2] },
                    { ""amount"": 10, ""eligibleSeats"": [1] }
                ],
                ""move"": 0, ""status"": ""in_progress"",
                ""smallBlind"": 1, ""bigBlind"": 2,
                ""maxSeats"": 6, ""currentBet"": 0,
                ""winners"": [{ ""seat"": 1, ""playerId"": 101 }]
            }";

            var game = JsonConvert.DeserializeObject<GameState>(json);

            Assert.AreEqual(3, game.SidePots.Count);
            Assert.AreEqual(60, game.SidePots[0].Amount);
            Assert.AreEqual(3, game.SidePots[0].EligibleSeats.Count);
            Assert.AreEqual(10, game.SidePots[2].Amount);
            Assert.AreEqual(1, game.SidePots[2].EligibleSeats.Count);
        }

        [Test]
        public void Deserialize_MultipleWinners()
        {
            string json = @"{
                ""id"": 1, ""tableId"": 1, ""tableName"": ""T"", ""gameNo"": 1,
                ""handStep"": 13, ""stepName"": ""FIND_WINNERS"",
                ""dealerSeat"": 1, ""smallBlindSeat"": 2, ""bigBlindSeat"": 3,
                ""communityCards"": [], ""pot"": 100, ""sidePots"": [],
                ""move"": 0, ""status"": ""in_progress"",
                ""smallBlind"": 1, ""bigBlind"": 2,
                ""maxSeats"": 6, ""currentBet"": 0,
                ""winners"": [
                    { ""seat"": 1, ""playerId"": 101 },
                    { ""seat"": 4, ""playerId"": 104 }
                ]
            }";

            var game = JsonConvert.DeserializeObject<GameState>(json);

            Assert.AreEqual(2, game.Winners.Count);
            Assert.AreEqual(1, game.Winners[0].Seat);
            Assert.AreEqual(4, game.Winners[1].Seat);
        }

        [Test]
        public void Deserialize_FiveCommunityCards()
        {
            string json = @"{
                ""id"": 1, ""tableId"": 1, ""tableName"": ""T"", ""gameNo"": 1,
                ""handStep"": 12, ""stepName"": ""AFTER_RIVER_BETTING_ROUND"",
                ""dealerSeat"": 1, ""smallBlindSeat"": 2, ""bigBlindSeat"": 3,
                ""communityCards"": [""AH"", ""KD"", ""QC"", ""JS"", ""10H""],
                ""pot"": 50, ""sidePots"": [],
                ""move"": 0, ""status"": ""in_progress"",
                ""smallBlind"": 1, ""bigBlind"": 2,
                ""maxSeats"": 6, ""currentBet"": 0, ""winners"": []
            }";

            var game = JsonConvert.DeserializeObject<GameState>(json);

            Assert.AreEqual(5, game.CommunityCards.Count);
            Assert.AreEqual("AH", game.CommunityCards[0]);
            Assert.AreEqual("10H", game.CommunityCards[4]);
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(4, false)]
        [TestCase(11, false)]
        [TestCase(12, true)]
        [TestCase(14, true)]
        [TestCase(15, true)]
        public void IsShowdown_AllSteps(int step, bool expected)
        {
            var state = CreateGameState(handStep: step);
            Assert.AreEqual(expected, state.IsShowdown);
        }
    }
}
