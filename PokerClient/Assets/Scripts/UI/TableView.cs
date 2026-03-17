using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using DG.Tweening;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    public class TableView : MonoBehaviour
    {
        [SerializeField] private TableStateManager _stateManager;
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private SeatView[] _seatViews;
        [SerializeField] private CommunityCardsView _communityCardsView;

        [Header("Center Pot")]
        [SerializeField] private TextMeshProUGUI _centerPotText;
        [SerializeField] private RectTransform _potTarget;

        [Header("Loading")]
        [SerializeField] private CanvasGroup _loadingOverlayGroup;
        [SerializeField] private TextMeshProUGUI _loadingOverlayText;

        [Header("Animation")]
        [SerializeField] private RectTransform _dealerSource;
        [SerializeField] private RectTransform _animLayer;
        [SerializeField] private Sprite _chipFlySprite;

        private Sprite[] _allAvatars;
        private Dictionary<int, Sprite> _seatAvatars;
        private bool _avatarsAssigned;

        // State tracking for animations
        private int _prevHandStep = -1;
        private int _prevGameNo = -1;
        private Dictionary<int, float> _prevBets = new();
        private float _displayedPot;
        private bool _isFirstState = true;
        private bool _hasPlayedDealThisHand;

        // Card back sprite for deal animation
        private Sprite _cardBackSprite;

        private void Awake()
        {
            _allAvatars = Resources.LoadAll<Sprite>("Avatars");
            _seatAvatars = new Dictionary<int, Sprite>();
            _cardBackSprite = Resources.Load<Sprite>("Cards/cardBack_red2");
            if (_cardBackSprite == null)
            {
                var allCards = Resources.LoadAll<Sprite>("Cards");
                _cardBackSprite = allCards.FirstOrDefault(s => s.name.StartsWith("cardBack"));
            }
            EnsureAnimationRefs();
            SetLoadingVisible(true, immediate: true);
        }

        private void OnEnable() => _stateManager.OnTableStateChanged += OnStateChanged;
        private void OnDisable() => _stateManager.OnTableStateChanged -= OnStateChanged;

        private void AssignAvatars(List<PlayerState> players)
        {
            if (_avatarsAssigned || _allAvatars == null || _allAvatars.Length == 0) return;
            _avatarsAssigned = true;

            string chosenAvatar = PlayerPrefs.GetString("PlayerAvatar", "");
            Sprite playerSprite = null;
            if (!string.IsNullOrEmpty(chosenAvatar))
                playerSprite = _allAvatars.FirstOrDefault(s => s.name == chosenAvatar);

            var pool = _allAvatars.Where(s => s != playerSprite).ToList();
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            int poolIdx = 0;
            foreach (var player in players)
            {
                if (player.Seat == 1 && playerSprite != null)
                    _seatAvatars[1] = playerSprite;
                else if (poolIdx < pool.Count)
                    _seatAvatars[player.Seat] = pool[poolIdx++];
            }
        }

        private void OnStateChanged(TableResponse state)
        {
            string localName = _gameManager != null ? _gameManager.PlayerName : null;
            AssignAvatars(state.Players);

            int step = state.Game.HandStep;
            bool newHand = state.Game.GameNo != _prevGameNo;

            // Reset tracking on new hand
            if (newHand)
            {
                _prevBets.Clear();
                _prevHandStep = -1;
                _hasPlayedDealThisHand = false;
            }

            // ── 1. CHIP FLY ANIMATION (bet increases) ──
            if (!_isFirstState)
            {
                foreach (var player in state.Players)
                {
                    _prevBets.TryGetValue(player.Seat, out float prevBet);
                    if (player.Bet > prevBet && player.Bet > 0)
                    {
                        int idx = player.Seat - 1;
                        if (idx >= 0 && idx < _seatViews.Length)
                            AnimateChipFly(_seatViews[idx]);
                    }
                }
            }

            // ── 2. CENTER POT UPDATE ──
            UpdateCenterPot(state.Game.Pot);

            // ── 3. DETECT CARD DEALING ──
            // Trigger exactly once per hand once cards exist and we are at/after deal step.
            bool hasDealablePlayers = state.Players.Any(p => p.HasCards && p.Seat > 0);
            bool shouldDealCards = !_hasPlayedDealThisHand && step >= 4 && hasDealablePlayers;

            ResolveBlindSeats(state, out int dealerSeat, out int sbSeat, out int bbSeat);

            // ── 4. NORMAL REDRAW ──
            foreach (var seat in _seatViews)
                seat.Clear();

            foreach (var player in state.Players)
            {
                int idx = player.Seat - 1;
                if (idx >= 0 && idx < _seatViews.Length)
                {
                    if (_seatAvatars.TryGetValue(player.Seat, out var avatar))
                        _seatViews[idx].SetAvatar(avatar);
                    _seatViews[idx].Render(player, state.Game, localName);
                    _seatViews[idx].SetPositionBadges(
                        player.Seat == dealerSeat,
                        player.Seat == sbSeat,
                        player.Seat == bbSeat);
                }
            }

            _communityCardsView.Refresh(state.Game.CommunityCards);

            // Hide hole cards until deal step begins.
            if (step < 4)
            {
                foreach (var player in state.Players)
                {
                    int idx = player.Seat - 1;
                    if (idx >= 0 && idx < _seatViews.Length)
                        _seatViews[idx].SetCardsVisible(false);
                }
            }
            else if (!shouldDealCards && _hasPlayedDealThisHand)
            {
                foreach (var player in state.Players)
                {
                    int idx = player.Seat - 1;
                    if (idx >= 0 && idx < _seatViews.Length)
                        _seatViews[idx].SetCardsVisible(true);
                }
            }

            // ── 5. CARD DEAL ANIMATION (after redraw so cards are set up) ──
            if (shouldDealCards)
            {
                var activePlayers = state.Players
                    .Where(p => p.HasCards && p.Seat - 1 >= 0 && p.Seat - 1 < _seatViews.Length)
                    .ToList();
                StartCoroutine(AnimateCardDeal(activePlayers, dealerSeat));
                _hasPlayedDealThisHand = true;
            }

            // Save state for next comparison
            _prevHandStep = step;
            _prevGameNo = state.Game.GameNo;
            _prevBets.Clear();
            foreach (var p in state.Players)
                _prevBets[p.Seat] = p.Bet;

            if (_isFirstState)
                SetLoadingVisible(false, immediate: false);

            _isFirstState = false;
        }

        private void ResolveBlindSeats(TableResponse state, out int dealerSeat, out int sbSeat, out int bbSeat)
        {
            dealerSeat = state.Game.DealerSeat;
            sbSeat = state.Game.SmallBlindSeat;
            bbSeat = state.Game.BigBlindSeat;

            if (dealerSeat > 0 && sbSeat > 0 && bbSeat > 0) return;

            var occupied = state.Players
                .Select(p => p.Seat)
                .Where(s => s > 0)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            if (occupied.Count == 0) return;
            if (dealerSeat <= 0) dealerSeat = occupied[0];
            if (sbSeat <= 0) sbSeat = GetNextOccupiedSeat(occupied, dealerSeat);
            if (bbSeat <= 0) bbSeat = GetNextOccupiedSeat(occupied, sbSeat);
        }

        private static int GetNextOccupiedSeat(List<int> occupied, int fromSeat)
        {
            for (int i = 0; i < occupied.Count; i++)
            {
                if (occupied[i] > fromSeat)
                    return occupied[i];
            }
            return occupied[0];
        }

        // ══════ CHIP FLY ANIMATION ══════
        private void AnimateChipFly(SeatView seat)
        {
            if (_animLayer == null || _potTarget == null || _chipFlySprite == null) return;

            var chipGO = new GameObject("FlyChip", typeof(RectTransform));
            chipGO.transform.SetParent(_animLayer, false);
            var img = chipGO.AddComponent<Image>();
            img.sprite = _chipFlySprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            var rt = chipGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(32, 32);

            // Position at seat (world space, same canvas)
            chipGO.transform.position = seat.transform.position;

            // Fly to pot center
            var seq = DOTween.Sequence();
            seq.Append(chipGO.transform.DOMove(_potTarget.position, 0.45f).SetEase(Ease.InOutCubic));
            seq.Join(DOTween.To(() => rt.sizeDelta, v => rt.sizeDelta = v, new Vector2(20, 20), 0.45f).SetEase(Ease.InQuad));
            seq.Join(DOTween.ToAlpha(() => img.color, c => img.color = c, 0f, 0.1f).SetDelay(0.35f));
            seq.OnComplete(() => Destroy(chipGO));
        }

        // ══════ CARD DEAL ANIMATION ══════
        private IEnumerator AnimateCardDeal(List<PlayerState> activePlayers, int dealerSeat)
        {
            EnsureAnimationRefs();
            if (_animLayer == null || _cardBackSprite == null || activePlayers == null || activePlayers.Count == 0)
                yield break;

            activePlayers = BuildDealOrder(activePlayers, dealerSeat);

            // Hide real cards on all seats being dealt to
            foreach (var player in activePlayers)
            {
                int idx = player.Seat - 1;
                _seatViews[idx].SetCardsVisible(false);
            }

            // Deal cards one by one in a visible clockwise rhythm.
            float stagger = 0.09f;
            float travelDuration = 0.3f;
            var flyingCards = new List<GameObject>();

            // Two rounds: first card to each player, then second card
            for (int round = 0; round < 2; round++)
            {
                foreach (var player in activePlayers)
                {
                    int idx = player.Seat - 1;
                    var seat = _seatViews[idx];

                    var cardGO = new GameObject($"DealCard_{player.Seat}_{round}", typeof(RectTransform));
                    cardGO.transform.SetParent(_animLayer, false);
                    var img = cardGO.AddComponent<Image>();
                    img.sprite = _cardBackSprite;
                    img.preserveAspect = true;
                    img.raycastTarget = false;

                    var rt = cardGO.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(44, 62);
                    cardGO.transform.SetAsLastSibling();

                    // Start at dealer
                    Vector3 startPos = _dealerSource != null
                        ? _dealerSource.position
                        : transform.position + new Vector3(0f, 180f, 0f);
                    cardGO.transform.position = startPos;
                    rt.localScale = Vector3.one * 0.3f;

                    // Target: seat's card area
                    Vector3 targetPos = seat.CardAreaWorldPosition + new Vector3(round == 0 ? -12f : 12f, round == 0 ? 2f : -2f, 0f);

                    // Animate in a gentle arc so travel is obvious.
                    float targetAngle = round == 0 ? -10f : 10f;
                    if (targetPos.x > startPos.x) targetAngle += 4f;

                    var seq = DOTween.Sequence();
                    seq.Append(CreateArcMoveTween(rt, startPos, targetPos, travelDuration));
                    seq.Join(rt.DOScale(Vector3.one, travelDuration).SetEase(Ease.OutCubic));
                    seq.Join(rt.DORotate(new Vector3(0f, 0f, targetAngle), travelDuration).SetEase(Ease.OutQuad));

                    flyingCards.Add(cardGO);

                    yield return new WaitForSeconds(stagger);
                }
            }

            // Wait for last animation to finish
            yield return new WaitForSeconds(0.35f);

            // Destroy flying cards and show real cards
            foreach (var go in flyingCards)
            {
                if (go != null) Destroy(go);
            }

            foreach (var player in activePlayers)
            {
                int idx = player.Seat - 1;
                _seatViews[idx].SetCardsVisible(true);
            }
        }

        private static List<PlayerState> BuildDealOrder(List<PlayerState> players, int dealerSeat)
        {
            if (players == null || players.Count == 0)
                return new List<PlayerState>();

            var sorted = players
                .Where(p => p.Seat > 0)
                .OrderBy(p => p.Seat)
                .ToList();

            if (sorted.Count <= 1 || dealerSeat <= 0)
                return sorted;

            int startIdx = sorted.FindIndex(p => p.Seat > dealerSeat);
            if (startIdx < 0) startIdx = 0;

            var ordered = new List<PlayerState>(sorted.Count);
            for (int i = 0; i < sorted.Count; i++)
                ordered.Add(sorted[(startIdx + i) % sorted.Count]);

            return ordered;
        }

        private static Tween CreateArcMoveTween(RectTransform card, Vector3 start, Vector3 end, float duration)
        {
            float height = Mathf.Clamp(Vector3.Distance(start, end) * 0.12f, 30f, 96f);
            Vector3 control = (start + end) * 0.5f + new Vector3(0f, height, 0f);

            return DOVirtual.Float(0f, 1f, duration, t =>
            {
                float u = 1f - t;
                card.position = (u * u * start) + (2f * u * t * control) + (t * t * end);
            }).SetEase(Ease.OutCubic);
        }

        // ══════ CENTER POT DISPLAY ══════
        private void UpdateCenterPot(float pot)
        {
            if (_centerPotText == null) return;

            DOTween.Kill(_centerPotText);
            if (pot <= 0)
            {
                _centerPotText.text = "";
                _displayedPot = 0;
                return;
            }

            DOVirtual.Float(_displayedPot, pot, 0.5f, value =>
            {
                _displayedPot = value;
                _centerPotText.text = $"POT: {MoneyFormatter.Format(value)}";
            }).SetEase(Ease.OutCubic).SetTarget(_centerPotText);
        }

        private void OnDestroy()
        {
            if (_centerPotText != null) DOTween.Kill(_centerPotText);
            if (_loadingOverlayGroup != null) DOTween.Kill(_loadingOverlayGroup);
        }

        private void EnsureAnimationRefs()
        {
            if (_animLayer == null)
            {
                var existing = GameObject.Find("AnimationLayer");
                if (existing != null)
                    _animLayer = existing.GetComponent<RectTransform>();
            }

            if (_animLayer == null)
            {
                var canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    var go = new GameObject("AnimationLayer", typeof(RectTransform));
                    go.transform.SetParent(canvas.transform, false);
                    _animLayer = go.GetComponent<RectTransform>();
                    _animLayer.anchorMin = Vector2.zero;
                    _animLayer.anchorMax = Vector2.one;
                    _animLayer.offsetMin = Vector2.zero;
                    _animLayer.offsetMax = Vector2.zero;
                }
            }

            if (_dealerSource == null)
            {
                var existing = GameObject.Find("DealerSource");
                if (existing != null)
                    _dealerSource = existing.GetComponent<RectTransform>();
            }
        }

        private void SetLoadingVisible(bool visible, bool immediate)
        {
            if (_loadingOverlayGroup == null) return;

            if (_loadingOverlayText != null)
                _loadingOverlayText.text = "Loading Table...";

            _loadingOverlayGroup.DOKill();
            _loadingOverlayGroup.interactable = visible;
            _loadingOverlayGroup.blocksRaycasts = visible;

            if (immediate)
            {
                _loadingOverlayGroup.alpha = visible ? 1f : 0f;
                return;
            }

            DOTween.To(
                () => _loadingOverlayGroup.alpha,
                v => _loadingOverlayGroup.alpha = v,
                visible ? 1f : 0f,
                0.28f
            ).SetEase(Ease.OutQuad);
        }
    }
}
