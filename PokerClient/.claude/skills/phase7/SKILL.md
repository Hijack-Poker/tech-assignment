---
name: phase7
description: Final verification — Docker end-to-end test, multi-hand play-through, unit test run, README update. Submission readiness check.
---

# Phase 7: Docker Verification + README

Read CLAUDE.md before starting. All previous phases must be complete.

## Goal
End-to-end working with the Docker backend. All Must Have acceptance criteria met. Clean console. Submission ready.

---

## Step 1: Start Docker Backend

```bash
cd ~/Desktop/Gauntlet/tech-assignment
docker compose --profile engine up -d
```

Verify:
```bash
curl http://localhost:3030/health
# Expected: {"service":"holdem-processor","status":"ok","timestamp":"..."}
```

If Docker isn't running or `curl` fails → fix before proceeding.

---

## Step 2: Run Unit Tests

In Unity Editor:
- Window → General → Test Runner → EditMode tab
- Run All

**All tests must pass.** If any fail, fix before moving to Step 3.

Tests that must pass:
- `ApiClientTests` (all deserialization tests)
- `GameStateTests` (IsShowdown, IsHandComplete, PlayerState convenience props)
- `CardUtilsTests` (ParseCard, IsRedSuit, GetSuitSymbol, MoneyFormatter)

---

## Step 3: End-to-End Play-Through

With Docker running, press Play in Unity Editor:

**Manual verification checklist:**
- [ ] StatusBar shows "Connected" within 3 seconds
- [ ] Initial state renders on screen (table visible, seats populated)
- [ ] Click "Next Step" — step advances, phase label changes
- [ ] Click "Next Step" 15 more times — completes full hand (steps 0-15)
- [ ] At step 12 (Showdown) — hole cards flip face-up
- [ ] At step 13-14 — winner seat pulses gold, hand rank + winnings text appears
- [ ] At step 15 (Hand Complete) — history shows separator line
- [ ] Next click after step 15 — starts new hand (Hand #increments)
- [ ] Enable Auto Play at 1s speed — hand advances automatically
- [ ] Change speed to 0.25s — advances faster
- [ ] Stop Auto Play — stops immediately
- [ ] Multiple consecutive hands play without errors or hangs
- [ ] Hand history scrolls and shows all steps + winner summaries

**Console check (no errors):**
- Use `mcp__unity__read_console` or Unity Console window
- Zero error (red) messages during normal play
- Warnings acceptable if non-critical

---

## Step 4: Error Handling Verification

Stop the Docker containers:
```bash
docker compose --profile engine down
```

In Unity Play Mode:
- [ ] StatusBar shows red error "Cannot reach holdem-processor at localhost:3030"
- [ ] Error message appears within ~30 seconds (after 3 retries × 2s delay)
- [ ] Clicking Next Step shows error in StatusBar
- [ ] Auto-play stops on API failure

Restart Docker:
```bash
docker compose --profile engine up -d
```
- [ ] Unity reconnects (may require Next Step click or auto-play restart)

---

## Step 5: Update CLAUDE.md Phase Checklist

In `PokerClient/CLAUDE.md`, mark all phases complete:
```
- [x] Phase 0: Packages installed
- [x] Phase 1: API layer + models + unit tests
- [x] Phase 2: Managers + CardUtils + MoneyFormatter
- [x] Phase 3: Scene skeleton
- [x] Phase 4: Core views (Table, Seat, Card, HUD)
- [x] Phase 5: Controls + Hand History + Status Bar
- [x] Phase 6: Animations + polish
- [x] Phase 7: Docker verification + README
```

---

## Step 6: Write README.md

Create `PokerClient/README.md`:

```markdown
# HijackPoker — Unity Client

Unity 2022.3 LTS poker table client for the Gauntlet tech challenge (Option D).
Visualizes Texas Hold'em hands stepping through a 16-step state machine via the holdem-processor REST API.

## Prerequisites
- Unity 2022.3 LTS
- Docker Desktop + Docker Compose v2
- Packages: Newtonsoft JSON, DOTween, TextMeshPro (already installed)

## Running

### 1. Start the backend
```bash
cd ~/Desktop/Gauntlet/tech-assignment
docker compose --profile engine up -d
curl http://localhost:3030/health  # verify
```

### 2. Open in Unity
- Open Unity Hub → Add project → select `PokerClient/` folder
- Open `Assets/Scenes/PokerTable.unity`
- Press Play

### 3. Play
- Click **Next Step** to advance the hand one state at a time
- Click **Auto Play** to advance automatically (select speed: 0.25s / 0.5s / 1s / 2s)
- Watch the **Hand History** panel for a step-by-step log

## Architecture Decisions

| Decision | Choice | Reason |
|----------|--------|--------|
| UI system | uGUI (Canvas) | Battle-tested, extensive community examples |
| Async | UnityWebRequest + TaskCompletionSource | No extra package, built-in Unity |
| JSON | Newtonsoft JSON | Handles nested arrays and nullable types correctly |
| Pattern | Event-driven MVP | `TableStateManager` fires `OnTableStateChanged`; all views subscribe |
| Animation | DOTween | Industry-standard Unity tween library |
| Tests | NUnit Edit Mode | Testable without Play Mode; all 3 suites pass |

## Project Structure
```
Assets/Scripts/
  Api/          — PokerApiClient (REST calls)
  Models/       — TableResponse, GameState, PlayerState, HandHistoryEntry
  Managers/     — TableStateManager (state + events), GameManager (orchestration)
  UI/           — TableView, SeatView, CardView, CommunityCardsView, HudView,
                  ControlsView, HandHistoryView, StatusBarView
  Utils/        — CardUtils (parse "AH" → rank/suit), MoneyFormatter
Tests/EditMode/ — ApiClientTests, GameStateTests, CardUtilsTests
```

## Known Limitations
- Table ID is hardcoded to `1` (configurable via Inspector)
- No card sprites — rank + suit symbol on colored rectangles
- Editor play mode only — no build/deployment configuration
```

---

## Must Have Acceptance Criteria (final check)

| # | Requirement | Status |
|---|-------------|--------|
| 1 | Poker table renders with 6 player seats around a felt surface | [ ] |
| 2 | API client connects to holdem-processor at localhost:3030 | [ ] |
| 3 | POST /process advances the hand by one step | [ ] |
| 4 | GET /table/1 fetches and displays current state | [ ] |
| 5 | Community cards appear incrementally (3/4/5) | [ ] |
| 6 | Hole cards face-down during play, revealed at showdown | [ ] |
| 7 | Player name, stack, bet, action display correctly | [ ] |
| 8 | Pot amount updates in center | [ ] |
| 9 | Dealer / SB / BB badges display correctly | [ ] |
| 10 | Winner highlighting with hand rank + payout | [ ] |
| 11 | Stack amounts reflect winnings after payout | [ ] |
| 12 | Next Step button triggers one state advance | [ ] |
| 13 | Phase label shows human-readable step name | [ ] |
| 14 | Unit tests pass on API client + data model deserialization | [ ] |
| 15 | Docker Compose engine profile starts backend, Unity connects | [ ] |

All 15 must be checked before submission.
