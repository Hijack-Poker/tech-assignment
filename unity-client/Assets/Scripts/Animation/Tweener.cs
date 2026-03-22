using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HijackPoker.Animation
{
    public enum EaseType
    {
        Linear,
        SmoothStep,
        EaseOutBack,
        EaseInOutQuad,
        EaseOutQuart,
        EaseOutElastic,
        EaseOutCirc,
        EaseInOutCubic,
        Spring,
        EaseInCubic,
        EaseOutBounce,
        EaseOutCubic,
    }

    public class TweenHandle
    {
        internal Coroutine Coroutine;

        /// <summary>
        /// Action to run when Cancel() is called — should set the target to its final state.
        /// Publicly settable so views can customize snap behavior for compound animations.
        /// </summary>
        public Action SnapToFinal { get; set; }

        private bool _isComplete;
        private Action _onComplete;

        public bool IsComplete => _isComplete;

        public TweenHandle OnComplete(Action callback)
        {
            _onComplete = callback;
            return this;
        }

        public void Cancel()
        {
            if (_isComplete) return;
            _isComplete = true;
            if (Coroutine != null)
                Tweener.StopTween(Coroutine);
            SnapToFinal?.Invoke();
        }

        internal void MarkComplete()
        {
            if (_isComplete) return;
            _isComplete = true;
            _onComplete?.Invoke();
        }
    }

    /// <summary>
    /// Hosts coroutines for the static Tweener class.
    /// </summary>
    public class TweenRunner : MonoBehaviour { }

    /// <summary>
    /// Static tween utility. Creates coroutine-based animations via a shared TweenRunner.
    /// All methods return cancellable TweenHandles.
    /// </summary>
    public static class Tweener
    {
        /// <summary>
        /// Global speed multiplier for all tween durations. Higher = faster animations.
        /// Synced with AnimationConfig.GlobalSpeed for backward compatibility.
        /// </summary>
        public static float SpeedMultiplier
        {
            get => AnimationConfig.GlobalSpeed;
            set => AnimationConfig.GlobalSpeed = value;
        }

        private static TweenRunner _runner;

        [UnityEngine.RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _runner = null; }

        private static TweenRunner GetRunner()
        {
            if (_runner == null)
            {
                var go = new GameObject("[TweenRunner]");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.hideFlags = HideFlags.HideInHierarchy;
                _runner = go.AddComponent<TweenRunner>();
            }
            return _runner;
        }

        internal static void StopTween(Coroutine coroutine)
        {
            if (_runner != null && coroutine != null)
                _runner.StopCoroutine(coroutine);
        }

        // ── Tween Factories ────────────────────────────────────────────

        public static TweenHandle TweenFloat(float from, float to, float duration,
            Action<float> onUpdate, EaseType ease = EaseType.SmoothStep)
        {
            var handle = new TweenHandle { SnapToFinal = () => onUpdate(to) };
            handle.Coroutine = GetRunner().StartCoroutine(
                FloatRoutine(from, to, duration, onUpdate, ease, handle));
            return handle;
        }

        public static TweenHandle TweenColor(Color from, Color to, float duration,
            Action<Color> onUpdate, EaseType ease = EaseType.SmoothStep)
        {
            var handle = new TweenHandle { SnapToFinal = () => onUpdate(to) };
            handle.Coroutine = GetRunner().StartCoroutine(
                ColorRoutine(from, to, duration, onUpdate, ease, handle));
            return handle;
        }

        /// <summary>
        /// Continuously pulses a float between min and max (e.g. for glow alpha).
        /// Runs forever until cancelled.
        /// </summary>
        public static TweenHandle PulseGlow(Action<float> onUpdate,
            float min, float max, float cycleDuration)
        {
            var handle = new TweenHandle { SnapToFinal = () => onUpdate(0f) };
            handle.Coroutine = GetRunner().StartCoroutine(
                PulseGlowRoutine(onUpdate, min, max, cycleDuration, handle));
            return handle;
        }

        /// <summary>
        /// Scale pop: 0 -> overshoot -> 1 over duration.
        /// </summary>
        public static TweenHandle ScalePop(Transform target, float duration,
            float overshoot = 1.2f)
        {
            var handle = new TweenHandle
            {
                SnapToFinal = () => { if (target != null) target.localScale = Vector3.one; }
            };
            handle.Coroutine = GetRunner().StartCoroutine(
                ScalePopRoutine(target, duration, overshoot, handle));
            return handle;
        }

        /// <summary>
        /// Card flip: scaleX 1 -> 0, invoke midFlipAction (swap content), scaleX 0 -> 1.
        /// </summary>
        public static TweenHandle FlipCard(RectTransform rt, Action midFlipAction,
            float duration = 0.3f)
        {
            var handle = new TweenHandle
            {
                SnapToFinal = () =>
                {
                    midFlipAction();
                    if (rt != null) rt.localScale = Vector3.one;
                }
            };
            handle.Coroutine = GetRunner().StartCoroutine(
                FlipCardRoutine(rt, midFlipAction, duration, handle));
            return handle;
        }

        /// <summary>
        /// 3D perspective card flip: X-squeeze + Y-compression creating a tilt illusion.
        /// EaseOutBack overshoot on expansion for a satisfying pop.
        /// Optional edgeHighlight Image brightens during squeeze phase.
        /// </summary>
        public static TweenHandle FlipCard3D(RectTransform rt, Action midFlipAction,
            float duration = 0.3f, Image edgeHighlight = null)
        {
            var handle = new TweenHandle
            {
                SnapToFinal = () =>
                {
                    midFlipAction();
                    if (rt != null) rt.localScale = Vector3.one;
                    if (edgeHighlight != null)
                    {
                        var c = edgeHighlight.color;
                        edgeHighlight.color = new Color(c.r, c.g, c.b, c.a);
                    }
                }
            };
            handle.Coroutine = GetRunner().StartCoroutine(
                FlipCard3DRoutine(rt, midFlipAction, duration, edgeHighlight, handle));
            return handle;
        }

        /// <summary>
        /// Tweens a RectTransform's anchoredPosition between two points.
        /// </summary>
        public static TweenHandle TweenPosition(RectTransform rt, Vector2 from, Vector2 to,
            float duration, EaseType ease = EaseType.SmoothStep)
        {
            var handle = new TweenHandle
            {
                SnapToFinal = () => { if (rt != null) rt.anchoredPosition = to; }
            };
            handle.Coroutine = GetRunner().StartCoroutine(
                PositionRoutine(rt, from, to, duration, ease, handle));
            return handle;
        }

        /// <summary>
        /// Punch scale: oscillates scale around 1.0 with decaying amplitude.
        /// </summary>
        public static TweenHandle PunchScale(Transform target, float duration,
            float magnitude = 0.15f, int vibrato = 8)
        {
            var handle = new TweenHandle
            {
                SnapToFinal = () => { if (target != null) target.localScale = Vector3.one; }
            };
            handle.Coroutine = GetRunner().StartCoroutine(
                PunchScaleRoutine(target, duration, magnitude, vibrato, handle));
            return handle;
        }

        /// <summary>
        /// Simple delay. SnapToFinal is a no-op by default — override for sequences.
        /// </summary>
        public static TweenHandle Delay(float duration)
        {
            var handle = new TweenHandle { SnapToFinal = () => { } };
            handle.Coroutine = GetRunner().StartCoroutine(
                DelayRoutine(duration, handle));
            return handle;
        }

        /// <summary>
        /// Tweens a CanvasGroup's alpha between two values.
        /// </summary>
        public static TweenHandle TweenAlpha(CanvasGroup cg, float from, float to,
            float duration, EaseType ease = EaseType.SmoothStep)
        {
            var handle = new TweenHandle
            {
                SnapToFinal = () => { if (cg != null) cg.alpha = to; }
            };
            handle.Coroutine = GetRunner().StartCoroutine(
                FloatRoutine(from, to, duration,
                    v => { if (cg != null) cg.alpha = v; }, ease, handle));
            return handle;
        }

        /// <summary>
        /// Tweens a Transform's localScale between two vectors.
        /// </summary>
        public static TweenHandle TweenScale(Transform target, Vector3 from, Vector3 to,
            float duration, EaseType ease = EaseType.SmoothStep)
        {
            var handle = new TweenHandle
            {
                SnapToFinal = () => { if (target != null) target.localScale = to; }
            };
            handle.Coroutine = GetRunner().StartCoroutine(
                ScaleRoutine(target, from, to, duration, ease, handle));
            return handle;
        }

        /// <summary>
        /// Tweens a RectTransform's Z-axis rotation between two angles.
        /// </summary>
        public static TweenHandle TweenRotation(RectTransform rt, float fromAngle, float toAngle,
            float duration, EaseType ease = EaseType.SmoothStep)
        {
            var handle = new TweenHandle
            {
                SnapToFinal = () => { if (rt != null) rt.localEulerAngles = new Vector3(0, 0, toAngle); }
            };
            handle.Coroutine = GetRunner().StartCoroutine(
                FloatRoutine(fromAngle, toAngle, duration,
                    v => { if (rt != null) rt.localEulerAngles = new Vector3(0, 0, v); },
                    ease, handle));
            return handle;
        }

        // ── Coroutines ─────────────────────────────────────────────────

        private static IEnumerator FloatRoutine(float from, float to, float duration,
            Action<float> onUpdate, EaseType ease, TweenHandle handle)
        {
            duration = AdjustDuration(duration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                onUpdate(Mathf.LerpUnclamped(from, to, ApplyEase(t, ease)));
                yield return null;
            }
            onUpdate(to);
            handle.MarkComplete();
        }

        private static IEnumerator ColorRoutine(Color from, Color to, float duration,
            Action<Color> onUpdate, EaseType ease, TweenHandle handle)
        {
            duration = AdjustDuration(duration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                onUpdate(Color.LerpUnclamped(from, to, ApplyEase(t, ease)));
                yield return null;
            }
            onUpdate(to);
            handle.MarkComplete();
        }

        private static IEnumerator PulseGlowRoutine(Action<float> onUpdate,
            float min, float max, float cycleDuration, TweenHandle handle)
        {
            float t = 0f;
            while (!handle.IsComplete)
            {
                t += Time.deltaTime / cycleDuration;
                t %= 1f; // prevent precision loss from unbounded growth
                float alpha = Mathf.Lerp(min, max, (Mathf.Sin(t * Mathf.PI * 2f) + 1f) / 2f);
                onUpdate(alpha);
                yield return null;
            }
        }

        private static IEnumerator ScalePopRoutine(Transform target, float duration,
            float overshoot, TweenHandle handle)
        {
            if (target == null) { handle.MarkComplete(); yield break; }

            duration = AdjustDuration(duration);
            target.localScale = Vector3.zero;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (target == null) { handle.MarkComplete(); yield break; }
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float s;
                if (t < 0.6f)
                    s = Mathf.Lerp(0f, overshoot, t / 0.6f);
                else
                    s = Mathf.Lerp(overshoot, 1f, (t - 0.6f) / 0.4f);

                target.localScale = Vector3.one * s;
                yield return null;
            }

            if (target != null) target.localScale = Vector3.one;
            handle.MarkComplete();
        }

        private static IEnumerator PunchScaleRoutine(Transform target, float duration,
            float magnitude, int vibrato, TweenHandle handle)
        {
            if (target == null) { handle.MarkComplete(); yield break; }

            duration = AdjustDuration(duration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (target == null) { handle.MarkComplete(); yield break; }
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = 1f + magnitude * (1f - t) * Mathf.Sin(vibrato * Mathf.PI * t);
                target.localScale = Vector3.one * scale;
                yield return null;
            }

            if (target != null) target.localScale = Vector3.one;
            handle.MarkComplete();
        }

        private static IEnumerator FlipCardRoutine(RectTransform rt, Action midFlipAction,
            float duration, TweenHandle handle)
        {
            if (rt == null) { handle.MarkComplete(); yield break; }

            duration = AdjustDuration(duration);
            float half = duration / 2f;
            float elapsed = 0f;

            // Squeeze scaleX to 0 (with SmoothStep easing)
            while (elapsed < half)
            {
                if (rt == null) { handle.MarkComplete(); yield break; }
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                float eased = ApplyEase(t, EaseType.SmoothStep);
                rt.localScale = new Vector3(1f - eased, 1f, 1f);
                yield return null;
            }

            // Swap content at midpoint
            midFlipAction();

            // Expand scaleX back to 1 (with SmoothStep easing)
            elapsed = 0f;
            while (elapsed < half)
            {
                if (rt == null) { handle.MarkComplete(); yield break; }
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                float eased = ApplyEase(t, EaseType.SmoothStep);
                rt.localScale = new Vector3(eased, 1f, 1f);
                yield return null;
            }

            if (rt != null) rt.localScale = Vector3.one;
            handle.MarkComplete();
        }

        private static IEnumerator FlipCard3DRoutine(RectTransform rt, Action midFlipAction,
            float duration, Image edgeHighlight, TweenHandle handle)
        {
            if (rt == null) { handle.MarkComplete(); yield break; }

            duration = AdjustDuration(duration);
            float half = duration / 2f;
            float elapsed = 0f;
            float baseEdgeAlpha = edgeHighlight != null ? edgeHighlight.color.a : 0f;

            // Squeeze phase: scaleX 1->0, scaleY 1->0.85
            while (elapsed < half)
            {
                if (rt == null) { handle.MarkComplete(); yield break; }
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                float eased = ApplyEase(t, EaseType.SmoothStep);
                float scaleX = 1f - eased;
                float scaleY = Mathf.Lerp(1f, 0.85f, eased);
                rt.localScale = new Vector3(scaleX, scaleY, 1f);

                if (edgeHighlight != null)
                {
                    var c = edgeHighlight.color;
                    edgeHighlight.color = new Color(c.r, c.g, c.b,
                        Mathf.Lerp(baseEdgeAlpha, 0.4f, eased));
                }
                yield return null;
            }

            // Swap content at midpoint
            midFlipAction();

            // Expansion phase: scaleX 0->1 with EaseOutBack, scaleY 0.85->1.0
            elapsed = 0f;
            while (elapsed < half)
            {
                if (rt == null) { handle.MarkComplete(); yield break; }
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                float scaleX = ApplyEase(t, EaseType.EaseOutBack);
                float scaleY = Mathf.Lerp(0.85f, 1f, ApplyEase(t, EaseType.SmoothStep));
                rt.localScale = new Vector3(scaleX, scaleY, 1f);

                if (edgeHighlight != null)
                {
                    var c = edgeHighlight.color;
                    edgeHighlight.color = new Color(c.r, c.g, c.b,
                        Mathf.Lerp(0.4f, baseEdgeAlpha, t));
                }
                yield return null;
            }

            if (rt != null) rt.localScale = Vector3.one;
            if (edgeHighlight != null)
            {
                var c2 = edgeHighlight.color;
                edgeHighlight.color = new Color(c2.r, c2.g, c2.b, baseEdgeAlpha);
            }
            handle.MarkComplete();
        }

        private static IEnumerator PositionRoutine(RectTransform rt, Vector2 from, Vector2 to,
            float duration, EaseType ease, TweenHandle handle)
        {
            if (rt == null) { handle.MarkComplete(); yield break; }

            duration = AdjustDuration(duration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (rt == null) { handle.MarkComplete(); yield break; }
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rt.anchoredPosition = Vector2.LerpUnclamped(from, to, ApplyEase(t, ease));
                yield return null;
            }

            if (rt != null) rt.anchoredPosition = to;
            handle.MarkComplete();
        }

        private static IEnumerator ScaleRoutine(Transform target, Vector3 from, Vector3 to,
            float duration, EaseType ease, TweenHandle handle)
        {
            if (target == null) { handle.MarkComplete(); yield break; }

            duration = AdjustDuration(duration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null) { handle.MarkComplete(); yield break; }
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.localScale = Vector3.LerpUnclamped(from, to, ApplyEase(t, ease));
                yield return null;
            }

            if (target != null) target.localScale = to;
            handle.MarkComplete();
        }

        private static IEnumerator DelayRoutine(float duration, TweenHandle handle)
        {
            yield return new WaitForSeconds(AdjustDuration(duration));
            handle.MarkComplete();
        }

        private static float AdjustDuration(float duration)
        {
            return AnimationConfig.Scale(duration);
        }

        // ── Easing ─────────────────────────────────────────────────────

        private static float ApplyEase(float t, EaseType ease)
        {
            switch (ease)
            {
                case EaseType.Linear:
                    return t;
                case EaseType.SmoothStep:
                    return t * t * (3f - 2f * t);
                case EaseType.EaseOutBack:
                    const float c = 1.70158f;
                    float t1 = t - 1f;
                    return 1f + (c + 1f) * t1 * t1 * t1 + c * t1 * t1;
                case EaseType.EaseInOutQuad:
                    return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                case EaseType.EaseOutQuart:
                    return 1f - Mathf.Pow(1f - t, 4f);
                case EaseType.EaseOutElastic:
                {
                    if (t <= 0f) return 0f;
                    if (t >= 1f) return 1f;
                    float p = 0.3f;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - p / 4f) * (2f * Mathf.PI) / p) + 1f;
                }
                case EaseType.EaseOutCirc:
                    return Mathf.Sqrt(1f - Mathf.Pow(t - 1f, 2f));
                case EaseType.EaseInOutCubic:
                    return t < 0.5f
                        ? 4f * t * t * t
                        : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
                case EaseType.Spring:
                {
                    // Damped spring oscillation
                    float s = Mathf.Sin(t * Mathf.PI * (0.2f + 2.5f * t * t * t));
                    float p2 = Mathf.Pow(1f - t, 2.2f);
                    return t + (s * p2 * 0.3f);
                }
                case EaseType.EaseInCubic:
                    return t * t * t;
                case EaseType.EaseOutBounce:
                {
                    if (t < 1f / 2.75f)
                        return 7.5625f * t * t;
                    if (t < 2f / 2.75f)
                    {
                        float tb = t - 1.5f / 2.75f;
                        return 7.5625f * tb * tb + 0.75f;
                    }
                    if (t < 2.5f / 2.75f)
                    {
                        float tb = t - 2.25f / 2.75f;
                        return 7.5625f * tb * tb + 0.9375f;
                    }
                    {
                        float tb = t - 2.625f / 2.75f;
                        return 7.5625f * tb * tb + 0.984375f;
                    }
                }
                case EaseType.EaseOutCubic:
                    return 1f - Mathf.Pow(1f - t, 3f);
                default:
                    return t;
            }
        }
    }
}
