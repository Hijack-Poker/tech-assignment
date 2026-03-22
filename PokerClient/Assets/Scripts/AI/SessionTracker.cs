using System;
using System.Collections.Generic;
using UnityEngine;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.Utils;

namespace HijackPoker.AI
{
    public class SessionStats
    {
        public int HandsPlayed;
        public int HandsWon;
        public float WinRate => HandsPlayed > 0 ? (float)HandsWon / HandsPlayed : 0f;
        public float BiggestWin;
        public float BiggestLoss;
        public float TotalProfit;
        public float StartingStack;
        public int CurrentStreak; // positive = wins, negative = losses
        public int BestStreak;
        public int WorstStreak;

        // Position tracking
        public int WinsFromButton;
        public int WinsFromBlinds;
        public int WinsFromOther;

        // Action counts
        public int FoldCount;
        public int CallCount;
        public int RaiseCount;
        public int AllInCount;

        // VPIP tracking
        public int VpipHands;
        public int VpipEligibleHands;
        public float VPIP => VpipEligibleHands > 0 ? (float)VpipHands / VpipEligibleHands : 0f;
    }

    public class SessionTracker : MonoBehaviour
    {
        private TableStateManager _stateManager;
        private string _localPlayerName;
        private int _localSeat;

        private readonly SessionStats _stats = new SessionStats();
        private int _lastProcessedGameNo = -1;
        private int _handStartGameNo = -1; // separate tracker for hand-start capture
        private int _lastBettingStep = -1;
        private bool _vpipTrackedThisHand;
        private float _stackAtHandStart;

        public event Action<SessionStats> OnStatsUpdated;
        public SessionStats CurrentStats => _stats;

        private void Awake()
        {
            _stateManager = FindObjectOfType<TableStateManager>();
        }

        private void OnEnable()
        {
            if (_stateManager != null)
            {
                _stateManager.OnTableStateChanged += OnStateChanged;
                _stateManager.OnTableReset += OnTableReset;
            }
        }

        private void OnDisable()
        {
            if (_stateManager != null)
            {
                _stateManager.OnTableStateChanged -= OnStateChanged;
                _stateManager.OnTableReset -= OnTableReset;
            }
        }

        private void OnTableReset()
        {
            _lastProcessedGameNo = -1;
            _handStartGameNo = -1;
            _lastBettingStep = -1;
            _vpipTrackedThisHand = false;
        }

        private void OnStateChanged(TableResponse state)
        {
            if (state?.Game == null || state.Players == null) return;

            ResolveLocalPlayer(state);
            if (_localSeat <= 0) return;

            var localPlayer = FindLocalPlayer(state);
            if (localPlayer == null) return;

            int step = state.Game.HandStep;
            int gameNo = state.Game.GameNo;

            // Capture starting stack ONCE when a new hand begins
            if (gameNo != _handStartGameNo)
            {
                _handStartGameNo = gameNo;

                if (_stats.StartingStack <= 0f)
                    _stats.StartingStack = localPlayer.Stack + localPlayer.TotalBet;

                // Capture stack at hand start (stack + any blinds already posted)
                _stackAtHandStart = localPlayer.Stack + localPlayer.TotalBet;
                _vpipTrackedThisHand = false;
                _lastBettingStep = -1;
                _lastActionKey.Clear();
            }

            // Track VPIP during betting steps
            if (PokerConstants.IsBettingStep(step) && step != _lastBettingStep)
            {
                _lastBettingStep = step;
                TrackVPIP(localPlayer, state.Game);
            }

            // Track actions during betting
            if (PokerConstants.IsBettingStep(step))
                TrackActions(localPlayer);

            // Process completed hand — wait for step 15 so winnings are populated
            if (step >= 15 && gameNo != _lastProcessedGameNo)
            {
                _lastProcessedGameNo = gameNo;
                ProcessCompletedHand(localPlayer, state.Game);
                OnStatsUpdated?.Invoke(_stats);
            }
        }

        private void ResolveLocalPlayer(TableResponse state)
        {
            if (_localSeat > 0 && !string.IsNullOrEmpty(_localPlayerName)) return;

            _localPlayerName = PlayerPrefs.GetString("PlayerName", "Player");
            _localSeat = SeatResolver.ResolveLocalSeat(state.Players, _localPlayerName);
        }

        private PlayerState FindLocalPlayer(TableResponse state)
        {
            foreach (var p in state.Players)
                if (p.Seat == _localSeat) return p;
            return null;
        }

        private void TrackVPIP(PlayerState player, GameState game)
        {
            if (game.HandStep == 5 && !_vpipTrackedThisHand)
            {
                _stats.VpipEligibleHands++;

                string action = player.Action?.ToLower();
                if (action == "call" || action == "raise" || action == "bet" || action == "allin")
                {
                    _stats.VpipHands++;
                    _vpipTrackedThisHand = true;
                }
            }
            if (!_vpipTrackedThisHand)
            {
                string action = player.Action?.ToLower();
                if (action == "raise" || action == "bet" || action == "allin")
                {
                    _stats.VpipHands++;
                    _vpipTrackedThisHand = true;
                }
            }
        }

        private readonly Dictionary<int, string> _lastActionKey = new();

        private void TrackActions(PlayerState player)
        {
            string action = player.Action?.ToLower();
            if (string.IsNullOrEmpty(action)) return;

            string key = $"{action}_{player.Bet}_{player.TotalBet}";
            _lastActionKey.TryGetValue(player.Seat, out string prev);
            if (key == prev) return;
            _lastActionKey[player.Seat] = key;

            switch (action)
            {
                case "fold": _stats.FoldCount++; break;
                case "call": case "check": _stats.CallCount++; break;
                case "raise": case "bet": _stats.RaiseCount++; break;
                case "allin": _stats.AllInCount++; break;
            }
        }

        private void ProcessCompletedHand(PlayerState player, GameState game)
        {
            _stats.HandsPlayed++;

            if (player.IsWinner && player.Winnings > 0)
            {
                _stats.HandsWon++;

                if (player.Winnings > _stats.BiggestWin)
                    _stats.BiggestWin = player.Winnings;

                if (_stats.CurrentStreak >= 0)
                    _stats.CurrentStreak++;
                else
                    _stats.CurrentStreak = 1;

                if (_stats.CurrentStreak > _stats.BestStreak)
                    _stats.BestStreak = _stats.CurrentStreak;

                if (player.Seat == game.DealerSeat)
                    _stats.WinsFromButton++;
                else if (player.Seat == game.SmallBlindSeat || player.Seat == game.BigBlindSeat)
                    _stats.WinsFromBlinds++;
                else
                    _stats.WinsFromOther++;
            }
            else
            {
                float loss = _stackAtHandStart - player.Stack;
                if (loss > _stats.BiggestLoss)
                    _stats.BiggestLoss = loss;

                if (_stats.CurrentStreak <= 0)
                    _stats.CurrentStreak--;
                else
                    _stats.CurrentStreak = -1;

                if (_stats.CurrentStreak < _stats.WorstStreak)
                    _stats.WorstStreak = _stats.CurrentStreak;
            }

            _stats.TotalProfit = player.Stack - _stats.StartingStack;
        }
    }
}
