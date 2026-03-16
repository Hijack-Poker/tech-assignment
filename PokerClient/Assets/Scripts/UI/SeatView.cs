using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
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
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Winner")]
        [SerializeField] private TextMeshProUGUI _handRankText;
        [SerializeField] private TextMeshProUGUI _winningsText;

        private static readonly Color NormalColor = new Color(0.12f, 0.12f, 0.2f, 0.9f);
        private static readonly Color AllInColor = new Color(0.8f, 0.6f, 0f);
        private static readonly Color GoldColor = new Color(1f, 0.84f, 0f);

        private Tweener _winnerPulseTween;
        private float _displayedStack;

        public void Render(PlayerState player, GameState game)
        {
            gameObject.SetActive(true);

            _nameText.text = player.Username;
            _betText.text = player.Bet > 0 ? MoneyFormatter.Format(player.Bet) : "";
            _actionText.text = player.Action?.ToUpper() ?? "";

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

            _canvasGroup.alpha = player.IsFolded ? 0.4f : 1f;
            _borderImage.color = player.IsAllIn ? AllInColor : Color.clear;

            bool showCards = game.IsShowdown || player.IsWinner;
            if (player.HasCards && player.Cards.Count >= 2)
            {
                _card1.SetCard(player.Cards[0], showCards);
                _card2.SetCard(player.Cards[1], showCards);
            }
            else
            {
                _card1.SetBlueBack();
                _card2.SetBlueBack();
            }

            // Winner animation
            if (_winnerPulseTween != null) { _winnerPulseTween.Kill(); _winnerPulseTween = null; }

            bool isWinner = player.IsWinner;
            if (isWinner)
            {
                _backgroundImage.color = NormalColor;
                _winnerPulseTween = DOTween.To(
                    () => _backgroundImage.color,
                    c => _backgroundImage.color = c,
                    GoldColor, 0.4f)
                    .SetLoops(6, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);

                _winningsText.text = MoneyFormatter.FormatGain(player.Winnings);
                _winningsText.color = new Color(0.4f, 0.87f, 0.4f);
                _winningsText.transform.localScale = Vector3.zero;
                _winningsText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            }
            else
            {
                _backgroundImage.color = NormalColor;
                _winningsText.text = "";
            }

            _handRankText.text = isWinner && !string.IsNullOrEmpty(player.HandRank) ? player.HandRank : "";
        }

        public void Clear()
        {
            if (_winnerPulseTween != null) { _winnerPulseTween.Kill(); _winnerPulseTween = null; }
            _backgroundImage.color = NormalColor;
            _displayedStack = 0f;
            DOTween.Kill(_stackText);
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            DOTween.Kill(_stackText);
            if (_winnerPulseTween != null) _winnerPulseTween.Kill();
        }
    }
}
