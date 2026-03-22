using HijackPoker.Models;
using HijackPoker.Utils;
using UnityEngine;

namespace HijackPoker.Managers
{
    public struct ActionResult
    {
        public string Action;
        public float Amount;

        public ActionResult(string action, float amount)
        {
            Action = action;
            Amount = amount;
        }
    }

    public static class AutoPlayDecision
    {
        public static ActionResult Decide(AutoPlayStyle style, BettingContext ctx, PlayerState actor, bool hasRaised, float roll)
        {
            float toCall = ctx.ToCall;
            float minRaise = ctx.MinRaise;
            bool canRaise = ctx.CanRaise;

            switch (style)
            {
                case AutoPlayStyle.Safe:
                    if (toCall <= 0f)
                        return new ActionResult("check", 0f);
                    else
                        return new ActionResult("call", toCall);

                case AutoPlayStyle.SmallRandom:
                    if (!hasRaised && canRaise && roll < 0.2f)
                    {
                        float raiseAmount = Mathf.Min(minRaise, actor.Stack + actor.Bet);
                        if (toCall <= 0f)
                            return new ActionResult("bet", raiseAmount);
                        else
                            return new ActionResult("raise", raiseAmount);
                    }
                    else if (toCall <= 0f)
                    {
                        return new ActionResult("check", 0f);
                    }
                    else
                    {
                        return new ActionResult("call", toCall);
                    }

                case AutoPlayStyle.Hard:
                    if (roll < 0.10f && toCall > 0f)
                    {
                        return new ActionResult("fold", 0f);
                    }
                    else if (roll < 0.50f)
                    {
                        if (toCall <= 0f)
                            return new ActionResult("check", 0f);
                        else
                            return new ActionResult("call", toCall);
                    }
                    else if (roll < 0.80f && canRaise)
                    {
                        float maxR = Mathf.Min(minRaise * 3f, actor.Stack + actor.Bet);
                        float raiseAmount = Mathf.Max(minRaise, Random.Range(minRaise, maxR));
                        if (toCall <= 0f)
                            return new ActionResult("bet", raiseAmount);
                        else
                            return new ActionResult("raise", raiseAmount);
                    }
                    else if (canRaise)
                    {
                        return new ActionResult("allin", actor.Stack);
                    }
                    else
                    {
                        if (toCall <= 0f)
                            return new ActionResult("check", 0f);
                        else
                            return new ActionResult("call", toCall);
                    }

                default:
                    return new ActionResult("check", 0f);
            }
        }
    }
}
