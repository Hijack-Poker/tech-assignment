using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using HijackPoker.Animation;
using HijackPoker.Managers;

namespace HijackPoker.UI
{
    /// <summary>
    /// Full-screen overlay that shows an enlarged card when a face-up card is tapped.
    /// Tap anywhere to dismiss.
    /// </summary>
    public class CardPreviewOverlay : MonoBehaviour, IPointerClickHandler
    {
        private RectTransform _rt;
        private Image _backdrop;
        private CardView _previewCard;
        private CanvasGroup _canvasGroup;
        private AnimationController _animController;
        private TweenHandle _showTween;
        private TweenHandle _hideTween;
        private bool _isVisible;

        public static CardPreviewOverlay Create(Transform parent, AnimationController anim)
        {
            var go = new GameObject("CardPreviewOverlay", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            UIFactory.StretchFill(rt);

            var view = go.AddComponent<CardPreviewOverlay>();
            view._rt = rt;
            view._animController = anim;
            view._canvasGroup = go.AddComponent<CanvasGroup>();
            view.BuildUI();
            view.Hide(false);
            return view;
        }

        private void BuildUI()
        {
            // Semi-transparent backdrop
            _backdrop = UIFactory.CreateImage("Backdrop", transform,
                new Color(0, 0, 0, 0.6f));
            var backdropRt = _backdrop.GetComponent<RectTransform>();
            UIFactory.StretchFill(backdropRt);

            // Enlarged card at center (2x scale)
            var cardSize = LayoutConfig.CardSize;
            _previewCard = CardView.Create("PreviewCard", transform);
            _previewCard.RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _previewCard.RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _previewCard.RectTransform.anchoredPosition = Vector2.zero;
            _previewCard.RectTransform.localScale = Vector3.one * 2f;
            _previewCard.RectTransform.localEulerAngles = Vector3.zero;
        }

        public void ShowCard(string cardString)
        {
            if (string.IsNullOrEmpty(cardString)) return;

            _showTween?.Cancel();
            _hideTween?.Cancel();
            _isVisible = true;

            gameObject.SetActive(true);
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = true;

            _previewCard.SetFaceUp(cardString);

            AudioManager.Instance?.Play(SoundType.CardFlip);

            if (_animController != null)
            {
                _showTween = _animController.Play(Tweener.TweenAlpha(
                    _canvasGroup, 0f, 1f, 0.2f));
                _animController.Play(Tweener.ScalePop(
                    _previewCard.RectTransform, 0.25f, 1.15f));
            }
            else
            {
                _canvasGroup.alpha = 1f;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isVisible) Hide(true);
        }

        private void Hide(bool animate)
        {
            _showTween?.Cancel();
            _isVisible = false;

            if (animate && _animController != null)
            {
                _hideTween = _animController.Play(Tweener.TweenAlpha(
                    _canvasGroup, _canvasGroup.alpha, 0f, 0.15f));
                _hideTween.OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    _canvasGroup.blocksRaycasts = false;
                });
            }
            else
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                gameObject.SetActive(false);
            }
        }
    }
}
