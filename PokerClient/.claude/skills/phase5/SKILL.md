---
name: phase5
description: Build ControlsView (Next Step, Auto-play, speed), HandHistoryView (scrollable log), and StatusBarView (error feedback). Makes the app fully playable end-to-end.
---

# Phase 5: Controls + Hand History + Error Handling

Read CLAUDE.md before starting. Phase 4 must be complete.

## Goal
Fully playable: user can step through hands, enable auto-play at configurable speed, see a scrollable action log, and see connection/error status. FR-2, FR-6, and error handling done.

---

## File 1: `Assets/Scripts/UI/ControlsView.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.Managers;

namespace HijackPoker.UI
{
    public class ControlsView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager _gameManager;

        [Header("Buttons")]
        [SerializeField] private Button _nextStepButton;
        [SerializeField] private Button _autoPlayButton;
        [SerializeField] private TextMeshProUGUI _autoPlayButtonText;
        [SerializeField] private Button[] _speedButtons; // 0.25, 0.5, 1, 2
        [SerializeField] private Image[] _speedButtonImages;

        private static readonly Color SelectedSpeedColor  = new Color(0.2f, 0.6f, 1f);
        private static readonly Color DefaultSpeedColor   = new Color(0.25f, 0.25f, 0.35f);
        private static readonly Color AutoPlayActiveColor = new Color(0.9f, 0.3f, 0.3f);
        private static readonly Color AutoPlayIdleColor   = new Color(0.2f, 0.6f, 0.2f);

        private int _selectedSpeedIndex = 2; // default: 1s (index 2 of [0.25, 0.5, 1, 2])

        private void Awake()
        {
            _nextStepButton.onClick.AddListener(OnNextStepClicked);
            _autoPlayButton.onClick.AddListener(OnAutoPlayClicked);

            for (int i = 0; i < _speedButtons.Length; i++)
            {
                int idx = i; // capture for closure
                _speedButtons[i].onClick.AddListener(() => OnSpeedSelected(idx));
            }

            RefreshSpeedButtons();
        }

        private void Update()
        {
            // Disable Next Step while processing or auto-playing
            _nextStepButton.interactable = !_gameManager.IsAutoPlaying;

            // Update auto-play button visual
            var img = _autoPlayButton.GetComponent<Image>();
            if (img != null)
                img.color = _gameManager.IsAutoPlaying ? AutoPlayActiveColor : AutoPlayIdleColor;
            _autoPlayButtonText.text = _gameManager.IsAutoPlaying ? "⏸ Stop" : "▶ Auto Play";
        }

        private void OnNextStepClicked()
        {
            _ = _gameManager.AdvanceStepAsync();
        }

        private void OnAutoPlayClicked()
        {
            _gameManager.ToggleAutoPlay();
        }

        private void OnSpeedSelected(int index)
        {
            _selectedSpeedIndex = index;
            _gameManager.SetAutoPlaySpeed(GameManager.SpeedOptions[index]);
            RefreshSpeedButtons();
        }

        private void RefreshSpeedButtons()
        {
            for (int i = 0; i < _speedButtonImages.Length; i++)
                _speedButtonImages[i].color = i == _selectedSpeedIndex ? SelectedSpeedColor : DefaultSpeedColor;
        }
    }
}
```

---

## File 2: `Assets/Scripts/UI/HandHistoryView.cs`

```csharp
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
        [SerializeField] private Transform _content;        // VerticalLayoutGroup parent
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private GameObject _entryPrefab;  // prefab with TMP text

        private const int MaxEntries = 200;
        private readonly List<GameObject> _entries = new List<GameObject>();

        private void OnEnable() => _stateManager.OnTableStateChanged += OnStateChanged;
        private void OnDisable() => _stateManager.OnTableStateChanged -= OnStateChanged;

        private void OnStateChanged(TableResponse state)
        {
            var game = state.Game;
            string label = _stateManager.GetStepLabel(game.HandStep);
            string line = $"<color=#888>Hand #{game.GameNo}</color> — {label}";

            // Add winner info at PAY_WINNERS step
            if (game.HandStep == 14 && state.Players != null)
            {
                foreach (var p in state.Players)
                {
                    if (p.IsWinner)
                        line += $"\n  <color=#FFD700>★ {p.Username} wins {MoneyFormatter.FormatGain(p.Winnings)}</color>";
                }
            }

            AddEntry(line);

            // Separator after hand complete
            if (game.HandStep == 15)
                AddEntry("──────────────────");
        }

        private void AddEntry(string text)
        {
            // Trim old entries
            while (_entries.Count >= MaxEntries)
            {
                Destroy(_entries[0]);
                _entries.RemoveAt(0);
            }

            var go = Instantiate(_entryPrefab, _content);
            go.GetComponentInChildren<TextMeshProUGUI>().text = text;
            _entries.Add(go);

            // Scroll to bottom next frame
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
```

---

## File 3: `Assets/Scripts/UI/StatusBarView.cs`

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.Managers;

namespace HijackPoker.UI
{
    public class StatusBarView : MonoBehaviour
    {
        [SerializeField] private TableStateManager _stateManager;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private Image _background;

        private static readonly Color ConnectedColor   = new Color(0.1f, 0.5f, 0.1f);
        private static readonly Color ConnectingColor  = new Color(0.5f, 0.4f, 0.0f);
        private static readonly Color ErrorColor       = new Color(0.5f, 0.1f, 0.1f);

        private Coroutine _clearErrorCoroutine;

        private void OnEnable() => _stateManager.OnConnectionStatusChanged += OnStatusChanged;
        private void OnDisable() => _stateManager.OnConnectionStatusChanged -= OnStatusChanged;

        private void OnStatusChanged(string status)
        {
            if (_clearErrorCoroutine != null)
            {
                StopCoroutine(_clearErrorCoroutine);
                _clearErrorCoroutine = null;
            }

            _statusText.text = status;

            if (status == "Connected")
            {
                _background.color = ConnectedColor;
            }
            else if (status == "Connecting...")
            {
                _background.color = ConnectingColor;
            }
            else // Error
            {
                _background.color = ErrorColor;
                _clearErrorCoroutine = StartCoroutine(ClearErrorAfterDelay(5f));
            }
        }

        private IEnumerator ClearErrorAfterDelay(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _statusText.text = "Connected";
            _background.color = ConnectedColor;
        }
    }
}
```

---

## HandHistory Entry Prefab

Create a prefab `Assets/Resources/HistoryEntryPrefab.prefab`:
- Root: empty GameObject
- Add: `LayoutElement` (minHeight=30, preferredHeight=40)
- Child: TextMeshProUGUI
  - Font size: 13
  - Text wrapping: enabled
  - Color: white
  - Rich text: enabled (for `<color>` tags)

---

## Wire Up in Scene (via MCP)

1. Add `ControlsView` to `ControlsPanel` GameObject, wire all button references and GameManager
2. Add `HandHistoryView` to `HistoryPanel`, wire ScrollRect, content Transform, stateManager, prefab
3. Add `StatusBarView` to `StatusBar`, wire text + background image + stateManager

---

## Acceptance Criteria
- [ ] Clicking Next Step advances hand by exactly 1 step
- [ ] Double-clicking Next Step rapidly: only 1 advance (isProcessing guard)
- [ ] Auto-play runs at selected speed (test 0.25s, 1s, 2s)
- [ ] Auto-play button changes to "⏸ Stop" when active
- [ ] Auto-play stops when clicked again
- [ ] Next Step button disabled while auto-playing
- [ ] Speed selection highlights selected button
- [ ] History appends new entry on every state change
- [ ] History scrolls to bottom on each new entry
- [ ] Winner highlighted in gold in history at step 14
- [ ] Separator line added at step 15
- [ ] StatusBar shows red "Error:" when API unreachable
- [ ] StatusBar shows green "Connected" after successful call
- [ ] Error message auto-clears after 5 seconds

## Next Phase
→ `/phase6`
