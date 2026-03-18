using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using DG.Tweening;
using HijackPoker.Managers;
using HijackPoker.Models;

namespace HijackPoker.UI
{
    public class ShowdownView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TableStateManager _stateManager;
        [SerializeField] private SeatView[] _seatViews;

        [Header("Showdown Panel")]
        [SerializeField] private GameObject _showdownPanel;
        [SerializeField] private TextMeshProUGUI _winnerText;
        [SerializeField] private RectTransform _contentRoot;

        private List<GameObject> _displayedCards = new List<GameObject>();
        private bool _isShowingShowdown;
        private int _lastShownPlayerCount;
        private Button _nextHandButton;
        private GameManager _gameManager;

        private static readonly Color WinnerGlow = new Color(1f, 0.84f, 0.2f, 1f);

        // Layout
        private const float CardW = 44f;
        private const float CardH = 62f;
        private const float CardGap = 4f;
        private const float LabelH = 22f;
        private const float RowGap = 6f;
        private const float Pad = 12f;
        private const float TitleH = 58f;

        private float RowH => CardH + LabelH + 2f;

        private void Start()
        {
            if (_stateManager == null)
                _stateManager = FindObjectOfType<TableStateManager>();
            if (_gameManager == null)
                _gameManager = FindObjectOfType<GameManager>();

            if (_seatViews == null || _seatViews.Length == 0)
            {
                var seats = new List<SeatView>();
                for (int i = 1; i <= 6; i++)
                {
                    var seatGO = GameObject.Find($"Seat{i}");
                    if (seatGO != null)
                    {
                        var sv = seatGO.GetComponent<SeatView>();
                        if (sv != null) seats.Add(sv);
                    }
                }
                _seatViews = seats.ToArray();
            }

            if (_stateManager != null)
            {
                _stateManager.OnTableStateChanged += OnStateChanged;
                var cur = _stateManager.CurrentState;
                if (cur?.Game != null && cur.Game.HandStep >= 13 && cur.Game.HandStep <= 14)
                    StartCoroutine(DelayedCheck(cur));
            }

            EnsurePanel();
            HideShowdown();
        }

        private void OnDestroy()
        {
            if (_stateManager != null)
                _stateManager.OnTableStateChanged -= OnStateChanged;
        }

        private System.Collections.IEnumerator DelayedCheck(TableResponse state)
        {
            yield return null;
            EnsurePanel();
            OnStateChanged(state);
        }

        private void OnStateChanged(TableResponse state)
        {
            if (state?.Game == null) return;

            int step = state.Game.HandStep;
            bool isSD = step >= 13 && step <= 14;

            int withHands = 0;
            if (isSD && state.Players != null)
                withHands = state.Players.Count(p => !p.IsFolded && p.HasBestHand);

            if (isSD && withHands > 0)
            {
                if (!_isShowingShowdown || withHands != _lastShownPlayerCount)
                    ShowShowdown(state);
            }
            else if (!isSD && _isShowingShowdown)
            {
                HideShowdown();
            }
        }

        private void ShowShowdown(TableResponse state)
        {
            _isShowingShowdown = true;

            var players = state.Players?
                .Where(p => !p.IsFolded && p.HasBestHand)
                .OrderByDescending(p => p.IsWinner)
                .ThenByDescending(p => p.Winnings)
                .ToList();

            if (players == null || players.Count == 0)
            {
                HideShowdown();
                return;
            }

            _lastShownPlayerCount = players.Count;

            var winner = players.FirstOrDefault(p => p.IsWinner);
            if (_winnerText != null)
            {
                if (winner != null)
                {
                    string rank = !string.IsNullOrEmpty(winner.HandRank) ? winner.HandRank : "Best Hand";
                    string winText = winner.Winnings > 0
                        ? $"\n<size=18><color=#4AE86C>+${winner.Winnings:F2}</color></size>"
                        : "";
                    _winnerText.text = $"{winner.Username} wins with {rank}!{winText}";
                }
                else
                {
                    _winnerText.text = "Showdown";
                }
            }

            BuildPlayerRows(players);

            // Size the panel to fit all rows + next hand button
            float buttonH = 56f;
            float contentH = TitleH + players.Count * (RowH + RowGap) + buttonH + Pad;
            var panelRT = _showdownPanel.GetComponent<RectTransform>();
            panelRT.sizeDelta = new Vector2(460f, contentH);

            _showdownPanel.SetActive(true);
            _showdownPanel.transform.SetAsLastSibling();

            var cg = _showdownPanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                DOTween.To(() => cg.alpha, x => cg.alpha = x, 1f, 0.4f).SetEase(Ease.OutQuad);
            }
            _showdownPanel.transform.localScale = Vector3.one * 0.88f;
            _showdownPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }

        private void HideShowdown()
        {
            _isShowingShowdown = false;
            _lastShownPlayerCount = 0;

            if (_showdownPanel != null)
            {
                var cg = _showdownPanel.GetComponent<CanvasGroup>();
                if (cg != null)
                    DOTween.To(() => cg.alpha, x => cg.alpha = x, 0f, 0.3f)
                        .OnComplete(() => _showdownPanel.SetActive(false));
                else
                    _showdownPanel.SetActive(false);
            }

            ClearCards();

            if (_seatViews != null)
                foreach (var s in _seatViews)
                    if (s != null)
                    {
                        var cg = s.GetComponent<CanvasGroup>();
                        if (cg != null) cg.alpha = 1f;
                    }
        }

        private void BuildPlayerRows(List<PlayerState> players)
        {
            ClearCards();
            if (_contentRoot == null) return;

            for (int pi = 0; pi < players.Count; pi++)
            {
                var p = players[pi];
                if (p.BestHand == null || p.BestHand.Count == 0) continue;

                float y = -pi * (RowH + RowGap);

                // Row
                var row = new GameObject($"Row_{p.Username}", typeof(RectTransform));
                row.transform.SetParent(_contentRoot, false);
                var rrt = row.GetComponent<RectTransform>();
                rrt.anchorMin = new Vector2(0.5f, 1f);
                rrt.anchorMax = new Vector2(0.5f, 1f);
                rrt.pivot = new Vector2(0.5f, 1f);
                rrt.anchoredPosition = new Vector2(0, y);
                rrt.sizeDelta = new Vector2(430f, RowH);
                _displayedCards.Add(row);

                // Label
                var lbl = new GameObject("Lbl", typeof(RectTransform), typeof(TextMeshProUGUI));
                lbl.transform.SetParent(row.transform, false);
                var lrt = lbl.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0, 1);
                lrt.anchorMax = new Vector2(1, 1);
                lrt.pivot = new Vector2(0.5f, 1f);
                lrt.anchoredPosition = Vector2.zero;
                lrt.sizeDelta = new Vector2(0, LabelH);
                var lt = lbl.GetComponent<TextMeshProUGUI>();
                string hr = !string.IsNullOrEmpty(p.HandRank) ? p.HandRank : "";
                lt.text = p.IsWinner
                    ? $"<b><color=#FFD700>{p.Username}</color></b> - {hr} <color=#4AE86C>WINNER</color>"
                    : $"{p.Username} - {hr}";
                lt.fontSize = 15;
                lt.alignment = TextAlignmentOptions.Center;
                lt.color = p.IsWinner ? WinnerGlow : new Color(0.8f, 0.8f, 0.8f);

                // Cards
                int cc = p.BestHand.Count;
                float tw = cc * CardW + (cc - 1) * CardGap;
                float sx = -tw / 2f + CardW / 2f;

                for (int ci = 0; ci < cc; ci++)
                {
                    var cgo = MakeCard(p.BestHand[ci]);
                    cgo.transform.SetParent(row.transform, false);
                    var crt = cgo.GetComponent<RectTransform>();
                    crt.anchorMin = new Vector2(0.5f, 0);
                    crt.anchorMax = new Vector2(0.5f, 0);
                    crt.pivot = new Vector2(0.5f, 0);
                    crt.anchoredPosition = new Vector2(sx + ci * (CardW + CardGap), 0);
                    crt.sizeDelta = new Vector2(CardW, CardH);

                    cgo.transform.localScale = Vector3.zero;
                    cgo.transform.DOScale(1f, 0.2f)
                        .SetEase(Ease.OutBack)
                        .SetDelay(pi * 0.08f + ci * 0.025f);
                }
            }
        }

        private GameObject MakeCard(string card)
        {
            var go = new GameObject($"C_{card}", typeof(RectTransform), typeof(Image));
            var img = go.GetComponent<Image>();
            img.color = new Color(0.97f, 0.97f, 0.93f);

            var tgo = new GameObject("T", typeof(RectTransform), typeof(TextMeshProUGUI));
            tgo.transform.SetParent(go.transform, false);
            var trt = tgo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            var tmp = tgo.GetComponent<TextMeshProUGUI>();
            tmp.text = FmtCard(card);
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = IsRed(card) ? new Color(0.85f, 0.1f, 0.1f) : new Color(0.1f, 0.1f, 0.1f);

            return go;
        }

        private string FmtCard(string c)
        {
            if (string.IsNullOrEmpty(c)) return "";
            string s = c[^1..], r = c[..^1];
            string sym = s switch { "H" => "\u2665", "D" => "\u2666", "C" => "\u2663", "S" => "\u2660", _ => s };
            return $"{r}\n{sym}";
        }

        private bool IsRed(string c)
        {
            if (string.IsNullOrEmpty(c)) return false;
            string s = c[^1..];
            return s == "H" || s == "D";
        }

        private void ClearCards()
        {
            foreach (var c in _displayedCards)
                if (c != null) Destroy(c);
            _displayedCards.Clear();
        }

        private void UpdateSeatOpacity(TableResponse state, bool isSD)
        {
            if (_seatViews == null || state?.Players == null) return;
            foreach (var seat in _seatViews)
            {
                if (seat == null) continue;
                var cg = seat.GetComponent<CanvasGroup>();
                if (cg == null) continue;
                if (!isSD) { cg.alpha = 1f; continue; }

                int sn = GetSeatNum(seat);
                var pl = state.Players.Find(p => p.Seat == sn);
                if (pl == null) cg.alpha = 0.5f;
                else if (pl.IsWinner) DOTween.To(() => cg.alpha, x => cg.alpha = x, 1f, 0.3f);
                else DOTween.To(() => cg.alpha, x => cg.alpha = x, 0.4f, 0.3f);
            }
        }

        private int GetSeatNum(SeatView s)
        {
            string n = s.gameObject.name;
            return n.StartsWith("Seat") && int.TryParse(n.Substring(4), out int v) ? v : -1;
        }

        private void EnsurePanel()
        {
            if (_showdownPanel != null) return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            // Panel (dark bg, clips children)
            _showdownPanel = new GameObject("ShowdownPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _showdownPanel.transform.SetParent(canvas.transform, false);

            var prt = _showdownPanel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = Vector2.zero;
            prt.sizeDelta = new Vector2(460f, 400f);

            var bg = _showdownPanel.GetComponent<Image>();
            bg.color = new Color(0.04f, 0.08f, 0.14f, 0.97f);

            // Title
            var tgo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            tgo.transform.SetParent(_showdownPanel.transform, false);
            var trt = tgo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 1);
            trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0, -4f);
            trt.sizeDelta = new Vector2(-20f, TitleH - 4f);

            _winnerText = tgo.GetComponent<TextMeshProUGUI>();
            _winnerText.fontSize = 20;
            _winnerText.fontStyle = FontStyles.Bold;
            _winnerText.alignment = TextAlignmentOptions.Center;
            _winnerText.color = WinnerGlow;

            // Content root (below title, above button)
            var cgo = new GameObject("Content", typeof(RectTransform));
            cgo.transform.SetParent(_showdownPanel.transform, false);
            _contentRoot = cgo.GetComponent<RectTransform>();
            _contentRoot.anchorMin = new Vector2(0, 0);
            _contentRoot.anchorMax = new Vector2(1, 1);
            _contentRoot.offsetMin = new Vector2(Pad, Pad + 52f);
            _contentRoot.offsetMax = new Vector2(-Pad, -TitleH);

            // NEXT HAND button at bottom
            var btnGO = new GameObject("NextHandButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(_showdownPanel.transform, false);
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.5f, 0);
            btnRT.anchorMax = new Vector2(0.5f, 0);
            btnRT.pivot = new Vector2(0.5f, 0);
            btnRT.anchoredPosition = new Vector2(0, Pad);
            btnRT.sizeDelta = new Vector2(200f, 44f);

            var btnImg = btnGO.GetComponent<Image>();
            btnImg.color = new Color(0.15f, 0.56f, 0.91f, 0.96f);

            var btnLabelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnLabelGO.transform.SetParent(btnGO.transform, false);
            var btnLabelRT = btnLabelGO.GetComponent<RectTransform>();
            btnLabelRT.anchorMin = Vector2.zero;
            btnLabelRT.anchorMax = Vector2.one;
            btnLabelRT.offsetMin = Vector2.zero;
            btnLabelRT.offsetMax = Vector2.zero;

            var btnLabel = btnLabelGO.GetComponent<TextMeshProUGUI>();
            btnLabel.text = "NEXT HAND";
            btnLabel.alignment = TextAlignmentOptions.Center;
            btnLabel.fontSize = 20;
            btnLabel.fontStyle = FontStyles.Bold;
            btnLabel.color = Color.white;
            btnLabel.raycastTarget = false;

            _nextHandButton = btnGO.GetComponent<Button>();
            _nextHandButton.onClick.AddListener(OnNextHandClicked);

            _showdownPanel.SetActive(false);
        }

        private void OnNextHandClicked()
        {
            HideShowdown();
            if (_gameManager != null)
                _ = _gameManager.AdvanceStepAsync();
        }
    }
}
