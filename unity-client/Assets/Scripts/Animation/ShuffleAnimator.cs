using System.Collections.Generic;
using UnityEngine;
using TMPro;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.UI;

namespace HijackPoker.Animation
{
    /// <summary>
    /// Animates end-of-hand sequence: card sweep to center, shuffle visual with sparkles,
    /// and "Hand #N" flash overlay with frosted glass pill + starburst effect.
    /// Uses Timeline for flat sequencing (replaces nested OnComplete chains).
    /// </summary>
    public static class ShuffleAnimator
    {
        private static Vector2 Card1Offset => new Vector2(-UI.LayoutConfig.SeatCardSpacing, -24f);
        private static Vector2 Card2Offset => new Vector2(UI.LayoutConfig.SeatCardSpacing, -24f);

        public static void PlayShuffle(
            AnimationController anim,
            Transform canvas,
            SeatView[] seats,
            TableResponse oldState,
            int newHandNumber,
            Vector2 centerPos)
        {
            var flyingCards = new List<GameObject>();
            float delay = 0f;

            // Sweep hole cards from old state
            if (oldState?.Players != null)
            {
                foreach (var player in oldState.Players)
                {
                    if (player == null || !player.HasCards || player.IsFolded) continue;
                    Vector2 seatPos = GetSeatCanvasPos(seats, player.Seat);

                    var fly1 = SweepCard(anim, canvas, seatPos + Card1Offset,
                        centerPos, delay, UI.LayoutConfig.SeatCardScale);
                    flyingCards.Add(fly1);
                    delay += AnimationConfig.ShuffleSweepStagger;

                    var fly2 = SweepCard(anim, canvas, seatPos + Card2Offset,
                        centerPos, delay, UI.LayoutConfig.SeatCardScale);
                    flyingCards.Add(fly2);
                    delay += AnimationConfig.ShuffleSweepStagger;
                }
            }

            // Sweep community cards
            if (oldState?.Game?.CommunityCards != null && oldState.Game.CommunityCards.Count > 0)
            {
                float totalWidth = 5 * CardView.CardSize.x + 4 * 6f;
                float startX = -totalWidth / 2f + CardView.CardSize.x / 2f;

                for (int i = 0; i < oldState.Game.CommunityCards.Count && i < 5; i++)
                {
                    Vector2 cardPos = centerPos +
                        new Vector2(startX + i * (CardView.CardSize.x + 6f), 0);
                    var fly = SweepCard(anim, canvas, cardPos, centerPos, delay, 1f);
                    flyingCards.Add(fly);
                    delay += AnimationConfig.ShuffleSweepStagger;
                }
            }

            if (flyingCards.Count == 0)
            {
                PlayHandFlash(anim, canvas, newHandNumber, 0f);
                return;
            }

            float sweepEnd = delay + AnimationConfig.ShuffleSweep;
            ScheduleShuffleVisual(anim, canvas, centerPos, sweepEnd, flyingCards);
            PlayHandFlash(anim, canvas, newHandNumber,
                sweepEnd + AnimationConfig.ShuffleMergeDuration + 0.1f);
        }

        private static Vector2 GetSeatCanvasPos(SeatView[] seats, int seatNum)
        {
            if (seatNum >= 1 && seatNum <= LayoutConfig.MaxSeats && seats[seatNum] != null)
                return LayoutConfig.WorldToCanvasPos(seats[seatNum].RectTransform);
            return Vector2.zero;
        }

        private static GameObject SweepCard(AnimationController anim, Transform canvas,
            Vector2 from, Vector2 to, float delay, float cardScale)
        {
            var go = CreateCardBack(canvas, from, cardScale);
            var rt = go.GetComponent<RectTransform>();

            System.Action snap = () => { if (go != null) Object.Destroy(go); };

            System.Action doSweep = () =>
            {
                if (go == null) return;

                var posHandle = anim.Play(
                    Tweener.TweenPosition(rt, from, to, AnimationConfig.ShuffleSweep,
                        EaseType.EaseInOutQuad));
                posHandle.SnapToFinal = snap;

                anim.Play(Tweener.TweenRotation(rt, 0f,
                    Random.Range(-15f, 15f), AnimationConfig.ShuffleSweep, EaseType.EaseInOutQuad));

                anim.Play(Tweener.TweenFloat(cardScale, cardScale * 0.6f, AnimationConfig.ShuffleSweep,
                    s => { if (rt != null) rt.localScale = Vector3.one * s; }))
                    .SnapToFinal = snap;
            };

            if (delay > 0f)
            {
                var dh = anim.Play(Tweener.Delay(delay));
                dh.SnapToFinal = snap;
                dh.OnComplete(doSweep);
            }
            else
            {
                doSweep();
            }

            return go;
        }

        private static void ScheduleShuffleVisual(AnimationController anim, Transform canvas,
            Vector2 center, float delay, List<GameObject> sweepCards)
        {
            System.Action cleanupSweep = () =>
            {
                foreach (var g in sweepCards)
                    if (g != null) Object.Destroy(g);
            };

            float mergeOffset = AnimationConfig.ShuffleMergeOffset;
            float half = AnimationConfig.ShuffleMergeDuration * 0.5f;

            GameObject left = null, right = null;
            RectTransform leftRt = null, rightRt = null;

            System.Action cleanupStacks = () =>
            {
                if (left != null) Object.Destroy(left);
                if (right != null) Object.Destroy(right);
            };

            System.Action cleanupAll = () =>
            {
                cleanupSweep();
                cleanupStacks();
            };

            new Timeline()
                .AppendInterval(delay)
                .AppendCallback(() =>
                {
                    cleanupSweep();
                    AudioManager.Instance?.Play(SoundType.Shuffle);

                    left = CreateCardBack(canvas, center + new Vector2(-mergeOffset, 0), 1f);
                    right = CreateCardBack(canvas, center + new Vector2(mergeOffset, 0), 1f);
                    leftRt = left.GetComponent<RectTransform>();
                    rightRt = right.GetComponent<RectTransform>();
                })
                .Append(() =>
                {
                    var h = Tweener.TweenPosition(leftRt,
                        center + new Vector2(-mergeOffset, 0), center, half, EaseType.EaseInOutQuad);
                    h.SnapToFinal = cleanupStacks;
                    return h;
                })
                .Join(() =>
                {
                    var h = Tweener.TweenPosition(rightRt,
                        center + new Vector2(mergeOffset, 0), center, half, EaseType.EaseInOutQuad);
                    h.SnapToFinal = cleanupStacks;
                    return h;
                })
                .AppendCallback(() =>
                {
                    SparkleEffects.SpawnSparkles(canvas, center, 10,
                        new Color(1f, 1f, 1f, 0.8f), 40f, 0.5f, anim);
                    AudioManager.Instance?.Play(SoundType.Sparkle);
                })
                .Append(() =>
                {
                    var h = Tweener.TweenFloat(1f, 0f, half,
                        a =>
                        {
                            if (leftRt != null)
                                leftRt.localScale = Vector3.one * (1f + (1f - a) * 0.15f);
                            if (rightRt != null)
                                rightRt.localScale = Vector3.one * (1f + (1f - a) * 0.15f);
                        });
                    h.SnapToFinal = cleanupStacks;
                    return h;
                })
                .AppendCallback(cleanupStacks)
                .Play(anim, cleanupAll);
        }

        private static void PlayHandFlash(AnimationController anim, Transform canvas,
            int handNumber, float delay)
        {
            GameObject pillGo = null;
            CanvasGroup pillCg = null;

            System.Action cleanup = () => { if (pillGo != null) Object.Destroy(pillGo); };

            new Timeline()
                .AppendInterval(delay)
                .AppendCallback(() =>
                {
                    pillGo = new GameObject("HandFlashPill", typeof(RectTransform));
                    pillGo.transform.SetParent(canvas, false);
                    var pillRt = pillGo.GetComponent<RectTransform>();
                    pillRt.anchorMin = new Vector2(0.5f, 0.5f);
                    pillRt.anchorMax = new Vector2(0.5f, 0.5f);
                    pillRt.pivot = new Vector2(0.5f, 0.5f);
                    pillRt.anchoredPosition = Vector2.zero;
                    pillRt.sizeDelta = new Vector2(300, 50);

                    var pillBg = pillGo.AddComponent<UnityEngine.UI.Image>();
                    pillBg.color = new Color(1f, 1f, 1f, 0.4f);
                    pillBg.sprite = TextureGenerator.GetRoundedRect(300, 50, 25);
                    pillBg.type = UnityEngine.UI.Image.Type.Sliced;
                    pillBg.raycastTarget = false;

                    pillCg = pillGo.AddComponent<CanvasGroup>();
                    pillCg.alpha = 0f;

                    var text = UIFactory.CreateText("HandFlash", pillGo.transform,
                        $"Hand #{handNumber}", 40f, UIFactory.AccentGold,
                        TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold);
                    UIFactory.StretchFill(text.GetComponent<RectTransform>());

                    SparkleEffects.SpawnStarburst(canvas, Vector2.zero,
                        UIFactory.AccentGold, 0.6f, anim);
                })
                .Append(() => Tweener.TweenFloat(0f, 1f, AnimationConfig.HandFlashFadeIn,
                    a => { if (pillCg != null) pillCg.alpha = a; }))
                .Join(() => Tweener.ScalePop(
                    pillGo != null ? pillGo.transform : null,
                    AnimationConfig.HandFlashScalePop, 1.1f))
                .AppendInterval(AnimationConfig.HandFlashHold)
                .Append(() => Tweener.TweenFloat(1f, 0f, AnimationConfig.HandFlashFadeOut,
                    a => { if (pillCg != null) pillCg.alpha = a; }))
                .AppendCallback(cleanup)
                .Play(anim, cleanup);
        }

        private static GameObject CreateCardBack(Transform canvas, Vector2 position, float scale)
        {
            var go = new GameObject("ShuffleCard", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = CardView.CardSize;
            rt.localScale = Vector3.one * scale;

            var bg = UIFactory.CreateImage("Back", go.transform, Color.white);
            bg.sprite = TextureGenerator.GetVerticalGradient(64, 96,
                UIFactory.CardBackOverlay, UIFactory.CardBackLight, 8);
            bg.type = UnityEngine.UI.Image.Type.Sliced;
            UIFactory.StretchFill(bg.GetComponent<RectTransform>());

            var border = UIFactory.CreateImage("Border", bg.transform,
                UIFactory.CardBackBorder);
            border.sprite = TextureGenerator.GetRoundedRect(64, 96, 8);
            border.type = UnityEngine.UI.Image.Type.Sliced;
            var bRt = border.GetComponent<RectTransform>();
            bRt.anchorMin = Vector2.zero;
            bRt.anchorMax = Vector2.one;
            bRt.offsetMin = new Vector2(2, 2);
            bRt.offsetMax = new Vector2(-2, -2);

            return go;
        }
    }
}
