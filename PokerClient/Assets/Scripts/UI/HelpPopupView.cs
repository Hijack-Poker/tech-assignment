using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace HijackPoker.UI
{
    /// <summary>
    /// Full-screen help popup explaining controls, button behavior, and poker basics.
    /// Created dynamically — no scene setup needed.
    /// </summary>
    public class HelpPopupView : MonoBehaviour
    {
        private RectTransform _root;
        private CanvasGroup _rootCG;
        private bool _isOpen;

        private const string HelpContent =
@"<size=28><b>CONTROLS</b></size>

<b><color=#6EC6FF>NEXT</color></b>
Advance one step manually. Use this to step through the hand at your own pace. Disabled during betting rounds (use action buttons instead) and during auto/focused modes.

<b><color=#24B88C>FOCUS  Safe / Small / Hard</color></b>
Others play automatically — YOU decide your own cards. The game flows naturally, auto-playing all other seats, but pauses and shows the action panel when it's your turn. Click again to stop. Each click cycles the AI style:
  • <b>Safe</b> — Others only call/check
  • <b>Small</b> — Others mostly call, occasionally raise
  • <b>Hard</b> — Others play aggressively (fold, raise, all-in)

<b><color=#1F7DDC>AUTO  Safe / Small / Hard</color></b>
Full auto-play — ALL seats play automatically, including yours. Click to start, click again to stop. Cycles through the same Safe/Small/Hard styles.

<b><color=#4A90D9>SPEED (0.25x  0.5x  1x  2x)</color></b>
Controls how fast auto-play and focused mode advance. Applies to both modes.

<size=28><b>ACTION BUTTONS</b></size>
Shown during betting rounds when it's the acting seat's turn:

<b><color=#B24848>FOLD</color></b> — Surrender your hand. You lose any bets already placed.
<b><color=#2174B8>CHECK</color></b> — Pass without betting (only when no bet to match).
<b><color=#2174B8>CALL $X</color></b> — Match the current bet to stay in the hand.
<b><color=#BF7A24>RAISE $X</color></b> — Increase the bet. Min raise = 2× current bet.
<b><color=#BF7A24>BET 2X / 3X</color></b> — Quick-bet 2× or 3× the big blind (when no bet exists).
<b><color=#D94040>ALL IN</color></b> — Put your entire stack in. Go big or go home.

<b>TIP $1</b> — Tip the dealer $1 from the acting player's stack. Just for fun.

<size=28><b>CARD PREVIEW</b></size>
<b>Tap your hole cards</b> during your turn to see a large preview with a flip animation. Tap the backdrop to dismiss.

<size=28><b>HUD</b></size>
<b>RESTART</b> — Reset the table and start a completely fresh game.
<b>EXIT</b> — Return to the home screen.

<size=28><b>POKER BASICS</b></size>

<b>Hand Flow</b>
1. Blinds are posted (Small Blind + Big Blind)
2. Two hole cards dealt to each player
3. <b>Pre-Flop Betting</b> — bet based on your hole cards
4. <b>Flop</b> — 3 community cards revealed
5. <b>Flop Betting</b>
6. <b>Turn</b> — 4th community card
7. <b>Turn Betting</b>
8. <b>River</b> — 5th community card
9. <b>River Betting</b>
10. <b>Showdown</b> — best 5-card hand wins

<b>Hand Rankings</b> (strongest → weakest)
  <b>Royal Flush</b>      A K Q J 10 same suit
  <b>Straight Flush</b>   5 in sequence, same suit
  <b>Four of a Kind</b>   4 cards same rank
  <b>Full House</b>       3 of a kind + a pair
  <b>Flush</b>            5 cards same suit
  <b>Straight</b>         5 in sequence
  <b>Three of a Kind</b>  3 cards same rank
  <b>Two Pair</b>         2 different pairs
  <b>One Pair</b>         2 cards same rank
  <b>High Card</b>        Nothing — highest card plays

<b>Key Terms</b>
  <b>Blinds</b> — Forced bets by the two players left of the dealer
  <b>Pot</b> — Total chips bet by all players this hand
  <b>Community Cards</b> — Shared cards on the table (flop + turn + river)
  <b>Hole Cards</b> — Your two private cards
  <b>Showdown</b> — When remaining players reveal cards
  <b>All-In</b> — Betting everything you have
  <b>Dealer (D)</b> — Rotates each hand, determines blind positions";

        public void Toggle()
        {
            if (_isOpen) Hide();
            else Show();
        }

        public void Show()
        {
            EnsureUI();
            if (_root == null) return;

            _isOpen = true;
            _root.gameObject.SetActive(true);
            _rootCG.alpha = 0f;
            DOTween.To(() => _rootCG.alpha, v => _rootCG.alpha = v, 1f, 0.2f).SetEase(Ease.OutQuad);
        }

        public void Hide()
        {
            if (_root == null || !_isOpen) return;
            _isOpen = false;

            DOTween.To(() => _rootCG.alpha, v => _rootCG.alpha = v, 0f, 0.15f)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    if (_root != null) _root.gameObject.SetActive(false);
                });
        }

        private void EnsureUI()
        {
            if (_root != null) return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            // Root overlay
            var rootGO = new GameObject("HelpPopup", typeof(RectTransform), typeof(CanvasGroup));
            rootGO.transform.SetParent(canvas.transform, false);
            rootGO.transform.SetAsLastSibling();
            _root = rootGO.GetComponent<RectTransform>();
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;
            _rootCG = rootGO.GetComponent<CanvasGroup>();

            // Backdrop
            var backdropGO = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            backdropGO.transform.SetParent(_root, false);
            var bdRt = backdropGO.GetComponent<RectTransform>();
            bdRt.anchorMin = Vector2.zero;
            bdRt.anchorMax = Vector2.one;
            bdRt.offsetMin = Vector2.zero;
            bdRt.offsetMax = Vector2.zero;
            var bdImg = backdropGO.GetComponent<Image>();
            bdImg.color = new Color(0.02f, 0.04f, 0.08f, 0.85f);
            bdImg.raycastTarget = true;
            backdropGO.GetComponent<Button>().onClick.AddListener(Hide);

            // Panel
            var panelGO = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(_root, false);
            var panelRt = panelGO.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.08f, 0.06f);
            panelRt.anchorMax = new Vector2(0.92f, 0.94f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            var panelImg = panelGO.GetComponent<Image>();
            panelImg.color = new Color(0.06f, 0.10f, 0.16f, 0.97f);
            panelImg.raycastTarget = true;

            // Title bar
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(panelGO.transform, false);
            var titleRt = titleGO.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -8f);
            titleRt.sizeDelta = new Vector2(0f, 40f);
            var titleTxt = titleGO.GetComponent<TextMeshProUGUI>();
            titleTxt.text = "HELP & POKER GUIDE";
            titleTxt.fontSize = 22;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = new Color(0.96f, 0.85f, 0.45f);
            titleTxt.raycastTarget = false;

            // Close button (X) in top-right
            var closeGO = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGO.transform.SetParent(panelGO.transform, false);
            var closeRt = closeGO.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-8f, -8f);
            closeRt.sizeDelta = new Vector2(36f, 36f);
            var closeImg = closeGO.GetComponent<Image>();
            closeImg.color = new Color(0.8f, 0.25f, 0.22f);
            closeGO.GetComponent<Button>().onClick.AddListener(Hide);

            var closeLabelGO = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeLabelGO.transform.SetParent(closeGO.transform, false);
            var clRt = closeLabelGO.GetComponent<RectTransform>();
            clRt.anchorMin = Vector2.zero;
            clRt.anchorMax = Vector2.one;
            clRt.offsetMin = Vector2.zero;
            clRt.offsetMax = Vector2.zero;
            var clTxt = closeLabelGO.GetComponent<TextMeshProUGUI>();
            clTxt.text = "X";
            clTxt.fontSize = 20;
            clTxt.fontStyle = FontStyles.Bold;
            clTxt.alignment = TextAlignmentOptions.Center;
            clTxt.color = Color.white;
            clTxt.raycastTarget = false;

            // Scroll view for content
            var scrollGO = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(panelGO.transform, false);
            var scrollRt = scrollGO.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(16f, 16f);
            scrollRt.offsetMax = new Vector2(-16f, -52f);

            // Viewport with mask
            var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var vpRt = viewportGO.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            var vpImg = viewportGO.GetComponent<Image>();
            vpImg.color = new Color(1f, 1f, 1f, 0.003f); // near-invisible but needed for mask
            viewportGO.GetComponent<Mask>().showMaskGraphic = false;

            // Content — uses VerticalLayoutGroup so children drive height
            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRt = contentGO.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);
            var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            var csf = contentGO.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Text — stretches to full width via layout, height is preferred
            var textGO = new GameObject("HelpText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            textGO.transform.SetParent(contentGO.transform, false);
            var helpTxt = textGO.GetComponent<TextMeshProUGUI>();
            helpTxt.text = HelpContent;
            helpTxt.fontSize = 15;
            helpTxt.color = new Color(0.88f, 0.92f, 0.97f);
            helpTxt.lineSpacing = 4f;
            helpTxt.enableWordWrapping = true;
            helpTxt.raycastTarget = false;
            var textLE = textGO.GetComponent<LayoutElement>();
            textLE.flexibleWidth = 1f;

            // Wire up ScrollRect
            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.content = contentRt;
            scroll.viewport = vpRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            _root.gameObject.SetActive(false);
        }
    }
}
