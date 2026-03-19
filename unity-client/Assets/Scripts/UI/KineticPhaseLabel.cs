using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.Animation;

namespace HijackPoker.UI
{
    public enum PhaseCategory
    {
        Setup,
        Deal,
        Betting,
        Showdown
    }

    /// <summary>
    /// Phase label with category-specific entrance animations.
    /// Deal: ScalePop, Betting: horizontal slide-in, Showdown: ScalePop + starburst.
    /// </summary>
    public class KineticPhaseLabel : MonoBehaviour
    {
        private TextMeshProUGUI _label;
        private RectTransform _rt;
        private RectTransform _pillRt;
        private TweenHandle _entranceTween;
        private string _currentText;

        public TextMeshProUGUI Label => _label;

        public static KineticPhaseLabel Create(Transform parent, Vector2 pillSize)
        {
            // Frosted glass pill behind phase label
            var pillGo = new GameObject("PhasePill", typeof(RectTransform));
            pillGo.transform.SetParent(parent, false);
            var pillRt = pillGo.GetComponent<RectTransform>();
            pillRt.anchorMin = new Vector2(0.5f, 0.5f);
            pillRt.anchorMax = new Vector2(0.5f, 0.5f);
            pillRt.pivot = new Vector2(0.5f, 0.5f);
            pillRt.sizeDelta = pillSize;

            var pillBg = pillGo.AddComponent<Image>();
            pillBg.color = new Color(1f, 1f, 1f, 0.5f);
            pillBg.sprite = TextureGenerator.GetRoundedRect((int)pillSize.x, (int)pillSize.y, 16);
            pillBg.type = Image.Type.Sliced;
            pillBg.raycastTarget = false;

            // Soft shadow on pill
            var pillShadow = UIFactory.CreateImage("PillShadow", pillGo.transform,
                new Color(0, 0, 0, 0.04f));
            pillShadow.sprite = TextureGenerator.GetRoundedRect((int)pillSize.x, (int)pillSize.y, 16);
            pillShadow.type = Image.Type.Sliced;
            var psRt = pillShadow.GetComponent<RectTransform>();
            psRt.anchorMin = Vector2.zero;
            psRt.anchorMax = Vector2.one;
            psRt.offsetMin = new Vector2(-1, -2);
            psRt.offsetMax = new Vector2(1, 0);
            pillShadow.transform.SetAsFirstSibling();

            pillGo.AddComponent<RectMask2D>();

            var comp = pillGo.AddComponent<KineticPhaseLabel>();
            comp._pillRt = pillRt;
            comp._rt = pillRt;

            comp._label = UIFactory.CreateText("PhaseLabel", pillGo.transform, "Waiting...",
                LayoutConfig.PhaseFontSize, UIFactory.AccentCyan, TextAlignmentOptions.Center,
                FontStyles.Bold);
            UIFactory.StretchFill(comp._label.GetComponent<RectTransform>());

            return comp;
        }

        public void Announce(string text, PhaseCategory category, AnimationController anim)
        {
            if (text == _currentText) return;
            _currentText = text;
            _entranceTween?.Cancel();

            if (anim == null)
            {
                _label.text = text;
                return;
            }

            // Fade out old text quickly
            var labelCg = _label.GetComponent<CanvasGroup>();
            if (labelCg == null) labelCg = _label.gameObject.AddComponent<CanvasGroup>();

            _entranceTween = anim.Play(Tweener.TweenFloat(1f, 0f, 0.12f,
                a => { if (labelCg != null) labelCg.alpha = a; }));

            _entranceTween.OnComplete(() =>
            {
                _label.text = text;
                _label.color = GetCategoryColor(category);
                if (labelCg != null) labelCg.alpha = 1f;

                switch (category)
                {
                    case PhaseCategory.Deal:
                        _entranceTween = anim.Play(
                            Tweener.ScalePop(_pillRt, 0.25f, 1.05f));
                        _entranceTween.OnComplete(() =>
                            anim.Play(Tweener.PunchScale(_pillRt,
                                AnimationConfig.PhasePunchDuration,
                                AnimationConfig.PhasePunchMagnitude,
                                AnimationConfig.PhasePunchVibrato)));
                        break;

                    case PhaseCategory.Betting:
                        var labelRt = _label.GetComponent<RectTransform>();
                        var startPos = labelRt.anchoredPosition;
                        labelRt.anchoredPosition = new Vector2(-60f, startPos.y);
                        _entranceTween = anim.Play(
                            Tweener.TweenPosition(labelRt,
                                new Vector2(-60f, startPos.y),
                                new Vector2(0, startPos.y),
                                0.3f, EaseType.EaseOutQuart));
                        anim.Play(Tweener.PunchScale(_pillRt,
                            AnimationConfig.PhasePunchDuration,
                            AnimationConfig.PhasePunchMagnitude,
                            AnimationConfig.PhasePunchVibrato));
                        break;

                    case PhaseCategory.Showdown:
                        _entranceTween = anim.Play(
                            Tweener.ScalePop(_pillRt, 0.3f, 1.1f));
                        _entranceTween.OnComplete(() =>
                            anim.Play(Tweener.PunchScale(_pillRt,
                                AnimationConfig.PhasePunchDuration,
                                AnimationConfig.PhasePunchMagnitude,
                                AnimationConfig.PhasePunchVibrato)));
                        // Starburst at pill center
                        var canvas = GetCanvasTransform();
                        if (canvas != null)
                        {
                            SparkleEffects.SpawnStarburst(canvas, Vector2.zero,
                                UIFactory.AccentGold, 0.5f, anim);
                        }
                        break;

                    default:
                        _entranceTween = anim.Play(Tweener.TweenFloat(0f, 1f, 0.2f,
                            a => { if (labelCg != null) labelCg.alpha = a; }));
                        break;
                }
            });
        }

        public static PhaseCategory GetCategory(int handStep)
        {
            if (handStep <= 3) return PhaseCategory.Setup;
            if (handStep == 4 || handStep == 6 || handStep == 8 || handStep == 10)
                return PhaseCategory.Deal;
            if (handStep == 5 || handStep == 7 || handStep == 9 || handStep == 11)
                return PhaseCategory.Betting;
            return PhaseCategory.Showdown; // 12+
        }

        private static Color GetCategoryColor(PhaseCategory category)
        {
            switch (category)
            {
                case PhaseCategory.Deal: return UIFactory.AccentCyan;
                case PhaseCategory.Betting: return UIFactory.AccentGold;
                case PhaseCategory.Showdown: return UIFactory.AccentMagenta;
                default: return UIFactory.AccentCyan;
            }
        }

        private Transform GetCanvasTransform()
        {
            var t = transform;
            while (t != null)
            {
                if (t.GetComponent<Canvas>() != null)
                    return t;
                t = t.parent;
            }
            return null;
        }
    }
}
