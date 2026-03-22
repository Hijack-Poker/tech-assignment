using NUnit.Framework;
using HijackPoker.Analytics;

namespace HijackPoker.Tests
{
    [TestFixture]
    public class PlayerProfilerTests
    {
        private PlayerProfiler _profiler;

        [SetUp]
        public void SetUp()
        {
            _profiler = new PlayerProfiler();
        }

        // ── Basic getters with no data ────────────────────────────────

        [Test]
        public void GetProfile_NoData_ReturnsNull()
        {
            Assert.IsNull(_profiler.GetProfile(1));
        }

        [Test]
        public void GetVPIP_NoData_ReturnsZero()
        {
            Assert.AreEqual(0f, _profiler.GetVPIP(1));
        }

        [Test]
        public void GetPFR_NoData_ReturnsZero()
        {
            Assert.AreEqual(0f, _profiler.GetPFR(1));
        }

        [Test]
        public void GetAggressionFactor_NoData_ReturnsZero()
        {
            Assert.AreEqual(0f, _profiler.GetAggressionFactor(1));
        }

        [Test]
        public void ClassifyStyle_NoData_ReturnsUnknown()
        {
            Assert.AreEqual(PlayStyle.Unknown, _profiler.ClassifyStyle(1));
        }

        // ── RecordAction: VPIP tracking ──────────────────────────────

        [Test]
        public void RecordAction_PreflopCall_CountsAsVPIP()
        {
            _profiler.RecordAction(1, "call", 5, false);
            RecordHands(1, 1);

            Assert.AreEqual(100f, _profiler.GetVPIP(1));
        }

        [Test]
        public void RecordAction_PreflopBlindPost_DoesNotCountAsVPIP()
        {
            _profiler.RecordAction(1, "call", 5, true);
            RecordHands(1, 1);

            Assert.AreEqual(0f, _profiler.GetVPIP(1));
        }

        [Test]
        public void RecordAction_PreflopRaise_CountsAsVPIPAndPFR()
        {
            _profiler.RecordAction(1, "raise", 5, false);
            RecordHands(1, 1);

            Assert.AreEqual(100f, _profiler.GetVPIP(1));
            Assert.AreEqual(100f, _profiler.GetPFR(1));
        }

        [Test]
        public void RecordAction_PostflopCall_DoesNotCountAsVPIP()
        {
            _profiler.RecordAction(1, "call", 7, false); // Flop
            RecordHands(1, 1);

            Assert.AreEqual(0f, _profiler.GetVPIP(1));
        }

        // ── RecordAction: Aggression Factor ──────────────────────────

        [Test]
        public void AggressionFactor_BetsAndRaisesOverCalls()
        {
            // 2 aggressive, 1 passive => AF = 2.0
            _profiler.RecordAction(1, "bet", 7, false);
            _profiler.RecordAction(1, "raise", 9, false);
            _profiler.RecordAction(1, "call", 11, false);

            Assert.AreEqual(2f, _profiler.GetAggressionFactor(1));
        }

        [Test]
        public void AggressionFactor_NoCalls_ReturnsZero()
        {
            // AF with zero calls and zero aggression => 0
            _profiler.RecordAction(1, "check", 7, false);

            Assert.AreEqual(0f, _profiler.GetAggressionFactor(1));
        }

        [Test]
        public void RecordAction_Fold_TracksFoldCount()
        {
            _profiler.RecordAction(1, "fold", 5, false);

            var profile = _profiler.GetProfile(1);
            Assert.AreEqual(1, profile.FoldCount);
        }

        [Test]
        public void RecordAction_AllIn_CountsAsAggressive()
        {
            _profiler.RecordAction(1, "allin", 5, false);

            var profile = _profiler.GetProfile(1);
            Assert.AreEqual(1, profile.TotalAggressive);
            Assert.AreEqual(1, profile.PreFlopRaise);
            Assert.AreEqual(1, profile.VoluntaryPutInPot);
        }

        // ── RecordHandResult ─────────────────────────────────────────

        [Test]
        public void RecordHandResult_TracksHandCount()
        {
            _profiler.RecordHandResult(1, false, false, false);
            _profiler.RecordHandResult(1, false, false, false);

            Assert.AreEqual(2, _profiler.GetProfile(1).HandsTracked);
        }

        [Test]
        public void RecordHandResult_Showdown_TracksWTSD()
        {
            _profiler.RecordHandResult(1, true, true, true);

            var profile = _profiler.GetProfile(1);
            Assert.AreEqual(1, profile.WentToShowdown);
            Assert.AreEqual(1, profile.SawFlop);
        }

        [Test]
        public void RecordHandResult_NoShowdown_DoesNotTrackWTSD()
        {
            _profiler.RecordHandResult(1, false, false, true);

            Assert.AreEqual(0, _profiler.GetProfile(1).WentToShowdown);
        }

        // ── Classification ───────────────────────────────────────────

        [Test]
        public void Classify_TAG_TightWithHighPFR()
        {
            var profile = new PlayerProfiler.PlayerProfile
            {
                HandsTracked = 10,
                VoluntaryPutInPot = 2,  // 20% VPIP
                PreFlopRaise = 2,       // 20% PFR
                TotalAggressive = 5,
                TotalPassive = 2        // AF = 2.5
            };
            Assert.AreEqual(PlayStyle.TAG, PlayerProfiler.Classify(profile));
        }

        [Test]
        public void Classify_LAG_LooseWithHighAggression()
        {
            var profile = new PlayerProfiler.PlayerProfile
            {
                HandsTracked = 10,
                VoluntaryPutInPot = 5,  // 50% VPIP
                PreFlopRaise = 3,       // 30% PFR
                TotalAggressive = 8,
                TotalPassive = 3        // AF = 2.67
            };
            Assert.AreEqual(PlayStyle.LAG, PlayerProfiler.Classify(profile));
        }

        [Test]
        public void Classify_Nit_VeryTightVeryPassive()
        {
            var profile = new PlayerProfiler.PlayerProfile
            {
                HandsTracked = 10,
                VoluntaryPutInPot = 1,  // 10% VPIP
                PreFlopRaise = 0,       // 0% PFR
                TotalAggressive = 1,
                TotalPassive = 3        // AF = 0.33
            };
            Assert.AreEqual(PlayStyle.Nit, PlayerProfiler.Classify(profile));
        }

        [Test]
        public void Classify_Fish_LoosePassive()
        {
            var profile = new PlayerProfiler.PlayerProfile
            {
                HandsTracked = 10,
                VoluntaryPutInPot = 4,  // 40% VPIP
                PreFlopRaise = 1,
                TotalAggressive = 2,
                TotalPassive = 2        // AF = 1.0
            };
            Assert.AreEqual(PlayStyle.Fish, PlayerProfiler.Classify(profile));
        }

        [Test]
        public void Classify_Maniac_VeryLooseHyperAggressive()
        {
            var profile = new PlayerProfiler.PlayerProfile
            {
                HandsTracked = 10,
                VoluntaryPutInPot = 6,  // 60% VPIP
                PreFlopRaise = 5,
                TotalAggressive = 12,
                TotalPassive = 3        // AF = 4.0
            };
            Assert.AreEqual(PlayStyle.Maniac, PlayerProfiler.Classify(profile));
        }

        [Test]
        public void Classify_CallingStation_LooseNeverRaises()
        {
            var profile = new PlayerProfiler.PlayerProfile
            {
                HandsTracked = 10,
                VoluntaryPutInPot = 5,  // 50% VPIP
                PreFlopRaise = 0,
                TotalAggressive = 1,
                TotalPassive = 8        // AF = 0.125
            };
            Assert.AreEqual(PlayStyle.CallingStation, PlayerProfiler.Classify(profile));
        }

        [Test]
        public void Classify_Unknown_TooFewHands()
        {
            var profile = new PlayerProfiler.PlayerProfile
            {
                HandsTracked = 2,
                VoluntaryPutInPot = 2,
                PreFlopRaise = 2,
                TotalAggressive = 5,
                TotalPassive = 1
            };
            Assert.AreEqual(PlayStyle.Unknown, PlayerProfiler.Classify(profile));
        }

        [Test]
        public void Classify_NullProfile_ReturnsUnknown()
        {
            Assert.AreEqual(PlayStyle.Unknown, PlayerProfiler.Classify(null));
        }

        // ── Tilt detection ───────────────────────────────────────────

        [Test]
        public void TiltDetection_ThreeLossesInFive_DetectsTilt()
        {
            _profiler.RecordHandResult(1, false, false, false); // loss
            _profiler.RecordHandResult(1, false, false, false); // loss
            _profiler.RecordHandResult(1, false, false, false); // loss

            Assert.IsTrue(_profiler.GetProfile(1).IsTilting);
        }

        [Test]
        public void TiltDetection_TwoLosses_NoTilt()
        {
            _profiler.RecordHandResult(1, false, true, false);  // win
            _profiler.RecordHandResult(1, false, false, false); // loss
            _profiler.RecordHandResult(1, false, false, false); // loss

            Assert.IsFalse(_profiler.GetProfile(1).IsTilting);
        }

        // ── Edge cases ───────────────────────────────────────────────

        [Test]
        public void RecordAction_InvalidSeat_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _profiler.RecordAction(0, "call", 5, false));
            Assert.DoesNotThrow(() => _profiler.RecordAction(-1, "raise", 5, false));
        }

        [Test]
        public void RecordAction_NullAction_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _profiler.RecordAction(1, null, 5, false));
            Assert.DoesNotThrow(() => _profiler.RecordAction(1, "", 5, false));
        }

        [Test]
        public void RecordHandResult_InvalidSeat_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _profiler.RecordHandResult(0, false, false, false));
        }

        [Test]
        public void Reset_ClearsAllProfiles()
        {
            _profiler.RecordAction(1, "call", 5, false);
            _profiler.RecordHandResult(1, false, false, false);

            _profiler.Reset();

            Assert.IsNull(_profiler.GetProfile(1));
            Assert.AreEqual(0f, _profiler.GetVPIP(1));
        }

        [Test]
        public void MultipleSeats_TrackedIndependently()
        {
            _profiler.RecordAction(1, "raise", 5, false);
            _profiler.RecordAction(2, "fold", 5, false);
            RecordHands(1, 1);
            RecordHands(2, 1);

            Assert.AreEqual(100f, _profiler.GetVPIP(1));
            Assert.AreEqual(0f, _profiler.GetVPIP(2));
        }

        // ── Helpers ──────────────────────────────────────────────────

        private void RecordHands(int seat, int count)
        {
            for (int i = 0; i < count; i++)
                _profiler.RecordHandResult(seat, false, false, false);
        }
    }
}
