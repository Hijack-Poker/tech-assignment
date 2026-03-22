# Option D: Unity Game Client

## How to Run

1. **Start the backend**
   ```bash
   cp .env.example .env
   docker compose --profile engine up -d
   ```
   Wait until `docker compose ps` shows all containers healthy (~2 min first time).

2. **Open Unity project** — open Unity Hub, click Open, select the `PokerClient/` folder.

3. **Press Play** — open `Assets/Scenes/HomeScene.unity`, hit Play. Enter a name, pick an avatar, choose a table, click Play.

The game connects to `localhost:3030` automatically.

## Architecture

**UI System: uGUI** — chose Canvas-based UI over UI Toolkit for its maturity and community support.

**Pattern: Event-driven MVP** — `GameManager` orchestrates API calls, `TableStateManager` broadcasts state via `OnTableStateChanged`, all views subscribe and redraw independently.

**Async: UnityWebRequest + TaskCompletionSource** — wraps Unity's coroutine-based web requests in async/await. No extra packages needed.

**JSON: Newtonsoft JSON** (`com.unity.nuget.newtonsoft-json`) — handles nested arrays and nullable types that `JsonUtility` can't.

**Animation: DOTween** — card deal arcs, chip fly, shuffle riffle, pot/stack tweening, winner glow.

**State: Full redraw** — every state change triggers a complete UI refresh. Simple, correct, no delta-patching bugs.

```
GameManager.AdvanceStepAsync()
  -> PokerApiClient.ProcessStepAsync()
  -> PokerApiClient.GetTableStateAsync()
  -> TableStateManager.SetState()
  -> OnTableStateChanged fires
  -> TableView, SeatView, HudView, HandHistoryView, ShowdownView all redraw
```

## Project Structure

```
PokerClient/Assets/
├── Scripts/
│   ├── Api/           PokerApiClient.cs, WebSocketClient.cs
│   ├── Models/        GameState.cs, PlayerState.cs, TableResponse.cs
│   ├── Managers/      GameManager.cs, TableStateManager.cs
│   ├── UI/            TableView, SeatView, CardView, CommunityCardsView,
│   │                  HudView, ControlsView, HandHistoryView, ShowdownView,
│   │                  HomeScreenView, ChipStackView, StatusBarView
│   └── Utils/         CardUtils.cs, MoneyFormatter.cs
├── Scenes/            HomeScene.unity, PokerTable.unity
├── Resources/         Avatars/, Cards/, Audio/, Sprites/Chips/
└── Tests/EditMode/    ApiClientTests.cs, GameStateTests.cs, CardUtilsTests.cs
```

## Tests

**39 tests** — run in Unity Editor: Window > General > Test Runner > EditMode > Run All.

- **ApiClientTests** (6) — Health, Process, Table response deserialization, error cases
- **GameStateTests** (16) — Showdown detection, hand completion, player status, winner identification, JSON parsing
- **CardUtilsTests** (17) — Card parsing, suit colors, display strings, money formatting, step labels

## Features Implemented

**Must Have:** 6-seat table, API connection, step-by-step hand playback, card rendering with showdown reveal, player info per seat, pot display, D/SB/BB badges, winner highlighting with hand rank + payout, Next Step button, phase labels, unit tests, Docker backend.

**Should Have:** Auto-play with 4 speeds and 3 styles (Safe/Small/Hard), card reveal animation, stack/pot tweening, phase label animation, seamless multi-hand play, hand history log, error handling with status bar, hand number display.

**Could Have:** Card sprites, chip stack visualization, 7 sound effects, responsive layout, 50+ player avatars, card deal animation, shuffle animation, WebSocket client, fresh restart button, configurable table ID.

**Beyond Scope:** Home screen with avatar selection, betting UI (Fold/Call/Raise/All-In/2X/3X/Custom), tip dealer button with animation, turn timer with warning sound.
