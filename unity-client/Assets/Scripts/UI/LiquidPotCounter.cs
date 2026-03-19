using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.Animation;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    /// <summary>
    /// Odometer-style rolling pot counter with font size scaling and punch effect.
    /// </summary>
    public class LiquidPotCounter : MonoBehaviour
    {
        private TextMeshProUGUI _text;
        private RectTransform _rt;
        private AnimationController _anim;
        private TweenHandle _rollTween;
        private TweenHandle _scaleTween;
        private float _displayValue;
        private float _targetValue;
        private float _baseFontSize;
        private float _baselineStack = 200f; // default baseline for scaling

        public TextMeshProUGUI Text => _text;

        public static LiquidPotCounter Create(Transform parent, TextMeshProUGUI existingText,
            float baseFontSize)
        {
            var comp = existingText.gameObject.AddComponent<LiquidPotCounter>();
            comp._text = existingText;
            comp._rt = existingText.GetComponent<RectTransform>();
            comp._baseFontSize = baseFontSize;
            return comp;
        }

        public void SetAnimController(AnimationController anim)
        {
            _anim = anim;
        }

        public void SetPot(float newPot, string sidePotStr, bool animate)
        {
            if (Mathf.Abs(newPot - _targetValue) < 0.01f) return;

            float oldPot = _targetValue;
            _targetValue = newPot;

            if (!animate || _anim == null || newPot <= 0)
            {
                _displayValue = newPot;
                _text.text = newPot > 0 ? $"{MoneyFormatter.Format(newPot)}{sidePotStr}" : "";
                _text.fontSize = _baseFontSize;
                return;
            }

            // Odometer roll
            _rollTween?.Cancel();
            float from = _displayValue;
            _rollTween = _anim.Play(Tweener.TweenFloat(from, newPot, 0.4f,
                v =>
                {
                    _displayValue = v;
                    if (_text != null)
                        _text.text = $"{MoneyFormatter.Format(v)}{sidePotStr}";
                }));

            // Font size scales with pot importance
            float potRatio = _baselineStack > 0 ? newPot / (_baselineStack * 0.5f) : 1f;
            float targetFontScale = Mathf.Clamp(potRatio, 1f, 1.4f);
            _text.fontSize = _baseFontSize * targetFontScale;

            // Scale punch on large increments (>20% of pot value)
            float increment = newPot - oldPot;
            if (increment > 0 && oldPot > 0 && increment / oldPot > 0.2f)
            {
                _scaleTween?.Cancel();
                _scaleTween = _anim.Play(Tweener.ScalePop(_rt, 0.2f, 1.08f));
            }
        }

        public void SetBaseline(float baseline)
        {
            _baselineStack = Mathf.Max(baseline, 1f);
        }
    }
}
