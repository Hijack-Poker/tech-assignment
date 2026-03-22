# Hijack Poker — Unity Game Client (Option D)

A fully-featured 6-seat poker table client built in Unity that connects to the holdem-processor REST API, steps through hands one state at a time, and renders cards, stacks, bets, community cards, winners, and hand history — with animations, sound effects, and polish.

---

## Prerequisites

| Tool | Version | Download |
|------|---------|----------|
| Docker Desktop | v4+ with Compose v2 | [docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop/) |
| Unity Hub | Latest | [unity.com/download](https://unity.com/download) |
| Unity Editor | 2022.3+ LTS or Unity 6 | Install via Unity Hub |
| Node.js | 22+ (for backend tests) | [nodejs.org](https://nodejs.org/) |
| Git | Latest | [git-scm.com](https://git-scm.com/) |

---

## Setup — Step by Step

### Step 1: Clone and configure

```bash
git clone <this-repo>
cd tech-assignment
cp .env.example .env
```

### Step 2: Start the backend with Docker

```bash
docker compose --profile engine up -d
```

This spins up 7 containers:

| Container | What it does | Port |
|-----------|-------------|------|
| MySQL 8.0 | Game state database (players, tables, hands) | 3306 |
| Redis 7 | Caching layer | 6379 |
| DynamoDB Local | NoSQL store (WebSocket connections) | 8000 |
| ElasticMQ | SQS message queue mock | 9324 |
| EventBridge Mock | Event bus mock | 4010 |
| **Holdem Processor** | **Game engine REST API** | **3030** |
| Cash Game Broadcast | WebSocket broadcast service | 3032 |

First run takes **2-3 minutes** (npm install inside containers). Wait for all to be healthy:

```bash
docker compose ps
```

### Step 3: Verify the backend

```bash
curl http://localhost:3030/health
# -> {"service":"holdem-processor","status":"ok","timestamp":"..."}

curl http://localhost:3030/table/1
# -> Full table state with 6 players
```

### Step 4: Open the Unity project

1. Open **Unity Hub**
2. Click **Open**
3. Navigate to `tech-assignment/PokerClient/` and select it
4. Unity Hub detects the project — if prompted, install the matching Editor version
5. Wait for import to complete (first time takes a few minutes)

### Step 5: Play

1. In Unity Editor, open **Assets/Scenes/HomeScene.unity**
2. Press the **Play** button
3. Enter your name, pick an avatar, choose a table (Starter $1/$2 or High Stakes $5/$10)
4. Click **Play** — the poker table loads and connects to `localhost:3030`

### Stopping

```bash
docker compose --profile engine down        # Stop containers
docker compose --profile engine down -v     # Stop + wipe all database data
```

---

## Controls

| Control | What it does |
|---------|-------------|
| **Next Step** | Advance one step in the 16-step hand state machine |
| **Auto Play** | Toggle auto-advance. Cycles styles: Safe -> Small Random -> Hard |
| **Speed** (0.25x / 0.5x / 1x / 2x) | Auto-play interval |
| **Fold / Call / Raise / All-In** | Manual betting actions (shown during betting rounds) |
| **2X / 3X / Custom** | Bet sizing presets |
| **RESTART** (top right) | Fresh reset — wipes game history, all players back to initial stacks |
| **TIP $1** (below dealer) | Tip the dealer — deducts $1 from acting player with chip animation |
| **X** (top right) | Exit to home screen |

---

## Architecture

### Design Decisions

| Decision | Choice | Why |
|----------|--------|-----|
| UI System | **uGUI (Canvas-based)** | Mature, battle-tested, more community resources than UI Toolkit |
| Async | **UnityWebRequest + TaskCompletionSource** | Built-in async/await wrapper, no extra packages |
| JSON | **Newtonsoft JSON** | Handles nested arrays and nullable types (JsonUtility can't) |
| Text | **TextMeshPro** | Required for quality text rendering |
| Animation | **DOTween** | Industry-standard Unity tweening library |
| Pattern | **Event-driven MVP** | Decoupled views, testable managers |
| State | **Full redraw on every state change** | Simple, correct, no error-prone delta patching |
| Tests | **NUnit Edit Mode** | Fast, no Play Mode dependency |

### Data Flow

```
User clicks "Next Step"
  -> GameManager.AdvanceStepAsync()
    -> POST /process { tableId: 1 }          (advance one step)
    -> GET /table/1                           (fetch new state)
    -> TableStateManager.SetState(response)
    -> OnTableStateChanged event fires
    -> TableView, SeatView, HudView, HandHistoryView, ShowdownView all redraw
```

### API Endpoints

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/health` | Backend health check |
| POST | `/process` | Advance hand by one step |
| GET | `/table/{tableId}` | Full table state (game + players) |
| POST | `/table/{tableId}/reset` | Reset table (carry over stacks) |
| POST | `/table/{tableId}/fresh-reset` | Wipe all history, fresh stacks |
| POST | `/table/{tableId}/tip` | Tip dealer ($1 from player stack) |

---

## Project Structure

```
PokerClient/
├── Assets/
│   ├── Scripts/
│   │   ├── Api/
│   │   │   ├── PokerApiClient.cs            REST client (async/await over UnityWebRequest)
│   │   │   └── WebSocketClient.cs           Real-time updates with graceful fallback
│   │   ├── Models/
│   │   │   ├── GameState.cs                 Game state data + Winner class
│   │   │   ├── PlayerState.cs               Player data + IsFolded/IsAllIn/IsWinner helpers
│   │   │   └── TableResponse.cs             Top-level API response wrapper
│   │   ├── Managers/
│   │   │   ├── GameManager.cs               Game orchestration, auto-play (3 styles), restart
│   │   │   └── TableStateManager.cs         State broadcasting via OnTableStateChanged event
│   │   ├── UI/
│   │   │   ├── TableView.cs                 Table rendering, deal/shuffle/chip-fly animations
│   │   │   ├── SeatView.cs                  Per-seat: name, stack, cards, bet, action, badges
│   │   │   ├── CardView.cs                  Individual card display (face-up / face-down)
│   │   │   ├── CommunityCardsView.cs        5 community card slots (flop/turn/river)
│   │   │   ├── HudView.cs                   Phase label, hand #, pot, restart & exit buttons
│   │   │   ├── ControlsView.cs              Next Step, Auto Play, Speed, Fold/Call/Raise/All-In
│   │   │   ├── HandHistoryView.cs           Scrollable color-coded action log
│   │   │   ├── ShowdownView.cs              Showdown card reveal with hand ranks
│   │   │   ├── HomeScreenView.cs            Name input, avatar picker, table selector
│   │   │   ├── ChipStackView.cs             Chip denomination columns (100/25/5/1)
│   │   │   └── StatusBarView.cs             Connection status indicator
│   │   └── Utils/
│   │       ├── CardUtils.cs                 "AH" -> Ace of Hearts, suit colors
│   │       └── MoneyFormatter.cs            float -> "$150.00"
│   ├── Scenes/
│   │   ├── HomeScene.unity                  Home screen
│   │   └── PokerTable.unity                 Main poker table
│   ├── Editor/
│   │   ├── RebuildScene.cs                  Procedural scene builder
│   │   └── BuildHomeScene.cs                Home scene builder
│   ├── Resources/
│   │   ├── Avatars/                         50+ player avatar sprites
│   │   ├── Cards/                           Full deck of card face sprites
│   │   ├── Audio/                           7 sound effect clips
│   │   └── Sprites/Chips/                   Chip denomination sprites
│   └── Tests/EditMode/
│       ├── ApiClientTests.cs                6 tests — response deserialization
│       ├── GameStateTests.cs                16 tests — state logic, models, winners
│       └── CardUtilsTests.cs                17 tests — parsing, formatting, labels
```

---

## Tests

**39 unit tests** — all Edit Mode (NUnit). Run in Unity Editor:

**Window > General > Test Runner > EditMode > Run All**

| Suite | Count | Covers |
|-------|-------|--------|
| ApiClientTests | 6 | Health, Process, Table response deserialization, error cases |
| GameStateTests | 16 | Showdown detection, hand completion, player status codes, winner identification, JSON parsing edge cases |
| CardUtilsTests | 17 | Card parsing (AH, 10D, 2C, KS), suit colors, display strings, money formatting, step labels |

Backend tests:

```bash
cd serverless-v2/services/holdem-processor && npm install && npm test
```

---

## Features

### Must Have (all complete)

- 6-seat poker table with felt surface
- API client connects to holdem-processor at `localhost:3030`
- `POST /process` advances hand by one step, `GET /table/1` fetches and displays state
- Community cards appear incrementally (3 flop, 1 turn, 1 river)
- Hole cards face-down during play, revealed at showdown (step 12+)
- Player name, stack, bet, action at each seat with currency formatting
- Pot display in center of table
- Dealer / SB / BB position badges
- Winner highlighting with hand rank text and payout amount
- Stack amounts update to reflect winnings
- Next Step button triggers one state advance
- Phase label shows human-readable step name
- Unit tests on API client, data models, and game state logic
- Docker `engine` profile starts the backend, Unity connects to it

### Should Have (all complete)

- Auto-play mode with 4 speeds (0.25s, 0.5s, 1s, 2s) and 3 play styles (Safe, Small Random, Hard)
- Card reveal animation at showdown
- Smooth tween on stack and pot amount changes (DOTween)
- Phase label punch-scale animation on step change
- Multiple consecutive hands play through seamlessly
- Hand history log with step headers and color-coded player actions
- Error handling with user-visible connection status bar
- Hand number display

### Could Have (all complete)

- Card sprites (full deck)
- Chip stack visualization (denomination columns: $100, $25, $5, $1)
- Sound effects: card shuffle, chip clink, fold, turn start, time warning, crowd cheers, win
- Responsive layout (CanvasScaler 1920x1080, matchWidthOrHeight 0.5)
- 50+ player avatars with selection on home screen
- Card deal animation (quadratic Bezier arc from dealer to seats)
- Shuffle animation (grow-in + 3x riffle) on every new hand
- WebSocket client for real-time table updates (graceful fallback when unavailable)
- Fresh restart button — wipes all game history, resets player stacks
- Configurable table ID — select Starter ($1/$2) or High Stakes ($5/$10) on home screen

### Beyond Requirements

- Home screen with name input, avatar selection, and table picker
- Full betting UI: Fold, Call, Raise, All-In, 2X, 3X, custom bet input
- Tip dealer button with chip fly animation and $1 stack deduction
- Turn timer with low-time warning sound (5 second threshold)
- Winner seat gold pulse animation
- Chip fly animation on bets (seat to pot)
- "+$1" float text on dealer tip

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Backend not reachable | Wait 2-3 min on first start. Run `docker compose ps` to check health |
| Port 3030 in use | Edit `.env`: `HOLDEM_PROCESSOR_PORT=3031` |
| Unity errors on Play | Ensure Docker backend is running. Check StatusBarView in bottom bar |
| No cards rendering | Verify `Resources/Cards/` has sprites. Run RebuildScene from Unity menu |
| No sound | Check sound toggle. Verify `Resources/Audio/` has WAV files |
| Fresh restart fails | Run `docker compose restart holdem-processor` to reload backend code |
| Tests fail "Cannot find module" | Run `npm install` in the service directory first |
| Want a completely fresh start | `docker compose --profile engine down -v && docker compose --profile engine up -d` |
