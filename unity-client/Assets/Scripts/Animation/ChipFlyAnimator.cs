using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.UI;

namespace HijackPoker.Animation
{
    /// <summary>
    /// Animates temporary chip images arcing from seat positions to the pot
    /// when bets are collected. Follows the DealAnimator/PotDistributionAnimator
    /// pattern of creating cosmetic objects on the canvas layer.
    /// </summary>
    public static class ChipFlyAnimator
    {
        private static readonly (int value, Color face, Color edge)[] Denominations =
        {
            (100, new Color(0.15f, 0.15f, 0.18f, 1f), new Color(0.85f, 0.68f, 0.15f, 1f)),
            (25,  new Color(0.15f, 0.65f, 0.30f, 1f), new Color(0.10f, 0.50f, 0.22f, 1f)),
            (5,   new Color(0.85f, 0.20f, 0.20f, 1f), new Color(0.70f, 0.12f, 0.12f, 1f)),
            (1,   new Color(0.92f, 0.92f, 0.90f, 1f), new Color(0.78f, 0.78f, 0.76f, 1f)),
        };

        public static void PlayChipFly(
            AnimationController anim,
            Transform canvas,
            SeatView[] seats,
            TableResponse oldState,
            TableResponse newState,
            Vector2 potPos)
        {
            if (anim == null || canvas == null) return;
            if (oldState?.Players == null || newState?.Players == null) return;

            // Build lookup: new state bets by seat
            var newBetBySeat = new Dictionary<int, float>();
            foreach (var np in newState.Players)
                newBetBySeat[np.Seat] = np.Bet;

            float totalDelay = 0f;

            foreach (var op in oldState.Players)
            {
                if (op.Bet < 1f) continue;
                if (op.Seat < 1 || op.Seat > LayoutConfig.MaxSeats) continue;

                // Check if bet was collected (old > 0, new ~= 0)
                if (!newBetBySeat.TryGetValue(op.Seat, out float newBet)) continue;
                if (newBet >= 0.01f) continue;

                Vector2 seatPos = LayoutConfig.WorldToCanvasPos(seats[op.Seat].RectTransform);
                var chips = ChipStackView.DecomposeBet(op.Bet);
                bool soundPlayed = false;

                float chipDelay = totalDelay;
                foreach (var (count, denomIdx) in chips)
                {
                    var (_, face, edge) = Denominations[denomIdx];
                    int capped = Mathf.Min(count, 5);
                    for (int i = 0; i < capped; i++)
                    {
                        float chipDia = LayoutConfig.ChipDiameter;
                        Vector2 from = seatPos + new Vector2(0, i * LayoutConfig.ChipOverlap);
                        bool playSoundOnArrival = !soundPlayed;
                        soundPlayed = true;

                        FlyChip(anim, canvas, from, potPos, chipDelay, chipDia, face, edge,
                            playSoundOnArrival);
                        chipDelay += AnimationConfig.ChipFlyStagger;
                    }
                }

                totalDelay = chipDelay;
            }
        }

        private static void FlyChip(
            AnimationController anim, Transform canvas,
            Vector2 from, Vector2 to, float delay,
            float chipDia, Color face, Color edge,
            bool playSound)
        {
            var chipImg = UIFactory.CreateImage("FlyChip", canvas,
                Color.white, new Vector2(chipDia, chipDia));
            chipImg.sprite = TextureGenerator.GetChipTexture((int)chipDia, face, edge);
            var chipRt = chipImg.GetComponent<RectTransform>();
            chipRt.anchorMin = new Vector2(0.5f, 0.5f);
            chipRt.anchorMax = new Vector2(0.5f, 0.5f);
            chipRt.pivot = new Vector2(0.5f, 0.5f);
            chipRt.anchoredPosition = from;
            chipImg.raycastTarget = false;
            chipImg.gameObject.SetActive(false);

            GameObject capturedGo = chipImg.gameObject;
            bool capturedSound = playSound;

            System.Action snap = () =>
            {
                if (capturedGo != null) Object.Destroy(capturedGo);
            };

            new Timeline()
                .AppendInterval(delay)
                .AppendCallback(() =>
                {
                    if (capturedGo != null) capturedGo.SetActive(true);
                })
                .Append(() =>
                {
                    var h = Tweener.TweenFloat(0f, 1f, AnimationConfig.ChipFlyDuration, t =>
                    {
                        if (chipRt == null) return;
                        Vector2 linear = Vector2.Lerp(from, to, t);
                        Vector2 dir = (to - from).normalized;
                        Vector2 perp = new Vector2(-dir.y, dir.x);
                        float arc = Mathf.Sin(t * Mathf.PI) * AnimationConfig.ChipFlyArcHeight;
                        chipRt.anchoredPosition = linear + perp * arc;
                    }, EaseType.EaseInOutQuad);
                    h.SnapToFinal = snap;
                    return h;
                })
                .AppendCallback(() =>
                {
                    if (capturedGo != null) Object.Destroy(capturedGo);
                    if (capturedSound)
                        AudioManager.Instance?.Play(SoundType.ChipClink);
                })
                .Play(anim, snap);
        }
    }
}
