using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.Animation;

namespace HijackPoker.UI
{
    /// <summary>
    /// Full-screen scrollable overlay showing poker hand rankings, quick rules,
    /// and keyboard shortcuts. Dismissable via close button or background tap.
    /// </summary>
    public class HelpPopupView : MonoBehaviour
    {
        private RectTransform _rt;
        private CanvasGroup _canvasGroup;
        private AnimationController _animController;
        private TweenHandle _showTween;
        private TweenHandle _hideTween;
        private bool _isVisible;

        public bool IsVisible => _isVisible;

        public static HelpPopupView Create(Transform parent, AnimationController anim)
        {
            var go = new GameObject("HelpPopupView", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            UIFactory.StretchFill(rt);

            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;

            var view = go.AddComponent<HelpPopupView>();
            view._rt = rt;
            view._canvasGroup = cg;
            view._animController = anim;
            view.BuildUI();
            view.gameObject.SetActive(false);
            return view;
        }

        private void BuildUI()
        {
            // ── Dark semi-transparent full-screen background ──────────────
            var backdrop = UIFactory.CreateImage("Backdrop", transform,
                new Color(0.04f, 0.06f, 0.10f, 0.95f));
            var backdropRt = backdrop.GetComponent<RectTransform>();
            UIFactory.StretchFill(backdropRt);
            backdrop.raycastTarget = true;

            // Dismiss on backdrop tap
            var backdropBtn = backdrop.gameObject.AddComponent<Button>();
            backdropBtn.transition = Selectable.Transition.None;
            backdropBtn.onClick.AddListener(Dismiss);

            // ── Centered content panel ────────────────────────────────────
            var panelGo = new GameObject("Panel", typeof(RectTransform));
            panelGo.transform.SetParent(transform, false);
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.10f, 0.06f);
            panelRt.anchorMax = new Vector2(0.90f, 0.94f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            // Panel background with rounded rect
            var panelBg = UIFactory.CreateImage("PanelBg", panelGo.transform,
                new Color(0.10f, 0.10f, 0.13f, 0.94f));
            panelBg.sprite = TextureGenerator.GetRoundedRect(64, 64, 16);
            panelBg.type = Image.Type.Sliced;
            var panelBgRt = panelBg.GetComponent<RectTransform>();
            UIFactory.StretchFill(panelBgRt);
            var panelBgLE = panelBg.gameObject.AddComponent<LayoutElement>();
            panelBgLE.ignoreLayout = true;

            // ── Close button (X) in top-right ─────────────────────────────
            var closeBtnGo = new GameObject("CloseBtn", typeof(RectTransform));
            closeBtnGo.transform.SetParent(panelGo.transform, false);
            var closeBtnRt = closeBtnGo.GetComponent<RectTransform>();
            closeBtnRt.anchorMin = new Vector2(1f, 1f);
            closeBtnRt.anchorMax = new Vector2(1f, 1f);
            closeBtnRt.pivot = new Vector2(1f, 1f);
            closeBtnRt.sizeDelta = new Vector2(40, 40);
            closeBtnRt.anchoredPosition = new Vector2(-8, -8);

            var closeIcon = UIFactory.CreateImage("CloseIcon", closeBtnGo.transform,
                UIFactory.TextSecondary, new Vector2(32, 32));
            closeIcon.sprite = TextureGenerator.GetCloseIcon(32);
            closeIcon.raycastTarget = false;
            var closeIconRt = closeIcon.GetComponent<RectTransform>();
            closeIconRt.anchorMin = new Vector2(0.5f, 0.5f);
            closeIconRt.anchorMax = new Vector2(0.5f, 0.5f);
            closeIconRt.anchoredPosition = Vector2.zero;

            var closeBtn = closeBtnGo.AddComponent<Button>();
            var closeBgImg = closeBtnGo.AddComponent<Image>();
            closeBgImg.color = Color.clear;
            closeBtn.targetGraphic = closeBgImg;
            closeBtn.onClick.AddListener(Dismiss);

            // ── ScrollRect for content ────────────────────────────────────
            var scrollGo = new GameObject("Scroll", typeof(RectTransform));
            scrollGo.transform.SetParent(panelGo.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(16, 16);
            scrollRt.offsetMax = new Vector2(-16, -16);

            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.color = Color.clear;
            scrollGo.AddComponent<RectMask2D>();

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 30f;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Scroll content container
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.sizeDelta = new Vector2(0, 0);

            var contentVlg = contentGo.AddComponent<VerticalLayoutGroup>();
            contentVlg.spacing = 6;
            contentVlg.padding = new RectOffset(8, 8, 8, 8);
            contentVlg.childAlignment = TextAnchor.UpperCenter;
            contentVlg.childControlWidth = true;
            contentVlg.childControlHeight = true;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;

            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRt;

            // ── Title ─────────────────────────────────────────────────────
            var title = UIFactory.CreateText("Title", contentGo.transform,
                "POKER GUIDE", 26f, UIFactory.AccentGold,
                TextAlignmentOptions.Center, FontStyles.Bold);
            var titleLE = title.gameObject.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 44;

            AddSeparator(contentGo.transform);

            // ── Section 1: Hand Rankings ──────────────────────────────────
            BuildHandRankingsSection(contentGo.transform);

            AddSeparator(contentGo.transform);

            // ── Section 2: Quick Rules ────────────────────────────────────
            BuildQuickRulesSection(contentGo.transform);

            AddSeparator(contentGo.transform);

            // ── Section 3: Controls ───────────────────────────────────────
            BuildControlsSection(contentGo.transform);
        }

        // ── Section builders ──────────────────────────────────────────────

        private void BuildHandRankingsSection(Transform parent)
        {
            AddSectionHeader(parent, "HAND RANKINGS");

            string[] hands =
            {
                "Royal Flush",      "A-K-Q-J-10, all same suit",
                "Straight Flush",   "Five consecutive cards, same suit",
                "Four of a Kind",   "Four cards of the same rank",
                "Full House",       "Three of a kind plus a pair",
                "Flush",            "Five cards of the same suit",
                "Straight",         "Five consecutive cards, any suit",
                "Three of a Kind",  "Three cards of the same rank",
                "Two Pair",         "Two different pairs",
                "One Pair",         "Two cards of the same rank",
                "High Card",        "Highest card when no other hand is made"
            };

            for (int i = 0; i < hands.Length; i += 2)
            {
                AddRankingRow(parent, i / 2 + 1, hands[i], hands[i + 1]);
            }
        }

        private void BuildQuickRulesSection(Transform parent)
        {
            AddSectionHeader(parent, "QUICK RULES");

            string[] rules =
            {
                "Each player is dealt 2 hole cards face-down.",
                "5 community cards are dealt in stages: Flop (3), Turn (1), River (1).",
                "Players make the best 5-card hand from any combination of their hole cards and community cards.",
                "Betting rounds occur after the deal, flop, turn, and river.",
                "Players can check, bet, call, raise, or fold on their turn.",
                "The best hand at showdown wins the pot. If all others fold, the last player standing wins."
            };

            foreach (var rule in rules)
            {
                AddBulletPoint(parent, rule);
            }
        }

        private void BuildControlsSection(Transform parent)
        {
            AddSectionHeader(parent, "CONTROLS");

            string[] shortcuts =
            {
                "Space",    "Next Step \u2014 advance the hand by one step",
                "A",        "Auto-Play \u2014 toggle automatic hand progression",
                "S",        "Speed \u2014 cycle auto-play speed (1x\u20135x)",
                "H",        "Hand History \u2014 toggle the step-by-step log panel",
                "R",        "Reset \u2014 restart the current hand",
                "M",        "Mute \u2014 toggle all procedural audio"
            };

            for (int i = 0; i < shortcuts.Length; i += 2)
            {
                AddControlRow(parent, shortcuts[i], shortcuts[i + 1]);
            }
        }

        // ── UI helpers ────────────────────────────────────────────────────

        private void AddSectionHeader(Transform parent, string text)
        {
            var header = UIFactory.CreateText("Header", parent,
                text, 18f, UIFactory.TextBright,
                TextAlignmentOptions.Left, FontStyles.Bold);
            var headerLE = header.gameObject.AddComponent<LayoutElement>();
            headerLE.preferredHeight = 32;
        }

        private void AddSeparator(Transform parent)
        {
            var sep = UIFactory.CreateImage("Separator", parent,
                UIFactory.SeparatorColor, new Vector2(0, 1));
            var sepLE = sep.gameObject.AddComponent<LayoutElement>();
            sepLE.preferredHeight = 1;
            sepLE.flexibleWidth = 1;
        }

        private void AddRankingRow(Transform parent, int rank, string handName, string description)
        {
            var rowGo = new GameObject($"Rank_{rank}", typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            var rowLE = rowGo.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 28;
            rowLE.flexibleWidth = 1;

            var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.padding = new RectOffset(4, 4, 2, 2);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Rank number
            var rankText = UIFactory.CreateText("Num", rowGo.transform,
                $"{rank}.", 13f, UIFactory.TextMuted,
                TextAlignmentOptions.Right, FontStyles.Normal);
            var rankLE = rankText.gameObject.AddComponent<LayoutElement>();
            rankLE.preferredWidth = 24;

            // Hand name
            Color nameColor = rank <= 3 ? UIFactory.AccentGold : UIFactory.TextPrimary;
            var nameText = UIFactory.CreateText("Name", rowGo.transform,
                handName, 14f, nameColor,
                TextAlignmentOptions.Left, FontStyles.Bold);
            var nameLE = nameText.gameObject.AddComponent<LayoutElement>();
            nameLE.preferredWidth = 130;

            // Description
            var descText = UIFactory.CreateText("Desc", rowGo.transform,
                description, 12f, UIFactory.TextSecondary,
                TextAlignmentOptions.Left, FontStyles.Normal);
            var descLE = descText.gameObject.AddComponent<LayoutElement>();
            descLE.flexibleWidth = 1;
        }

        private void AddBulletPoint(Transform parent, string text)
        {
            var rowGo = new GameObject("Bullet", typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            var rowLE = rowGo.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 26;
            rowLE.flexibleWidth = 1;

            var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.padding = new RectOffset(8, 4, 2, 2);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Bullet dot
            var dot = UIFactory.CreateText("Dot", rowGo.transform,
                "\u2022", 14f, UIFactory.AccentGold,
                TextAlignmentOptions.Left, FontStyles.Normal);
            var dotLE = dot.gameObject.AddComponent<LayoutElement>();
            dotLE.preferredWidth = 14;

            // Text
            var bodyText = UIFactory.CreateText("Text", rowGo.transform,
                text, 13f, UIFactory.TextPrimary,
                TextAlignmentOptions.Left, FontStyles.Normal);
            var bodyLE = bodyText.gameObject.AddComponent<LayoutElement>();
            bodyLE.flexibleWidth = 1;
        }

        private void AddControlRow(Transform parent, string key, string description)
        {
            var rowGo = new GameObject($"Control_{key}", typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            var rowLE = rowGo.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 30;
            rowLE.flexibleWidth = 1;

            var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.padding = new RectOffset(8, 4, 2, 2);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Key badge background
            var badgeGo = new GameObject("KeyBadge", typeof(RectTransform));
            badgeGo.transform.SetParent(rowGo.transform, false);

            var badgeBg = badgeGo.AddComponent<Image>();
            badgeBg.color = UIFactory.ButtonDefault;
            badgeBg.sprite = TextureGenerator.GetRoundedRect(48, 28, 6);
            badgeBg.type = Image.Type.Sliced;
            badgeBg.raycastTarget = false;

            var badgeLE = badgeGo.AddComponent<LayoutElement>();
            badgeLE.preferredWidth = 64;
            badgeLE.preferredHeight = 24;

            var keyText = UIFactory.CreateText("Key", badgeGo.transform,
                key, 13f, UIFactory.TextBright,
                TextAlignmentOptions.Center, FontStyles.Bold);
            var keyRt = keyText.GetComponent<RectTransform>();
            keyRt.anchorMin = Vector2.zero;
            keyRt.anchorMax = Vector2.one;
            keyRt.sizeDelta = Vector2.zero;

            // Description
            var descText = UIFactory.CreateText("Desc", rowGo.transform,
                description, 13f, UIFactory.TextPrimary,
                TextAlignmentOptions.Left, FontStyles.Normal);
            var descLE = descText.gameObject.AddComponent<LayoutElement>();
            descLE.flexibleWidth = 1;
        }

        // ── Show / Dismiss ────────────────────────────────────────────────

        public void Show()
        {
            if (_isVisible) return;
            _isVisible = true;

            _hideTween?.Cancel();
            gameObject.SetActive(true);
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = true;

            if (_animController != null)
            {
                _showTween = _animController.Play(
                    Tweener.TweenAlpha(_canvasGroup, 0f, 1f, 0.25f));
            }
            else
            {
                _canvasGroup.alpha = 1f;
            }
        }

        public void Dismiss()
        {
            if (!_isVisible) return;
            _isVisible = false;

            _showTween?.Cancel();

            if (_animController != null)
            {
                _hideTween = _animController.Play(
                    Tweener.TweenAlpha(_canvasGroup, _canvasGroup.alpha, 0f, 0.2f));
                _hideTween.OnComplete(() =>
                {
                    _canvasGroup.blocksRaycasts = false;
                    gameObject.SetActive(false);
                });
            }
            else
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                gameObject.SetActive(false);
            }
        }
    }
}
