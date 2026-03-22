using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.UI;

namespace HijackPoker.AI
{
    public class TiltDetector : MonoBehaviour
    {
        private HandDataCollector _collector;
        private TableStateManager _stateManager;

        private readonly Dictionary<int, GameObject> _seatLabels = new();
        private readonly Dictionary<int, string> _lastAdvice = new();

        // Toast UI
        private GameObject _toastPanel;
        private TextMeshProUGUI _toastText;
        private CanvasGroup _toastCG;
        private int _lastToastGameNo = -1;

        private static readonly Color TagColor = new Color(0.3f, 0.7f, 1f);      // blue
        private static readonly Color LagColor = new Color(1f, 0.6f, 0.2f);      // orange
        private static readonly Color FishColor = new Color(0.4f, 0.9f, 0.4f);   // green
        private static readonly Color NitColor = new Color(0.6f, 0.6f, 0.7f);    // gray
        private static readonly Color ManiacColor = new Color(1f, 0.3f, 0.8f);   // pink
        private static readonly Color TiltColor = new Color(0.95f, 0.2f, 0.15f); // red
        private static readonly Color RegColor = new Color(0.75f, 0.75f, 0.8f);  // light gray

        private void Awake()
        {
            _collector = FindObjectOfType<HandDataCollector>();
            _stateManager = FindObjectOfType<TableStateManager>();
        }

        private void OnEnable()
        {
            if (_collector != null) _collector.OnHandCompleted += OnHandCompleted;
            if (_stateManager != null) _stateManager.OnTableReset += OnTableReset;
        }

        private void OnDisable()
        {
            if (_collector != null) _collector.OnHandCompleted -= OnHandCompleted;
            if (_stateManager != null) _stateManager.OnTableReset -= OnTableReset;
        }

        private void OnTableReset()
        {
            ClearLabels();
            _lastAdvice.Clear();
            _lastToastGameNo = -1;
        }

        private void OnHandCompleted(HandSnapshot hand)
        {
            UpdateSeatLabels();

            // Check for exploitation advice
            foreach (var kvp in _collector.Profiles)
            {
                var profile = kvp.Value;
                string advice = GetExploitAdvice(profile);
                if (advice == null) continue;

                _lastAdvice.TryGetValue(kvp.Key, out string prev);
                if (advice != prev && hand.GameNo != _lastToastGameNo)
                {
                    _lastAdvice[kvp.Key] = advice;
                    _lastToastGameNo = hand.GameNo;
                    ShowToast(advice);
                    break; // one toast per hand
                }
            }
        }

        private void UpdateSeatLabels()
        {
            var seatViews = FindObjectsOfType<SeatView>();

            foreach (var kvp in _collector.Profiles)
            {
                int seat = kvp.Key;
                var profile = kvp.Value;
                string style = profile.GetPlayStyle();

                if (style == null)
                {
                    HideLabel(seat);
                    continue;
                }

                // Find SeatView
                SeatView sv = null;
                foreach (var s in seatViews)
                {
                    string goName = s.gameObject.name;
                    if (goName.StartsWith("Seat") && int.TryParse(goName.Substring(4), out int sn) && sn == seat)
                    { sv = s; break; }
                }
                if (sv == null) continue;

                ShowLabel(seat, sv, style, profile);
            }
        }

        private void ShowLabel(int seat, SeatView sv, string style, PlayerProfile profile)
        {
            if (!_seatLabels.TryGetValue(seat, out var go) || go == null)
            {
                go = new GameObject($"StyleLabel_{seat}", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(sv.transform, false);

                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -2f);
                rt.sizeDelta = new Vector2(120f, 14f);

                var tmp = go.GetComponent<TextMeshProUGUI>();
                tmp.fontSize = 9;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontStyle = FontStyles.Bold;
                tmp.enableWordWrapping = false;

                _seatLabels[seat] = go;
            }

            go.SetActive(true);
            var text = go.GetComponent<TextMeshProUGUI>();

            Color color = style switch
            {
                "TAG" => TagColor,
                "LAG" => LagColor,
                "Fish" => FishColor,
                "Nit" or "Rock" => NitColor,
                "Maniac" => ManiacColor,
                "TILT" => TiltColor,
                "Reg" => RegColor,
                _ => RegColor,
            };

            string desc = profile.GetPlayStyleDescription() ?? style;
            string vpip = $"{profile.VPIP:P0}";
            text.text = $"{desc} ({vpip})";
            text.color = color;
        }

        private void HideLabel(int seat)
        {
            if (_seatLabels.TryGetValue(seat, out var go) && go != null)
                go.SetActive(false);
        }

        private void ClearLabels()
        {
            foreach (var kvp in _seatLabels)
                if (kvp.Value != null) kvp.Value.SetActive(false);
        }

        // ── Exploitation Advice ────────────────────────────────

        private static string GetExploitAdvice(PlayerProfile p)
        {
            if (p.HandsSeen < 5) return null;

            string style = p.GetPlayStyle();

            if (style == "TILT")
                return $"\u26a0 {p.Username} is on tilt! (Lost {p.ConsecutiveLosses} in a row) — Value bet wide, don't bluff them.";

            if (style == "Fish")
                return $"\U0001f3af {p.Username} plays like a calling station (VPIP {p.VPIP:P0}, AF {p.AF:F1}) — Bet for value, avoid bluffing.";

            if (style == "Nit" && p.FoldPct > 0.65f)
                return $"\U0001f3af {p.Username} is playing ultra-tight (folds {p.FoldPct:P0}) — Steal their blinds freely.";

            if (style == "Maniac")
                return $"\u26a0 {p.Username} is a maniac (VPIP {p.VPIP:P0}, AF {p.AF:F1}) — Let them bluff into your strong hands.";

            if (style == "LAG" && p.AF > 3f)
                return $"\U0001f3af {p.Username} is hyper-aggressive (AF {p.AF:F1}) — Trap them with strong hands.";

            // Behavioral shift detection
            if (p.HandsSeen >= 8)
            {
                float vpipDelta = p.RecentVPIP - p.VPIP;
                if (vpipDelta > 0.25f)
                    return $"\U0001f4c8 {p.Username} has loosened up significantly — they may be tilting or gambling.";
            }

            return null;
        }

        // ── Toast UI ───────────────────────────────────────────

        private void ShowToast(string message)
        {
            EnsureToast();
            if (_toastPanel == null) return;

            _toastText.text = message;
            _toastPanel.SetActive(true);

            DOTween.Kill(_toastCG);
            _toastCG.alpha = 0f;

            DOTween.Sequence()
                .Append(DOTween.To(() => _toastCG.alpha, x => _toastCG.alpha = x, 1f, 0.3f))
                .AppendInterval(6f)
                .Append(DOTween.To(() => _toastCG.alpha, x => _toastCG.alpha = x, 0f, 1f))
                .OnComplete(() => _toastPanel.SetActive(false))
                .SetTarget(_toastCG);
        }

        private void EnsureToast()
        {
            if (_toastPanel != null) return;
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            _toastPanel = new GameObject("PlayerReadToast", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _toastPanel.transform.SetParent(canvas.transform, false);
            _toastPanel.transform.SetAsLastSibling();

            var rt = _toastPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -50f);
            rt.sizeDelta = new Vector2(460f, 40f);

            _toastPanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.15f, 0.95f);
            _toastCG = _toastPanel.GetComponent<CanvasGroup>();
            _toastCG.alpha = 0f;

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(_toastPanel.transform, false);
            var tRt = textGO.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(12f, 4f);
            tRt.offsetMax = new Vector2(-12f, -4f);

            _toastText = textGO.GetComponent<TextMeshProUGUI>();
            _toastText.fontSize = 12;
            _toastText.color = new Color(0.92f, 0.9f, 0.8f);
            _toastText.alignment = TextAlignmentOptions.Center;
            _toastText.richText = true;
            _toastText.enableWordWrapping = true;

            _toastPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            DOTween.Kill(_toastCG);
            foreach (var kvp in _seatLabels)
                if (kvp.Value != null) Destroy(kvp.Value);
            if (_toastPanel != null) Destroy(_toastPanel);
        }
    }
}
