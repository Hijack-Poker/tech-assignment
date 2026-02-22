# Hijack Poker — Technical Assignment

Welcome to the Hijack Poker technical challenge! This repo provides a working serverless infrastructure skeleton that mirrors our production architecture. Your job is to build one of three challenge options on top of it.

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (with Docker Compose v2)
- [Node.js 22+](https://nodejs.org/)
- Git

## Challenge Options

| Option | Challenge | Stack |
|--------|-----------|-------|
| **A** | Rewards System | React + Serverless API + DynamoDB |
| **B** | Bomb Pots | Game Engine Pipeline (SQS → Lambda → EventBridge) |
| **C** | Daily Streaks | React + Serverless API + DynamoDB |

Read the full challenge docs at: **[Challenge Documentation](https://hijack-poker.github.io/tech-assignment/)**

## Quick Start

### 1. Clone & configure

```bash
git clone <this-repo>
cd tech-assignment
cp .env.example .env
```

### 2. Start your challenge

```bash
# Option A: Rewards System
docker compose --profile rewards up

# Option B: Bomb Pots (Engine Pipeline)
docker compose --profile engine up

# Option C: Daily Streaks
docker compose --profile streaks up
```

### 3. Verify it's running

**Option A — Rewards:**
- API health: http://localhost:5000/local/api/v1/health
- Frontend: http://localhost:4000

**Option B — Engine Pipeline:**
- Processor: http://localhost:3030/local/health
- Broadcast: http://localhost:3032/local/health
- Simulate hands: `node scripts/simulate-hands.js`

**Option C — Streaks:**
- API health: http://localhost:5001/local/api/v1/health
- Frontend: http://localhost:4001

### 4. Seed test data (optional)

```bash
# For Option A
node scripts/seed-rewards.js

# For Option C
node scripts/seed-streaks.js
```

### 5. Stop everything

```bash
docker compose down -v
```

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│  Docker Compose Profiles                                    │
│                                                             │
│  core:    MySQL │ Redis │ DynamoDB Local                    │
│                                                             │
│  engine:  core + ElasticMQ (SQS) + Holdem Processor        │
│           + Cash Game Broadcast + EventBridge Mock          │
│                                                             │
│  rewards: core + Rewards API (Serverless) + React Frontend  │
│                                                             │
│  streaks: core + Streaks API (Serverless) + React Frontend  │
└─────────────────────────────────────────────────────────────┘
```

### Engine Pipeline (Option B)

```
simulate-hands.js → SQS (ElasticMQ) → Holdem Processor → EventBridge Mock → Cash Game Broadcast
```

### API + Frontend (Options A & C)

```
React Frontend (Vite) → Serverless API (serverless-offline) → DynamoDB Local
```

## Project Structure

```
serverless-v2/
├── shared/                     # Shared code (mounted into service containers)
│   ├── config/                 # DB, Redis, DynamoDB, Logger
│   ├── utils/                  # Common helpers
│   └── games/common/           # Poker game logic (constants, cards, betting, pots)
└── services/
    ├── holdem-processor/       # Option B: Hand processing pipeline
    ├── cash-game-broadcast/    # Option B: WebSocket broadcast
    ├── rewards-api/            # Option A: Rewards backend
    ├── rewards-frontend/       # Option A: Rewards React dashboard
    ├── streaks-api/            # Option C: Streaks backend
    └── streaks-frontend/       # Option C: Streaks React UI
```

## Running Tests

```bash
cd serverless-v2/services/holdem-processor && npm test
cd serverless-v2/services/rewards-api && npm test
cd serverless-v2/services/streaks-api && npm test
```

## Shared Code

All serverless services mount `serverless-v2/shared/` for access to common config and game logic. In the Docker containers, the shared directory is mounted at `/app/shared`. Locally, each service has a symlink: `shared -> ../../shared`.

Key shared modules:
- `shared/config/db.js` — Sequelize MySQL connection
- `shared/config/redis.js` — ioredis client
- `shared/config/dynamo.js` — DynamoDB DocumentClient
- `shared/games/common/constants.js` — Game enums (GAME_HAND, PLAYER_STATUS, ACTION)
- `shared/games/common/cards.js` — Deck, shuffle, deal, hand evaluation
- `shared/games/common/betting.js` — Bet processing and validation
- `shared/games/common/pots.js` — Main/side pot calculation

## Troubleshooting

**Containers won't start?**
- Make sure Docker Desktop is running
- Try `docker compose down -v` then start again

**MySQL connection refused?**
- Wait for the health check — MySQL takes ~15s to initialize on first run

**DynamoDB tables missing?**
- The `dynamodb-init` container creates them automatically
- Check its logs: `docker compose logs dynamodb-init`

**Port conflicts?**
- Edit ports in `.env` (copy from `.env.example`)
