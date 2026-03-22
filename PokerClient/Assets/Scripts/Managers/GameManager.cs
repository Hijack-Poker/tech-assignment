using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;
using HijackPoker.Api;
using HijackPoker.Models;

namespace HijackPoker.Managers
{
    public enum AutoPlayStyle
    {
        Safe,        // Call/check only, never raise
        SmallRandom, // Mostly call/check, occasionally one player doubles per round
        Hard         // Random: fold, call, raise, all-in — anything goes
    }

    public class GameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PokerApiClient _apiClient;
        [SerializeField] private TableStateManager _stateManager;

        [Header("Settings")]
        [SerializeField] private int _tableId = 1;
        [SerializeField] private float _autoPlaySpeed = 1f;

        public bool IsAutoPlaying { get; private set; }
        public AutoPlayStyle PlayStyle { get; private set; } = AutoPlayStyle.Safe;
        public TableResponse CurrentState => _stateManager != null ? _stateManager.CurrentState : null;

        private bool _isProcessing;
        private Coroutine _autoPlayCoroutine;
        private bool _hasRaisedThisRound;
        private int _lastBettingStep = -1;
        private HijackPoker.Api.WebSocketClient _wsClient;

        public static readonly float[] SpeedOptions = { 0.25f, 0.5f, 1f, 2f };

        private void Awake()
        {
            DOTween.Init(recycleAllByDefault: true, useSafeMode: true, logBehaviour: LogBehaviour.ErrorsOnly);
        }

        public string PlayerName { get; private set; }

        private async void Start()
        {
            PlayerName = PlayerPrefs.GetString("PlayerName", "Player");
            _tableId = PlayerPrefs.GetInt("TableId", _tableId);

            bool connected = await _apiClient.CheckConnectionAsync(maxRetries: 3);

            if (connected)
            {
                // Reset to a fresh game
                await _apiClient.ResetTableAsync(_tableId);

                // Advance until we reach blind posting (step 2),
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

                // Start WebSocket for real-time updates (falls back gracefully)
                ConnectWebSocket();
            }
            else
            {
                _stateManager.NotifyConnectionStatus("Error: Cannot reach holdem-processor at localhost:3030");
            }
        }

        private void ConnectWebSocket()
        {
            _wsClient = gameObject.GetComponent<HijackPoker.Api.WebSocketClient>();
            if (_wsClient == null)
                _wsClient = gameObject.AddComponent<HijackPoker.Api.WebSocketClient>();

            _wsClient.OnTableUpdate += OnWebSocketTableUpdate;
            _wsClient.Connect(_tableId);
        }

        private async void OnWebSocketTableUpdate(int tableId)
        {
            if (tableId != _tableId || _isProcessing) return;

            var state = await _apiClient.GetTableStateAsync(_tableId);
            if (state != null)
                _stateManager.SetState(state);
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

        public async Task FreshRestartAsync()
        {
            if (_isProcessing) return;
            _isProcessing = true;

            try
            {
                StopAutoPlay();

                _stateManager.NotifyTableReset();

                bool ok = await _apiClient.FreshResetTableAsync(_tableId);
                if (!ok)
                {
                    _stateManager.NotifyConnectionStatus("Error: Fresh reset failed");
                    return;
                }

                // Advance until we reach blind posting (step 2)
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
            else StartAutoPlayWithStyle(PlayStyle);
        }

        public void StartAutoPlayWithStyle(AutoPlayStyle style)
        {
            StopAutoPlay();
            PlayStyle = style;
            IsAutoPlaying = true;
            _hasRaisedThisRound = false;
            _lastBettingStep = -1;
            _autoPlayCoroutine = StartCoroutine(AutoPlayCoroutine());
        }

        public void SetAutoPlaySpeed(float intervalSeconds)
        {
            _autoPlaySpeed = intervalSeconds;
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
                if (_isProcessing) continue;

                var state = _stateManager?.CurrentState;
                if (state?.Game == null) continue;

                int step = state.Game.HandStep;
                int move = state.Game.Move;

                // Reset raise tracker on new betting round
                if (IsBettingStep(step) && step != _lastBettingStep)
                {
                    _hasRaisedThisRound = false;
                    _lastBettingStep = step;
                }

                if (IsBettingStep(step) && move > 0)
                {
                    var actor = state.Players?.Find(p => p.Seat == move);
                    if (actor != null && !actor.IsFolded && !actor.IsAllIn)
                        SubmitAutoAction(actor, state);
                    else
                        _ = AdvanceStepAsync();
                }
                else if (state.Game.Status == "completed" || step >= 15)
                {
                    _ = AdvanceStepAsync();
                }
                else
                {
                    _ = AdvanceStepAsync();
                }
            }
        }

        private void SubmitAutoAction(PlayerState actor, TableResponse state)
        {
            float toCall = Mathf.Max(0f, state.Game.CurrentBet - actor.Bet);
            float minRaise = Mathf.Max(state.Game.CurrentBet + state.Game.BigBlind,
                                       state.Game.CurrentBet * 2f);
            bool canRaise = actor.Stack > toCall + 0.01f;

            switch (PlayStyle)
            {
                case AutoPlayStyle.Safe:
                    // Always call or check
                    if (toCall <= 0f)
                        _ = AdvanceStepAsync("check", 0f);
                    else
                        _ = AdvanceStepAsync("call", toCall);
                    break;

                case AutoPlayStyle.SmallRandom:
                    // One raise per betting round, otherwise call/check
                    if (!_hasRaisedThisRound && canRaise && Random.value < 0.2f)
                    {
                        _hasRaisedThisRound = true;
                        float raiseAmount = Mathf.Min(minRaise, actor.Stack + actor.Bet);
                        if (toCall <= 0f)
                            _ = AdvanceStepAsync("bet", raiseAmount);
                        else
                            _ = AdvanceStepAsync("raise", raiseAmount);
                    }
                    else if (toCall <= 0f)
                    {
                        _ = AdvanceStepAsync("check", 0f);
                    }
                    else
                    {
                        _ = AdvanceStepAsync("call", toCall);
                    }
                    break;

                case AutoPlayStyle.Hard:
                    // Random actions: 10% fold, 40% call/check, 30% raise, 20% all-in
                    float roll = Random.value;
                    if (roll < 0.10f && toCall > 0f)
                    {
                        // Fold (only if there's something to call)
                        _ = AdvanceStepAsync("fold", 0f);
                    }
                    else if (roll < 0.50f)
                    {
                        // Call/check
                        if (toCall <= 0f)
                            _ = AdvanceStepAsync("check", 0f);
                        else
                            _ = AdvanceStepAsync("call", toCall);
                    }
                    else if (roll < 0.80f && canRaise)
                    {
                        // Raise (random between min raise and 3x)
                        float maxR = Mathf.Min(minRaise * 3f, actor.Stack + actor.Bet);
                        float raiseAmount = Mathf.Max(minRaise, Random.Range(minRaise, maxR));
                        if (toCall <= 0f)
                            _ = AdvanceStepAsync("bet", raiseAmount);
                        else
                            _ = AdvanceStepAsync("raise", raiseAmount);
                    }
                    else if (canRaise)
                    {
                        // All-in
                        _ = AdvanceStepAsync("allin", actor.Stack);
                    }
                    else
                    {
                        // Can't raise, just call
                        if (toCall <= 0f)
                            _ = AdvanceStepAsync("check", 0f);
                        else
                            _ = AdvanceStepAsync("call", toCall);
                    }
                    break;
            }
        }

        private static bool IsBettingStep(int step)
        {
            return step == 5 || step == 7 || step == 9 || step == 11;
        }

        private void OnDestroy()
        {
            StopAutoPlay();
            if (_wsClient != null)
            {
                _wsClient.OnTableUpdate -= OnWebSocketTableUpdate;
                _wsClient.Disconnect();
            }
        }
    }
}
