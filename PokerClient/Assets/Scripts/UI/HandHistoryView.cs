using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    public class HandHistoryView : MonoBehaviour
    {
        [SerializeField] private TableStateManager _stateManager;
        [SerializeField] private Transform _content;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private GameObject _entryPrefab;

        private const int MaxEntries = 200;
        private readonly List<GameObject> _entries = new List<GameObject>();

        private void OnEnable() => _stateManager.OnTableStateChanged += OnStateChanged;
        private void OnDisable() => _stateManager.OnTableStateChanged -= OnStateChanged;

        private void OnStateChanged(TableResponse state)
        {
            var game = state.Game;
            string label = _stateManager.GetStepLabel(game.HandStep);
            string line = $"<color=#888>Hand #{game.GameNo}</color> — {label}";

            if (game.HandStep == 14 && state.Players != null)
            {
                foreach (var p in state.Players)
                {
                    if (p.IsWinner)
                        line += $"\n  <color=#FFD700>{p.Username} wins {MoneyFormatter.FormatGain(p.Winnings)}</color>";
                }
            }

            AddEntry(line);

            if (game.HandStep == 15)
                AddEntry("──────────────────");
        }

        private void AddEntry(string text)
        {
            while (_entries.Count >= MaxEntries)
            {
                Destroy(_entries[0]);
                _entries.RemoveAt(0);
            }

            var go = Instantiate(_entryPrefab, _content);
            go.GetComponentInChildren<TextMeshProUGUI>().text = text;
            _entries.Add(go);

            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
