using System.Collections.Generic;
using NUnit.Framework;
using HijackPoker.Managers;
using HijackPoker.Models;

namespace HijackPoker.Tests
{
    [TestFixture]
    public class TableStateManagerTests
    {
        private TableStateManager _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new TableStateManager();
        }

        [Test]
        public void CurrentState_InitiallyNull()
        {
            Assert.IsNull(_manager.CurrentState);
        }

        [Test]
        public void UpdateState_SetsCurrentState()
        {
            var state = CreateState(step: 0);
            _manager.UpdateState(state);
            Assert.AreSame(state, _manager.CurrentState);
        }

        [Test]
        public void UpdateState_FiresOnStateChanged()
        {
            TableResponse receivedOld = null;
            TableResponse receivedNew = null;
            int fireCount = 0;

            _manager.OnStateChanged += (old, @new) =>
            {
                receivedOld = old;
                receivedNew = @new;
                fireCount++;
            };

            var state = CreateState(step: 0);
            _manager.UpdateState(state);

            Assert.AreEqual(1, fireCount);
            Assert.IsNull(receivedOld);
            Assert.AreSame(state, receivedNew);
        }

        [Test]
        public void UpdateState_ProvidesOldStateOnSecondUpdate()
        {
            var state1 = CreateState(step: 0);
            var state2 = CreateState(step: 1);

            TableResponse receivedOld = null;
            TableResponse receivedNew = null;

            _manager.UpdateState(state1);

            _manager.OnStateChanged += (old, @new) =>
            {
                receivedOld = old;
                receivedNew = @new;
            };

            _manager.UpdateState(state2);

            Assert.AreSame(state1, receivedOld);
            Assert.AreSame(state2, receivedNew);
        }

        [Test]
        public void UpdateState_SequentialUpdates_ChainCorrectly()
        {
            var states = new List<TableResponse>();
            for (int i = 0; i <= 15; i++)
                states.Add(CreateState(step: i));

            var receivedPairs = new List<(TableResponse old, TableResponse @new)>();
            _manager.OnStateChanged += (old, @new) => receivedPairs.Add((old, @new));

            foreach (var s in states)
                _manager.UpdateState(s);

            Assert.AreEqual(16, receivedPairs.Count);

            // First update: old is null
            Assert.IsNull(receivedPairs[0].old);
            Assert.AreSame(states[0], receivedPairs[0].@new);

            // Subsequent: old is previous new
            for (int i = 1; i < 16; i++)
            {
                Assert.AreSame(states[i - 1], receivedPairs[i].old);
                Assert.AreSame(states[i], receivedPairs[i].@new);
            }
        }

        [Test]
        public void UpdateState_WithNull_SetsCurrentStateToNull()
        {
            _manager.UpdateState(CreateState(step: 5));
            _manager.UpdateState(null);
            Assert.IsNull(_manager.CurrentState);
        }

        [Test]
        public void UpdateState_WithNull_FiresEventWithOldState()
        {
            var state = CreateState(step: 5);
            _manager.UpdateState(state);

            TableResponse receivedOld = null;
            TableResponse receivedNew = null;
            _manager.OnStateChanged += (old, @new) =>
            {
                receivedOld = old;
                receivedNew = @new;
            };

            _manager.UpdateState(null);

            Assert.AreSame(state, receivedOld);
            Assert.IsNull(receivedNew);
        }

        [Test]
        public void Clear_SetsCurrentStateToNull()
        {
            _manager.UpdateState(CreateState(step: 3));
            _manager.Clear();
            Assert.IsNull(_manager.CurrentState);
        }

        [Test]
        public void Clear_FiresEvent()
        {
            var state = CreateState(step: 3);
            _manager.UpdateState(state);

            TableResponse receivedOld = null;
            bool fired = false;
            _manager.OnStateChanged += (old, @new) =>
            {
                receivedOld = old;
                fired = true;
            };

            _manager.Clear();

            Assert.IsTrue(fired);
            Assert.AreSame(state, receivedOld);
        }

        [Test]
        public void Clear_WhenAlreadyNull_DoesNotFireEvent()
        {
            bool fired = false;
            _manager.OnStateChanged += (old, @new) => fired = true;

            _manager.Clear();

            Assert.IsFalse(fired);
        }

        [Test]
        public void MultipleSubscribers_AllNotified()
        {
            int count1 = 0, count2 = 0;
            _manager.OnStateChanged += (_, __) => count1++;
            _manager.OnStateChanged += (_, __) => count2++;

            _manager.UpdateState(CreateState(step: 0));

            Assert.AreEqual(1, count1);
            Assert.AreEqual(1, count2);
        }

        [Test]
        public void UpdateState_SameStateTwice_StillFiresEvent()
        {
            var state = CreateState(step: 5);
            int fireCount = 0;
            _manager.OnStateChanged += (_, __) => fireCount++;

            _manager.UpdateState(state);
            _manager.UpdateState(state);

            Assert.AreEqual(2, fireCount);
        }

        // ── Helper ──────────────────────────────────────────────────

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
