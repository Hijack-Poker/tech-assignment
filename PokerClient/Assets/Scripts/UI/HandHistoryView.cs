using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    public class HandHistoryView : MonoBehaviour
    {
        [SerializeField] private TableStateManager _stateManager;
        [SerializeField] private TextMeshProUGUI _historyText;

        private readonly List<string> _lines = new List<string>();
        private const int MaxLines = 100;
        private ScrollRect _scrollRect;

        // Track last known action per seat to only log new actions
        private readonly Dictionary<int, string> _lastActions = new();
        private int _lastGameNo = -1;
        private int _lastHandStep = -1;
        private bool _winnersLoggedThisHand;

        private void Awake()
        {
            SetupScrollableHistory();
        }

        private void SetupScrollableHistory()
        {
            if (_historyText == null) return;

            // The _historyText sits inside a panel. We'll wrap it in a ScrollRect
            // so the full history is scrollable and new entries auto-scroll to bottom.
            var panel = _historyText.transform.parent;
            if (panel == null) return;

            // If there's already a ScrollRect, just grab it
            _scrollRect = panel.GetComponent<ScrollRect>();
            if (_scrollRect == null)
                _scrollRect = panel.GetComponentInChildren<ScrollRect>();
            if (_scrollRect != null) return;

            // Create scroll structure inside the existing panel
            var scrollGO = new GameObject("HistoryScroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(panel, false);
            var scrollRt = scrollGO.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(8f, 4f);
            scrollRt.offsetMax = new Vector2(-8f, -4f);

            // Viewport
            var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var vpRt = viewportGO.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            var vpImg = viewportGO.GetComponent<Image>();
            vpImg.color = new Color(1f, 1f, 1f, 0.003f);
            viewportGO.GetComponent<Mask>().showMaskGraphic = false;

            // Content container — anchored to bottom so text grows upward
            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRt = contentGO.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 0f);
            contentRt.anchorMax = new Vector2(1f, 0f);
            contentRt.pivot = new Vector2(0.5f, 0f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);
            var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childAlignment = TextAnchor.LowerLeft;
            var csf = contentGO.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Reparent the text into the content container
            _historyText.transform.SetParent(contentGO.transform, false);
            var textRt = _historyText.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 0f);
            textRt.anchorMax = new Vector2(1f, 0f);
            textRt.pivot = new Vector2(0f, 0f);
            textRt.anchoredPosition = Vector2.zero;
            textRt.sizeDelta = Vector2.zero;

            // Ensure text wraps and sizes correctly
            _historyText.enableWordWrapping = true;
            _historyText.overflowMode = TextOverflowModes.Overflow;
            _historyText.alignment = TextAlignmentOptions.BottomLeft;

            // Add LayoutElement so the layout group can measure it
            var le = _historyText.GetComponent<LayoutElement>();
            if (le == null) le = _historyText.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;

            // Wire up ScrollRect
            _scrollRect = scrollGO.GetComponent<ScrollRect>();
            _scrollRect.content = contentRt;
            _scrollRect.viewport = vpRt;
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 20f;
        }

        private void OnEnable()
        {
            if (_stateManager == null) return;
            _stateManager.OnTableStateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            if (_stateManager != null)
                _stateManager.OnTableStateChanged -= OnStateChanged;
        }

        private void OnStateChanged(TableResponse state)
        {
            var game = state.Game;

            // Reset action tracking on new hand
            if (game.GameNo != _lastGameNo)
            {
                _lastActions.Clear();
                _lastGameNo = game.GameNo;
                _winnersLoggedThisHand = false;
            }

            // Only show step header when the step changes
            if (game.HandStep != _lastHandStep)
            {
                string label = _stateManager.GetStepLabel(game.HandStep);
                AddLine($"Hand #{game.GameNo} — {label}");

                // Reset actions when entering a new betting round
                if (IsBettingStep(game.HandStep) && !IsBettingStep(_lastHandStep))
                    _lastActions.Clear();
            }

            // Show blind posts
            if (game.HandStep == 2 && _lastHandStep != 2)
            {
                var sbPlayer = GetPlayerBySeat(state.Players, game.SmallBlindSeat);
                if (sbPlayer != null)
                    AddLine($"  {ColoredName(sbPlayer)} posts SB {MoneyFormatter.Format(game.SmallBlind)}");
            }
            else if (game.HandStep == 3 && _lastHandStep != 3)
            {
                var bbPlayer = GetPlayerBySeat(state.Players, game.BigBlindSeat);
                if (bbPlayer != null)
                    AddLine($"  {ColoredName(bbPlayer)} posts BB {MoneyFormatter.Format(game.BigBlind)}");
            }

            // Show only NEW player actions during betting rounds
            if (IsBettingStep(game.HandStep) && state.Players != null)
            {
                foreach (var p in state.Players)
                {
                    if (string.IsNullOrEmpty(p.Action)) continue;

                    // Build a key: seat + action + bet amount to detect changes
                    string actionKey = $"{p.Action}_{p.Bet}_{p.TotalBet}";
                    _lastActions.TryGetValue(p.Seat, out string prevKey);

                    if (actionKey != prevKey)
                    {
                        _lastActions[p.Seat] = actionKey;
                        string actionText = FormatAction(p);
                        if (!string.IsNullOrEmpty(actionText))
                            AddLine($"  {actionText}");
                    }
                }
            }

            // Show winners at step 14 or 15 — keep retrying until winnings are populated
            if (game.HandStep >= 14 && !_winnersLoggedThisHand && state.Players != null)
            {
                foreach (var p in state.Players)
                {
                    if (p.IsWinner && p.Winnings > 0)
                    {
                        AddLine($"  {ColoredName(p)} wins <color=#FFD700>{MoneyFormatter.FormatGain(p.Winnings)}</color>");
                        _winnersLoggedThisHand = true;
                    }
                }
            }

            if (game.HandStep == 15 && _lastHandStep != 15)
                AddLine("────────────────");

            _lastHandStep = game.HandStep;
            UpdateDisplay();
        }

        private PlayerState GetPlayerBySeat(List<PlayerState> players, int seat)
        {
            if (players == null) return null;
            foreach (var p in players)
                if (p.Seat == seat) return p;
            return null;
        }

        private static bool IsBettingStep(int step) => PokerConstants.IsBettingStep(step);

        internal static string ColoredName(PlayerState p)
        {
            var c = SeatView.GetSeatColor(p.Seat);
            string hex = ColorUtility.ToHtmlStringRGB(c);
            return $"<color=#{hex}>{p.Username}</color>";
        }

        internal static string FormatAction(PlayerState p)
        {
            string name = ColoredName(p);
            switch (p.Action?.ToLower())
            {
                case "fold":
                    return $"{name} folds";
                case "check":
                    return $"{name} checks";
                case "call":
                    return $"{name} calls {MoneyFormatter.Format(p.Bet)}";
                case "bet":
                    return $"{name} bets {MoneyFormatter.Format(p.Bet)}";
                case "raise":
                    return $"{name} raises to {MoneyFormatter.Format(p.TotalBet)}";
                case "allin":
                    return $"{name} ALL-IN {MoneyFormatter.Format(p.TotalBet)}";
                default:
                    return null;
            }
        }

        private void AddLine(string text)
        {
            _lines.Add(text);
            while (_lines.Count > MaxLines)
                _lines.RemoveAt(0);
        }

        private void UpdateDisplay()
        {
            if (_historyText == null) return;
            _historyText.text = string.Join("\n", _lines);

            // Force layout rebuild and scroll to bottom
            if (_scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                _scrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }
}
