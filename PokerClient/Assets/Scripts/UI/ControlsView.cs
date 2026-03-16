using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.Managers;

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

        private static readonly Color SelectedSpeedColor = new Color(0.2f, 0.6f, 1f);
        private static readonly Color DefaultSpeedColor = new Color(0.25f, 0.25f, 0.35f);
        private static readonly Color AutoPlayActiveColor = new Color(0.9f, 0.3f, 0.3f);
        private static readonly Color AutoPlayIdleColor = new Color(0.13f, 0.59f, 0.95f);

        private int _selectedSpeedIndex = 2;

        private void Awake()
        {
            _nextStepButton.onClick.AddListener(OnNextStepClicked);
            _autoPlayButton.onClick.AddListener(OnAutoPlayClicked);

            for (int i = 0; i < _speedButtons.Length; i++)
            {
                int idx = i;
                _speedButtons[i].onClick.AddListener(() => OnSpeedSelected(idx));
            }

            RefreshSpeedButtons();
        }

        private void Update()
        {
            _nextStepButton.interactable = !_gameManager.IsAutoPlaying;

            var img = _autoPlayButton.GetComponent<Image>();
            if (img != null)
                img.color = _gameManager.IsAutoPlaying ? AutoPlayActiveColor : AutoPlayIdleColor;
            _autoPlayButtonText.text = _gameManager.IsAutoPlaying ? "Stop" : "Auto Play";
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

        private void RefreshSpeedButtons()
        {
            for (int i = 0; i < _speedButtonImages.Length; i++)
                _speedButtonImages[i].color = i == _selectedSpeedIndex ? SelectedSpeedColor : DefaultSpeedColor;
        }
    }
}
