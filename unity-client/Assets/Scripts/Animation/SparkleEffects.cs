using UnityEngine;
using UnityEngine.UI;
using HijackPoker.UI;

namespace HijackPoker.Animation
{
    /// <summary>
    /// Static utility for creating sparkle/shimmer/glint/starburst effects using
    /// small animated UI elements. All effects are self-cleaning (auto-destroy on completion).
    /// </summary>
    public static class SparkleEffects
    {
        /// <summary>
        /// Creates N tiny diamond/star shapes that burst outward from a point,
        /// twinkle (pulse alpha), and fade. Used when something "lands" or "appears."
        /// </summary>
        public static void SpawnSparkles(Transform canvas, Vector2 position, int count,
            Color color, float radius, float duration, AnimationController anim)
        {
            if (canvas == null || anim == null) return;

            for (int i = 0; i < count; i++)
            {
                float angle = (360f / count) * i + Random.Range(-20f, 20f);
                float dist = Random.Range(radius * 0.4f, radius);
                float size = Random.Range(4f, 9f);
                float delay = Random.Range(0f, duration * 0.35f);

                Vector2 dir = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad));
                Vector2 target = position + dir * dist;

                var go = ParticlePool.Instance.Rent(canvas, position, size, color);
                var rt = go.GetComponent<RectTransform>();
                var cg = go.GetComponent<CanvasGroup>();
                cg.alpha = 0f;

                bool returned = false;
                System.Action cleanup = () =>
                {
                    if (returned || go == null) return;
                    returned = true;
                    ParticlePool.Instance.Return(go);
                };
                float sparkDur = duration - delay;

                System.Action doSparkle = () =>
                {
                    if (go == null || returned) return;

                    // Move outward
                    var moveH = anim.Play(Tweener.TweenPosition(rt, position, target,
                        sparkDur * 0.6f, EaseType.EaseOutQuart));
                    moveH.SnapToFinal = cleanup;

                    // Fade in then out
                    anim.Play(Tweener.TweenFloat(0f, 1f, sparkDur * 0.15f,
                        a => { if (cg != null) cg.alpha = a; }));
                    var fadeOut = anim.Play(Tweener.TweenFloat(1f, 0f, sparkDur * 0.8f,
                        a => { if (cg != null) cg.alpha = a; }));
                    fadeOut.SnapToFinal = cleanup;
                    fadeOut.OnComplete(cleanup);

                    // Scale pulse
                    anim.Play(Tweener.TweenScale(go.transform,
                        Vector3.one * 0.3f, Vector3.one * 1.2f,
                        sparkDur * 0.5f, EaseType.EaseOutElastic));
                };

                if (delay > 0.01f)
                {
                    var dh = anim.Play(Tweener.Delay(delay));
                    dh.SnapToFinal = cleanup;
                    dh.OnComplete(doSparkle);
                }
                else
                {
                    doSparkle();
                }
            }
        }

        /// <summary>
        /// Creates small dots along a path that twinkle in sequence (staggered fade-in/out),
        /// leaving a sparkle trail. Used for flying cards and pot distribution.
        /// </summary>
        public static void SpawnShimmerTrail(Transform canvas, Vector2 fromPos, Vector2 toPos,
            int count, Color color, float duration, AnimationController anim)
        {
            if (canvas == null || anim == null) return;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1);
                Vector2 pos = Vector2.Lerp(fromPos, toPos, t);
                // Add slight perpendicular offset for organic feel
                Vector2 dir = (toPos - fromPos).normalized;
                Vector2 perp = new Vector2(-dir.y, dir.x);
                pos += perp * Random.Range(-10f, 10f);

                float size = Random.Range(3f, 5f);
                float delay = t * duration * 0.7f;

                var go = ParticlePool.Instance.Rent(canvas, pos, size, color);
                var cg = go.GetComponent<CanvasGroup>();
                cg.alpha = 0f;

                bool returned = false;
                System.Action cleanup = () =>
                {
                    if (returned || go == null) return;
                    returned = true;
                    ParticlePool.Instance.Return(go);
                };
                float sparkDur = duration * 0.55f;

                System.Action doSparkle = () =>
                {
                    if (go == null || returned) return;
                    var fadeIn = anim.Play(Tweener.TweenFloat(0f, 1f, sparkDur * 0.3f,
                        a => { if (cg != null) cg.alpha = a; }));
                    fadeIn.SnapToFinal = cleanup;
                    fadeIn.OnComplete(() =>
                    {
                        var fadeOut = anim.Play(Tweener.TweenFloat(1f, 0f, sparkDur * 0.7f,
                            a => { if (cg != null) cg.alpha = a; }));
                        fadeOut.SnapToFinal = cleanup;
                        fadeOut.OnComplete(cleanup);
                    });
                };

                if (delay > 0.01f)
                {
                    var dh = anim.Play(Tweener.Delay(delay));
                    dh.SnapToFinal = cleanup;
                    dh.OnComplete(doSparkle);
                }
                else
                {
                    doSparkle();
                }
            }
        }

        /// <summary>
        /// Glint sweep — pruned as part of effect reduction. No-op to preserve caller compatibility.
        /// </summary>
        public static void SpawnGlintSweep(RectTransform target, Color color,
            float duration, AnimationController anim)
        {
            // Pruned: glint sweep removed for cleaner visual language
        }

        /// <summary>
        /// Starburst — pruned as part of effect reduction. No-op to preserve caller compatibility.
        /// </summary>
        public static void SpawnStarburst(Transform canvas, Vector2 position, Color color,
            float duration, AnimationController anim)
        {
            // Pruned: starburst removed for cleaner visual language
        }

        /// <summary>
        /// An expanding ring that grows outward while fading (like a ripple).
        /// Used for active player turn start, all-in declaration.
        /// </summary>
        public static void SpawnRingPulse(Transform canvas, Vector2 position, Color color,
            float diameter, float duration, AnimationController anim)
        {
            if (canvas == null || anim == null) return;

            var go = new GameObject("RingPulse", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(diameter, diameter);
            rt.localScale = Vector3.one * 0.3f;

            var img = go.AddComponent<Image>();
            img.sprite = TextureGenerator.GetRing((int)diameter, 8);
            img.color = color;
            img.raycastTarget = false;

            // Radial glow fill inside ring
            var glowChild = new GameObject("RingGlow", typeof(RectTransform));
            glowChild.transform.SetParent(go.transform, false);
            var glowRt = glowChild.GetComponent<RectTransform>();
            glowRt.anchoredPosition = Vector2.zero;
            glowRt.sizeDelta = new Vector2(diameter * 0.9f, diameter * 0.9f);
            var glowImg = glowChild.AddComponent<Image>();
            glowImg.sprite = TextureGenerator.GetRadialGradient(64,
                new Color(color.r, color.g, color.b, 0.3f), new Color(color.r, color.g, color.b, 0f));
            glowImg.raycastTarget = false;

            var cg = go.AddComponent<CanvasGroup>();

            bool destroyed = false;
            System.Action cleanup = () =>
            {
                if (destroyed || go == null) return;
                destroyed = true;
                Object.Destroy(go);
            };

            // Expand
            var scaleH = anim.Play(Tweener.TweenScale(go.transform,
                Vector3.one * 0.3f, Vector3.one * 1.8f,
                duration, EaseType.EaseOutQuart));
            scaleH.SnapToFinal = cleanup;

            // Fade out
            var fadeH = anim.Play(Tweener.TweenFloat(1.0f, 0f, duration,
                a => { if (cg != null) cg.alpha = a; },
                EaseType.EaseOutQuart));
            fadeH.SnapToFinal = cleanup;
            fadeH.OnComplete(cleanup);
        }

        /// <summary>
        /// Gold sparkle shower for winner celebration. Multiple sparkles falling
        /// and twinkling around a position.
        /// </summary>
        public static void SpawnGoldShower(Transform canvas, Vector2 position, int count,
            float duration, AnimationController anim)
        {
            SpawnSparkles(canvas, position, count,
                new Color(0.96f, 0.62f, 0.04f, 1.0f), 100f, duration, anim);
        }

        /// <summary>
        /// Card reveal burst: white radial flash + ring pulse + sparkle jets.
        /// Simplified from 3 staggered effects to a clean flash + sparkles.
        /// </summary>
        public static void SpawnCardRevealBurst(Transform canvas, Vector2 position,
            AnimationController anim)
        {
            if (canvas == null || anim == null) return;

            // White radial flash (scale 0→1, fade)
            var flashGo = new GameObject("RevealFlash", typeof(RectTransform));
            flashGo.transform.SetParent(canvas, false);
            var flashRt = flashGo.GetComponent<RectTransform>();
            flashRt.anchorMin = new Vector2(0.5f, 0.5f);
            flashRt.anchorMax = new Vector2(0.5f, 0.5f);
            flashRt.pivot = new Vector2(0.5f, 0.5f);
            flashRt.anchoredPosition = position;
            flashRt.sizeDelta = new Vector2(120, 120);
            flashRt.localScale = Vector3.zero;

            var flashImg = flashGo.AddComponent<Image>();
            flashImg.sprite = TextureGenerator.GetRadialGradient(64,
                new Color(1, 1, 1, 0.85f), new Color(1, 1, 1, 0f));
            flashImg.raycastTarget = false;

            var flashCg = flashGo.AddComponent<CanvasGroup>();
            bool flashDestroyed = false;
            System.Action flashCleanup = () =>
            {
                if (flashDestroyed || flashGo == null) return;
                flashDestroyed = true;
                Object.Destroy(flashGo);
            };

            anim.Play(Tweener.TweenScale(flashGo.transform,
                Vector3.zero, Vector3.one * 1.8f, AnimationConfig.RevealBurstScale, EaseType.EaseOutQuart));
            var flashFade = anim.Play(Tweener.TweenFloat(1f, 0f, AnimationConfig.RevealBurstFade,
                a => { if (flashCg != null) flashCg.alpha = a; }, EaseType.EaseOutQuart));
            flashFade.SnapToFinal = flashCleanup;
            flashFade.OnComplete(flashCleanup);

            // Single ring pulse
            SpawnRingPulse(canvas, position, new Color(1, 1, 1, 0.7f), 100f, 0.4f, anim);

            // 8 sparkle jets (reduced from 12)
            SpawnSparkles(canvas, position, 8,
                new Color(1, 0.95f, 0.8f, 0.8f), 60f, 0.5f, anim);
        }

        // ── Helpers ─────────────────────────────────────────────────

        private static GameObject CreateSparkleElement(Transform canvas, Vector2 position,
            float size, Color color)
        {
            var go = new GameObject("Sparkle", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(size, size);
            // Rotate 45° for diamond shape
            rt.localEulerAngles = new Vector3(0, 0, 45);

            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            return go;
        }
    }
}
