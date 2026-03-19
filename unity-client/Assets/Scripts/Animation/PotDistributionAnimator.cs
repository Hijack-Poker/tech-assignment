using System;
using System.Collections.Generic;
using UnityEngine;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.UI;
using HijackPoker.Utils;

namespace HijackPoker.Animation
{
    /// <summary>
    /// Animates pot-to-winner fly animations with shimmer trails, gold bursts,
    /// starburst effects, and confetti for big wins.
    /// Uses Timeline for flat sequencing (replaces nested Delay→OnComplete chains).
    /// </summary>
    public static class PotDistributionAnimator
    {
        public static void PlayPotDistribution(
            AnimationController anim,
            Transform canvas,
            SeatView[] seats,
            TableResponse oldState,
            TableResponse newState,
            Vector2 potPos)
        {
            if (newState?.Players == null) return;

            var winners = new List<PlayerState>();
            foreach (var p in newState.Players)
                if (p.IsWinner && p.Winnings > 0) winners.Add(p);
            if (winners.Count == 0) return;

            bool hasSidePots = oldState?.Game?.SidePots != null
                && oldState.Game.SidePots.Count > 0;

            var flights = new List<(string label, int seat, float delay)>();

            if (!hasSidePots)
            {
                for (int i = 0; i < winners.Count; i++)
                {
                    var w = winners[i];
                    flights.Add((
                        $"+{MoneyFormatter.Format(w.Winnings)}",
                        w.Seat, i * AnimationConfig.PotFlightStagger));
                }
            }
            else
            {
                var winnerSeatSet = new HashSet<int>();
                if (newState.Game?.Winners != null)
                    foreach (var w in newState.Game.Winners)
                        winnerSeatSet.Add(w.Seat);

                float sidePotTotal = 0;
                foreach (var sp in oldState.Game.SidePots)
                    sidePotTotal += sp.Amount;
                float mainPot = Mathf.Max(0, oldState.Game.Pot - sidePotTotal);

                int idx = 0;

                if (mainPot > 0.01f && winners.Count > 0)
                {
                    flights.Add((
                        $"Pot: {MoneyFormatter.Format(mainPot)}",
                        winners[0].Seat, idx * AnimationConfig.PotFlightStagger));
                    idx++;
                }

                for (int i = 0; i < oldState.Game.SidePots.Count; i++)
                {
                    var sp = oldState.Game.SidePots[i];
                    if (sp.Amount <= 0.01f || sp.EligibleSeats == null) continue;

                    int spWinner = -1;
                    foreach (var seat in sp.EligibleSeats)
                    {
                        if (winnerSeatSet.Contains(seat))
                        {
                            spWinner = seat;
                            break;
                        }
                    }
                    if (spWinner < 1) spWinner = winners[0].Seat;

                    flights.Add((
                        $"Side Pot {i + 1}: {MoneyFormatter.Format(sp.Amount)}",
                        spWinner, idx * AnimationConfig.PotFlightStagger));
                    idx++;
                }
            }

            if (flights.Count == 0) return;

            var lastFlightPerSeat = new Dictionary<int, int>();
            for (int i = 0; i < flights.Count; i++)
                lastFlightPerSeat[flights[i].seat] = i;

            float totalWinnings = 0;
            foreach (var w in winners) totalWinnings += w.Winnings;

            for (int i = 0; i < flights.Count; i++)
            {
                var (label, seat, delay) = flights[i];
                var seatPos = GetSeatCanvasPos(seats, seat);
                bool triggerStack = lastFlightPerSeat[seat] == i;
                bool isLastFlight = i == flights.Count - 1;
                CreateFlyingPot(anim, canvas, seats, label, potPos, seatPos, delay,
                    triggerStack ? seat : -1, isLastFlight, totalWinnings);
            }
        }

        private static Vector2 GetSeatCanvasPos(SeatView[] seats, int seatNum)
        {
            if (seatNum >= 1 && seatNum <= LayoutConfig.MaxSeats && seats[seatNum] != null)
                return LayoutConfig.WorldToCanvasPos(seats[seatNum].RectTransform);
            return Vector2.zero;
        }

        private static void CreateFlyingPot(
            AnimationController anim,
            Transform canvas,
            SeatView[] seats,
            string text, Vector2 from, Vector2 to,
            float delay, int stackSeatNum,
            bool isLastFlight, float totalWinnings)
        {
            var flyText = UIFactory.CreateText("FlyPot", canvas, text,
                15f, UIFactory.AccentGold, TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold);
            var flyRt = flyText.GetComponent<RectTransform>();
            flyRt.anchorMin = new Vector2(0.5f, 0.5f);
            flyRt.anchorMax = new Vector2(0.5f, 0.5f);
            flyRt.pivot = new Vector2(0.5f, 0.5f);
            flyRt.anchoredPosition = from;
            flyRt.sizeDelta = new Vector2(160, 24);

            int capturedSeat = stackSeatNum;
            bool capturedLast = isLastFlight;
            float capturedWinnings = totalWinnings;

            // Lightweight cleanup for cancellation — does NOT spawn new effects
            Action snapCleanup = () =>
            {
                if (flyText != null) UnityEngine.Object.Destroy(flyText.gameObject);
                if (capturedSeat >= 1 && capturedSeat <= LayoutConfig.MaxSeats)
                    seats[capturedSeat].AnimateDeferredStack();
            };

            Action onArrival = () =>
            {
                if (flyText != null) UnityEngine.Object.Destroy(flyText.gameObject);
                AudioManager.Instance?.Play(SoundType.ChipClink);

                // Gold burst at arrival
                SparkleEffects.SpawnSparkles(canvas, to, 24,
                    new Color(0.96f, 0.62f, 0.04f, 0.95f), 80f, 1.0f, anim);
                AudioManager.Instance?.PlayWithDelay(SoundType.Starburst, 0.05f);

                // Starburst at winner avatar
                SparkleEffects.SpawnStarburst(canvas, to, UIFactory.AccentGold, 0.8f, anim);

                // Ring pulse from seat + second staggered ring
                SparkleEffects.SpawnRingPulse(canvas, to, UIFactory.AccentGold, 120f, 1.0f, anim);
                var ringDelay = anim.Play(Tweener.Delay(AnimationConfig.PotRingStagger));
                ringDelay.OnComplete(() =>
                    SparkleEffects.SpawnRingPulse(canvas, to,
                        new Color(0.96f, 0.62f, 0.04f, 0.5f), 100f, 0.8f, anim));

                if (capturedSeat >= 1 && capturedSeat <= LayoutConfig.MaxSeats)
                    seats[capturedSeat].AnimateDeferredStack();

                // Multi-phase winner celebration for last flight
                if (capturedLast)
                {
                    WinnerCelebration.Play(anim, canvas, seats, capturedSeat, capturedWinnings);
                }
            };

            new Timeline()
                .AppendInterval(delay)
                .AppendCallback(() =>
                {
                    SparkleEffects.SpawnShimmerTrail(canvas, from, to, 10,
                        new Color(0.96f, 0.62f, 0.04f, 0.7f), AnimationConfig.PotFly, anim);
                })
                .Append(() =>
                {
                    var h = Tweener.TweenPosition(flyRt, from, to,
                        AnimationConfig.PotFly, EaseType.EaseInOutQuad);
                    h.SnapToFinal = () =>
                    {
                        if (flyRt != null) flyRt.anchoredPosition = to;
                    };
                    return h;
                })
                .AppendCallback(onArrival)
                .Play(anim, snapCleanup);
        }
    }
}
