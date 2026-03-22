using UnityEngine;

namespace HijackPoker.Animation
{
    /// <summary>
    /// Applies a decaying sinusoidal screen shake to the canvas root.
    /// Additive — stores startPos, never uses TweenPosition.
    /// </summary>
    public static class ScreenShakeEffect
    {
        public static TweenHandle Play(AnimationController anim, RectTransform canvasRoot,
            float intensity = 8f, float duration = 0.3f)
        {
            if (anim == null || canvasRoot == null) return null;

            Vector2 startPos = canvasRoot.anchoredPosition;

            var handle = anim.Play(Tweener.TweenFloat(0f, 1f, duration,
                t =>
                {
                    if (canvasRoot == null) return;
                    float decay = 1f - t;
                    float offsetX = Mathf.Sin(t * 30f) * intensity * decay;
                    float offsetY = Mathf.Cos(t * 25f) * intensity * decay * 0.7f;
                    canvasRoot.anchoredPosition = startPos + new Vector2(offsetX, offsetY);
                }, EaseType.Linear));

            handle.SnapToFinal = () =>
            {
                if (canvasRoot != null) canvasRoot.anchoredPosition = startPos;
            };
            handle.OnComplete(() =>
            {
                if (canvasRoot != null) canvasRoot.anchoredPosition = startPos;
            });

            return handle;
        }
    }
}
