using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;
using HijackPoker.Api;
using HijackPoker.Models;

namespace HijackPoker.Managers
{
    public class GameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PokerApiClient _apiClient;
        [SerializeField] private TableStateManager _stateManager;

        [Header("Settings")]
        [SerializeField] private int _tableId = 1;
        [SerializeField] private float _autoPlaySpeed = 1f;

        public bool IsAutoPlaying { get; private set; }
        public TableResponse CurrentState => _stateManager != null ? _stateManager.CurrentState : null;

        private bool _isProcessing;
        private Coroutine _autoPlayCoroutine;

        public static readonly float[] SpeedOptions = { 0.25f, 0.5f, 1f, 2f };

        private void Awake()
        {
            DOTween.Init(recycleAllByDefault: true, useSafeMode: true, logBehaviour: LogBehaviour.ErrorsOnly);
        }

        public string PlayerName { get; private set; }

        private async void Start()
        {
            PlayerName = PlayerPrefs.GetString("PlayerName", "Player");

            bool connected = await _apiClient.CheckConnectionAsync(maxRetries: 3);

            if (connected)
            {
                // Advance until we reach blind posting for a fresh hand (step 2),
                // so startup skips the two setup-only phases.
                HijackPoker.Models.TableResponse lastState = null;
                for (int i = 0; i < 30; i++)
                {
                    await _apiClient.ProcessStepAsync(_tableId);
                    var check = await _apiClient.GetTableStateAsync(_tableId);
                    if (check != null) lastState = check;
                    if (check != null && check.Game.HandStep == 2)
                    {
                        _stateManager.SetState(check);
                        break;
                    }
                }

                if (_stateManager.CurrentState == null && lastState != null)
                    _stateManager.SetState(lastState);
            }
            else
            {
                _stateManager.NotifyConnectionStatus("Error: Cannot reach holdem-processor at localhost:3030");
            }
        }

        public async Task AdvanceStepAsync(string action = null, float amount = 0f)
        {
            if (_isProcessing) return;
            _isProcessing = true;

            try
            {
                int actingSeat = _stateManager != null && _stateManager.CurrentState != null
                    ? _stateManager.CurrentState.Game.Move
                    : 0;
                var processResult = await _apiClient.ProcessStepAsync(_tableId, action, amount, actingSeat);
                if (processResult == null || !processResult.Success)
                {
                    _stateManager.NotifyConnectionStatus("Error: Process step failed");
                    StopAutoPlay();
                    return;
                }

                var state = await _apiClient.GetTableStateAsync(_tableId);
                if (state == null)
                {
                    _stateManager.NotifyConnectionStatus("Error: Could not fetch table state");
                    StopAutoPlay();
                    return;
                }

                _stateManager.SetState(state);
                _stateManager.NotifyConnectionStatus("Connected");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        public void ToggleAutoPlay()
        {
            if (IsAutoPlaying) StopAutoPlay();
            else StartAutoPlay();
        }

        public void SetAutoPlaySpeed(float intervalSeconds)
        {
            _autoPlaySpeed = intervalSeconds;
        }

        private void StartAutoPlay()
        {
            IsAutoPlaying = true;
            _autoPlayCoroutine = StartCoroutine(AutoPlayCoroutine());
        }

        private void StopAutoPlay()
        {
            IsAutoPlaying = false;
            if (_autoPlayCoroutine != null)
            {
                StopCoroutine(_autoPlayCoroutine);
                _autoPlayCoroutine = null;
            }
        }

        private IEnumerator AutoPlayCoroutine()
        {
            while (IsAutoPlaying)
            {
                yield return new WaitForSeconds(_autoPlaySpeed);
                if (!_isProcessing)
                {
                    _ = AdvanceStepAsync();
                }
            }
        }

        private void OnDestroy()
        {
            StopAutoPlay();
        }
    }
}
