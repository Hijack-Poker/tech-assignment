using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    public class HudView : MonoBehaviour
    {
        [SerializeField] private TableStateManager _stateManager;
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private TextMeshProUGUI _phaseLabel;
        [SerializeField] private TextMeshProUGUI _handNumberText;
        [SerializeField] private TextMeshProUGUI _actionText;
        [SerializeField] private TextMeshProUGUI _potText;
        [SerializeField] private Image _potChipImage;
        [SerializeField] private Button _exitButton;

        private Button _restartButton;
        private Button _helpButton;
        private HelpPopupView _helpPopup;

        private void Awake()
        {
            if (_exitButton == null)
            {
                var btn = transform.Find("TopRightExitBtn");
                if (btn != null) _exitButton = btn.GetComponent<Button>();
            }
            if (_exitButton != null)
                _exitButton.onClick.AddListener(OnExitClicked);

            CreateRestartButton();
            CreateHelpButton();
        }

        private void OnEnable() => _stateManager.OnTableStateChanged += OnStateChanged;
        private void OnDisable() => _stateManager.OnTableStateChanged -= OnStateChanged;

        private void OnStateChanged(TableResponse state)
        {
            var game = state.Game;

            _phaseLabel.text = _stateManager.GetStepLabel(game.HandStep);
            _phaseLabel.transform.DOKill();
            _phaseLabel.transform.DOPunchScale(Vector3.one * 0.15f, 0.35f, 8, 0.5f);

            _handNumberText.text = $"Hand #{game.GameNo}";

            // Hide pot display in HUD (shown on table instead)
            if (_potText != null) _potText.gameObject.SetActive(false);
            if (_potChipImage != null) _potChipImage.gameObject.SetActive(false);

            if (game.Move > 0)
            {
                _actionText.text = $"Seat {game.Move} to act";
                _actionText.gameObject.SetActive(true);
            }
            else
            {
                _actionText.gameObject.SetActive(false);
            }
        }

        private void CreateRestartButton()
        {
            if (_exitButton == null) return;

            var exitRt = _exitButton.GetComponent<RectTransform>();

            // Create restart button as sibling, positioned to the left of exit
            var go = new GameObject("RestartBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(exitRt.parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = exitRt.anchorMin;
            rt.anchorMax = exitRt.anchorMax;
            rt.pivot = exitRt.pivot;
            rt.sizeDelta = new Vector2(90f, exitRt.sizeDelta.y);
            // Place to the left of exit button with 8px gap
            rt.anchoredPosition = exitRt.anchoredPosition + new Vector2(-(exitRt.sizeDelta.x + 8f), 0f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.75f, 0.22f, 0.17f);

            var txtGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(go.transform, false);
            var txtRt = txtGO.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            var txt = txtGO.GetComponent<TextMeshProUGUI>();
            txt.text = "RESTART";
            txt.fontSize = 14;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;

            _restartButton = go.GetComponent<Button>();
            _restartButton.onClick.AddListener(OnRestartClicked);
        }

        private void OnRestartClicked()
        {
            if (_gameManager == null) return;
            _ = _gameManager.FreshRestartAsync();
        }

        private void OnExitClicked()
        {
            if (_gameManager != null && _gameManager.IsAutoPlaying)
                _gameManager.ToggleAutoPlay();
            DOTween.KillAll();
            SceneManager.LoadScene("HomeScene");
        }

        private void CreateHelpButton()
        {
            // Place "?" button to the left of the restart button
            RectTransform anchorRt = null;
            Transform parent = null;

            if (_restartButton != null)
            {
                anchorRt = _restartButton.GetComponent<RectTransform>();
                parent = anchorRt.parent;
            }
            else if (_exitButton != null)
            {
                anchorRt = _exitButton.GetComponent<RectTransform>();
                parent = anchorRt.parent;
            }

            if (anchorRt == null || parent == null) return;

            var go = new GameObject("HelpBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorRt.anchorMin;
            rt.anchorMax = anchorRt.anchorMax;
            rt.pivot = anchorRt.pivot;
            rt.sizeDelta = new Vector2(36f, anchorRt.sizeDelta.y);
            rt.anchoredPosition = anchorRt.anchoredPosition + new Vector2(-(anchorRt.sizeDelta.x + 8f), 0f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.18f, 0.42f, 0.68f);

            var txtGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(go.transform, false);
            var txtRt = txtGO.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            var txt = txtGO.GetComponent<TextMeshProUGUI>();
            txt.text = "?";
            txt.fontSize = 20;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;

            _helpButton = go.GetComponent<Button>();
            _helpButton.onClick.AddListener(OnHelpClicked);
        }

        private void OnHelpClicked()
        {
            if (_helpPopup == null)
            {
                _helpPopup = GetComponent<HelpPopupView>();
                if (_helpPopup == null)
                    _helpPopup = gameObject.AddComponent<HelpPopupView>();
            }
            _helpPopup.Toggle();
        }

        private void OnDestroy()
        {
            if (_phaseLabel != null) _phaseLabel.transform.DOKill();
        }
    }
}
