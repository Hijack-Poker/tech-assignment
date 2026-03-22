using System.Collections.Generic;
using NUnit.Framework;
using HijackPoker.Managers;
using HijackPoker.Models;

namespace HijackPoker.Tests
{
    [TestFixture]
    public class SessionTrackerTests
    {
        private SessionTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _tracker = new SessionTracker();
        }

        [Test]
        public void GetDelta_NoData_ReturnsZero()
        {
            Assert.AreEqual(0f, _tracker.GetDelta(1));
        }

        [Test]
        public void GetSession_NoData_ReturnsNull()
        {
            Assert.IsNull(_tracker.GetSession(1));
        }

        [Test]
        public void RecordHandEnd_SingleHand_TracksInitialStack()
        {
            var state = CreateState(1, 100f, 0f);
            _tracker.RecordHandEnd(state);

            var session = _tracker.GetSession(1);
            Assert.IsNotNull(session);
            Assert.AreEqual(100f, session.InitialStack);
            Assert.AreEqual(100f, session.CurrentStack);
            Assert.AreEqual(1, session.HandsPlayed);
        }

        [Test]
        public void RecordHandEnd_Winner_IncreasesHandsWon()
        {
            var state = CreateState(1, 150f, 50f);
            _tracker.RecordHandEnd(state);

            var session = _tracker.GetSession(1);
            Assert.AreEqual(1, session.HandsWon);
            Assert.AreEqual(50f, session.BiggestPot);
        }

        [Test]
        public void RecordHandEnd_MultipleHands_TracksDelta()
        {
            // Hand 1: start at 100
            var state1 = CreateState(1, 100f, 0f, gameNo: 1);
            _tracker.RecordHandEnd(state1);

            // Hand 2: won, now at 150
            var state2 = CreateState(1, 150f, 60f, gameNo: 2);
            _tracker.RecordHandEnd(state2);

            Assert.AreEqual(50f, _tracker.GetDelta(1));
            Assert.AreEqual(2, _tracker.GetSession(1).HandsPlayed);
            Assert.AreEqual(1, _tracker.GetSession(1).HandsWon);
        }

        [Test]
        public void RecordHandEnd_SameGameNo_DoesNotDuplicate()
        {
            var state = CreateState(1, 100f, 0f, gameNo: 1);
            _tracker.RecordHandEnd(state);
            _tracker.RecordHandEnd(state);

            Assert.AreEqual(1, _tracker.GetSession(1).HandsPlayed);
        }

        [Test]
        public void RecordHandEnd_MultipleSeats_TracksIndependently()
        {
            var state = new TableResponse
            {
                Game = new GameState { GameNo = 1 },
                Players = new List<PlayerState>
                {
                    new PlayerState { Seat = 1, Stack = 100f, Winnings = 0f },
                    new PlayerState { Seat = 2, Stack = 200f, Winnings = 50f }
                }
            };
            _tracker.RecordHandEnd(state);

            Assert.AreEqual(100f, _tracker.GetSession(1).CurrentStack);
            Assert.AreEqual(200f, _tracker.GetSession(2).CurrentStack);
            Assert.AreEqual(0, _tracker.GetSession(1).HandsWon);
            Assert.AreEqual(1, _tracker.GetSession(2).HandsWon);
        }

        [Test]
        public void GetDelta_Loss_ReturnsNegative()
        {
            var state1 = CreateState(1, 100f, 0f, gameNo: 1);
            _tracker.RecordHandEnd(state1);

            var state2 = CreateState(1, 80f, 0f, gameNo: 2);
            _tracker.RecordHandEnd(state2);

            Assert.AreEqual(-20f, _tracker.GetDelta(1));
        }

        [Test]
        public void RecordHandEnd_BiggestPot_TracksMaximum()
        {
            var state1 = CreateState(1, 130f, 30f, gameNo: 1);
            _tracker.RecordHandEnd(state1);

            var state2 = CreateState(1, 180f, 50f, gameNo: 2);
            _tracker.RecordHandEnd(state2);

            var state3 = CreateState(1, 200f, 20f, gameNo: 3);
            _tracker.RecordHandEnd(state3);

            Assert.AreEqual(50f, _tracker.GetSession(1).BiggestPot);
        }

        [Test]
        public void Reset_ClearsAllData()
        {
            var state = CreateState(1, 100f, 50f);
            _tracker.RecordHandEnd(state);

            _tracker.Reset();

            Assert.IsNull(_tracker.GetSession(1));
            Assert.AreEqual(0f, _tracker.GetDelta(1));
        }

        [Test]
        public void RecordHandEnd_NullState_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _tracker.RecordHandEnd(null));
        }

        [Test]
        public void RecordHandEnd_NullPlayers_DoesNotThrow()
        {
            var state = new TableResponse { Game = new GameState { GameNo = 1 } };
            Assert.DoesNotThrow(() => _tracker.RecordHandEnd(state));
        }

        [Test]
        public void RecordHandEnd_InvalidSeat_Skipped()
        {
            var state = new TableResponse
            {
                Game = new GameState { GameNo = 1 },
                Players = new List<PlayerState>
                {
                    new PlayerState { Seat = 0, Stack = 100f }
                }
            };
            _tracker.RecordHandEnd(state);
            Assert.IsNull(_tracker.GetSession(0));
        }

        private TableResponse CreateState(int seat, float stack, float winnings,
            int gameNo = 1)
        {
            return new TableResponse
            {
                Game = new GameState { GameNo = gameNo },
                Players = new List<PlayerState>
                {
                    new PlayerState
                    {
                        Seat = seat,
                        Stack = stack,
                        Winnings = winnings
                    }
                }
            };
        }
    }
}
