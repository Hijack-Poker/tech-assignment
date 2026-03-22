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
        private CanvasGroup _panelCG;
        private TextMeshProUGUI _contentText;
        private TextMeshProUGUI _tabLabel;
        private bool _isOpen;

        private const float PanelWidth = 220f;
        private const float PanelHeight = 380f;
        private const float SlideTime = 0.3f;

        private static readonly Color ProfitGreen = new Color(0.3f, 0.85f, 0.3f);
        private static readonly Color LossRed = new Color(0.95f, 0.3f, 0.3f);
        private static readonly Color LabelColor = new Color(0.55f, 0.55f, 0.6f);
        private static readonly Color ValueColor = new Color(0.92f, 0.92f, 0.95f);
        private static readonly Color GoldColor = new Color(0.9f, 0.85f, 0.6f);

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

            _panel.SetActive(true);
            DOTween.Kill(_panelCG);

            if (_isOpen)
            {
                _panelCG.alpha = 0f;
                DOTween.To(() => _panelCG.alpha, x => _panelCG.alpha = x, 1f, SlideTime)
                    .SetEase(Ease.OutCubic).SetTarget(_panelCG);
                if (_tracker != null) UpdateContent(_tracker.CurrentStats);
            }
            else
            {
                DOTween.To(() => _panelCG.alpha, x => _panelCG.alpha = x, 0f, SlideTime)
                    .SetEase(Ease.InCubic).SetTarget(_panelCG)
                    .OnComplete(() => _panel.SetActive(false));
            }

            if (_tabLabel != null)
                _tabLabel.text = _isOpen ? "STATS  ✕" : "STATS";
        }

        private void UpdateContent(SessionStats stats)
        {
            if (_contentText == null || stats == null) return;

            string pc = ColorUtility.ToHtmlStringRGB(stats.TotalProfit >= 0 ? ProfitGreen : LossRed);
            string lc = ColorUtility.ToHtmlStringRGB(LabelColor);
            string vc = ColorUtility.ToHtmlStringRGB(ValueColor);
            string gc = ColorUtility.ToHtmlStringRGB(ProfitGreen);
            string rc = ColorUtility.ToHtmlStringRGB(LossRed);

            string streak;
            if (stats.CurrentStreak > 0) streak = $"<color=#{gc}>W{stats.CurrentStreak}</color>";
            else if (stats.CurrentStreak < 0) streak = $"<color=#{rc}>L{Mathf.Abs(stats.CurrentStreak)}</color>";
            else streak = "—";

            _contentText.text =
                $"<color=#{lc}>Hands</color> <color=#{vc}>{stats.HandsPlayed}</color>     " +
                $"<color=#{lc}>Win%</color> <color=#{vc}>{stats.WinRate:P0}</color>\n\n" +

                $"<color=#{lc}>Profit</color>\n" +
                $"<color=#{pc}><size=22>{MoneyFormatter.FormatGain(stats.TotalProfit)}</size></color>\n\n" +

                $"<color=#{lc}>Best Win</color>  <color=#{vc}>{MoneyFormatter.Format(stats.BiggestWin)}</color>\n" +
                $"<color=#{lc}>Worst Loss</color>  <color=#{vc}>{MoneyFormatter.Format(stats.BiggestLoss)}</color>\n\n" +

                $"<color=#{lc}>Streak</color> {streak}   " +
                $"<color=#{lc}>Peak</color> <color=#{vc}>W{stats.BestStreak}/L{Mathf.Abs(stats.WorstStreak)}</color>\n\n" +

                $"<color=#{lc}>VPIP</color>  <color=#{vc}>{stats.VPIP:P0}</color>\n\n" +

                $"<color=#{lc}>Actions</color>\n" +
                $"<color=#{vc}>Fold:{stats.FoldCount}  Call:{stats.CallCount}  Raise:{stats.RaiseCount}  AI:{stats.AllInCount}</color>\n\n" +

                $"<color=#{lc}>Position Wins</color>\n" +
                $"<color=#{vc}>BTN:{stats.WinsFromButton}  Blind:{stats.WinsFromBlinds}  Other:{stats.WinsFromOther}</color>";
        }

        private void EnsureUI()
        {
            if (_panel != null) return;

            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            // --- Tab Button: top-left corner ---
            _tabButton = new GameObject("StatsTabButton", typeof(RectTransform), typeof(Image), typeof(Button));
            _tabButton.transform.SetParent(canvas.transform, false);

            var tabRt = _tabButton.GetComponent<RectTransform>();
            tabRt.anchorMin = new Vector2(0f, 1f);
            tabRt.anchorMax = new Vector2(0f, 1f);
            tabRt.pivot = new Vector2(0f, 1f);
            tabRt.anchoredPosition = new Vector2(8f, -8f);
            tabRt.sizeDelta = new Vector2(80f, 28f);

            var tabBg = _tabButton.GetComponent<Image>();
            tabBg.color = new Color(0.1f, 0.12f, 0.2f, 0.95f);

            _tabButton.GetComponent<Button>().onClick.AddListener(TogglePanel);

            var tabLabelGO = new GameObject("TabLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            tabLabelGO.transform.SetParent(_tabButton.transform, false);
            var tabLabelRt = tabLabelGO.GetComponent<RectTransform>();
            tabLabelRt.anchorMin = Vector2.zero;
            tabLabelRt.anchorMax = Vector2.one;
            tabLabelRt.offsetMin = Vector2.zero;
            tabLabelRt.offsetMax = Vector2.zero;

            _tabLabel = tabLabelGO.GetComponent<TextMeshProUGUI>();
            _tabLabel.text = "STATS";
            _tabLabel.fontSize = 13;
            _tabLabel.color = GoldColor;
            _tabLabel.alignment = TextAlignmentOptions.Center;
            _tabLabel.fontStyle = FontStyles.Bold;

            // --- Panel: directly below the tab button ---
            _panel = new GameObject("SessionStatsPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _panel.transform.SetParent(canvas.transform, false);

            _panelRt = _panel.GetComponent<RectTransform>();
            _panelRt.anchorMin = new Vector2(0f, 1f);
            _panelRt.anchorMax = new Vector2(0f, 1f);
            _panelRt.pivot = new Vector2(0f, 1f);
            _panelRt.anchoredPosition = new Vector2(8f, -40f);
            _panelRt.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            _panelCG = _panel.GetComponent<CanvasGroup>();
            _panelCG.alpha = 0f;

            var panelBg = _panel.GetComponent<Image>();
            panelBg.color = new Color(0.05f, 0.06f, 0.1f, 0.94f);

            // Title bar
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(_panel.transform, false);
            var titleRt = titleGO.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -6f);
            titleRt.sizeDelta = new Vector2(0f, 24f);

            var titleText = titleGO.GetComponent<TextMeshProUGUI>();
            titleText.text = "SESSION STATS";
            titleText.fontSize = 14;
            titleText.color = GoldColor;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;

            // Content
            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(TextMeshProUGUI));
            contentGO.transform.SetParent(_panel.transform, false);
            var contentRt = contentGO.GetComponent<RectTransform>();
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.offsetMin = new Vector2(12f, 10f);
            contentRt.offsetMax = new Vector2(-12f, -34f);

            _contentText = contentGO.GetComponent<TextMeshProUGUI>();
            _contentText.fontSize = 13;
            _contentText.color = ValueColor;
            _contentText.alignment = TextAlignmentOptions.TopLeft;
            _contentText.richText = true;
            _contentText.enableWordWrapping = true;

            // Ensure render order: panel behind tab
            _panel.transform.SetAsLastSibling();
            _tabButton.transform.SetAsLastSibling();

            _panel.SetActive(false);
            UpdateContent(new SessionStats());
        }

        private void OnDestroy()
        {
            DOTween.Kill(_panelCG);
            if (_panel != null) Destroy(_panel);
            if (_tabButton != null) Destroy(_tabButton);
        }
    }
}
