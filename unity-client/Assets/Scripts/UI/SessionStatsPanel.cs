using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.Analytics;
using HijackPoker.Animation;
using HijackPoker.Managers;

namespace HijackPoker.UI
{
    public class SessionStatsPanel : MonoBehaviour
    {
        public event System.Action<bool> OnPanelToggled;

        private RectTransform _panelRt;
        private GameObject _panelBody;
        private Image _toggleBg;
        private Image _iconImage;
        private bool _isExpanded;

        private SessionTracker _sessionTracker;
        private PlayerProfiler _playerProfiler;
        private AnimationController _animController;

        private TextMeshProUGUI _handsPlayedText;
        private TextMeshProUGUI _winRateText;
        private TextMeshProUGUI _vpipText;
        private TextMeshProUGUI _pfrText;
        private TextMeshProUGUI _biggestWinText;
        private TextMeshProUGUI _sessionPLText;

        public static SessionStatsPanel Create(Transform parent, AnimationController anim,
            SessionTracker tracker, PlayerProfiler profiler)
        {
            bool portrait = LayoutConfig.IsPortrait;

            // Circular toggle button
            int btnSize = portrait ? 36 : 40;
            var toggleGo = new GameObject("SessionStatsToggle", typeof(RectTransform));
            toggleGo.transform.SetParent(parent, false);
            var toggleRt = toggleGo.GetComponent<RectTransform>();

            if (portrait)
            {
                toggleRt.anchorMin = new Vector2(0f, 0f);
                toggleRt.anchorMax = new Vector2(0f, 0f);
                toggleRt.pivot = new Vector2(0f, 0f);
                toggleRt.sizeDelta = new Vector2(btnSize, btnSize);
                toggleRt.anchoredPosition = new Vector2(8, LayoutConfig.ControlsBarHeight + 8);
            }
            else
            {
                toggleRt.anchorMin = new Vector2(0f, 0.5f);
                toggleRt.anchorMax = new Vector2(0f, 0.5f);
                toggleRt.pivot = new Vector2(0f, 0.5f);
                toggleRt.sizeDelta = new Vector2(btnSize, btnSize);
                toggleRt.anchoredPosition = new Vector2(4, 0);
            }

            // Shadow
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

            var view = toggleGo.AddComponent<SessionStatsPanel>();
            view._toggleBg = toggleBg;
            view._animController = anim;
            view._sessionTracker = tracker;
            view._playerProfiler = profiler;

            var toggleBtn = toggleGo.AddComponent<Button>();
            toggleBtn.targetGraphic = toggleBg;
            var btnColors = toggleBtn.colors;
            btnColors.normalColor = Color.white;
            btnColors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            btnColors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            toggleBtn.colors = btnColors;
            toggleBtn.onClick.AddListener(view.TogglePanel);

            // Icon (reuse list icon style)
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
            view._iconImage.color = UIFactory.AccentGold;
            view._iconImage.raycastTarget = false;

            // Panel body
            var panelGo = new GameObject("SessionStatsBody", typeof(RectTransform));
            panelGo.transform.SetParent(parent, false);
            var panelRt = panelGo.GetComponent<RectTransform>();
            view._panelRt = panelRt;

            if (portrait)
            {
                float barNorm = LayoutConfig.ControlsBarHeight / LayoutConfig.ReferenceResolution.y;
                panelRt.anchorMin = new Vector2(0.02f, barNorm + 0.02f);
                panelRt.anchorMax = new Vector2(0.98f, 0.45f);
            }
            else
            {
                panelRt.anchorMin = new Vector2(0.005f, 0.08f);
                panelRt.anchorMax = new Vector2(0.18f, 0.92f);
            }
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            var bg = panelGo.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.10f, 0.13f, 0.94f);
            bg.sprite = TextureGenerator.GetRoundedRect(128, 128, 12);
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = true;

            view._panelBody = panelGo;
            view.BuildPanelUI(panelGo.transform);

            panelGo.SetActive(false);
            toggleGo.transform.SetAsLastSibling();

            return view;
        }

        private void BuildPanelUI(Transform parent)
        {
            // Vertical layout for stats rows
            var vlg = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 2;
            vlg.padding = new RectOffset(12, 12, 10, 10);

            // Title
            var title = UIFactory.CreateText("Title", parent, "TABLE STATS",
                14f, UIFactory.AccentGold, TextAlignmentOptions.Center, FontStyles.Bold);
            var titleLE = title.gameObject.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 24;

            // Divider
            var divGo = new GameObject("Divider", typeof(RectTransform));
            divGo.transform.SetParent(parent, false);
            var divImg = divGo.AddComponent<Image>();
            divImg.color = new Color(1f, 1f, 1f, 0.08f);
            var divLE = divGo.AddComponent<LayoutElement>();
            divLE.preferredHeight = 1;

            // Stats rows
            _handsPlayedText = CreateStatRow(parent, "Hands Dealt");
            _winRateText = CreateStatRow(parent, "Avg Win Rate");
            _vpipText = CreateStatRow(parent, "Avg VPIP");
            _pfrText = CreateStatRow(parent, "Avg PFR");
            _biggestWinText = CreateStatRow(parent, "Biggest Pot");
            _sessionPLText = CreateStatRow(parent, "Top Mover");
        }

        private TextMeshProUGUI CreateStatRow(Transform parent, string label)
        {
            var rowGo = new GameObject($"Row_{label}", typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            var rowLE = rowGo.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 22;

            var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.spacing = 4;

            var labelText = UIFactory.CreateText("Label", rowGo.transform, label,
                11f, UIFactory.TextSecondary, TextAlignmentOptions.Left);
            var labelLE = labelText.gameObject.AddComponent<LayoutElement>();
            labelLE.flexibleWidth = 1;

            var valueText = UIFactory.CreateText("Value", rowGo.transform, "--",
                11f, Color.white, TextAlignmentOptions.Right, FontStyles.Bold);
            var valueLE = valueText.gameObject.AddComponent<LayoutElement>();
            valueLE.flexibleWidth = 1;

            return valueText;
        }

        public void TogglePanel()
        {
            _isExpanded = !_isExpanded;
            _panelBody.SetActive(_isExpanded);

            if (_isExpanded)
            {
                _iconImage.color = Color.white;
                _toggleBg.color = UIFactory.AccentGold;
                UpdateStats();
            }
            else
            {
                _iconImage.color = UIFactory.AccentGold;
                _toggleBg.color = new Color(0.13f, 0.13f, 0.16f, 0.92f);
            }
            OnPanelToggled?.Invoke(_isExpanded);
        }

        public void CollapsePanel()
        {
            if (!_isExpanded) return;
            _isExpanded = false;
            _panelBody.SetActive(false);
            _iconImage.color = UIFactory.AccentGold;
            _toggleBg.color = new Color(0.13f, 0.13f, 0.16f, 0.92f);
        }

        public void UpdateTrackers(SessionTracker tracker, PlayerProfiler profiler)
        {
            _sessionTracker = tracker;
            _playerProfiler = profiler;
        }

        public void UpdateStats()
        {
            if (!_isExpanded) return;

            // Aggregate stats across all seats at the table
            int totalHands = 0;
            int totalWon = 0;
            float biggestWin = 0f;
            int vpipSeats = 0;
            float vpipSum = 0f;
            float pfrSum = 0f;
            int activeSeats = 0;

            for (int seat = 1; seat <= LayoutConfig.MaxSeats; seat++)
            {
                var session = _sessionTracker?.GetSession(seat);
                if (session == null || session.HandsPlayed == 0) continue;

                activeSeats++;
                totalHands = Mathf.Max(totalHands, session.HandsPlayed);
                totalWon += session.HandsWon;
                if (session.BiggestPot > biggestWin)
                    biggestWin = session.BiggestPot;

                var profile = _playerProfiler?.GetProfile(seat);
                if (profile != null && profile.HandsTracked >= 3)
                {
                    vpipSeats++;
                    vpipSum += _playerProfiler.GetVPIP(seat);
                    pfrSum += _playerProfiler.GetPFR(seat);
                }
            }

            _handsPlayedText.text = totalHands.ToString();
            _winRateText.text = totalHands > 0 && activeSeats > 0
                ? $"{(float)totalWon / totalHands * 100f / activeSeats:F0}%"
                : "0%";
            _vpipText.text = vpipSeats > 0 ? $"{vpipSum / vpipSeats:F0}%" : "--";
            _pfrText.text = vpipSeats > 0 ? $"{pfrSum / vpipSeats:F0}%" : "--";
            _biggestWinText.text = biggestWin > 0 ? $"${biggestWin:F0}" : "--";

            // Show biggest mover's P/L
            float maxAbsDelta = 0f;
            float bestDelta = 0f;
            for (int seat = 1; seat <= LayoutConfig.MaxSeats; seat++)
            {
                float d = _sessionTracker?.GetDelta(seat) ?? 0f;
                if (Mathf.Abs(d) > maxAbsDelta)
                {
                    maxAbsDelta = Mathf.Abs(d);
                    bestDelta = d;
                }
            }

            if (bestDelta > 0)
            {
                _sessionPLText.text = $"+${bestDelta:F0}";
                _sessionPLText.color = UIFactory.AccentGreen;
            }
            else if (bestDelta < 0)
            {
                _sessionPLText.text = $"-${-bestDelta:F0}";
                _sessionPLText.color = UIFactory.AccentMagenta;
            }
            else
            {
                _sessionPLText.text = "$0";
                _sessionPLText.color = UIFactory.TextSecondary;
            }
        }
    }
}
