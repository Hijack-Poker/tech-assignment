# Presearch: Option D — Unity Poker Game Client

All architectural decisions, trade-off analysis, failure modes, and gap analysis.
Produced via 5-loop presearch before any code was written.

---

## Loop 1: Constraints

### 1.1 Domain & Use Cases
- **Problem**: Visualize a poker hand playing through a 16-step state machine driven by the holdem-processor REST API
- **Users**: Evaluators + developers running the Unity Editor (no production deployment required)
- **Core use cases**:
  1. Step through a hand one state at a time (Next Step)
  2. Watch hands auto-play at configurable speed
  3. See card reveals, winner highlights, and stack payouts
  4. Review hand history in a scrollable log
- **Greenfield Unity project** consuming an existing API — no backend changes

### 1.2 Scale & Performance
| Requirement | Target |
|-------------|--------|
| API calls | Sequential, 1 at a time (not concurrent) |
| Latency | < 100ms for UI update after API response |
| Concurrent users | 1 (local Editor play) |
| Data volume | 6 players, 5 community cards, 16 steps per hand |
| Auto-play interval | 0.25s / 0.5s / 1s / 2s |

### 1.3 Budget & Cost
| Category | Budget |
|----------|--------|
| Total spend | $0 — open source tools only |
| API costs | $0 — local holdem-processor |
| Infrastructure | $0 — local Docker |
| Deployment | Unity Editor play mode only |

### 1.4 Time to Ship
| Milestone | Focus |
|-----------|-------|
| MVP | FR-1 table view + FR-2 Next Step + FR-3 cards + FR-4 HUD + unit tests |
| Full release | FR-5 winner animations + FR-6 hand history + auto-play + error handling |

### 1.5 Data Sensitivity
- No PII, no external APIs — all data is synthetic poker hand state
- Local Docker only — no data residency concerns

### 1.6 Team & Skill Constraints
| Technology | Level |
|------------|-------|
| Unity 2022.3 | Comfortable |
| C# / async-await | Comfortable |
| uGUI (Canvas) | Comfortable |
| DOTween | Growing |
| Unity MCP tooling | New |

**Key constraint**: Unity MCP (CoplayDev/unity-mcp v9.5.3) used to control Unity Editor from Claude Code — HTTP transport at `127.0.0.1:8080`.

### 1.7 Reliability & Verification
- Cost of failure: demo looks broken, evaluator can't step through hands
- Non-negotiable verification: unit tests on API deserialization + game state logic
- Human-in-the-loop: none (auto-play is full automation)
- Error handling: must show user-visible feedback on API failures (connection refused, timeout)

---

## Loop 2: Architecture Decisions

### 2.1 Core Architecture Pattern — LOCKED: Event-driven MVP

| Pattern | Pros | Cons | Decision |
|---------|------|------|----------|
| **Event-driven MVP** | Decoupled views, testable managers, clear data flow | Slightly more boilerplate | ✅ CHOSEN |
| MVC | Familiar | Controller tends to bloat in Unity | ❌ |
| MVVM | Clean bindings | UI Toolkit required for data binding | ❌ |
| ScriptableObject events | Very decoupled | Complex for this scope | ❌ |

**Data flow:**
```
User clicks "Next Step"
  → GameManager.AdvanceStepAsync()
  → PokerApiClient.ProcessStepAsync(tableId=1)
  → PokerApiClient.GetTableStateAsync(tableId=1)
  → TableStateManager.SetState(TableResponse)
  → Event fires: OnTableStateChanged(TableResponse)
  → All views receive and redraw from new state
```

**Full-redraw strategy**: Every `OnTableStateChanged` causes all views to redraw from scratch. No delta patching. Simple and correct for this scale.

### 2.2 Tech Stack — LOCKED

| Layer | Choice | Alt 1 | Alt 2 | Rationale |
|-------|--------|-------|-------|-----------|
| Engine | Unity 2022.3 LTS | Unity 6 | — | LTS stability |
| UI System | uGUI (Canvas) | UI Toolkit | — | Battle-tested, more examples |
| HTTP | UnityWebRequest | HttpClient | UniTask | Built-in, no extra package |
| Async bridge | TaskCompletionSource | UniTask | Coroutines | No extra package needed |
| JSON | Newtonsoft JSON | JsonUtility | System.Text.Json | Handles nested arrays + nullables |
| Text | TextMeshPro | Unity UI Text | — | Required by evaluators |
| Animation | DOTween | LeanTween | iTween | Best Unity tween library |
| Testing | NUnit (Unity Test Framework) | — | — | Built-in, Edit Mode only |
| MCP | CoplayDev/unity-mcp v9.5.3 | — | — | Direct Unity Editor control |

### 2.3 Data Architecture

**Models:**
```
TableResponse
  ├── GameState          (id, tableId, tableName, gameNo, handStep, stepName,
  │                       dealerSeat, smallBlindSeat, bigBlindSeat,
  │                       communityCards[], pot, sidePots[], move,
  │                       status, smallBlind, bigBlind, maxSeats,
  │                       currentBet, winners[])
  │   ├── IsShowdown     → handStep >= 12
  │   └── IsHandComplete → stepName == "RECORD_STATS_AND_NEW_HAND"
  └── PlayerState[]      (playerId, username, seat, stack, bet, totalBet,
                          status, action, cards[], handRank, winnings)
      ├── IsActive        → status == "1"
      ├── IsFolded        → status == "11"
      ├── IsAllIn         → status == "12"
      ├── IsWinner        → winnings > 0
      └── HasCards        → cards != null && count > 0

HandHistoryEntry
  (int HandNo, int Step, string StepLabel, string[] WinnerNames,
   float PotSize, Dictionary<string,float> StackDeltas, DateTime Timestamp)
```

**State machine (16 steps):**
| Step | Name | Label | Community Cards |
|------|------|-------|----------------|
| 0 | GAME_PREP | Preparing Hand | 0 |
| 1 | SETUP_DEALER | Setting Up Dealer | 0 |
| 2 | SETUP_SMALL_BLIND | Posting Small Blind | 0 |
| 3 | SETUP_BIG_BLIND | Posting Big Blind | 0 |
| 4 | DEAL_CARDS | Dealing Hole Cards | 0 |
| 5 | PRE_FLOP_BETTING_ROUND | Pre-Flop Betting | 0 |
| 6 | DEAL_FLOP | Dealing Flop | 3 |
| 7 | FLOP_BETTING_ROUND | Flop Betting | 3 |
| 8 | DEAL_TURN | Dealing Turn | 4 |
| 9 | TURN_BETTING_ROUND | Turn Betting | 4 |
| 10 | DEAL_RIVER | Dealing River | 5 |
| 11 | RIVER_BETTING_ROUND | River Betting | 5 |
| 12 | AFTER_RIVER_BETTING_ROUND | Showdown | 5 |
| 13 | FIND_WINNERS | Evaluating Hands | 5 |
| 14 | PAY_WINNERS | Paying Winners | 5 |
| 15 | RECORD_STATS_AND_NEW_HAND | Hand Complete | 5 |

**Community card count**: Use `game.CommunityCards.Count` from API response directly — simpler, always correct.

**Showdown / card reveal logic:**
```csharp
bool isShowdown = game.HandStep >= 12;
bool showCards  = isShowdown || player.Winnings > 0;
```

**Player status codes:**
```
"1"  = Active
"2"  = Sitting Out
"3"  = Leaving
"4"  = Show Cards
"5"  = Post Blind
"6"  = Wait for BB
"11" = Folded
"12" = All-In
```

### 2.4 Service Topology
| Service | Port | Role |
|---------|------|------|
| holdem-processor | 3030 | REST API (Docker) |
| Unity Editor | — | Client (Play Mode) |
| Unity MCP HTTP server | 8080 | Claude Code → Unity control |

### 2.5 API Integration
| Endpoint | Method | Notes |
|----------|--------|-------|
| `/health` | GET | Connection check on startup |
| `/process` | POST | Body: `{"tableId": 1}` |
| `/table/{tableId}` | GET | Full state after each process call |

**Card format:** `"AH"` → rank = `card[..^1]`, suit = `card[^1..]`
**Red suits:** H, D | **Black suits:** C, S

### 2.6 UI Architecture
```
Canvas (Screen Space - Overlay)
├── TableView          ← oval green felt background
│   ├── Seat[0-5]      ← SeatView instances around the oval
│   │   ├── NameText, StackText, BetText, ActionText
│   │   ├── BadgeGroup (D / SB / BB)
│   │   └── CardGroup  ← 2x CardView (face-up or face-down)
│   └── CenterGroup
│       ├── CommunityCards ← 5x CardView slots
│       └── PotText
├── HudPanel           ← top bar
│   ├── PhaseLabel, HandNumberText, ActionText
│   └── ConnectionStatus
├── ControlsPanel      ← bottom bar
│   ├── NextStepButton, AutoPlayToggle
│   └── SpeedButtons (0.25s / 0.5s / 1s / 2s)
├── HistoryPanel       ← right side, ScrollRect
│   └── HistoryContent ← HandHistoryView entries
└── StatusBar          ← thin strip, error feedback
```

**Seat positions** (oval, clockwise from bottom-left, 1080p reference):
```
Seat 1: bottom-left   (230, 130)
Seat 2: bottom-right  (850, 130)
Seat 3: right         (1050, 400)
Seat 4: top-right     (850, 670)
Seat 5: top-left      (230, 670)
Seat 6: left          (30, 400)
```
(anchor: center of 1920×1080 canvas)

### 2.7 Observability
- `Debug.Log` all API responses (raw JSON) in development
- StatusBarView shows last error to user
- `isProcessing` bool prevents double-advance and is visible in Inspector

### 2.8 Testing Strategy
- **Edit Mode only** — no Play Mode tests required
- Mock HTTP responses via in-memory JSON strings
- Tests: `ApiClientTests`, `GameStateTests`, `CardUtilsTests`
- CI: Unity Test Framework, run on every push

---

## Loop 3: Failure Mode Analysis

| Failure Mode | Impact | Mitigation |
|-------------|--------|------------|
| API not ready (Docker starting) | Next Step fails | StatusBar shows "Connecting…", retry up to 3x |
| Connection refused (port 3030) | No state | StatusBar "Cannot reach holdem-processor" |
| Timeout (> 10s) | Stale state | `timeoutSeconds = 10`, error shown |
| `ProcessResponse.Success == false` | Step not advanced | Log error, show in StatusBar |
| Double-click Next Step | Double advance | `isProcessing` bool blocks re-entry |
| Auto-play during API call | Same double-advance | Same `isProcessing` guard |
| Null `communityCards` | NullRef crash | Init to `new List<string>()` in model |
| Empty `players` array | NullRef in SeatView | Null-check before rendering |
| `handRank` empty string | Empty label | Show nothing if empty |
| `winnings == 0` on winner | Wrong card reveal | Use `player.IsWinner` (winnings > 0) |
| MCP HTTP server not started | Claude can't control Unity | Manual script copy fallback |

### Security
- No auth required (local-only)
- No user input to sanitize (read-only viewer)
- API keys: none
- MCP server: localhost-only, no external exposure

### Performance
- Full redraw on every state change — acceptable at 6 players + 5 cards
- DOTween animations run concurrently with next state poll (no blocking)
- Auto-play timer uses `WaitForSeconds` coroutine, cancelled on toggle-off

---

## Loop 4: Implementation Phases

See `PRD.md` for full phase breakdown with acceptance criteria.

**Build order rationale:**
1. API + models first (foundation, testable independently)
2. Managers second (state pipeline before any UI)
3. Scene skeleton third (layout before functionality)
4. Core views fourth (render state before interaction)
5. Controls + history fifth (interaction + log)
6. Animations last (polish, never blocks core function)

---

## Loop 5: Gap Analysis

### Gaps Found and Resolved

| Gap | Resolution |
|-----|-----------|
| `HandHistoryEntry` model not defined | Defined in 2.3 above |
| `GameManager` vs `TableStateManager` split ambiguous | GameManager owns API calls + timer; TableStateManager holds state + fires events |
| Error handling UI not designed | `StatusBarView` — thin persistent strip, non-blocking |
| Community card count logic ambiguous | Use `game.CommunityCards.Count` directly from API |
| Card face-down visual not specified | Dark rect + "?" TMP text, no sprites needed |
| Winner highlight mechanism | DOTween color pulse on seat background (white → gold, 3 loops) |
| Phase label animation | DOTween `DOPunchScale` on label text |
| `TableResponse.cs` inline vs. own file | Extract to own file for clarity |
| Connection retry not designed | Health check on startup + 3 retries on failure |
| Auto-play cancellation | `CancellationToken` pattern or simple `_isAutoPlaying` bool + coroutine stop |

### Decision Confidence

| Decision | Confidence | Risk if Wrong | Reversibility |
|----------|-----------|---------------|---------------|
| uGUI over UI Toolkit | High | Limited styling options | Medium — refactor views |
| TaskCompletionSource async bridge | High | None for this scope | Easy |
| Full-redraw on state change | High | Performance at scale | Easy — add delta patching later |
| DOTween for animations | High | None — standard library | Easy — swap tweens |
| Newtonsoft JSON | High | None — handles all edge cases | Easy |
| Edit Mode tests only | High | Play Mode bugs uncaught | Medium |

---

## Final Tech Stack Reference

| Concern | Decision |
|---------|----------|
| Engine | Unity 2022.3 LTS |
| UI | uGUI (Canvas-based) |
| Async | UnityWebRequest + TaskCompletionSource |
| JSON | Newtonsoft JSON (`com.unity.nuget.newtonsoft-json`) |
| Text | TextMeshPro |
| Animation | DOTween (Demigiant) |
| Architecture | Event-driven MVP |
| Tests | NUnit, Edit Mode only |
| Card reveal | `handStep >= 12 || player.winnings > 0` |
| Community cards | `game.CommunityCards.Count` |
| Error UI | StatusBarView (persistent strip) |
| MCP | CoplayDev/unity-mcp v9.5.3, HTTP @ 127.0.0.1:8080 |
| Backend | holdem-processor @ localhost:3030 (Docker) |
