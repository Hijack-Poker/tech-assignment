using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    public class CardView : MonoBehaviour
    {
        [SerializeField] private Image _cardImage;
        [SerializeField] private Color _foldDimTint = new Color(0.72f, 0.74f, 0.78f, 1f);

        private static readonly Color EmptyColor = new Color(0.15f, 0.15f, 0.2f, 0.3f);

        // Rotating blue backs for empty/starting state
        private static readonly string[] BlueBackNames = { "cardBack_blue5", "cardBack_blue2", "cardBack_blue4" };
        private static Sprite[] _blueBackSprites;
        private static int _blueBackIndex;

        // Red back for face-down during play
        private static Sprite _redBack;

        private bool _wasFaceDown = true;
        private bool _isDimmed;
        private Color _baseColor = Color.white;

        private static readonly System.Collections.Generic.Dictionary<string, string> SuitNames = new()
        {
            { "H", "Hearts" }, { "D", "Diamonds" }, { "C", "Clubs" }, { "S", "Spades" }
        };

        private static void EnsureSpritesLoaded()
        {
            if (_blueBackSprites == null)
            {
                _blueBackSprites = new Sprite[BlueBackNames.Length];
                for (int i = 0; i < BlueBackNames.Length; i++)
                    _blueBackSprites[i] = Resources.Load<Sprite>($"Cards/{BlueBackNames[i]}");
            }
            if (_redBack == null)
                _redBack = Resources.Load<Sprite>("Cards/cardBack_red2");
        }

        /// <summary>
        /// Show a blue card back as placeholder (before cards are dealt).
        /// Rotates through blue5, blue2, blue4.
        /// </summary>
        public void SetBlueBack()
        {
            gameObject.SetActive(true);
            EnsureSpritesLoaded();
            int idx = _blueBackIndex % _blueBackSprites.Length;
            var sprite = _blueBackSprites[idx];
            _blueBackIndex++;
            if (sprite != null)
            {
                _cardImage.sprite = sprite;
                SetBaseColor(Color.white);
            }
            else
            {
                _cardImage.sprite = null;
                SetBaseColor(new Color(0.1f, 0.2f, 0.5f));
            }
            _wasFaceDown = true;
        }

        public void SetCard(string cardCode, bool faceUp)
        {
            gameObject.SetActive(true);

            bool shouldFlip = _wasFaceDown && faceUp;
            _wasFaceDown = !faceUp;

            if (shouldFlip)
            {
                DOTween.Kill(transform);
                transform.DOScaleX(0f, 0.12f).SetEase(Ease.InSine).OnComplete(() =>
                {
                    ApplySprite(cardCode, true);
                    transform.DOScaleX(1f, 0.12f).SetEase(Ease.OutSine);
                });
            }
            else
            {
                ApplySprite(cardCode, faceUp);
            }
        }

        public void SetEmpty()
        {
            gameObject.SetActive(true);
            _wasFaceDown = true;
            _cardImage.sprite = null;
            SetBaseColor(EmptyColor);
        }

        public void SetDimmed(bool dimmed)
        {
            _isDimmed = dimmed;
            RefreshVisualColor();
        }

        private void ApplySprite(string cardCode, bool faceUp)
        {
            EnsureSpritesLoaded();

            if (faceUp)
            {
                var (rank, suit) = CardUtils.ParseCard(cardCode);
                string suitName = SuitNames.ContainsKey(suit) ? SuitNames[suit] : suit;
                string spriteName = $"card{suitName}{rank}";
                var sprite = Resources.Load<Sprite>($"Cards/{spriteName}");
                if (sprite != null)
                {
                    _cardImage.sprite = sprite;
                    SetBaseColor(Color.white);
                }
                else
                {
                    _cardImage.sprite = null;
                    SetBaseColor(new Color(1f, 0.98f, 0.94f));
                    Debug.LogWarning($"Card sprite not found: Cards/{spriteName}");
                }
            }
            else
            {
                if (_redBack != null)
                {
                    _cardImage.sprite = _redBack;
                    SetBaseColor(Color.white);
                }
                else
                {
                    _cardImage.sprite = null;
                    SetBaseColor(new Color(0.5f, 0.1f, 0.1f));
                }
            }
        }

        private void SetBaseColor(Color color)
        {
            _baseColor = color;
            RefreshVisualColor();
        }

        private void RefreshVisualColor()
        {
            if (_cardImage == null) return;

            if (_isDimmed)
            {
                _cardImage.color = new Color(
                    _baseColor.r * _foldDimTint.r,
                    _baseColor.g * _foldDimTint.g,
                    _baseColor.b * _foldDimTint.b,
                    _baseColor.a);
            }
            else
            {
                _cardImage.color = _baseColor;
            }
        }

        private void OnDestroy()
        {
            DOTween.Kill(transform);
        }
    }
}
