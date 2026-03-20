using System;
using HijackPoker.Models;

namespace HijackPoker.Managers
{
    /// <summary>
    /// Holds the current table state and notifies subscribers when it changes.
    /// Single source of truth for all views.
    /// </summary>
    public class TableStateManager
    {
        public event Action<TableResponse, TableResponse> OnStateChanged;

        private TableResponse _currentState;

        public TableResponse CurrentState => _currentState;

        public void UpdateState(TableResponse newState)
        {
            var oldState = _currentState;
            _currentState = newState;
            OnStateChanged?.Invoke(oldState, newState);
        }

        public void SetStateSilently(TableResponse state)
        {
            _currentState = state;
        }

        public void Clear()
        {
            var oldState = _currentState;
            _currentState = null;
            if (oldState != null)
                OnStateChanged?.Invoke(oldState, null);
        }
    }
}
