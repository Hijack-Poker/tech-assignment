using System.Collections.Generic;
using NUnit.Framework;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.Utils;

namespace HijackPoker.Tests
{
    /// <summary>
    /// Integration tests that verify the full data flow from mock API responses
    /// through the state manager, with card visibility, phase labels, and
    /// money formatting applied correctly at each step.
    /// </summary>
    [TestFixture]
    public class StateIntegrationTests
    {
        private TableStateManager _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new TableStateManager();
        }

        // ── Card Visibility Integration ──────────────────────────────

        [Test]
        public void CardVisibility_PreShowdown_ActivePlayerHidden()
        {
            var state = CreateStateWithPlayer(step: 7, status: "1", winnings: 0);
            _manager.UpdateState(state);

            var player = _manager.CurrentState.Players[0];
            bool show = ShowdownLogic.ShouldShowCards(
                _manager.CurrentState.Game.HandStep, player.Status, player.Winnings);

            Assert.IsFalse(show, "Active player cards should be hidden before showdown");
        }

        [Test]
        public void CardVisibility_PreShowdown_FoldedPlayerHidden()
        {
            var state = CreateStateWithPlayer(step: 9, status: "11", winnings: 0);
            _manager.UpdateState(state);

            var player = _manager.CurrentState.Players[0];
            bool show = ShowdownLogic.ShouldShowCards(
                _manager.CurrentState.Game.HandStep, player.Status, player.Winnings);

            Assert.IsFalse(show, "Folded player cards should be hidden");
        }

        [Test]
        public void CardVisibility_Showdown_ActivePlayerRevealed()
        {
            var state = CreateStateWithPlayer(step: 12, status: "1", winnings: 0);
            _manager.UpdateState(state);

            var player = _manager.CurrentState.Players[0];
            bool show = ShowdownLogic.ShouldShowCards(
                _manager.CurrentState.Game.HandStep, player.Status, player.Winnings);

            Assert.IsTrue(show, "Active player cards should be visible at showdown (step >= 12)");
        }

        [Test]
        public void CardVisibility_Showdown_FoldedPlayerStillHidden()
        {
            var state = CreateStateWithPlayer(step: 13, status: "11", winnings: 0);
            _manager.UpdateState(state);

            var player = _manager.CurrentState.Players[0];
            bool show = ShowdownLogic.ShouldShowCards(
                _manager.CurrentState.Game.HandStep, player.Status, player.Winnings);

            Assert.IsFalse(show, "Folded player cards should remain hidden even at showdown");
        }

        [Test]
        public void CardVisibility_Winner_AlwaysRevealed()
        {
            var state = CreateStateWithPlayer(step: 14, status: "1", winnings: 50f);
            _manager.UpdateState(state);

            var player = _manager.CurrentState.Players[0];
            bool show = ShowdownLogic.ShouldShowCards(
                _manager.CurrentState.Game.HandStep, player.Status, player.Winnings);

            Assert.IsTrue(show, "Winner cards should always be visible");
        }

        [Test]
        public void CardVisibility_AllIn_RevealedAtShowdown()
        {
            var state = CreateStateWithPlayer(step: 12, status: "12", winnings: 0);
            _manager.UpdateState(state);

            var player = _manager.CurrentState.Players[0];
            bool show = ShowdownLogic.ShouldShowCards(
                _manager.CurrentState.Game.HandStep, player.Status, player.Winnings);

            Assert.IsTrue(show, "All-in player cards should be visible at showdown");
        }

        [Test]
        public void CardVisibility_FullHandProgression()
        {
            // Active player through all 16 steps
            for (int step = 0; step <= 15; step++)
            {
                var state = CreateStateWithPlayer(step: step, status: "1", winnings: 0);
                _manager.UpdateState(state);

                var player = _manager.CurrentState.Players[0];
                bool show = ShowdownLogic.ShouldShowCards(step, player.Status, player.Winnings);

                if (step >= 12)
                    Assert.IsTrue(show, $"Cards should be visible at step {step}");
                else
                    Assert.IsFalse(show, $"Cards should be hidden at step {step}");
            }
        }

        [Test]
        public void CardVisibility_MixedTable_CorrectPerPlayer()
        {
            var state = new TableResponse
            {
                Game = new GameState
                {
                    HandStep = 13,
                    CommunityCards = new List<string> { "AH", "7D", "2C", "KS", "9H" },
                    SidePots = new List<SidePot>(),
                    Winners = new List<Winner>
                    {
                        new Winner { Seat = 1, PlayerId = 101 }
                    }
                },
                Players = new List<PlayerState>
                {
                    new PlayerState { PlayerId = 101, Seat = 1, Status = "1",
                        Cards = new List<string> { "AS", "KD" }, Winnings = 30f,
                        HandRank = "Two Pair" },
                    new PlayerState { PlayerId = 102, Seat = 2, Status = "11",
                        Cards = new List<string> { "3D", "5C" }, Winnings = 0 },
                    new PlayerState { PlayerId = 103, Seat = 3, Status = "1",
                        Cards = new List<string> { "QH", "JH" }, Winnings = 0 },
                    new PlayerState { PlayerId = 104, Seat = 4, Status = "12",
                        Cards = new List<string> { "9D", "9S" }, Winnings = 0 },
                }
            };

            _manager.UpdateState(state);
            var game = _manager.CurrentState.Game;

            // Winner: visible (winnings > 0)
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(
                game.HandStep, state.Players[0].Status, state.Players[0].Winnings));

            // Folded: hidden
            Assert.IsFalse(ShowdownLogic.ShouldShowCards(
                game.HandStep, state.Players[1].Status, state.Players[1].Winnings));

            // Active non-winner at showdown: visible
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(
                game.HandStep, state.Players[2].Status, state.Players[2].Winnings));

            // All-in at showdown: visible
            Assert.IsTrue(ShowdownLogic.ShouldShowCards(
                game.HandStep, state.Players[3].Status, state.Players[3].Winnings));
        }

        // ── Phase Label Integration ──────────────────────────────────

        [Test]
        public void PhaseLabels_CorrectThroughFullHand()
        {
            for (int step = 0; step <= 15; step++)
            {
                var state = CreateStateWithPlayer(step: step, status: "1", winnings: 0);
                _manager.UpdateState(state);

                string label = PhaseLabels.GetLabel(_manager.CurrentState.Game.HandStep);
                Assert.IsNotNull(label, $"Phase label should not be null at step {step}");
                Assert.IsNotEmpty(label, $"Phase label should not be empty at step {step}");
            }
        }

        // ── Rapid State Updates ──────────────────────────────────────

        [Test]
        public void RapidUpdates_NoRaceConditions()
        {
            int eventCount = 0;
            TableResponse lastOld = null;
            TableResponse lastNew = null;

            _manager.OnStateChanged += (old, @new) =>
            {
                eventCount++;
                lastOld = old;
                lastNew = @new;
            };

            // Rapidly update through 100 states
            var states = new List<TableResponse>();
            for (int i = 0; i < 100; i++)
                states.Add(CreateStateWithPlayer(step: i % 16, status: "1", winnings: 0));

            foreach (var s in states)
                _manager.UpdateState(s);

            Assert.AreEqual(100, eventCount, "All events should fire");
            Assert.AreSame(states[98], lastOld, "Last old state should be second-to-last update");
            Assert.AreSame(states[99], lastNew, "Last new state should be final update");
            Assert.AreSame(states[99], _manager.CurrentState, "CurrentState should be final");
        }

        [Test]
        public void RapidUpdates_HandTransitions()
        {
            // Simulate multiple hand transitions
            var transitions = new List<(TableResponse old, TableResponse @new)>();
            _manager.OnStateChanged += (old, @new) =>
            {
                if (old?.Game != null && @new?.Game != null
                    && old.Game.GameNo != @new.Game.GameNo)
                    transitions.Add((old, @new));
            };

            for (int hand = 1; hand <= 5; hand++)
            {
                for (int step = 0; step <= 15; step++)
                {
                    _manager.UpdateState(CreateStateWithGameNo(hand, step));
                }
            }

            // 4 transitions (hand 1→2, 2→3, 3→4, 4→5)
            Assert.AreEqual(4, transitions.Count, "Should detect 4 hand transitions");

            for (int i = 0; i < transitions.Count; i++)
            {
                Assert.AreEqual(i + 1, transitions[i].old.Game.GameNo);
                Assert.AreEqual(i + 2, transitions[i].@new.Game.GameNo);
            }
        }

        // ── Player Convenience Properties Integration ────────────────

        [Test]
        public void PlayerState_ConvenienceProperties_CorrectAfterStateUpdate()
        {
            var state = new TableResponse
            {
                Game = new GameState
                {
                    HandStep = 14,
                    CommunityCards = new List<string>(),
                    SidePots = new List<SidePot>(),
                    Winners = new List<Winner>()
                },
                Players = new List<PlayerState>
                {
                    new PlayerState { Status = "1", Cards = new List<string> { "AH", "KD" },
                        Winnings = 50f },
                    new PlayerState { Status = "11", Cards = new List<string>(),
                        Winnings = 0 },
                    new PlayerState { Status = "12", Cards = new List<string> { "9H", "9D" },
                        Winnings = 0 },
                }
            };

            _manager.UpdateState(state);
            var players = _manager.CurrentState.Players;

            // Active winner
            Assert.IsTrue(players[0].IsActive);
            Assert.IsFalse(players[0].IsFolded);
            Assert.IsFalse(players[0].IsAllIn);
            Assert.IsTrue(players[0].IsWinner);
            Assert.IsTrue(players[0].HasCards);

            // Folded
            Assert.IsFalse(players[1].IsActive);
            Assert.IsTrue(players[1].IsFolded);
            Assert.IsFalse(players[1].IsWinner);
            Assert.IsFalse(players[1].HasCards);

            // All-in
            Assert.IsFalse(players[2].IsActive);
            Assert.IsTrue(players[2].IsAllIn);
            Assert.IsFalse(players[2].IsWinner);
            Assert.IsTrue(players[2].HasCards);
        }

        // ── Money Formatting Integration ─────────────────────────────

        [Test]
        public void MoneyFormatting_StackAndWinnings()
        {
            var state = CreateStateWithPlayer(step: 14, status: "1", winnings: 1234.50f);
            state.Players[0].Stack = 5678.00f;
            _manager.UpdateState(state);

            var player = _manager.CurrentState.Players[0];
            Assert.AreEqual("$5,678.00", MoneyFormatter.Format(player.Stack));
            Assert.AreEqual("$1,234.50", MoneyFormatter.Format(player.Winnings));
        }

        // ── Helpers ──────────────────────────────────────────────────

        private static TableResponse CreateStateWithPlayer(int step, string status, float winnings)
        {
            return new TableResponse
            {
                Game = new GameState
                {
                    GameNo = 1,
                    HandStep = step,
                    CommunityCards = new List<string>(),
                    SidePots = new List<SidePot>(),
                    Winners = new List<Winner>()
                },
                Players = new List<PlayerState>
                {
                    new PlayerState
                    {
                        PlayerId = 1, Seat = 1, Status = status,
                        Cards = new List<string> { "AH", "KD" },
                        Winnings = winnings
                    }
                }
            };
        }

        private static TableResponse CreateStateWithGameNo(int gameNo, int step)
        {
            return new TableResponse
            {
                Game = new GameState
                {
                    GameNo = gameNo,
                    HandStep = step,
                    CommunityCards = new List<string>(),
                    SidePots = new List<SidePot>(),
                    Winners = new List<Winner>()
                },
                Players = new List<PlayerState>()
            };
        }
    }
}
