using System.Collections.Generic;
using HijackPoker.Models;

namespace HijackPoker.Analytics
{
    public enum SituationType
    {
        Normal,
        BluffSteal,
        Cooler,
        AllInClash,
        DonkBet,
        SlowPlay,
        StealAttempt,
        WalkTheBlinds,
        MonsterPot
    }

    public class HandNarrator
    {
        public SituationType ClassifySituation(TableResponse state, TableResponse prevState,
            PlayerProfiler profiler)
        {
            if (state?.Game == null || state.Players == null) return SituationType.Normal;

            int step = state.Game.HandStep;
            var players = state.Players;
            float pot = state.Game.Pot;
            float bb = state.Game.BigBlind > 0 ? state.Game.BigBlind : 2f;

            // Walk the blinds: everyone folded preflop to big blind
            if (step >= 5 && step <= 5)
            {
                int activePlayers = 0;
                int foldedPlayers = 0;
                foreach (var p in players)
                {
                    if (p.Seat < 1) continue;
                    if (p.IsFolded) foldedPlayers++;
                    else if (p.IsActive || p.IsAllIn) activePlayers++;
                }
                if (activePlayers == 1 && foldedPlayers >= 2)
                    return SituationType.WalkTheBlinds;
            }

            // All-in clash: 2+ players all-in
            int allInCount = 0;
            foreach (var p in players)
            {
                if (p.Seat < 1) continue;
                if (p.IsAllIn) allInCount++;
            }
            if (allInCount >= 2)
                return SituationType.AllInClash;

            // Monster pot: pot > 8x big blind by the flop
            if (step >= 6 && step <= 7 && pot > bb * 8f)
                return SituationType.MonsterPot;

            // Steal attempt: raise from late position preflop with tight player
            if (step >= 5 && step <= 5 && profiler != null && prevState?.Players != null)
            {
                foreach (var p in players)
                {
                    if (p.Seat < 1 || string.IsNullOrEmpty(p.Action)) continue;
                    if (p.Action == "raise" || p.Action == "allin")
                    {
                        var profile = profiler.GetProfile(p.Seat);
                        if (profile != null && profile.HandsTracked >= 3)
                        {
                            float vpip = (float)profile.VoluntaryPutInPot / profile.HandsTracked * 100f;
                            if (vpip < 20f)
                                return SituationType.StealAttempt;
                        }
                    }
                }
            }

            // Cooler: at showdown, multiple players with ranked hands
            if (step >= 13)
            {
                int strongHands = 0;
                foreach (var p in players)
                {
                    if (p.Seat < 1 || p.IsFolded) continue;
                    if (!string.IsNullOrEmpty(p.HandRank) && p.HandRank != "High Card")
                        strongHands++;
                }
                if (strongHands >= 2)
                    return SituationType.Cooler;
            }

            return SituationType.Normal;
        }

        public string GenerateNarration(TableResponse state, TableResponse prevState,
            PlayerProfiler profiler, BoardTexture boardTexture)
        {
            if (state?.Game == null || state.Players == null) return null;

            int step = state.Game.HandStep;
            var situation = ClassifySituation(state, prevState, profiler);

            var parts = new List<string>();

            // Board texture narration on flop/turn/river
            if (!string.IsNullOrEmpty(boardTexture.Description))
            {
                if (step == 6 || step == 7) // DEAL_FLOP or FLOP_BETTING_ROUND
                    parts.Add(boardTexture.Description);
            }

            // Situation-specific narration
            switch (situation)
            {
                case SituationType.WalkTheBlinds:
                    parts.Add("Everyone folds — big blind takes the pot uncontested");
                    break;
                case SituationType.AllInClash:
                    parts.Add("All-in confrontation! Players commit their stacks");
                    break;
                case SituationType.MonsterPot:
                    parts.Add($"Massive pot building — ${state.Game.Pot:F0} and climbing");
                    break;
                case SituationType.StealAttempt:
                    parts.Add("Tight player raises — possible steal attempt");
                    break;
                case SituationType.Cooler:
                    parts.Add("Cooler! Multiple strong hands collide at showdown");
                    break;
            }

            // Action commentary with profile context
            if (profiler != null && prevState?.Players != null)
            {
                foreach (var np in state.Players)
                {
                    if (np.Seat < 1 || string.IsNullOrEmpty(np.Action)) continue;

                    string prevAction = null;
                    foreach (var op in prevState.Players)
                    {
                        if (op.Seat == np.Seat) { prevAction = op.Action; break; }
                    }

                    if (np.Action != prevAction)
                    {
                        var commentary = GenerateActionCommentary(np, np.Action, profiler);
                        if (!string.IsNullOrEmpty(commentary))
                            parts.Add(commentary);
                    }
                }
            }

            if (parts.Count == 0) return null;
            return string.Join(" \u2022 ", parts);
        }

        public string GenerateActionCommentary(PlayerState player, string action,
            PlayerProfiler profiler)
        {
            if (player == null || profiler == null) return null;

            var profile = profiler.GetProfile(player.Seat);
            if (profile == null || profile.HandsTracked < 3) return null;

            string name = !string.IsNullOrEmpty(player.Username)
                ? player.Username : $"Seat {player.Seat}";
            string styleLabel = PlayStyleHelper.GetLabel(profile.Style);
            if (string.IsNullOrEmpty(styleLabel)) return null;

            switch (action?.ToLowerInvariant())
            {
                case "raise":
                case "allin":
                    if (profile.Style == PlayStyle.Nit || profile.Style == PlayStyle.Rock)
                        return $"{name} ({styleLabel}) raises — watch out, they usually have it";
                    if (profile.Style == PlayStyle.Maniac || profile.Style == PlayStyle.LAG)
                        return $"{name} ({styleLabel}) raises — could be anything";
                    break;

                case "call":
                    if (profile.Style == PlayStyle.CallingStation)
                        return $"{name} ({styleLabel}) calls — as expected";
                    break;

                case "fold":
                    if (profile.Style == PlayStyle.Fish || profile.Style == PlayStyle.CallingStation)
                        return $"{name} ({styleLabel}) folds — unusual for this player";
                    break;
            }

            return null;
        }
    }
}
