using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using HijackPoker.Models;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    public class SeatView : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _stackText;
        [SerializeField] private TextMeshProUGUI _betText;
        [SerializeField] private TextMeshProUGUI _actionText;

        [Header("Badges")]
        [SerializeField] private GameObject _dealerBadge;
        [SerializeField] private GameObject _sbBadge;
        [SerializeField] private GameObject _bbBadge;

        [Header("Cards")]
        [SerializeField] private CardView _card1;
        [SerializeField] private CardView _card2;

        [Header("Visual")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _borderImage;
        [SerializeField] private Image _avatarRingImage;
        [SerializeField] private Image _avatarImage;
        [SerializeField] private TextMeshProUGUI _avatarInitialText;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Chips")]
        [SerializeField] private Image _betChipImage;
        [SerializeField] private ChipStackView _chipStackView;

        [Header("Cards Container")]
        [SerializeField] private CanvasGroup _cardsGroup;
        [SerializeField] private Image _turnTimerRing;

        [Header("Winner")]
        [SerializeField] private TextMeshProUGUI _handRankText;
        [SerializeField] private TextMeshProUGUI _winningsText;


        // Seat ring colors — must match RebuildScene.AvatarColors
        public static readonly Color[] SeatRingColors = {
            new Color(0.118f, 0.533f, 0.898f), // Seat 1 — #1E88E5 blue
            new Color(0.898f, 0.224f, 0.208f), // Seat 2 — #E53935 red
            new Color(0.263f, 0.627f, 0.278f), // Seat 3 — #43A047 green
            new Color(0.984f, 0.549f, 0.000f), // Seat 4 — #FB8C00 orange
            new Color(0.557f, 0.141f, 0.667f), // Seat 5 — #8E24AA purple
            new Color(0.000f, 0.675f, 0.757f), // Seat 6 — #00ACC1 teal
        };

        private static readonly Color NormalColor = new Color(0.04f, 0.09f, 0.14f, 0.9f);
        private static readonly Color PlayerColor = new Color(0.06f, 0.18f, 0.28f, 0.95f);
        private static readonly Color PlayerBorderColor = new Color(0.38f, 0.78f, 0.88f, 0.6f);
        private static readonly Color AllInColor = new Color(0.96f, 0.69f, 0.28f);
        private static readonly Color GoldColor = new Color(0.96f, 0.79f, 0.45f);
        private static readonly Color ActionDefaultColor = Color.white;
        private static readonly Color ActionPromptColor = new Color(0.98f, 0.84f, 0.38f);

        private bool _isLocalPlayer;
        private bool _wasActingTurn;
        private bool _isActingTurnNow;
        private string _card1Code;
        private string _card2Code;
        private CardPreviewController _cardPreview;

        private Tweener _winnerPulseTween;
        private float _displayedStack;
        private int _seatNumber;

        // Runtime color lookup by seat number
        private static readonly Dictionary<int, Color> _runtimeSeatColors = new();
        public static Color GetSeatColor(int seat) =>
            _runtimeSeatColors.TryGetValue(seat, out var c) ? c : Color.white;

        private void Awake()
        {
            RegisterCardClickHandlers();

            // Register ring color early so HandHistoryView can use it
            // Parse seat number from GameObject name (e.g. "Seat1" → 1)
            string goName = gameObject.name;
            if (goName.StartsWith("Seat") && int.TryParse(goName.Substring(4), out int seatNum))
            {
                _seatNumber = seatNum;
                if (_avatarRingImage != null)
                    _runtimeSeatColors[_seatNumber] = _avatarRingImage.color;
            }
        }

        public void Render(PlayerState player, GameState game, string localPlayerName = null)
        {
            gameObject.SetActive(true);

            // Register this seat's ring color for history view
            _seatNumber = player.Seat;
            if (_avatarRingImage != null)
                _runtimeSeatColors[_seatNumber] = _avatarRingImage.color;

            // Check if this seat belongs to the local player (seat 1)
            bool nameMatch = !string.IsNullOrEmpty(localPlayerName) &&
                             string.Equals(player.Username, localPlayerName, System.StringComparison.OrdinalIgnoreCase);
            _isLocalPlayer = nameMatch || player.Seat == 1;

            // Scale up avatar for local player to make them distinct
            if (_avatarRingImage != null)
            {
                var avatarParent = _avatarRingImage.transform.parent;
                if (avatarParent != null)
                    avatarParent.localScale = _isLocalPlayer ? Vector3.one * 1.35f : Vector3.one;
            }

            _nameText.text = _isLocalPlayer
                ? $"{localPlayerName} <size=10><color=#6EC6FF>(You)</color></size>"
                : player.Username;

            // Set avatar initial as fallback
            if (_avatarInitialText != null)
            {
                bool hasAvatar = _avatarImage != null && _avatarImage.sprite != null;
                string displayName = _isLocalPlayer ? localPlayerName : player.Username;
                _avatarInitialText.text = hasAvatar ? "" :
                    (!string.IsNullOrEmpty(displayName) ? displayName[0].ToString().ToUpper() : "?");
            }
            _betText.text = player.Bet > 0 ? MoneyFormatter.Format(player.Bet) : "";
            if (_betChipImage != null) _betChipImage.gameObject.SetActive(player.Bet > 0);
            if (_chipStackView != null) _chipStackView.Render(player.Stack);
            // All-in gets a bold styled label with pulse, others show normal action
            if (player.IsAllIn)
            {
                _actionText.text = "\u26A0 ALL IN \u26A0";
                _actionText.color = AllInColor;
                _actionText.fontSize = _actionText.fontSize > 0 ? _actionText.fontSize : 14f;
                DOTween.Kill(_actionText.transform, true);
                _actionText.transform.localScale = Vector3.one;
                _actionText.transform.DOScale(1.15f, 0.6f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetTarget(_actionText.transform);
            }
            else
            {
                DOTween.Kill(_actionText.transform, true);
                _actionText.transform.localScale = Vector3.one;
                _actionText.text = player.Action?.ToUpper() ?? "";
                _actionText.color = ActionDefaultColor;
            }

            // Stack tween
            DOTween.Kill(_stackText);
            DOVirtual.Float(_displayedStack, player.Stack, 0.4f, value =>
            {
                _displayedStack = value;
                _stackText.text = MoneyFormatter.Format(value);
            }).SetEase(Ease.OutCubic).SetTarget(_stackText);

            _dealerBadge.SetActive(player.Seat == game.DealerSeat);
            _sbBadge.SetActive(player.Seat == game.SmallBlindSeat);
            _bbBadge.SetActive(player.Seat == game.BigBlindSeat);

            ApplyFoldVisual(player.IsFolded);

            bool isWinner = player.IsWinner;

            // Border: gold for winners, pulsing orange for all-in, blue for local, clear otherwise
            DOTween.Kill(_borderImage);
            if (isWinner)
            {
                _borderImage.color = GoldColor;
            }
            else if (player.IsAllIn)
            {
                _borderImage.color = AllInColor;
                DOTween.To(() => _borderImage.color, c => _borderImage.color = c,
                    new Color(1f, 0.3f, 0.1f), 0.5f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetTarget(_borderImage);
            }
            else
            {
                _borderImage.color = _isLocalPlayer ? PlayerBorderColor : Color.clear;
            }

            // Avatar ring: gold glow for winners, scaled up 2x with pulse
            if (_avatarRingImage != null)
            {
                DOTween.Kill(_avatarRingImage);
                DOTween.Kill(_avatarRingImage.transform);
                if (isWinner)
                {
                    _avatarRingImage.color = GoldColor;
                    // Scale up to 2x and pulse between 1.8x and 2.2x
                    _avatarRingImage.transform.localScale = Vector3.one * 2f;
                    _avatarRingImage.transform.DOScale(2.2f, 0.7f)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetEase(Ease.InOutSine)
                        .SetTarget(_avatarRingImage.transform);
                }
                else
                {
                    _avatarRingImage.transform.localScale = Vector3.one;
                    var rc = _avatarRingImage.color;
                    rc.a = player.IsFolded ? 0.75f : 1f;
                    _avatarRingImage.color = new Color(rc.r, rc.g, rc.b, rc.a);
                }
            }

            bool isActingTurn = IsBettingStep(game.HandStep) && game.Move == player.Seat;
            _isActingTurnNow = isActingTurn;
            bool showCards = game.IsShowdown || isWinner || isActingTurn;
            if (player.HasCards && player.Cards.Count >= 2)
            {
                _card1Code = player.Cards[0];
                _card2Code = player.Cards[1];
                _card1.SetCard(player.Cards[0], showCards);
                _card2.SetCard(player.Cards[1], showCards);
            }
            else
            {
                _card1Code = null;
                _card2Code = null;
                _card1.SetBlueBack();
                _card2.SetBlueBack();
            }
            AnimateTurnCardFocus(isActingTurn);

            // Winner animation
            if (_winnerPulseTween != null) { _winnerPulseTween.Kill(); _winnerPulseTween = null; }

            var baseColor = _isLocalPlayer ? PlayerColor : NormalColor;
            if (player.IsFolded)
                baseColor.a *= 0.72f;
            if (isWinner)
            {
                // Pulse gold then stay gold-tinted
                _backgroundImage.color = baseColor;
                _winnerPulseTween = DOTween.To(
                    () => _backgroundImage.color,
                    c => _backgroundImage.color = c,
                    GoldColor, 0.5f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);

                _winningsText.text = MoneyFormatter.FormatGain(player.Winnings);
                _winningsText.color = new Color(0.45f, 0.91f, 0.61f);
                _winningsText.transform.localScale = Vector3.zero;
                _winningsText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            }
            else
            {
                _backgroundImage.color = baseColor;
                _winningsText.text = "";
            }

            // Show hand rank for all remaining players at showdown
            bool showRank = (game.IsShowdown || isWinner) && !string.IsNullOrEmpty(player.HandRank);
            _handRankText.text = showRank ? player.HandRank : "";
            if (showRank)
                _handRankText.color = isWinner ? Color.white : new Color(0.7f, 0.7f, 0.7f);
        }

        /// <summary>World position of the cards area (for deal animation targeting).</summary>
        public Vector3 CardAreaWorldPosition =>
            _cardsGroup != null ? _cardsGroup.transform.position : transform.position;

        public void SetPositionBadges(bool isDealer, bool isSmallBlind, bool isBigBlind)
        {
            if (_dealerBadge != null) _dealerBadge.SetActive(isDealer);
            if (_sbBadge != null) _sbBadge.SetActive(isSmallBlind);
            if (_bbBadge != null) _bbBadge.SetActive(isBigBlind);
        }

        /// <summary>Show/hide the cards container (used during deal animation).</summary>
        public void SetCardsVisible(bool visible)
        {
            if (_cardsGroup != null)
            {
                _cardsGroup.alpha = visible ? 1f : 0f;
            }
            else
            {
                if (_card1 != null) _card1.gameObject.SetActive(visible);
                if (_card2 != null) _card2.gameObject.SetActive(visible);
            }
        }

        public void SetTurnTimer(bool active, float normalizedRemaining)
        {
            EnsureTurnTimerRing();
            if (_turnTimerRing == null) return;

            _turnTimerRing.gameObject.SetActive(active);
            if (!active) return;

            _turnTimerRing.fillAmount = Mathf.Clamp01(normalizedRemaining);
        }

        public void SetActionPrompt(bool showPrompt)
        {
            if (_actionText == null) return;
            if (showPrompt)
            {
                _actionText.text = "FOLD | CALL | RAISE";
                _actionText.color = ActionPromptColor;
            }
            else
            {
                _actionText.color = ActionDefaultColor;
            }
        }

        public void SetAvatar(Sprite sprite)
        {
            if (_avatarImage == null) return;
            if (sprite != null)
            {
                _avatarImage.sprite = sprite;
                _avatarImage.color = Color.white;
                if (_avatarInitialText != null) _avatarInitialText.text = "";
            }
        }

        public void Clear()
        {
            if (_winnerPulseTween != null) { _winnerPulseTween.Kill(); _winnerPulseTween = null; }
            if (_avatarRingImage != null)
            {
                DOTween.Kill(_avatarRingImage);
                DOTween.Kill(_avatarRingImage.transform);
                _avatarRingImage.transform.localScale = Vector3.one;
            }
            DOTween.Kill(_borderImage);
            DOTween.Kill(_actionText?.transform);
            if (_actionText != null) _actionText.transform.localScale = Vector3.one;
            _backgroundImage.color = NormalColor;
            _displayedStack = 0f;
            DOTween.Kill(_stackText);
            _wasActingTurn = false;
            if (_cardsGroup != null) _cardsGroup.transform.localScale = Vector3.one;
            HideCardPreview();
            if (_chipStackView != null) _chipStackView.Clear();
            SetTurnTimer(false, 0f);
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            DOTween.Kill(_stackText);
            if (_winnerPulseTween != null) _winnerPulseTween.Kill();
            if (_avatarRingImage != null)
            {
                DOTween.Kill(_avatarRingImage);
                DOTween.Kill(_avatarRingImage.transform);
            }
            HideCardPreview();
        }

        private void EnsureTurnTimerRing()
        {
            if (_turnTimerRing != null || _avatarRingImage == null) return;

            var go = new GameObject("TurnTimerRing", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_avatarRingImage.transform.parent, false);
            go.transform.SetAsLastSibling();

            var rt = go.GetComponent<RectTransform>();
            var ringRt = _avatarRingImage.rectTransform;
            rt.anchorMin = ringRt.anchorMin;
            rt.anchorMax = ringRt.anchorMax;
            rt.pivot = ringRt.pivot;
            rt.anchoredPosition = ringRt.anchoredPosition;
            rt.sizeDelta = ringRt.sizeDelta + new Vector2(14f, 14f);
            rt.localScale = Vector3.one;

            var img = go.GetComponent<Image>();
            img.sprite = _avatarRingImage.sprite;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Radial360;
            img.fillOrigin = (int)Image.Origin360.Top;
            img.fillClockwise = false;
            img.fillAmount = 1f;
            img.color = new Color(0.96f, 0.81f, 0.33f, 0.92f);
            img.raycastTarget = false;

            _turnTimerRing = img;
            _turnTimerRing.gameObject.SetActive(false);
        }

        private void AnimateTurnCardFocus(bool isActingTurn)
        {
            Transform target = _cardsGroup != null ? _cardsGroup.transform : transform;
            if (target == null) return;

            if (isActingTurn && !_wasActingTurn)
            {
                DOTween.Kill(target);
                target.localScale = Vector3.one;
                DOTween.Sequence()
                    .Append(target.DOScale(1.08f, 0.14f).SetEase(Ease.OutBack))
                    .Append(target.DOScale(1f, 0.12f).SetEase(Ease.OutQuad));
            }
            else if (!isActingTurn && _wasActingTurn)
            {
                DOTween.Kill(target);
                target.localScale = Vector3.one;
                HideCardPreview();
            }

            _wasActingTurn = isActingTurn;
        }

        private static bool IsBettingStep(int step) => PokerConstants.IsBettingStep(step);

        private void RegisterCardClickHandlers()
        {
            if (_card1 != null)
            {
                _card1.Clicked -= OnCardClicked;
                _card1.Clicked += OnCardClicked;
            }
            if (_card2 != null)
            {
                _card2.Clicked -= OnCardClicked;
                _card2.Clicked += OnCardClicked;
            }
        }

        private void OnCardClicked(CardView clickedCard)
        {
            if (!_isActingTurnNow) return;
            if (string.IsNullOrEmpty(_card1Code) || string.IsNullOrEmpty(_card2Code)) return;

            if (_cardPreview == null) _cardPreview = new CardPreviewController(this);

            Vector3 fromLeft = _card1 != null ? _card1.transform.position : clickedCard.transform.position;
            Vector3 fromRight = _card2 != null ? _card2.transform.position : clickedCard.transform.position;
            _cardPreview.Show(_card1Code, _card2Code, fromLeft, fromRight);
        }

        private void HideCardPreview()
        {
            _cardPreview?.Hide();
        }

        private void ApplyFoldVisual(bool isFolded)
        {
            // Dim card sprites directly (no rectangular overlay box).
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
            if (_card1 != null) _card1.SetDimmed(isFolded);
            if (_card2 != null) _card2.SetDimmed(isFolded);

            float uiAlpha = isFolded ? 0.65f : 1f;

            if (_nameText != null) _nameText.alpha = uiAlpha;
            if (_stackText != null) _stackText.alpha = uiAlpha;
            if (_betText != null) _betText.alpha = uiAlpha;
            if (_actionText != null) _actionText.alpha = uiAlpha;
            if (_avatarInitialText != null) _avatarInitialText.alpha = uiAlpha;

            if (_avatarImage != null)
            {
                var c = _avatarImage.color;
                c.a = uiAlpha;
                _avatarImage.color = c;
            }

            if (_avatarRingImage != null)
            {
                var c = _avatarRingImage.color;
                c.a = isFolded ? 0.75f : 1f;
                _avatarRingImage.color = c;
            }

            if (_betChipImage != null)
            {
                var c = _betChipImage.color;
                c.a = uiAlpha;
                _betChipImage.color = c;
            }
        }
    }
}
