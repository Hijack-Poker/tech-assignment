using System.Collections.Generic;

namespace HijackPoker.Analytics
{
    public class PlayerProfiler
    {
        public class PlayerProfile
        {
            public int HandsTracked;
            public int VoluntaryPutInPot;  // Times voluntarily put $ in preflop (excludes forced blinds)
            public int PreFlopRaise;       // Times raised preflop
            public int TotalAggressive;    // Bets + raises across all streets
            public int TotalPassive;       // Calls across all streets
            public int FoldCount;
            public int WentToShowdown;     // Reached showdown without folding
            public int SawFlop;            // Was still active at flop
            public PlayStyle Style;
            public bool IsTilting;

            // Circular buffer for last 5 hand results (true = won)
            public bool[] RecentResults = new bool[5];
            public int RecentResultIndex;
            public int RecentResultCount;
        }

        private readonly Dictionary<int, PlayerProfile> _profiles = new();
        private const int MinHandsForClassification = 3;

        public void RecordAction(int seat, string action, int handStep, bool isBlindPost)
        {
            if (seat < 1 || string.IsNullOrEmpty(action)) return;

            var profile = GetOrCreateProfile(seat);

            bool isPreflop = handStep >= 4 && handStep <= 5;

            switch (action.ToLowerInvariant())
            {
                case "call":
                    profile.TotalPassive++;
                    if (isPreflop && !isBlindPost)
                        profile.VoluntaryPutInPot++;
                    break;

                case "bet":
                    profile.TotalAggressive++;
                    break;

                case "raise":
                    profile.TotalAggressive++;
                    if (isPreflop)
                    {
                        profile.PreFlopRaise++;
                        profile.VoluntaryPutInPot++;
                    }
                    break;

                case "allin":
                    profile.TotalAggressive++;
                    if (isPreflop)
                    {
                        profile.PreFlopRaise++;
                        profile.VoluntaryPutInPot++;
                    }
                    break;

                case "fold":
                    profile.FoldCount++;
                    break;

                // "check" is neither aggressive nor passive
            }
        }

        public void RecordHandResult(int seat, bool reachedShowdown, bool won, bool sawFlop)
        {
            if (seat < 1) return;

            var profile = GetOrCreateProfile(seat);
            profile.HandsTracked++;

            if (reachedShowdown)
                profile.WentToShowdown++;

            if (sawFlop)
                profile.SawFlop++;

            // Track recent results for tilt detection
            profile.RecentResults[profile.RecentResultIndex] = won;
            profile.RecentResultIndex = (profile.RecentResultIndex + 1) % 5;
            if (profile.RecentResultCount < 5)
                profile.RecentResultCount++;

            // Update tilt status: 3+ losses in last 5 hands
            profile.IsTilting = DetectTilt(profile);

            // Reclassify after each hand
            profile.Style = Classify(profile);
        }

        public PlayerProfile GetProfile(int seat)
        {
            _profiles.TryGetValue(seat, out var profile);
            return profile;
        }

        public float GetVPIP(int seat)
        {
            var profile = GetProfile(seat);
            if (profile == null || profile.HandsTracked == 0) return 0f;
            return (float)profile.VoluntaryPutInPot / profile.HandsTracked * 100f;
        }

        public float GetPFR(int seat)
        {
            var profile = GetProfile(seat);
            if (profile == null || profile.HandsTracked == 0) return 0f;
            return (float)profile.PreFlopRaise / profile.HandsTracked * 100f;
        }

        public float GetAggressionFactor(int seat)
        {
            var profile = GetProfile(seat);
            if (profile == null || profile.TotalPassive == 0) return 0f;
            return (float)profile.TotalAggressive / profile.TotalPassive;
        }

        public PlayStyle ClassifyStyle(int seat)
        {
            var profile = GetProfile(seat);
            if (profile == null) return PlayStyle.Unknown;
            return profile.Style;
        }

        public void Reset()
        {
            _profiles.Clear();
        }

        private PlayerProfile GetOrCreateProfile(int seat)
        {
            if (!_profiles.TryGetValue(seat, out var profile))
            {
                profile = new PlayerProfile();
                _profiles[seat] = profile;
            }
            return profile;
        }

        internal static PlayStyle Classify(PlayerProfile profile)
        {
            if (profile == null || profile.HandsTracked < MinHandsForClassification)
                return PlayStyle.Unknown;

            float vpip = (float)profile.VoluntaryPutInPot / profile.HandsTracked * 100f;
            float pfr = (float)profile.PreFlopRaise / profile.HandsTracked * 100f;
            float af = profile.TotalPassive > 0
                ? (float)profile.TotalAggressive / profile.TotalPassive
                : (profile.TotalAggressive > 0 ? 10f : 0f);

            // Maniac: very loose, hyper-aggressive
            if (vpip > 50f && af > 3f)
                return PlayStyle.Maniac;

            // LAG: loose aggressive
            if (vpip > 30f && af > 2f)
                return PlayStyle.LAG;

            // TAG: tight aggressive
            if (vpip < 25f && pfr > 10f)
                return PlayStyle.TAG;

            // Nit: very tight, very passive
            if (vpip < 12f && af < 1.5f)
                return PlayStyle.Nit;

            // Rock: tight, low aggression
            if (vpip < 20f && af < 1.5f)
                return PlayStyle.Rock;

            // Calling Station: loose, rarely raises
            if (vpip > 30f && af < 1f)
                return PlayStyle.CallingStation;

            // Fish: loose passive
            if (vpip > 30f && af < 1.5f)
                return PlayStyle.Fish;

            return PlayStyle.Unknown;
        }

        private static bool DetectTilt(PlayerProfile profile)
        {
            if (profile.RecentResultCount < 3) return false;

            int losses = 0;
            int count = profile.RecentResultCount;
            for (int i = 0; i < count; i++)
            {
                if (!profile.RecentResults[i])
                    losses++;
            }
            return losses >= 3;
        }
    }
}
