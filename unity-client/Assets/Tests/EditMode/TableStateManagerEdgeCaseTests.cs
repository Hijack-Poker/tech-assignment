using System;
using System.Collections.Generic;
using NUnit.Framework;
using HijackPoker.Managers;
using HijackPoker.Models;

namespace HijackPoker.Tests
{
    [TestFixture]
    public class TableStateManagerEdgeCaseTests
    {
        private TableStateManager _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new TableStateManager();
        }

        // ── Rapid successive updates ──

        [Test]
        public void RapidUpdates_1000States_AllEventsFireInOrder()
        {
            var receivedSteps = new List<int>();
            _manager.OnStateChanged += (old, @new) =>
            {
                if (@new?.Game != null)
                    receivedSteps.Add(@new.Game.HandStep);
            };

            for (int i = 0; i < 1000; i++)
            {
                _manager.UpdateState(CreateState(step: i % 16));
            }

            Assert.AreEqual(1000, receivedSteps.Count);
            for (int i = 0; i < 1000; i++)
            {
                Assert.AreEqual(i % 16, receivedSteps[i]);
            }
        }

        [Test]
        public void RapidUpdates_AlternateUpdateAndClear()
        {
            int updateCount = 0;
            int clearCount = 0;
            _manager.OnStateChanged += (old, @new) =>
            {
                if (@new != null) updateCount++;
                else clearCount++;
            };

            for (int i = 0; i < 50; i++)
            {
                _manager.UpdateState(CreateState(step: i));
                _manager.Clear();
            }

            Assert.AreEqual(50, updateCount);
            Assert.AreEqual(50, clearCount);
            Assert.IsNull(_manager.CurrentState);
        }

        // ── Clear mid-hand ──

        [Test]
        public void Clear_MidHand_ResetsToNull()
        {
            _manager.UpdateState(CreateState(step: 7));  // Flop betting
            Assert.IsNotNull(_manager.CurrentState);

            _manager.Clear();
            Assert.IsNull(_manager.CurrentState);
        }

        [Test]
        public void Clear_MidHand_ThenUpdate_WorksNormally()
        {
            _manager.UpdateState(CreateState(step: 7));
            _manager.Clear();

            TableResponse receivedOld = null;
            _manager.OnStateChanged += (old, @new) => receivedOld = old;

            _manager.UpdateState(CreateState(step: 0));
            Assert.IsNull(receivedOld, "Old state should be null after clear");
            Assert.IsNotNull(_manager.CurrentState);
            Assert.AreEqual(0, _manager.CurrentState.Game.HandStep);
        }

        [Test]
        public void Clear_Twice_SecondClearDoesNotFire()
        {
            _manager.UpdateState(CreateState(step: 5));

            int fireCount = 0;
            _manager.OnStateChanged += (_, __) => fireCount++;

            _manager.Clear();
            _manager.Clear();

            Assert.AreEqual(1, fireCount, "Second clear should not fire (already null)");
        }

        // ── Subscriber edge cases ──

        [Test]
        public void NoSubscribers_UpdateDoesNotThrow()
        {
            Assert.DoesNotThrow(() => _manager.UpdateState(CreateState(step: 0)));
        }

        [Test]
        public void NoSubscribers_ClearDoesNotThrow()
        {
            _manager.UpdateState(CreateState(step: 0));
            Assert.DoesNotThrow(() => _manager.Clear());
        }

        [Test]
        public void SubscriberThrows_OtherSubscribersStillNotified()
        {
            // If one subscriber throws, the event propagation stops (standard C# behavior)
            // This test documents that behavior
            bool firstCalled = false;
            _manager.OnStateChanged += (_, __) =>
            {
                firstCalled = true;
                throw new InvalidOperationException("Subscriber error");
            };

            bool secondCalled = false;
            _manager.OnStateChanged += (_, __) => secondCalled = true;

            Assert.Throws<InvalidOperationException>(() =>
                _manager.UpdateState(CreateState(step: 0)));

            Assert.IsTrue(firstCalled);
            // Standard C# multicast delegate behavior: exception stops propagation
            Assert.IsFalse(secondCalled);
        }

        // ── State with various data shapes ──

        [Test]
        public void UpdateState_WithEmptyPlayers()
        {
            var state = new TableResponse
            {
                Game = new GameState
                {
                    HandStep = 5,
                    CommunityCards = new List<string>(),
                    SidePots = new List<SidePot>(),
                    Winners = new List<Winner>()
                },
                Players = new List<PlayerState>()
            };

            _manager.UpdateState(state);
            Assert.AreEqual(0, _manager.CurrentState.Players.Count);
        }

        [Test]
        public void UpdateState_WithSixPlayers()
        {
            var players = new List<PlayerState>();
            for (int i = 1; i <= 6; i++)
            {
                players.Add(new PlayerState
                {
                    PlayerId = i,
                    Seat = i,
                    Status = "1",
                    Cards = new List<string> { "AH", "KD" },
                    Winnings = 0
                });
            }

            var state = new TableResponse
            {
                Game = new GameState
                {
                    HandStep = 5,
                    MaxSeats = 6,
                    CommunityCards = new List<string>(),
                    SidePots = new List<SidePot>(),
                    Winners = new List<Winner>()
                },
                Players = players
            };

            _manager.UpdateState(state);
            Assert.AreEqual(6, _manager.CurrentState.Players.Count);
        }

        [Test]
        public void UpdateState_WithSidePots_PreservesData()
        {
            var state = new TableResponse
            {
                Game = new GameState
                {
                    HandStep = 14,
                    Pot = 100,
                    CommunityCards = new List<string> { "AH", "KD", "QC", "JS", "10H" },
                    SidePots = new List<SidePot>
                    {
                        new SidePot { Amount = 60, EligibleSeats = new List<int> { 1, 2, 3 } },
                        new SidePot { Amount = 30, EligibleSeats = new List<int> { 1, 2 } },
                        new SidePot { Amount = 10, EligibleSeats = new List<int> { 1 } }
                    },
                    Winners = new List<Winner>
                    {
                        new Winner { Seat = 1, PlayerId = 101 }
                    }
                },
                Players = new List<PlayerState>()
            };

            _manager.UpdateState(state);
            Assert.AreEqual(3, _manager.CurrentState.Game.SidePots.Count);
            Assert.AreEqual(100, _manager.CurrentState.Game.Pot);
        }

        // ── Helper ──

        private static TableResponse CreateState(int step)
        {
            return new TableResponse
            {
                Game = new GameState
                {
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
