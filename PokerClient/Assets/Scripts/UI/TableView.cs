using UnityEngine;
using UnityEngine.UI;
using System;
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

        // State tracking
        private int _prevHandStep = -1;
        private int _prevGameNo = -1;
        private Dictionary<int, float> _prevBets = new();
        private bool _isFirstState = true;
        private bool _hasPlayedDealThisHand;
        private int _badgeGameNo = -1;
        private int _badgeDealerSeat = -1;
        private int _badgeSmallBlindSeat = -1;
        private int _badgeBigBlindSeat = -1;
        private int _localSeat = 1;
        private HashSet<int> _prevFoldedSeats = new();

        // Card back sprite for deal animation
        private Sprite _cardBackSprite;

        // Sub-controllers
        private TableAudioController _audio;
        private PotDisplayController _potDisplay;
        private TurnTimerController _turnTimer;
        private TipController _tipController;
        private WinnerCelebrationController _celebration;
        private TableAnimationController _animation;

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
            InitializeSubControllers();
            SetLoadingVisible(true, immediate: true);
        }

        private void InitializeSubControllers()
        {
            // Audio
            _audio = GetComponent<TableAudioController>();
            if (_audio == null) _audio = gameObject.AddComponent<TableAudioController>();
            _audio.Initialize();

            // Pot Display
            _potDisplay = GetComponent<PotDisplayController>();
            if (_potDisplay == null) _potDisplay = gameObject.AddComponent<PotDisplayController>();
            _potDisplay.Initialize(_centerPotText, _potTarget, _chipFlySprite);

            // Turn Timer
            _turnTimer = GetComponent<TurnTimerController>();
            if (_turnTimer == null) _turnTimer = gameObject.AddComponent<TurnTimerController>();
            _turnTimer.Initialize(_seatViews, SeatToViewIndex, _gameManager, _stateManager, _audio);

            // Tip Controller
            _tipController = GetComponent<TipController>();
            if (_tipController == null) _tipController = gameObject.AddComponent<TipController>();
            _tipController.Initialize(_dealerSource, _animLayer, _chipFlySprite,
                                      _seatViews, _gameManager, _stateManager, _audio, SeatToViewIndex);

            // Winner Celebration
            _celebration = GetComponent<WinnerCelebrationController>();
            if (_celebration == null) _celebration = gameObject.AddComponent<WinnerCelebrationController>();
            _celebration.Initialize(_animLayer, _potTarget, _chipFlySprite,
                                    _seatViews, _gameManager, _seatAvatars, _audio, SeatToViewIndex);

            // Animation Controller
            _animation = GetComponent<TableAnimationController>();
            if (_animation == null) _animation = gameObject.AddComponent<TableAnimationController>();
            _animation.Initialize(_animLayer, _dealerSource, _potTarget, _chipFlySprite,
                                  _cardBackSprite, _seatViews, _audio, SeatToViewIndex);
        }

        private void OnEnable()
        {
            _stateManager.OnTableStateChanged += OnStateChanged;
            _stateManager.OnTableReset += OnTableReset;

            if (GetComponent<ShowdownView>() == null)
                gameObject.AddComponent<ShowdownView>();
        }

        private void OnDisable()
        {
            _stateManager.OnTableStateChanged -= OnStateChanged;
            _stateManager.OnTableReset -= OnTableReset;
        }

        private void OnTableReset()
        {
            _prevGameNo = -1;
            _prevHandStep = -1;
            _celebration.ResetTracking();
            _isFirstState = false;
            _avatarsAssigned = false;
            _seatAvatars.Clear();
        }

        private void AssignAvatars(List<PlayerState> players, string localPlayerName)
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
                int j = UnityEngine.Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            int poolIdx = 0;
            foreach (var player in players)
            {
                bool isLocal = !string.IsNullOrEmpty(localPlayerName) &&
                               !string.IsNullOrEmpty(player.Username) &&
                               player.Username.Equals(localPlayerName, StringComparison.OrdinalIgnoreCase);
                if (!isLocal && player.Seat == 1) isLocal = true;
                if (isLocal && playerSprite != null)
                    _seatAvatars[player.Seat] = playerSprite;
                else if (poolIdx < pool.Count)
                    _seatAvatars[player.Seat] = pool[poolIdx++];
            }

            _celebration.UpdateSeatAvatars(_seatAvatars);
        }

        private void OnStateChanged(TableResponse state)
        {
            string localName = _gameManager != null ? _gameManager.PlayerName : null;
            _localSeat = SeatResolver.ResolveLocalSeat(state.Players, localName);
            _tipController.SetLocalSeat(_localSeat);
            AssignAvatars(state.Players, localName);

            int step = state.Game.HandStep;
            bool newHand = state.Game.GameNo != _prevGameNo;

            ResolveBlindSeatsForHand(state, out int dealerSeat, out int sbSeat, out int bbSeat, newHand);

            if (newHand)
            {
                _prevBets.Clear();
                _prevHandStep = -1;
                _hasPlayedDealThisHand = false;
                _prevFoldedSeats.Clear();
                _celebration.HideWinnerCelebration();
                StartCoroutine(_animation.AnimateShuffleAtDealer());
            }

            // ── 1. CHIP FLY ANIMATION (bet increases) ──
            bool shouldAnimateChipFly = !_isFirstState;
            if (shouldAnimateChipFly)
            {
                foreach (var player in state.Players)
                {
                    _prevBets.TryGetValue(player.Seat, out float prevBet);
                    bool betIncreased = player.Bet > prevBet && player.Bet > 0;
                    if (!betIncreased) continue;

                    if (step == 3 && state.Game.BigBlindSeat == 0 && player.Seat != state.Game.SmallBlindSeat) continue;
                    if (step == 4 && player.Seat != state.Game.BigBlindSeat) continue;

                    int idx = SeatToViewIndex(player.Seat);
                    if (idx >= 0 && idx < _seatViews.Length)
                        _animation.AnimateChipFly(_seatViews[idx]);
                }
            }

            // ── 2. CENTER POT UPDATE ──
            float livePot = state.Game.Pot;
            if (state.Players != null)
                foreach (var p in state.Players)
                    livePot += p.Bet;
            _potDisplay.UpdateCenterPot(livePot);

            // ── 3. DETECT CARD DEALING ──
            bool hasDealablePlayers = state.Players.Any(p => p.HasCards && p.Seat > 0);
            bool shouldDealCards = !_hasPlayedDealThisHand && step >= 4 && hasDealablePlayers;

            // ── 4. NORMAL REDRAW ──
            foreach (var seat in _seatViews)
                seat.Clear();

            foreach (var player in state.Players)
            {
                int idx = SeatToViewIndex(player.Seat);
                if (idx >= 0 && idx < _seatViews.Length)
                {
                    if (_seatAvatars.TryGetValue(player.Seat, out var avatar))
                        _seatViews[idx].SetAvatar(avatar);
                    _seatViews[idx].Render(player, state.Game, localName);
                    _seatViews[idx].SetActionPrompt(IsBettingStep(step) && state.Game.Move == player.Seat);
                    _seatViews[idx].SetPositionBadges(
                        player.Seat == dealerSeat,
                        player.Seat == sbSeat,
                        player.Seat == bbSeat);
                }
            }

            _communityCardsView.Refresh(state.Game.CommunityCards);

            // Hide hole cards until deal step begins
            if (step < 4)
            {
                foreach (var player in state.Players)
                {
                    int idx = SeatToViewIndex(player.Seat);
                    if (idx >= 0 && idx < _seatViews.Length)
                        _seatViews[idx].SetCardsVisible(false);
                }
            }
            else if (!shouldDealCards && _hasPlayedDealThisHand)
            {
                foreach (var player in state.Players)
                {
                    int idx = SeatToViewIndex(player.Seat);
                    if (idx >= 0 && idx < _seatViews.Length)
                        _seatViews[idx].SetCardsVisible(true);
                }
            }

            // ── 5. CARD DEAL ANIMATION ──
            if (shouldDealCards)
            {
                var activePlayers = state.Players
                    .Where(p =>
                    {
                        int idx = SeatToViewIndex(p.Seat);
                        return p.HasCards && idx >= 0 && idx < _seatViews.Length;
                    })
                    .ToList();
                StartCoroutine(_animation.AnimateCardDeal(activePlayers, dealerSeat));
                _hasPlayedDealThisHand = true;
            }

            UpdateFoldSounds(state.Players);
            _celebration.UpdateWinnerCelebration(state);

            // Save state for next comparison
            _prevHandStep = step;
            _prevGameNo = state.Game.GameNo;
            _prevBets.Clear();
            foreach (var p in state.Players)
                _prevBets[p.Seat] = p.Bet;

            if (_isFirstState)
                SetLoadingVisible(false, immediate: false);

            _turnTimer.UpdateTurnTimer(state);
            _isFirstState = false;
        }

        private void ResolveBlindSeatsForHand(TableResponse state, out int dealerSeat, out int sbSeat, out int bbSeat, bool newHand)
        {
            var occupied = state.Players
                .Select(p => p.Seat)
                .Where(s => s > 0)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            bool apiHasBlinds = state.Game.SmallBlindSeat > 0 && state.Game.BigBlindSeat > 0;
            if (!newHand && _badgeGameNo == state.Game.GameNo &&
                _badgeDealerSeat > 0 && _badgeSmallBlindSeat > 0 && _badgeBigBlindSeat > 0 &&
                apiHasBlinds)
            {
                dealerSeat = _badgeDealerSeat;
                sbSeat = _badgeSmallBlindSeat;
                bbSeat = _badgeBigBlindSeat;
                return;
            }

            int apiDealerSeat = state.Game.DealerSeat;
            int apiSmallBlindSeat = state.Game.SmallBlindSeat;
            int apiBigBlindSeat = state.Game.BigBlindSeat;

            dealerSeat = 0;
            sbSeat = 0;
            bbSeat = 0;

            var clockwiseOccupied = _animation.GetClockwiseSeatOrder(occupied);

            if (clockwiseOccupied.Count == 0)
            {
                dealerSeat = apiDealerSeat > 0 ? apiDealerSeat : 1;
                sbSeat = dealerSeat;
                bbSeat = dealerSeat;
            }
            else
            {
                if (clockwiseOccupied.Contains(apiDealerSeat))
                    dealerSeat = apiDealerSeat;

                if (clockwiseOccupied.Contains(apiSmallBlindSeat) && clockwiseOccupied.Contains(apiBigBlindSeat))
                {
                    sbSeat = apiSmallBlindSeat;
                    bbSeat = apiBigBlindSeat;
                }

                if (dealerSeat <= 0)
                {
                    if (_badgeDealerSeat > 0)
                        dealerSeat = GetNextClockwiseSeat(clockwiseOccupied, _badgeDealerSeat);
                    else
                        dealerSeat = clockwiseOccupied[0];
                }

                if (sbSeat <= 0 || bbSeat <= 0)
                {
                    sbSeat = GetNextClockwiseSeat(clockwiseOccupied, dealerSeat);
                    bbSeat = GetNextClockwiseSeat(clockwiseOccupied, sbSeat);
                }
            }

            _badgeGameNo = state.Game.GameNo;
            _badgeDealerSeat = dealerSeat;
            _badgeSmallBlindSeat = sbSeat;
            _badgeBigBlindSeat = bbSeat;
        }

        private static int GetNextClockwiseSeat(List<int> clockwiseSeats, int fromSeat)
        {
            if (clockwiseSeats == null || clockwiseSeats.Count == 0) return fromSeat;
            int idx = clockwiseSeats.IndexOf(fromSeat);
            if (idx < 0) return clockwiseSeats[0];
            return clockwiseSeats[(idx + 1) % clockwiseSeats.Count];
        }

        private void UpdateFoldSounds(List<PlayerState> players)
        {
            if (players == null || players.Count == 0)
            {
                _prevFoldedSeats.Clear();
                return;
            }

            var currentFoldedSeats = new HashSet<int>();
            bool hasNewFold = false;

            foreach (var player in players)
            {
                if (player == null || player.Seat <= 0) continue;

                bool isFoldedNow = player.IsFolded ||
                                   (!string.IsNullOrEmpty(player.Action) &&
                                    player.Action.Equals("fold", StringComparison.OrdinalIgnoreCase));
                if (!isFoldedNow) continue;

                currentFoldedSeats.Add(player.Seat);
                if (!_isFirstState && !_prevFoldedSeats.Contains(player.Seat))
                    hasNewFold = true;
            }

            if (hasNewFold)
                _audio.PlayFoldSound();

            _prevFoldedSeats = currentFoldedSeats;
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

        private static bool IsBettingStep(int step) => PokerConstants.IsBettingStep(step);

        private int SeatToViewIndex(int seat)
        {
            if (seat <= 0 || _seatViews == null || _seatViews.Length == 0) return -1;
            if (_seatViews.Length == 6)
            {
                int rel = ((seat - _localSeat) % 6 + 6) % 6;
                int[] relativeToView = { 0, 4, 5, 1, 2, 3 };
                int idx = relativeToView[rel];
                if (idx >= 0 && idx < _seatViews.Length) return idx;
            }
            int fallback = seat - 1;
            return fallback >= 0 && fallback < _seatViews.Length ? fallback : -1;
        }

        private void OnDestroy()
        {
            _turnTimer?.StopTurnTimer();
            _celebration?.Cleanup();
            _potDisplay?.Cleanup();
            if (_loadingOverlayGroup != null) DOTween.Kill(_loadingOverlayGroup);
        }
    }
}
