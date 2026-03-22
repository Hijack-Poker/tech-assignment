using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using HijackPoker.Animation;
using HijackPoker.Models;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    /// <summary>
    /// Immersive mini-table preview card for the lobby. The card IS the table —
    /// green felt fills the entire surface, info floats as translucent pills,
    /// clicking the card spectates, and a small SIT pill joins the game.
    /// </summary>
    public class TablePreviewCard : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public event Action<int> OnClicked;

        private int _tableId;
        private RectTransform _rt;
        private AnimationController _animController;
        private TextMeshProUGUI _namePillText;
        private TextMeshProUGUI _playerCountText;
        private TextMeshProUGUI _potText;
        private TextMeshProUGUI _infoPillText;
        private AvatarCircleView[] _seatAvatars;
        private Image[] _seatGlows;
        private TextMeshProUGUI[] _seatNameLabels;
        private CardView[] _miniCardViews;
        private Image _cardBg;
        private Image _glowBorder;
        private TextMeshProUGUI _statusText;
        private GameObject _autoPlayBadge;
        private bool _hasActivePlayers;

        // Kept for UpdateFromState data flow
        private string _currentTableName;
        private int _currentHandNo;
        private string _currentBlinds = "";
        private string _currentPhase = "";

        // --- Immersive felt palette ---
        private TableTheme _theme;
        private static readonly Color PillBg = new Color(0f, 0f, 0f, 0.45f);

        private static readonly Color EmptySeatColor = new Color(1f, 1f, 1f, 0.15f);
        private static readonly Color ActiveSeatColor = UIFactory.AccentCyan;
        private static readonly Color WinnerSeatColor = UIFactory.AccentGold;

        public static TablePreviewCard Create(int tableId, Transform parent,
            AnimationController anim = null, TableTheme theme = null)
        {
            var go = new GameObject($"TablePreview_{tableId}", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();

            var view = go.AddComponent<TablePreviewCard>();
            view._tableId = tableId;
            view._rt = rt;
            view._animController = anim;
            view._theme = theme ?? TableTheme.ForTable(tableId);
            view.BuildUI();
            return view;
        }

        private void BuildUI()
        {
            // Drop shadow (extends 12px beyond card edges for elevation)
            var shadow = UIFactory.CreateImage("Shadow", transform, Color.white);
            shadow.sprite = TextureGenerator.GetSoftShadow(128, 128, 16, 12, 0.2f);
            shadow.type = Image.Type.Sliced;
            var shadowRt = shadow.GetComponent<RectTransform>();
            UIFactory.StretchFill(shadowRt, -12);

            // Border glow (extends 6px beyond card, subtle cyan)
            _glowBorder = UIFactory.CreateImage("GlowBorder", transform, _theme.GlowBorder);
            _glowBorder.sprite = TextureGenerator.GetGlowBorder(128, 128, 16, 8f, 6f);
            _glowBorder.type = Image.Type.Sliced;
            var glowRt = _glowBorder.GetComponent<RectTransform>();
            UIFactory.StretchFill(glowRt, -6);

            // Card background — full felt green
            _cardBg = gameObject.AddComponent<Image>();
            _cardBg.sprite = TextureGenerator.GetRoundedRect(128, 128, 16);
            _cardBg.type = Image.Type.Sliced;
            _cardBg.color = _theme.PreviewFeltBase;

            // Radial highlight — natural light pooling at center
            var radial = UIFactory.CreateImage("RadialHighlight", transform, Color.white);
            radial.sprite = TextureGenerator.GetRadialGradient(256,
                _theme.PreviewFeltHighlight, new Color(0f, 0f, 0f, 0f));
            radial.type = Image.Type.Simple;
            radial.preserveAspect = false;
            radial.raycastTarget = false;
            var radialRt = radial.GetComponent<RectTransform>();
            radialRt.anchorMin = new Vector2(0.1f, 0.1f);
            radialRt.anchorMax = new Vector2(0.9f, 0.9f);
            radialRt.offsetMin = Vector2.zero;
            radialRt.offsetMax = Vector2.zero;

            // Vignette — subtle edge darkening like a table rail shadow
            var vignette = UIFactory.CreateImage("Vignette", transform, Color.white);
            vignette.sprite = TextureGenerator.GetVignette(256, 256, 0.4f);
            vignette.type = Image.Type.Simple;
            vignette.preserveAspect = false;
            vignette.raycastTarget = false;
            var vignetteRt = vignette.GetComponent<RectTransform>();
            UIFactory.StretchFill(vignetteRt);

            // --- Seat avatars spread across full card ---
            _seatAvatars = new AvatarCircleView[6];
            _seatGlows = new Image[6];
            _seatNameLabels = new TextMeshProUGUI[6];
            float dotSize = 22f;
            Vector2[] seatAnchors = new Vector2[]
            {
                new Vector2(0.14f, 0.82f),  // seat 1: top-left
                new Vector2(0.50f, 0.88f),  // seat 2: top-center
                new Vector2(0.86f, 0.82f),  // seat 3: top-right
                new Vector2(0.86f, 0.18f),  // seat 4: bottom-right
                new Vector2(0.50f, 0.14f),  // seat 5: bottom-center
                new Vector2(0.14f, 0.18f),  // seat 6: bottom-left
            };

            for (int i = 0; i < 6; i++)
            {
                // Soft glow behind avatar (initially invisible)
                var glow = UIFactory.CreateImage($"SeatGlow{i + 1}", transform,
                    new Color(ActiveSeatColor.r, ActiveSeatColor.g, ActiveSeatColor.b, 0f),
                    new Vector2(dotSize * 2.0f, dotSize * 2.0f));
                glow.sprite = TextureGenerator.GetCircle((int)(dotSize * 10));
                var seatGlowRt = glow.GetComponent<RectTransform>();
                seatGlowRt.anchorMin = seatAnchors[i];
                seatGlowRt.anchorMax = seatAnchors[i];
                seatGlowRt.pivot = new Vector2(0.5f, 0.5f);
                seatGlowRt.anchoredPosition = Vector2.zero;
                _seatGlows[i] = glow;

                var avatar = AvatarCircleView.Create(transform, dotSize, includeBorder: false);
                var avatarRt = avatar.GetComponent<RectTransform>();
                avatarRt.anchorMin = seatAnchors[i];
                avatarRt.anchorMax = seatAnchors[i];
                avatarRt.pivot = new Vector2(0.5f, 0.5f);
                avatarRt.anchoredPosition = Vector2.zero;
                avatar.SetEmpty(EmptySeatColor);
                _seatAvatars[i] = avatar;

                var nameLabel = UIFactory.CreateText($"SeatName{i + 1}", transform, "",
                    7f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
                var nameLabelRt = nameLabel.GetComponent<RectTransform>();
                nameLabelRt.anchorMin = seatAnchors[i];
                nameLabelRt.anchorMax = seatAnchors[i];
                nameLabelRt.pivot = new Vector2(0.5f, 1f);
                nameLabelRt.anchoredPosition = new Vector2(0f, -(dotSize * 0.6f));
                nameLabelRt.sizeDelta = new Vector2(52f, 12f);
                nameLabel.enableAutoSizing = true;
                nameLabel.fontSizeMin = 6f;
                nameLabel.fontSizeMax = 9f;
                _seatNameLabels[i] = nameLabel;
            }

            // Community cards row (centered in card, 42%-58% vertical)
            var cardsRow = new GameObject("CardsRow", typeof(RectTransform));
            cardsRow.transform.SetParent(transform, false);
            var cardsRowRt = cardsRow.GetComponent<RectTransform>();
            cardsRowRt.anchorMin = new Vector2(0.15f, 0.42f);
            cardsRowRt.anchorMax = new Vector2(0.85f, 0.58f);
            cardsRowRt.offsetMin = Vector2.zero;
            cardsRowRt.offsetMax = Vector2.zero;

            var cardsHlg = cardsRow.AddComponent<HorizontalLayoutGroup>();
            cardsHlg.spacing = 2;
            cardsHlg.childAlignment = TextAnchor.MiddleCenter;
            cardsHlg.childControlWidth = false;
            cardsHlg.childControlHeight = false;
            cardsHlg.childForceExpandWidth = false;
            cardsHlg.childForceExpandHeight = false;

            _miniCardViews = new CardView[5];
            float scale = 0.80f;
            Vector2 scaledSize = CardView.CardSize * scale;
            for (int i = 0; i < 5; i++)
            {
                var cardView = CardView.Create($"MiniCard{i}", cardsRow.transform, _theme);
                cardView.RectTransform.localScale = Vector3.one * scale;
                cardView.RectTransform.sizeDelta = CardView.CardSize;
                var le = cardView.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = scaledSize.x;
                le.preferredHeight = scaledSize.y;
                _miniCardViews[i] = cardView;
            }

            // Pot text — floating gold text centered below community cards
            _potText = UIFactory.CreateText("Pot", transform, "",
                14f, UIFactory.AccentGold, TextAlignmentOptions.Center, FontStyles.Bold);
            _potText.raycastTarget = false;
            var potRt = _potText.GetComponent<RectTransform>();
            potRt.anchorMin = new Vector2(0.2f, 0.33f);
            potRt.anchorMax = new Vector2(0.8f, 0.42f);
            potRt.offsetMin = Vector2.zero;
            potRt.offsetMax = Vector2.zero;

            // --- Floating overlay pills ---

            // Name pill (top-left): table name + hand #
            var namePillBg = UIFactory.CreateImage("NamePill", transform, PillBg);
            namePillBg.sprite = TextureGenerator.GetRoundedRect(64, 32, 10);
            namePillBg.type = Image.Type.Sliced;
            namePillBg.raycastTarget = false;
            var namePillRt = namePillBg.GetComponent<RectTransform>();
            namePillRt.anchorMin = new Vector2(0f, 1f);
            namePillRt.anchorMax = new Vector2(0f, 1f);
            namePillRt.pivot = new Vector2(0f, 1f);
            namePillRt.anchoredPosition = new Vector2(8, -8);
            namePillRt.sizeDelta = new Vector2(140, 22);
            var namePillFitter = namePillBg.gameObject.AddComponent<ContentSizeFitter>();
            namePillFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            namePillFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            var namePillHlg = namePillBg.gameObject.AddComponent<HorizontalLayoutGroup>();
            namePillHlg.padding = new RectOffset(8, 8, 2, 2);
            namePillHlg.childAlignment = TextAnchor.MiddleLeft;
            namePillHlg.childControlWidth = false;
            namePillHlg.childControlHeight = true;
            namePillHlg.childForceExpandWidth = false;
            namePillHlg.childForceExpandHeight = true;

            _namePillText = UIFactory.CreateText("NameLabel", namePillBg.transform,
                $"Table {_tableId}", 12f, Color.white, TextAlignmentOptions.MidlineLeft,
                FontStyles.Bold);
            _namePillText.raycastTarget = false;
            var nameLabelLe = _namePillText.gameObject.AddComponent<LayoutElement>();
            nameLabelLe.preferredWidth = 160;

            // Player count badge (top-right)
            var countPillBg = UIFactory.CreateImage("CountPill", transform, PillBg);
            countPillBg.sprite = TextureGenerator.GetRoundedRect(64, 32, 10);
            countPillBg.type = Image.Type.Sliced;
            countPillBg.raycastTarget = false;
            var countPillRt = countPillBg.GetComponent<RectTransform>();
            countPillRt.anchorMin = new Vector2(1f, 1f);
            countPillRt.anchorMax = new Vector2(1f, 1f);
            countPillRt.pivot = new Vector2(1f, 1f);
            countPillRt.anchoredPosition = new Vector2(-8, -8);
            countPillRt.sizeDelta = new Vector2(36, 22);

            _playerCountText = UIFactory.CreateText("CountLabel", countPillBg.transform, "",
                12f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            _playerCountText.raycastTarget = false;
            var countTextRt = _playerCountText.GetComponent<RectTransform>();
            UIFactory.StretchFill(countTextRt);

            // Info pill (bottom-left): stakes + phase
            var infoPillBg = UIFactory.CreateImage("InfoPill", transform, PillBg);
            infoPillBg.sprite = TextureGenerator.GetRoundedRect(64, 32, 10);
            infoPillBg.type = Image.Type.Sliced;
            infoPillBg.raycastTarget = false;
            var infoPillRt = infoPillBg.GetComponent<RectTransform>();
            infoPillRt.anchorMin = new Vector2(0f, 0f);
            infoPillRt.anchorMax = new Vector2(0f, 0f);
            infoPillRt.pivot = new Vector2(0f, 0f);
            infoPillRt.anchoredPosition = new Vector2(8, 8);
            infoPillRt.sizeDelta = new Vector2(120, 22);
            var infoPillFitter = infoPillBg.gameObject.AddComponent<ContentSizeFitter>();
            infoPillFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            infoPillFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            var infoPillHlg = infoPillBg.gameObject.AddComponent<HorizontalLayoutGroup>();
            infoPillHlg.padding = new RectOffset(8, 8, 2, 2);
            infoPillHlg.childAlignment = TextAnchor.MiddleLeft;
            infoPillHlg.childControlWidth = false;
            infoPillHlg.childControlHeight = true;
            infoPillHlg.childForceExpandWidth = false;
            infoPillHlg.childForceExpandHeight = true;

            _infoPillText = UIFactory.CreateText("InfoLabel", infoPillBg.transform, "",
                12f, new Color(0.8f, 0.85f, 0.9f, 1f), TextAlignmentOptions.MidlineLeft);
            _infoPillText.raycastTarget = false;
            var infoLabelLe = _infoPillText.gameObject.AddComponent<LayoutElement>();
            infoLabelLe.preferredWidth = 160;

            // Status overlay text (shown when loading/error)
            _statusText = UIFactory.CreateText("Status", transform, "Loading...",
                14f, new Color(0.7f, 0.75f, 0.85f, 0.8f), TextAlignmentOptions.Center);
            _statusText.raycastTarget = false;
            var statusRt = _statusText.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0.1f, 0.3f);
            statusRt.anchorMax = new Vector2(0.9f, 0.7f);
            statusRt.offsetMin = Vector2.zero;
            statusRt.offsetMax = Vector2.zero;

            // AUTO badge (top-right, hidden by default — positioned next to count pill)
            _autoPlayBadge = new GameObject("AutoBadge", typeof(RectTransform));
            _autoPlayBadge.transform.SetParent(transform, false);
            var badgeBg = _autoPlayBadge.AddComponent<Image>();
            badgeBg.sprite = TextureGenerator.GetRoundedRect(64, 32, 8);
            badgeBg.type = Image.Type.Sliced;
            badgeBg.color = new Color(0.15f, 0.65f, 0.30f, 0.95f);
            badgeBg.raycastTarget = false;
            var badgeRt = _autoPlayBadge.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(1f, 1f);
            badgeRt.anchorMax = new Vector2(1f, 1f);
            badgeRt.pivot = new Vector2(1f, 1f);
            badgeRt.anchoredPosition = new Vector2(-48, -8);
            badgeRt.sizeDelta = new Vector2(40, 18);

            var badgeText = UIFactory.CreateText("AutoLabel", _autoPlayBadge.transform, "AUTO",
                9f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            badgeText.raycastTarget = false;
            var badgeTextRt = badgeText.GetComponent<RectTransform>();
            UIFactory.StretchFill(badgeTextRt);

            _autoPlayBadge.SetActive(false);

            // Live pulse on border glow when table has active players
            if (_animController != null)
            {
                _animController.Play(Tweener.PulseGlow(
                    alpha =>
                    {
                        if (_glowBorder == null) return;
                        var c = _theme.GlowBorder;
                        c.a = _hasActivePlayers ? alpha : 0.06f;
                        _glowBorder.color = c;
                    },
                    0.06f, 0.18f, 2f));
            }
        }

        /// <summary>
        /// Update the preview card from a table state response.
        /// </summary>
        public void UpdateFromState(TableResponse state)
        {
            if (state == null || state.Game == null)
            {
                _statusText.gameObject.SetActive(true);
                _statusText.text = "No data";
                return;
            }

            _statusText.gameObject.SetActive(false);

            var game = state.Game;
            var players = state.Players;

            // Cache data for pills
            _currentTableName = game.TableName ?? $"Table {_tableId}";
            _currentHandNo = game.GameNo;
            _currentPhase = FormatPhase(game.StepName);

            if (game.SmallBlind > 0 && game.BigBlind > 0)
                _currentBlinds = $"{MoneyFormatter.Format(game.SmallBlind)}/{MoneyFormatter.Format(game.BigBlind)}";
            else
                _currentBlinds = "";

            // Name pill: table name + hand #
            string handSuffix = _currentHandNo > 0 ? $"  #{_currentHandNo}" : "";
            _namePillText.text = $"{_currentTableName}{handSuffix}";

            // Player count badge
            int playerCount = players?.Count ?? 0;
            _playerCountText.text = $"{playerCount}/6";
            _hasActivePlayers = playerCount > 0;

            // Info pill: stakes + phase
            string infoText = "";
            if (!string.IsNullOrEmpty(_currentBlinds))
                infoText = _currentBlinds;
            if (!string.IsNullOrEmpty(_currentPhase))
            {
                if (infoText.Length > 0) infoText += "     ";
                infoText += _currentPhase;
            }
            _infoPillText.text = infoText;

            // Pot
            if (game.Pot > 0)
                _potText.text = $"Pot: {MoneyFormatter.Format(game.Pot)}";
            else
                _potText.text = "";

            // Seat dots
            UpdateSeatDots(players, game);

            // Community cards
            UpdateMiniCards(game.CommunityCards);
        }

        /// <summary>
        /// Show a loading/status message on the card.
        /// </summary>
        public void SetStatus(string message)
        {
            _statusText.gameObject.SetActive(true);
            _statusText.text = message;
        }

        private void UpdateSeatDots(List<PlayerState> players, GameState game)
        {
            // Reset all avatars and glows
            for (int i = 0; i < _seatAvatars.Length; i++)
            {
                _seatAvatars[i].SetEmpty(EmptySeatColor);
                _seatGlows[i].color = new Color(ActiveSeatColor.r, ActiveSeatColor.g,
                    ActiveSeatColor.b, 0f);
                _seatNameLabels[i].text = "";
            }

            if (players == null) return;

            foreach (var player in players)
            {
                int idx = player.Seat - 1;
                if (idx < 0 || idx >= _seatAvatars.Length) continue;

                // Show player's unique avatar identicon
                _seatAvatars[idx].UpdatePlayer(player.PlayerId);
                _seatNameLabels[idx].text = player.Username ?? "";

                if (player.IsWinner && game.HandStep >= 13)
                {
                    _seatGlows[idx].color = new Color(
                        WinnerSeatColor.r, WinnerSeatColor.g,
                        WinnerSeatColor.b, 0.4f);
                }
                else if (player.IsFolded)
                {
                    _seatAvatars[idx].SetFolded(true);
                }
                else if (game.Move == player.Seat)
                {
                    // Show soft glow behind the active player's avatar
                    _seatGlows[idx].color = new Color(
                        UIFactory.AccentGreen.r, UIFactory.AccentGreen.g,
                        UIFactory.AccentGreen.b, 0.3f);
                }
            }
        }

        private void UpdateMiniCards(List<string> communityCards)
        {
            for (int i = 0; i < _miniCardViews.Length; i++)
            {
                if (communityCards != null && i < communityCards.Count)
                    _miniCardViews[i].SetFaceUp(communityCards[i]);
                else
                    _miniCardViews[i].SetEmpty();
            }
        }

        private string FormatPhase(string stepName)
        {
            if (string.IsNullOrEmpty(stepName)) return "";

            return stepName switch
            {
                "DEAL_CARDS" => "DEAL",
                "POST_SMALL_BLIND" or "POST_BIG_BLIND" => "BLINDS",
                "PREFLOP_BETTING_ROUND" => "PREFLOP",
                "DEAL_FLOP" or "FLOP_BETTING_ROUND" => "FLOP",
                "DEAL_TURN" or "TURN_BETTING_ROUND" => "TURN",
                "DEAL_RIVER" or "RIVER_BETTING_ROUND" => "RIVER",
                "AFTER_RIVER_BETTING_ROUND" or "FIND_WINNERS" => "SHOWDOWN",
                "PAY_WINNERS" => "PAYOUT",
                "RECORD_STATS_AND_NEW_HAND" => "NEW HAND",
                _ => stepName.Replace("_", " ")
            };
        }

        public void SetAutoPlayIndicator(bool active)
        {
            if (_autoPlayBadge != null)
                _autoPlayBadge.SetActive(active);
        }

        /// <summary>
        /// Staggered entrance: scale from 0 with elastic ease after a delay.
        /// </summary>
        public void PlayEntrance(float delay)
        {
            if (_animController == null) return;
            transform.localScale = Vector3.zero;
            StartCoroutine(EntranceCoroutine(delay));
        }

        private IEnumerator EntranceCoroutine(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            _animController?.Play(Tweener.TweenScale(
                transform, Vector3.zero, Vector3.one, 0.5f, EaseType.EaseOutElastic));
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClicked?.Invoke(_tableId);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_animController != null)
                _animController.Play(Tweener.TweenScale(
                    transform, transform.localScale, Vector3.one * 1.04f, 0.15f));
            else
                transform.localScale = Vector3.one * 1.04f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_animController != null)
                _animController.Play(Tweener.TweenScale(
                    transform, transform.localScale, Vector3.one, 0.15f));
            else
                transform.localScale = Vector3.one;
        }
    }
}
