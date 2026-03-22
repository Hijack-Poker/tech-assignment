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
        private CanvasGroup _panelCG;
        private TextMeshProUGUI _contentText;
        private TextMeshProUGUI _reviewText;
        private TextMeshProUGUI _tipsText;
        private TextMeshProUGUI _tabLabel;
        private bool _isOpen;

        private const float PanelWidth = 250f;
        private const float PanelHeight = 560f;

        private static readonly Color ProfitGreen = new Color(0.3f, 0.85f, 0.3f);
        private static readonly Color LossRed = new Color(0.95f, 0.3f, 0.3f);
        private static readonly Color LabelColor = new Color(0.5f, 0.5f, 0.55f);
        private static readonly Color ValueColor = new Color(0.92f, 0.92f, 0.95f);
        private static readonly Color GoldColor = new Color(0.9f, 0.85f, 0.55f);
        private static readonly Color TipCritColor = new Color(1f, 0.4f, 0.35f);
        private static readonly Color TipWarnColor = new Color(1f, 0.8f, 0.3f);
        private static readonly Color TipInfoColor = new Color(0.5f, 0.8f, 1f);
        private static readonly Color ReviewColor = new Color(0.82f, 0.88f, 0.95f);

        private void Awake() => _tracker = FindObjectOfType<SessionTracker>();

        private void OnEnable()
        {
            if (_tracker != null) _tracker.OnStatsUpdated += OnStatsUpdated;
        }

        private void OnDisable()
        {
            if (_tracker != null) _tracker.OnStatsUpdated -= OnStatsUpdated;
        }

        private void Start() => EnsureUI();

        private void OnStatsUpdated(SessionStats stats) => UpdateContent(stats);

        private void TogglePanel()
        {
            _isOpen = !_isOpen;
            _panel.SetActive(true);
            DOTween.Kill(_panelCG);

            if (_isOpen)
            {
                _panelCG.alpha = 0f;
                DOTween.To(() => _panelCG.alpha, x => _panelCG.alpha = x, 1f, 0.25f)
                    .SetEase(Ease.OutCubic).SetTarget(_panelCG);
                if (_tracker != null) UpdateContent(_tracker.CurrentStats);
            }
            else
            {
                DOTween.To(() => _panelCG.alpha, x => _panelCG.alpha = x, 0f, 0.2f)
                    .SetEase(Ease.InCubic).SetTarget(_panelCG)
                    .OnComplete(() => _panel.SetActive(false));
            }

            if (_tabLabel != null)
                _tabLabel.text = _isOpen ? "AI COACH  \u2715" : "AI COACH";
        }

        private void UpdateContent(SessionStats s)
        {
            if (_contentText == null || s == null) return;

            string lc = ColorUtility.ToHtmlStringRGB(LabelColor);
            string vc = ColorUtility.ToHtmlStringRGB(ValueColor);
            string gc = ColorUtility.ToHtmlStringRGB(ProfitGreen);
            string rc = ColorUtility.ToHtmlStringRGB(LossRed);
            string pc = ColorUtility.ToHtmlStringRGB(s.TotalProfit >= 0 ? ProfitGreen : LossRed);

            string streak;
            if (s.CurrentStreak > 0) streak = $"<color=#{gc}>W{s.CurrentStreak}</color>";
            else if (s.CurrentStreak < 0) streak = $"<color=#{rc}>L{Mathf.Abs(s.CurrentStreak)}</color>";
            else streak = "\u2014";

            _contentText.text =
                // Profit hero
                $"<color=#{pc}><size=22>{MoneyFormatter.FormatGain(s.TotalProfit)}</size></color>\n" +
                $"<color=#{lc}><size=10>session profit</size></color>\n\n" +

                // Core stats row 1
                $"<color=#{lc}>VPIP</color> <color=#{vc}>{s.VPIP:P0}</color>   " +
                $"<color=#{lc}>PFR</color> <color=#{vc}>{s.PFR:P0}</color>   " +
                $"<color=#{lc}>AF</color> <color=#{vc}>{s.AF:F1}</color>\n" +

                // Core stats row 2 — showdown & c-bet
                $"<color=#{lc}>WTSD</color> <color=#{vc}>{s.WTSD:P0}</color>   " +
                $"<color=#{lc}>WSD</color> <color=#{vc}>{s.WSD:P0}</color>   " +
                (s.ContinuationBetOpps > 0
                    ? $"<color=#{lc}>CB</color> <color=#{vc}>{s.CBet:P0}</color>"
                    : $"<color=#{lc}>CB</color> <color=#{vc}>\u2014</color>") + "\n\n" +

                // Record
                $"<color=#{lc}>Hands</color> <color=#{vc}>{s.HandsPlayed}</color>   " +
                $"<color=#{lc}>Won</color> <color=#{vc}>{s.HandsWon}</color>   " +
                $"<color=#{lc}>Win%</color> <color=#{vc}>{s.WinRate:P0}</color>\n\n" +

                // Wins/losses
                $"<color=#{lc}>Best Win</color> <color=#{gc}>{MoneyFormatter.Format(s.BiggestWin)}</color>   " +
                $"<color=#{lc}>Worst</color> <color=#{rc}>{MoneyFormatter.Format(s.BiggestLoss)}</color>\n\n" +

                // Streak
                $"<color=#{lc}>Streak</color> {streak}   " +
                $"<color=#{lc}>Peak</color> <color=#{vc}>W{s.BestStreak} / L{Mathf.Abs(s.WorstStreak)}</color>\n\n" +

                // Position
                $"<color=#{lc}>Position W/L</color>\n" +
                $"<color=#{vc}>BTN:{s.WinsFromButton}/{s.HandsFromButton}  " +
                $"Blind:{s.WinsFromBlinds}/{s.HandsFromBlinds}  " +
                $"Other:{s.WinsFromOther}/{s.HandsFromOther}</color>";

            // Hand review
            if (_reviewText != null)
            {
                string revColor = ColorUtility.ToHtmlStringRGB(ReviewColor);
                if (!string.IsNullOrEmpty(s.LastHandReview))
                    _reviewText.text = $"<color=#{revColor}>{s.LastHandReview}</color>";
                else
                    _reviewText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(LabelColor)}>Complete a hand to see analysis...</color>";
            }

            // Coaching tips
            if (_tipsText != null)
            {
                if (s.Tips == null || s.Tips.Count == 0)
                {
                    _tipsText.text = "";
                }
                else
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var tip in s.Tips)
                    {
                        Color tc = tip.Severity == 2 ? TipCritColor : tip.Severity == 1 ? TipWarnColor : TipInfoColor;
                        string icon = tip.Severity == 2 ? "\u26a0" : tip.Severity == 1 ? "\u25cf" : "\u2713";
                        sb.AppendLine($"<color=#{ColorUtility.ToHtmlStringRGB(tc)}>{icon} {tip.Message}</color>");
                    }
                    _tipsText.text = sb.ToString().TrimEnd();
                }
            }
        }

        private void EnsureUI()
        {
            if (_panel != null) return;
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            // Tab button — top-left
            _tabButton = new GameObject("CoachTabButton", typeof(RectTransform), typeof(Image), typeof(Button));
            _tabButton.transform.SetParent(canvas.transform, false);

            var tabRt = _tabButton.GetComponent<RectTransform>();
            tabRt.anchorMin = new Vector2(0f, 1f);
            tabRt.anchorMax = new Vector2(0f, 1f);
            tabRt.pivot = new Vector2(0f, 1f);
            tabRt.anchoredPosition = new Vector2(8f, -52f);
            tabRt.sizeDelta = new Vector2(100f, 28f);
            _tabButton.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.18f, 0.95f);
            _tabButton.GetComponent<Button>().onClick.AddListener(TogglePanel);

            var tabLabelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            tabLabelGO.transform.SetParent(_tabButton.transform, false);
            var tlRt = tabLabelGO.GetComponent<RectTransform>();
            tlRt.anchorMin = Vector2.zero;
            tlRt.anchorMax = Vector2.one;
            tlRt.offsetMin = tlRt.offsetMax = Vector2.zero;
            _tabLabel = tabLabelGO.GetComponent<TextMeshProUGUI>();
            _tabLabel.text = "AI COACH";
            _tabLabel.fontSize = 12;
            _tabLabel.color = GoldColor;
            _tabLabel.alignment = TextAlignmentOptions.Center;
            _tabLabel.fontStyle = FontStyles.Bold;

            // Panel — below tab
            _panel = new GameObject("CoachPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _panel.transform.SetParent(canvas.transform, false);

            var panelRt = _panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 1f);
            panelRt.anchorMax = new Vector2(0f, 1f);
            panelRt.pivot = new Vector2(0f, 1f);
            panelRt.anchoredPosition = new Vector2(8f, -84f);
            panelRt.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            _panelCG = _panel.GetComponent<CanvasGroup>();
            _panelCG.alpha = 0f;
            _panel.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.09f, 0.96f);

            // ── Stats content (top) ──
            var statsGO = new GameObject("Stats", typeof(RectTransform), typeof(TextMeshProUGUI));
            statsGO.transform.SetParent(_panel.transform, false);
            var sRt = statsGO.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0f, 0.54f);
            sRt.anchorMax = Vector2.one;
            sRt.offsetMin = new Vector2(12f, 0f);
            sRt.offsetMax = new Vector2(-12f, -10f);
            _contentText = statsGO.GetComponent<TextMeshProUGUI>();
            _contentText.fontSize = 11;
            _contentText.color = ValueColor;
            _contentText.alignment = TextAlignmentOptions.TopLeft;
            _contentText.richText = true;
            _contentText.enableWordWrapping = true;

            // ── Divider 1 ──
            CreateDivider(_panel.transform, 0.53f);

            // ── Hand Review header ──
            CreateSectionHeader(_panel.transform, "HAND REVIEW", 0.48f, 0.53f);

            // ── Hand Review text ──
            var reviewGO = new GameObject("Review", typeof(RectTransform), typeof(TextMeshProUGUI));
            reviewGO.transform.SetParent(_panel.transform, false);
            var rRt = reviewGO.GetComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0f, 0.30f);
            rRt.anchorMax = new Vector2(1f, 0.48f);
            rRt.offsetMin = new Vector2(12f, 0f);
            rRt.offsetMax = new Vector2(-12f, 0f);
            _reviewText = reviewGO.GetComponent<TextMeshProUGUI>();
            _reviewText.fontSize = 10;
            _reviewText.color = ReviewColor;
            _reviewText.alignment = TextAlignmentOptions.TopLeft;
            _reviewText.richText = true;
            _reviewText.enableWordWrapping = true;

            // ── Divider 2 ──
            CreateDivider(_panel.transform, 0.29f);

            // ── Tips header ──
            CreateSectionHeader(_panel.transform, "COACHING TIPS", 0.24f, 0.29f);

            // ── Tips content (bottom) ──
            var tipsGO = new GameObject("Tips", typeof(RectTransform), typeof(TextMeshProUGUI));
            tipsGO.transform.SetParent(_panel.transform, false);
            var tRt = tipsGO.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = new Vector2(1f, 0.24f);
            tRt.offsetMin = new Vector2(12f, 8f);
            tRt.offsetMax = new Vector2(-12f, 0f);
            _tipsText = tipsGO.GetComponent<TextMeshProUGUI>();
            _tipsText.fontSize = 10;
            _tipsText.color = ValueColor;
            _tipsText.alignment = TextAlignmentOptions.TopLeft;
            _tipsText.richText = true;
            _tipsText.enableWordWrapping = true;

            // Render order
            _panel.transform.SetAsLastSibling();
            _tabButton.transform.SetAsLastSibling();
            _panel.SetActive(false);

            UpdateContent(new SessionStats());
        }

        private void CreateDivider(Transform parent, float yAnchor)
        {
            var divGO = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            divGO.transform.SetParent(parent, false);
            var dRt = divGO.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.05f, yAnchor);
            dRt.anchorMax = new Vector2(0.95f, yAnchor);
            dRt.pivot = new Vector2(0.5f, 0.5f);
            dRt.sizeDelta = new Vector2(0f, 1f);
            divGO.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.35f, 0.5f);
        }

        private void CreateSectionHeader(Transform parent, string title, float yMin, float yMax)
        {
            var headerGO = new GameObject("Header_" + title, typeof(RectTransform), typeof(TextMeshProUGUI));
            headerGO.transform.SetParent(parent, false);
            var hRt = headerGO.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, yMin);
            hRt.anchorMax = new Vector2(1f, yMax);
            hRt.offsetMin = new Vector2(12f, 0f);
            hRt.offsetMax = new Vector2(-12f, 0f);
            var headerTmp = headerGO.GetComponent<TextMeshProUGUI>();
            headerTmp.text = title;
            headerTmp.fontSize = 10;
            headerTmp.color = GoldColor;
            headerTmp.alignment = TextAlignmentOptions.Left;
            headerTmp.fontStyle = FontStyles.Bold;
        }

        private void OnDestroy()
        {
            DOTween.Kill(_panelCG);
            if (_panel != null) Destroy(_panel);
            if (_tabButton != null) Destroy(_tabButton);
        }
    }
}
