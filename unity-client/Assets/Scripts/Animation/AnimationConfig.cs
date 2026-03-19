namespace HijackPoker.Animation
{
    /// <summary>
    /// Animation group identifiers for per-group speed control.
    /// </summary>
    public enum AnimGroup
    {
        Default,
        Deal,
        Particles,
        UI,
    }

    /// <summary>
    /// Centralized timing registry for all animation durations.
    /// All hardcoded constants extracted from individual animators live here.
    /// Provides per-group speed multipliers via Scale().
    /// </summary>
    public static class AnimationConfig
    {
        // ── Global Speed ──────────────────────────────────────────────
        public static float GlobalSpeed = 1f;

        // Per-group speed multipliers (higher = faster)
        public static float DealSpeed = 1f;
        public static float ParticlesSpeed = 1f;
        public static float UISpeed = 1f;

        // ── Deal ──────────────────────────────────────────────────────
        public static readonly float DealCardFly = 0.22f;
        public static readonly float DealCardGap = 0.05f;
        public static readonly float DealArcHeight = 35f;
        public static readonly float DealInitialDelay = 0.08f;
        public static readonly float DealFlightRotation = 8f;
        public static readonly float DealDeckFade = 0.2f;
        public static readonly float DealBetFade = 0.25f;

        // ── Shuffle ───────────────────────────────────────────────────
        public static readonly float ShuffleSweep = 0.45f;
        public static readonly float ShuffleSweepStagger = 0.06f;
        public static readonly float ShuffleMergeDuration = 0.5f;
        public static readonly float ShuffleMergeOffset = 28f;

        // ── Hand Flash ────────────────────────────────────────────────
        public static readonly float HandFlashFadeIn = 0.2f;
        public static readonly float HandFlashScalePop = 0.3f;
        public static readonly float HandFlashHold = 0.4f;
        public static readonly float HandFlashFadeOut = 0.2f;

        // ── Pot Distribution ──────────────────────────────────────────
        public static readonly float PotFly = 0.6f;
        public static readonly float PotFlightStagger = 0.2f;
        public static readonly float PotRingStagger = 0.12f;

        // ── All-In Impact ─────────────────────────────────────────────
        public static readonly float AllInRingStagger1 = 0.08f;
        public static readonly float AllInRingStagger2 = 0.16f;
        public static readonly float AllInStampPop = 0.25f;
        public static readonly float AllInStampHold = 0.3f;
        public static readonly float AllInStampFade = 0.3f;

        // ── Card Reveal ───────────────────────────────────────────────
        public static readonly float RevealGlintSweep = 0.5f;
        public static readonly float RevealScreenFlashFade = 0.6f;
        public static readonly float RevealRingDelayTier4 = 0.15f;
        public static readonly float RevealBurstScale = 0.25f;
        public static readonly float RevealBurstFade = 0.3f;
        public static readonly float RevealBurstRingDelay = 0.08f;

        // ── Winner Celebration ────────────────────────────────────────
        public static readonly float WinnerBackdropFade = 0.3f;
        public static readonly float WinnerAvatarFly = 0.5f;
        public static readonly float WinnerAvatarScale = 2.5f;
        public static readonly float WinnerBannerDelay = 0.5f;
        public static readonly float WinnerBannerPop = 0.35f;
        public static readonly float WinnerConfettiDelay = 0.15f;
        public static readonly float WinnerConfettiDuration = 2.5f;
        public static readonly float WinnerDismissDelay = 2.5f;
        public static readonly float WinnerDismissFade = 0.4f;

        // ── Chip Fly ─────────────────────────────────────────────────
        public static readonly float ChipFlyDuration = 0.45f;
        public static readonly float ChipFlyArcHeight = 40f;
        public static readonly float ChipFlyStagger = 0.05f;

        // ── Phase Punch ──────────────────────────────────────────────
        public static readonly float PhasePunchDuration = 0.35f;
        public static readonly float PhasePunchMagnitude = 0.15f;
        public static readonly int PhasePunchVibrato = 8;

        // ── Seat UI ───────────────────────────────────────────────────
        public static readonly float SeatStackTween = 0.4f;
        public static readonly float SeatFoldDuration = 0.4f;
        public static readonly float SeatBetTween = 0.3f;
        public static readonly float SeatActionPop = 0.2f;
        public static readonly float SeatHandRankFade = 0.4f;
        public static readonly float SeatWinningsPop = 0.4f;
        public static readonly float SeatFoldGhostSweep = 0.35f;
        public static readonly float SeatFoldGhostDuration = 0.4f;
        public static readonly float SeatFoldRecovery = 0.3f;

        /// <summary>
        /// Applies both global and per-group speed multipliers to a base duration.
        /// </summary>
        public static float Scale(float baseDuration, AnimGroup group = AnimGroup.Default)
        {
            float groupSpeed = group switch
            {
                AnimGroup.Deal => DealSpeed,
                AnimGroup.Particles => ParticlesSpeed,
                AnimGroup.UI => UISpeed,
                _ => 1f,
            };
            float combinedSpeed = GlobalSpeed * groupSpeed;
            return combinedSpeed > 0f ? baseDuration / combinedSpeed : baseDuration;
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            GlobalSpeed = 1f;
            DealSpeed = 1f;
            ParticlesSpeed = 1f;
            UISpeed = 1f;
        }
    }
}
