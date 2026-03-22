using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.UI;

namespace HijackPoker.Animation
{
    /// <summary>
    /// All-in impact: 3 concentric ring pulses + screen shake + "ALL IN" text stamp.
    /// Uses Timeline for flat sequencing of staggered ring delays.
    /// </summary>
    public static class AllInImpactEffect
    {
        public static void Play(AnimationController anim, Transform canvas,
            RectTransform canvasRoot, Vector2 playerPos)
        {
            if (anim == null || canvas == null) return;

            Color magenta = new Color(0.94f, 0.27f, 0.27f, 0.7f);

            // 3 concentric ring pulses via flat Timeline (replaces 2 nested Delay→OnComplete chains)
            new Timeline()
                .AppendCallback(() =>
                    SparkleEffects.SpawnRingPulse(canvas, playerPos, magenta, 120f, 0.5f, anim))
                .AppendInterval(AnimationConfig.AllInRingStagger1)
                .AppendCallback(() =>
                    SparkleEffects.SpawnRingPulse(canvas, playerPos,
                        new Color(magenta.r, magenta.g, magenta.b, 0.5f), 150f, 0.45f, anim))
                .AppendInterval(AnimationConfig.AllInRingStagger2 - AnimationConfig.AllInRingStagger1)
                .AppendCallback(() =>
                {
                    SparkleEffects.SpawnRingPulse(canvas, playerPos,
                        new Color(magenta.r, magenta.g, magenta.b, 0.35f), 180f, 0.4f, anim);
                    SparkleEffects.SpawnSparkles(canvas, playerPos, 12,
                        new Color(magenta.r, magenta.g, magenta.b, 0.8f), 60f, 0.6f, anim);
                    SparkleEffects.SpawnStarburst(canvas, playerPos, magenta, 0.7f, anim);
                })
                .Play(anim);

            // Screen shake
            if (canvasRoot != null)
                ScreenShakeEffect.Play(anim, canvasRoot, 6f, 0.25f);

            // "ALL IN" text stamp at center
            var stampGo = new GameObject("AllInStamp", typeof(RectTransform));
            stampGo.transform.SetParent(canvas, false);
            var stampRt = stampGo.GetComponent<RectTransform>();
            stampRt.anchorMin = new Vector2(0.5f, 0.5f);
            stampRt.anchorMax = new Vector2(0.5f, 0.5f);
            stampRt.pivot = new Vector2(0.5f, 0.5f);
            stampRt.anchoredPosition = playerPos + new Vector2(0, 30f);
            stampRt.sizeDelta = new Vector2(120, 40);
            stampRt.localScale = Vector3.zero;

            var stampText = stampGo.AddComponent<TextMeshProUGUI>();
            stampText.text = "ALL IN";
            stampText.fontSize = 24;
            stampText.color = Color.white;
            stampText.alignment = TextAlignmentOptions.Center;
            stampText.fontStyle = FontStyles.Bold;
            stampText.raycastTarget = false;

            var stampCg = stampGo.AddComponent<CanvasGroup>();

            System.Action cleanup = () => { if (stampGo != null) Object.Destroy(stampGo); };

            // Stamp animation via Timeline (replaces hold Delay→OnComplete→fadeOut chain)
            new Timeline()
                .Append(() => Tweener.ScalePop(stampGo.transform, AnimationConfig.AllInStampPop, 1.2f))
                .Join(() => Tweener.Delay(AnimationConfig.AllInStampHold))
                .Append(() => Tweener.TweenFloat(1f, 0f, AnimationConfig.AllInStampFade,
                    a => { if (stampCg != null) stampCg.alpha = a; }))
                .AppendCallback(cleanup)
                .Play(anim, cleanup);
        }
    }
}
