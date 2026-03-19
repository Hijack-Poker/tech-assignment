using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.Animation;

namespace HijackPoker.UI
{
    /// <summary>
    /// Full-screen semi-transparent overlay shown during loading/connecting.
    /// Static Create() factory following the SceneTransition pattern.
    /// </summary>
    public class LoadingOverlay : MonoBehaviour
    {
        private CanvasGroup _canvasGroup;
        private TextMeshProUGUI _label;
        private AnimationController _animController;
        private TweenHandle _pulseTween;

        public static LoadingOverlay Create(Transform parent, AnimationController anim)
        {
            var go = new GameObject("LoadingOverlay", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            UIFactory.StretchFill(rt);

            // Semi-transparent black background
            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.7f);
            img.raycastTarget = true;

            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;

            // Centered label
            var label = UIFactory.CreateText("LoadingLabel", go.transform,
                "Loading...", 20f, UIFactory.AccentGold);
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;
            var labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0.2f, 0.4f);
            labelRt.anchorMax = new Vector2(0.8f, 0.6f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            var view = go.AddComponent<LoadingOverlay>();
            view._canvasGroup = cg;
            view._label = label;
            view._animController = anim;
            return view;
        }

        public void Show(string message)
        {
            _label.text = message;
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;

            // Start pulsing text
            if (_pulseTween != null)
                _pulseTween.Cancel();

            _pulseTween = Tweener.PulseGlow(
                a => { if (_label != null) _label.alpha = a; },
                0.4f, 1f, 1.2f);
            _animController?.Play(_pulseTween);
        }

        public void Hide()
        {
            if (_pulseTween != null)
            {
                _pulseTween.Cancel();
                _pulseTween = null;
            }

            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }

        public void SetMessage(string message)
        {
            _label.text = message;
        }
    }
}
