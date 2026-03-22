using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.Managers;
namespace HijackPoker.UI
{
    /// <summary>
    /// Redesigned toolbar: bottom-center playback dock + top-left lobby pill + phase indicator.
    /// Single layout for both orientations — no portrait/landscape branching.
    /// </summary>
    public class ControlsView : MonoBehaviour
    {
        public event Action OnNextStep;
        public event Action OnReset;
        public event Action OnAutoPlayToggle;
        public event Action<int> OnTableConnect;
        public event Action OnBackToLobby;
        public event Action OnHelpRequested;
        private Button _nextStepButton;
        private Button _resetButton;
        private Button _playPauseButton;
        private Image _playPauseIcon;
        private Image _playPauseBg;
        private TextMeshProUGUI _playPauseLabel;
        private Button _muteButton;
        private Button _speedButton;
        private TextMeshProUGUI _speedLabel;
        private Image _muteIconImage;
        private Image _nextStepIcon;
        private Image[] _tableBadges;
        private TextMeshProUGUI[] _tableBadgeLabels;
        private RectTransform _rt;
        private int _activeTableId = 1;

        // Lobby pill (parented to safe area, not dock)
        private GameObject _lobbyPillGo;

        // Phase indicator (parented to controls bar, above dock)
        private GameObject _phaseIndicatorGo;
        private TextMeshProUGUI[] _phaseSegmentLabels;
        private Image[] _phaseSegmentBgs;
        private int _currentPhaseIndex;

        private static readonly string[] PhaseNames = { "Pre", "Flop", "Turn", "River", "Show" };
        private static readonly float[] SpeedOptions = { 0.25f, 0.5f, 1.0f, 2.0f };
        private int _speedIndex = 2; // default 1x

        public float CurrentSpeed => SpeedOptions[_speedIndex];

        public static ControlsView Create(Transform parent)
        {
            float barHeight = LayoutConfig.ControlsBarHeight;

            // Outer bar: full-width but transparent (maintains VLG row sizing)
            var barBg = UIFactory.CreatePanel("ControlsBar", parent, UIFactory.Transparent);
            var barLE = barBg.gameObject.AddComponent<LayoutElement>();
            barLE.preferredHeight = barHeight;
            barLE.flexibleWidth = 1;

            // Controls container
            var go = new GameObject("Controls", typeof(RectTransform));
            go.transform.SetParent(barBg, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);

            var view = go.AddComponent<ControlsView>();
            view._rt = rt;
            view.BuildUI();

            // Build lobby pill anchored to safe area (parent.parent = safeArea)
            Transform safeArea = parent.parent;
            if (safeArea != null)
                view.BuildLobbyPill(safeArea);

            // Build phase indicator above dock (inside controls bar)
            view.BuildPhaseIndicator(barBg);

            return view;
        }

        private void BuildUI()
        {
            float maxW = LayoutConfig.ToolbarMaxWidth;
            float toolbarH = LayoutConfig.ToolbarHeight;
            int cornerR = LayoutConfig.ToolbarCornerRadius;

            _rt.sizeDelta = new Vector2(maxW, toolbarH);

            // Drop shadow (behind toolbar bg)
            var shadowImg = UIFactory.CreateImage("Shadow", transform);
            shadowImg.sprite = TextureGenerator.GetSoftShadow(
                (int)maxW, (int)toolbarH, cornerR, 8, 0.12f);
            shadowImg.type = Image.Type.Sliced;
            shadowImg.color = Color.white;
            shadowImg.raycastTarget = false;
            var shadowRt = shadowImg.GetComponent<RectTransform>();
            shadowRt.anchorMin = Vector2.zero;
            shadowRt.anchorMax = Vector2.one;
            shadowRt.offsetMin = new Vector2(-8, -10);
            shadowRt.offsetMax = new Vector2(8, 6);

            // Toolbar background (rounded rect)
            var bgImg = UIFactory.CreateImage("ToolbarBg", transform);
            bgImg.sprite = TextureGenerator.GetRoundedRect((int)maxW, (int)toolbarH, cornerR);
            bgImg.type = Image.Type.Sliced;
            bgImg.color = UIFactory.ToolbarBg;
            bgImg.raycastTarget = true;
            var bgRt = bgImg.GetComponent<RectTransform>();
            UIFactory.StretchFill(bgRt);

            // Content container inside bg
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            UIFactory.StretchFill(contentRt);

            // Single horizontal layout — no portrait/landscape branching
            BuildDockLayout(content.transform);
        }

        private void BuildDockLayout(Transform content)
        {
            int iconSz = LayoutConfig.ToolbarIconSize;
            float pad = LayoutConfig.ToolbarInnerPadding;
            float divH = LayoutConfig.ToolbarHeight - 12f;

            var hlg = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.padding = new RectOffset((int)pad, (int)pad, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // ── Reset (ghost icon button) ──
            BuildResetButton(content, iconSz);

            // ── Next Step (secondary ghost button) ──
            BuildNextStepButton(content, iconSz);

            // ── Play/Pause (primary filled button) ──
            BuildPlayPauseButton(content, iconSz);

            // ── Speed pill (standalone) ──
            BuildSpeedPill(content, iconSz);

            UIFactory.CreateVerticalDivider("Div1", content, divH);

            // ── Table badges ──
            BuildTableBadges(content);

            // ── Mute (ghost icon button) ──
            BuildMuteButton(content, iconSz);
        }

        private void BuildResetButton(Transform parent, int iconSz)
        {
            _resetButton = UIFactory.CreateIconButton("Reset", parent,
                TextureGenerator.GetResetIcon(iconSz * 2), UIFactory.TextSecondary, iconSz);
            var resetLe = _resetButton.gameObject.AddComponent<LayoutElement>();
            resetLe.preferredWidth = LayoutConfig.ToolbarSecondaryBtnH;
            resetLe.preferredHeight = LayoutConfig.ToolbarSecondaryBtnH;
            _resetButton.onClick.AddListener(() => OnReset?.Invoke());
        }

        private void BuildNextStepButton(Transform parent, int iconSz)
        {
            _nextStepButton = UIFactory.CreateIconButton("NextStep", parent,
                TextureGenerator.GetTriangle(iconSz * 2), UIFactory.TextSecondary, iconSz,
                "Step", 11f, labelColor: UIFactory.TextSecondary);
            _nextStepIcon = _nextStepButton.transform.GetChild(0).GetComponent<Image>();
            var nsLe = _nextStepButton.gameObject.AddComponent<LayoutElement>();
            nsLe.preferredWidth = 60;
            nsLe.preferredHeight = LayoutConfig.ToolbarSecondaryBtnH;
            _nextStepButton.onClick.AddListener(() => OnNextStep?.Invoke());
        }

        private void BuildPlayPauseButton(Transform parent, int iconSz)
        {
            _playPauseButton = UIFactory.CreateIconButton("PlayPause", parent,
                TextureGenerator.GetTriangle(iconSz * 2), Color.white, iconSz,
                "Play", 13f, UIFactory.AccentCyan, Color.white,
                new Vector2(LayoutConfig.ToolbarPrimaryBtnW, LayoutConfig.ToolbarPrimaryBtnH));
            _playPauseBg = _playPauseButton.GetComponent<Image>();
            _playPauseBg.sprite = TextureGenerator.GetRoundedRect(64, 34, 10);
            _playPauseBg.type = Image.Type.Sliced;
            _playPauseIcon = _playPauseButton.transform.GetChild(0).GetComponent<Image>();
            _playPauseLabel = _playPauseButton.GetComponentInChildren<TextMeshProUGUI>();
            var ppLe = _playPauseButton.gameObject.AddComponent<LayoutElement>();
            ppLe.preferredWidth = LayoutConfig.ToolbarPrimaryBtnW;
            ppLe.preferredHeight = LayoutConfig.ToolbarPrimaryBtnH;
            _playPauseButton.onClick.AddListener(() =>
            {
                AudioManager.Instance?.Play(SoundType.ButtonClick);
                OnAutoPlayToggle?.Invoke();
            });
        }

        private void BuildSpeedPill(Transform parent, int iconSz)
        {
            var speedGo = new GameObject("SpeedPill", typeof(RectTransform));
            speedGo.transform.SetParent(parent, false);

            var speedBgImg = speedGo.AddComponent<Image>();
            speedBgImg.sprite = TextureGenerator.GetRoundedRect(64, 34, 14);
            speedBgImg.type = Image.Type.Sliced;
            speedBgImg.color = UIFactory.Transparent;
            speedBgImg.raycastTarget = true;

            _speedButton = speedGo.AddComponent<Button>();
            _speedButton.targetGraphic = speedBgImg;
            var speedColors = _speedButton.colors;
            speedColors.normalColor = Color.white;
            speedColors.highlightedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
            speedColors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            speedColors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            _speedButton.colors = speedColors;
            _speedButton.onClick.AddListener(() =>
            {
                AudioManager.Instance?.Play(SoundType.ButtonClick);
                CycleSpeed();
            });

            // Outline border
            var outlineGo = new GameObject("Outline", typeof(RectTransform));
            outlineGo.transform.SetParent(speedGo.transform, false);
            var outlineImg = outlineGo.AddComponent<Image>();
            outlineImg.sprite = TextureGenerator.GetRoundedRect(64, 34, 14);
            outlineImg.type = Image.Type.Sliced;
            outlineImg.color = UIFactory.ToolbarDivider;
            outlineImg.raycastTarget = false;
            UIFactory.StretchFill(outlineGo.GetComponent<RectTransform>());
            outlineGo.AddComponent<LayoutElement>().ignoreLayout = true;

            _speedLabel = UIFactory.CreateText("SpeedLabel", speedGo.transform,
                "1x", 11f, UIFactory.TextPrimary);
            _speedLabel.raycastTarget = false;
            var speedLabelRt = _speedLabel.GetComponent<RectTransform>();
            speedLabelRt.anchorMin = Vector2.zero;
            speedLabelRt.anchorMax = Vector2.one;
            speedLabelRt.sizeDelta = Vector2.zero;

            var speedLe = speedGo.AddComponent<LayoutElement>();
            speedLe.preferredWidth = LayoutConfig.SpeedPillWidth;
            speedLe.preferredHeight = LayoutConfig.ToolbarSecondaryBtnH;
        }

        private void BuildMuteButton(Transform parent, int iconSz)
        {
            _muteButton = UIFactory.CreateIconButton("Mute", parent,
                TextureGenerator.GetSpeakerIcon(iconSz * 2), UIFactory.TextSecondary, iconSz);
            _muteIconImage = _muteButton.transform.GetChild(0).GetComponent<Image>();
            var muteLe = _muteButton.gameObject.AddComponent<LayoutElement>();
            muteLe.preferredWidth = LayoutConfig.ToolbarSecondaryBtnH;
            muteLe.preferredHeight = LayoutConfig.ToolbarSecondaryBtnH;
            _muteButton.onClick.AddListener(HandleMuteToggle);
        }

        private void BuildLobbyPill(Transform safeArea)
        {
            float pillH = LayoutConfig.LobbyPillHeight;
            int cornerR = LayoutConfig.LobbyPillCornerRadius;
            int iconSz = LayoutConfig.ToolbarIconSize;

            _lobbyPillGo = new GameObject("LobbyPill", typeof(RectTransform));
            _lobbyPillGo.transform.SetParent(safeArea, false);

            var rt = _lobbyPillGo.GetComponent<RectTransform>();
            // Anchor top-left
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(12f, -42f);
            rt.sizeDelta = new Vector2(80f, pillH);

            // Background
            var bgImg = _lobbyPillGo.AddComponent<Image>();
            bgImg.sprite = TextureGenerator.GetRoundedRect(80, (int)pillH, cornerR);
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0f, 0f, 0f, 0.35f);
            bgImg.raycastTarget = true;

            // HLG for icon + label
            var hlg = _lobbyPillGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.padding = new RectOffset(8, 10, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Back arrow icon
            var arrowImg = UIFactory.CreateImage("Arrow", _lobbyPillGo.transform,
                Color.white, new Vector2(iconSz, iconSz));
            arrowImg.sprite = TextureGenerator.GetBackArrow(iconSz * 2);
            arrowImg.raycastTarget = false;

            // "Lobby" label
            var label = UIFactory.CreateText("Label", _lobbyPillGo.transform,
                "Lobby", 12f, Color.white);
            label.raycastTarget = false;
            var labelRt = label.GetComponent<RectTransform>();
            labelRt.sizeDelta = new Vector2(label.preferredWidth + 4, pillH);

            // Button
            var btn = _lobbyPillGo.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(() => OnBackToLobby?.Invoke());

            // Help button "?" — positioned right of lobby pill
            var helpGo = new GameObject("HelpBtn", typeof(RectTransform));
            helpGo.transform.SetParent(safeArea, false);
            var helpRt = helpGo.GetComponent<RectTransform>();
            helpRt.anchorMin = new Vector2(0f, 1f);
            helpRt.anchorMax = new Vector2(0f, 1f);
            helpRt.pivot = new Vector2(0f, 1f);
            helpRt.anchoredPosition = new Vector2(100f, -42f);
            helpRt.sizeDelta = new Vector2(pillH, pillH);

            var helpBg = helpGo.AddComponent<Image>();
            helpBg.sprite = TextureGenerator.GetCircle(64);
            helpBg.color = new Color(0f, 0f, 0f, 0.35f);
            helpBg.raycastTarget = true;

            var helpLabel = UIFactory.CreateText("HelpLabel", helpGo.transform,
                "?", 14f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.StretchFill(helpLabel.GetComponent<RectTransform>());
            helpLabel.raycastTarget = false;

            var helpBtn = helpGo.AddComponent<Button>();
            helpBtn.targetGraphic = helpBg;
            helpBtn.colors = colors;
            helpBtn.onClick.AddListener(() => OnHelpRequested?.Invoke());
        }

        private void BuildPhaseIndicator(RectTransform barParent)
        {
            float indicatorW = LayoutConfig.PhaseIndicatorWidth;
            float indicatorH = LayoutConfig.PhaseIndicatorHeight;
            float fontSize = LayoutConfig.PhaseIndicatorFontSize;

            _phaseIndicatorGo = new GameObject("PhaseIndicator", typeof(RectTransform));
            _phaseIndicatorGo.transform.SetParent(barParent, false);

            var rt = _phaseIndicatorGo.GetComponent<RectTransform>();
            // Position centered above the dock
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(indicatorW, indicatorH);

            // HLG for segments
            var hlg = _phaseIndicatorGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 2;
            hlg.padding = new RectOffset(4, 4, 2, 2);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            _phaseSegmentLabels = new TextMeshProUGUI[5];
            _phaseSegmentBgs = new Image[5];

            for (int i = 0; i < 5; i++)
            {
                var segGo = new GameObject($"Phase_{PhaseNames[i]}", typeof(RectTransform));
                segGo.transform.SetParent(_phaseIndicatorGo.transform, false);

                var segBg = segGo.AddComponent<Image>();
                segBg.sprite = TextureGenerator.GetRoundedRect(60, 20, 6);
                segBg.type = Image.Type.Sliced;
                segBg.color = UIFactory.Transparent;
                segBg.raycastTarget = false;
                _phaseSegmentBgs[i] = segBg;

                var label = UIFactory.CreateText($"Label_{PhaseNames[i]}", segGo.transform,
                    PhaseNames[i], fontSize, UIFactory.TextMuted);
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
                var labelRt = label.GetComponent<RectTransform>();
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.sizeDelta = Vector2.zero;
                _phaseSegmentLabels[i] = label;
            }

            _currentPhaseIndex = 0;
            UpdatePhaseHighlight();
        }

        private void UpdatePhaseHighlight()
        {
            if (_phaseSegmentLabels == null) return;
            for (int i = 0; i < 5; i++)
            {
                bool active = (i == _currentPhaseIndex);
                _phaseSegmentLabels[i].color = active ? UIFactory.AccentCyan : UIFactory.TextMuted;
                _phaseSegmentLabels[i].fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
                _phaseSegmentBgs[i].color = active
                    ? new Color(UIFactory.AccentCyan.r, UIFactory.AccentCyan.g, UIFactory.AccentCyan.b, 0.12f)
                    : UIFactory.Transparent;
            }
        }

        public void SetPhase(int handStep)
        {
            int newIndex = HandStepToPhaseIndex(handStep);
            if (newIndex == _currentPhaseIndex) return;
            _currentPhaseIndex = newIndex;
            UpdatePhaseHighlight();
        }

        public static int HandStepToPhaseIndex(int handStep)
        {
            if (handStep <= 5) return 0;   // Pre-flop
            if (handStep <= 7) return 1;   // Flop
            if (handStep <= 9) return 2;   // Turn
            if (handStep <= 11) return 3;  // River
            return 4;                       // Showdown
        }

        private void BuildTableBadges(Transform parent)
        {
            var badgesGo = new GameObject("TableBadges", typeof(RectTransform));
            badgesGo.transform.SetParent(parent, false);
            var badgesHlg = badgesGo.AddComponent<HorizontalLayoutGroup>();
            badgesHlg.spacing = 4;
            badgesHlg.padding = new RectOffset(4, 4, 0, 0);
            badgesHlg.childAlignment = TextAnchor.MiddleCenter;
            badgesHlg.childControlWidth = false;
            badgesHlg.childControlHeight = false;
            badgesHlg.childForceExpandWidth = false;
            badgesHlg.childForceExpandHeight = false;

            float badgeSz = LayoutConfig.ToolbarTableBadgeSize;
            int texSz = Mathf.Max(16, (int)(badgeSz * 2));

            _tableBadges = new Image[4];
            _tableBadgeLabels = new TextMeshProUGUI[4];

            for (int t = 0; t < 4; t++)
            {
                int tableId = t + 1;
                var badgeGo = new GameObject($"Badge{tableId}", typeof(RectTransform));
                badgeGo.transform.SetParent(badgesGo.transform, false);
                var badgeRt = badgeGo.GetComponent<RectTransform>();
                badgeRt.sizeDelta = new Vector2(badgeSz, badgeSz);

                var badgeImg = badgeGo.AddComponent<Image>();
                badgeImg.sprite = TextureGenerator.GetCircle(texSz);
                bool active = (tableId == 1);
                badgeImg.color = active ? UIFactory.AccentCyan : UIFactory.SubtleBorder;
                badgeImg.raycastTarget = true;
                _tableBadges[t] = badgeImg;

                // Number label inside badge
                var numLabel = UIFactory.CreateText($"Num{tableId}", badgeGo.transform,
                    tableId.ToString(), badgeSz * 0.5f,
                    active ? Color.white : UIFactory.TextMuted);
                numLabel.raycastTarget = false;
                numLabel.fontStyle = FontStyles.Bold;
                var numRt = numLabel.GetComponent<RectTransform>();
                numRt.anchorMin = Vector2.zero;
                numRt.anchorMax = Vector2.one;
                numRt.sizeDelta = Vector2.zero;
                _tableBadgeLabels[t] = numLabel;

                var badgeBtn = badgeGo.AddComponent<Button>();
                badgeBtn.targetGraphic = badgeImg;
                var dColors = badgeBtn.colors;
                dColors.normalColor = Color.white;
                dColors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
                dColors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
                badgeBtn.colors = dColors;
                badgeBtn.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.Play(SoundType.ButtonClick);
                    SelectTable(tableId);
                });

                var badgeLe = badgeGo.AddComponent<LayoutElement>();
                badgeLe.preferredWidth = badgeSz;
                badgeLe.preferredHeight = badgeSz;
            }
        }

        public void CycleSpeed()
        {
            _speedIndex = (_speedIndex + 1) % SpeedOptions.Length;
            UpdateSpeedLabel();
        }

        public int GetSpeedIndex() => _speedIndex;

        public void SetSpeedIndex(int index)
        {
            _speedIndex = Mathf.Clamp(index, 0, SpeedOptions.Length - 1);
            UpdateSpeedLabel();
        }

        private void UpdateSpeedLabel()
        {
            float s = SpeedOptions[_speedIndex];
            _speedLabel.text = s >= 1f ? $"{s:0}x" : $"{s:0.##}x";
        }

        public void SetActiveTable(int tableId)
        {
            _activeTableId = tableId;
            UpdateTableButtonStyles();
        }

        private void SelectTable(int tableId)
        {
            _activeTableId = tableId;
            UpdateTableButtonStyles();
            OnTableConnect?.Invoke(tableId);
        }

        private void UpdateTableButtonStyles()
        {
            if (_tableBadges == null) return;
            for (int i = 0; i < _tableBadges.Length; i++)
            {
                bool active = (i + 1) == _activeTableId;
                _tableBadges[i].color = active ? UIFactory.AccentCyan : UIFactory.SubtleBorder;
                if (_tableBadgeLabels != null && i < _tableBadgeLabels.Length)
                    _tableBadgeLabels[i].color = active ? Color.white : UIFactory.TextMuted;
            }
        }

        private void HandleMuteToggle()
        {
            var audio = AudioManager.Instance;
            if (audio == null) return;
            audio.ToggleMute();
            int iconSz = LayoutConfig.ToolbarIconSize;
            _muteIconImage.sprite = audio.IsMuted
                ? TextureGenerator.GetSpeakerMutedIcon(iconSz * 2)
                : TextureGenerator.GetSpeakerIcon(iconSz * 2);
            _muteIconImage.color = audio.IsMuted ? UIFactory.TextMuted : UIFactory.TextSecondary;
        }

        public void SetAutoPlayActive(bool active)
        {
            int iconSz = LayoutConfig.ToolbarIconSize;

            // Swap Play/Pause icon and label
            if (_playPauseIcon != null)
                _playPauseIcon.sprite = active
                    ? TextureGenerator.GetPauseIcon(iconSz * 2)
                    : TextureGenerator.GetTriangle(iconSz * 2);

            if (_playPauseLabel != null)
                _playPauseLabel.text = active ? "Pause" : "Play";

            // Change bg color: AccentGreen when active, AccentCyan when idle
            if (_playPauseBg != null)
                _playPauseBg.color = active ? UIFactory.AccentGreen : UIFactory.AccentCyan;
        }

        public void SetInteractable(bool interactable)
        {
            _nextStepButton.interactable = interactable;
            _resetButton.interactable = interactable;
            _playPauseButton.interactable = interactable;
        }

        /// <summary>
        /// Fade controls bar visibility (e.g. when betting UI takes over).
        /// </summary>
        public void SetVisible(bool visible)
        {
            var cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = visible ? 1f : 0f;
            cg.interactable = visible;

            if (_lobbyPillGo != null)
                _lobbyPillGo.SetActive(visible);

            if (_phaseIndicatorGo != null)
                _phaseIndicatorGo.SetActive(visible);
        }

        private void OnDestroy()
        {
            if (_lobbyPillGo != null)
                Destroy(_lobbyPillGo);

            if (_phaseIndicatorGo != null)
                Destroy(_phaseIndicatorGo);
        }
    }
}
