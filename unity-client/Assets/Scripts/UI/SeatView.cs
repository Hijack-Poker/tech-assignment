using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using HijackPoker.Analytics;
using HijackPoker.Animation;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    /// <summary>
    /// Renders a single player seat: avatar, name/stack pill, hole cards, bet,
    /// position badge, action badge, hand rank, and winnings.
    /// Animates transitions: stack/bet tweens, card flips, glow pulses, badge pops.
    /// </summary>
    public class SeatView : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>Fired when a seat is tapped. Passes seat number and canvas position.</summary>
        public event Action<int, Vector2> OnSeatTapped;
        private int _seatNumber;
        private RectTransform _rt;
        private CanvasGroup _canvasGroup;

        // UI elements
        private AvatarCircleView _avatar;
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _stackText;
        private CardView _card1;
        private CardView _card2;
        private TextMeshProUGUI _betText;
        private RectTransform _positionBadge;
        private TextMeshProUGUI _positionBadgeText;
        private Image _positionBadgeBg;
        private RectTransform _actionBadge;
        private TextMeshProUGUI _actionBadgeText;
        private Image _actionBadgeBg;
        private RectTransform _allInBadge;
        private TextMeshProUGUI _handRankText;
        private TextMeshProUGUI _winningsText;
        private Image _activeGlow;
        private CanvasGroup _actionBadgeCg;
        private CanvasGroup _betCg;

        // ── Animation state ─────────────────────────────────────────
        private bool _hasRenderedOnce;
        private float _prevStack;
        private float _prevBet;
        private bool _wasActive;
        private bool _wasWinner;
        private bool _wasFolded;
        private string _prevAction;
        private bool _prevHadWinnings;
        private bool _prevCardsWereFaceDown;
        private float? _deferredStack;

        // Active continuous tweens (cancelled on state change)
        private TweenHandle _activeGlowTween;
        private TweenHandle _winnerGlowTween;
        private TweenHandle _foldGhostTween;
        private int _prevPlayerId = -1;
        private bool _isFoldGhosted;

        // In-flight card animation handles
        private TweenHandle _flipHandle1;
        private TweenHandle _flipHandle2;
        private TweenHandle _foldTiltHandle;
        private TweenHandle _foldFadeHandle;

        // Chip stack, turn timer, session delta, profile badge
        private ChipStackView _chipStack;
        private TurnTimerView _turnTimer;
        private TextMeshProUGUI _sessionDeltaText;
        private GameObject _sessionDeltaRow;
        private GameObject _profileBadge;
        private TextMeshProUGUI _profileBadgeText;
        private Image _profileBadgeBg;

        public int SeatNumber => _seatNumber;
        public RectTransform RectTransform => _rt;
        public CardView Card1 => _card1;
        public CardView Card2 => _card2;
        public CanvasGroup BetCanvasGroup => _betCg;
        public AnimationController AnimController { get; set; }

        private TableTheme _theme;

        public static SeatView Create(int seatNumber, Transform parent, TableTheme theme = null)
        {
            var go = new GameObject($"Seat_{seatNumber}", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = LayoutConfig.SeatSize;

            var view = go.AddComponent<SeatView>();
            view._seatNumber = seatNumber;
            view._rt = rt;
            view._theme = theme;
            view._canvasGroup = go.AddComponent<CanvasGroup>();
            view.BuildUI();
            view.SetVisible(false);

            return view;
        }

        private void BuildUI()
        {
            float avatarSize = LayoutConfig.AvatarSize;
            float avatarRingSize = LayoutConfig.AvatarRingSize;
            var infoSz = LayoutConfig.SeatInfoSize;
            float cardSpacing = LayoutConfig.SeatCardSpacing;
            float cardScale = LayoutConfig.SeatCardScale;
            var cardSz = LayoutConfig.CardSize;
            float scaledCardH = cardSz.y * cardScale;
            float spacing = LayoutConfig.SeatElementSpacing;
            float nameW = LayoutConfig.SeatNameWidth;

            // ── Main column (VerticalLayoutGroup) ─────────────────────────
            var columnGo = new GameObject("Column", typeof(RectTransform));
            columnGo.transform.SetParent(transform, false);
            var columnRt = columnGo.GetComponent<RectTransform>();
            UIFactory.StretchFill(columnRt);

            var vlg = columnGo.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.spacing = spacing;
            vlg.padding = LayoutConfig.SeatPadding;

            // ── Section 1: Avatar ─────────────────────────────────────────
            _avatar = AvatarCircleView.Create(columnGo.transform, avatarRingSize);
            var avatarLE = _avatar.gameObject.AddComponent<LayoutElement>();
            avatarLE.preferredWidth = avatarRingSize;
            avatarLE.preferredHeight = avatarRingSize + 6f;

            // Position badge — top-right corner of avatar
            var posBadgeSz = LayoutConfig.SeatPositionBadgeSize;
            _positionBadge = UIFactory.CreatePanel("PosBadge", _avatar.transform,
                null, posBadgeSz);
            _positionBadge.anchorMin = new Vector2(1f, 1f);
            _positionBadge.anchorMax = new Vector2(1f, 1f);
            _positionBadge.pivot = new Vector2(0.5f, 0.5f);
            _positionBadge.anchoredPosition = new Vector2(2f, -6f);
            _positionBadgeBg = _positionBadge.gameObject.AddComponent<Image>();
            _positionBadgeBg.sprite = TextureGenerator.GetRoundedRect((int)posBadgeSz.x, (int)posBadgeSz.y, 11);
            _positionBadgeBg.type = Image.Type.Sliced;
            _positionBadgeBg.color = UIFactory.AccentGold; // default warm pill
            _positionBadgeText = UIFactory.CreateText("PosBadgeText", _positionBadge,
                "", 11f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.StretchFill(_positionBadgeText.GetComponent<RectTransform>());
            _positionBadge.gameObject.SetActive(false);

            // All-in badge — anchored above avatar
            var allInSz = LayoutConfig.SeatAllInBadgeSize;
            _allInBadge = UIFactory.CreatePanel("AllInBadge", _avatar.transform,
                null, allInSz);
            _allInBadge.anchorMin = new Vector2(0.5f, 1f);
            _allInBadge.anchorMax = new Vector2(0.5f, 1f);
            _allInBadge.pivot = new Vector2(0.5f, 0f);
            _allInBadge.anchoredPosition = Vector2.zero;
            var allInBg = _allInBadge.gameObject.AddComponent<Image>();
            allInBg.color = UIFactory.AccentMagenta;
            allInBg.sprite = TextureGenerator.GetRoundedRect((int)allInSz.x, (int)allInSz.y, 10);
            allInBg.type = Image.Type.Sliced;
            allInBg.raycastTarget = false;
            var allInText = UIFactory.CreateText("AllInText", _allInBadge, "ALL IN",
                10f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.StretchFill(allInText.GetComponent<RectTransform>());
            _allInBadge.gameObject.SetActive(false);

            // ── Section 2: Info panel (name + stack) ─────────────────────
            var infoSection = new GameObject("InfoSection", typeof(RectTransform));
            infoSection.transform.SetParent(columnGo.transform, false);
            var infoLE = infoSection.AddComponent<LayoutElement>();
            infoLE.preferredWidth = infoSz.x;
            infoLE.preferredHeight = infoSz.y;

            // Active glow (halo around info pill, behind everything in this section)
            float cornerRadius = LayoutConfig.SeatInfoCornerRadius;
            _activeGlow = UIFactory.CreateImage("ActiveGlow", infoSection.transform,
                new Color(UIFactory.ActiveGlowCyan.r, UIFactory.ActiveGlowCyan.g,
                    UIFactory.ActiveGlowCyan.b, 0f));
            var glowRt = _activeGlow.GetComponent<RectTransform>();
            UIFactory.StretchFill(glowRt, -4f);
            _activeGlow.sprite = TextureGenerator.GetRoundedRect(64, 64, (int)(cornerRadius + 2));
            _activeGlow.type = Image.Type.Sliced;

            // Dark pill background with subtle gradient (darker at bottom, lighter at top)
            var infoPill = UIFactory.CreateImage("InfoPill", infoSection.transform,
                new Color(0.06f, 0.09f, 0.14f, 0.50f));
            infoPill.sprite = TextureGenerator.GetRoundedRect(64, 64, (int)cornerRadius);
            infoPill.type = Image.Type.Sliced;
            infoPill.raycastTarget = false;
            var infoPillRt = infoPill.GetComponent<RectTransform>();
            UIFactory.StretchFill(infoPillRt);

            // Subtle top-half gradient overlay for depth
            var gradOverlay = UIFactory.CreateImage("GradOverlay", infoSection.transform,
                new Color(1f, 1f, 1f, 0.04f));
            var gradRt = gradOverlay.GetComponent<RectTransform>();
            gradRt.anchorMin = new Vector2(0f, 0.5f);
            gradRt.anchorMax = new Vector2(1f, 1f);
            gradRt.offsetMin = Vector2.zero;
            gradRt.offsetMax = Vector2.zero;
            gradOverlay.sprite = TextureGenerator.GetRoundedRect(64, 32, (int)cornerRadius);
            gradOverlay.type = Image.Type.Sliced;
            gradOverlay.raycastTarget = false;

            // Name (upper half of info panel)
            float nameFontMax = LayoutConfig.SeatNameFontSize;
            _nameText = UIFactory.CreateText("Name", infoSection.transform, "",
                nameFontMax, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            var nameRt = _nameText.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0.5f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.offsetMin = new Vector2(10, 0);
            nameRt.offsetMax = new Vector2(-10, -2);
            _nameText.overflowMode = TextOverflowModes.Ellipsis;
            _nameText.enableAutoSizing = true;
            _nameText.fontSizeMin = 9f;
            _nameText.fontSizeMax = nameFontMax;

            // Stack (lower half of info panel)
            float stackFontMax = LayoutConfig.SeatStackFontSize;
            _stackText = UIFactory.CreateText("Stack", infoSection.transform, "",
                stackFontMax, new Color(0.75f, 0.78f, 0.82f, 1f), TextAlignmentOptions.Center);
            var stackRt = _stackText.GetComponent<RectTransform>();
            stackRt.anchorMin = new Vector2(0f, 0f);
            stackRt.anchorMax = new Vector2(1f, 0.48f);
            stackRt.offsetMin = new Vector2(10, 2);
            stackRt.offsetMax = new Vector2(-10, 0);
            _stackText.overflowMode = TextOverflowModes.Ellipsis;
            _stackText.enableAutoSizing = true;
            _stackText.fontSizeMin = 9f;
            _stackText.fontSizeMax = stackFontMax;

            // ── Session delta row (between info and cards) ─────────────────
            _sessionDeltaRow = new GameObject("DeltaRow", typeof(RectTransform));
            _sessionDeltaRow.transform.SetParent(columnGo.transform, false);
            var deltaRowLE = _sessionDeltaRow.AddComponent<LayoutElement>();
            deltaRowLE.preferredWidth = infoSz.x;
            deltaRowLE.preferredHeight = LayoutConfig.SessionDeltaRowHeight;

            var deltaPillBg = UIFactory.CreateImage("DeltaPill", _sessionDeltaRow.transform,
                new Color(0.06f, 0.09f, 0.14f, 0.40f));
            deltaPillBg.sprite = TextureGenerator.GetRoundedRect(64, 64, (int)cornerRadius);
            deltaPillBg.type = Image.Type.Sliced;
            deltaPillBg.raycastTarget = false;
            var deltaPillRt = deltaPillBg.GetComponent<RectTransform>();
            UIFactory.StretchFill(deltaPillRt);

            _sessionDeltaText = UIFactory.CreateText("SessionDelta", _sessionDeltaRow.transform, "",
                LayoutConfig.SessionDeltaFontSize, UIFactory.AccentGreen,
                TextAlignmentOptions.Center, FontStyles.Bold);
            var deltaRt = _sessionDeltaText.GetComponent<RectTransform>();
            UIFactory.StretchFill(deltaRt);
            _sessionDeltaText.enableAutoSizing = true;
            _sessionDeltaText.fontSizeMin = 9f;
            _sessionDeltaText.fontSizeMax = LayoutConfig.SessionDeltaFontSize;
            _sessionDeltaRow.SetActive(false);

            // ── Profile badge row (play style + VPIP%) ───────────────────────
            _profileBadge = new GameObject("ProfileBadge", typeof(RectTransform));
            _profileBadge.transform.SetParent(columnGo.transform, false);
            var profileLE = _profileBadge.AddComponent<LayoutElement>();
            profileLE.preferredWidth = infoSz.x;
            profileLE.preferredHeight = LayoutConfig.SessionDeltaRowHeight;

            _profileBadgeBg = _profileBadge.AddComponent<Image>();
            _profileBadgeBg.sprite = TextureGenerator.GetRoundedRect(64, 64, (int)cornerRadius);
            _profileBadgeBg.type = Image.Type.Sliced;
            _profileBadgeBg.color = new Color(0.20f, 0.60f, 0.86f, 0.70f);

            _profileBadgeText = UIFactory.CreateText("ProfileText", _profileBadge.transform, "",
                LayoutConfig.SessionDeltaFontSize, Color.white,
                TextAlignmentOptions.Center, FontStyles.Bold);
            var profileTextRt = _profileBadgeText.GetComponent<RectTransform>();
            UIFactory.StretchFill(profileTextRt);
            _profileBadgeText.enableAutoSizing = true;
            _profileBadgeText.fontSizeMin = 8f;
            _profileBadgeText.fontSizeMax = LayoutConfig.SessionDeltaFontSize;
            _profileBadge.SetActive(false);

            // ── Section 3: Cards (fanned layout) ─────────────────────────────
            float fanAngle = LayoutConfig.SeatCardFanAngle;
            float fanYOffset = LayoutConfig.SeatCardFanYOffset;
            float cardsRowW = cardSpacing * 2 + cardSz.x;
            var cardsSection = new GameObject("CardsSection", typeof(RectTransform));
            cardsSection.transform.SetParent(columnGo.transform, false);
            var cardsLE = cardsSection.AddComponent<LayoutElement>();
            cardsLE.preferredWidth = cardsRowW;
            cardsLE.preferredHeight = scaledCardH + LayoutConfig.SeatCardRotationPadding;

            float cardYShift = LayoutConfig.IsPortrait ? -2f : -2f;
            _card1 = CardView.Create("Card1", cardsSection.transform, _theme);
            _card1.RectTransform.anchoredPosition = new Vector2(-cardSpacing, -fanYOffset / 2f + cardYShift);
            _card1.RectTransform.localScale = Vector3.one * cardScale;
            _card1.RectTransform.localEulerAngles = new Vector3(0, 0, fanAngle);

            _card2 = CardView.Create("Card2", cardsSection.transform, _theme);
            _card2.RectTransform.anchoredPosition = new Vector2(cardSpacing, fanYOffset / 2f + cardYShift);
            _card2.RectTransform.localScale = Vector3.one * cardScale;
            _card2.RectTransform.localEulerAngles = new Vector3(0, 0, -fanAngle);

            // ── Bet amount + chip stack ──────────────────────────────────
            var betRow = new GameObject("BetRow", typeof(RectTransform));
            betRow.transform.SetParent(columnGo.transform, false);
            var betRowLE = betRow.AddComponent<LayoutElement>();
            betRowLE.preferredWidth = LayoutConfig.SeatBetWidth;
            betRowLE.preferredHeight = 18;

            _betText = UIFactory.CreateText("Bet", betRow.transform, "",
                stackFontMax, UIFactory.AccentCyan, TextAlignmentOptions.Center, FontStyles.Bold);
            var betRt = _betText.GetComponent<RectTransform>();
            UIFactory.StretchFill(betRt);
            _betCg = betRow.AddComponent<CanvasGroup>();
            _betCg.alpha = 0f;

            // Chip stack below bet text
            _chipStack = ChipStackView.Create(columnGo.transform);
            var chipLE = _chipStack.gameObject.AddComponent<LayoutElement>();
            chipLE.preferredWidth = LayoutConfig.ChipDiameter * 4 + 6;
            chipLE.preferredHeight = LayoutConfig.ChipMaxHeight;

            // Turn timer ring around avatar
            _turnTimer = TurnTimerView.Create(_avatar.transform, LayoutConfig.TimerDiameter);


            // ── Section 4: Action badge (pill with gradient) ──────────────
            var actionSz = LayoutConfig.SeatActionBadgeSize;
            _actionBadge = UIFactory.CreatePanel("ActionBadge", columnGo.transform, null, actionSz);
            var actionLE = _actionBadge.gameObject.AddComponent<LayoutElement>();
            actionLE.preferredWidth = actionSz.x;
            actionLE.preferredHeight = actionSz.y;
            _actionBadgeBg = _actionBadge.gameObject.AddComponent<Image>();
            _actionBadgeBg.color = UIFactory.ActionCall;
            _actionBadgeBg.sprite = TextureGenerator.GetRoundedRect((int)actionSz.x, (int)actionSz.y, 14);
            _actionBadgeBg.type = Image.Type.Sliced;
            _actionBadgeBg.raycastTarget = false;
            _actionBadgeText = UIFactory.CreateText("ActionText", _actionBadge, "",
                12f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.StretchFill(_actionBadgeText.GetComponent<RectTransform>());
            _actionBadgeCg = _actionBadge.gameObject.AddComponent<CanvasGroup>();
            _actionBadgeCg.alpha = 0f;

            // ── Section 5: Hand rank (shown at showdown) ──────────────────
            _handRankText = UIFactory.CreateText("HandRank", columnGo.transform, "",
                14f, UIFactory.AccentGold, TextAlignmentOptions.Center,
                FontStyles.Italic);
            var hrLE = _handRankText.gameObject.AddComponent<LayoutElement>();
            hrLE.preferredWidth = nameW;
            hrLE.preferredHeight = 16;
            _handRankText.gameObject.SetActive(false);

            // ── Winnings (shown at payout) ────────────────────────────────
            _winningsText = UIFactory.CreateText("Winnings", columnGo.transform, "",
                20f, UIFactory.AccentGold, TextAlignmentOptions.Center,
                FontStyles.Bold);
            var wLE = _winningsText.gameObject.AddComponent<LayoutElement>();
            wLE.preferredWidth = nameW;
            wLE.preferredHeight = 22;
            _winningsText.gameObject.SetActive(false);
        }

        public void SetVisible(bool visible)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.blocksRaycasts = visible;
            _canvasGroup.interactable = visible;
        }

        public void UpdateFromState(PlayerState player, GameState game, bool isHumanSeat = false)
        {
            if (player == null)
            {
                SetVisible(false);
                ResetAnimState();
                return;
            }

            SetVisible(true);
            bool animate = _hasRenderedOnce && AnimController != null;

            // Avatar — procedural identicon
            if (player.PlayerId != _prevPlayerId)
            {
                _avatar.UpdatePlayer(player.PlayerId);
                _prevPlayerId = player.PlayerId;
            }

            // Name
            _nameText.text = player.Username ?? "";
            if (player.IsAllIn)
                _nameText.color = UIFactory.AccentMagenta;
            else if (player.IsFolded)
                _nameText.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            else
                _nameText.color = Color.white;

            // Stack (animated tween, deferred when pot fly-in is active)
            float newStack = player.Stack;
            if (_deferredStack.HasValue)
            {
                _stackText.text = MoneyFormatter.Format(_deferredStack.Value);
            }
            else if (animate && Mathf.Abs(newStack - _prevStack) > 0.01f)
            {
                float fromStack = _prevStack;
                AnimController.Play(Tweener.TweenFloat(fromStack, newStack, AnimationConfig.SeatStackTween,
                    v => _stackText.text = MoneyFormatter.Format(v), EaseType.EaseOutCubic));
            }
            else
            {
                _stackText.text = MoneyFormatter.Format(newStack);
            }
            _prevStack = newStack;

            // Fold ghost effect (animated desaturation)
            UpdateFoldGhostEffect(player, animate);

            // Hole cards (with fold animation and flip animation)
            UpdateCardsAnimated(player, game, animate, isHumanSeat);

            // Bet (animated tween)
            UpdateBetAnimated(player, animate);

            // Position badge
            UpdatePositionBadge(player.Seat, game);

            // Action badge (with scale pop)
            UpdateActionBadgeAnimated(player, animate);

            // Persistent ALL-IN badge
            _allInBadge.gameObject.SetActive(player.IsAllIn);

            // Active glow (breathing pulse)
            UpdateActiveGlow(player, game, animate);

            // Winner state (pulsing gold glow)
            UpdateWinnerState(player, game, animate);

            // Collapse invisible bet/action from layout when showing
            // end-of-hand elements, so they don't squish the content.
            bool endOfHand = _handRankText.gameObject.activeSelf
                || _winningsText.gameObject.activeSelf;
            _betText.gameObject.SetActive(!endOfHand);
            _actionBadge.gameObject.SetActive(!endOfHand);

            _hasRenderedOnce = true;
        }

        public void ResetContinuousTweens()
        {
            _activeGlowTween = null;
            _winnerGlowTween = null;
            _deferredStack = null;
            _wasActive = false;
            _wasWinner = false;
        }

        public void DeferNextStackTween()
        {
            _deferredStack = _prevStack;
        }

        public void AnimateDeferredStack()
        {
            if (!_deferredStack.HasValue) return;
            float from = _deferredStack.Value;
            float to = _prevStack;
            _deferredStack = null;

            if (AnimController != null && Mathf.Abs(to - from) > 0.01f)
            {
                AnimController.Play(Tweener.TweenFloat(from, to, AnimationConfig.SeatStackTween,
                    v => _stackText.text = MoneyFormatter.Format(v), EaseType.EaseOutCubic));
            }
            else
            {
                _stackText.text = MoneyFormatter.Format(to);
            }
        }

        // ── Animated sub-updates ────────────────────────────────────

        private void UpdateCardsAnimated(PlayerState player, GameState game, bool animate, bool isHumanSeat = false)
        {
            if (!player.HasCards || game.HandStep < 4 || player.IsFolded)
            {
                if (_foldTiltHandle != null && !_foldTiltHandle.IsComplete)
                    goto TrackState;

                if (player.IsFolded && !_wasFolded && animate
                    && _card1.CurrentState != CardView.State.Empty)
                {
                    AnimateFoldCards();
                }
                else
                {
                    _card1.SetEmpty();
                    _card2.SetEmpty();
                }

                TrackState:
                _wasFolded = player.IsFolded;
                _prevCardsWereFaceDown = false;
                return;
            }

            string card1Str = player.Cards.Count > 0 ? player.Cards[0] : null;
            string card2Str = player.Cards.Count > 1 ? player.Cards[1] : null;

            bool shouldShowFaceUp = ShowdownLogic.ShouldShowCards(
                game.HandStep, player.Status, player.Winnings, isHumanSeat);

            if (shouldShowFaceUp && _prevCardsWereFaceDown && animate)
            {
                if (!string.IsNullOrEmpty(card1Str))
                    _flipHandle1 = _card1.AnimateFlip(card1Str, AnimController);
                if (!string.IsNullOrEmpty(card2Str))
                    _flipHandle2 = _card2.AnimateFlip(card2Str, AnimController);
            }
            else
            {
                bool card1Flipping = _flipHandle1 != null && !_flipHandle1.IsComplete;
                bool card2Flipping = _flipHandle2 != null && !_flipHandle2.IsComplete;

                if (!card1Flipping)
                {
                    if (shouldShowFaceUp && !string.IsNullOrEmpty(card1Str))
                        _card1.SetFaceUp(card1Str);
                    else if (string.IsNullOrEmpty(card1Str))
                        _card1.SetEmpty();
                    else
                        _card1.SetFaceDown();
                }
                if (!card2Flipping)
                {
                    if (shouldShowFaceUp && !string.IsNullOrEmpty(card2Str))
                        _card2.SetFaceUp(card2Str);
                    else if (string.IsNullOrEmpty(card2Str))
                        _card2.SetEmpty();
                    else
                        _card2.SetFaceDown();
                }
            }

            _prevCardsWereFaceDown = !shouldShowFaceUp
                && _card1.CurrentState != CardView.State.Empty;
            _wasFolded = player.IsFolded;
        }

        private void AnimateFoldCards()
        {
            AudioManager.Instance?.Play(SoundType.FoldSwoosh);
            float duration = AnimationConfig.SeatFoldDuration;
            float baseFanAngle = LayoutConfig.SeatCardFanAngle;
            System.Action snapBothToFinal = () =>
            {
                _card1.SetEmpty();
                _card2.SetEmpty();
                _card1.ResetFoldVisuals();
                _card2.ResetFoldVisuals();
                // Restore fan rotation after fold clears
                _card1.RectTransform.localEulerAngles = new Vector3(0, 0, baseFanAngle);
                _card2.RectTransform.localEulerAngles = new Vector3(0, 0, -baseFanAngle);
            };

            // Tilt relative to fan base angle
            _foldTiltHandle = AnimController.Play(Tweener.TweenFloat(0f, 1f, duration,
                v =>
                {
                    _card1.RectTransform.localEulerAngles = new Vector3(0, 0, baseFanAngle + v * 15f);
                    _card2.RectTransform.localEulerAngles = new Vector3(0, 0, -baseFanAngle - v * 15f);
                }, EaseType.EaseOutQuart));
            _foldTiltHandle.SnapToFinal = snapBothToFinal;

            // Fade cards out
            _foldFadeHandle = AnimController.Play(Tweener.TweenFloat(1f, 0f, duration,
                v =>
                {
                    _card1.CanvasGroup.alpha = v;
                    _card2.CanvasGroup.alpha = v;
                }, EaseType.EaseOutQuart));
            _foldFadeHandle.SnapToFinal = snapBothToFinal;
            _foldFadeHandle.OnComplete(snapBothToFinal);
        }

        private void UpdateBetAnimated(PlayerState player, bool animate)
        {
            float newBet = player.Bet;
            if (newBet > 0)
            {
                _betCg.alpha = 1f;
                if (animate && Mathf.Abs(newBet - _prevBet) > 0.01f)
                {
                    float fromBet = _prevBet;
                    AnimController.Play(Tweener.TweenFloat(fromBet, newBet, AnimationConfig.SeatBetTween,
                        v => _betText.text = MoneyFormatter.Format(v)));
                    if (newBet > _prevBet)
                        AudioManager.Instance?.Play(SoundType.ChipClink);
                }
                else
                {
                    _betText.text = MoneyFormatter.Format(newBet);
                }
            }
            else
            {
                _betCg.alpha = 0f;
                _chipStack?.Clear();
            }
            _prevBet = newBet;

            // Update chip stack visualization
            if (newBet >= 1f)
                _chipStack?.UpdateBet(newBet, animate ? AnimController : null);
            else
                _chipStack?.Clear();
        }

        private void UpdateActionBadgeAnimated(PlayerState player, bool animate)
        {
            if (string.IsNullOrEmpty(player.Action))
            {
                _actionBadgeCg.alpha = 0f;
                _prevAction = null;
                return;
            }

            string displayAction = player.Action.ToUpper();
            _actionBadgeText.text = displayAction;

            Color badgeColor;
            switch (player.Action.ToLower())
            {
                case "check": badgeColor = UIFactory.ActionCheck; break;
                case "call": badgeColor = UIFactory.ActionCall; break;
                case "bet":
                case "raise": badgeColor = UIFactory.ActionBet; break;
                case "fold": badgeColor = UIFactory.ActionFold; break;
                case "allin": badgeColor = UIFactory.ActionAllIn; break;
                default: badgeColor = UIFactory.TextMuted; break;
            }
            _actionBadgeBg.color = badgeColor;

            _actionBadgeCg.alpha = 1f;

            bool isNewAction = player.Action != _prevAction;
            if (animate && isNewAction)
            {
                AnimController.Play(Tweener.ScalePop(_actionBadge, AnimationConfig.SeatActionPop, 1.3f));

                // All-in impact effect
                if (player.Action.ToLower() == "allin")
                {
                    var canvas = GetCanvasTransform();
                    if (canvas != null)
                    {
                        var canvasRt = canvas.GetComponent<RectTransform>();
                        Vector2 playerPos = GetCanvasPosition(canvas);
                        AllInImpactEffect.Play(AnimController, canvas, canvasRt, playerPos);
                    }
                }
            }

            _prevAction = player.Action;
        }

        private void UpdateActiveGlow(PlayerState player, GameState game, bool animate)
        {
            bool isActive = game.Move == player.Seat && game.Move > 0;

            bool needsRestart = isActive
                && (_activeGlowTween == null || _activeGlowTween.IsComplete);

            if (isActive && (!_wasActive || needsRestart))
            {
                _activeGlowTween?.Cancel();
                _activeGlowTween = AnimController?.Play(Tweener.PulseGlow(
                    a => _activeGlow.color = new Color(UIFactory.ActiveGlowCyan.r, UIFactory.ActiveGlowCyan.g, UIFactory.ActiveGlowCyan.b, a),
                    0.05f, 0.25f, 1.2f));

                if (_activeGlowTween == null)
                    _activeGlow.color = new Color(UIFactory.ActiveGlowCyan.r, UIFactory.ActiveGlowCyan.g, UIFactory.ActiveGlowCyan.b, 0.10f);

                // Start turn timer
                if (!_wasActive && _turnTimer != null && AnimController != null)
                    _turnTimer.StartTimer(LayoutConfig.TimerDuration, AnimController);
            }
            else if (!isActive && _wasActive)
            {
                _activeGlowTween?.Cancel();
                _activeGlowTween = null;
                _activeGlow.color = new Color(0, 0, 0, 0);

                // Stop turn timer
                _turnTimer?.StopTimer();
            }
            else if (!isActive)
            {
                if (!_wasWinner && !(player.IsWinner && game.HandStep >= 13))
                    _activeGlow.color = new Color(0, 0, 0, 0);
            }

            // Avatar border state
            if (isActive)
                _avatar.Border?.SetState(AvatarBorderController.BorderState.Active, AnimController);
            else if (!isActive && _wasActive && !player.IsFolded)
                _avatar.Border?.SetState(AvatarBorderController.BorderState.Idle, AnimController);

            _wasActive = isActive;
        }

        private void UpdateWinnerState(PlayerState player, GameState game, bool animate)
        {
            bool isWinner = player.IsWinner && game.HandStep >= 13;

            bool needsRestart = isWinner
                && (_winnerGlowTween == null || _winnerGlowTween.IsComplete);

            if (isWinner && (!_wasWinner || needsRestart))
            {
                _activeGlowTween?.Cancel();
                _winnerGlowTween?.Cancel();
                _winnerGlowTween = AnimController?.Play(Tweener.PulseGlow(
                    a => _activeGlow.color = new Color(UIFactory.WinnerGlowGold.r, UIFactory.WinnerGlowGold.g, UIFactory.WinnerGlowGold.b, a),
                    0.06f, 0.35f, 0.8f));

                if (_winnerGlowTween == null)
                    _activeGlow.color = new Color(UIFactory.WinnerGlowGold.r, UIFactory.WinnerGlowGold.g, UIFactory.WinnerGlowGold.b, 0.12f);
            }
            else if (!isWinner && _wasWinner)
            {
                _winnerGlowTween?.Cancel();
                _winnerGlowTween = null;
                _avatar.Border?.SetState(AvatarBorderController.BorderState.Idle, AnimController);
            }

            if (isWinner && !_wasWinner)
                _avatar.Border?.SetState(AvatarBorderController.BorderState.Winner, AnimController);

            // Card edge glow and tiered reveal based on hand strength
            if (isWinner && !_wasWinner && animate)
            {
                var glowColor = CardView.GetHandStrengthColor(player.HandRank);
                _card1.SetWinnerGlow(glowColor, AnimController);
                _card2.SetWinnerGlow(glowColor, AnimController);

                // Tiered card reveal effects
                int tier = HandStrengthClassifier.GetTier(player.HandRank);
                var canvas = GetCanvasTransform();
                CardRevealEffects.PlayReveal(AnimController, canvas, _card1, tier);
                CardRevealEffects.PlayReveal(AnimController, canvas, _card2, tier);
            }
            else if (!isWinner && _wasWinner)
            {
                _card1.ClearEdgeGlow();
                _card2.ClearEdgeGlow();
            }

            // Hand rank (with fade-in) — show for all non-folded players at showdown
            bool atShowdown = game.HandStep >= 13;
            bool showHandRank = atShowdown && !player.IsFolded && !string.IsNullOrEmpty(player.HandRank);
            if (showHandRank)
            {
                _handRankText.text = player.HandRank;
                _handRankText.gameObject.SetActive(true);

                Color rankColor = isWinner ? UIFactory.AccentGold : UIFactory.TextSecondary;
                if (animate && !_wasWinner && isWinner)
                {
                    var colorFaded = new Color(rankColor.r, rankColor.g, rankColor.b, 0f);
                    _handRankText.color = colorFaded;
                    AnimController.Play(Tweener.TweenColor(colorFaded, rankColor,
                        AnimationConfig.SeatHandRankFade, c => { if (_handRankText != null) _handRankText.color = c; }));
                }
                else
                {
                    _handRankText.color = rankColor;
                }
            }
            else
            {
                _handRankText.gameObject.SetActive(false);
            }

            // Winnings (with scale pop — bigger)
            bool hasWinnings = player.Winnings > 0 && game.HandStep >= 14;
            if (hasWinnings)
            {
                _winningsText.text = $"+{MoneyFormatter.Format(player.Winnings)}";
                _winningsText.gameObject.SetActive(true);

                if (animate && !_prevHadWinnings)
                {
                    AnimController.Play(
                        Tweener.ScalePop(_winningsText.transform, AnimationConfig.SeatWinningsPop, 1.3f));
                }
            }
            else
            {
                _winningsText.gameObject.SetActive(false);
            }
            _prevHadWinnings = hasWinnings;

            _wasWinner = isWinner;
        }

        private void UpdatePositionBadge(int seat, GameState game)
        {
            string label = null;
            Color badgeColor = UIFactory.AccentGold;
            if (seat == game.DealerSeat)
            {
                label = "D";
                badgeColor = UIFactory.AccentGold;
            }
            else if (seat == game.SmallBlindSeat)
            {
                label = "SB";
                badgeColor = UIFactory.AccentCyan;
            }
            else if (seat == game.BigBlindSeat)
            {
                label = "BB";
                badgeColor = UIFactory.AccentCyan;
            }

            if (label != null)
            {
                _positionBadge.gameObject.SetActive(true);
                _positionBadgeText.text = label;
                _positionBadgeBg.color = badgeColor;
            }
            else
            {
                _positionBadge.gameObject.SetActive(false);
            }
        }

        private void UpdateFoldGhostEffect(PlayerState player, bool animate)
        {
            if (player.IsFolded && !_isFoldGhosted)
            {
                _isFoldGhosted = true;
                _avatar.Border?.SetState(AvatarBorderController.BorderState.Folded, AnimController);

                if (animate && AnimController != null)
                {
                    // Grey desaturation wipe
                    SparkleEffects.SpawnGlintSweep(_rt, new Color(0.5f, 0.5f, 0.5f, 0.5f),
                        AnimationConfig.SeatFoldGhostSweep, AnimController);

                    // Avatar toward grey
                    AnimController.Play(Tweener.TweenColor(
                        _avatar.AvatarImage.color,
                        new Color(0.4f, 0.4f, 0.45f, 1f),
                        AnimationConfig.SeatFoldGhostDuration,
                        c => { if (_avatar.AvatarImage != null) _avatar.AvatarImage.color = c; }));

                    // Fade to dim
                    _foldGhostTween?.Cancel();
                    _foldGhostTween = AnimController.Play(Tweener.TweenAlpha(
                        _canvasGroup, 1f, 0.35f, AnimationConfig.SeatFoldGhostDuration, EaseType.EaseOutQuart));
                }
                else
                {
                    _canvasGroup.alpha = 0.35f;
                }
            }
            else if (!player.IsFolded && _isFoldGhosted)
            {
                // Recovery on new hand
                _isFoldGhosted = false;
                _foldGhostTween?.Cancel();

                if (animate && AnimController != null)
                {
                    _foldGhostTween = AnimController.Play(Tweener.TweenAlpha(
                        _canvasGroup, _canvasGroup.alpha, 1f, AnimationConfig.SeatFoldRecovery));
                    _avatar.Border?.SetState(AvatarBorderController.BorderState.Idle, AnimController);
                }
                else
                {
                    _canvasGroup.alpha = 1f;
                }
            }
            else if (!player.IsFolded)
            {
                _canvasGroup.alpha = 1f;
            }
        }

        public void UpdateProfileBadge(PlayerProfiler.PlayerProfile profile)
        {
            if (_profileBadge == null) return;

            if (profile == null || profile.HandsTracked < 3 || profile.Style == PlayStyle.Unknown)
            {
                _profileBadge.SetActive(false);
                return;
            }

            _profileBadge.SetActive(true);
            float vpip = profile.HandsTracked > 0
                ? (float)profile.VoluntaryPutInPot / profile.HandsTracked * 100f
                : 0f;
            string label = PlayStyleHelper.GetLabel(profile.Style);
            _profileBadgeText.text = $"{label} {vpip:F0}%";

            Color styleColor = PlayStyleHelper.GetColor(profile.Style);
            _profileBadgeBg.color = new Color(styleColor.r, styleColor.g, styleColor.b, 0.70f);
        }

        public void UpdateSessionDelta(float delta)
        {
            if (Mathf.Abs(delta) < 0.01f)
            {
                _sessionDeltaRow.SetActive(false);
                return;
            }

            _sessionDeltaRow.SetActive(true);
            if (delta > 0)
            {
                _sessionDeltaText.text = $"+${delta:F0}";
                _sessionDeltaText.color = UIFactory.AccentGreen;
            }
            else
            {
                _sessionDeltaText.text = $"-${-delta:F0}";
                _sessionDeltaText.color = UIFactory.AccentMagenta;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            var canvas = GetCanvasTransform();
            if (canvas != null)
            {
                Vector2 pos = GetCanvasPosition(canvas);
                OnSeatTapped?.Invoke(_seatNumber, pos);
            }
        }

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
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRt, RectTransformUtility.WorldToScreenPoint(null, worldPos),
                null, out Vector2 localPoint);
            return localPoint;
        }

        private void ResetAnimState()
        {
            _hasRenderedOnce = false;
            _prevStack = 0;
            _prevBet = 0;
            _wasActive = false;
            _wasWinner = false;
            _wasFolded = false;
            _prevAction = null;
            _prevHadWinnings = false;
            _prevCardsWereFaceDown = false;
            _deferredStack = null;
            _prevPlayerId = -1;
            _isFoldGhosted = false;

            _activeGlowTween?.Cancel();
            _activeGlowTween = null;
            _winnerGlowTween?.Cancel();
            _winnerGlowTween = null;
            _foldGhostTween?.Cancel();
            _foldGhostTween = null;
            _flipHandle1 = null;
            _flipHandle2 = null;
            _foldTiltHandle = null;
            _foldFadeHandle = null;

            _chipStack?.Clear();
            _turnTimer?.StopTimer();
            _sessionDeltaRow?.SetActive(false);
            _profileBadge?.SetActive(false);
        }
    }
}
