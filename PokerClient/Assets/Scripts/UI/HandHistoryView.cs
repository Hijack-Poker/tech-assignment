using System.Collections.Generic;
using UnityEngine;
using TMPro;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    public class HandHistoryView : MonoBehaviour
    {
        [SerializeField] private TableStateManager _stateManager;
        [SerializeField] private TextMeshProUGUI _historyText;

        private readonly List<string> _lines = new List<string>();
        private const int MaxLines = 30;

        // Track last known action per seat to only log new actions
        private readonly Dictionary<int, string> _lastActions = new();
        private int _lastGameNo = -1;
        private int _lastHandStep = -1;

        private void OnEnable()
        {
            if (_stateManager == null) return;
            _stateManager.OnTableStateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            if (_stateManager != null)
                _stateManager.OnTableStateChanged -= OnStateChanged;
        }

        private void OnStateChanged(TableResponse state)
        {
            var game = state.Game;

            // Reset action tracking on new hand
            if (game.GameNo != _lastGameNo)
            {
                _lastActions.Clear();
                _lastGameNo = game.GameNo;
            }

            // Only show step header when the step changes
            if (game.HandStep != _lastHandStep)
            {
                string label = _stateManager.GetStepLabel(game.HandStep);
                AddLine($"Hand #{game.GameNo} — {label}");

                // Reset actions when entering a new betting round
                if (IsBettingStep(game.HandStep) && !IsBettingStep(_lastHandStep))
                    _lastActions.Clear();
            }

            // Show blind posts
            if (game.HandStep == 2 && _lastHandStep != 2)
            {
                var sbPlayer = GetPlayerBySeat(state.Players, game.SmallBlindSeat);
                if (sbPlayer != null)
                    AddLine($"  {ColoredName(sbPlayer)} posts SB {MoneyFormatter.Format(game.SmallBlind)}");
            }
            else if (game.HandStep == 3 && _lastHandStep != 3)
            {
                var bbPlayer = GetPlayerBySeat(state.Players, game.BigBlindSeat);
                if (bbPlayer != null)
                    AddLine($"  {ColoredName(bbPlayer)} posts BB {MoneyFormatter.Format(game.BigBlind)}");
            }

            // Show only NEW player actions during betting rounds
            if (IsBettingStep(game.HandStep) && state.Players != null)
            {
                foreach (var p in state.Players)
                {
                    if (string.IsNullOrEmpty(p.Action)) continue;

                    // Build a key: seat + action + bet amount to detect changes
                    string actionKey = $"{p.Action}_{p.Bet}_{p.TotalBet}";
                    _lastActions.TryGetValue(p.Seat, out string prevKey);

                    if (actionKey != prevKey)
                    {
                        _lastActions[p.Seat] = actionKey;
                        string actionText = FormatAction(p);
                        if (!string.IsNullOrEmpty(actionText))
                            AddLine($"  {actionText}");
                    }
                }
            }

            // Show winners
            if (game.HandStep == 14 && _lastHandStep != 14 && state.Players != null)
            {
                foreach (var p in state.Players)
                {
                    if (p.IsWinner)
                        AddLine($"  {ColoredName(p)} wins <color=#FFD700>{MoneyFormatter.FormatGain(p.Winnings)}</color>");
                }
            }

            if (game.HandStep == 15 && _lastHandStep != 15)
                AddLine("────────────────");

            _lastHandStep = game.HandStep;
            UpdateDisplay();
        }

        private PlayerState GetPlayerBySeat(List<PlayerState> players, int seat)
        {
            if (players == null) return null;
            foreach (var p in players)
                if (p.Seat == seat) return p;
            return null;
        }

        private bool IsBettingStep(int step)
        {
            // 5=Pre-Flop, 7=Flop, 9=Turn, 11=River betting
            return step == 5 || step == 7 || step == 9 || step == 11;
        }

        private string ColoredName(PlayerState p)
        {
            var c = SeatView.GetSeatColor(p.Seat);
            string hex = ColorUtility.ToHtmlStringRGB(c);
            return $"<color=#{hex}>{p.Username}</color>";
        }

        private string FormatAction(PlayerState p)
        {
            string name = ColoredName(p);
            switch (p.Action?.ToLower())
            {
                case "fold":
                    return $"{name} folds";
                case "check":
                    return $"{name} checks";
                case "call":
                    return $"{name} calls {MoneyFormatter.Format(p.Bet)}";
                case "bet":
                    return $"{name} bets {MoneyFormatter.Format(p.Bet)}";
                case "raise":
                    return $"{name} raises to {MoneyFormatter.Format(p.TotalBet)}";
                case "allin":
                    return $"{name} ALL-IN {MoneyFormatter.Format(p.TotalBet)}";
                default:
                    return null;
            }
        }

        private void AddLine(string text)
        {
            _lines.Add(text);
            while (_lines.Count > MaxLines)
                _lines.RemoveAt(0);
        }

        private void UpdateDisplay()
        {
            if (_historyText != null)
                _historyText.text = string.Join("\n", _lines);
        }
    }
}
