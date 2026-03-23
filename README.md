# Hijack Poker — Omaha Hi-Lo (8-or-Better) Implementation

## What Was Built

This branch extends the existing Texas Hold'em engine pipeline with **Omaha Hi-Lo (8-or-better)** — a split-pot poker variant that exercises every layer of the architecture: card dealing, hand evaluation, pot distribution, event broadcasting, and UI rendering. The implementation adds a fully functional new game type that runs alongside Hold'em with zero regressions to existing behavior.

Omaha Hi-Lo was chosen because it is one of the most mechanically demanding variants to implement correctly. It requires enforcing a strict card-selection constraint (exactly 2 of 4 hole cards), implementing an entirely separate hand evaluation system for qualifying low hands, and splitting pots conditionally between high and low winners — including side pots, odd-chip allocation, scooping, and quartering scenarios.

---

## Omaha Hi-Lo Rules (for the Engineer Reviewer)

### Card Selection: The "2+3" Rule

In Texas Hold'em, the best 5 cards are chosen from any combination of the player's 2 hole cards and 5 community cards (7 cards total). In Omaha, each player receives **4 hole cards** but must construct their hand using **exactly 2 hole cards and exactly 3 community cards**. This is not optional — even if a player's 4 hole cards contain a flush, they cannot use more than 2 of them.

This means a player holding A♥K♥Q♥J♥ on a board of T♥9♥2♦3♣4♠ does **not** have an ace-high flush. They must pick exactly 2 of their hole cards and 3 from the board. The best they can make is A♥K♥ + T♥9♥2♦ (flush) or A♥K♥ + T♥9♥4♠ (flush), but never using 3+ hole cards.

This constraint means every hand evaluation requires examining C(4,2) × C(5,3) = **60 valid 5-card combinations** per player.

### The High Hand

The high hand follows standard poker rankings (Royal Flush → High Card). Each player's best high hand is determined from their 60 valid combinations. The player(s) with the strongest high hand win the high half of the pot.

### The Low Hand: 8-or-Better Qualification

The pot is split between the best **high** hand and the best qualifying **low** hand. A qualifying low hand must:

1. Consist of 5 cards all ranked **8 or lower** (Ace counts as 1 for low)
2. Contain **no pairs** (all 5 ranks must be distinct)
3. Follow the same 2+3 Omaha rule (exactly 2 hole cards + 3 community cards)

Straights and flushes do **not** count against a low hand. The best possible low is A-2-3-4-5 ("the wheel"), and the worst qualifying low is 4-5-6-7-8.

Low hands are compared top-down from the highest card. For example, 7-5-4-3-A beats 8-4-3-2-A because 7 < 8 in the first position.

### When No Low Is Possible

The board must contain **at least 3 unpaired cards ranked 8 or lower** for any low hand to be possible. If the community cards are K-Q-J-9-2, only one card qualifies — no player can form a valid low regardless of their hole cards. When no qualifying low exists, the **high hand scoops the entire pot**.

In practice, boards produce no qualifying low approximately 40-50% of the time.

### Split Pot Mechanics

When a qualifying low exists:
- The pot is divided **50/50** between the high winner(s) and the low winner(s)
- If multiple players tie for high or low, that half is split equally among them
- **Odd chip** (from indivisible cents) goes to the high winner
- **Quartering**: If two players tie for low while one wins high, the low players each get 25% of the pot (they split the low half). This is a critical strategic consideration in real Omaha Hi-Lo
- **Scooping**: A single player can win both high and low, taking the entire pot
- Side pots from all-in players are each split independently using the same hi-lo logic

---

## Architecture Decisions

### Single Processor, Game-Type Branching

Rather than creating a separate service for Omaha, the existing `holdem-processor` reads `game_type` from the `game_tables` database row and branches at three points in the state machine: dealing (4 cards vs 2), winner evaluation (Omaha hi-lo vs standard), and pot distribution (split vs single). This keeps the architecture simple and demonstrates that the engine was designed for multi-variant support — `game_type` already existed in the schema but was unused.

### Pokersolver Reuse via Combo Enumeration

The high hand evaluator reuses the existing `pokersolver` dependency (already battle-tested for Hold'em in this codebase). For each player, we enumerate all 60 valid 2+3 combinations and solve each as a standard 5-card hand. This is correct because pokersolver's `Hand.solve()` evaluates exactly the 5 cards you give it. No new dependency is needed.

### Custom Low Hand Evaluator

No well-maintained npm package exists for 8-or-better low hand evaluation (the closest, `handranker`, was last published 12 years ago with a deleted source repo). The low evaluator is intentionally simple: for each 5-card combo, check if all ranks are ≤ 8, check for no pairs, then sort descending for comparison. The entire evaluator is ~60 lines with no external dependencies.

### 60-Combination Enumeration

C(4,2) × C(5,3) = 6 × 10 = 60 combinations per player. Even at a 9-player table this is 540 evaluations — trivially fast for a state machine that processes one step per request. The combination utility is shared between high and low evaluators.

---

## How to Validate

### Run the Engine

```bash
docker compose --profile engine up -d
```

### Open the Hand Viewer

Navigate to **http://localhost:8080**. Use the **table selector** dropdown to switch between:
- **Table 1** — Texas Hold'em (existing behavior, unchanged)
- **Table 3** — Omaha Hi-Lo (new)

### Observe Omaha Hi-Lo Hands

On the Omaha Hi-Lo table:
1. Click **Auto Play** (or step manually with **Next Step**)
2. Each player is dealt **4 hole cards** (visible at showdown)
3. At showdown, each player's **High** and **Low** hand descriptions are displayed
4. The pot splits between high and low winners when a qualifying low exists
5. When no low qualifies, the log and display show "No qualifying low — high scoops"

### Simulate via CLI

```bash
node scripts/simulate-hands.js --table 3 --game-type omaha_hilo --loop
```

### Run Tests

```bash
cd serverless-v2/services/holdem-processor
npm install && cd ../../shared && npm install && cd ../../services/holdem-processor
npm test
```

The test suite covers:
| Test File | What It Covers |
|-----------|---------------|
| `omaha-combinations.test.js` | 60-combo enumeration correctness, no duplicates, 2+3 structure |
| `omaha-high-eval.test.js` | Omaha 2+3 rule enforcement, best-hand selection, multi-player winners |
| `omaha-low-eval.test.js` | 8-or-better qualification, no-low boards, low ranking/comparison |
| `omaha-pot-split.test.js` | Hi-lo split, scooping, odd chip, side pots, quartering |
| `omaha-hilo-hand.test.js` | Full 16-step hand integration (GAME_PREP → RECORD_STATS) |
| `process-table.test.js` | Original 15 Hold'em tests — all pass, zero regressions |

### Scenarios to Look For

- **Low qualifies**: Board has 3+ cards ≤ 8. Pot splits between high and low winner. Check that stacks add up correctly.
- **No qualifying low**: Board is all high cards. High hand scoops entire pot. Log shows "No qualifying low."
- **Same player scoops**: One player wins both high and low. They receive the full pot.

---

## File Change Manifest

| File | Change |
|------|--------|
| **Evaluation Module (new)** | |
| `serverless-v2/shared/games/omaha-hilo/combinations.js` | Combo enumeration: C(4,2) × C(5,3) |
| `serverless-v2/shared/games/omaha-hilo/high-eval.js` | Omaha high hand evaluator via pokersolver |
| `serverless-v2/shared/games/omaha-hilo/low-eval.js` | 8-or-better low hand evaluator |
| `serverless-v2/shared/games/omaha-hilo/index.js` | Module re-exports |
| **Engine Pipeline (modified)** | |
| `serverless-v2/shared/games/common/constants.js` | Added `OMAHA_HI_LO` to `GAME` enum |
| `serverless-v2/services/holdem-processor/lib/process-table.js` | Branches at deal, find winners, pay winners |
| `serverless-v2/services/holdem-processor/lib/table-fetcher.js` | Selects + normalizes `game_type`, persists `low_winners`/`low_hand_rank` |
| `serverless-v2/services/holdem-processor/lib/event-publisher.js` | Dynamic `gameType` from DB instead of hardcoded |
| `serverless-v2/services/holdem-processor/handler.js` | Exposes `gameType`, `lowHandRank` in API response |
| **Infrastructure (modified)** | |
| `infrastructure/mysql-init/01-schema.sql` | Added `low_winners`/`low_hand_rank` columns, Omaha Hi-Lo table seed |
| `scripts/simulate-hands.js` | Added `--game-type` CLI flag |
| **UI (modified)** | |
| `ui/index.html` | 4-card layout, hi/lo display, table selector, game type label |
| **Tests (new)** | |
| `holdem-processor/__tests__/omaha-combinations.test.js` | Combination utility tests |
| `holdem-processor/__tests__/omaha-high-eval.test.js` | High hand evaluator tests |
| `holdem-processor/__tests__/omaha-low-eval.test.js` | Low hand evaluator tests |
| `holdem-processor/__tests__/omaha-pot-split.test.js` | Hi-lo pot splitting tests |
| `holdem-processor/__tests__/omaha-hilo-hand.test.js` | Full hand integration test |

---

# Hijack Poker — Technical Assignment

Welcome to the Hijack Poker technical challenge. This repo provides a working serverless infrastructure skeleton that mirrors our production architecture. Your job is to build one of four challenge options on top of it.

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (with Docker Compose v2)
- [Node.js 22+](https://nodejs.org/) (for running tests and scripts locally)
- Git

## Challenge Options

| Option | Challenge | Stack | Profile |
|--------|-----------|-------|---------|
| **A** | [Rewards System](https://hijack-poker.github.io/tech-assignment/#/challenge-rewards) | React + Serverless API + DynamoDB | `rewards` |
| **B** | [Bomb Pots](https://hijack-poker.github.io/tech-assignment/#/challenge-bomb-pots) | Game Engine Pipeline (SQS → Lambda → EventBridge) | `engine` |
| **C** | [Daily Streaks](https://hijack-poker.github.io/tech-assignment/#/challenge-streaks) | React + Serverless API + DynamoDB | `streaks` |
| **D** | [Unity Game Client](https://hijack-poker.github.io/tech-assignment/#/challenge-unity-client) | Unity + C# + REST API | `engine` |

Full challenge documentation: **https://hijack-poker.github.io/tech-assignment/**

---

## Quick Start

### 1. Clone & configure

```bash
git clone <this-repo>
cd tech-assignment
cp .env.example .env
```

### 2. Start your challenge profile

Each challenge option has a Docker Compose profile that starts only the services you need. All profiles include the `core` infrastructure (MySQL, Redis, DynamoDB Local).

```bash
# Option A: Rewards System
docker compose --profile rewards up

# Option B: Bomb Pots (Engine Pipeline)
docker compose --profile engine up

# Option C: Daily Streaks
docker compose --profile streaks up
```

> First run takes 2–3 minutes as containers install npm dependencies. Subsequent starts are faster.

### 3. Verify it's running

**Option A — Rewards:**

| Service | URL |
|---------|-----|
| Rewards API health | http://localhost:5000/api/v1/health |
| Rewards Frontend | http://localhost:4000 |

**Option B — Engine Pipeline:**

| Service | URL |
|---------|-----|
| Holdem Processor health | http://localhost:3030/health |
| Cash Game Broadcast health | http://localhost:3032/health |
| Hand Viewer UI | http://localhost:8080 |

**Option C — Streaks:**

| Service | URL |
|---------|-----|
| Streaks API health | http://localhost:5001/api/v1/health |
| Streaks Frontend | http://localhost:4001 |

**Option D — Unity Game Client:**

| Service | URL |
|---------|-----|
| Holdem Processor health | http://localhost:3030/health |
| Table state | http://localhost:3030/table/1 |

> Option D uses the same `engine` Docker profile as Option B. The Unity app runs natively in the Unity Editor (not in Docker) and connects to the holdem-processor API. See `unity-client/README.md` for Unity project setup.

### 4. Stop everything

```bash
docker compose --profile <your-profile> down

# To also remove database volumes (full reset):
docker compose --profile <your-profile> down -v
```

---

## Architecture Overview

```
┌──────────────────────────────────────────────────────────────────┐
│  Docker Compose Profiles                                         │
│                                                                  │
│  core:    MySQL 8.0 │ Redis 7 │ DynamoDB Local                   │
│                                                                  │
│  engine:  core + ElasticMQ (SQS) + EventBridge Mock              │
│           + Holdem Processor (:3030) + Broadcast (:3032)         │
│           + Hand Viewer (:8080)                                  │
│                                                                  │
│  rewards: core + Rewards API (:5000) + React Frontend (:4000)    │
│                                                                  │
│  streaks: core + Streaks API (:5001) + React Frontend (:4001)    │
└──────────────────────────────────────────────────────────────────┘
```

### Engine Pipeline (Option B)

```
                  ┌──────────┐    ┌─────────────────┐    ┌─────────────────┐    ┌───────────────┐
HTTP POST ──────► │ ElasticMQ│───►│ Holdem Processor │───►│ EventBridge Mock│───►│ Cash Game     │
/process          │  (SQS)   │    │   (Lambda)       │    │                 │    │ Broadcast     │
                  └──────────┘    └────────┬─────────┘    └─────────────────┘    └───────────────┘
                                           │
                                      ┌────▼────┐
                                      │  MySQL  │
                                      │ (state) │
                                      └─────────┘
```

The holdem processor runs a **16-step state machine** for each poker hand:

```
GAME_PREP → SETUP_DEALER → SETUP_SMALL_BLIND → SETUP_BIG_BLIND → DEAL_CARDS
→ PRE_FLOP_BETTING_ROUND → DEAL_FLOP → FLOP_BETTING_ROUND → DEAL_TURN
→ TURN_BETTING_ROUND → DEAL_RIVER → RIVER_BETTING_ROUND
→ AFTER_RIVER_BETTING_ROUND → FIND_WINNERS → PAY_WINNERS
→ RECORD_STATS_AND_NEW_HAND
```

Each call to `processTable(tableId)` advances the hand by **one step**. After the final step, the next call starts a new hand automatically.

### API + Frontend (Options A & C)

```
React Frontend (Vite) → Serverless API (serverless-offline) → DynamoDB Local
```

---

## Hand Viewer UI (Option B)

A simple vanilla JS poker table UI is included for visualizing the hand processing pipeline.

### Running the Hand Viewer

The Hand Viewer is served automatically when running the `engine` profile:

```bash
docker compose --profile engine up -d
```

Then open http://localhost:8080.

### What it shows

- Green felt poker table with 6 player seats
- Community cards dealt to the center (flop, turn, river)
- Player stacks, bets, and actions at each seat
- Dealer / SB / BB position badges
- Cards face-down during play, revealed at showdown
- Winner highlighting with hand rank and payout
- Step-by-step log of the hand processing

### Controls

| Button | Action |
|--------|--------|
| **Next Step** | Advance one state machine step |
| **Auto Play** | Automatically cycle through steps |
| **Speed** (1s/0.5s/0.25s/2s) | Auto-play interval |
| **Reset** | Refresh table state |

---

## Project Structure

```
tech-assignment/
├── docker-compose.yml              # All services with profiles
├── .env.example                    # Environment variable defaults
├── infrastructure/
│   ├── elasticmq.conf              # SQS queue definitions
│   └── mysql-init/
│       └── 01-schema.sql           # Database schema + seed data
├── scripts/
│   ├── init-dynamodb.sh            # Create DynamoDB tables
│   ├── seed-rewards.js             # Seed rewards data (Option A)
│   ├── seed-streaks.js             # Seed streaks data (Option C)
│   └── simulate-hands.js           # Send SQS messages (Option B)
├── ui/
│   └── index.html                  # Poker hand viewer (Option B)
├── serverless-v2/
│   ├── shared/                     # Shared code across all services
│   │   ├── config/                 # db.js, redis.js, dynamo.js, logger.js
│   │   ├── utils/                  # Common helpers (toMoney, etc.)
│   │   └── games/common/           # Poker logic
│   │       ├── constants.js        # GAME_HAND (0–16), PLAYER_STATUS, ACTION
│   │       ├── cards.js            # Deck, shuffle, deal, hand evaluation
│   │       ├── betting.js          # Bet processing
│   │       ├── players.js          # Seat/player management
│   │       └── pots.js             # Main/side pot calculation
│   └── services/
│       ├── holdem-processor/       # Option B: Hand processing Lambda
│       ├── cash-game-broadcast/    # Option B: EventBridge → WebSocket
│       ├── rewards-api/            # Option A: Rewards backend
│       ├── rewards-frontend/       # Option A: React dashboard (Vite)
│       ├── streaks-api/            # Option C: Streaks backend
│       └── streaks-frontend/       # Option C: React UI (Vite)
```

### Shared Code

All services mount `serverless-v2/shared/` for access to common config and game logic. In Docker, it's mounted at `/app/shared`. Locally, each service has a symlink: `shared -> ../../shared`.

---

## Running Tests

```bash
# Holdem processor (15 tests)
cd serverless-v2/services/holdem-processor && npm install && npm test

# Rewards API (1 test)
cd serverless-v2/services/rewards-api && npm install && npm test

# Streaks API (1 test)
cd serverless-v2/services/streaks-api && npm install && npm test
```

---

## Useful Commands

```bash
# Check which containers are running
docker compose ps

# View logs for a specific service
docker compose logs holdem-processor --tail 50 -f

# Restart a single service (picks up code changes)
docker compose restart holdem-processor

# Process one hand step manually (Option B)
curl -X POST http://localhost:3030/process \
  -H 'Content-Type: application/json' \
  -d '{"tableId": 1}'

# Read current table state (Option B)
curl http://localhost:3030/table/1

# Connect to MySQL
docker compose exec mysql mysql -uhijack -phijack_dev hijack_poker

# Reset game state (Option B)
docker compose exec mysql mysql -uhijack -phijack_dev hijack_poker \
  -e "DELETE FROM game_players; DELETE FROM games;"
```

---

## Database

### MySQL Schema (Options A & B)

The `infrastructure/mysql-init/01-schema.sql` file creates tables and seed data on first run:

| Table | Purpose |
|-------|---------|
| `players` | 6 seeded players (Alice, Bob, Charlie, Diana, Eve, Frank) |
| `game_tables` | 2 poker tables (Starter Table 1/2 blinds, High Stakes 5/10) |
| `games` | Hand state: step, dealer, blinds, community cards, deck, pot, winners |
| `game_players` | Per-hand player state: seat, stack, cards, bets, action, winnings |
| `game_stats` | Aggregate stats per player per table |
| `ledger` | Financial transactions |

### DynamoDB Tables (Options A & C)

Created by `scripts/init-dynamodb.sh` (also run by the `dynamodb-init` container on startup):

- `rewards-players` — Player tier and points
- `rewards-transactions` — Points transaction history
- `rewards-leaderboard` — Monthly leaderboard
- `rewards-notifications` — Player notifications
- `streaks-players` — Streak state
- `streaks-activity` — Daily check-in records
- `streaks-rewards` — Streak milestone rewards
- `streaks-freeze-history` — Freeze usage history
- `connections` — WebSocket connection tracking (Option B)

---

## Port Reference

| Service | Port | Profile |
|---------|------|---------|
| MySQL | 3306 (or `MYSQL_EXTERNAL_PORT`) | core |
| Redis | 6379 (or `REDIS_EXTERNAL_PORT`) | core |
| DynamoDB Local | 8000 (or `DYNAMODB_EXTERNAL_PORT`) | core |
| ElasticMQ (SQS) | 9324 | engine |
| EventBridge Mock | 4010 | engine |
| Holdem Processor | 3030 | engine |
| Cash Game Broadcast | 3032 | engine |
| Hand Viewer | 8080 (or `HAND_VIEWER_PORT`) | engine |
| Rewards API | 5000 | rewards |
| Rewards Frontend | 4000 | rewards |
| Streaks API | 5001 | streaks |
| Streaks Frontend | 4001 | streaks |

### Port Conflicts

If you have other services running on these ports, edit `.env` to remap the external ports:

```bash
# Example: remap core services to avoid conflicts
MYSQL_EXTERNAL_PORT=3307
REDIS_EXTERNAL_PORT=6380
DYNAMODB_EXTERNAL_PORT=8001
```

---

## Troubleshooting

**Containers take a long time on first start?**
- Normal. Each service container runs `npm install` on first boot. Subsequent restarts are faster because `node_modules` is cached in the container volume.

**MySQL connection refused?**
- MySQL takes ~15 seconds to initialize on first run. Other services wait for its health check before starting. Check status: `docker compose ps`

**Port already in use?**
- Another service is using the port. Remap in `.env` (see [Port Conflicts](#port-conflicts)).

**Lambda timeout errors?**
- Default Lambda timeout is 30 seconds. If you see `[504] Lambda timeout`, your function is likely hanging on an external call. Check EventBridge/MySQL connectivity in the logs.

**Changes not picked up?**
- Service code is volume-mounted, but serverless-offline doesn't hot-reload. Restart the service: `docker compose restart <service-name>`

**Want a completely fresh start?**
```bash
docker compose --profile <your-profile> down -v
docker compose --profile <your-profile> up
```
This removes all database volumes and reinitializes from scratch.

**Tests fail with "Cannot find module"?**
- Run `npm install` in the service directory first. Docker installs deps inside the container, but local test runs need local `node_modules`.
