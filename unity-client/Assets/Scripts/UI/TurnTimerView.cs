using UnityEngine;
using UnityEngine.UI;
using HijackPoker.Animation;
using HijackPoker.Managers;

namespace HijackPoker.UI
{
    /// <summary>
    /// Countdown arc ring around the active player's avatar.
    /// Purely cosmetic — backend controls actual timing.
    /// Color transitions: cyan -> yellow (50%) -> magenta (25%).
    /// </summary>
    public class TurnTimerView : MonoBehaviour
    {
        private Image _ringImage;
        private RectTransform _rt;
        private TweenHandle _timerTween;
        private TweenHandle _tickTween;
        private float _fillAmount = 1f;
        private bool _isRunning;

        public bool IsRunning => _isRunning;

        public static TurnTimerView Create(Transform parent, float diameter)
        {
            var go = new GameObject("TurnTimer", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            UIFactory.SetAnchor(rt, 0.5f, 0.5f);
            rt.sizeDelta = new Vector2(diameter, diameter);

            var view = go.AddComponent<TurnTimerView>();
            view._rt = rt;

            // Ring image with filled radial
            view._ringImage = go.AddComponent<Image>();
            view._ringImage.sprite = TextureGenerator.GetRing((int)diameter, (int)(diameter * 0.08f));
            view._ringImage.type = Image.Type.Filled;
            view._ringImage.fillMethod = Image.FillMethod.Radial360;
            view._ringImage.fillOrigin = (int)Image.Origin360.Top;
            view._ringImage.fillClockwise = false;
            view._ringImage.fillAmount = 1f;
            view._ringImage.color = GetTimerColor(1f);
            view._ringImage.raycastTarget = false;

            go.SetActive(false);
            return view;
        }

        public void StartTimer(float duration, AnimationController anim)
        {
            if (anim == null) return;

            StopTimer();
            _isRunning = true;
            _fillAmount = 1f;
            gameObject.SetActive(true);
            _ringImage.fillAmount = 1f;
            _ringImage.color = GetTimerColor(1f);

            _timerTween = anim.Play(Tweener.TweenFloat(1f, 0f, duration, fill =>
            {
                _fillAmount = fill;
                if (_ringImage != null)
                {
                    _ringImage.fillAmount = fill;
                    _ringImage.color = GetTimerColor(fill);
                }
            }, EaseType.Linear));

            _timerTween.OnComplete(() =>
            {
                _isRunning = false;
                if (gameObject != null) gameObject.SetActive(false);
            });

            // Tick sounds when < 25% remaining
            StartTickSounds(duration, anim);
        }

        public void StopTimer()
        {
            _timerTween?.Cancel();
            _timerTween = null;
            _tickTween?.Cancel();
            _tickTween = null;
            _isRunning = false;
            if (gameObject != null) gameObject.SetActive(false);
        }

        private void StartTickSounds(float duration, AnimationController anim)
        {
            float tickStartElapsed = duration * 0.75f;
            float nextTickAt = tickStartElapsed;

            _tickTween = anim.Play(Tweener.TweenFloat(0f, duration, duration, elapsed =>
            {
                if (elapsed >= nextTickAt && _fillAmount > 0f)
                {
                    nextTickAt += 1f;
                    AudioManager.Instance?.Play(SoundType.TimerWarning);
                }
            }, EaseType.Linear));
        }

        /// <summary>
        /// Color interpolation: cyan (100%-50%) -> yellow (50%-25%) -> magenta (25%-0%).
        /// Exposed as static for testability.
        /// </summary>
        public static Color GetTimerColor(float fill)
        {
            fill = Mathf.Clamp01(fill);

            if (fill > 0.5f)
            {
                // Cyan
                return UIFactory.AccentCyan;
            }
            else if (fill > 0.25f)
            {
                // Cyan -> Yellow
                float t = 1f - (fill - 0.25f) / 0.25f;
                return Color.Lerp(UIFactory.AccentCyan, UIFactory.AccentGold, t);
            }
            else
            {
                // Yellow -> Magenta
                float t = 1f - fill / 0.25f;
                return Color.Lerp(UIFactory.AccentGold, UIFactory.AccentMagenta, t);
            }
        }
    }
}
