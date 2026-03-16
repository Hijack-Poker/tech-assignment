using UnityEngine;
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
        [SerializeField] private TextMeshProUGUI _phaseLabel;
        [SerializeField] private TextMeshProUGUI _handNumberText;
        [SerializeField] private TextMeshProUGUI _actionText;
        [SerializeField] private TextMeshProUGUI _potText;

        private float _displayedPot;

        private void OnEnable() => _stateManager.OnTableStateChanged += OnStateChanged;
        private void OnDisable() => _stateManager.OnTableStateChanged -= OnStateChanged;

        private void OnStateChanged(TableResponse state)
        {
            var game = state.Game;

            _phaseLabel.text = _stateManager.GetStepLabel(game.HandStep);
            _phaseLabel.transform.DOKill();
            _phaseLabel.transform.DOPunchScale(Vector3.one * 0.15f, 0.35f, 8, 0.5f);

            _handNumberText.text = $"Hand #{game.GameNo}";

            // Pot tween
            DOTween.Kill(_potText);
            DOVirtual.Float(_displayedPot, game.Pot, 0.4f, value =>
            {
                _displayedPot = value;
                _potText.text = $"Pot: {MoneyFormatter.Format(value)}";
            }).SetEase(Ease.OutCubic).SetTarget(_potText);

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

        private void OnDestroy()
        {
            DOTween.Kill(_potText);
            if (_phaseLabel != null) _phaseLabel.transform.DOKill();
        }
    }
}
