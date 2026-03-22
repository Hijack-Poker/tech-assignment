# Hijack Poker — Unity Game Client

A 6-seat poker table client built in Unity that connects to the holdem-processor REST API.

## Prerequisites

- [Unity Hub](https://unity.com/download) with Unity **2022.3+ LTS** (or Unity 6)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) with Docker Compose v2

## Setup

### 1. Start the backend

From the repo root (`tech-assignment/`):

```bash
cp .env.example .env
docker compose --profile engine up -d
```

Wait ~2 minutes for first boot. Verify:

```bash
curl http://localhost:3030/health
```

### 2. Open the Unity project

1. Open **Unity Hub**
2. Click **Open**
3. Select this `PokerClient/` folder
4. If prompted, install the required Unity Editor version

### 3. Press Play

1. Open **Assets/Scenes/HomeScene.unity**
2. Hit **Play**
3. Enter your name, pick an avatar, choose a table, click Play

The game connects to `http://localhost:3030` automatically.

### Stopping

```bash
docker compose --profile engine down        # stop
docker compose --profile engine down -v     # stop + wipe database
```

---

## Architecture

| Decision | Choice | Why |
|----------|--------|-----|
| UI system | uGUI (Canvas) | Mature, battle-tested, more community resources than UI Toolkit |
| Async | UnityWebRequest + TaskCompletionSource | Built-in async/await, no extra packages |
| JSON | Newtonsoft JSON | Handles nested arrays and nullables (JsonUtility can't) |
| Animation | DOTween | Industry-standard Unity tweening |
| Pattern | Event-driven MVP | Decoupled views, testable managers |
| State updates | Full redraw per state change | Simple, correct, no delta-patching bugs |
| Tests | NUnit Edit Mode | Fast execution, no Play Mode dependency |

### Data Flow

```
GameManager.AdvanceStepAsync()
  -> POST /process (advance one step)
  -> GET /table/1 (fetch new state)
  -> TableStateManager.SetState()
  -> OnTableStateChanged event fires
  -> TableView, SeatView, HudView, HandHistoryView, ShowdownView redraw
```

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── Api/
│   │   ├── PokerApiClient.cs          REST client (async/await)
│   │   └── WebSocketClient.cs         Real-time updates (graceful fallback)
│   ├── Models/
│   │   ├── GameState.cs               Game state + Winner
│   │   ├── PlayerState.cs             Player state + status helpers
│   │   └── TableResponse.cs           API response wrapper
│   ├── Managers/
│   │   ├── GameManager.cs             Game flow, auto-play, restart
│   │   └── TableStateManager.cs       State broadcasting, step labels
│   ├── UI/
│   │   ├── TableView.cs               Table rendering, deal/shuffle/chip animations
│   │   ├── SeatView.cs                Per-seat: cards, stack, bet, badges
│   │   ├── CardView.cs                Single card display
│   │   ├── CommunityCardsView.cs      5 community card slots
│   │   ├── HudView.cs                 Phase label, hand #, pot, restart, exit
│   │   ├── ControlsView.cs            Next Step, Auto Play, Speed, Betting actions
│   │   ├── HandHistoryView.cs         Scrollable action log
│   │   ├── ShowdownView.cs            Showdown hand display
│   │   ├── HomeScreenView.cs          Name input, avatar picker, table selector
│   │   ├── ChipStackView.cs           Chip denomination columns
│   │   └── StatusBarView.cs           Connection status
│   └── Utils/
│       ├── CardUtils.cs               Card parsing ("AH" -> A of Hearts)
│       └── MoneyFormatter.cs           "$150.00" formatting
├── Scenes/
│   ├── HomeScene.unity
│   └── PokerTable.unity
├── Resources/
│   ├── Avatars/                       50+ player avatar sprites
│   ├── Cards/                         Card face sprites
│   ├── Audio/                         Sound effects (7 clips)
│   └── Sprites/Chips/                 Chip denomination sprites
└── Tests/EditMode/
    ├── ApiClientTests.cs              6 tests
    ├── GameStateTests.cs              16 tests
    └── CardUtilsTests.cs             17 tests
```

---

## Tests

**39 unit tests** in Edit Mode (NUnit). Run via: **Window > General > Test Runner > EditMode > Run All**

| Suite | Tests | Covers |
|-------|-------|--------|
| ApiClientTests | 6 | Response deserialization, error handling |
| GameStateTests | 16 | Showdown detection, winner identification, player status, JSON parsing |
| CardUtilsTests | 17 | Card parsing, suit colors, display strings, money formatting |

---

## What's Implemented

### Must Have
- 6-seat poker table with felt surface
- API client connecting to `localhost:3030`
- Step-by-step hand playback via POST /process
- Community cards appearing incrementally (flop/turn/river)
- Hole cards face-down, revealed at showdown (step 12+)
- Player name, stack, bet, action, D/SB/BB badges per seat
- Pot display, winner highlighting with hand rank + payout
- Next Step button, phase labels, hand number
- Unit tests on API client, models, and game state logic

### Should Have
- Auto-play with 4 speeds (0.25s, 0.5s, 1s, 2s) and 3 styles (Safe, Small Random, Hard)
- Card reveal animation at showdown
- Stack and pot tweening (DOTween)
- Phase label punch animation on step change
- Seamless multi-hand play
- Hand history log with color-coded actions
- Error handling with connection status bar
- Hand number display

### Could Have
- Card sprites (full deck)
- Chip stack visualization (denomination columns)
- 7 sound effects (shuffle, chips, fold, turn start, time warning, cheers, win)
- Responsive layout (CanvasScaler 1920x1080, 0.5 match)
- 50+ player avatars
- Card deal animation (arc from dealer)
- Shuffle animation (grow-in + 3x riffle)
- WebSocket client for real-time updates
- Fresh restart button (resets all player stacks)
- Table selector (Starter $1/$2, High Stakes $5/$10)

### Beyond Requirements
- Home screen with name input + avatar selection
- Full betting UI (Fold, Call, Raise, All-In, 2X, 3X, custom amount)
- Tip dealer button with chip fly animation
- Turn timer with low-time warning sound
- Winner seat gold pulse animation
