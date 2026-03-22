using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.Animation;
using HijackPoker.Models;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    /// <summary>
    /// Full-screen modal overlay shown at showdown (steps 13-14).
    /// Displays all non-folded players' hands, ranks, and winnings.
    /// Winners highlighted in gold with sparkle effects.
    /// </summary>
    public class ShowdownOverlay : MonoBehaviour
    {
        public event Action OnDismissed;

        private RectTransform _rt;
        private CanvasGroup _canvasGroup;
        private AnimationController _animController;
        private GameObject _contentContainer;
        private GameObject _rowsContainer;
        private Button _nextHandButton;
        private bool _isVisible;
        private Coroutine _autoDismissCoroutine;
        private TableTheme _theme;

        public bool IsVisible => _isVisible;

        public static ShowdownOverlay Create(Transform parent, AnimationController anim, TableTheme theme = null)
        {
            var go = new GameObject("ShowdownOverlay", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            UIFactory.StretchFill(rt);

            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;

            var view = go.AddComponent<ShowdownOverlay>();
            view._rt = rt;
            view._canvasGroup = cg;
            view._animController = anim;
            view._theme = theme;
            view.BuildUI();
            view.gameObject.SetActive(false);
            return view;
        }

        private void BuildUI()
        {
            // Dark backdrop
            var backdrop = UIFactory.CreateImage("Backdrop", transform,
                new Color(0f, 0f, 0f, 0.75f));
            var backdropRt = backdrop.GetComponent<RectTransform>();
            UIFactory.StretchFill(backdropRt);

            // Content panel (centered)
            _contentContainer = new GameObject("Content", typeof(RectTransform));
            _contentContainer.transform.SetParent(transform, false);
            var contentRt = _contentContainer.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.08f, 0.10f);
            contentRt.anchorMax = new Vector2(0.92f, 0.90f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            // Panel background
            var panelBg = UIFactory.CreateImage("PanelBg", _contentContainer.transform,
                new Color(0.08f, 0.10f, 0.18f, 0.95f));
            panelBg.sprite = TextureGenerator.GetRoundedRect(64, 64, 16);
            panelBg.type = Image.Type.Sliced;
            var panelBgRt = panelBg.GetComponent<RectTransform>();
            UIFactory.StretchFill(panelBgRt);
            var panelBgLE = panelBg.gameObject.AddComponent<LayoutElement>();
            panelBgLE.ignoreLayout = true;

            var vlg = _contentContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Title
            var title = UIFactory.CreateText("Title", _contentContainer.transform,
                "SHOWDOWN", 28f, UIFactory.AccentGold,
                TextAlignmentOptions.Center, FontStyles.Bold);
            var titleLE = title.gameObject.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 40;

            // Rows container (scrollable if many players)
            _rowsContainer = new GameObject("Rows", typeof(RectTransform));
            _rowsContainer.transform.SetParent(_contentContainer.transform, false);
            var rowsLE = _rowsContainer.AddComponent<LayoutElement>();
            rowsLE.flexibleHeight = 1;
            rowsLE.flexibleWidth = 1;

            var rowsVlg = _rowsContainer.AddComponent<VerticalLayoutGroup>();
            rowsVlg.spacing = 6;
            rowsVlg.childAlignment = TextAnchor.UpperCenter;
            rowsVlg.childControlWidth = true;
            rowsVlg.childControlHeight = true;
            rowsVlg.childForceExpandWidth = true;
            rowsVlg.childForceExpandHeight = false;

            // Next Hand button
            _nextHandButton = UIFactory.CreateButton("NextHand", _contentContainer.transform,
                "NEXT HAND", 16f, UIFactory.AccentCyan, Color.white,
                new Vector2(200, 44));
            var btnLE = _nextHandButton.gameObject.AddComponent<LayoutElement>();
            btnLE.preferredHeight = 44;
            btnLE.preferredWidth = 200;
            _nextHandButton.onClick.AddListener(Dismiss);
        }

        public void Show(TableResponse state)
        {
            if (state?.Players == null || state.Game == null) return;

            _isVisible = true;
            gameObject.SetActive(true);

            // Clear previous rows
            for (int i = _rowsContainer.transform.childCount - 1; i >= 0; i--)
                Destroy(_rowsContainer.transform.GetChild(i).gameObject);

            // Build winner seat set from game.Winners (available at step 13, before winnings are paid)
            var winnerSeats = new HashSet<int>();
            if (state.Game.Winners != null)
            {
                foreach (var w in state.Game.Winners)
                    winnerSeats.Add(w.Seat);
            }

            // Sort: winners first, then by seat
            var sorted = new List<PlayerState>(state.Players);
            sorted.RemoveAll(p => p.IsFolded || !p.HasCards);
            sorted.Sort((a, b) =>
            {
                bool aWinner = winnerSeats.Contains(a.Seat);
                bool bWinner = winnerSeats.Contains(b.Seat);
                if (aWinner && !bWinner) return -1;
                if (!aWinner && bWinner) return 1;
                // Then by winnings descending
                if (a.Winnings != b.Winnings) return b.Winnings.CompareTo(a.Winnings);
                return a.Seat.CompareTo(b.Seat);
            });

            // Create player rows with stagger animation
            float delay = 0f;
            foreach (var player in sorted)
            {
                CreatePlayerRow(player, state.Game, delay, winnerSeats.Contains(player.Seat));
                delay += 0.1f;
            }

            // Community cards row
            if (state.Game.CommunityCards != null && state.Game.CommunityCards.Count > 0)
                CreateCommunityRow(state.Game.CommunityCards);

            // Fade in
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = true;

            if (_animController != null)
            {
                _animController.Play(
                    Tweener.TweenAlpha(_canvasGroup, 0f, 1f, 0.3f));
            }
            else
            {
                _canvasGroup.alpha = 1f;
            }
        }

        public void ShowAutoMode(TableResponse state, float delay)
        {
            Show(state);
            _nextHandButton.gameObject.SetActive(false);
            _autoDismissCoroutine = StartCoroutine(AutoDismissAfter(delay));
        }

        private IEnumerator AutoDismissAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            _autoDismissCoroutine = null;
            Dismiss();
        }

        private void CreatePlayerRow(PlayerState player, GameState game, float staggerDelay, bool isWinner)
        {
            float rowHeight = LayoutConfig.ShowdownRowHeight;

            var rowGo = new GameObject($"Row_{player.Seat}", typeof(RectTransform));
            rowGo.transform.SetParent(_rowsContainer.transform, false);
            var rowLE = rowGo.AddComponent<LayoutElement>();
            rowLE.preferredHeight = rowHeight;
            rowLE.flexibleWidth = 1;

            // Row background
            Color rowBg = isWinner
                ? new Color(UIFactory.AccentGold.r, UIFactory.AccentGold.g, UIFactory.AccentGold.b, 0.15f)
                : new Color(1f, 1f, 1f, 0.05f);
            var bg = rowGo.AddComponent<Image>();
            bg.color = rowBg;
            bg.sprite = TextureGenerator.GetRoundedRect(64, 64, 10);
            bg.type = Image.Type.Sliced;

            var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.padding = new RectOffset(10, 10, 4, 4);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Avatar
            var avatar = AvatarCircleView.Create(rowGo.transform, 32f);
            avatar.UpdatePlayer(player.PlayerId);
            var avatarLE = avatar.gameObject.AddComponent<LayoutElement>();
            avatarLE.preferredWidth = 32;
            avatarLE.preferredHeight = 32;

            // Name
            Color nameColor = isWinner ? UIFactory.AccentGold : Color.white;
            var nameText = UIFactory.CreateText("Name", rowGo.transform,
                player.Username ?? $"Player {player.Seat}",
                14f, nameColor, TextAlignmentOptions.Left, FontStyles.Bold);
            var nameLE = nameText.gameObject.AddComponent<LayoutElement>();
            nameLE.flexibleWidth = 0;
            nameLE.preferredWidth = 80;

            // Cards (fanned layout matching SeatView)
            float cardScale = LayoutConfig.ShowdownCardScale;
            var cardSz = LayoutConfig.CardSize;
            float fanAngle = LayoutConfig.ShowdownCardFanAngle;
            float fanYOffset = LayoutConfig.ShowdownCardFanYOffset;
            float cardSpacing = LayoutConfig.ShowdownCardSpacing;
            float containerW = cardSpacing * 2 + cardSz.x * cardScale;
            float containerH = cardSz.y * cardScale + LayoutConfig.ShowdownCardRotationPadding;

            var cardsContainer = new GameObject("Cards", typeof(RectTransform));
            cardsContainer.transform.SetParent(rowGo.transform, false);
            var cardsLE = cardsContainer.AddComponent<LayoutElement>();
            cardsLE.preferredWidth = containerW;
            cardsLE.preferredHeight = containerH;
            cardsLE.minWidth = containerW;
            cardsLE.minHeight = containerH;

            if (player.Cards != null && player.Cards.Count > 0)
            {
                for (int i = 0; i < player.Cards.Count && i < 2; i++)
                {
                    var card = CardView.Create($"Card{i}", cardsContainer.transform, _theme);
                    card.SetFaceUp(player.Cards[i]);
                    card.RectTransform.localScale = Vector3.one * cardScale;

                    float xOff = i == 0 ? -cardSpacing : cardSpacing;
                    float yOff = i == 0 ? -fanYOffset / 2f : fanYOffset / 2f;
                    float angle = i == 0 ? fanAngle : -fanAngle;
                    card.RectTransform.anchoredPosition = new Vector2(xOff, yOff);
                    card.RectTransform.localEulerAngles = new Vector3(0, 0, angle);

                    if (isWinner)
                    {
                        var glowColor = CardView.GetHandStrengthColor(player.HandRank);
                        card.SetWinnerGlow(glowColor, _animController);
                    }
                }
            }

            // Hand rank
            string rank = !string.IsNullOrEmpty(player.HandRank) ? player.HandRank : "";
            var rankText = UIFactory.CreateText("Rank", rowGo.transform,
                rank, 12f, UIFactory.AccentGold,
                TextAlignmentOptions.Center, FontStyles.Italic);
            var rankLE = rankText.gameObject.AddComponent<LayoutElement>();
            rankLE.flexibleWidth = 0;
            rankLE.preferredWidth = 80;

            // Winnings
            if (player.Winnings > 0)
            {
                var winText = UIFactory.CreateText("Win", rowGo.transform,
                    $"+{MoneyFormatter.Format(player.Winnings)}",
                    16f, UIFactory.AccentGold,
                    TextAlignmentOptions.Right, FontStyles.Bold);
                var winLE = winText.gameObject.AddComponent<LayoutElement>();
                winLE.preferredWidth = 70;
            }

            // Stagger animation
            if (_animController != null && staggerDelay > 0)
            {
                var rowCg = rowGo.AddComponent<CanvasGroup>();
                rowCg.alpha = 0f;
                var delayH = _animController.Play(Tweener.Delay(staggerDelay));
                delayH.OnComplete(() =>
                {
                    if (rowCg != null)
                        _animController.Play(Tweener.TweenAlpha(rowCg, 0f, 1f, 0.2f));
                });
            }

            // Winner sparkle
            if (isWinner && _animController != null)
            {
                var delayH = _animController.Play(Tweener.Delay(staggerDelay + 0.2f));
                delayH.OnComplete(() =>
                {
                    if (rowGo != null)
                    {
                        SparkleEffects.SpawnSparkles(rowGo.transform, Vector2.zero, 14,
                            UIFactory.AccentGold, 90f, 1.0f, _animController);
                    }
                });
            }
        }

        private void CreateCommunityRow(List<string> communityCards)
        {
            float rowHeight = LayoutConfig.ShowdownCommunityRowHeight;
            float scale = LayoutConfig.ShowdownCommunityCardScale;
            float gap = LayoutConfig.ShowdownCommunityCardGap;
            var cardSz = LayoutConfig.CardSize * scale;

            var rowGo = new GameObject("CommunityRow", typeof(RectTransform));
            rowGo.transform.SetParent(_rowsContainer.transform, false);
            var rowLE = rowGo.AddComponent<LayoutElement>();
            rowLE.preferredHeight = rowHeight;
            rowLE.flexibleWidth = 1;

            var bg = rowGo.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.03f);
            bg.sprite = TextureGenerator.GetRoundedRect(64, 64, 10);
            bg.type = Image.Type.Sliced;

            var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = gap;
            hlg.padding = new RectOffset(10, 10, 4, 4);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            foreach (var cardStr in communityCards)
            {
                var card = CardView.Create("CC", rowGo.transform, _theme);
                card.SetFaceUp(cardStr);
                card.RectTransform.localScale = Vector3.one * scale;
                var cardLE2 = card.gameObject.AddComponent<LayoutElement>();
                cardLE2.preferredWidth = cardSz.x;
                cardLE2.preferredHeight = cardSz.y;
                cardLE2.minWidth = cardSz.x;
                cardLE2.minHeight = cardSz.y;
            }
        }

        public void Dismiss()
        {
            if (!_isVisible) return;
            _isVisible = false;

            if (_autoDismissCoroutine != null)
            {
                StopCoroutine(_autoDismissCoroutine);
                _autoDismissCoroutine = null;
            }

            _nextHandButton.gameObject.SetActive(true);

            if (_animController != null)
            {
                var tween = _animController.Play(
                    Tweener.TweenAlpha(_canvasGroup, 1f, 0f, 0.2f));
                tween.OnComplete(() =>
                {
                    _canvasGroup.blocksRaycasts = false;
                    gameObject.SetActive(false);
                    OnDismissed?.Invoke();
                });
            }
            else
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                gameObject.SetActive(false);
                OnDismissed?.Invoke();
            }
        }
    }
}
