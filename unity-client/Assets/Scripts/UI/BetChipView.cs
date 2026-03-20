using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.Animation;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    /// <summary>
    /// Renders a bet amount as a frosted white pill on the table felt surface,
    /// positioned between a player seat and the pot center.
    /// </summary>
    public class BetChipView : MonoBehaviour
    {
        private RectTransform _rt;
        private CanvasGroup _cg;
        private TextMeshProUGUI _text;
        private Image _bg;
        private float _prevBet;

        public RectTransform RectTransform => _rt;

        public static BetChipView Create(Transform parent)
        {
            var go = new GameObject("BetChip", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80, 24);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var view = go.AddComponent<BetChipView>();
            view._rt = rt;
            view._cg = go.AddComponent<CanvasGroup>();
            view._cg.alpha = 0f;
            view.BuildUI();
            return view;
        }

        private void BuildUI()
        {
            var roundedSprite = TextureGenerator.GetRoundedRect(80, 24, 12);

            // Soft shadow behind pill
            var shadow = UIFactory.CreateImage("Shadow", transform,
                new Color(0, 0, 0, 0.06f), new Vector2(80, 24));
            shadow.sprite = roundedSprite;
            shadow.type = Image.Type.Sliced;
            var shadowRt = shadow.GetComponent<RectTransform>();
            UIFactory.StretchFill(shadowRt);
            shadowRt.offsetMin = new Vector2(-1, -2);
            shadowRt.offsetMax = new Vector2(1, 0);

            _bg = gameObject.AddComponent<Image>();
            _bg.color = new Color(1f, 1f, 1f, 0.90f);
            _bg.sprite = roundedSprite;
            _bg.type = Image.Type.Sliced;
            _bg.raycastTarget = false;

            // Subtle border
            var border = UIFactory.CreateImage("Border", transform,
                new Color(0, 0, 0, 0.06f));
            border.sprite = roundedSprite;
            border.type = Image.Type.Sliced;
            var borderRt = border.GetComponent<RectTransform>();
            UIFactory.StretchFill(borderRt);

            _text = UIFactory.CreateText("BetText", transform, "",
                13f, UIFactory.AccentCyan, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.StretchFill(_text.GetComponent<RectTransform>());
        }

        public void UpdateBet(float amount, Vector2 position, bool animate, AnimationController anim)
        {
            _rt.anchoredPosition = position;

            if (amount > 0)
            {
                _cg.alpha = 1f;

                if (animate && anim != null && Mathf.Abs(amount - _prevBet) > 0.01f)
                {
                    float from = _prevBet;
                    anim.Play(Tweener.TweenFloat(from, amount, 0.3f,
                        v => _text.text = MoneyFormatter.Format(v)));
                }
                else
                {
                    _text.text = MoneyFormatter.Format(amount);
                }
            }
            else
            {
                _cg.alpha = 0f;
            }

            _prevBet = amount;
        }

        public void ResetState()
        {
            _prevBet = 0f;
            _cg.alpha = 0f;
        }
    }
}
