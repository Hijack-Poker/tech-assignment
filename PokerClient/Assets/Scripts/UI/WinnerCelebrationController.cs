using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    /// <summary>
    /// Manages the winner celebration overlay: avatar zoom, banner, confetti burst,
    /// winner cards, and the restart hand button.
    /// </summary>
    public class WinnerCelebrationController : MonoBehaviour
    {
        private RectTransform _animLayer;
        private RectTransform _potTarget;
        private Sprite _chipFlySprite;
        private SeatView[] _seatViews;
        private GameManager _gameManager;
        private Dictionary<int, Sprite> _seatAvatars;
        private TableAudioController _audio;
        private Func<int, int> _seatToViewIndex;

        private int _celebratedGameNo = -1;
        private RectTransform _celebrationRoot;
        private Image _focusBackdrop;
        private RectTransform _confettiLayer;
        private Image _winnerAvatarMaskImage;
        private Image _winnerAvatarImage;
        private Image _winnerCard1Image;
        private Image _winnerCard2Image;
        private Image _winnerBannerPlate;
        private TextMeshProUGUI _winnerBannerText;
        private Button _restartHandButton;
        private Tween _restartButtonDelayTween;
        private const float RestartButtonDelaySeconds = 2.8f;

        public void Initialize(RectTransform animLayer, RectTransform potTarget, Sprite chipFlySprite,
                               SeatView[] seatViews, GameManager gameManager,
                               Dictionary<int, Sprite> seatAvatars, TableAudioController audio,
                               Func<int, int> seatToViewIndex)
        {
            _animLayer = animLayer;
            _potTarget = potTarget;
            _chipFlySprite = chipFlySprite;
            _seatViews = seatViews;
            _gameManager = gameManager;
            _seatAvatars = seatAvatars;
            _audio = audio;
            _seatToViewIndex = seatToViewIndex;
        }

        public void UpdateSeatAvatars(Dictionary<int, Sprite> seatAvatars)
        {
            _seatAvatars = seatAvatars;
        }

        public void UpdateWinnerCelebration(TableResponse state)
        {
            if (state == null || state.Game == null || state.Players == null) return;
            if (state.Game.HandStep < 13) return;

            var winners = state.Players.Where(p => p != null && p.IsWinner && p.Seat > 0).ToList();
            if (winners.Count == 0) return;
            if (_celebratedGameNo == state.Game.GameNo) return;

            _celebratedGameNo = state.Game.GameNo;

            _audio?.PlayCrowdClapSound();
        }

        public void HideWinnerCelebration()
        {
            _restartButtonDelayTween?.Kill();
            if (_celebrationRoot != null)
                _celebrationRoot.gameObject.SetActive(false);
        }

        public void ResetTracking()
        {
            _celebratedGameNo = -1;
        }

        public void Cleanup()
        {
            _restartButtonDelayTween?.Kill();
        }

        public void StartWinnerCelebration(List<PlayerState> winners)
        {
            EnsureCelebrationUI();
            if (_celebrationRoot == null || winners == null || winners.Count == 0) return;

            _celebrationRoot.gameObject.SetActive(true);
            _winnerAvatarImage.gameObject.SetActive(true);
            if (_winnerBannerPlate != null) _winnerBannerPlate.gameObject.SetActive(true);
            _winnerBannerText.gameObject.SetActive(true);
            _restartHandButton.gameObject.SetActive(false);
            _restartButtonDelayTween?.Kill();

            var primaryWinner = winners[0];
            string handRank = !string.IsNullOrEmpty(primaryWinner.HandRank) ? primaryWinner.HandRank : "";
            string winAmount = primaryWinner.Winnings > 0 ? $"  {MoneyFormatter.FormatGain(primaryWinner.Winnings)}" : "";

            if (winners.Count == 1)
            {
                string line1 = $"WINNER: {primaryWinner.Username?.ToUpper() ?? "PLAYER"}";
                string line2 = "";
                if (!string.IsNullOrEmpty(handRank))
                    line2 += handRank;
                if (!string.IsNullOrEmpty(winAmount))
                    line2 += (line2.Length > 0 ? "  |  " : "") + $"<color=#4AE86C>{winAmount.Trim()}</color>";
                _winnerBannerText.text = string.IsNullOrEmpty(line2) ? line1 : $"{line1}\n<size=22>{line2}</size>";
            }
            else
            {
                var lines = new List<string> { $"SPLIT POT: {winners.Count} WINNERS" };
                foreach (var w in winners)
                {
                    string wRank = !string.IsNullOrEmpty(w.HandRank) ? $" ({w.HandRank})" : "";
                    string wAmount = w.Winnings > 0 ? $" <color=#4AE86C>{MoneyFormatter.FormatGain(w.Winnings)}</color>" : "";
                    lines.Add($"<size=20>{w.Username}{wRank}{wAmount}</size>");
                }
                _winnerBannerText.text = string.Join("\n", lines);
            }
            SetWinnerCards(primaryWinner);

            if (_seatAvatars != null && _seatAvatars.TryGetValue(primaryWinner.Seat, out var avatar) && avatar != null)
                _winnerAvatarImage.sprite = avatar;
            if (_winnerAvatarMaskImage != null && _chipFlySprite != null)
                _winnerAvatarMaskImage.sprite = _chipFlySprite;

            int winnerIdx = _seatToViewIndex(primaryWinner.Seat);
            Vector3 startPos = (_potTarget != null) ? _potTarget.position : transform.position;
            if (winnerIdx >= 0 && winnerIdx < _seatViews.Length && _seatViews[winnerIdx] != null)
                startPos = _seatViews[winnerIdx].transform.position;
            Vector3 targetPos = _potTarget != null ? _potTarget.position : transform.position;

            _winnerAvatarMaskImage.rectTransform.position = startPos;
            _winnerAvatarMaskImage.rectTransform.localScale = Vector3.one * 0.72f;
            _winnerAvatarImage.color = new Color(1f, 1f, 1f, 0f);
            if (_focusBackdrop != null)
                _focusBackdrop.color = new Color(0.03f, 0.06f, 0.10f, 0f);
            if (_winnerBannerPlate != null)
                _winnerBannerPlate.color = new Color(0.09f, 0.16f, 0.25f, 0f);

            DOTween.Sequence()
                .Append(_focusBackdrop != null
                    ? DOTween.ToAlpha(() => _focusBackdrop.color, c => _focusBackdrop.color = c, 0.62f, 0.22f)
                    : DOVirtual.DelayedCall(0f, () => { }))
                .Append(DOTween.ToAlpha(
                    () => _winnerAvatarImage.color,
                    c => _winnerAvatarImage.color = c,
                    1f,
                    0.16f))
                .Join(_winnerAvatarMaskImage.rectTransform.DOMove(targetPos, 0.48f).SetEase(Ease.OutBack))
                .Join(_winnerAvatarMaskImage.rectTransform.DOScale(1.26f, 0.48f).SetEase(Ease.OutBack))
                .Append(_winnerAvatarMaskImage.rectTransform.DOScale(1.1f, 0.18f).SetEase(Ease.OutQuad));

            _winnerBannerText.alpha = 0f;
            _winnerBannerText.transform.localScale = Vector3.one * 0.82f;
            DOTween.Sequence()
                .AppendInterval(0.22f)
                .Append(_winnerBannerPlate != null
                    ? DOTween.ToAlpha(() => _winnerBannerPlate.color, c => _winnerBannerPlate.color = c, 0.92f, 0.16f)
                    : DOVirtual.DelayedCall(0f, () => { }))
                .Append(DOTween.ToAlpha(
                    () => _winnerBannerText.color,
                    c => _winnerBannerText.color = c,
                    1f,
                    0.22f))
                .Join(_winnerBannerText.transform.DOScale(1f, 0.22f).SetEase(Ease.OutBack));

            if (_winnerCard1Image != null && _winnerCard2Image != null)
            {
                var c1 = _winnerCard1Image.color; c1.a = 0f; _winnerCard1Image.color = c1;
                var c2 = _winnerCard2Image.color; c2.a = 0f; _winnerCard2Image.color = c2;
                _winnerCard1Image.transform.localScale = Vector3.one * 0.86f;
                _winnerCard2Image.transform.localScale = Vector3.one * 0.86f;

                DOTween.Sequence()
                    .AppendInterval(0.18f)
                    .Append(DOTween.ToAlpha(() => _winnerCard1Image.color, v => _winnerCard1Image.color = v, 1f, 0.18f))
                    .Join(_winnerCard1Image.transform.DOScale(1f, 0.22f).SetEase(Ease.OutBack))
                    .Join(DOTween.ToAlpha(() => _winnerCard2Image.color, v => _winnerCard2Image.color = v, 1f, 0.18f))
                    .Join(_winnerCard2Image.transform.DOScale(1f, 0.22f).SetEase(Ease.OutBack));
            }

            SpawnConfettiBurst(56);

            _audio?.PlayCrowdClapSound();

            _restartButtonDelayTween = DOVirtual.DelayedCall(RestartButtonDelaySeconds, () =>
            {
                if (_restartHandButton == null) return;
                _restartHandButton.gameObject.SetActive(true);
                _restartHandButton.transform.localScale = Vector3.one * 0.84f;
                _restartHandButton.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
            });
        }

        private void EnsureCelebrationUI()
        {
            if (_celebrationRoot != null) return;
            if (_animLayer == null) return;

            var rootGO = new GameObject("WinnerCelebration", typeof(RectTransform), typeof(CanvasGroup));
            rootGO.transform.SetParent(_animLayer, false);
            _celebrationRoot = rootGO.GetComponent<RectTransform>();
            _celebrationRoot.anchorMin = Vector2.zero;
            _celebrationRoot.anchorMax = Vector2.one;
            _celebrationRoot.offsetMin = Vector2.zero;
            _celebrationRoot.offsetMax = Vector2.zero;

            var backdropGO = new GameObject("FocusBackdrop", typeof(RectTransform), typeof(Image));
            backdropGO.transform.SetParent(_celebrationRoot, false);
            var backdropRt = backdropGO.GetComponent<RectTransform>();
            backdropRt.anchorMin = Vector2.zero;
            backdropRt.anchorMax = Vector2.one;
            backdropRt.offsetMin = Vector2.zero;
            backdropRt.offsetMax = Vector2.zero;
            _focusBackdrop = backdropGO.GetComponent<Image>();
            _focusBackdrop.color = new Color(0.03f, 0.06f, 0.10f, 0f);
            _focusBackdrop.raycastTarget = false;

            var confettiGO = new GameObject("ConfettiLayer", typeof(RectTransform));
            confettiGO.transform.SetParent(_celebrationRoot, false);
            _confettiLayer = confettiGO.GetComponent<RectTransform>();
            _confettiLayer.anchorMin = Vector2.zero;
            _confettiLayer.anchorMax = Vector2.one;
            _confettiLayer.offsetMin = Vector2.zero;
            _confettiLayer.offsetMax = Vector2.zero;

            var avatarMaskGO = new GameObject("WinnerAvatarCircle", typeof(RectTransform), typeof(Image), typeof(Mask));
            avatarMaskGO.transform.SetParent(_celebrationRoot, false);
            var avatarMaskRT = avatarMaskGO.GetComponent<RectTransform>();
            avatarMaskRT.sizeDelta = new Vector2(110f, 110f);
            _winnerAvatarMaskImage = avatarMaskGO.GetComponent<Image>();
            _winnerAvatarMaskImage.sprite = _chipFlySprite;
            _winnerAvatarMaskImage.type = Image.Type.Simple;
            _winnerAvatarMaskImage.preserveAspect = true;
            _winnerAvatarMaskImage.color = Color.white;
            var mask = avatarMaskGO.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            var avatarGO = new GameObject("WinnerAvatar", typeof(RectTransform), typeof(Image));
            avatarGO.transform.SetParent(avatarMaskGO.transform, false);
            var avatarRT = avatarGO.GetComponent<RectTransform>();
            avatarRT.anchorMin = Vector2.zero;
            avatarRT.anchorMax = Vector2.one;
            avatarRT.offsetMin = Vector2.zero;
            avatarRT.offsetMax = Vector2.zero;
            _winnerAvatarImage = avatarGO.GetComponent<Image>();
            _winnerAvatarImage.preserveAspect = true;
            _winnerAvatarImage.raycastTarget = false;
            _winnerAvatarImage.color = new Color(1f, 1f, 1f, 0f);

            var ringGO = new GameObject("WinnerAvatarRing", typeof(RectTransform), typeof(Image));
            ringGO.transform.SetParent(avatarMaskGO.transform, false);
            var ringRT = ringGO.GetComponent<RectTransform>();
            ringRT.anchorMin = new Vector2(0.5f, 0.5f);
            ringRT.anchorMax = new Vector2(0.5f, 0.5f);
            ringRT.pivot = new Vector2(0.5f, 0.5f);
            ringRT.anchoredPosition = Vector2.zero;
            ringRT.sizeDelta = avatarMaskRT.sizeDelta + new Vector2(14f, 14f);
            var ring = ringGO.GetComponent<Image>();
            ring.sprite = _chipFlySprite;
            ring.color = new Color(1f, 0.86f, 0.44f, 0.95f);
            ring.raycastTarget = false;

            var bannerPlateGO = new GameObject("WinnerBannerPlate", typeof(RectTransform), typeof(Image));
            bannerPlateGO.transform.SetParent(_celebrationRoot, false);
            var plateRT = bannerPlateGO.GetComponent<RectTransform>();
            plateRT.anchorMin = new Vector2(0.5f, 0.5f);
            plateRT.anchorMax = new Vector2(0.5f, 0.5f);
            plateRT.pivot = new Vector2(0.5f, 0.5f);
            plateRT.anchoredPosition = new Vector2(0f, -80f);
            plateRT.sizeDelta = new Vector2(520f, 100f);
            _winnerBannerPlate = bannerPlateGO.GetComponent<Image>();
            _winnerBannerPlate.color = new Color(0.09f, 0.16f, 0.25f, 0f);
            _winnerBannerPlate.raycastTarget = false;

            var cardsGO = new GameObject("WinnerCards", typeof(RectTransform));
            cardsGO.transform.SetParent(_celebrationRoot, false);
            var cardsRT = cardsGO.GetComponent<RectTransform>();
            cardsRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardsRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardsRT.pivot = new Vector2(0.5f, 0.5f);
            cardsRT.anchoredPosition = new Vector2(0f, -12f);
            cardsRT.sizeDelta = new Vector2(210f, 110f);

            _winnerCard1Image = CreateWinnerCardImage(cardsRT, new Vector2(-32f, 0f), -8f);
            _winnerCard2Image = CreateWinnerCardImage(cardsRT, new Vector2(32f, 0f), 8f);

            var textGO = new GameObject("WinnerBanner", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(bannerPlateGO.transform, false);
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0.5f, 0.5f);
            textRT.anchorMax = new Vector2(0.5f, 0.5f);
            textRT.pivot = new Vector2(0.5f, 0.5f);
            textRT.anchoredPosition = Vector2.zero;
            textRT.sizeDelta = new Vector2(480f, 90f);
            _winnerBannerText = textGO.GetComponent<TextMeshProUGUI>();
            _winnerBannerText.alignment = TextAlignmentOptions.Center;
            _winnerBannerText.fontSize = 34;
            _winnerBannerText.fontStyle = FontStyles.Bold;
            _winnerBannerText.color = new Color(1f, 0.95f, 0.70f, 0f);
            _winnerBannerText.raycastTarget = false;

            _restartHandButton = CreateRestartButton(_celebrationRoot);
            _restartHandButton.gameObject.SetActive(false);
            _restartHandButton.onClick.AddListener(OnRestartHandClicked);

            _celebrationRoot.gameObject.SetActive(false);
        }

        private Image CreateWinnerCardImage(RectTransform parent, Vector2 anchoredPos, float zRotation)
        {
            var go = new GameObject("WinnerCard", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(72f, 100f);
            rt.localRotation = Quaternion.Euler(0f, 0f, zRotation);

            var img = go.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = new Color(1f, 1f, 1f, 0f);
            return img;
        }

        private void SetWinnerCards(PlayerState winner)
        {
            if (_winnerCard1Image == null || _winnerCard2Image == null) return;

            var back = Resources.Load<Sprite>("Cards/cardBack_red2");
            _winnerCard1Image.sprite = back;
            _winnerCard2Image.sprite = back;

            if (winner == null || winner.Cards == null || winner.Cards.Count < 2) return;

            _winnerCard1Image.sprite = CardSpriteLoader.LoadCardSprite(winner.Cards[0]) ?? back;
            _winnerCard2Image.sprite = CardSpriteLoader.LoadCardSprite(winner.Cards[1]) ?? back;
        }

        private Button CreateRestartButton(RectTransform parent)
        {
            var buttonGO = new GameObject("RestartHandButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(parent, false);
            var rt = buttonGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -160f);
            rt.sizeDelta = new Vector2(220f, 52f);

            var img = buttonGO.GetComponent<Image>();
            img.color = new Color(0.15f, 0.56f, 0.91f, 0.96f);

            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(buttonGO.transform, false);
            var labelRt = labelGO.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            var label = labelGO.GetComponent<TextMeshProUGUI>();
            label.text = "NEXT HAND";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 22;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.raycastTarget = false;

            return buttonGO.GetComponent<Button>();
        }

        private void OnRestartHandClicked()
        {
            HideWinnerCelebration();
            if (_gameManager != null)
                _ = _gameManager.AdvanceStepAsync();
        }

        private void SpawnConfettiBurst(int count)
        {
            if (_confettiLayer == null || _celebrationRoot == null) return;

            for (int i = _confettiLayer.childCount - 1; i >= 0; i--)
                Destroy(_confettiLayer.GetChild(i).gameObject);

            float width = _celebrationRoot.rect.width;
            float height = _celebrationRoot.rect.height;
            if (width <= 0f || height <= 0f)
            {
                width = Screen.width;
                height = Screen.height;
            }

            Color[] colors =
            {
                new Color(1f, 0.34f, 0.33f),
                new Color(0.99f, 0.80f, 0.28f),
                new Color(0.32f, 0.87f, 0.56f),
                new Color(0.29f, 0.70f, 0.98f),
                new Color(0.93f, 0.47f, 0.95f)
            };

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"Confetti_{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_confettiLayer, false);

                var rt = go.GetComponent<RectTransform>();
                float sizeX = UnityEngine.Random.Range(6f, 12f);
                float sizeY = UnityEngine.Random.Range(8f, 20f);
                rt.sizeDelta = new Vector2(sizeX, sizeY);
                rt.anchoredPosition = new Vector2(
                    UnityEngine.Random.Range(-width * 0.48f, width * 0.48f),
                    UnityEngine.Random.Range(height * 0.35f, height * 0.52f));
                rt.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));

                var img = go.GetComponent<Image>();
                img.color = colors[UnityEngine.Random.Range(0, colors.Length)];
                img.raycastTarget = false;

                float fall = UnityEngine.Random.Range(1.1f, 1.95f);
                float xDrift = UnityEngine.Random.Range(-80f, 80f);
                DOTween.To(
                    () => rt.anchoredPosition,
                    p => rt.anchoredPosition = p,
                    new Vector2(rt.anchoredPosition.x + xDrift, -height * 0.58f),
                    fall
                ).SetEase(Ease.InQuad);
                rt.DORotate(new Vector3(0f, 0f, UnityEngine.Random.Range(-420f, 420f)), fall, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear);
                DOTween.ToAlpha(
                    () => img.color,
                    c => img.color = c,
                    0f,
                    fall * 0.85f).SetDelay(fall * 0.15f);
                DOVirtual.DelayedCall(fall + 0.08f, () => { if (go != null) Destroy(go); });
            }
        }
    }
}
