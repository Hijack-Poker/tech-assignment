using UnityEngine;
using UnityEngine.UI;
using HijackPoker.UI;

namespace HijackPoker.Animation
{
    /// <summary>
    /// Tiered card reveal effects based on hand strength.
    /// Tier 0-1: GlintSweep, Tier 2: +RingPulse, Tier 3: +Starburst, Tier 4: screen flash + explosions.
    /// </summary>
    public static class CardRevealEffects
    {
        public static void PlayReveal(AnimationController anim, Transform canvas,
            CardView card, int tier)
        {
            if (anim == null || card == null) return;

            var cardRt = card.RectTransform;

            // All tiers get a glint sweep
            SparkleEffects.SpawnGlintSweep(cardRt, Color.white, AnimationConfig.RevealGlintSweep, anim);

            if (tier < 2) return;

            // Tier 2+: Ring pulse + sparkles
            if (canvas != null)
            {
                Vector2 pos = GetCardCanvasPos(card, canvas);
                SparkleEffects.SpawnRingPulse(canvas, pos,
                    new Color(1f, 0.9f, 0.6f, 0.7f), 100f, 0.7f, anim);
                SparkleEffects.SpawnSparkles(canvas, pos, 6,
                    new Color(1f, 0.9f, 0.6f, 0.8f), 40f, 0.6f, anim);
            }

            if (tier < 3) return;

            // Tier 3+: Starburst + ring pulse underneath
            if (canvas != null)
            {
                Vector2 pos = GetCardCanvasPos(card, canvas);
                SparkleEffects.SpawnRingPulse(canvas, pos,
                    new Color(1f, 0.85f, 0.4f, 0.5f), 80f, 0.8f, anim);
                SparkleEffects.SpawnStarburst(canvas, pos,
                    UIFactory.AccentGold, 0.9f, anim);
            }

            if (tier < 4) return;

            // Tier 4: White screen flash + multiple starbursts + sparkle explosion + CardRevealBurst
            if (canvas != null)
            {
                Vector2 pos = GetCardCanvasPos(card, canvas);

                // White flash overlay
                var flashGo = new GameObject("ScreenFlash", typeof(RectTransform));
                flashGo.transform.SetParent(canvas, false);
                var flashRt = flashGo.GetComponent<RectTransform>();
                flashRt.anchorMin = Vector2.zero;
                flashRt.anchorMax = Vector2.one;
                flashRt.offsetMin = Vector2.zero;
                flashRt.offsetMax = Vector2.zero;

                var flashImg = flashGo.AddComponent<Image>();
                flashImg.color = new Color(1, 1, 1, 0.7f);
                flashImg.raycastTarget = false;

                var flashCg = flashGo.AddComponent<CanvasGroup>();
                System.Action flashCleanup = () => { if (flashGo != null) Object.Destroy(flashGo); };

                var fadeH = anim.Play(Tweener.TweenFloat(0.7f, 0f, AnimationConfig.RevealScreenFlashFade,
                    a => { if (flashCg != null) flashCg.alpha = a; }));
                fadeH.SnapToFinal = flashCleanup;
                fadeH.OnComplete(flashCleanup);

                // Multiple starbursts (wider offsets + 3rd starburst)
                SparkleEffects.SpawnStarburst(canvas, pos + new Vector2(-30, 10),
                    new Color(1f, 0.85f, 0.3f, 0.8f), 0.7f, anim);
                SparkleEffects.SpawnStarburst(canvas, pos + new Vector2(30, -10),
                    new Color(1f, 0.7f, 0.2f, 0.7f), 0.6f, anim);
                SparkleEffects.SpawnStarburst(canvas, pos + new Vector2(0, 25),
                    new Color(1f, 0.8f, 0.25f, 0.75f), 0.65f, anim);

                // Sparkle explosion
                SparkleEffects.SpawnSparkles(canvas, pos, 35,
                    new Color(1f, 0.9f, 0.5f, 0.9f), 120f, 1.8f, anim);

                // CardRevealBurst + second ring pulse
                SparkleEffects.SpawnCardRevealBurst(canvas, pos, anim);
                var ringDelay = anim.Play(Tweener.Delay(AnimationConfig.RevealRingDelayTier4));
                ringDelay.OnComplete(() =>
                    SparkleEffects.SpawnRingPulse(canvas, pos,
                        new Color(1f, 0.85f, 0.4f, 0.6f), 90f, 0.6f, anim));
            }
        }

        private static Vector2 GetCardCanvasPos(CardView card, Transform canvas)
        {
            var canvasRt = canvas.GetComponent<RectTransform>();
            if (canvasRt == null || card.RectTransform == null) return Vector2.zero;

            Vector3 worldPos = card.RectTransform.position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRt, RectTransformUtility.WorldToScreenPoint(null, worldPos),
                null, out Vector2 localPoint);
            return localPoint;
        }
    }
}
