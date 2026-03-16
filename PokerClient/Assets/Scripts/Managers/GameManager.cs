using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;
using HijackPoker.Api;

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

        private bool _isProcessing;
        private Coroutine _autoPlayCoroutine;

        public static readonly float[] SpeedOptions = { 0.25f, 0.5f, 1f, 2f };

        private void Awake()
        {
            DOTween.Init(recycleAllByDefault: true, useSafeMode: true, logBehaviour: LogBehaviour.ErrorsOnly);
        }

        private async void Start()
        {
            _stateManager.NotifyConnectionStatus("Connecting...");
            bool connected = await _apiClient.CheckConnectionAsync(maxRetries: 3);

            if (connected)
            {
                _stateManager.NotifyConnectionStatus("Connected");
                var state = await _apiClient.GetTableStateAsync(_tableId);
                if (state != null) _stateManager.SetState(state);
            }
            else
            {
                _stateManager.NotifyConnectionStatus("Error: Cannot reach holdem-processor at localhost:3030");
            }
        }

        public async Task AdvanceStepAsync()
        {
            if (_isProcessing) return;
            _isProcessing = true;

            try
            {
                var processResult = await _apiClient.ProcessStepAsync(_tableId);
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
