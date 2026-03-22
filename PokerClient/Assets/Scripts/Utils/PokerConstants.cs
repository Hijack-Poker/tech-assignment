namespace HijackPoker.Utils
{
    public static class PokerConstants
    {
        public const float TurnDurationSeconds = 20f;
        public const float LowTimeWarningSeconds = 5f;
        public const int MaxNameLength = 20;

        /// <summary>
        /// Steps 5, 7, 9, 11 are the four betting rounds:
        /// Pre-Flop, Flop, Turn, River.
        /// </summary>
        public static bool IsBettingStep(int step)
        {
            return step == 5 || step == 7 || step == 9 || step == 11;
        }
    }
}
