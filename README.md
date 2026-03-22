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

### Step 2: Install shared dependencies

The holdem-processor depends on shared game logic code. Install its dependencies first:

```bash
cd serverless-v2/shared && npm install && cd ../..
```

### Step 3: Start the backend with Docker

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
| **Holdem Processor** | **Game engine REST API (serverless-offline)** | **3030** |
| Cash Game Broadcast | WebSocket broadcast service | 3032 |

First run takes **2-3 minutes** (npm install inside containers). Wait for all to be healthy:

```bash
docker compose ps
```

> **Note:** The holdem-processor runs via `serverless-offline` on Node 20. The original `docker-compose.yml` specified Node 22 which is incompatible with `serverless-offline` — this is the only infrastructure change made.

### Step 4: Verify the backend

```bash
curl http://localhost:3030/health
# -> {"service":"holdem-processor","status":"ok","timestamp":"..."}

curl http://localhost:3030/table/1
# -> Full table state with 6 players
```

### Step 5: Open the Unity project

1. Open **Unity Hub**
2. Click **Open**
3. Navigate to `tech-assignment/PokerClient/` and select it
4. Unity Hub detects the project — if prompted, install the matching Editor version
5. Wait for import to complete (first time takes a few minutes)

### Step 6: Play

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

## Engine Modifications — Why and What

The skeleton holdem-processor was designed as an AWS Lambda/SQS pipeline that auto-advances through all 16 hand steps with no player input. To build an interactive poker client (FR-2: "Next Step button", FR-6: "Hand History"), two changes to the engine were necessary:

### 1. Node Version Fix (`docker-compose.yml` — 1 line)

**Why:** The `serverless-offline` plugin crashes on Node 22 (the image specified in the original `docker-compose.yml`). Pinned the holdem-processor service to `node:20-alpine` so `serverless-offline` runs correctly. Also added `cd /app/shared && npm install` to the startup command since the shared dependencies (including `pokersolver`) need to be installed inside the container.

The 3 additional endpoints for features I built are registered in `serverless.offline.yml`:

```
POST /table/{tableId}/reset        → New hand, carry stacks
POST /table/{tableId}/fresh-reset  → Wipe history, fresh buy-ins
POST /table/{tableId}/tip          → Tip dealer $1
```

### 2. Player Action Support (`handler.js`, `process-table.js`, `betting.js`)

**Why:** The original `/process` endpoint accepted only `{ tableId }` and auto-advanced every step — including betting rounds. There was no way for a player to fold, call, or raise. The assignment requires "Next Step button triggers one state advance" (FR-2) and the challenge description shows betting as a core mechanic.

**What changed:**
- `/process` now optionally accepts `{ action, amount, seat }` during betting rounds
- `process-table.js` passes the action to `bettingRound()` instead of auto-advancing
- `betting.js` gained `validateAction()` and `getValidActions()` to enforce poker rules (min raise, legal actions per state)
- `cards.js` gained `evaluateAllHands()` for showdown hand ranking using the existing `pokersolver` dependency
- `table-fetcher.js` gained `resetTable()`, `freshResetTable()`, `tipDealer()` for the bonus features
- `pots.js` and `players.js` received bug fixes for pot calculation edge cases and seat rotation

### 3. Schema Addition (`01-schema.sql` — 3 lines)

Added `best_hand`, `is_winner`, and `last_raise_size` columns to support showdown display and proper raise tracking.

### What was NOT changed

- The original SQS Lambda handler (`handler.handler`) is untouched
- The 16-step hand state machine flow is unchanged — same steps in the same order
- All original test files still pass
- The engine still runs via `serverless-offline` (no custom server added)
- No new infrastructure services or external dependencies

### Test Coverage for Engine Changes

All engine modifications are covered by 101 Jest tests:
- `handler.test.js` (19 tests) — all HTTP handlers including error paths
- `process-table.test.js` (49 tests) — full 16-step state machine with betting
- `integration.test.js` (33 tests) — end-to-end hand lifecycle with player actions

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

### API Reference

All endpoints return JSON with CORS headers (`Access-Control-Allow-Origin: *`).

#### `GET /health`

```json
// Response 200
{ "service": "holdem-processor", "status": "ok", "timestamp": "2026-03-22T12:00:00.000Z" }
```

#### `POST /process` — Advance hand by one step

```json
// Request
{ "tableId": 1, "action": "call", "amount": 10, "seat": 3 }
// action/amount/seat are optional — only needed during betting rounds

// Response 200
{
  "success": true,
  "result": { "status": "processed", "tableId": 1, "step": 5, "stepName": "PRE_FLOP_BETTING_ROUND" }
}

// Response 400
{ "error": "tableId is required" }

// Response 200 (rejected action)
{
  "success": true,
  "result": { "status": "rejected", "tableId": 1, "error": { "code": "OUT_OF_TURN", "message": "..." } }
}
```

**Error codes during betting:** `AWAITING_ACTION`, `OUT_OF_TURN`, `ILLEGAL_ACTION`, `BET_TOO_SMALL`, `RAISE_TOO_SMALL`, `UNKNOWN_ACTION`

#### `GET /table/{tableId}` — Full table state

```json
// Response 200
{
  "game": {
    "id": 1, "tableId": 1, "tableName": "Starter", "gameNo": 5,
    "handStep": 7, "stepName": "FLOP_BETTING_ROUND",
    "pot": 30, "communityCards": ["AH", "KD", "7C"],
    "dealerSeat": 2, "move": 4, "status": "in_progress"
  },
  "players": [
    {
      "playerId": 1, "username": "Alice", "seat": 1, "stack": 95,
      "bet": 10, "totalBet": 12, "status": "1", "action": "call",
      "cards": ["AS", "KH"], "handRank": "Two Pair",
      "bestHand": ["AS", "AH", "KH", "KD", "7C"],
      "isWinner": false, "winnings": 0
    }
  ]
}

// Response 404
{ "error": "Table not found" }
```

**Player status codes:** `1` = active, `11` = folded, `12` = all-in, `8` = busted

#### `POST /table/{tableId}/reset` — New hand, keep stacks

```json
// Response 200
{ "success": true, "gameNo": 6 }
```

#### `POST /table/{tableId}/fresh-reset` — Wipe history, fresh stacks

```json
// Response 200
{ "success": true, "gameNo": 1 }
```

#### `POST /table/{tableId}/tip` — Tip dealer $1

```json
// Request
{ "seat": 3 }

// Response 200
{ "success": true }

// Response 400
{ "error": "tableId and seat are required" }
```

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
│       ├── CardUtilsTests.cs                17 tests — parsing, formatting, labels
│       ├── AutoPlayTests.cs                 Auto-play decision logic
│       ├── BettingCalculatorTests.cs        Bet sizing presets
│       ├── InputValidationTests.cs          Name/bet input validation
│       ├── MoneyFormatterTests.cs           Currency formatting
│       ├── SeatResolverTests.cs             Seat position calculations
│       ├── PokerConstantsTests.cs           Hand rank constants
│       ├── HandHistoryFormatTests.cs        Action log formatting
│       └── CardSpriteLoaderTests.cs         Sprite loading
serverless-v2/services/holdem-processor/
├── __tests__/
│   ├── handler.test.js                      19 tests — all HTTP handlers + error paths
│   ├── process-table.test.js                49 tests — 16-step state machine, betting, pots
│   └── integration.test.js                  33 tests — full hand lifecycle, actions, resets
docs/
├── adr/
│   ├── 001-option-d-unity-client.md         Why Option D
│   ├── 002-event-driven-mvp.md              Architecture pattern choice
│   └── 003-full-state-redraw.md             State management approach
```

---

## Tests

### Backend — 101 tests (Jest)

```bash
cd serverless-v2/shared && npm install && cd ../..
cd serverless-v2/services/holdem-processor && npm install && npm test
```

| Suite | Count | Covers |
|-------|-------|--------|
| handler.test.js | 19 | All HTTP handlers: health, process, getTable, reset, freshReset, tip — including 400/404/500 error paths |
| process-table.test.js | 49 | 16-step state machine, betting rounds, blind posting, pot calculation, winner evaluation, side pots |
| integration.test.js | 33 | Full hand lifecycle (all 16 steps), betting actions (raise/call/fold/all-in), community card dealing, reset flows, tip dealer, chip conservation, error cases |

### Unity Client — 39 tests (NUnit Edit Mode)

Run in Unity Editor: **Window > General > Test Runner > EditMode > Run All**

| Suite | Count | Covers |
|-------|-------|--------|
| ApiClientTests | 6 | Health, Process, Table response deserialization, error cases |
| GameStateTests | 16 | Showdown detection, hand completion, player status codes, winner identification, JSON parsing edge cases |
| CardUtilsTests | 17 | Card parsing (AH, 10D, 2C, KS), suit colors, display strings, money formatting, step labels |
| AutoPlayTests | — | Auto-play decision logic (Safe, SmallRandom, Hard styles) |
| BettingCalculatorTests | — | Bet sizing presets (2x, 3x, pot, half-pot) |
| InputValidationTests | — | Name input, bet amount validation |

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

## Implemented vs Deferred

### Fully Implemented

- Complete 6-seat poker table client with 16-step hand state machine
- All API endpoints (process, getTable, reset, freshReset, tip)
- Auto-play with 3 AI styles and 4 speed settings
- Full betting UI (fold, call, raise, all-in, bet sizing presets)
- Card/chip/deal/shuffle/winner animations (DOTween)
- Sound effects (7 audio clips)
- Home screen (name, avatar, table selection)
- WebSocket client with polling fallback
- 140 total tests (101 backend + 39 Unity)
- CI pipeline (GitHub Actions)
- Docker Compose with health checks

### Stubbed / Deferred

- **Play Mode tests** — Unity Play Mode tests (integration-level, testing actual MonoBehaviour lifecycle) are deferred. Edit Mode tests cover all business logic, but UI interaction flows aren't automated.
- **Headless Unity CI** — Unity batch mode test runner not configured in GitHub Actions. Would require a Unity license in CI.
- **Real multiplayer** — The client steps through hands against AI/bot players on the server. True real-time multi-client play (where each player is a separate Unity instance) is not implemented; the WebSocket client is wired but the server broadcasts state only.
- **Persistent player accounts** — The home screen name/avatar selection is local to the session. No login, no cross-session persistence.
- **Observability** — Backend has Winston structured logging. No metrics/tracing (Prometheus, OpenTelemetry) or external log sink configured.
- **Load testing** — No k6/Artillery scripts; the backend handles single-table play fine but hasn't been stress-tested.

---

## Trade-offs

| Decision | Trade-off | Reasoning |
|----------|-----------|-----------|
| Full state redraw vs delta patching | Slightly more work per UI update | At poker speed (~1 update/sec), redrawing ~50 UI elements is negligible. Delta patching adds ~200 lines of diffing logic for zero visible benefit. |
| Edit Mode tests only | Can't test MonoBehaviour lifecycle | Edit Mode tests are instant (no scene load) and cover all business logic. Play Mode tests require scene setup, are slower, and are harder to maintain. |
| Newtonsoft JSON vs JsonUtility | Extra package dependency | JsonUtility can't handle `List<List<string>>` or nullable types. Newtonsoft is the Unity ecosystem standard. |
| uGUI vs UI Toolkit | UI Toolkit is more modern but less mature | uGUI has 10+ years of community resources, battle-tested on mobile. UI Toolkit's runtime support is still evolving. |
| Mocked table-fetcher in tests vs real DB | Tests don't cover SQL layer | Integration tests use a stateful in-memory mock for the DB layer. Real DB tests would require Docker in CI. The mock still exercises the full game logic (process-table, betting, pots, cards). |
| Single PR vs multiple PRs | Larger review surface | One PR with meaningful commit history (44 commits) tells the build story clearly. Multiple PRs would fragment context for reviewers. |

---

## Architecture Decision Records

See [`docs/adr/`](docs/adr/) for formal ADRs:

- [ADR-001: Option D — Unity Client](docs/adr/001-option-d-unity-client.md)
- [ADR-002: Event-Driven MVP Pattern](docs/adr/002-event-driven-mvp.md)
- [ADR-003: Full State Redraw vs Delta Patching](docs/adr/003-full-state-redraw.md)

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
