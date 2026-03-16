---
name: phase1
description: Implement the API layer (PokerApiClient), complete data models, define HandHistoryEntry, and write all unit tests. Pure C# — no Unity Editor interaction needed.
---

# Phase 1: API Layer + Data Models + Unit Tests

Read CLAUDE.md before starting. All decisions are locked — do not re-evaluate architecture.

## Goal
A fully tested, compilable API layer. After this phase, all unit tests pass and the data pipeline is proven correct before any UI is built.

## Context
- Namespace: `HijackPoker`
- Backend: `http://localhost:3030`
- JSON library: Newtonsoft JSON (`[JsonProperty]` attributes)
- Async pattern: `UnityWebRequest` + `TaskCompletionSource` (already in skeleton)

## Files to Create/Modify

### MODIFY: `Assets/Scripts/Api/PokerApiClient.cs`
The skeleton has `SendGetRequest<T>` and `SendPostRequest<T>` helpers already implemented.
Fill in the 3 TODO methods:

```csharp
public async Task<HealthResponse> GetHealthAsync()
{
    return await SendGetRequest<HealthResponse>("/health");
}

public async Task<ProcessResponse> ProcessStepAsync(int tableId)
{
    var body = new { tableId = tableId };
    return await SendPostRequest<ProcessResponse>("/process", body);
}

public async Task<TableResponse> GetTableStateAsync(int tableId)
{
    return await SendGetRequest<TableResponse>($"/table/{tableId}");
}
```

Also add:
- `public bool IsProcessing { get; private set; }` property
- Startup health check method: `public async Task<bool> CheckConnectionAsync(int maxRetries = 3)`
  - Retries up to `maxRetries` times with 2s delay between
  - Returns true if health check succeeds, false if all retries fail
- On any `SendGetRequest`/`SendPostRequest` failure: return `null` (do NOT throw)

### CREATE: `Assets/Scripts/Models/TableResponse.cs`
Extract `TableResponse` class from `GameState.cs` into its own file:
```csharp
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HijackPoker.Models
{
    public class TableResponse
    {
        [JsonProperty("game")]
        public GameState Game;

        [JsonProperty("players")]
        public List<PlayerState> Players;
    }
}
```
Then remove `TableResponse` from `GameState.cs`.

### KEEP: `Assets/Scripts/Models/GameState.cs`
Already complete. Do not change unless `TableResponse` extraction requires it.

### KEEP: `Assets/Scripts/Models/PlayerState.cs`
Already complete. Do not change.

### CREATE: `Assets/Scripts/Models/HandHistoryEntry.cs`
```csharp
using System;
using System.Collections.Generic;

namespace HijackPoker.Models
{
    public class HandHistoryEntry
    {
        public int HandNo;
        public int Step;
        public string StepLabel;
        public string[] WinnerNames;
        public float PotSize;
        public Dictionary<string, float> StackDeltas; // username → delta
        public DateTime Timestamp;
    }
}
```

### CREATE: `Assets/Tests/EditMode/GameStateTests.cs`
Test asmdef must reference `HijackPoker` assembly. Tests use NUnit.

Cover:
- `IsShowdown` true when `HandStep >= 12`, false when `HandStep == 11`
- `IsHandComplete` when `StepName == "RECORD_STATS_AND_NEW_HAND"`
- `communityCards` parses from JSON array `["JH","7D","2C"]`
- `sidePots` parses correctly, including empty `[]`
- `PlayerState.IsFolded` when `status == "11"`
- `PlayerState.IsAllIn` when `status == "12"`
- `PlayerState.IsWinner` when `winnings > 0`
- `PlayerState.HasCards` when `cards` array has 2 entries
- Deserialize full `/table/1` sample JSON → verify all fields

Sample JSON to use in tests:
```json
{
  "game": {
    "id": 1, "tableId": 1, "tableName": "Starter Table",
    "gameNo": 3, "handStep": 6, "stepName": "DEAL_FLOP",
    "dealerSeat": 2, "smallBlindSeat": 3, "bigBlindSeat": 4,
    "communityCards": ["JH", "7D", "2C"],
    "pot": 3.00, "sidePots": [], "move": 0, "status": "in_progress",
    "smallBlind": 1.00, "bigBlind": 2.00, "maxSeats": 6,
    "currentBet": 0, "winners": []
  },
  "players": [
    {
      "playerId": 1, "username": "Alice", "seat": 1,
      "stack": 150.00, "bet": 0, "totalBet": 0,
      "status": "1", "action": "", "cards": ["AH", "KD"],
      "handRank": "", "winnings": 0
    }
  ]
}
```

### CREATE: `Assets/Tests/EditMode/ApiClientTests.cs`
Test deserialization of all 3 API response types using sample JSON strings (no network calls in tests):

- Deserialize `/health` response → `service == "holdem-processor"`, `status == "ok"`
- Deserialize `/process` response → `success == true`, `result.step == 6`, `result.stepName == "DEAL_FLOP"`
- Deserialize `/table/1` response → `game.HandStep == 6`, `players[0].Username == "Alice"`
- Null-safety: empty `communityCards` array → `List<string>` with 0 items, not null
- Null-safety: empty `sidePots` → `List<SidePot>` with 0 items, not null

### CREATE: `Assets/Tests/EditMode/CardUtilsTests.cs`
Placeholder file only — CardUtils is built in Phase 2. Create with one passing test:
```csharp
[Test]
public void Placeholder_CardUtils_Phase2() => Assert.IsTrue(true);
```

## Test Assembly Definition
Create `Assets/Tests/EditMode/HijackPoker.Tests.asmdef`:
```json
{
    "name": "HijackPoker.Tests",
    "references": ["HijackPoker"],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "Newtonsoft.Json.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

Also create or verify `Assets/Scripts/HijackPoker.asmdef` exists:
```json
{
    "name": "HijackPoker",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "Newtonsoft.Json.dll"
    ],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

## Acceptance Criteria
- [ ] All unit tests pass in Unity Test Runner (Window → General → Test Runner → EditMode)
- [ ] Zero compilation errors
- [ ] `TableResponse` is in its own file
- [ ] `HandHistoryEntry` exists with all fields
- [ ] `PokerApiClient.IsProcessing` property exists
- [ ] `PokerApiClient.CheckConnectionAsync()` retries 3 times

## Next Phase
After all tests pass → `/phase2`
