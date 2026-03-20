namespace HijackPoker.Utils
{
    public static class ShowdownLogic
    {
        /// <summary>
        /// Determines whether a player's hole cards should be shown face-up.
        ///
        /// Rules (in priority order):
        /// 1. winnings > 0 → always show (winner)
        /// 2. status "4" (Show Cards) → always show
        /// 3. status "11" (Folded) → always hide
        /// 4. step >= 12 (showdown) → show for active/all-in players
        /// 5. Otherwise → hide (face-down)
        /// </summary>
        public static bool ShouldShowCards(int handStep, string status, float winnings)
        {
            // Winners always show cards
            if (winnings > 0)
                return true;

            // Show Cards status always reveals
            if (status == "4")
                return true;

            // Folded always hidden
            if (status == "11")
                return false;

            // At showdown (step >= 12), active and all-in players show cards
            if (handStep >= 12)
                return true;

            // During play, cards are face-down
            return false;
        }

        public static bool ShouldShowCards(int handStep, string status, float winnings, bool isHumanSeat)
        {
            // Human player always sees their own cards (once dealt, not folded)
            if (isHumanSeat && handStep >= 4 && status != "11")
                return true;

            return ShouldShowCards(handStep, status, winnings);
        }
    }
}
