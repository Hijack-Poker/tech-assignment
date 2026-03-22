using System;
using System.Collections.Generic;
using System.Linq;
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
        private TableStateManager _stateManager;
        private int _lastProcessedGameNo = -1;

        // Per-player tracking
        private readonly Dictionary<int, PlayerTiltData> _tiltData = new();

        // UI: dot indicators per seat
        private readonly Dictionary<int, Image> _dots = new();

        // Toast UI
        private GameObject _toastPanel;
        private TextMeshProUGUI _toastText;
        private CanvasGroup _toastCG;

        private static readonly Color GreenTilt = new Color(0.2f, 0.8f, 0.2f);
        private static readonly Color YellowTilt = new Color(0.95f, 0.85f, 0.2f);
        private static readonly Color RedTilt = new Color(0.9f, 0.2f, 0.15f);

        private class PlayerTiltData
        {
            public string Username;
            public int ConsecutiveLosses;
            public float PeakStack;
            public float CurrentStack;
            public int AllInCount;
            public int FoldCount;
            public int HandsPlayed;
            public float TiltScore;
            public bool WasRedWarned;
        }

        private void Awake()
        {
            _stateManager = FindObjectOfType<TableStateManager>();
        }

        private void OnEnable()
        {
            if (_stateManager != null)
            {
                _stateManager.OnTableStateChanged += OnStateChanged;
                _stateManager.OnTableReset += OnTableReset;
            }
        }

        private void OnDisable()
        {
            if (_stateManager != null)
            {
                _stateManager.OnTableStateChanged -= OnStateChanged;
                _stateManager.OnTableReset -= OnTableReset;
            }
        }

        private void OnTableReset()
        {
            _tiltData.Clear();
            _lastProcessedGameNo = -1;
            foreach (var kvp in _dots)
            {
                if (kvp.Value != null)
                    kvp.Value.color = GreenTilt;
            }
        }

        private void OnStateChanged(TableResponse state)
        {
            if (state?.Game == null || state.Players == null) return;

            // Initialize/update peak stacks every state change
            foreach (var p in state.Players)
            {
                if (!_tiltData.ContainsKey(p.Seat))
                {
                    _tiltData[p.Seat] = new PlayerTiltData
                    {
                        Username = p.Username,
                        PeakStack = p.Stack,
                        CurrentStack = p.Stack,
                    };
                }
                else
                {
                    var data = _tiltData[p.Seat];
                    data.Username = p.Username;
                    data.CurrentStack = p.Stack;
                    if (p.Stack > data.PeakStack)
                        data.PeakStack = p.Stack;
                }
            }

            // Process completed hands (step 14-15), once per hand
            int step = state.Game.HandStep;
            int gameNo = state.Game.GameNo;
            if (step < 14 || gameNo == _lastProcessedGameNo) return;
            _lastProcessedGameNo = gameNo;

            foreach (var p in state.Players)
            {
                if (!_tiltData.TryGetValue(p.Seat, out var data)) continue;

                data.HandsPlayed++;

                // Track win/loss streaks
                if (p.IsWinner && p.Winnings > 0)
                    data.ConsecutiveLosses = 0;
                else if (!p.IsFolded)
                    data.ConsecutiveLosses++;

                // Track all-in and fold frequency
                if (p.IsAllIn) data.AllInCount++;
                if (p.IsFolded) data.FoldCount++;

                // Compute tilt score
                data.TiltScore = ComputeTiltScore(data);
            }

            // Update UI
            EnsureDots(state);
            UpdateDots();
            CheckRedWarnings();
        }

        private float ComputeTiltScore(PlayerTiltData data)
        {
            float score = 0f;

            // Consecutive losses (0.15 per loss, max 0.45)
            score += Mathf.Min(data.ConsecutiveLosses * 0.15f, 0.45f);

            // Stack decline from peak (0-0.3)
            if (data.PeakStack > 0f)
            {
                float decline = 1f - (data.CurrentStack / data.PeakStack);
                score += Mathf.Clamp(decline, 0f, 1f) * 0.3f;
            }

            // All-in frequency (0-0.15)
            if (data.HandsPlayed > 0)
            {
                float allInRate = (float)data.AllInCount / data.HandsPlayed;
                score += Mathf.Min(allInRate * 0.5f, 0.15f);
            }

            // Low fold rate suggests overplaying (0-0.1)
            if (data.HandsPlayed >= 3)
            {
                float foldRate = (float)data.FoldCount / data.HandsPlayed;
                if (foldRate < 0.15f)
                    score += 0.1f;
            }

            return Mathf.Clamp01(score);
        }

        private void EnsureDots(TableResponse state)
        {
            if (_dots.Count > 0) return;

            // Find all SeatView GameObjects in scene
            var seatViews = FindObjectsOfType<SeatView>();
            foreach (var sv in seatViews)
            {
                string goName = sv.gameObject.name;
                if (!goName.StartsWith("Seat") || !int.TryParse(goName.Substring(4), out int seatNum))
                    continue;

                if (_dots.ContainsKey(seatNum)) continue;

                // Create small colored dot at top-right of seat
                var dotGO = new GameObject($"TiltDot_{seatNum}", typeof(RectTransform), typeof(Image));
                dotGO.transform.SetParent(sv.transform, false);

                var rt = dotGO.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-4f, -4f);
                rt.sizeDelta = new Vector2(10f, 10f);

                var img = dotGO.GetComponent<Image>();
                img.color = GreenTilt;

                // Make it circular by adding a mask or just use default sprite
                // Unity's default white sprite renders as a square, which is fine for a small indicator

                _dots[seatNum] = img;
            }
        }

        private void UpdateDots()
        {
            foreach (var kvp in _tiltData)
            {
                if (!_dots.TryGetValue(kvp.Key, out var dot) || dot == null) continue;

                Color targetColor;
                float score = kvp.Value.TiltScore;
                if (score > 0.6f)
                    targetColor = RedTilt;
                else if (score > 0.3f)
                    targetColor = YellowTilt;
                else
                    targetColor = GreenTilt;

                DOTween.Kill(dot);
                DOTween.To(() => dot.color, c => dot.color = c, targetColor, 0.5f)
                    .SetEase(Ease.OutCubic)
                    .SetTarget(dot);
            }
        }

        private void CheckRedWarnings()
        {
            foreach (var kvp in _tiltData)
            {
                var data = kvp.Value;
                if (data.TiltScore > 0.6f && !data.WasRedWarned)
                {
                    data.WasRedWarned = true;
                    ShowToast($"⚠ {data.Username} may be on tilt!");
                }
            }
        }

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
                .AppendInterval(4f)
                .Append(DOTween.To(() => _toastCG.alpha, x => _toastCG.alpha = x, 0f, 0.8f))
                .OnComplete(() => _toastPanel.SetActive(false))
                .SetTarget(_toastCG);
        }

        private void EnsureToast()
        {
            if (_toastPanel != null) return;

            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            _toastPanel = new GameObject("TiltWarningToast", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _toastPanel.transform.SetParent(canvas.transform, false);

            var rt = _toastPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -80f);
            rt.sizeDelta = new Vector2(300f, 40f);

            var bg = _toastPanel.GetComponent<Image>();
            bg.color = new Color(0.35f, 0.08f, 0.08f, 0.92f);

            _toastCG = _toastPanel.GetComponent<CanvasGroup>();
            _toastCG.alpha = 0f;

            var textGO = new GameObject("ToastText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(_toastPanel.transform, false);

            var textRt = textGO.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8f, 4f);
            textRt.offsetMax = new Vector2(-8f, -4f);

            _toastText = textGO.GetComponent<TextMeshProUGUI>();
            _toastText.fontSize = 14;
            _toastText.color = new Color(1f, 0.85f, 0.85f);
            _toastText.alignment = TextAlignmentOptions.Center;
            _toastText.fontStyle = FontStyles.Bold;

            _toastPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            DOTween.Kill(_toastCG);
            foreach (var kvp in _dots)
            {
                if (kvp.Value != null)
                    DOTween.Kill(kvp.Value);
            }
            if (_toastPanel != null) Destroy(_toastPanel);
        }
    }
}
