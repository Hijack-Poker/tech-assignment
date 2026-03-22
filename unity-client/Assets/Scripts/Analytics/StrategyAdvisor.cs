using System.Collections.Generic;
using HijackPoker.Models;

namespace HijackPoker.Analytics
{
    public static class StrategyAdvisor
    {
        public static List<string> GetAdvice(TableResponse state, PlayerProfiler profiler,
            int targetSeat)
        {
            var advice = new List<string>();
            if (state?.Players == null || profiler == null) return advice;

            var profile = profiler.GetProfile(targetSeat);
            if (profile == null || profile.HandsTracked < 3) return advice;

            string label = PlayStyleHelper.GetLabel(profile.Style);
            float vpip = (float)profile.VoluntaryPutInPot / profile.HandsTracked * 100f;

            switch (profile.Style)
            {
                case PlayStyle.Nit:
                case PlayStyle.Rock:
                    advice.Add($"Tight player ({vpip:F0}% VPIP) — steal their blinds");
                    advice.Add("Fold to their raises unless you're strong");
                    break;

                case PlayStyle.Fish:
                case PlayStyle.CallingStation:
                    advice.Add($"Loose passive ({vpip:F0}% VPIP) — value bet wider");
                    advice.Add("Don't bluff — they call too often");
                    break;

                case PlayStyle.LAG:
                    advice.Add($"Aggressive ({vpip:F0}% VPIP) — re-raise with premiums");
                    advice.Add("Their range is wide, look for trapping spots");
                    break;

                case PlayStyle.Maniac:
                    advice.Add($"Hyper-aggressive ({vpip:F0}% VPIP) — let them hang themselves");
                    advice.Add("Call down lighter, they're often bluffing");
                    break;

                case PlayStyle.TAG:
                    advice.Add($"Solid player ({vpip:F0}% VPIP) — respect their raises");
                    advice.Add("Avoid marginal spots against this player");
                    break;
            }

            if (profile.IsTilting)
                advice.Add("Player is tilting — expect wider calls and overbets");

            return advice;
        }

        public static string GetTableAdvice(TableResponse state, PlayerProfiler profiler,
            BoardTexture boardTexture)
        {
            if (state?.Players == null || profiler == null) return null;

            // Count loose/tight players
            int loose = 0, tight = 0, total = 0;
            foreach (var p in state.Players)
            {
                if (p.Seat < 1 || p.IsFolded) continue;
                var profile = profiler.GetProfile(p.Seat);
                if (profile == null || profile.HandsTracked < 3) continue;
                total++;
                float vpip = (float)profile.VoluntaryPutInPot / profile.HandsTracked * 100f;
                if (vpip > 30f) loose++;
                else if (vpip < 20f) tight++;
            }

            if (total == 0) return null;

            if (loose > tight && loose >= 2)
                return "Loose table — tighten up and value bet relentlessly";
            if (tight > loose && tight >= 2)
                return "Tight table — steal blinds and apply pressure";

            if (boardTexture.WetnessRating >= 7f)
                return "Wet board — tighter ranges recommended, draws are live";
            if (boardTexture.WetnessRating <= 3f)
                return "Dry board — c-bets will get more folds here";

            return null;
        }
    }
}
