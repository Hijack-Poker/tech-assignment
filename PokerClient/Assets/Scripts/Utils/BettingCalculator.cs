using UnityEngine;

namespace HijackPoker.Utils
{
    public struct BettingContext
    {
        public float ToCall;
        public float MinRaise;
        public bool CanRaise;
        public bool IsCallAllIn;
    }

    public static class BettingCalculator
    {
        public static BettingContext Calculate(float currentBet, float actorBet, float actorStack, float bigBlind)
        {
            float toCall = Mathf.Max(0f, currentBet - actorBet);
            float minRaise = Mathf.Max(currentBet + bigBlind, currentBet * 2f);
            bool canRaise = actorStack > toCall + 0.01f;
            bool isCallAllIn = toCall >= actorStack && actorStack > 0;

            return new BettingContext
            {
                ToCall = toCall,
                MinRaise = minRaise,
                CanRaise = canRaise,
                IsCallAllIn = isCallAllIn
            };
        }
    }
}
