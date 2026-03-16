using UnityEngine;
using HijackPoker.Managers;
using HijackPoker.Models;

namespace HijackPoker.UI
{
    public class TableView : MonoBehaviour
    {
        [SerializeField] private TableStateManager _stateManager;
        [SerializeField] private SeatView[] _seatViews;
        [SerializeField] private CommunityCardsView _communityCardsView;

        private void OnEnable() => _stateManager.OnTableStateChanged += OnStateChanged;
        private void OnDisable() => _stateManager.OnTableStateChanged -= OnStateChanged;

        private void OnStateChanged(TableResponse state)
        {
            foreach (var seat in _seatViews)
                seat.Clear();

            foreach (var player in state.Players)
            {
                int idx = player.Seat - 1;
                if (idx >= 0 && idx < _seatViews.Length)
                    _seatViews[idx].Render(player, state.Game);
            }

            _communityCardsView.Refresh(state.Game.CommunityCards);
        }
    }
}
