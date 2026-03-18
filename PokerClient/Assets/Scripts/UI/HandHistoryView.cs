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
            string label = _stateManager.GetStepLabel(game.HandStep);

            AddLine($"Hand #{game.GameNo} — {label}");

            // Show blind posts
            if (game.HandStep == 2) // Posting Small Blind
            {
                var sbPlayer = GetPlayerBySeat(state.Players, game.SmallBlindSeat);
                if (sbPlayer != null)
                    AddLine($"  {ColoredName(sbPlayer)} posts SB {MoneyFormatter.Format(game.SmallBlind)}");
            }
            else if (game.HandStep == 3) // Posting Big Blind
            {
                var bbPlayer = GetPlayerBySeat(state.Players, game.BigBlindSeat);
                if (bbPlayer != null)
                    AddLine($"  {ColoredName(bbPlayer)} posts BB {MoneyFormatter.Format(game.BigBlind)}");
            }

            // Show player actions during betting rounds
            if (IsBettingStep(game.HandStep) && state.Players != null)
            {
                foreach (var p in state.Players)
                {
                    if (!string.IsNullOrEmpty(p.Action))
                    {
                        string actionText = FormatAction(p);
                        if (!string.IsNullOrEmpty(actionText))
                            AddLine($"  {actionText}");
                    }
                }
            }

            // Show winners
            if (game.HandStep == 14 && state.Players != null)
            {
                foreach (var p in state.Players)
                {
                    if (p.IsWinner)
                        AddLine($"  {ColoredName(p)} wins <color=#FFD700>{MoneyFormatter.FormatGain(p.Winnings)}</color>");
                }
            }

            if (game.HandStep == 15)
                AddLine("────────────────");

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
