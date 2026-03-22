# Hijack Poker — Unity Game Client (Option D)

A poker table client built in Unity that connects to the holdem-processor REST API at `localhost:3030`.

## How to Run

### 1. Start the backend

Follow the [main README](../README.md) to start Docker:

```bash
docker compose --profile engine up -d
```

### 2. Open in Unity

1. Open **Unity Hub**
2. Click **Open** > select the `PokerClient/` folder
3. If prompted, install the matching Unity Editor version (2022.3+ LTS)

### 3. Play

1. Open `Assets/Scenes/HomeScene.unity`
2. Press **Play**
3. Enter a name, pick an avatar, choose a table, click **Play**

That's it. The game connects to `localhost:3030` automatically.

---

## Architecture Decisions

| Concern | Decision | Why |
|---------|----------|-----|
| UI System | **uGUI** (Canvas-based) | Battle-tested, more community support than UI Toolkit |
| Async | **UnityWebRequest + TaskCompletionSource** | Built-in, no extra packages |
| JSON | **Newtonsoft JSON** | Handles nested arrays + nullables (JsonUtility can't) |
| Text | **TextMeshPro** | Required for quality text rendering |
| Animation | **DOTween** | Industry-standard tweening |
| Pattern | **Event-driven MVP** | Decoupled views, testable managers |
| State | **Full redraw on every state change** | Simple and correct, no delta patching bugs |
| Tests | **NUnit Edit Mode** | Fast, no Play Mode dependency |

### Event Flow

```
GameManager.AdvanceStepAsync()
  -> PokerApiClient.ProcessStepAsync(tableId)
  -> PokerApiClient.GetTableStateAsync(tableId)
  -> TableStateManager.SetState(TableResponse)
  -> OnTableStateChanged event fires
  -> All views redraw
```

---

## Project Structure

```
PokerClient/Assets/
├── Scripts/
│   ├── Api/
│   │   ├── PokerApiClient.cs        # REST client
│   │   └── WebSocketClient.cs       # Real-time updates (graceful fallback)
│   ├── Models/
│   │   ├── GameState.cs             # Game data + Winner class
│   │   ├── PlayerState.cs           # Player data + status helpers
│   │   └── TableResponse.cs        # API response wrapper
│   ├── Managers/
│   │   ├── GameManager.cs           # Game flow, auto-play, restart
│   │   └── TableStateManager.cs    # State events + step labels
│   ├── UI/
│   │   ├── TableView.cs            # Table, animations, tip button
│   │   ├── SeatView.cs             # Per-seat (cards, stack, bet, badges)
│   │   ├── CardView.cs             # Single card rendering
│   │   ├── CommunityCardsView.cs   # 5 community card slots
│   │   ├── HudView.cs              # Phase label, hand #, pot, exit/restart
│   │   ├── ControlsView.cs         # Next Step, Auto Play, Speed, Actions
│   │   ├── HandHistoryView.cs      # Scrollable action log
│   │   ├── ShowdownView.cs         # Showdown cards + hand ranks
│   │   ├── HomeScreenView.cs       # Name, avatar, table selection
│   │   ├── ChipStackView.cs        # Chip denomination columns
│   │   └── StatusBarView.cs        # Connection status
│   └── Utils/
│       ├── CardUtils.cs            # "AH" -> rank + suit
│       └── MoneyFormatter.cs       # float -> "$150.00"
├── Scenes/
│   ├── HomeScene.unity
│   └── PokerTable.unity
├── Resources/
│   ├── Avatars/                    # 50+ player avatars
│   ├── Cards/                      # Card face sprites
│   ├── Audio/                      # Sound effects
│   └── Sprites/Chips/              # Chip sprites
└── Tests/EditMode/
    ├── ApiClientTests.cs           # 6 tests — API deserialization
    ├── GameStateTests.cs           # 16 tests — state logic, models
    └── CardUtilsTests.cs           # 17 tests — parsing, formatting
```

---

## Features

**Core:** 6-seat table, community cards, pot, stacks, bets, actions, D/SB/BB badges, winner highlighting, hand history

**Controls:** Next Step, Auto Play (Safe/Small/Hard), 4 speed options, Fold/Call/Raise/All-In, 2X/3X/Custom bet, Restart, Tip $1

**Animations:** Card deal arc, shuffle riffle, chip fly, pot/stack tweening, winner glow, phase punch scale

**Sound:** Shuffle, chips, fold, turn start, time warning, crowd cheers

**Bonus:** Home screen with avatars, table selector (Starter/High Stakes), chip stack visualization, WebSocket client, responsive layout

---

## Running Tests

In Unity Editor: **Window > General > Test Runner > EditMode > Run All**

39 tests covering API deserialization, game state logic, card parsing, and money formatting.
