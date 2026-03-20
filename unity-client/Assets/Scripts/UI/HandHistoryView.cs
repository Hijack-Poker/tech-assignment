using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    /// <summary>
    /// Collapsible log panel showing step-by-step actions, hand summaries,
    /// and running stack changes. Frosted white panel with soft shadow.
    /// Right side of screen, collapsed by default.
    /// </summary>
    public class HandHistoryView : MonoBehaviour
    {
        private const int MaxEntries = 200;

        private RectTransform _content;
        private ScrollRect _scrollRect;
        private readonly List<TextMeshProUGUI> _entries = new();
        private bool _scrollPending;

        private readonly Dictionary<int, float> _startOfHandStacks = new();
        private readonly Dictionary<int, string> _prevPlayerActions = new();
        private int _lastGameNo = -1;

        private RectTransform _panelRt;
        private GameObject _panelBody;
        private Image _iconImage;
        private Image _toggleBg;
        private bool _isExpanded = false;

        public static HandHistoryView Create(Transform parent)
        {
            bool portrait = LayoutConfig.IsPortrait;

            // Circular toggle button (always visible)
            int btnSize = portrait ? 36 : 40;
            var toggleGo = new GameObject("HandHistoryToggle", typeof(RectTransform));
            toggleGo.transform.SetParent(parent, false);
            var toggleRt = toggleGo.GetComponent<RectTransform>();

            if (portrait)
            {
                toggleRt.anchorMin = new Vector2(1f, 0f);
                toggleRt.anchorMax = new Vector2(1f, 0f);
                toggleRt.pivot = new Vector2(1f, 0f);
                toggleRt.sizeDelta = new Vector2(btnSize, btnSize);
                toggleRt.anchoredPosition = new Vector2(-8, LayoutConfig.ControlsBarHeight + 8);
            }
            else
            {
                toggleRt.anchorMin = new Vector2(1f, 0.5f);
                toggleRt.anchorMax = new Vector2(1f, 0.5f);
                toggleRt.pivot = new Vector2(1f, 0.5f);
                toggleRt.sizeDelta = new Vector2(btnSize, btnSize);
                toggleRt.anchoredPosition = new Vector2(-4, 0);
            }

            // Soft shadow behind circle
            var shadowGo = new GameObject("ToggleShadow", typeof(RectTransform));
            shadowGo.transform.SetParent(toggleGo.transform, false);
            var shadowRt = shadowGo.GetComponent<RectTransform>();
            UIFactory.StretchFill(shadowRt, -4);
            var shadowImg = shadowGo.AddComponent<Image>();
            shadowImg.sprite = TextureGenerator.GetSoftShadow(48, 48, 24, 6, 0.30f);
            shadowImg.type = Image.Type.Sliced;
            shadowImg.color = Color.white;
            shadowImg.raycastTarget = false;

            // Circle background
            var toggleBg = toggleGo.AddComponent<Image>();
            toggleBg.sprite = TextureGenerator.GetCircle(64);
            toggleBg.color = new Color(0.13f, 0.13f, 0.16f, 0.92f);
            toggleBg.raycastTarget = true;

            var view = toggleGo.AddComponent<HandHistoryView>();
            view._toggleBg = toggleBg;

            var toggleBtn = toggleGo.AddComponent<Button>();
            toggleBtn.targetGraphic = toggleBg;
            var btnColors = toggleBtn.colors;
            btnColors.normalColor = Color.white;
            btnColors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            btnColors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            btnColors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            toggleBtn.colors = btnColors;
            toggleBtn.onClick.AddListener(view.TogglePanel);

            // List icon centered inside circle
            int iconSize = Mathf.RoundToInt(btnSize * 0.55f);
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(toggleGo.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(iconSize, iconSize);
            iconRt.anchoredPosition = Vector2.zero;
            view._iconImage = iconGo.AddComponent<Image>();
            view._iconImage.sprite = TextureGenerator.GetListIcon(32);
            view._iconImage.color = UIFactory.AccentCyan;
            view._iconImage.raycastTarget = false;

            // Panel (collapsible body) — frosted white with soft shadow
            var go = new GameObject("HandHistoryPanel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            view._panelRt = rt;

            if (portrait)
            {
                float barNorm = LayoutConfig.ControlsBarHeight / LayoutConfig.ReferenceResolution.y;
                rt.anchorMin = new Vector2(0.02f, barNorm + 0.02f);
                rt.anchorMax = new Vector2(0.98f, 0.55f);
            }
            else
            {
                rt.anchorMin = new Vector2(0.82f, 0.08f);
                rt.anchorMax = new Vector2(0.995f, 0.92f);
            }
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.10f, 0.13f, 0.94f);
            bg.sprite = TextureGenerator.GetRoundedRect(128, 128, 12);
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = true;

            view._panelBody = go;
            view.BuildUI(go.transform);

            go.SetActive(false);
            toggleGo.transform.SetAsLastSibling();

            return view;
        }

        public void TogglePanel()
        {
            _isExpanded = !_isExpanded;
            _panelBody.SetActive(_isExpanded);

            if (_isExpanded)
            {
                _iconImage.sprite = TextureGenerator.GetCloseIcon(32);
                _iconImage.color = Color.white;
                _toggleBg.color = UIFactory.AccentCyan;
            }
            else
            {
                _iconImage.sprite = TextureGenerator.GetListIcon(32);
                _iconImage.color = UIFactory.AccentCyan;
                _toggleBg.color = new Color(0.13f, 0.13f, 0.16f, 0.92f);
            }
        }

        private void BuildUI(Transform panelTransform)
        {
            // Header bar
            var headerBar = UIFactory.CreatePanel("HeaderBar", panelTransform,
                new Color(0.12f, 0.12f, 0.15f, 1f));
            var headerBarRt = headerBar.GetComponent<RectTransform>();
            headerBarRt.anchorMin = new Vector2(0, 1);
            headerBarRt.anchorMax = new Vector2(1, 1);
            headerBarRt.pivot = new Vector2(0.5f, 1);
            headerBarRt.sizeDelta = new Vector2(0, 28);

            var header = UIFactory.CreateText("Header", headerBar, "Hand History",
                13f, UIFactory.AccentCyan, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.StretchFill(header.GetComponent<RectTransform>());

            // Scroll viewport
            var scrollGo = new GameObject("Scroll", typeof(RectTransform));
            scrollGo.transform.SetParent(panelTransform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(6, 6);
            scrollRt.offsetMax = new Vector2(-6, -32);

            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.color = Color.clear;
            scrollGo.AddComponent<RectMask2D>();

            _scrollRect = scrollGo.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.scrollSensitivity = 20f;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(scrollGo.transform, false);
            _content = contentGo.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0, 1);
            _content.anchorMax = new Vector2(1, 1);
            _content.pivot = new Vector2(0.5f, 1);
            _content.sizeDelta = new Vector2(0, 0);

            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 3;
            layout.padding = new RectOffset(4, 4, 2, 2);

            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scrollRect.content = _content;
        }

        public void LogStateChange(TableResponse oldState, TableResponse newState)
        {
            if (newState?.Game == null) return;

            var game = newState.Game;
            int step = game.HandStep;

            if (_lastGameNo >= 0 && game.GameNo != _lastGameNo)
            {
                AddHandSeparator(oldState);
                RecordStartStacks(newState);
            }
            else if (_lastGameNo < 0)
            {
                RecordStartStacks(newState);
            }
            _lastGameNo = game.GameNo;

            string label = GetShortLabel(step);
            string detail = GenerateDetail(step, newState);
            string entry = string.IsNullOrEmpty(detail)
                ? label
                : $"{label}\n  {detail}";

            AddEntry(entry, GetStepColor(step));

            if (step == 5 || step == 7 || step == 9 || step == 11)
                LogPlayerActionDiffs(oldState, newState);
        }

        public void LogError(string message)
        {
            AddEntry($"<b>ERROR:</b> {message}", UIFactory.HexColor("#f44336"));
        }

        public void Clear()
        {
            foreach (var entry in _entries)
            {
                if (entry != null)
                    Destroy(entry.gameObject);
            }
            _entries.Clear();
            _lastGameNo = -1;
            _startOfHandStacks.Clear();
            _prevPlayerActions.Clear();
        }

        public List<TableContext.HandHistoryEntry> ExportEntries()
        {
            var result = new List<TableContext.HandHistoryEntry>();
            foreach (var tmp in _entries)
            {
                if (tmp != null)
                    result.Add(new TableContext.HandHistoryEntry { Text = tmp.text, Color = tmp.color });
            }
            return result;
        }

        public int ExportLastGameNo() => _lastGameNo;
        public Dictionary<int, float> ExportStartStacks() => new(_startOfHandStacks);
        public Dictionary<int, string> ExportPrevActions() => new(_prevPlayerActions);

        public void ImportEntries(List<TableContext.HandHistoryEntry> entries, int lastGameNo,
            Dictionary<int, float> startStacks, Dictionary<int, string> prevActions)
        {
            Clear();
            _lastGameNo = lastGameNo;

            if (startStacks != null)
                foreach (var kv in startStacks)
                    _startOfHandStacks[kv.Key] = kv.Value;

            if (prevActions != null)
                foreach (var kv in prevActions)
                    _prevPlayerActions[kv.Key] = kv.Value;

            if (entries != null)
                foreach (var entry in entries)
                    AddEntry(entry.Text, entry.Color);
        }

        private void LogPlayerActionDiffs(TableResponse oldState, TableResponse newState)
        {
            if (newState?.Players == null) return;

            var oldActions = new Dictionary<int, string>();
            if (oldState?.Players != null)
            {
                foreach (var p in oldState.Players)
                    oldActions[p.Seat] = p.Action ?? "";
            }

            foreach (var player in newState.Players)
            {
                if (string.IsNullOrEmpty(player.Action)) continue;

                oldActions.TryGetValue(player.Seat, out string prevAction);
                _prevPlayerActions.TryGetValue(player.Seat, out string trackedAction);

                if (player.Action != (trackedAction ?? ""))
                {
                    string actionLower = player.Action.ToLower();
                    string actionHex = UIFactory.ColorToHex(UIFactory.GetActionColor(player.Action));
                    string goldHex = UIFactory.ColorToHex(UIFactory.AccentGold);
                    string betTag = player.Bet > 0
                        ? $" <color={goldHex}>{MoneyFormatter.Format(player.Bet)}</color>"
                        : "";
                    string actionDisplay = actionLower switch
                    {
                        "fold" => $"<color={actionHex}>folds</color>",
                        "check" => $"<color={actionHex}>checks</color>",
                        "call" => $"<color={actionHex}>calls</color>{betTag}",
                        "bet" => $"<color={actionHex}>bets</color>{betTag}",
                        "raise" => $"<color={actionHex}>raises</color>{betTag}",
                        "allin" => $"<color={actionHex}>ALL-IN</color>{betTag}",
                        _ => player.Action
                    };
                    AddEntry($"  <b>{player.Username}</b> {actionDisplay}", UIFactory.TextSecondary);
                    _prevPlayerActions[player.Seat] = player.Action;
                }
            }
        }

        private string GenerateDetail(int step, TableResponse state)
        {
            var game = state.Game;
            string goldHex = UIFactory.ColorToHex(UIFactory.AccentGold);

            switch (step)
            {
                case 0: return $"<color={goldHex}>Hand #{game.GameNo}</color>";
                case 1: return $"Dealer: Seat {game.DealerSeat}";
                case 2:
                    var sbPlayer = FindPlayerBySeat(state, game.SmallBlindSeat);
                    return sbPlayer != null
                        ? $"<b>{sbPlayer.Username}</b> posts <color={goldHex}>{MoneyFormatter.Format(game.SmallBlind)}</color>" : "";
                case 3:
                    var bbPlayer = FindPlayerBySeat(state, game.BigBlindSeat);
                    return bbPlayer != null
                        ? $"<b>{bbPlayer.Username}</b> posts <color={goldHex}>{MoneyFormatter.Format(game.BigBlind)}</color>" : "";
                case 4: return "Cards dealt";
                case 6:
                    if (game.CommunityCards?.Count >= 3)
                        return FormatCardsRichText(game.CommunityCards, 0, 3);
                    return "";
                case 8:
                    if (game.CommunityCards?.Count >= 4)
                        return FormatCardRichText(game.CommunityCards[3]);
                    return "";
                case 10:
                    if (game.CommunityCards?.Count >= 5)
                        return FormatCardRichText(game.CommunityCards[4]);
                    return "";
                case 5: case 7: case 9: case 11:
                    return $"Pot: <color={goldHex}>{MoneyFormatter.Format(game.Pot)}</color>";
                case 12: return "Cards revealed";
                case 13: return GenerateWinnerDetail(state);
                case 14: return GeneratePayoutDetail(state);
                default: return "";
            }
        }

        private string GenerateWinnerDetail(TableResponse state)
        {
            if (state?.Players == null) return "";
            string goldHex = UIFactory.ColorToHex(UIFactory.AccentGold);
            var parts = new List<string>();
            foreach (var p in state.Players)
            {
                if (p.IsWinner)
                {
                    string rankPart = string.IsNullOrEmpty(p.HandRank) ? "" : $" <color={goldHex}>({p.HandRank})</color>";
                    parts.Add($"<b>{p.Username}</b>{rankPart}");
                }
            }
            return parts.Count > 0 ? string.Join(", ", parts) : "";
        }

        private string GeneratePayoutDetail(TableResponse state)
        {
            if (state?.Players == null) return "";
            string greenHex = UIFactory.ColorToHex(UIFactory.AccentGreen);
            string goldHex = UIFactory.ColorToHex(UIFactory.AccentGold);
            var parts = new List<string>();
            foreach (var p in state.Players)
            {
                if (p.Winnings > 0)
                {
                    string rankPart = string.IsNullOrEmpty(p.HandRank) ? "" : $" <color={goldHex}>({p.HandRank})</color>";
                    parts.Add($"<b>{p.Username}</b> <color={greenHex}>+{MoneyFormatter.Format(p.Winnings)}</color>{rankPart}");
                }
            }
            return parts.Count > 0 ? string.Join(", ", parts) : "";
        }

        private void AddHandSeparator(TableResponse oldState)
        {
            if (oldState?.Game == null) return;
            string mutedHex = UIFactory.ColorToHex(UIFactory.TextMuted);
            string goldHex = UIFactory.ColorToHex(UIFactory.AccentGold);
            string greenHex = UIFactory.ColorToHex(UIFactory.AccentGreen);

            AddEntry($"<color={mutedHex}>------------------------</color>", UIFactory.TextMuted);

            string winnerName = "";
            float winnings = 0;
            if (oldState.Players != null)
            {
                foreach (var p in oldState.Players)
                {
                    if (p.IsWinner)
                    {
                        winnerName = p.Username;
                        winnings = p.Winnings;
                        break;
                    }
                }
            }
            string summary = string.IsNullOrEmpty(winnerName)
                ? $"<color={goldHex}>Hand #{oldState.Game.GameNo}</color>"
                : $"<color={goldHex}>Hand #{oldState.Game.GameNo}</color> <b>{winnerName}</b> wins <color={greenHex}>{MoneyFormatter.Format(winnings)}</color>";
            AddEntry(summary, UIFactory.TextPrimary);
            AddStackDeltas(oldState);
        }

        private void AddStackDeltas(TableResponse state)
        {
            if (state?.Players == null || _startOfHandStacks.Count == 0) return;
            string greenHex = UIFactory.ColorToHex(UIFactory.AccentGreen);
            string redHex = UIFactory.ColorToHex(UIFactory.AccentMagenta);
            var parts = new List<string>();
            foreach (var p in state.Players)
            {
                if (_startOfHandStacks.TryGetValue(p.PlayerId, out float startStack))
                {
                    float delta = p.Stack - startStack;
                    if (Mathf.Abs(delta) > 0.01f)
                    {
                        string colorHex = delta > 0 ? greenHex : redHex;
                        string sign = delta > 0 ? "+" : "";
                        parts.Add($"  {p.Username} <color={colorHex}>{sign}{MoneyFormatter.Format(delta)}</color>");
                    }
                }
            }
            if (parts.Count > 0)
                AddEntry(string.Join("\n", parts), UIFactory.TextSecondary);
        }

        private void RecordStartStacks(TableResponse state)
        {
            _startOfHandStacks.Clear();
            if (state?.Players == null) return;
            foreach (var p in state.Players)
                _startOfHandStacks[p.PlayerId] = p.Stack;
        }

        private void AddEntry(string text, Color color)
        {
            var tmp = UIFactory.CreateText("Entry", _content, text,
                12f, color, TextAlignmentOptions.TopLeft);
            tmp.richText = true;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;

            var le = tmp.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 14;

            _entries.Add(tmp);

            while (_entries.Count > MaxEntries)
            {
                var oldest = _entries[0];
                _entries.RemoveAt(0);
                if (oldest != null) Destroy(oldest.gameObject);
            }

            _scrollPending = true;
        }

        private void LateUpdate()
        {
            if (_scrollPending && _scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 0f;
                _scrollPending = false;
            }
        }

        private static Color GetStepColor(int step)
        {
            if (step <= 1) return UIFactory.TextMuted;
            if (step == 4 || step == 6 || step == 8 || step == 10)
                return UIFactory.AccentCyan;
            if (step >= 13) return UIFactory.AccentGold;
            return UIFactory.TextPrimary;
        }

        private static PlayerState FindPlayerBySeat(TableResponse state, int seat)
        {
            if (state?.Players == null) return null;
            foreach (var p in state.Players)
                if (p.Seat == seat) return p;
            return null;
        }

        private static string GetShortLabel(int step)
        {
            return step switch
            {
                0 => "<b>New Hand</b>",
                1 => "<b>Dealer</b>",
                2 => "<b>Small Blind</b>",
                3 => "<b>Big Blind</b>",
                4 => "<b>Hole Cards</b>",
                5 => "<b>Pre-Flop Betting</b>",
                6 => "<b>Flop</b>",
                7 => "<b>Flop Betting</b>",
                8 => "<b>Turn</b>",
                9 => "<b>Turn Betting</b>",
                10 => "<b>River</b>",
                11 => "<b>River Betting</b>",
                12 => "<b>Showdown</b>",
                13 => "<b>Results</b>",
                14 => "<b>Payouts</b>",
                _ => $"<b>Step {step}</b>"
            };
        }

        private static string FormatCardRichText(string card)
        {
            try
            {
                var parsed = CardUtils.Parse(card);
                var suitColor = UIFactory.GetSuitColor(parsed.Symbol[0]);
                // Black suits are invisible on dark panel — use TextPrimary instead
                if (suitColor == UIFactory.CardBlack || suitColor == UIFactory.SuitSpades)
                    suitColor = UIFactory.TextPrimary;
                string hex = UIFactory.ColorToHex(suitColor);
                return $"<color={hex}>{parsed.Display}</color>";
            }
            catch { return card; }
        }

        private static string FormatCardsRichText(List<string> cards, int start, int count)
        {
            var parts = new List<string>();
            for (int i = start; i < start + count && i < cards.Count; i++)
                parts.Add(FormatCardRichText(cards[i]));
            return string.Join("  ", parts);
        }

        private static string FormatCard(string card)
        {
            try
            {
                var parsed = CardUtils.Parse(card);
                return parsed.Display;
            }
            catch { return card; }
        }

        private static string FormatCards(List<string> cards, int start, int count)
        {
            var parts = new List<string>();
            for (int i = start; i < start + count && i < cards.Count; i++)
                parts.Add(FormatCard(cards[i]));
            return string.Join(" ", parts);
        }
    }
}
