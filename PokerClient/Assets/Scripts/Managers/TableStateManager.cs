using System;
using UnityEngine;
using HijackPoker.Models;

namespace HijackPoker.Managers
{
    public class TableStateManager : MonoBehaviour
    {
        public TableResponse CurrentState { get; private set; }

        public event Action<TableResponse> OnTableStateChanged;
        public event Action<string> OnConnectionStatusChanged;
        public event Action OnTableReset;

        private static readonly string[] StepLabels = {
            "Preparing Hand",
            "Setting Up Dealer",
            "Posting Small Blind",
            "Posting Big Blind",
            "Dealing Hole Cards",
            "Pre-Flop Betting",
            "Dealing Flop",
            "Flop Betting",
            "Dealing Turn",
            "Turn Betting",
            "Dealing River",
            "River Betting",
            "Showdown",
            "Evaluating Hands",
            "Paying Winners",
            "Hand Complete"
        };

        public void SetState(TableResponse state)
        {
            CurrentState = state;
            OnTableStateChanged?.Invoke(state);
        }

        public string GetStepLabel(int step)
        {
            if (step >= 0 && step < StepLabels.Length)
                return StepLabels[step];
            return $"Step {step}";
        }

        public void NotifyTableReset()
        {
            OnTableReset?.Invoke();
        }

        public void NotifyConnectionStatus(string status)
        {
            OnConnectionStatusChanged?.Invoke(status);
        }
    }
}
