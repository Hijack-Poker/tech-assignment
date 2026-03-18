using UnityEngine;
using UnityEngine.UI;
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
        [SerializeField] private GameObject _actionPanel;
        [SerializeField] private Button _foldButton;
        [SerializeField] private Button _callButton;
        [SerializeField] private Button _raiseButton;
        [SerializeField] private TextMeshProUGUI _actionHintText;

        // Bet panel (for when no current bet - shows 2X, 3X, custom input)
        private GameObject _betOptionsPanel;
        private Button _bet2xButton;
        private Button _bet3xButton;
        private TMP_InputField _betInputField;
        private Button _betConfirmButton;

        private static readonly Color SelectedSpeedColor = new Color(0.17f, 0.55f, 0.92f);
        private static readonly Color DefaultSpeedColor = new Color(0.13f, 0.20f, 0.28f);
        private static readonly Color AutoPlayActiveColor = new Color(0.86f, 0.36f, 0.35f);
        private static readonly Color AutoPlayIdleColor = new Color(0.12f, 0.49f, 0.86f);
        private static readonly Color BetOptionColor = new Color(0.75f, 0.48f, 0.14f);

        private int _selectedSpeedIndex = 2;
        private float _currentToCall;
        private float _currentMinRaise;
        private bool _canActNow;
        private bool _canRaiseNow;
        private float _bigBlind;

        private void Awake()
        {
            RemoveLegacyControlsChildren();
            EnsureActionPanel();
            _nextStepButton.onClick.AddListener(OnNextStepClicked);
            _autoPlayButton.onClick.AddListener(OnAutoPlayClicked);
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

        private void RemoveLegacyControlsChildren()
        {
            RemoveChildIfExists("Separator");
            RemoveChildIfExists("Separator2");
            RemoveChildIfExists("ExitButton");
        }

        private void RemoveChildIfExists(string childName)
        {
            var child = transform.Find(childName);
            if (child != null)
                Destroy(child.gameObject);
        }

        private void Update()
        {
            var state = _gameManager.CurrentState;
            bool inBettingStep = state?.Game != null && IsBettingStep(state.Game.HandStep);
            bool driverCanAct = inBettingStep && state?.Game != null && state.Game.Move > 0;
            ComputeActionContext(state, inBettingStep, driverCanAct);

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
            if (_foldButton != null) _foldButton.interactable = _canActNow;
            if (_callButton != null) _callButton.interactable = _canActNow;

            // Show bet options (2X, 3X, input) when no current bet, otherwise show raise button
            bool isBetSituation = _currentToCall <= 0f && _canRaiseNow;
            if (_betOptionsPanel != null) _betOptionsPanel.SetActive(inBettingStep && isBetSituation && _canActNow);
            if (_raiseButton != null) _raiseButton.gameObject.SetActive(!isBetSituation);
            if (_raiseButton != null) _raiseButton.interactable = _canActNow && _canRaiseNow;

            SetActionButtonLabels();
            if (_actionHintText != null)
            {
                if (!inBettingStep) _actionHintText.text = "";
                else if (driverCanAct)
                {
                    if (_currentToCall <= 0f)
                        _actionHintText.text = $"Your turn (20s): Check, {( _canRaiseNow ? "Bet" : "No Raise" )}, or Fold";
                    else
                        _actionHintText.text = $"Your turn (20s): Fold, Call {_currentToCall:0.##}, or Raise";
                }
                else _actionHintText.text = $"Acting seat {state.Game.Move}: waiting for action";
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

        private void OnFoldClicked()
        {
            if (!_canActNow) return;
            _ = _gameManager.AdvanceStepAsync("fold", 0f);
        }

        private void OnCallClicked()
        {
            if (!_canActNow) return;

            if (_currentToCall <= 0f)
                _ = _gameManager.AdvanceStepAsync("check", 0f);
            else
                _ = _gameManager.AdvanceStepAsync("call", _currentToCall);
        }

        private void OnRaiseClicked()
        {
            if (!_canActNow || !_canRaiseNow) return;

            var state = _gameManager.CurrentState;
            if (state?.Game == null)
            {
                _ = _gameManager.AdvanceStepAsync("raise", 0f);
                return;
            }

            float amount = Mathf.Max(_currentMinRaise, Mathf.Max(state.Game.BigBlind, 1f));
            if (_currentToCall <= 0f)
                _ = _gameManager.AdvanceStepAsync("bet", amount);
            else
                _ = _gameManager.AdvanceStepAsync("raise", amount);
        }

        private void OnBet2xClicked()
        {
            if (!_canActNow || !_canRaiseNow) return;
            float amount = _bigBlind * 2f;
            _ = _gameManager.AdvanceStepAsync("bet", amount);
        }

        private void OnBet3xClicked()
        {
            if (!_canActNow || !_canRaiseNow) return;
            float amount = _bigBlind * 3f;
            _ = _gameManager.AdvanceStepAsync("bet", amount);
        }

        private void OnBetConfirmClicked()
        {
            if (!_canActNow || !_canRaiseNow) return;
            if (_betInputField == null) return;

            if (float.TryParse(_betInputField.text, out float amount) && amount >= _bigBlind)
            {
                _ = _gameManager.AdvanceStepAsync("bet", amount);
                _betInputField.text = "";
            }
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

        private void ComputeActionContext(TableResponse state, bool inBettingStep, bool driverCanAct)
        {
            _canActNow = false;
            _currentToCall = 0f;
            _currentMinRaise = 0f;
            _canRaiseNow = false;

            if (!inBettingStep || !driverCanAct || state?.Game == null)
                return;

            int actingSeat = state.Game.Move;
            var actor = state.Players != null ? state.Players.Find(p => p.Seat == actingSeat) : null;
            if (actor == null || actor.IsFolded || actor.IsAllIn)
                return;

            _canActNow = true;
            _bigBlind = state.Game.BigBlind;
            _currentToCall = Mathf.Max(0f, state.Game.CurrentBet - actor.Bet);
            // Min raise = 2x current bet (must at least double)
            float minRaiseTo = state.Game.CurrentBet * 2f;
            // But at least currentBet + bigBlind
            minRaiseTo = Mathf.Max(minRaiseTo, state.Game.CurrentBet + state.Game.BigBlind);
            _currentMinRaise = minRaiseTo;
            _canRaiseNow = actor.Stack > _currentToCall + 0.01f;
        }

        private void SetActionButtonLabels()
        {
            SetButtonLabel(_foldButton, "FOLD");

            if (_currentToCall <= 0f)
                SetButtonLabel(_callButton, "CHECK");
            else
                SetButtonLabel(_callButton, $"CALL {_currentToCall:0.#}");

            if (_currentToCall <= 0f)
                SetButtonLabel(_raiseButton, $"BET {Mathf.Max(_currentMinRaise, 1f):0.#}");
            else
                SetButtonLabel(_raiseButton, $"RAISE {Mathf.Max(_currentMinRaise, 1f):0.#}");
        }

        private static void SetButtonLabel(Button button, string text)
        {
            if (button == null) return;
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = text;
        }

        private void EnsureActionPanel()
        {
            if (_actionPanel != null && _foldButton != null && _callButton != null && _raiseButton != null)
                return;

            var panel = new GameObject("ActionPanel", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            var canvas = GetComponentInParent<Canvas>();
            panel.transform.SetParent(canvas != null ? canvas.transform : transform, false);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 0f);
            panelRt.anchorMax = new Vector2(1f, 0f);
            panelRt.pivot = new Vector2(0.5f, 0f);
            panelRt.anchoredPosition = new Vector2(0f, 86f);
            panelRt.sizeDelta = new Vector2(0f, 56f);

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
            _raiseButton = CreateActionButton(panel.transform, "RaiseBtn", "RAISE", BetOptionColor);

            // Create bet options panel (2X, 3X, input, confirm)
            _betOptionsPanel = new GameObject("BetOptionsPanel", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            _betOptionsPanel.transform.SetParent(panel.transform, false);
            var betH = _betOptionsPanel.GetComponent<HorizontalLayoutGroup>();
            betH.spacing = 6;
            betH.childAlignment = TextAnchor.MiddleCenter;
            betH.childForceExpandHeight = true;
            betH.childForceExpandWidth = false;
            var betLe = _betOptionsPanel.AddComponent<LayoutElement>();
            betLe.preferredWidth = 280f;

            _bet2xButton = CreateActionButton(_betOptionsPanel.transform, "Bet2x", "2X", BetOptionColor, 60f);
            _bet2xButton.onClick.AddListener(OnBet2xClicked);

            _bet3xButton = CreateActionButton(_betOptionsPanel.transform, "Bet3x", "3X", BetOptionColor, 60f);
            _bet3xButton.onClick.AddListener(OnBet3xClicked);

            // Input field
            var inputGO = new GameObject("BetInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
            inputGO.transform.SetParent(_betOptionsPanel.transform, false);
            var inputImg = inputGO.GetComponent<Image>();
            inputImg.color = new Color(0.15f, 0.20f, 0.28f);
            var inputLe = inputGO.GetComponent<LayoutElement>();
            inputLe.preferredWidth = 80f;
            inputLe.preferredHeight = 36f;

            var inputTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            inputTextGO.transform.SetParent(inputGO.transform, false);
            var inputTextRt = inputTextGO.GetComponent<RectTransform>();
            inputTextRt.anchorMin = Vector2.zero;
            inputTextRt.anchorMax = Vector2.one;
            inputTextRt.offsetMin = new Vector2(8, 4);
            inputTextRt.offsetMax = new Vector2(-8, -4);
            var inputText = inputTextGO.GetComponent<TextMeshProUGUI>();
            inputText.fontSize = 16;
            inputText.alignment = TextAlignmentOptions.Center;
            inputText.color = Color.white;

            var placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderGO.transform.SetParent(inputGO.transform, false);
            var phRt = placeholderGO.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(8, 4);
            phRt.offsetMax = new Vector2(-8, -4);
            var phText = placeholderGO.GetComponent<TextMeshProUGUI>();
            phText.text = "$";
            phText.fontSize = 16;
            phText.alignment = TextAlignmentOptions.Center;
            phText.color = new Color(0.5f, 0.5f, 0.5f);

            _betInputField = inputGO.GetComponent<TMP_InputField>();
            _betInputField.textComponent = inputText;
            _betInputField.placeholder = phText;
            _betInputField.contentType = TMP_InputField.ContentType.DecimalNumber;
            _betInputField.textViewport = inputTextRt;

            _betConfirmButton = CreateActionButton(_betOptionsPanel.transform, "BetConfirm", "BET", new Color(0.18f, 0.55f, 0.22f), 60f);
            _betConfirmButton.onClick.AddListener(OnBetConfirmClicked);

            _betOptionsPanel.SetActive(false);

            var hintGO = new GameObject("ActionHint", typeof(RectTransform), typeof(TextMeshProUGUI));
            hintGO.transform.SetParent(panel.transform, false);
            var hintRt = hintGO.GetComponent<RectTransform>();
            hintRt.sizeDelta = new Vector2(180f, 40f);
            _actionHintText = hintGO.GetComponent<TextMeshProUGUI>();
            _actionHintText.fontSize = 14;
            _actionHintText.alignment = TextAlignmentOptions.Center;
            _actionHintText.color = new Color(0.86f, 0.92f, 1f);
            _actionHintText.text = "";

            _actionPanel = panel;
            _actionPanel.SetActive(false);
        }

        private static Button CreateActionButton(Transform parent, string name, string label, Color color, float width = 92f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = width;
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
