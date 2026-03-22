# Hijack Poker — Unity Game Client (Option D)

A fully-featured poker table client built in Unity that connects to the holdem-processor REST API. Renders a 6-seat poker table, steps through hands one state at a time, and displays cards, stacks, bets, community cards, winners, and hand history with animations and sound.

<!-- Screenshots: replace these paths with your own captures -->
<!-- ![Poker Table](../PokerClient/Assets/Screenshots/poker-table.png) -->
<!-- ![Home Screen](../PokerClient/Assets/Screenshots/home-screen.png) -->

---

## Prerequisites

| Tool | Version | Download |
|------|---------|----------|
| **Unity Hub** | Latest | [unity.com/download](https://unity.com/download) |
| **Unity Editor** | 2022.3+ LTS or Unity 6 | Install via Unity Hub |
| **Docker Desktop** | v4+ with Compose v2 | [docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop/) |
| **Node.js** | 22+ | [nodejs.org](https://nodejs.org/) (for running tests locally) |
| **Git** | Latest | [git-scm.com](https://git-scm.com/) |

---

## Quick Start (Step by Step)

### Step 1: Clone the repo

```bash
git clone <this-repo>
cd tech-assignment
```

### Step 2: Configure environment

```bash
cp .env.example .env
```

The defaults work out of the box. Edit `.env` only if you have port conflicts.

### Step 3: Start the backend (Docker)

```bash
docker compose --profile engine up -d
```

This starts 7 containers:
- **MySQL 8.0** — game state database (port 3306)
- **Redis 7** — caching (port 6379)
- **DynamoDB Local** — NoSQL store (port 8000)
- **ElasticMQ** — SQS mock (port 9324)
- **EventBridge Mock** — event bus (port 4010)
- **Holdem Processor** — game engine API (port 3030)
- **Cash Game Broadcast** — WebSocket broadcast (port 3032)

First run takes 2-3 minutes for npm installs. Wait until all containers are healthy:

```bash
docker compose ps
```

### Step 4: Verify the backend is running

```bash
curl http://localhost:3030/health
# Expected: {"service":"holdem-processor","status":"ok","timestamp":"..."}

curl http://localhost:3030/table/1
# Expected: JSON with game state and 6 players
```

### Step 5: Open the Unity project

1. Open **Unity Hub**
2. Click **Open** > navigate to `tech-assignment/PokerClient/`
3. Select the folder and click **Open**
4. Unity Hub will detect the project version — install the matching Editor if prompted
5. Wait for the project to import (first time takes a few minutes)

### Step 6: Play

1. In Unity Editor, open **Assets/Scenes/HomeScene.unity** (or **PokerTable.unity** directly)
2. Press the **Play** button
3. Enter your name, choose an avatar, select a table, and click **Play**
4. The poker table loads and connects to the backend at `localhost:3030`

---

## Features

### Core Gameplay
- 6-seat poker table with oval felt surface
- Player names, stacks (formatted as currency), hole cards, bets, actions, position badges (D/SB/BB)
- Community cards appearing incrementally (3 flop, 1 turn, 1 river)
- Hole cards face-down during play, revealed at showdown (step 12+)
- Winner highlighting with hand rank text and payout amounts
- Pot display with tween animations
- Hand history log with color-coded player actions

### Controls
| Control | Action |
|---------|--------|
| **Next Step** | Advance one state machine step |
| **Auto Play** | Cycles through 3 styles: Safe / Small Random / Hard |
| **Speed** (0.25x / 0.5x / 1x / 2x) | Auto-play interval |
| **Fold / Call / Raise / All-In** | Manual betting actions during betting rounds |
| **2X / 3X / Custom** | Bet sizing options |
| **RESTART** | Fresh reset — all players back to initial stacks |
| **TIP $1** | Tip the dealer with chip animation |
| **Exit (X)** | Return to home screen |

### Animations & Polish
- Card deal animation (arc from dealer to seats)
- Shuffle animation (grow-in + 3x riffle) on new hand
- Chip fly animation on bets (seat to pot)
- Tip chip animation (seat to dealer)
- Pot/stack number tweening
- Phase label punch scale on step change
- Winner seat glow with gold pulse
- Turn timer with low-time warning sound
- "+$1" float text on dealer tip

### Sound Effects
- Card shuffle on new hand
- Chip clink on bets
- Card fold sound
- Turn start notification
- Time remaining warning
- Crowd cheers on winner

### Bonus Features
- Home screen with name input + avatar selection
- Table selector (Starter $1/$2 or High Stakes $5/$10)
- Chip stack visualization (denomination columns per seat)
- WebSocket client for real-time updates (graceful fallback)
- Responsive layout (CanvasScaler at 1920x1080, 0.5 match)

---

## Architecture

### Design Decisions

| Concern | Decision | Why |
|---------|----------|-----|
| UI System | **uGUI** (Canvas-based) | Battle-tested, more community support than UI Toolkit |
| Async | **UnityWebRequest + TaskCompletionSource** | No extra packages, built-in async/await |
| JSON | **Newtonsoft JSON** | Handles nested arrays + nullable types (JsonUtility can't) |
| Text | **TextMeshPro** | Required for quality text rendering |
| Animation | **DOTween** | Industry-standard Unity tweening library |
| Pattern | **Event-driven MVP** | Decoupled views, testable managers |
| State | **Full redraw on every state change** | Simple, correct, no error-prone delta patching |
| Tests | **NUnit Edit Mode only** | Fast, no Play Mode dependency |

### Event Flow

```
GameManager.AdvanceStepAsync()
  -> PokerApiClient.ProcessStepAsync(tableId)
  -> PokerApiClient.GetTableStateAsync(tableId)
  -> TableStateManager.SetState(TableResponse)
  -> OnTableStateChanged event fires
  -> TableView, HudView, SeatView, HandHistoryView, ShowdownView all redraw
```

### Project Structure

```
PokerClient/
├── Assets/
│   ├── Scripts/
│   │   ├── Api/
│   │   │   ├── PokerApiClient.cs          # REST client (UnityWebRequest + async/await)
│   │   │   └── WebSocketClient.cs         # WebSocket for real-time updates
│   │   ├── Models/
│   │   │   ├── GameState.cs               # Game data model + Winner class
│   │   │   ├── PlayerState.cs             # Player data model + status helpers
│   │   │   ├── TableResponse.cs           # Top-level API response wrapper
│   │   │   └── HandHistoryEntry.cs        # Hand history data
│   │   ├── Managers/
│   │   │   ├── GameManager.cs             # Game flow, auto-play, fresh restart
│   │   │   └── TableStateManager.cs       # State broadcasting + step labels
│   │   ├── UI/
│   │   │   ├── TableView.cs               # Table rendering, animations, tip button
│   │   │   ├── SeatView.cs                # Per-seat UI (cards, stack, bet, badges)
│   │   │   ├── CardView.cs                # Single card rendering
│   │   │   ├── CommunityCardsView.cs      # Center community cards (5 slots)
│   │   │   ├── HudView.cs                 # Phase label, hand #, pot, restart/exit
│   │   │   ├── ControlsView.cs            # Next Step, Auto Play, Speed, Actions
│   │   │   ├── HandHistoryView.cs         # Scrollable action log
│   │   │   ├── ShowdownView.cs            # Showdown card display + hand ranks
│   │   │   ├── StatusBarView.cs           # Connection status indicator
│   │   │   ├── HomeScreenView.cs          # Home screen (name, avatar, table select)
│   │   │   ├── ChipStackView.cs           # Chip denomination visualization
│   │   │   └── SoundToggleView.cs         # Sound on/off toggle
│   │   └── Utils/
│   │       ├── CardUtils.cs               # Parse "AH" -> rank + suit, color mapping
│   │       └── MoneyFormatter.cs           # Format floats as "$150.00"
│   ├── Scenes/
│   │   ├── HomeScene.unity                # Home screen scene
│   │   └── PokerTable.unity               # Main poker table scene
│   ├── Editor/
│   │   ├── RebuildScene.cs                # Procedural scene builder
│   │   └── BuildHomeScene.cs              # Home scene builder
│   ├── Resources/
│   │   ├── Avatars/                       # 50+ player avatar sprites
│   │   ├── Cards/                         # Card face sprites
│   │   ├── Audio/                         # Sound effect WAV files
│   │   └── Sprites/Chips/                 # Chip denomination sprites
│   └── Tests/
│       └── EditMode/
│           ├── ApiClientTests.cs          # API response deserialization tests
│           ├── GameStateTests.cs          # Game state logic + model tests
│           └── CardUtilsTests.cs          # Card parsing + money formatting tests
```

---

## API Endpoints

The holdem-processor runs at `http://localhost:3030`:

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/health` | Service health check |
| POST | `/process` | Advance one hand step |
| GET | `/table/{tableId}` | Full table state (game + players) |
| POST | `/table/{tableId}/reset` | Reset table (carry over stacks) |
| POST | `/table/{tableId}/fresh-reset` | Wipe history, fresh stacks for all players |
| POST | `/table/{tableId}/tip` | Tip dealer ($1 from player stack) |

### Card Format

Cards are strings: last character = suit, everything before = rank.

```
"AH"  -> A of Hearts (red)
"10D" -> 10 of Diamonds (red)
"2C"  -> 2 of Clubs (black)
"KS"  -> K of Spades (black)
```

### Player Status Codes

| Code | Meaning |
|------|---------|
| `"1"` | Active |
| `"2"` | Sitting Out |
| `"11"` | Folded |
| `"12"` | All-In |

### 16-Step State Machine

```
 0: GAME_PREP                  ->  Preparing Hand
 1: SETUP_DEALER               ->  Setting Up Dealer
 2: SETUP_SMALL_BLIND          ->  Posting Small Blind
 3: SETUP_BIG_BLIND            ->  Posting Big Blind
 4: DEAL_CARDS                 ->  Dealing Hole Cards
 5: PRE_FLOP_BETTING_ROUND     ->  Pre-Flop Betting
 6: DEAL_FLOP                  ->  Dealing Flop
 7: FLOP_BETTING_ROUND         ->  Flop Betting
 8: DEAL_TURN                  ->  Dealing Turn
 9: TURN_BETTING_ROUND         ->  Turn Betting
10: DEAL_RIVER                 ->  Dealing River
11: RIVER_BETTING_ROUND        ->  River Betting
12: AFTER_RIVER_BETTING_ROUND  ->  Showdown
13: FIND_WINNERS               ->  Evaluating Hands
14: PAY_WINNERS                ->  Paying Winners
15: RECORD_STATS_AND_NEW_HAND  ->  Hand Complete
```

---

## Running Tests

```bash
# Backend tests (holdem processor - 15 tests)
cd serverless-v2/services/holdem-processor && npm install && npm test

# Unity tests (run in Unity Editor)
# Window > General > Test Runner > EditMode > Run All
# Tests cover: API deserialization, game state logic, card parsing, money formatting
```

### Test Coverage

| File | Tests | Covers |
|------|-------|--------|
| ApiClientTests.cs | 6 | Health/Process/Table response deserialization, error handling |
| GameStateTests.cs | 16 | Showdown detection, hand completion, player status, winners, card parsing |
| CardUtilsTests.cs | 17 | Card parsing (AH, 10D, 2C, KS), suit colors, display strings, money formatting, step labels |

---

## Useful Commands

```bash
# Check which containers are running
docker compose ps

# View holdem-processor logs
docker compose logs holdem-processor --tail 50 -f

# Restart backend (picks up code changes)
docker compose restart holdem-processor

# Process one hand step manually
curl -X POST http://localhost:3030/process \
  -H 'Content-Type: application/json' \
  -d '{"tableId": 1}'

# Read current table state
curl http://localhost:3030/table/1

# Fresh reset (all players back to initial stacks)
curl -X POST http://localhost:3030/table/1/fresh-reset

# Stop everything
docker compose --profile engine down

# Full reset (wipe database volumes)
docker compose --profile engine down -v
```

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| **Backend not reachable** | Wait 2-3 minutes on first start. Check `docker compose ps` for healthy status |
| **Port 3030 in use** | Edit `.env`: `HOLDEM_PROCESSOR_PORT=3031` |
| **Unity console errors on Play** | Ensure Docker backend is running first. Check StatusBarView for connection status |
| **Cards not rendering** | Verify `Resources/Cards/` has card sprites. Run **RebuildScene** from menu if needed |
| **No sound** | Check sound toggle button. Verify `Resources/Audio/` has WAV files |
| **Tests fail with "Cannot find module"** | Run `npm install` in the service directory first |
| **Fresh restart not working** | Restart Docker: `docker compose restart holdem-processor` |

---

## Port Reference

| Service | Port |
|---------|------|
| MySQL | 3306 |
| Redis | 6379 |
| DynamoDB Local | 8000 |
| ElasticMQ (SQS) | 9324 |
| EventBridge Mock | 4010 |
| Holdem Processor | 3030 |
| Cash Game Broadcast | 3032 |
