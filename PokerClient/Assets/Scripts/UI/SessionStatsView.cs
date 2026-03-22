using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using HijackPoker.AI;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    public class SessionStatsView : MonoBehaviour
    {
        private SessionTracker _tracker;
        private GameObject _panel;
        private GameObject _tabButton;
        private RectTransform _panelRt;
        private TextMeshProUGUI _contentText;
        private bool _isOpen;

        private const float PanelWidth = 260f;
        private const float SlideTime = 0.35f;

        private static readonly Color ProfitGreen = new Color(0.3f, 0.85f, 0.3f);
        private static readonly Color LossRed = new Color(0.95f, 0.3f, 0.3f);
        private static readonly Color LabelColor = new Color(0.65f, 0.65f, 0.7f);
        private static readonly Color ValueColor = new Color(0.92f, 0.92f, 0.95f);

        private void Awake()
        {
            _tracker = FindObjectOfType<SessionTracker>();
        }

        private void OnEnable()
        {
            if (_tracker != null)
                _tracker.OnStatsUpdated += OnStatsUpdated;
        }

        private void OnDisable()
        {
            if (_tracker != null)
                _tracker.OnStatsUpdated -= OnStatsUpdated;
        }

        private void Start()
        {
            EnsureUI();
        }

        private void OnStatsUpdated(SessionStats stats)
        {
            UpdateContent(stats);
        }

        private void TogglePanel()
        {
            _isOpen = !_isOpen;

            DOTween.Kill(_panelRt);
            float targetX = _isOpen ? -PanelWidth : 0f;
            DOTween.To(() => _panelRt.anchoredPosition, v => _panelRt.anchoredPosition = v,
                    new Vector2(targetX, _panelRt.anchoredPosition.y), SlideTime)
                .SetEase(_isOpen ? Ease.OutCubic : Ease.InCubic)
                .SetTarget(_panelRt);

            // Update content when opening
            if (_isOpen && _tracker != null)
                UpdateContent(_tracker.CurrentStats);
        }

        private void UpdateContent(SessionStats stats)
        {
            if (_contentText == null || stats == null) return;

            string profitColor = stats.TotalProfit >= 0
                ? ColorUtility.ToHtmlStringRGB(ProfitGreen)
                : ColorUtility.ToHtmlStringRGB(LossRed);

            string streakStr;
            if (stats.CurrentStreak > 0)
                streakStr = $"<color=#{ColorUtility.ToHtmlStringRGB(ProfitGreen)}>W{stats.CurrentStreak}</color>";
            else if (stats.CurrentStreak < 0)
                streakStr = $"<color=#{ColorUtility.ToHtmlStringRGB(LossRed)}>L{Mathf.Abs(stats.CurrentStreak)}</color>";
            else
                streakStr = "—";

            string lbl = $"<color=#{ColorUtility.ToHtmlStringRGB(LabelColor)}>";
            string val = $"<color=#{ColorUtility.ToHtmlStringRGB(ValueColor)}>";

            _contentText.text =
                $"{lbl}Hands Played</color>\n{val}{stats.HandsPlayed}</color>\n\n" +
                $"{lbl}Win Rate</color>\n{val}{stats.WinRate:P0}</color>\n\n" +
                $"{lbl}Total Profit</color>\n<color=#{profitColor}>{MoneyFormatter.FormatGain(stats.TotalProfit)}</color>\n\n" +
                $"{lbl}Biggest Win</color>\n{val}{MoneyFormatter.Format(stats.BiggestWin)}</color>\n\n" +
                $"{lbl}Biggest Loss</color>\n{val}{MoneyFormatter.Format(stats.BiggestLoss)}</color>\n\n" +
                $"{lbl}Current Streak</color>\n{streakStr}\n\n" +
                $"{lbl}Best / Worst</color>\n{val}W{stats.BestStreak} / L{Mathf.Abs(stats.WorstStreak)}</color>\n\n" +
                $"{lbl}VPIP</color>\n{val}{stats.VPIP:P0}</color>\n\n" +
                $"{lbl}Actions</color>\n{val}F:{stats.FoldCount}  C:{stats.CallCount}  R:{stats.RaiseCount}  AI:{stats.AllInCount}</color>\n\n" +
                $"{lbl}Wins by Position</color>\n{val}BTN:{stats.WinsFromButton}  Blinds:{stats.WinsFromBlinds}  Other:{stats.WinsFromOther}</color>";
        }

        private void EnsureUI()
        {
            if (_panel != null) return;

            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            // --- Tab Button (always visible, right edge) ---
            _tabButton = new GameObject("StatsTabButton", typeof(RectTransform), typeof(Image), typeof(Button));
            _tabButton.transform.SetParent(canvas.transform, false);

            var tabRt = _tabButton.GetComponent<RectTransform>();
            tabRt.anchorMin = new Vector2(1f, 0.5f);
            tabRt.anchorMax = new Vector2(1f, 0.5f);
            tabRt.pivot = new Vector2(1f, 0.5f);
            tabRt.anchoredPosition = new Vector2(0f, 0f);
            tabRt.sizeDelta = new Vector2(36f, 80f);

            var tabBg = _tabButton.GetComponent<Image>();
            tabBg.color = new Color(0.15f, 0.15f, 0.22f, 0.9f);

            var tabBtn = _tabButton.GetComponent<Button>();
            tabBtn.onClick.AddListener(TogglePanel);

            // Tab label
            var tabLabelGO = new GameObject("TabLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            tabLabelGO.transform.SetParent(_tabButton.transform, false);

            var tabLabelRt = tabLabelGO.GetComponent<RectTransform>();
            tabLabelRt.anchorMin = Vector2.zero;
            tabLabelRt.anchorMax = Vector2.one;
            tabLabelRt.offsetMin = Vector2.zero;
            tabLabelRt.offsetMax = Vector2.zero;

            var tabLabel = tabLabelGO.GetComponent<TextMeshProUGUI>();
            tabLabel.text = "S\nT\nA\nT\nS";
            tabLabel.fontSize = 11;
            tabLabel.color = new Color(0.7f, 0.7f, 0.8f);
            tabLabel.alignment = TextAlignmentOptions.Center;
            tabLabel.fontStyle = FontStyles.Bold;
            tabLabel.lineSpacing = -20f;

            // --- Slide-in Panel (starts off-screen right) ---
            _panel = new GameObject("SessionStatsPanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(canvas.transform, false);

            _panelRt = _panel.GetComponent<RectTransform>();
            _panelRt.anchorMin = new Vector2(1f, 0f);
            _panelRt.anchorMax = new Vector2(1f, 1f);
            _panelRt.pivot = new Vector2(0f, 0.5f);
            _panelRt.anchoredPosition = new Vector2(0f, 0f);
            _panelRt.sizeDelta = new Vector2(PanelWidth, 0f);

            var panelBg = _panel.GetComponent<Image>();
            panelBg.color = new Color(0.06f, 0.06f, 0.1f, 0.95f);

            // Title
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(_panel.transform, false);

            var titleRt = titleGO.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -10f);
            titleRt.sizeDelta = new Vector2(0f, 30f);

            var titleText = titleGO.GetComponent<TextMeshProUGUI>();
            titleText.text = "SESSION STATS";
            titleText.fontSize = 16;
            titleText.color = new Color(0.9f, 0.85f, 0.6f);
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;

            // Content area
            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(TextMeshProUGUI));
            contentGO.transform.SetParent(_panel.transform, false);

            var contentRt = contentGO.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 0f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.offsetMin = new Vector2(14f, 14f);
            contentRt.offsetMax = new Vector2(-14f, -46f);

            _contentText = contentGO.GetComponent<TextMeshProUGUI>();
            _contentText.fontSize = 13;
            _contentText.color = ValueColor;
            _contentText.alignment = TextAlignmentOptions.TopLeft;
            _contentText.richText = true;
            _contentText.enableWordWrapping = true;

            UpdateContent(new SessionStats());
        }

        private void OnDestroy()
        {
            DOTween.Kill(_panelRt);
            if (_panel != null) Destroy(_panel);
            if (_tabButton != null) Destroy(_tabButton);
        }
    }
}
