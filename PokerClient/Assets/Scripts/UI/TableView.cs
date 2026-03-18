using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using TMPro;
using DG.Tweening;
using UnityEngine.Networking;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.Utils;
using uVegas.Demo;

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

        [Header("Audio")]
        [SerializeField] private AudioSource _sfxAudioSource;
        [SerializeField] private AudioClip _turnStartSound;
        [SerializeField] private AudioClip _timeRemainingSound;
        [SerializeField] private AudioClip _dealShuffleSound;
        [SerializeField] private AudioClip _foldSound;
        [SerializeField] private AudioClip _chipBetSound;

        private AudioClip _crowdClapSound;
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
        private Coroutine _turnTimerRoutine;
        private int _turnTimerSeat = -1;
        private int _turnTimerStep = -1;
        private int _turnTimerGameNo = -1;
        private const float TurnDurationSeconds = 20f;
        private const float LowTimeWarningSeconds = 5f;
        private bool _hasPlayedLowTimeWarning;
        private AudioSource _timeWarningAudioSource;
        private int _badgeGameNo = -1;
        private int _badgeDealerSeat = -1;
        private int _badgeSmallBlindSeat = -1;
        private int _badgeBigBlindSeat = -1;
        private int _localSeat = 1;
        private HashSet<int> _prevFoldedSeats = new();
        private float _lastChipSoundTime = -10f;
        private const float ChipSoundCooldownSeconds = 0.06f;
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
        private static readonly Dictionary<string, string> CardSuitNames = new()
        {
            { "H", "Hearts" }, { "D", "Diamonds" }, { "C", "Clubs" }, { "S", "Spades" }
        };

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
            EnsureAudioRefs();
            TryResolveTurnStartSound();
            TryResolveDealAndFoldSounds();
            SetLoadingVisible(true, immediate: true);
        }

        private void OnEnable()
        {
            _stateManager.OnTableStateChanged += OnStateChanged;

            // Ensure ShowdownView exists
            if (GetComponent<ShowdownView>() == null)
                gameObject.AddComponent<ShowdownView>();
        }

        private void OnDisable() => _stateManager.OnTableStateChanged -= OnStateChanged;

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
                               player.Username.Equals(localPlayerName, System.StringComparison.OrdinalIgnoreCase);
                if (!isLocal && player.Seat == 1) isLocal = true;
                if (isLocal && playerSprite != null)
                    _seatAvatars[player.Seat] = playerSprite;
                else if (poolIdx < pool.Count)
                    _seatAvatars[player.Seat] = pool[poolIdx++];
            }
        }

        private void OnStateChanged(TableResponse state)
        {
            string localName = _gameManager != null ? _gameManager.PlayerName : null;
            _localSeat = ResolveLocalSeat(state.Players, localName);
            AssignAvatars(state.Players, localName);

            int step = state.Game.HandStep;
            bool newHand = state.Game.GameNo != _prevGameNo;

            ResolveBlindSeatsForHand(state, out int dealerSeat, out int sbSeat, out int bbSeat, newHand);

            // Reset tracking on new hand
            if (newHand)
            {
                _prevBets.Clear();
                _prevHandStep = -1;
                _hasPlayedDealThisHand = false;
                _prevFoldedSeats.Clear();
                HideWinnerCelebration();
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

                    // Blind-post transitions:
                    // 2 -> 3 means SB was just posted, 3 -> 4 means BB was just posted.
                    bool isBlindTransition = _prevHandStep == 2 || _prevHandStep == 3;
                    if (isBlindTransition)
                    {
                        bool isExpectedBlindPayer = (_prevHandStep == 2 && player.Seat == sbSeat) ||
                                                    (_prevHandStep == 3 && player.Seat == bbSeat);
                        if (!isExpectedBlindPayer) continue;
                    }

                    int idx = SeatToViewIndex(player.Seat);
                    if (idx >= 0 && idx < _seatViews.Length)
                    {
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

            // Hide hole cards until deal step begins.
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

            // ── 5. CARD DEAL ANIMATION (after redraw so cards are set up) ──
            if (shouldDealCards)
            {
                var activePlayers = state.Players
                    .Where(p =>
                    {
                        int idx = SeatToViewIndex(p.Seat);
                        return p.HasCards && idx >= 0 && idx < _seatViews.Length;
                    })
                    .ToList();
                StartCoroutine(AnimateCardDeal(activePlayers, dealerSeat));
                _hasPlayedDealThisHand = true;
            }

            UpdateFoldSounds(state.Players);
            UpdateWinnerCelebration(state);

            // Save state for next comparison
            _prevHandStep = step;
            _prevGameNo = state.Game.GameNo;
            _prevBets.Clear();
            foreach (var p in state.Players)
                _prevBets[p.Seat] = p.Bet;

            if (_isFirstState)
                SetLoadingVisible(false, immediate: false);

            UpdateTurnTimer(state);
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

            // Keep badges stable inside one hand to avoid visible switching
            // while backend states are still warming up in first steps.
            if (!newHand && _badgeGameNo == state.Game.GameNo &&
                _badgeDealerSeat > 0 && _badgeSmallBlindSeat > 0 && _badgeBigBlindSeat > 0)
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

            var clockwiseOccupied = GetClockwiseSeatOrder(occupied);

            if (clockwiseOccupied.Count == 0)
            {
                dealerSeat = apiDealerSeat > 0 ? apiDealerSeat : 1;
                sbSeat = dealerSeat;
                bbSeat = dealerSeat;
            }
            else
            {
                // Prefer backend-provided dealer/blind seats when valid.
                if (clockwiseOccupied.Contains(apiDealerSeat))
                    dealerSeat = apiDealerSeat;

                if (clockwiseOccupied.Contains(apiSmallBlindSeat) && clockwiseOccupied.Contains(apiBigBlindSeat))
                {
                    sbSeat = apiSmallBlindSeat;
                    bbSeat = apiBigBlindSeat;
                }

                // If dealer is not provided yet, rotate clockwise from previous hand's dealer.
                if (dealerSeat <= 0)
                {
                    if (_badgeDealerSeat > 0)
                        dealerSeat = GetNextClockwiseSeat(clockwiseOccupied, _badgeDealerSeat);
                    else
                        dealerSeat = clockwiseOccupied[0];
                }

                // If SB/BB are missing from backend, derive from dealer clockwise.
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

        // ══════ CHIP FLY ANIMATION ══════
        private void AnimateChipFly(SeatView seat)
        {
            if (_animLayer == null || _potTarget == null || _chipFlySprite == null) return;
            PlayChipBetSound();

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

            activePlayers = BuildDealOrderClockwise(activePlayers, dealerSeat);
            PlayDealShuffleSound();

            // Hide real cards on all seats being dealt to
            foreach (var player in activePlayers)
            {
                int idx = SeatToViewIndex(player.Seat);
                if (idx < 0 || idx >= _seatViews.Length) continue;
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
                    int idx = SeatToViewIndex(player.Seat);
                    if (idx < 0 || idx >= _seatViews.Length) continue;
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
                int idx = SeatToViewIndex(player.Seat);
                if (idx < 0 || idx >= _seatViews.Length) continue;
                _seatViews[idx].SetCardsVisible(true);
            }
        }

        private List<PlayerState> BuildDealOrderClockwise(List<PlayerState> players, int dealerSeat)
        {
            if (players == null || players.Count == 0)
                return new List<PlayerState>();

            var validPlayers = players
                .Where(p => p.Seat > 0)
                .ToList();

            if (validPlayers.Count <= 1 || dealerSeat <= 0)
                return validPlayers;

            var clockwiseSeats = GetClockwiseSeatOrder(validPlayers.Select(p => p.Seat));
            if (clockwiseSeats.Count == 0)
                return validPlayers;

            int startIdx = clockwiseSeats.IndexOf(dealerSeat);
            if (startIdx < 0) startIdx = 0;
            startIdx = (startIdx + 1) % clockwiseSeats.Count; // first card starts left of dealer

            var bySeat = validPlayers.ToDictionary(p => p.Seat, p => p);
            var ordered = new List<PlayerState>(clockwiseSeats.Count);
            for (int i = 0; i < clockwiseSeats.Count; i++)
            {
                int seat = clockwiseSeats[(startIdx + i) % clockwiseSeats.Count];
                if (bySeat.TryGetValue(seat, out var player))
                    ordered.Add(player);
            }

            return ordered;
        }

        private List<int> GetClockwiseSeatOrder(IEnumerable<int> seats)
        {
            if (seats == null) return new List<int>();

            Vector3 center = _potTarget != null ? _potTarget.position : transform.position;

            return seats
                .Distinct()
                .Where(seat =>
                {
                    int idx = SeatToViewIndex(seat);
                    return seat > 0 && idx >= 0 && idx < _seatViews.Length && _seatViews[idx] != null;
                })
                .Select(seat =>
                {
                    int idx = SeatToViewIndex(seat);
                    Vector3 p = _seatViews[idx].transform.position - center;
                    float angle = Mathf.Atan2(p.y, p.x); // radians
                    return new { seat, angle };
                })
                .OrderByDescending(x => x.angle) // descending angle = clockwise traversal in screen space
                .Select(x => x.seat)
                .ToList();
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
            StopTurnTimer();
            _restartButtonDelayTween?.Kill();
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

        private void UpdateTurnTimer(TableResponse state)
        {
            if (state == null || state.Game == null) return;

            bool isBettingStep = IsBettingStep(state.Game.HandStep);
            int moveSeat = state.Game.Move;

            if (!isBettingStep || moveSeat <= 0)
            {
                StopTurnTimer();
                return;
            }

            bool changed = _turnTimerRoutine == null ||
                           _turnTimerSeat != moveSeat ||
                           _turnTimerStep != state.Game.HandStep ||
                           _turnTimerGameNo != state.Game.GameNo;
            if (!changed) return;

            StopTurnTimer();
            _turnTimerSeat = moveSeat;
            _turnTimerStep = state.Game.HandStep;
            _turnTimerGameNo = state.Game.GameNo;
            _hasPlayedLowTimeWarning = false;
            PlayTurnStartSound();
            _turnTimerRoutine = StartCoroutine(TurnTimerCoroutine(_turnTimerSeat, _turnTimerStep, _turnTimerGameNo));
        }

        private IEnumerator TurnTimerCoroutine(int seat, int step, int gameNo)
        {
            float remaining = TurnDurationSeconds;
            while (remaining > 0f)
            {
                float normalized = remaining / TurnDurationSeconds;
                ShowTurnTimerOnSeat(seat, normalized);

                if (!_hasPlayedLowTimeWarning && remaining <= LowTimeWarningSeconds)
                {
                    _hasPlayedLowTimeWarning = true;
                    PlayTimeRemainingSound();
                }

                remaining -= Time.deltaTime;
                yield return null;
            }

            ShowTurnTimerOnSeat(seat, 0f);

            var current = _stateManager != null ? _stateManager.CurrentState : null;
            bool stillSameTurn = current != null &&
                                 current.Game != null &&
                                 current.Game.GameNo == gameNo &&
                                 current.Game.HandStep == step &&
                                 current.Game.Move == seat &&
                                 IsBettingStep(current.Game.HandStep);
            if (stillSameTurn && _gameManager != null)
            {
                _ = _gameManager.AdvanceStepAsync("fold", 0f);
            }
        }

        private void StopTurnTimer()
        {
            if (_turnTimerRoutine != null)
            {
                StopCoroutine(_turnTimerRoutine);
                _turnTimerRoutine = null;
            }
            ClearTurnTimers();
            StopTimeRemainingSound();
            _turnTimerSeat = -1;
            _turnTimerStep = -1;
            _turnTimerGameNo = -1;
            _hasPlayedLowTimeWarning = false;
        }

        private void ShowTurnTimerOnSeat(int seat, float normalized)
        {
            for (int i = 0; i < _seatViews.Length; i++)
            {
                if (_seatViews[i] == null) continue;
                bool active = i == SeatToViewIndex(seat);
                _seatViews[i].SetTurnTimer(active, normalized);
            }
        }

        private void ClearTurnTimers()
        {
            for (int i = 0; i < _seatViews.Length; i++)
            {
                if (_seatViews[i] == null) continue;
                _seatViews[i].SetTurnTimer(false, 0f);
            }
        }

        private static bool IsBettingStep(int step)
        {
            return step == 5 || step == 7 || step == 9 || step == 11;
        }

        private void EnsureAudioRefs()
        {
            if (_sfxAudioSource == null)
                _sfxAudioSource = GetComponent<AudioSource>();
            if (_sfxAudioSource == null)
                _sfxAudioSource = gameObject.AddComponent<AudioSource>();
        }

        private void TryResolveTurnStartSound()
        {
            if (_turnStartSound != null) return;

            var cardThemeManager = FindObjectOfType<CardThemeManager>();
            if (cardThemeManager != null)
            {
                var clipField = typeof(CardThemeManager).GetField("winSound", BindingFlags.Instance | BindingFlags.NonPublic);
                var sourceField = typeof(CardThemeManager).GetField("sfxAudioSource", BindingFlags.Instance | BindingFlags.NonPublic);

                _turnStartSound = clipField?.GetValue(cardThemeManager) as AudioClip;
                if (_sfxAudioSource == null)
                    _sfxAudioSource = sourceField?.GetValue(cardThemeManager) as AudioSource;
            }

            if (_turnStartSound == null)
                StartCoroutine(LoadTurnStartSoundFromProjectPath());
            if (_timeRemainingSound == null)
                StartCoroutine(LoadTimeRemainingSoundFromProjectPath());
        }

        private IEnumerator LoadTurnStartSoundFromProjectPath()
        {
            string clipPath = Path.Combine(Application.dataPath, "uVegas/Audio/UI/win_01.wav");
            if (!File.Exists(clipPath))
                yield break;

            using var request = UnityWebRequestMultimedia.GetAudioClip($"file://{clipPath}", AudioType.WAV);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                _turnStartSound = DownloadHandlerAudioClip.GetContent(request);
        }

        private IEnumerator LoadTimeRemainingSoundFromProjectPath()
        {
            string[] candidates =
            {
                Path.Combine(Application.dataPath, "uVegas/Audio/UI/time_remmaining.wav"),
                Path.Combine(Application.dataPath, "uVegas/Audio/UI/time_remaning.wav"),
                Path.Combine(Application.dataPath, "uVegas/Audio/UI/time_remaining.wav")
            };

            string clipPath = candidates.FirstOrDefault(File.Exists);
            if (string.IsNullOrEmpty(clipPath))
                yield break;

            using var request = UnityWebRequestMultimedia.GetAudioClip($"file://{clipPath}", AudioType.WAV);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                _timeRemainingSound = DownloadHandlerAudioClip.GetContent(request);
        }

        private void TryResolveDealAndFoldSounds()
        {
            if (_dealShuffleSound == null)
                StartCoroutine(LoadDealShuffleSoundFromProjectPath());
            if (_foldSound == null)
                StartCoroutine(LoadFoldSoundFromProjectPath());
            // Always override with the dedicated chips.wav so inspector leftovers
            // or older runtime-loaded clips don't keep playing.
            StartCoroutine(LoadChipBetSoundFromProjectPath());
            StartCoroutine(LoadCrowdClapSound());
        }

        private IEnumerator LoadDealShuffleSoundFromProjectPath()
        {
            string clipPath = Path.Combine(Application.dataPath, "Audio/Custom/card_shuffle_custom.wav");
            if (!File.Exists(clipPath))
                yield break;

            using var request = UnityWebRequestMultimedia.GetAudioClip($"file://{clipPath}", AudioType.WAV);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                _dealShuffleSound = DownloadHandlerAudioClip.GetContent(request);
        }

        private IEnumerator LoadFoldSoundFromProjectPath()
        {
            string clipPath = Path.Combine(Application.dataPath, "uVegas/Audio/UI/hover_01.wav");
            if (!File.Exists(clipPath))
                yield break;

            using var request = UnityWebRequestMultimedia.GetAudioClip($"file://{clipPath}", AudioType.WAV);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                _foldSound = DownloadHandlerAudioClip.GetContent(request);
        }

        private IEnumerator LoadChipBetSoundFromProjectPath()
        {
            string clipPath = Path.Combine(Application.dataPath, "uVegas/Audio/UI/chips.wav");
            if (!File.Exists(clipPath))
                yield break;

            using var request = UnityWebRequestMultimedia.GetAudioClip($"file://{clipPath}", AudioType.WAV);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                _chipBetSound = DownloadHandlerAudioClip.GetContent(request);
        }

        private IEnumerator LoadCrowdClapSound()
        {
            string clipPath = Path.Combine(Application.dataPath, "uVegas/Audio/UI/crowd-clap.wav");
            if (!File.Exists(clipPath))
                yield break;

            using var request = UnityWebRequestMultimedia.GetAudioClip($"file://{clipPath}", AudioType.WAV);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                _crowdClapSound = DownloadHandlerAudioClip.GetContent(request);
        }

        private void PlayTurnStartSound()
        {
            if (_turnStartSound == null) return;
            EnsureAudioRefs();
            if (_sfxAudioSource == null) return;
            _sfxAudioSource.PlayOneShot(_turnStartSound);
        }

        private void PlayTimeRemainingSound()
        {
            if (_timeRemainingSound == null) return;
            EnsureAudioRefs();
            if (_sfxAudioSource == null) return;

            if (_timeWarningAudioSource == null)
            {
                _timeWarningAudioSource = gameObject.AddComponent<AudioSource>();
                _timeWarningAudioSource.playOnAwake = false;
            }
            _timeWarningAudioSource.clip = _timeRemainingSound;
            _timeWarningAudioSource.Play();
        }

        private void StopTimeRemainingSound()
        {
            if (_timeWarningAudioSource != null && _timeWarningAudioSource.isPlaying)
                _timeWarningAudioSource.Stop();
        }

        private void PlayDealShuffleSound()
        {
            if (_dealShuffleSound == null) return;
            EnsureAudioRefs();
            if (_sfxAudioSource == null) return;
            _sfxAudioSource.PlayOneShot(_dealShuffleSound);
        }

        private void PlayFoldSound()
        {
            if (_foldSound == null) return;
            EnsureAudioRefs();
            if (_sfxAudioSource == null) return;
            _sfxAudioSource.PlayOneShot(_foldSound);
        }

        private void PlayChipBetSound()
        {
            if (_chipBetSound == null) return;
            if (Time.unscaledTime - _lastChipSoundTime < ChipSoundCooldownSeconds) return;
            EnsureAudioRefs();
            if (_sfxAudioSource == null) return;

            _lastChipSoundTime = Time.unscaledTime;
            _sfxAudioSource.PlayOneShot(_chipBetSound);
        }

        private void UpdateWinnerCelebration(TableResponse state)
        {
            if (state == null || state.Game == null || state.Players == null) return;
            if (state.Game.HandStep < 13) return;

            var winners = state.Players.Where(p => p != null && p.IsWinner && p.Seat > 0).ToList();
            if (winners.Count == 0) return;
            if (_celebratedGameNo == state.Game.GameNo) return;

            _celebratedGameNo = state.Game.GameNo;
            StartWinnerCelebration(winners);
        }

        private void StartWinnerCelebration(List<PlayerState> winners)
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
            _winnerBannerText.text = winners.Count == 1
                ? $"WINNER: {primaryWinner.Username?.ToUpper() ?? "PLAYER"}"
                : $"SPLIT POT: {winners.Count} WINNERS";
            SetWinnerCards(primaryWinner);

            if (_seatAvatars.TryGetValue(primaryWinner.Seat, out var avatar) && avatar != null)
                _winnerAvatarImage.sprite = avatar;
            if (_winnerAvatarMaskImage != null && _chipFlySprite != null)
                _winnerAvatarMaskImage.sprite = _chipFlySprite;

            int winnerIdx = SeatToViewIndex(primaryWinner.Seat);
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

            // Play crowd clap sound
            if (_sfxAudioSource != null && _crowdClapSound != null)
                _sfxAudioSource.PlayOneShot(_crowdClapSound);

            _restartButtonDelayTween = DOVirtual.DelayedCall(RestartButtonDelaySeconds, () =>
            {
                if (_restartHandButton == null) return;
                _restartHandButton.gameObject.SetActive(true);
                _restartHandButton.transform.localScale = Vector3.one * 0.84f;
                _restartHandButton.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
            });
        }

        private void HideWinnerCelebration()
        {
            _restartButtonDelayTween?.Kill();
            if (_celebrationRoot != null)
                _celebrationRoot.gameObject.SetActive(false);
        }

        private void EnsureCelebrationUI()
        {
            EnsureAnimationRefs();
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
            plateRT.sizeDelta = new Vector2(520f, 72f);
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
            textRT.sizeDelta = new Vector2(480f, 60f);
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

            _winnerCard1Image.sprite = LoadCardSprite(winner.Cards[0]) ?? back;
            _winnerCard2Image.sprite = LoadCardSprite(winner.Cards[1]) ?? back;
        }

        private static Sprite LoadCardSprite(string cardCode)
        {
            var (rank, suit) = CardUtils.ParseCard(cardCode);
            if (string.IsNullOrEmpty(rank) || string.IsNullOrEmpty(suit))
                return null;

            string suitName = CardSuitNames.TryGetValue(suit, out var mapped) ? mapped : suit;
            string spriteName = $"card{suitName}{rank}";
            return Resources.Load<Sprite>($"Cards/{spriteName}");
        }

        private Button CreateRestartButton(RectTransform parent)
        {
            var buttonGO = new GameObject("RestartHandButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(parent, false);
            var rt = buttonGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -145f);
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
            label.text = "RESTART HAND";
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
                PlayFoldSound();

            _prevFoldedSeats = currentFoldedSeats;
        }

        private int SeatToViewIndex(int seat)
        {
            if (seat <= 0 || _seatViews == null || _seatViews.Length == 0) return -1;
            // Keep local player anchored at bottom-right (view index 0),
            // then place others clockwise around the table.
            if (_seatViews.Length == 6)
            {
                int rel = ((seat - _localSeat) % 6 + 6) % 6; // clockwise distance from local seat
                int[] relativeToView = { 0, 4, 5, 1, 2, 3 };
                int idx = relativeToView[rel];
                if (idx >= 0 && idx < _seatViews.Length) return idx;
            }
            int fallback = seat - 1;
            return fallback >= 0 && fallback < _seatViews.Length ? fallback : -1;
        }

        private static int ResolveLocalSeat(List<PlayerState> players, string localName)
        {
            if (players == null || players.Count == 0) return 1;
            if (!string.IsNullOrEmpty(localName))
            {
                var local = players.FirstOrDefault(p =>
                    !string.IsNullOrEmpty(p.Username) &&
                    p.Username.Equals(localName, StringComparison.OrdinalIgnoreCase));
                if (local != null && local.Seat > 0) return local.Seat;
            }
            return 1;
        }
    }
}
