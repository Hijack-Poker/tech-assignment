using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using HijackPoker.Managers;
using HijackPoker.Models;

namespace HijackPoker.UI
{
    public class ControlsView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager _gameManager;

        [Header("Buttons")]
        [SerializeField] private Button _nextStepButton;
        [SerializeField] private Button _autoPlayButton;
        [SerializeField] private TextMeshProUGUI _autoPlayButtonText;
        [SerializeField] private Button[] _speedButtons;
        [SerializeField] private Image[] _speedButtonImages;
        [SerializeField] private Button _exitButton;
        [SerializeField] private GameObject _actionPanel;
        [SerializeField] private Button _foldButton;
        [SerializeField] private Button _callButton;
        [SerializeField] private Button _raiseButton;
        [SerializeField] private TextMeshProUGUI _actionHintText;

        private static readonly Color SelectedSpeedColor = new Color(0.17f, 0.55f, 0.92f);
        private static readonly Color DefaultSpeedColor = new Color(0.13f, 0.20f, 0.28f);
        private static readonly Color AutoPlayActiveColor = new Color(0.86f, 0.36f, 0.35f);
        private static readonly Color AutoPlayIdleColor = new Color(0.12f, 0.49f, 0.86f);

        private int _selectedSpeedIndex = 2;

        private void Awake()
        {
            EnsureActionPanel();
            _nextStepButton.onClick.AddListener(OnNextStepClicked);
            _autoPlayButton.onClick.AddListener(OnAutoPlayClicked);
            if (_exitButton != null)
                _exitButton.onClick.AddListener(OnExitClicked);
            if (_foldButton != null) _foldButton.onClick.AddListener(OnFoldClicked);
            if (_callButton != null) _callButton.onClick.AddListener(OnCallClicked);
            if (_raiseButton != null) _raiseButton.onClick.AddListener(OnRaiseClicked);

            for (int i = 0; i < _speedButtons.Length; i++)
            {
                int idx = i;
                _speedButtons[i].onClick.AddListener(() => OnSpeedSelected(idx));
            }

            RefreshSpeedButtons();
        }

        private void Update()
        {
            var state = _gameManager.CurrentState;
            bool localCanAct = CanLocalPlayerAct(state);
            bool inBettingStep = state?.Game != null && IsBettingStep(state.Game.HandStep);

            _nextStepButton.interactable = !_gameManager.IsAutoPlaying && !inBettingStep;

            var img = _autoPlayButton.GetComponent<Image>();
            if (img != null)
                img.color = _gameManager.IsAutoPlaying ? AutoPlayActiveColor : AutoPlayIdleColor;
            _autoPlayButtonText.text = _gameManager.IsAutoPlaying ? "Stop" : "Auto Play";
            _autoPlayButton.interactable = !inBettingStep;
            if (inBettingStep && _gameManager.IsAutoPlaying)
                _gameManager.ToggleAutoPlay();

            if (_actionPanel != null)
                _actionPanel.SetActive(inBettingStep);
            if (_foldButton != null) _foldButton.interactable = inBettingStep;
            if (_callButton != null) _callButton.interactable = inBettingStep;
            if (_raiseButton != null) _raiseButton.interactable = inBettingStep;
            if (_actionHintText != null)
            {
                if (!inBettingStep) _actionHintText.text = "";
                else if (localCanAct) _actionHintText.text = "Your turn (20s): Fold, Call, or Raise";
                else _actionHintText.text = $"Acting seat {state.Game.Move}: choose Fold / Call / Raise";
            }
        }

        private void OnNextStepClicked()
        {
            _ = _gameManager.AdvanceStepAsync();
        }

        private void OnAutoPlayClicked()
        {
            _gameManager.ToggleAutoPlay();
        }

        private void OnSpeedSelected(int index)
        {
            _selectedSpeedIndex = index;
            _gameManager.SetAutoPlaySpeed(GameManager.SpeedOptions[index]);
            RefreshSpeedButtons();
        }

        private void OnExitClicked()
        {
            if (_gameManager.IsAutoPlaying)
                _gameManager.ToggleAutoPlay();
            SceneManager.LoadScene("HomeScene");
        }

        private void OnFoldClicked()
        {
            _ = _gameManager.AdvanceStepAsync("fold", 0f);
        }

        private void OnCallClicked()
        {
            _ = _gameManager.AdvanceStepAsync("call", 0f);
        }

        private void OnRaiseClicked()
        {
            var state = _gameManager.CurrentState;
            if (state?.Game == null)
            {
                _ = _gameManager.AdvanceStepAsync("raise", 0f);
                return;
            }

            int localSeat = ResolveLocalSeat(state);
            var me = state.Players != null ? state.Players.Find(p => p.Seat == localSeat) : null;
            float myBet = me != null ? me.Bet : 0f;
            float toCall = Mathf.Max(0f, state.Game.CurrentBet - myBet);
            float raiseAdd = toCall + Mathf.Max(state.Game.BigBlind, 1f);
            _ = _gameManager.AdvanceStepAsync("raise", raiseAdd);
        }

        private static bool IsBettingStep(int step)
        {
            return step == 5 || step == 7 || step == 9 || step == 11;
        }

        private static bool CanLocalPlayerAct(TableResponse state)
        {
            if (state?.Game == null) return false;
            if (!IsBettingStep(state.Game.HandStep)) return false;
            int localSeat = ResolveLocalSeat(state);
            return localSeat > 0 && state.Game.Move == localSeat;
        }

        private static int ResolveLocalSeat(TableResponse state)
        {
            if (state?.Players == null || state.Players.Count == 0) return -1;
            string localName = PlayerPrefs.GetString("PlayerName", "Player");
            var me = state.Players.Find(p =>
                !string.IsNullOrEmpty(p.Username) &&
                p.Username.Equals(localName, System.StringComparison.OrdinalIgnoreCase));
            return me != null ? me.Seat : 1;
        }

        private void EnsureActionPanel()
        {
            if (_actionPanel != null && _foldButton != null && _callButton != null && _raiseButton != null)
                return;

            var panel = new GameObject("ActionPanel", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            panel.transform.SetParent(transform, false);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 1f);
            panelRt.anchorMax = new Vector2(0.5f, 1f);
            panelRt.pivot = new Vector2(0.5f, 1f);
            panelRt.anchoredPosition = new Vector2(0f, 84f);
            panelRt.sizeDelta = new Vector2(540f, 56f);

            var panelImg = panel.GetComponent<Image>();
            panelImg.color = new Color(0.05f, 0.12f, 0.20f, 0.93f);

            var h = panel.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 12;
            h.padding = new RectOffset(12, 12, 8, 8);
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childForceExpandHeight = true;
            h.childForceExpandWidth = true;

            _foldButton = CreateActionButton(panel.transform, "FoldBtn", "FOLD", new Color(0.45f, 0.20f, 0.22f));
            _callButton = CreateActionButton(panel.transform, "CallBtn", "CALL", new Color(0.13f, 0.45f, 0.72f));
            _raiseButton = CreateActionButton(panel.transform, "RaiseBtn", "RAISE", new Color(0.75f, 0.48f, 0.14f));

            var hintGO = new GameObject("ActionHint", typeof(RectTransform), typeof(TextMeshProUGUI));
            hintGO.transform.SetParent(panel.transform, false);
            var hintRt = hintGO.GetComponent<RectTransform>();
            hintRt.sizeDelta = new Vector2(220f, 40f);
            _actionHintText = hintGO.GetComponent<TextMeshProUGUI>();
            _actionHintText.fontSize = 16;
            _actionHintText.alignment = TextAlignmentOptions.Center;
            _actionHintText.color = new Color(0.86f, 0.92f, 1f);
            _actionHintText.text = "";

            _actionPanel = panel;
            _actionPanel.SetActive(false);
        }

        private static Button CreateActionButton(Transform parent, string name, string label, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = 92f;
            le.preferredHeight = 40f;

            var txtGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(go.transform, false);
            var txtRt = txtGO.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            var txt = txtGO.GetComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.fontSize = 18;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;

            return go.GetComponent<Button>();
        }

        private void RefreshSpeedButtons()
        {
            for (int i = 0; i < _speedButtonImages.Length; i++)
                _speedButtonImages[i].color = i == _selectedSpeedIndex ? SelectedSpeedColor : DefaultSpeedColor;
        }
    }
}
