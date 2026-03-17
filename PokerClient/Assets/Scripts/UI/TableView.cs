using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HijackPoker.Managers;
using HijackPoker.Models;

namespace HijackPoker.UI
{
    public class TableView : MonoBehaviour
    {
        [SerializeField] private TableStateManager _stateManager;
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private SeatView[] _seatViews;
        [SerializeField] private CommunityCardsView _communityCardsView;

        private Sprite[] _allAvatars;
        private Dictionary<int, Sprite> _seatAvatars;
        private bool _avatarsAssigned;

        private void Awake()
        {
            _allAvatars = Resources.LoadAll<Sprite>("Avatars");
            _seatAvatars = new Dictionary<int, Sprite>();
        }

        private void OnEnable() => _stateManager.OnTableStateChanged += OnStateChanged;
        private void OnDisable() => _stateManager.OnTableStateChanged -= OnStateChanged;

        private void AssignAvatars(List<PlayerState> players)
        {
            if (_avatarsAssigned || _allAvatars == null || _allAvatars.Length == 0) return;
            _avatarsAssigned = true;

            // Player's chosen avatar (stored in PlayerPrefs)
            string chosenAvatar = PlayerPrefs.GetString("PlayerAvatar", "");
            Sprite playerSprite = null;
            if (!string.IsNullOrEmpty(chosenAvatar))
                playerSprite = _allAvatars.FirstOrDefault(s => s.name == chosenAvatar);

            // Shuffle remaining avatars for bots
            var pool = _allAvatars.Where(s => s != playerSprite).ToList();
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            int poolIdx = 0;
            foreach (var player in players)
            {
                if (player.Seat == 1 && playerSprite != null)
                    _seatAvatars[1] = playerSprite;
                else if (poolIdx < pool.Count)
                    _seatAvatars[player.Seat] = pool[poolIdx++];
            }
        }

        private void OnStateChanged(TableResponse state)
        {
            string localName = _gameManager != null ? _gameManager.PlayerName : null;

            AssignAvatars(state.Players);

            foreach (var seat in _seatViews)
                seat.Clear();

            foreach (var player in state.Players)
            {
                int idx = player.Seat - 1;
                if (idx >= 0 && idx < _seatViews.Length)
                {
                    if (_seatAvatars.TryGetValue(player.Seat, out var avatar))
                        _seatViews[idx].SetAvatar(avatar);
                    _seatViews[idx].Render(player, state.Game, localName);
                }
            }

            _communityCardsView.Refresh(state.Game.CommunityCards);
        }
    }
}
