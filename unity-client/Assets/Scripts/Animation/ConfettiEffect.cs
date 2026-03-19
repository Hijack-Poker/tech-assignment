using UnityEngine;
using HijackPoker.UI;

namespace HijackPoker.Animation
{
    /// <summary>
    /// Static utility for spawning confetti particles — colored rectangles that
    /// rise briefly, fall with gravity, rotate, and fade out.
    /// Uses ParticlePool (same pattern as SparkleEffects).
    /// </summary>
    public static class ConfettiEffect
    {
        private static readonly Color[] Palette =
        {
            UIFactory.HexColor("#E74C3C"), // red
            UIFactory.HexColor("#F5A623"), // gold
            UIFactory.HexColor("#2ECC71"), // green
            UIFactory.HexColor("#3498DB"), // blue
            UIFactory.HexColor("#E056A0"), // magenta
            UIFactory.HexColor("#F0E6D3"), // white
        };

        public static void SpawnConfetti(Transform canvas, Vector2 origin, int count,
            float duration, AnimationController anim)
        {
            if (canvas == null || anim == null) return;

            for (int i = 0; i < count; i++)
            {
                var color = Palette[Random.Range(0, Palette.Length)];
                float width = Random.Range(6f, 12f);
                float height = Random.Range(4f, 8f);
                float startAngle = Random.Range(0f, 360f);
                float xOffset = Random.Range(-80f, 80f);
                float delay = Random.Range(0f, duration * 0.15f);

                // Physics parameters
                float riseY = Random.Range(-30f, 0f);
                float fallY = Random.Range(200f, 350f);
                float driftX = Random.Range(-30f, 30f);
                float rotSpeed = Random.Range(90f, 360f);
                if (Random.value > 0.5f) rotSpeed = -rotSpeed;

                var go = ParticlePool.Instance.Rent(canvas, origin + new Vector2(xOffset, 0),
                    width, color);
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(width, height);
                rt.localEulerAngles = new Vector3(0, 0, startAngle);
                var cg = go.GetComponent<CanvasGroup>();
                cg.alpha = 1f;

                bool returned = false;
                System.Action cleanup = () =>
                {
                    if (returned || go == null) return;
                    returned = true;
                    ParticlePool.Instance.Return(go);
                };

                // Capture for closure
                float capturedRiseY = riseY;
                float capturedFallY = fallY;
                float capturedDriftX = driftX;
                float capturedRotSpeed = rotSpeed;
                float capturedDelay = delay;
                Vector2 startPos = origin + new Vector2(xOffset, 0);
                float particleDur = duration - delay;

                System.Action doConfetti = () =>
                {
                    if (go == null || returned) return;

                    float riseDur = 0.15f;
                    float fallDur = particleDur - riseDur;

                    // Phase 1: brief rise
                    Vector2 riseTarget = startPos + new Vector2(capturedDriftX * 0.1f, capturedRiseY);
                    var riseH = anim.Play(Tweener.TweenPosition(rt, startPos, riseTarget,
                        riseDur, EaseType.EaseOutQuart));
                    riseH.SnapToFinal = cleanup;
                    riseH.OnComplete(() =>
                    {
                        if (go == null || returned) return;

                        // Phase 2: gravity fall
                        Vector2 fallTarget = riseTarget + new Vector2(capturedDriftX, capturedFallY);
                        var fallH = anim.Play(Tweener.TweenPosition(rt, riseTarget, fallTarget,
                            fallDur, EaseType.EaseInCubic));
                        fallH.SnapToFinal = cleanup;
                        fallH.OnComplete(cleanup);
                    });

                    // Continuous rotation via TweenFloat
                    float totalRotation = capturedRotSpeed * particleDur;
                    var rotH = anim.Play(Tweener.TweenFloat(startAngle,
                        startAngle + totalRotation, particleDur,
                        angle => { if (rt != null) rt.localEulerAngles = new Vector3(0, 0, angle); },
                        EaseType.Linear));
                    rotH.SnapToFinal = cleanup;

                    // Alpha: full opacity, then fade starting at 75% duration
                    float fadeDelay = particleDur * 0.75f;
                    float fadeDur = particleDur * 0.25f;
                    var fadeDelayH = anim.Play(Tweener.Delay(fadeDelay));
                    fadeDelayH.SnapToFinal = cleanup;
                    fadeDelayH.OnComplete(() =>
                    {
                        if (go == null || returned) return;
                        var fadeH = anim.Play(Tweener.TweenFloat(1f, 0f, fadeDur,
                            a => { if (cg != null) cg.alpha = a; }));
                        fadeH.SnapToFinal = cleanup;
                    });
                };

                if (delay > 0.01f)
                {
                    var dh = anim.Play(Tweener.Delay(delay));
                    dh.SnapToFinal = cleanup;
                    dh.OnComplete(doConfetti);
                }
                else
                {
                    doConfetti();
                }
            }
        }
    }
}
