namespace HijackPoker.Utils
{
    public static class PhaseLabels
    {
        private static readonly string[] Labels =
        {
            "Preparing Hand",        // 0
            "Setting Up Dealer",     // 1
            "Posting Small Blind",   // 2
            "Posting Big Blind",     // 3
            "Dealing Hole Cards",    // 4
            "Pre-Flop Betting",      // 5
            "Dealing Flop",          // 6
            "Flop Betting",          // 7
            "Dealing Turn",          // 8
            "Turn Betting",          // 9
            "Dealing River",         // 10
            "River Betting",         // 11
            "Showdown",              // 12
            "Evaluating Hands",      // 13
            "Paying Winners",        // 14
            "Hand Complete"          // 15
        };

        public static string GetLabel(int step)
        {
            if (step >= 0 && step < Labels.Length)
                return Labels[step];

            return $"Unknown Step ({step})";
        }
    }
}
