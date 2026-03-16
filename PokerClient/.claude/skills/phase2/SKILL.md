---
name: phase2
description: Build TableStateManager, GameManager, CardUtils, and MoneyFormatter. The complete state pipeline before any UI. Pure C# — no Unity Editor interaction needed.
---

# Phase 2: Core Managers + Utilities

Read CLAUDE.md before starting. Phase 1 must be complete (all tests passing).

## Goal
Full data pipeline from API → state → event, plus utility helpers. After this phase, clicking "Next Step" in code will fire `OnTableStateChanged` with new state — even before any UI exists.

## Files to Create

### CREATE: `Assets/Scripts/Managers/TableStateManager.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using HijackPoker.Models;

namespace HijackPoker.Managers
{
    public class TableStateManager : MonoBehaviour
    {
        public TableResponse CurrentState { get; private set; }

        public event Action<TableResponse> OnTableStateChanged;
        public event Action<string> OnConnectionStatusChanged; // "Connected", "Connecting...", "Error: msg"

        private static readonly string[] StepLabels = {
            "Preparing Hand",       // 0
            "Setting Up Dealer",    // 1
            "Posting Small Blind",  // 2
            "Posting Big Blind",    // 3
            "Dealing Hole Cards",   // 4
            "Pre-Flop Betting",     // 5
            "Dealing Flop",         // 6
            "Flop Betting",         // 7
            "Dealing Turn",         // 8
            "Turn Betting",         // 9
            "Dealing River",        // 10
            "River Betting",        // 11
            "Showdown",             // 12
            "Evaluating Hands",     // 13
            "Paying Winners",       // 14
            "Hand Complete"         // 15
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

        public void NotifyConnectionStatus(string status)
        {
            OnConnectionStatusChanged?.Invoke(status);
        }
    }
}
```

### CREATE: `Assets/Scripts/Managers/GameManager.cs`

```csharp
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using HijackPoker.Api;
using HijackPoker.Managers;

namespace HijackPoker.Managers
{
    public class GameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PokerApiClient _apiClient;
        [SerializeField] private TableStateManager _stateManager;

        [Header("Settings")]
        [SerializeField] private int _tableId = 1;
        [SerializeField] private float _autoPlaySpeed = 1f;

        public bool IsAutoPlaying { get; private set; }

        private bool _isProcessing;
        private Coroutine _autoPlayCoroutine;

        public static readonly float[] SpeedOptions = { 0.25f, 0.5f, 1f, 2f };

        private async void Start()
        {
            _stateManager.NotifyConnectionStatus("Connecting...");
            bool connected = await _apiClient.CheckConnectionAsync(maxRetries: 3);

            if (connected)
            {
                _stateManager.NotifyConnectionStatus("Connected");
                // Load initial state
                var state = await _apiClient.GetTableStateAsync(_tableId);
                if (state != null) _stateManager.SetState(state);
            }
            else
            {
                _stateManager.NotifyConnectionStatus("Error: Cannot reach holdem-processor at localhost:3030");
            }
        }

        public async Task AdvanceStepAsync()
        {
            if (_isProcessing) return;
            _isProcessing = true;

            try
            {
                var processResult = await _apiClient.ProcessStepAsync(_tableId);
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

        public void ToggleAutoPlay()
        {
            if (IsAutoPlaying) StopAutoPlay();
            else StartAutoPlay();
        }

        public void SetAutoPlaySpeed(float intervalSeconds)
        {
            _autoPlaySpeed = intervalSeconds;
        }

        private void StartAutoPlay()
        {
            IsAutoPlaying = true;
            _autoPlayCoroutine = StartCoroutine(AutoPlayCoroutine());
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
                if (!_isProcessing)
                {
                    // Fire and don't await (coroutine can't await Task directly)
                    _ = AdvanceStepAsync();
                }
            }
        }

        private void OnDestroy()
        {
            StopAutoPlay();
        }
    }
}
```

### CREATE: `Assets/Scripts/Utils/CardUtils.cs`

```csharp
namespace HijackPoker.Utils
{
    public static class CardUtils
    {
        public static (string rank, string suit) ParseCard(string card)
        {
            if (string.IsNullOrEmpty(card) || card.Length < 2)
                return ("?", "?");
            string suit = card[^1..];
            string rank = card[..^1];
            return (rank, suit);
        }

        public static bool IsRedSuit(string suit) =>
            suit == "H" || suit == "D";

        public static string GetSuitSymbol(string suit) => suit switch
        {
            "H" => "♥",
            "D" => "♦",
            "C" => "♣",
            "S" => "♠",
            _   => suit
        };

        public static string GetDisplayString(string card)
        {
            var (rank, suit) = ParseCard(card);
            return rank + GetSuitSymbol(suit);
        }
    }
}
```

### CREATE: `Assets/Scripts/Utils/MoneyFormatter.cs`

```csharp
namespace HijackPoker.Utils
{
    public static class MoneyFormatter
    {
        public static string Format(float amount) =>
            $"${amount:F2}";

        public static string FormatGain(float amount) =>
            amount >= 0 ? $"+${amount:F2}" : $"-${System.Math.Abs(amount):F2}";
    }
}
```

## Complete CardUtilsTests.cs (replace placeholder from Phase 1)

Update `Assets/Tests/EditMode/CardUtilsTests.cs`:

```csharp
using NUnit.Framework;
using HijackPoker.Utils;

namespace HijackPoker.Tests
{
    public class CardUtilsTests
    {
        [Test] public void ParseCard_AH() { var (r, s) = CardUtils.ParseCard("AH"); Assert.AreEqual("A", r); Assert.AreEqual("H", s); }
        [Test] public void ParseCard_10D() { var (r, s) = CardUtils.ParseCard("10D"); Assert.AreEqual("10", r); Assert.AreEqual("D", s); }
        [Test] public void ParseCard_2C() { var (r, s) = CardUtils.ParseCard("2C"); Assert.AreEqual("2", r); Assert.AreEqual("C", s); }
        [Test] public void ParseCard_KS() { var (r, s) = CardUtils.ParseCard("KS"); Assert.AreEqual("K", r); Assert.AreEqual("S", s); }

        [Test] public void IsRedSuit_Hearts() => Assert.IsTrue(CardUtils.IsRedSuit("H"));
        [Test] public void IsRedSuit_Diamonds() => Assert.IsTrue(CardUtils.IsRedSuit("D"));
        [Test] public void IsBlackSuit_Clubs() => Assert.IsFalse(CardUtils.IsRedSuit("C"));
        [Test] public void IsBlackSuit_Spades() => Assert.IsFalse(CardUtils.IsRedSuit("S"));

        [Test] public void DisplayString_AH() => Assert.AreEqual("A♥", CardUtils.GetDisplayString("AH"));
        [Test] public void DisplayString_10D() => Assert.AreEqual("10♦", CardUtils.GetDisplayString("10D"));
        [Test] public void DisplayString_2C() => Assert.AreEqual("2♣", CardUtils.GetDisplayString("2C"));
        [Test] public void DisplayString_KS() => Assert.AreEqual("K♠", CardUtils.GetDisplayString("KS"));

        [Test] public void MoneyFormat_150() => Assert.AreEqual("$150.00", MoneyFormatter.Format(150f));
        [Test] public void MoneyFormat_Zero() => Assert.AreEqual("$0.00", MoneyFormatter.Format(0f));
        [Test] public void MoneyFormatGain_24() => Assert.AreEqual("+$24.00", MoneyFormatter.FormatGain(24f));
        [Test] public void MoneyFormatGain_Negative() => Assert.AreEqual("-$5.00", MoneyFormatter.FormatGain(-5f));

        [Test] public void StepLabel_6_IsDealingFlop()
        {
            var mgr = new UnityEngine.GameObject().AddComponent<HijackPoker.Managers.TableStateManager>();
            Assert.AreEqual("Dealing Flop", mgr.GetStepLabel(6));
        }
    }
}
```

## Acceptance Criteria
- [ ] All CardUtils unit tests pass
- [ ] All MoneyFormatter tests pass
- [ ] `StepLabel_6_IsDealingFlop` test passes
- [ ] `TableStateManager.OnTableStateChanged` fires when `SetState` is called
- [ ] `GameManager.AdvanceStepAsync()` has `isProcessing` guard (second call is no-op while first is running)
- [ ] Auto-play starts/stops via `ToggleAutoPlay()`
- [ ] Zero compilation errors

## Next Phase
After all tests pass → `/phase3`
