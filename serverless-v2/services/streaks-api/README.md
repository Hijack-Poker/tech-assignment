# Streaks API

Daily streak tracking service for Hijack Poker. Tracks login and play streaks, awards milestone rewards, manages streak freezes, and provides calendar/leaderboard/analytics views. Runs as an Express app on Serverless Framework (Lambda) with DynamoDB.

## Quick Start

```bash
docker compose --profile streaks up
```

The API is available at `http://localhost:5001`. The frontend is at `http://localhost:5173`.

To seed 60 days of test data:

```bash
docker compose exec streaks-api npm run seed
```

## API Endpoints

### Public

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/v1/health` | Health check |

### Internal (no auth)

| Method | Path | Description |
|--------|------|-------------|
| POST | `/internal/streaks/hand-completed` | Record a completed hand (called by game processor) |

### Admin (no JWT auth -- MVP)

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/v1/admin/freezes/grant` | Grant additional freezes to a player |
| GET | `/api/v1/admin/analytics` | Platform-wide analytics (DAU/WAU/MAU, retention, tiers) |
| POST | `/api/v1/admin/debug/clear-exclusion` | Clear a player's self-exclusion (dev only) |
| POST | `/api/v1/admin/debug/time-travel` | Override server date for testing |
| GET | `/api/v1/admin/debug/time-travel` | Check current server date |

### Protected (JWT via `X-Player-Id` header)

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/v1/streaks/check-in` | Daily login check-in |
| GET | `/api/v1/streaks` | Get player's current streak state |
| GET | `/api/v1/streaks/leaderboard` | Top 50 players by streak score |
| GET | `/api/v1/streaks/share` | Player streak data formatted for sharing |
| GET | `/api/v1/player/streaks/calendar?month=YYYY-MM` | Monthly activity calendar heat map |
| GET | `/api/v1/player/streaks/rewards` | Player's milestone reward history |
| GET | `/api/v1/player/streaks/freezes` | Freeze balance and usage history |
| GET | `/api/v1/missions` | Today's daily missions |
| POST | `/api/v1/missions/:missionId/claim` | Claim a completed mission reward |
| GET | `/api/v1/player/responsible-gaming` | Responsible gaming settings |
| PUT | `/api/v1/player/responsible-gaming` | Update responsible gaming settings |
| POST | `/api/v1/player/responsible-gaming/self-exclude` | Self-exclude for N days |

## Architecture

```
Request
  -> handler.js (Express app, CORS, route mounting)
    -> middleware/auth.js (extracts playerId from header)
    -> routes/*.js (request validation, response shaping)
      -> services/*.js (business logic)
        -> dynamo.service.js (DynamoDB read/write operations)
          -> DynamoDB (5 tables)
```

The service follows a layered architecture:

- **Routes** handle HTTP concerns: parsing request params, validating input, formatting JSON responses.
- **Services** contain business logic: streak calculation, freeze evaluation, milestone checking, mission generation.
- **dynamo.service.js** is the data access layer wrapping all DynamoDB operations behind simple functions (`getPlayer`, `putPlayer`, `addActivity`, etc.).

The Express app is wrapped with `serverless-http` for Lambda deployment but runs standalone via `serverless-offline` in local development.

## Data Model

Five DynamoDB tables:

| Table | Partition Key | Sort Key | Purpose |
|-------|--------------|----------|---------|
| `streaks-players` | `playerId` | -- | Player profile: current/best streaks, freeze balance, settings |
| `streaks-activity` | `playerId` | `date` | Daily activity log (login, play, freeze used, streak snapshot) |
| `streaks-rewards` | `playerId` | `rewardId` | Milestone rewards earned |
| `streaks-freeze-history` | `playerId` | `date` | Freeze consumption history |
| `streaks-missions` | `playerId` | `date` | Daily mission state and progress |

Table names are configurable via environment variables (see `serverless.offline.yml`).

## Key Design Decisions

**UTC day boundaries.** All streak logic uses UTC calendar days (`YYYY-MM-DD`). This avoids timezone ambiguity and ensures consistent streak counting regardless of where the player connects from.

**Idempotent check-in.** If a player has already checked in today, the endpoint returns the current state with no side effects. The `lastLoginDate === today` guard makes duplicate calls safe.

**Lazy freeze evaluation.** Monthly free freezes are granted lazily at check-in time rather than via a scheduled job. When `lastFreezeGrantDate !== currentMonth`, the system grants the monthly freeze inline. This eliminates the need for a cron process.

**Combo multiplier.** When both login and play streaks are active, a combo multiplier applies: `1 + min(floor((loginStreak + playStreak) / 10), 5) * 0.1`. This caps at 1.5x and incentivizes maintaining both streaks.

**Dual streak tracks.** Login and play streaks are tracked independently. Login streaks increment on daily check-in. Play streaks increment when the game processor calls `/internal/streaks/hand-completed`. Each has its own milestone reward tier.

**Freeze mechanics.** Freezes preserve a streak when exactly one day is missed (`daysSinceLogin === 2`). Players receive 1 free freeze per month. Admins can grant additional freezes. Two or more consecutive missed days always break the streak.

## Testing

```bash
cd serverless-v2/services/streaks-api
npm test
```

199 tests across 17 test files covering:

- Check-in flow (idempotency, streak increment, freeze consumption, reset)
- Hand-completed internal endpoint
- Calendar date range queries
- Reward/milestone awarding
- Freeze service logic
- Admin endpoints (freeze grants, analytics, time travel)
- Auth middleware
- Seed data generation consistency
- DynamoDB repository operations
- Table schema validation

All tests use mocked DynamoDB -- no running database required.

## Seeding

The seed script generates 60 days of realistic activity data for `player-42`:

```bash
npm run seed
```

The pattern includes consecutive streaks, missed days, freeze usage, login-only days, and multiple streak resets. This produces milestone rewards at days 3, 7, and 14 across several streak instances. Requires DynamoDB Local to be running (handled by Docker Compose).

## Project Structure

```
streaks-api/
  handler.js                          # Express app entry point, route mounting
  serverless.offline.yml              # Serverless config for local development
  serverless.yml                      # Serverless config for AWS deployment
  package.json
  tsconfig.json
  scripts/
    seed.js                           # Test data seeder (60-day activity pattern)
  src/
    config/
      constants.js                    # Milestone definitions, getMilestone()
      milestones.ts                   # TypeScript milestone types
    middleware/
      auth.js                        # Auth middleware (X-Player-Id header)
    models/
      streak.model.ts                # TypeScript streak model
    repositories/
      dynamo.repository.ts           # Typed DynamoDB repository
    routes/
      admin.js                       # Admin: freeze grants, analytics, debug
      calendar.js                    # Monthly activity calendar
      check-in.js                    # Daily login check-in
      freezes.js                     # Freeze balance and history
      health.js                      # Health check
      internal.js                    # Internal: hand-completed
      missions.js                    # Daily missions
      responsible-gaming.js          # Session limits, self-exclusion
      rewards.js                     # Reward history
      streaks.js                     # Current streaks, leaderboard, share
    services/
      dynamo.service.js              # DynamoDB data access layer
      freeze.service.js              # Freeze grant logic
      missions.service.js            # Mission generation and claiming
      rewards.service.js             # Milestone reward awarding
      streak.service.js              # Streak calculation logic
    types/
      index.ts                       # Shared TypeScript types
  __tests__/
    admin.test.js
    auth.test.js
    calendar.test.js
    check-in.test.js
    constants.test.js
    dynamo.repository.test.ts
    dynamo.service.test.js
    freeze.service.test.js
    freezes.test.js
    hand-completed.test.js
    health.test.js                   # (in repo, not listed as untracked)
    milestones.test.ts
    rewards.service.test.js
    rewards.test.js
    seed.test.js
    streaks.test.js
    table-schemas.test.js
```
