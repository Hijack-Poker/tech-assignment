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

        // Card back sprite for deal animation
        private Sprite _cardBackSprite;

        private void Awake()
        {
            _allAvatars = Resources.LoadAll<Sprite>("Avatars");
            _seatAvatars = new Dictionary<int, Sprite>();
            _cardBackSprite = Resources.Load<Sprite>("Cards/cardBack_red2");
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

            // ── 3. DETECT CARD DEALING (step 4 = Dealing Hole Cards) ──
            bool shouldDealCards = !_isFirstState && step == 4 && _prevHandStep < 4;

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
                }
            }

            _communityCardsView.Refresh(state.Game.CommunityCards);

            // ── 5. CARD DEAL ANIMATION (after redraw so cards are set up) ──
            if (shouldDealCards)
            {
                var activePlayers = state.Players
                    .Where(p => p.HasCards && p.Seat - 1 >= 0 && p.Seat - 1 < _seatViews.Length)
                    .ToList();
                StartCoroutine(AnimateCardDeal(activePlayers));
            }

            // Save state for next comparison
            _prevHandStep = step;
            _prevGameNo = state.Game.GameNo;
            _prevBets.Clear();
            foreach (var p in state.Players)
                _prevBets[p.Seat] = p.Bet;
            _isFirstState = false;
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
        private IEnumerator AnimateCardDeal(List<PlayerState> activePlayers)
        {
            if (_animLayer == null || _dealerSource == null || _cardBackSprite == null)
                yield break;

            // Hide real cards on all seats being dealt to
            foreach (var player in activePlayers)
            {
                int idx = player.Seat - 1;
                _seatViews[idx].SetCardsVisible(false);
            }

            // Deal cards one by one with stagger
            float stagger = 0.12f;
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

                    // Start at dealer
                    cardGO.transform.position = _dealerSource.position;
                    rt.localScale = Vector3.one * 0.3f;

                    // Target: seat's card area
                    Vector3 targetPos = seat.CardAreaWorldPosition;

                    // Animate
                    var seq = DOTween.Sequence();
                    seq.Append(cardGO.transform.DOMove(targetPos, 0.3f).SetEase(Ease.OutCubic));
                    seq.Join(rt.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutCubic));
                    // Slight rotation for flair
                    float angle = round == 0 ? -8f : 8f;
                    seq.Join(cardGO.transform.DOLocalRotate(new Vector3(0, 0, angle), 0.3f).SetEase(Ease.OutCubic));

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
        }
    }
}
