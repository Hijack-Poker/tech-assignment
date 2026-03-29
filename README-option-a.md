# Option A: Rewards System

## Why Option A

The rewards system is a data-heavy, full-stack challenge that exercises DynamoDB modeling, Redis caching, NestJS module architecture, and React state management — all areas I wanted to demonstrate depth in. It also has clear, testable business rules (points engine, tier progression, monthly reset) that lend themselves to comprehensive unit and integration testing.

---

## Setup

```bash
cp .env.example .env
docker compose --profile rewards up
```

| Service | URL |
|---------|-----|
| Rewards API | http://localhost:5000/api/v1/health |
| Rewards Frontend | http://localhost:4000 |
| DynamoDB Admin | http://localhost:8001 |

The seed container automatically creates 50 players with realistic point distributions across tiers.

---

## What's Implemented

### Core (Must Have)

| Requirement | Status | Details |
|-------------|--------|---------|
| Points awarded by stakes + tier multiplier | Done | `PointsService.awardPoints()` — base points from big-blind bracket, multiplied by tier multiplier |
| Tier progression with immediate upgrades | Done | Evaluated on every hand via `getTierForPoints(totalEarned)` |
| Immutable points ledger | Done | Append-only `rewards-transactions` table, no updates or deletes |
| Leaderboard (top players, monthly) | Done | Redis sorted sets, batch DynamoDB lookup for display names and stored tiers |
| Dashboard UI | Done | Player card, leaderboard, activity feed, notification bell, simulation controls |
| Tier change notifications | Done | Upgrade and downgrade notifications written to DynamoDB on tier change |
| REST API for Unity client | Done | Full REST API with `api/v1` prefix, CORS enabled, documented response shapes |
| Admin endpoints | Done | Separate `AdminModule` — view player, adjust points, override tier, leaderboard |
| Unit tests on points + tier logic | Done | 180 backend tests across 13 suites |
| Docker Compose + tests passing | Done | `docker compose --profile rewards up` boots everything; tests run in-container |

### Expected (Should Have)

| Requirement | Status | Details |
|-------------|--------|---------|
| Points history pagination | Done | Cursor-based pagination via `GET /player/history?limit=20&cursor=...` |
| Leaderboard shows player's rank | Done | `playerRank` field in response + `?view=nearby` for surrounding players |
| Notification dismiss | Done | `PATCH /player/notifications/:id/dismiss` |
| Monthly reset logic | Done | `POST /dev/monthly-reset` — tier floor protection (max 1 tier drop), downgrade notifications, tier history snapshots, leaderboard rotation |
| Tier timeline (6 months) | Done | `GET /dev/tier-history/:playerId` — includes mid-month changes (upgrades/downgrades from gameplay), not just monthly snapshots |
| Integration / e2e tests | Done | `monthly-reset.e2e.spec.ts` — boots full NestJS app against live DynamoDB + Redis |
| API response documentation | Done | TypeScript interfaces in `shared/types/rewards.ts`, documented in this file below |

### Bonus (Could Have)

| Requirement | Status | Details |
|-------------|--------|---------|
| Leaderboard caching | Done | Redis sorted sets are the primary data source — O(log N) rank queries |
| Milestone notifications | Done | First hand, 100 hands, 500 hands, 1,000 points — triggered on threshold crossing |
| Live simulation | Done | Toggle in dashboard to simulate other players earning points in real time |
| Frontend component tests | Done | 68 tests across 10 suites (Vitest + Testing Library) |

### Not Implemented (Out of Scope per spec)

- Full auth system — using `X-Player-Id` header stub (spec says "stub JWT guard")
- Email/push notification delivery — in-app only
- Game engine integration — points awarded via REST, not from game processor
- Unity client — REST API is ready for it, but no Unity project
- Admin backoffice UI — admin endpoints exist, no dedicated admin frontend
- Production deployment — local Docker Compose only

---

## Architecture

### Backend (NestJS)

```
rewards-api/
├── src/
│   ├── app.module.ts              # Root module — imports all feature modules
│   ├── config/constants.ts        # Tier definitions, stakes brackets, milestones
│   ├── dynamo/                    # DynamoDB service (shared data access layer)
│   ├── redis/                     # Redis service (leaderboard sorted sets)
│   ├── points/                    # PointsModule — award points, leaderboard
│   ├── player/                    # PlayerModule — auth'd player self-service
│   ├── admin/                     # AdminModule — CS/ops tools (cross-player)
│   ├── dev/                       # DevModule — demo tools (no auth, removed in prod)
│   ├── health/                    # HealthModule — liveness check
│   └── filters/                   # Custom exception filters
├── handler.ts                     # Serverless entry point (Express adapter)
└── __tests__/                     # Unit + e2e tests
```

Each module is self-contained with its own controller, service, and DTOs. `AdminModule` and `DevModule` are independent — either can be removed without affecting the other or core gameplay.

### Frontend (React + Vite + MUI)

```
rewards-frontend/
├── src/
│   ├── pages/Dashboard.tsx        # Main layout — three-panel with header controls
│   ├── components/
│   │   ├── PlayerCard.tsx         # Tier badge, points, progress bar, play hand, tier timeline
│   │   ├── Leaderboard.tsx        # Top-10 + nearby view with polling
│   │   ├── ActivityFeed.tsx       # Recent transactions with auto-scroll
│   │   ├── NotificationBell.tsx   # Unread badge + popover with dismiss
│   │   ├── TierTimeline.tsx       # Vertical timeline of tier progression
│   │   ├── AdjustPointsModal.tsx  # Dev tool for setting exact point values
│   │   ├── MonthlyResetButton.tsx # Trigger monthly reset with confirmation dialog
│   │   └── SimulationControls.tsx # Toggle live multi-player simulation
│   ├── api/client.ts              # Axios instance with X-Player-Id header
│   ├── hooks/usePolling.ts        # Generic polling hook
│   └── constants.ts               # Tier colors, thresholds, stakes options
```

### Data Flow

```
Play Hand → POST /points/award
  → Look up player (DynamoDB)
  → Calculate: basePoints(bigBlind) × tierMultiplier
  → Write transaction (DynamoDB, append-only)
  → Update player record (points, totalEarned, handsPlayed, tier)
  → Check tier change → write notification + tier history
  → Check milestones → write milestone notifications
  → Update leaderboard (Redis sorted set, fire-and-forget)
  → Return response with new state
```

---

## Key Design Decisions

### Redis for Leaderboard, Not DynamoDB

The challenge spec scaffolded a `rewards-leaderboard` DynamoDB table, but Redis sorted sets are a better fit:
- **O(log N) rank queries** — `ZREVRANK` gives a player's position instantly
- **O(log N + M) range queries** — `ZREVRANGE` returns top-N or a window around a rank
- **Atomic score updates** — `ZADD` is a single operation, no read-modify-write
- Redis is already a hard dependency across the platform (holdem-processor, cash-game-broadcast), so it adds no new infrastructure risk. If Redis data is ever lost, the `rewards-players` table has `points` and can rebuild the sorted set.

### Tier History Sort Key: `createdAt` Instead of `monthKey`

The scaffolding implied one tier-history entry per month (sort key: `YYYY-MM`). We changed to `createdAt` (ISO timestamp) because:
- **Mid-month changes are visible** — upgrades and downgrades from gameplay appear as distinct timeline entries
- **No silent overwrites** — with `monthKey`, a second tier change in the same month would silently replace the first
- **Natural uniqueness** — ISO timestamps are unique without synthetic suffixes
- **Lexicographic sorting** — ISO strings sort correctly as strings, so DynamoDB range queries work as expected

### Leaderboard Tier Display: Stored Tier, Not Score-Derived

After a monthly reset, all players have 0 points. A naive implementation would compute tier from score (`getTierForPoints(0)` → Bronze), making every player appear as Bronze on the leaderboard. Instead, we batch-fetch player records from DynamoDB and use the stored `tier` field. This correctly shows a Gold player with 0 monthly points as Gold, not Bronze.

### Self-Contained Service Modules

`AdminService`, `DevService`, and `PlayerService` each implement their own player-lookup and response-building logic rather than sharing a common service. This is intentional:
- **Independent removal** — `DevModule` can be deleted for production without touching `AdminModule`
- **No cross-module coupling** — each module depends only on `DynamoModule` and `RedisModule`
- **Minimal blast radius** — a change to admin response shape doesn't risk breaking player-facing endpoints
- The duplication is ~30 lines per service — acceptable for the isolation benefit

### Monthly Reset: Tier Floor Protection

Players can only drop one tier per monthly reset (`Math.max(1, currentTier - 1)`). This prevents a Platinum player from falling straight to Bronze after an inactive month, which would feel punitive and discourage re-engagement. The reset also preserves `totalEarned` (lifetime points), so an inactive Platinum player downgraded to Gold will re-promote on their very first hand back.

---

## API Reference

All endpoints are prefixed with `/api/v1`. The API uses NestJS validation pipes — invalid requests receive a `400` with field-level error messages.

### Points

#### `POST /points/award`

Award points for a completed hand. Requires `X-Player-Id` header.

```json
// Request
{
  "tableId": 1,
  "tableStakes": "$1/$2",
  "bigBlind": 2.00,
  "handId": "unique-hand-id"
}

// Response (201)
{
  "playerId": "player-001",
  "earnedPoints": 15,
  "newPoints": 1250,
  "newTotalEarned": 8430,
  "tier": "Gold",
  "transaction": {
    "timestamp": 1711670400000,
    "type": "gameplay",
    "basePoints": 10,
    "multiplier": 1.5,
    "earnedPoints": 15,
    "tableId": 1,
    "tableStakes": "$1/$2",
    "balanceAfter": 1250
  }
}
```

**Points calculation:**

| Big Blind | Base Points |
|-----------|-------------|
| ≤ $0.25 | 1 |
| ≤ $1.00 | 2 |
| ≤ $5.00 | 5 |
| > $5.00 | 10 |

Earned points = `basePoints × tierMultiplier` (rounded).

#### `GET /points/leaderboard?limit=10&view=top&month=2026-03`

Returns leaderboard rankings. Requires `X-Player-Id` header (used to calculate the requesting player's rank).

| Param | Default | Description |
|-------|---------|-------------|
| `limit` | 10 | Number of entries (max 100) |
| `view` | `top` | `top` for highest scores, `nearby` for ±5 around player's rank |
| `month` | current | `YYYY-MM` format |

```json
// Response (200)
{
  "leaderboard": [
    { "rank": 1, "playerId": "player-042", "displayName": "AceHigh", "tier": "Platinum", "points": 3420 },
    { "rank": 2, "playerId": "player-017", "displayName": "RiverRat", "tier": "Gold", "points": 2890 }
  ],
  "playerRank": 15
}
```

### Player (authenticated via X-Player-Id)

#### `GET /player/rewards`

```json
// Response (200)
{
  "playerId": "player-001",
  "tier": "Gold",
  "points": 1250,
  "totalEarned": 8430,
  "handsPlayed": 342,
  "nextTierAt": 10000,
  "nextTierName": "Platinum",
  "recentTransactions": [...]
}
```

#### `GET /player/history?limit=20&cursor=...`

Paginated transaction history. Returns a cursor for the next page.

```json
// Response (200)
{
  "transactions": [...],
  "total": 342,
  "limit": 20,
  "cursor": "eyJwbGF5ZXJJZCI6Li4ufQ=="
}
```

#### `GET /player/notifications?unread=true`

```json
// Response (200)
{
  "notifications": [
    {
      "notificationId": "01HYX...",
      "type": "tier_upgrade",
      "title": "Upgraded to Gold!",
      "description": "Congratulations! You've reached Gold tier with 2,150 points. Enjoy your 1.5x point multiplier!",
      "dismissed": false,
      "createdAt": "2026-03-15T10:30:00.000Z"
    }
  ],
  "unreadCount": 3
}
```

#### `PATCH /player/notifications/:id/dismiss`

```json
// Response (200)
{ "success": true }
```

### Admin

#### `GET /admin/players/:playerId/rewards`

Same as player rewards but includes `createdAt` and `updatedAt` internal timestamps. No auth required (would be gated in production).

#### `POST /admin/points/adjust`

Delta-based point adjustment with audit trail.

```json
// Request
{ "playerId": "player-001", "points": -500, "reason": "Duplicate transaction correction" }

// Response (201) — PlayerRewardsResponse with updated values
```

#### `GET /admin/leaderboard?limit=50&month=2026-03`

Same shape as player leaderboard but exposes raw `playerId` values for CS lookup.

#### `POST /admin/tier/override`

Manual tier override for VIP/compensation scenarios.

```json
// Request
{ "playerId": "player-001", "tier": 4, "expiry": "2026-04-01T00:00:00.000Z" }

// Response (201) — PlayerRewardsResponse with overridden tier
```

### Dev (demo/testing only)

#### `GET /dev/player/:playerId`

Unauthenticated player lookup (no `X-Player-Id` required).

#### `PUT /dev/player/:playerId/points`

Set exact point values, bypassing delta logic.

```json
// Request
{ "points": 5000, "totalEarned": 15000, "reason": "Demo setup" }
```

#### `POST /dev/monthly-reset`

Trigger the monthly reset cycle on demand.

```json
// Response (201)
{ "processed": 50, "downgrades": 12, "resetMonth": "2026-03" }
```

#### `GET /dev/tier-history/:playerId`

Tier progression timeline for the last 6 months.

```json
// Response (200)
{
  "playerId": "player-001",
  "history": [
    { "monthKey": "2026-01", "tier": "Silver", "points": 780, "totalEarned": 780, "reason": "monthly_reset", "createdAt": "2026-01-29T..." },
    { "monthKey": "2026-02", "tier": "Gold", "points": 2150, "totalEarned": 2930, "reason": "tier_change", "createdAt": "2026-02-14T..." }
  ]
}
```

---

## Testing

### Backend (180 tests, 13 suites)

```bash
docker compose exec rewards-api npx jest
```

| Suite | Tests | What's Covered |
|-------|-------|----------------|
| `constants.spec` | 28 | Tier lookup, points brackets, milestone triggers, edge cases |
| `dynamo.service.spec` | 20 | All DynamoDB operations (get/update/batch/query/notifications/tier-history) |
| `redis.service.spec` | 9 | Leaderboard sorted set operations, rank queries |
| `points.service.spec` | 42 | Points calculation, tier upgrades/downgrades, milestone notifications, leaderboard tier display |
| `player.service.spec` | 22 | Player rewards, paginated history, notification retrieval + dismiss |
| `admin.service.spec` | 21 | Admin player lookup, point adjustments with tier history, leaderboard, tier overrides |
| `dev.service.spec` | 12 | Dev player lookup, set points with tier change, tier history retrieval |
| `reset.service.spec` | 14 | Monthly reset: tier floor protection, downgrade notifications, point zeroing, edge cases |
| `auth.guard.spec` | 4 | X-Player-Id header validation |
| `award-points.dto.spec` | 4 | Request validation (missing fields, invalid types) |
| `not-found.filter.spec` | 2 | Custom 404 error normalization |
| `health.e2e.spec` | 1 | Liveness endpoint |
| `monthly-reset.e2e.spec` | 1 | Full reset flow against live DynamoDB + Redis |

### Frontend (68 tests, 10 suites)

```bash
docker compose exec rewards-frontend npx vitest run
```

| Suite | Tests | What's Covered |
|-------|-------|----------------|
| `PlayerCard.test` | 9 | Tier display, points, progress bar, play hand, max tier state |
| `Leaderboard.test` | 7 | Top view, nearby view, polling, rank display |
| `ActivityFeed.test` | 8 | Transaction rendering, auto-scroll, empty state |
| `AdjustPointsModal.test` | 9 | Point adjustment form, validation, API calls |
| `TierTimeline.test` | 8 | Multi-entry timeline, date labels, "Now" entry, tier change indicators |
| `NotificationBell.test` | 5 | Badge count, popover, dismiss, empty state |
| `SimulationControls.test` | 6 | Toggle state, button labels |
| `MonthlyResetButton.test` | 5 | Confirmation dialog, API call, success/error feedback |
| `constants.test` | 6 | Tier thresholds, stakes mapping, ID generation |
| `usePolling.test` | 5 | Polling interval, cleanup, immediate fetch |

---

## DynamoDB Schema

| Table | PK | SK | Purpose |
|-------|----|----|---------|
| `rewards-players` | `playerId` | — | Player tier, points, totalEarned, handsPlayed |
| `rewards-transactions` | `playerId` | `timestamp` (ULID) | Immutable transaction ledger |
| `rewards-notifications` | `playerId` | `notificationId` (ULID) | Tier change and milestone notifications |
| `rewards-tier-history` | `playerId` | `createdAt` (ISO) | Tier progression snapshots for timeline |

ULIDs are used for sort keys on transactions and notifications — they're time-ordered and globally unique, so newest-first queries use `ScanIndexForward: false`.
