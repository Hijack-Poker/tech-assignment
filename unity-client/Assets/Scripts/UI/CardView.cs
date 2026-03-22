using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using HijackPoker.Animation;
using HijackPoker.Managers;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    /// <summary>
    /// Renders a single playing card: face-up (rank + suit on themed card face),
    /// face-down (gradient with centered geometric mark and accent border),
    /// or empty placeholder (dashed outline).
    /// Simplified from 7 card face layers to 4. Supports dark card face for Noir theme.
    /// </summary>
    public class CardView : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>Fired when a face-up card is clicked. Passes the card string.</summary>
        public event Action<string> OnCardClicked;
        public static Vector2 CardSize => LayoutConfig.CardSize;

        private Image _background;
        private TextMeshProUGUI _rankText;
        private TextMeshProUGUI _suitText;
        private TextMeshProUGUI _cornerText;
        private Image _backOverlay;
        private Image _placeholderOutline;
        private Image _shadow;
        private Image _shadowAmbient;
        private RectTransform _rt;
        private CanvasGroup _canvasGroup;
        private Image _flipEdgeHighlight;
        private Image _edgeGlow;
        private TableTheme _theme;

        public RectTransform RectTransform => _rt;
        public CanvasGroup CanvasGroup => _canvasGroup;

        public enum State { Empty, FaceDown, FaceUp }

        private State _currentState = State.Empty;
        private string _cardString;
        public State CurrentState => _currentState;

        public static CardView Create(string name, Transform parent, TableTheme theme = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = CardSize;

            var view = go.AddComponent<CardView>();
            view._rt = rt;
            view._theme = theme ?? TableTheme.ForTable(1);
            view._canvasGroup = go.AddComponent<CanvasGroup>();
            view.BuildUI();
            view.SetEmpty();
            return view;
        }

        private void BuildUI()
        {
            // Padding adds transparent border around card sprites so the visible
            // edge sits inside the quad — eliminates jagged edges on rotated cards.
            const int CardPad = 3;
            var cardSprite = TextureGenerator.GetRoundedRect(128, 192, 8, CardPad);
            var utilSprite = TextureGenerator.GetRoundedRect(128, 192, 8);

            // Ambient shadow (soft, spread — Layer 2)
            _shadowAmbient = UIFactory.CreateImage("ShadowAmbient", transform,
                new Color(0, 0, 0, 0.06f), CardSize);
            _shadowAmbient.sprite = utilSprite;
            _shadowAmbient.type = Image.Type.Sliced;
            var shadowAmbRt = _shadowAmbient.GetComponent<RectTransform>();
            UIFactory.StretchFill(shadowAmbRt);
            shadowAmbRt.offsetMin = new Vector2(-4, -7);
            shadowAmbRt.offsetMax = new Vector2(4, 1);

            // Drop shadow — tight (Layer 1)
            _shadow = UIFactory.CreateImage("Shadow", transform,
                new Color(0, 0, 0, 0.18f), CardSize);
            _shadow.sprite = utilSprite;
            _shadow.type = Image.Type.Sliced;
            var shadowRt = _shadow.GetComponent<RectTransform>();
            UIFactory.StretchFill(shadowRt);
            shadowRt.offsetMin = new Vector2(-1, -4);
            shadowRt.offsetMax = new Vector2(1, 0);

            // Placeholder outline (dashed border effect — subtle dim outline)
            _placeholderOutline = UIFactory.CreateImage("Outline", transform,
                UIFactory.CardPlaceholderOutline, CardSize);
            _placeholderOutline.sprite = utilSprite;
            _placeholderOutline.type = Image.Type.Sliced;
            var outlineRt = _placeholderOutline.GetComponent<RectTransform>();
            UIFactory.StretchFill(outlineRt);

            var innerPlaceholder = UIFactory.CreateImage("InnerPlaceholder", _placeholderOutline.transform,
                UIFactory.CardPlaceholderFill);
            innerPlaceholder.sprite = utilSprite;
            innerPlaceholder.type = Image.Type.Sliced;
            var ipRt = innerPlaceholder.GetComponent<RectTransform>();
            ipRt.anchorMin = Vector2.zero;
            ipRt.anchorMax = Vector2.one;
            ipRt.offsetMin = new Vector2(2, 2);
            ipRt.offsetMax = new Vector2(-2, -2);

            // Card background (face-up) — themed card face
            // Expanded by CardPad on each side; padded sprite keeps visible shape at original size.
            _background = UIFactory.CreateImage("CardBg", transform,
                UIFactory.CardBgFaceUp * _theme.CardFaceTint, CardSize);
            _background.sprite = cardSprite;
            _background.type = Image.Type.Sliced;
            var bgRt = _background.GetComponent<RectTransform>();
            UIFactory.StretchFill(bgRt);
            bgRt.offsetMin = new Vector2(-CardPad, -CardPad);
            bgRt.offsetMax = new Vector2(CardPad, CardPad);

            // Subtle border tint on face-up card (used for flip edge highlight)
            _flipEdgeHighlight = UIFactory.CreateImage("CardEdge", _background.transform,
                UIFactory.CardEdgeHighlight);
            _flipEdgeHighlight.sprite = cardSprite;
            _flipEdgeHighlight.type = Image.Type.Sliced;
            var edgeRt = _flipEdgeHighlight.GetComponent<RectTransform>();
            UIFactory.StretchFill(edgeRt);

            // Corner pip (top-left): rank + suit
            Color defaultTextColor = _theme.IsDarkCardFace ? UIFactory.TextPrimary : UIFactory.CardBlack;
            _cornerText = UIFactory.CreateText("Corner", _background.transform, "",
                12f, defaultTextColor, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            _cornerText.lineSpacing = -25;
            var cornerRt = _cornerText.GetComponent<RectTransform>();
            cornerRt.anchorMin = Vector2.zero;
            cornerRt.anchorMax = Vector2.one;
            cornerRt.offsetMin = new Vector2(4 + CardPad, CardPad);
            cornerRt.offsetMax = new Vector2(-CardPad, -3 - CardPad);

            // Center suit pip — larger decorative element
            _suitText = UIFactory.CreateText("Suit", _background.transform, "",
                30f, defaultTextColor, TextAlignmentOptions.Center);
            var suitRt = _suitText.GetComponent<RectTransform>();
            suitRt.anchorMin = new Vector2(0, 0.18f);
            suitRt.anchorMax = new Vector2(1, 0.82f);
            suitRt.offsetMin = new Vector2(CardPad, 0);
            suitRt.offsetMax = new Vector2(-CardPad, 0);

            // Corner pip (bottom-right, rotated 180°): rank + suit
            _rankText = UIFactory.CreateText("CornerBR", _background.transform, "",
                12f, defaultTextColor, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            _rankText.lineSpacing = -25;
            var rankRt = _rankText.GetComponent<RectTransform>();
            rankRt.anchorMin = Vector2.zero;
            rankRt.anchorMax = Vector2.one;
            rankRt.offsetMin = new Vector2(CardPad, 5 + CardPad);
            rankRt.offsetMax = new Vector2(-5 - CardPad, -CardPad);
            rankRt.localEulerAngles = new Vector3(0, 0, 180);

            // ── Face-down overlay — simplified card back ──────────────
            // Gradient back (CardBackPrimary → CardBackSecondary)
            _backOverlay = UIFactory.CreateImage("Back", transform, Color.white, CardSize);
            _backOverlay.sprite = TextureGenerator.GetVerticalGradient(
                128, 192, _theme.CardBackPrimary, _theme.CardBackSecondary, 8, CardPad);
            _backOverlay.type = Image.Type.Sliced;
            var backRt = _backOverlay.GetComponent<RectTransform>();
            UIFactory.StretchFill(backRt);
            backRt.offsetMin = new Vector2(-CardPad, -CardPad);
            backRt.offsetMax = new Vector2(CardPad, CardPad);

            // Accent border on card back (higher contrast: 50% alpha)
            var backBorder = UIFactory.CreateImage("BackBorder", _backOverlay.transform,
                _theme.CardBackAccent);
            backBorder.sprite = cardSprite;
            backBorder.type = Image.Type.Sliced;
            var bbRt = backBorder.GetComponent<RectTransform>();
            UIFactory.StretchFill(bbRt, 2);

            // Centered geometric mark (replacing mandala + diamond + inner layers)
            int markSides = GetMarkSides(_theme.Name);
            bool markOutline = _theme.Name == "The Noir";
            var markColor = new Color(_theme.CardBackAccent.r, _theme.CardBackAccent.g,
                _theme.CardBackAccent.b, 0.6f);
            var backMark = UIFactory.CreateImage("BackMark", _backOverlay.transform, Color.white);
            backMark.sprite = TextureGenerator.GetCardBackMark(128, 192, 4,
                markColor, markSides, markOutline);
            backMark.type = Image.Type.Sliced;
            var bmRt = backMark.GetComponent<RectTransform>();
            bmRt.anchorMin = new Vector2(0.04f, 0.04f);
            bmRt.anchorMax = new Vector2(0.96f, 0.96f);
            bmRt.offsetMin = Vector2.zero;
            bmRt.offsetMax = Vector2.zero;

            // Edge glow (for hand-strength visualization)
            _edgeGlow = UIFactory.CreateImage("EdgeGlow", transform,
                new Color(1, 1, 1, 0));
            _edgeGlow.sprite = TextureGenerator.GetGlowBorder(128, 192, 8, 6f, 4f);
            _edgeGlow.type = Image.Type.Sliced;
            var egRt = _edgeGlow.GetComponent<RectTransform>();
            UIFactory.StretchFill(egRt);
            egRt.offsetMin = new Vector2(-4, -4);
            egRt.offsetMax = new Vector2(4, 4);
        }

        private static int GetMarkSides(string themeName)
        {
            return themeName switch
            {
                "The Classic" => 6,    // 6-petal rosette
                "The Sapphire" => 4,   // 4-pointed star
                "The Velvet" => 8,     // 8-petal rosette
                "The Noir" => 4,       // diamond outline
                _ => 6
            };
        }

        public void SetEmpty()
        {
            _currentState = State.Empty;
            _shadow.gameObject.SetActive(false);
            _shadowAmbient.gameObject.SetActive(false);
            _placeholderOutline.gameObject.SetActive(false);
            _background.gameObject.SetActive(false);
            _backOverlay.gameObject.SetActive(false);
            ClearEdgeGlow();
        }

        public void SetFaceDown()
        {
            _currentState = State.FaceDown;
            _shadow.gameObject.SetActive(true);
            _shadowAmbient.gameObject.SetActive(true);
            _placeholderOutline.gameObject.SetActive(false);
            _background.gameObject.SetActive(false);
            _backOverlay.gameObject.SetActive(true);
        }

        public void SetFaceUp(string cardString)
        {
            _currentState = State.FaceUp;
            _cardString = cardString;
            _shadow.gameObject.SetActive(true);
            _shadowAmbient.gameObject.SetActive(true);
            _placeholderOutline.gameObject.SetActive(false);
            _background.gameObject.SetActive(true);
            _backOverlay.gameObject.SetActive(false);

            ParsedCard parsed;
            try
            {
                parsed = CardUtils.Parse(cardString);
            }
            catch (System.ArgumentException)
            {
                SetEmpty();
                return;
            }

            // Support 4-color suits and dark card face
            var textColor = UIFactory.GetSuitColor(parsed.Symbol[0]);
            if (_theme.IsDarkCardFace)
            {
                // On dark card face, lighten non-red suits for readability
                float lum = textColor.r * 0.299f + textColor.g * 0.587f + textColor.b * 0.114f;
                if (lum < 0.3f)
                    textColor = Color.Lerp(textColor, UIFactory.TextPrimary, 0.7f);
            }

            _cornerText.text = $"{parsed.Rank}\n{parsed.Symbol}";
            _cornerText.color = textColor;

            _suitText.text = parsed.Symbol;
            _suitText.color = textColor;

            // Bottom-right corner (rotated 180°)
            _rankText.text = $"{parsed.Rank}\n{parsed.Symbol}";
            _rankText.color = textColor;
        }

        /// <summary>
        /// Set card state from game data: decides face-up vs face-down using ShowdownLogic.
        /// </summary>
        public void SetFromPlayerData(string cardString, int handStep, string status, float winnings)
        {
            if (string.IsNullOrEmpty(cardString))
            {
                SetEmpty();
                return;
            }

            bool showFaceUp = ShowdownLogic.ShouldShowCards(handStep, status, winnings);
            if (showFaceUp)
                SetFaceUp(cardString);
            else
                SetFaceDown();
        }

        /// <summary>
        /// Animated flip from face-down to face-up with 3D perspective tilt
        /// and card reveal burst. Duration reduced from 0.4s to 0.25s for snappier feel.
        /// </summary>
        public TweenHandle AnimateFlip(string cardString, AnimationController anim)
        {
            return anim.Play(Tweener.FlipCard3D(_rt, () =>
            {
                SetFaceUp(cardString);
                AudioManager.Instance?.Play(SoundType.CardFlip);
                TriggerRevealBurst(anim);
            }, 0.25f, _flipEdgeHighlight));
        }

        /// <summary>
        /// Resets card visual state after a fold animation (alpha, rotation).
        /// </summary>
        public void ResetFoldVisuals()
        {
            _canvasGroup.alpha = 1f;
            _rt.localEulerAngles = Vector3.zero;
        }

        // ── Card Reveal Burst ────────────────────────────────────────

        private void TriggerRevealBurst(AnimationController anim)
        {
            if (anim == null) return;
            var canvas = GetCanvasTransform();
            if (canvas == null) return;

            Vector2 pos = GetCanvasPosition(canvas);
            SparkleEffects.SpawnCardRevealBurst(canvas, pos, anim);
        }

        // ── Hand-Strength Edge Glow ─────────────────────────────────

        private TweenHandle _edgeGlowTween;

        public void SetWinnerGlow(Color color, AnimationController anim)
        {
            if (_edgeGlow == null) return;
            _edgeGlowTween?.Cancel();
            _edgeGlow.color = new Color(color.r, color.g, color.b, 0.3f);

            if (anim != null)
            {
                _edgeGlowTween = anim.Play(Tweener.PulseGlow(
                    a => { if (_edgeGlow != null) _edgeGlow.color = new Color(color.r, color.g, color.b, a); },
                    0.3f, 0.85f, 1.0f));
            }
        }

        public void SetLoserDim()
        {
            if (_edgeGlow == null) return;
            _edgeGlowTween?.Cancel();
            _edgeGlowTween = null;
            _edgeGlow.color = new Color(0.5f, 0.5f, 0.5f, 0.15f);
        }

        public void ClearEdgeGlow()
        {
            _edgeGlowTween?.Cancel();
            _edgeGlowTween = null;
            if (_edgeGlow != null)
                _edgeGlow.color = new Color(1, 1, 1, 0);
        }

        public static Color GetHandStrengthColor(string handRank)
        {
            if (string.IsNullOrEmpty(handRank)) return new Color(0.3f, 0.8f, 1f, 1f);
            string lower = handRank.ToLower();
            if (lower.Contains("royal") || lower.Contains("straight flush"))
                return new Color(0.65f, 0.3f, 0.95f, 1f); // purple
            if (lower.Contains("four"))
                return new Color(1f, 0.55f, 0.1f, 1f); // orange
            if (lower.Contains("full house") || lower.Contains("flush"))
                return new Color(1f, 0.84f, 0f, 1f); // gold
            return new Color(0.3f, 0.8f, 1f, 1f); // cyan
        }

        // ── Click handler ────────────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_currentState == State.FaceUp && !string.IsNullOrEmpty(_cardString))
            {
                eventData.Use();
                OnCardClicked?.Invoke(_cardString);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────

        private Transform GetCanvasTransform()
        {
            var t = transform;
            while (t != null)
            {
                if (t.GetComponent<Canvas>() != null)
                    return t;
                t = t.parent;
            }
            return null;
        }

        private Vector2 GetCanvasPosition(Transform canvas)
        {
            if (_rt == null) return Vector2.zero;
            var canvasRt = canvas.GetComponent<RectTransform>();
            if (canvasRt == null) return Vector2.zero;

            Vector3 worldPos = _rt.position;
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRt, RectTransformUtility.WorldToScreenPoint(null, worldPos),
                null, out localPoint);
            return localPoint;
        }
    }
}
