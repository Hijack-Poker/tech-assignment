using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    /// <summary>
    /// Manages the full-screen modal card preview shown when a player clicks
    /// their hole cards during their turn to act.
    /// </summary>
    public class CardPreviewController
    {
        private RectTransform _cardPreviewRoot;
        private Image _cardPreviewBackdrop;
        private Image _cardPreviewImageLeft;
        private Image _cardPreviewImageRight;

        private readonly MonoBehaviour _owner;

        public CardPreviewController(MonoBehaviour owner)
        {
            _owner = owner;
        }

        public bool IsActive => _cardPreviewRoot != null && _cardPreviewRoot.gameObject.activeSelf;

        public void Show(string card1Code, string card2Code, Vector3 fromWorldLeft, Vector3 fromWorldRight)
        {
            if (string.IsNullOrEmpty(card1Code) || string.IsNullOrEmpty(card2Code)) return;

            var spriteLeft = CardSpriteLoader.LoadCardSprite(card1Code);
            var spriteRight = CardSpriteLoader.LoadCardSprite(card2Code);
            if (spriteLeft == null || spriteRight == null) return;

            ShowCardPreview(fromWorldLeft, fromWorldRight, spriteLeft, spriteRight);
        }

        public void Hide()
        {
            HideCardPreview();
        }

        public void Cleanup()
        {
            HideCardPreview();
        }

        private void EnsureCardPreviewUI()
        {
            if (_cardPreviewRoot != null) return;

            var canvas = _owner.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            var rootGO = new GameObject("CardPreview", typeof(RectTransform), typeof(CanvasGroup));
            rootGO.transform.SetParent(canvas.transform, false);
            _cardPreviewRoot = rootGO.GetComponent<RectTransform>();
            _cardPreviewRoot.anchorMin = Vector2.zero;
            _cardPreviewRoot.anchorMax = Vector2.one;
            _cardPreviewRoot.offsetMin = Vector2.zero;
            _cardPreviewRoot.offsetMax = Vector2.zero;

            var backdropGO = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            backdropGO.transform.SetParent(_cardPreviewRoot, false);
            var backdropRT = backdropGO.GetComponent<RectTransform>();
            backdropRT.anchorMin = Vector2.zero;
            backdropRT.anchorMax = Vector2.one;
            backdropRT.offsetMin = Vector2.zero;
            backdropRT.offsetMax = Vector2.zero;
            _cardPreviewBackdrop = backdropGO.GetComponent<Image>();
            _cardPreviewBackdrop.color = new Color(0f, 0f, 0f, 0f);
            _cardPreviewBackdrop.raycastTarget = true;
            backdropGO.GetComponent<Button>().onClick.AddListener(HideCardPreview);

            _cardPreviewImageLeft = CreatePreviewCard("CardLeft");
            _cardPreviewImageRight = CreatePreviewCard("CardRight");

            _cardPreviewRoot.gameObject.SetActive(false);
        }

        private Image CreatePreviewCard(string name)
        {
            var cardGO = new GameObject(name, typeof(RectTransform), typeof(Image));
            cardGO.transform.SetParent(_cardPreviewRoot, false);
            var cardRT = cardGO.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.pivot = new Vector2(0.5f, 0.5f);
            cardRT.sizeDelta = new Vector2(164f, 228f);
            var img = cardGO.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = new Color(1f, 1f, 1f, 0f);
            return img;
        }

        private void ShowCardPreview(Vector3 fromWorldLeft, Vector3 fromWorldRight, Sprite revealLeft, Sprite revealRight)
        {
            EnsureCardPreviewUI();
            if (_cardPreviewRoot == null || _cardPreviewBackdrop == null ||
                _cardPreviewImageLeft == null || _cardPreviewImageRight == null) return;

            _cardPreviewRoot.gameObject.SetActive(true);

            DOTween.Kill(_cardPreviewBackdrop);
            DOTween.Kill(_cardPreviewImageLeft);
            DOTween.Kill(_cardPreviewImageLeft.transform);
            DOTween.Kill(_cardPreviewImageRight);
            DOTween.Kill(_cardPreviewImageRight.transform);

            _cardPreviewBackdrop.color = new Color(0f, 0f, 0f, 0f);
            _cardPreviewImageLeft.color = new Color(1f, 1f, 1f, 0f);
            _cardPreviewImageRight.color = new Color(1f, 1f, 1f, 0f);

            var leftRT = _cardPreviewImageLeft.rectTransform;
            var rightRT = _cardPreviewImageRight.rectTransform;
            leftRT.position = fromWorldLeft;
            rightRT.position = fromWorldRight;
            leftRT.localScale = Vector3.one * 0.72f;
            rightRT.localScale = Vector3.one * 0.72f;

            var back = Resources.Load<Sprite>("Cards/cardBack_red2");
            _cardPreviewImageLeft.sprite = back != null ? back : revealLeft;
            _cardPreviewImageRight.sprite = back != null ? back : revealRight;

            Vector3 centerLeft = _cardPreviewRoot.TransformPoint(new Vector3(-94f, 20f, 0f));
            Vector3 centerRight = _cardPreviewRoot.TransformPoint(new Vector3(94f, 20f, 0f));

            DOTween.ToAlpha(() => _cardPreviewBackdrop.color, c => _cardPreviewBackdrop.color = c, 0.45f, 0.18f);
            AnimatePreviewCard(_cardPreviewImageLeft, leftRT, centerLeft, revealLeft);
            AnimatePreviewCard(_cardPreviewImageRight, rightRT, centerRight, revealRight);
        }

        private void AnimatePreviewCard(Image img, RectTransform rt, Vector3 target, Sprite reveal)
        {
            DOTween.Sequence()
                .Append(DOTween.ToAlpha(() => img.color, c => img.color = c, 1f, 0.14f))
                .Join(rt.DOMove(target, 0.24f).SetEase(Ease.OutQuad))
                .Join(rt.DOScale(1.06f, 0.24f).SetEase(Ease.OutBack))
                .Append(rt.DOScaleX(0.05f, 0.10f).SetEase(Ease.InSine))
                .AppendCallback(() => img.sprite = reveal)
                .Append(rt.DOScaleX(1.12f, 0.12f).SetEase(Ease.OutSine))
                .Append(rt.DOScale(1f, 0.10f).SetEase(Ease.OutQuad));
        }

        private void HideCardPreview()
        {
            if (_cardPreviewRoot == null || !_cardPreviewRoot.gameObject.activeSelf) return;

            DOTween.Kill(_cardPreviewBackdrop);
            if (_cardPreviewImageLeft != null)
            {
                DOTween.Kill(_cardPreviewImageLeft);
                DOTween.Kill(_cardPreviewImageLeft.transform);
            }
            if (_cardPreviewImageRight != null)
            {
                DOTween.Kill(_cardPreviewImageRight);
                DOTween.Kill(_cardPreviewImageRight.transform);
            }

            DOTween.Sequence()
                .Append(DOTween.ToAlpha(() => _cardPreviewBackdrop.color, c => _cardPreviewBackdrop.color = c, 0f, 0.12f))
                .Join(_cardPreviewImageLeft != null
                    ? DOTween.ToAlpha(() => _cardPreviewImageLeft.color, c => _cardPreviewImageLeft.color = c, 0f, 0.10f)
                    : DOVirtual.DelayedCall(0f, () => { }))
                .Join(_cardPreviewImageRight != null
                    ? DOTween.ToAlpha(() => _cardPreviewImageRight.color, c => _cardPreviewImageRight.color = c, 0f, 0.10f)
                    : DOVirtual.DelayedCall(0f, () => { }))
                .OnComplete(() =>
                {
                    if (_cardPreviewRoot != null)
                        _cardPreviewRoot.gameObject.SetActive(false);
                });
        }
    }
}
