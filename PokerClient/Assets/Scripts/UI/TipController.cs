using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using HijackPoker.Managers;

namespace HijackPoker.UI
{
    /// <summary>
    /// Manages the dealer tip button, chip fly animation to dealer, and "+$1" float text.
    /// </summary>
    public class TipController : MonoBehaviour
    {
        private RectTransform _dealerSource;
        private RectTransform _animLayer;
        private Sprite _chipFlySprite;
        private SeatView[] _seatViews;
        private GameManager _gameManager;
        private TableStateManager _stateManager;
        private TableAudioController _audio;
        private System.Func<int, int> _seatToViewIndex;

        private Button _tipButton;
        private bool _isTipping;
        private int _localSeat = 1;

        public void Initialize(RectTransform dealerSource, RectTransform animLayer, Sprite chipFlySprite,
                               SeatView[] seatViews, GameManager gameManager, TableStateManager stateManager,
                               TableAudioController audio, System.Func<int, int> seatToViewIndex)
        {
            _dealerSource = dealerSource;
            _animLayer = animLayer;
            _chipFlySprite = chipFlySprite;
            _seatViews = seatViews;
            _gameManager = gameManager;
            _stateManager = stateManager;
            _audio = audio;
            _seatToViewIndex = seatToViewIndex;
            CreateTipButton();
        }

        public void SetLocalSeat(int seat) => _localSeat = seat;

        private void CreateTipButton()
        {
            StartCoroutine(CreateTipButtonDeferred());
        }

        private IEnumerator CreateTipButtonDeferred()
        {
            while (_dealerSource == null)
                yield return null;

            var canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;

            var go = new GameObject("TipButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.position = _dealerSource.position;
            rt.anchoredPosition += new Vector2(0f, -50f);
            rt.sizeDelta = new Vector2(72f, 30f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.18f, 0.55f, 0.22f, 0.9f);

            var txtGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(go.transform, false);
            var txtRt = txtGO.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            var txt = txtGO.GetComponent<TextMeshProUGUI>();
            txt.text = "TIP $1";
            txt.fontSize = 13;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;

            _tipButton = go.GetComponent<Button>();
            _tipButton.onClick.AddListener(OnTipClicked);
        }

        private async void OnTipClicked()
        {
            if (_isTipping) return;
            var state = _gameManager?.CurrentState;
            if (state?.Game == null || state.Players == null) return;

            int actingSeat = state.Game.Move > 0 ? state.Game.Move : _localSeat;
            var actor = state.Players.Find(p => p.Seat == actingSeat);
            if (actor == null || actor.Stack < 1f) return;

            _isTipping = true;

            int viewIdx = _seatToViewIndex(actingSeat);
            if (viewIdx >= 0 && viewIdx < _seatViews.Length)
                AnimateTipChip(_seatViews[viewIdx]);

            var apiClient = FindObjectOfType<HijackPoker.Api.PokerApiClient>();
            if (apiClient != null)
            {
                await apiClient.TipDealerAsync(state.Game.TableId, actingSeat);
                var updated = await apiClient.GetTableStateAsync(state.Game.TableId);
                if (updated != null)
                    _stateManager.SetState(updated);
            }

            _isTipping = false;
        }

        private void AnimateTipChip(SeatView seat)
        {
            if (_animLayer == null || _dealerSource == null || _chipFlySprite == null) return;
            _audio?.PlayChipBetSound();

            var chipGO = new GameObject("TipChip", typeof(RectTransform));
            chipGO.transform.SetParent(_animLayer, false);
            var img = chipGO.AddComponent<Image>();
            img.sprite = _chipFlySprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            var rt = chipGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(28, 28);
            chipGO.transform.position = seat.transform.position;

            var seq = DOTween.Sequence();
            seq.Append(chipGO.transform.DOMove(_dealerSource.position, 0.5f).SetEase(Ease.InOutCubic));
            seq.Join(DOTween.To(() => rt.sizeDelta, v => rt.sizeDelta = v, new Vector2(18, 18), 0.5f).SetEase(Ease.InQuad));
            seq.Join(DOTween.ToAlpha(() => img.color, c => img.color = c, 0f, 0.12f).SetDelay(0.38f));
            seq.OnComplete(() =>
            {
                Destroy(chipGO);
                ShowTipFloat();
            });
        }

        private void ShowTipFloat()
        {
            if (_dealerSource == null) return;

            var canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;

            var go = new GameObject("TipFloat", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            go.transform.position = _dealerSource.position;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80, 30);

            var txt = go.GetComponent<TextMeshProUGUI>();
            txt.text = "+$1";
            txt.fontSize = 20;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = new Color(0.29f, 0.87f, 0.42f);
            txt.raycastTarget = false;

            float targetY = rt.anchoredPosition.y + 40f;
            var seq = DOTween.Sequence();
            seq.Append(DOTween.To(() => rt.anchoredPosition, v => rt.anchoredPosition = v,
                new Vector2(rt.anchoredPosition.x, targetY), 0.8f).SetEase(Ease.OutCubic));
            seq.Join(DOTween.ToAlpha(() => txt.color, c => txt.color = c, 0f, 0.3f).SetDelay(0.5f));
            seq.OnComplete(() => Destroy(go));
        }
    }
}
