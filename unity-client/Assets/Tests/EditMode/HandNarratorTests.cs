using System.Collections.Generic;
using NUnit.Framework;
using HijackPoker.Analytics;
using HijackPoker.Models;

namespace HijackPoker.Tests
{
    [TestFixture]
    public class HandNarratorTests
    {
        private HandNarrator _narrator;
        private PlayerProfiler _profiler;

        [SetUp]
        public void SetUp()
        {
            _narrator = new HandNarrator();
            _profiler = new PlayerProfiler();
        }

        // ── Situation classification ─────────────────────────────────

        [Test]
        public void ClassifySituation_NullState_ReturnsNormal()
        {
            Assert.AreEqual(SituationType.Normal,
                _narrator.ClassifySituation(null, null, null));
        }

        [Test]
        public void ClassifySituation_WalkTheBlinds_Detected()
        {
            var state = CreateState(5, new List<PlayerState>
            {
                new PlayerState { Seat = 1, Status = "1" },   // Active (BB)
                new PlayerState { Seat = 2, Status = "11" },   // Folded
                new PlayerState { Seat = 3, Status = "11" },   // Folded
                new PlayerState { Seat = 4, Status = "11" },   // Folded
            });

            Assert.AreEqual(SituationType.WalkTheBlinds,
                _narrator.ClassifySituation(state, null, null));
        }

        [Test]
        public void ClassifySituation_AllInClash_Detected()
        {
            var state = CreateState(9, new List<PlayerState>
            {
                new PlayerState { Seat = 1, Status = "12" },   // All-in
                new PlayerState { Seat = 2, Status = "12" },   // All-in
                new PlayerState { Seat = 3, Status = "11" },   // Folded
            });

            Assert.AreEqual(SituationType.AllInClash,
                _narrator.ClassifySituation(state, null, null));
        }

        [Test]
        public void ClassifySituation_MonsterPot_Detected()
        {
            var state = CreateState(7, new List<PlayerState>
            {
                new PlayerState { Seat = 1, Status = "1" },
                new PlayerState { Seat = 2, Status = "1" },
            }, pot: 50f, bigBlind: 2f);

            Assert.AreEqual(SituationType.MonsterPot,
                _narrator.ClassifySituation(state, null, null));
        }

        [Test]
        public void ClassifySituation_Cooler_AtShowdown()
        {
            var state = CreateState(13, new List<PlayerState>
            {
                new PlayerState { Seat = 1, Status = "1", HandRank = "Full House" },
                new PlayerState { Seat = 2, Status = "1", HandRank = "Flush" },
            });

            Assert.AreEqual(SituationType.Cooler,
                _narrator.ClassifySituation(state, null, _profiler));
        }

        [Test]
        public void ClassifySituation_StealAttempt_TightPlayerRaises()
        {
            // Create a tight player profile
            for (int i = 0; i < 5; i++)
                _profiler.RecordHandResult(1, false, false, false);
            // VPIP = 1/5 = 20% — need lower, add action
            _profiler.RecordAction(1, "raise", 5, false); // 1 VPIP out of 5 = 20%

            var prevState = CreateState(4, new List<PlayerState>
            {
                new PlayerState { Seat = 1, Status = "1", Action = "" },
                new PlayerState { Seat = 2, Status = "1", Action = "" },
            });

            var state = CreateState(5, new List<PlayerState>
            {
                new PlayerState { Seat = 1, Status = "1", Action = "raise" },
                new PlayerState { Seat = 2, Status = "1", Action = "" },
            });

            var result = _narrator.ClassifySituation(state, prevState, _profiler);
            Assert.AreEqual(SituationType.StealAttempt, result);
        }

        // ── Narration generation ─────────────────────────────────────

        [Test]
        public void GenerateNarration_NullState_ReturnsNull()
        {
            var tex = new BoardTexture();
            Assert.IsNull(_narrator.GenerateNarration(null, null, null, tex));
        }

        [Test]
        public void GenerateNarration_IncludesBoardTexture_OnFlop()
        {
            var state = CreateState(6, new List<PlayerState>
            {
                new PlayerState { Seat = 1, Status = "1" },
            });
            var tex = new BoardTexture { Description = "Wet two-tone board" };

            string narration = _narrator.GenerateNarration(state, null, null, tex);
            Assert.IsNotNull(narration);
            Assert.IsTrue(narration.Contains("Wet two-tone board"));
        }

        [Test]
        public void GenerateNarration_AllInClash_HasNarration()
        {
            var state = CreateState(9, new List<PlayerState>
            {
                new PlayerState { Seat = 1, Status = "12" },
                new PlayerState { Seat = 2, Status = "12" },
            });

            string narration = _narrator.GenerateNarration(state, null, null, new BoardTexture());
            Assert.IsNotNull(narration);
            Assert.IsTrue(narration.Contains("All-in"));
        }

        // ── Action commentary ────────────────────────────────────────

        [Test]
        public void GenerateActionCommentary_NullPlayer_ReturnsNull()
        {
            Assert.IsNull(_narrator.GenerateActionCommentary(null, "raise", _profiler));
        }

        [Test]
        public void GenerateActionCommentary_NullProfiler_ReturnsNull()
        {
            var player = new PlayerState { Seat = 1, Username = "Alice" };
            Assert.IsNull(_narrator.GenerateActionCommentary(player, "raise", null));
        }

        [Test]
        public void GenerateActionCommentary_TooFewHands_ReturnsNull()
        {
            var player = new PlayerState { Seat = 1, Username = "Alice" };
            _profiler.RecordHandResult(1, false, false, false); // Only 1 hand
            Assert.IsNull(_narrator.GenerateActionCommentary(player, "raise", _profiler));
        }

        [Test]
        public void GenerateActionCommentary_NitRaises_WarningMessage()
        {
            // Build a Nit profile: very tight, passive
            for (int i = 0; i < 10; i++)
                _profiler.RecordHandResult(1, false, false, false);
            _profiler.RecordAction(1, "call", 7, false); // 1 passive

            var player = new PlayerState { Seat = 1, Username = "Alice" };
            var profile = _profiler.GetProfile(1);
            // Force Nit classification by setting stats directly
            profile.VoluntaryPutInPot = 1;
            profile.TotalAggressive = 1;
            profile.TotalPassive = 3;
            profile.PreFlopRaise = 0;
            profile.Style = PlayStyle.Nit;

            string commentary = _narrator.GenerateActionCommentary(player, "raise", _profiler);
            Assert.IsNotNull(commentary);
            Assert.IsTrue(commentary.Contains("Alice"));
            Assert.IsTrue(commentary.Contains("NIT"));
        }

        // ── Helpers ──────────────────────────────────────────────────

        private TableResponse CreateState(int handStep, List<PlayerState> players,
            float pot = 10f, float bigBlind = 2f)
        {
            return new TableResponse
            {
                Game = new GameState
                {
                    GameNo = 1,
                    HandStep = handStep,
                    Pot = pot,
                    BigBlind = bigBlind,
                    CommunityCards = new List<string>()
                },
                Players = players
            };
        }
    }
}
