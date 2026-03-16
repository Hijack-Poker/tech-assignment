# PRD: Option D — Unity Poker Game Client

Phased implementation plan. Every phase has a single goal, clear requirements, and testable acceptance criteria.

---

## Dependency Map

```
Phase 0 (Packages)
  └── Phase 1 (API + Models + Tests)
        └── Phase 2 (Managers + Utils)
              └── Phase 3 (Scene Skeleton)
                    └── Phase 4 (Core Views)
                          └── Phase 5 (Controls + History + Error)
                                └── Phase 6 (Animations + Polish)
                                      └── Phase 7 (Docker + README)
```

---

## Phase 0: Package Installation

**Goal:** Unity project has all required packages before any code is written.
**Depends on:** Unity project created at `~/Desktop/Gauntlet/PokerClient`

### Requirements
- [ ] Install `com.unity.nuget.newtonsoft-json` via Package Manager
- [ ] Install DOTween via Package Manager (Unity Asset Store or manual import)
- [ ] Import TextMeshPro Essentials (Window → TextMeshPro → Import TMP Essential Resources)
- [ ] Verify project compiles with no errors

### Acceptance Criteria
- `using Newtonsoft.Json;` compiles without error
- `using DG.Tweening;` compiles without error
- `using TMPro;` compiles without error
- No console errors on project open

---

## Phase 1: API Layer + Data Models + Unit Tests

**Goal:** A fully tested data pipeline that can call all 3 endpoints and deserialize responses.
**Depends on:** Phase 0

### Files to Create/Modify
| File | Action |
|------|--------|
| `Assets/Scripts/Api/PokerApiClient.cs` | Fill in 3 TODO methods + add retry/error handling |
| `Assets/Scripts/Models/GameState.cs` | Keep as-is |
| `Assets/Scripts/Models/PlayerState.cs` | Keep as-is |
| `Assets/Scripts/Models/TableResponse.cs` | Extract from GameState.cs into own file |
| `Assets/Scripts/Models/HandHistoryEntry.cs` | NEW |
| `Assets/Tests/EditMode/ApiClientTests.cs` | NEW |
| `Assets/Tests/EditMode/GameStateTests.cs` | NEW |
| `Assets/Tests/EditMode/CardUtilsTests.cs` | NEW (placeholder, CardUtils built Phase 2) |

### Requirements

#### PokerApiClient
- [ ] `GetHealthAsync()` — GET `/health`, return `HealthResponse`
- [ ] `ProcessStepAsync(int tableId)` — POST `/process` with `{"tableId": tableId}`, return `ProcessResponse`
- [ ] `GetTableStateAsync(int tableId)` — GET `/table/{tableId}`, return `TableResponse`
- [ ] On network failure: log error, return `null` (caller handles null)
- [ ] On `ProcessResponse.Success == false`: log error message
- [ ] `isProcessing` guard — public bool property, set true before API call, false after
- [ ] Startup health check with 3 retries, 2s delay between retries

#### HandHistoryEntry model
```csharp
public class HandHistoryEntry {
    public int HandNo;
    public int Step;
    public string StepLabel;
    public string[] WinnerNames;
    public float PotSize;
    public DateTime Timestamp;
}
```

#### Unit Tests
- [ ] `ApiClientTests`: Deserialize sample `/health` JSON → verify all fields
- [ ] `ApiClientTests`: Deserialize sample `/process` response → verify `Success`, `Step`, `StepName`
- [ ] `ApiClientTests`: Deserialize sample `/table/1` response → verify `game.HandStep`, `players[0].Username`
- [ ] `GameStateTests`: `IsShowdown` is true when `HandStep >= 12`
- [ ] `GameStateTests`: `IsShowdown` is false when `HandStep == 11`
- [ ] `GameStateTests`: `IsHandComplete` when `StepName == "RECORD_STATS_AND_NEW_HAND"`
- [ ] `GameStateTests`: `communityCards` parses correctly from JSON array
- [ ] `GameStateTests`: `sidePots` parses correctly, including empty array `[]`
- [ ] `GameStateTests`: `PlayerState.IsFolded` when `status == "11"`
- [ ] `GameStateTests`: `PlayerState.IsAllIn` when `status == "12"`
- [ ] `GameStateTests`: `PlayerState.IsWinner` when `winnings > 0`
- [ ] `GameStateTests`: `PlayerState.HasCards` when cards array has 2 entries

### Acceptance Criteria
- All unit tests pass in Edit Mode
- No compilation errors
- Raw JSON from sample API responses deserializes without exceptions
- `isProcessing` guard prevents re-entrant calls

---

## Phase 2: Core Managers + Utilities

**Goal:** Full state pipeline working end-to-end (API → state → event) with utility helpers.
**Depends on:** Phase 1

### Files to Create
| File | Purpose |
|------|---------|
| `Assets/Scripts/Managers/TableStateManager.cs` | Holds current `TableResponse`, fires `OnTableStateChanged` |
| `Assets/Scripts/Managers/GameManager.cs` | Orchestrates API calls, auto-play timer, `isProcessing` |
| `Assets/Scripts/Utils/CardUtils.cs` | Parse card strings, rank/suit/color |
| `Assets/Scripts/Utils/MoneyFormatter.cs` | Float → `"$150.00"` |

### Requirements

#### TableStateManager
- [ ] Singleton or single-instance MonoBehaviour
- [ ] `public TableResponse CurrentState { get; private set; }`
- [ ] `public event Action<TableResponse> OnTableStateChanged`
- [ ] `public void SetState(TableResponse state)` — sets state + fires event
- [ ] `public string GetStepLabel(int step)` — returns human-readable label from step number

Step label mapping:
```
0=Preparing Hand, 1=Setting Up Dealer, 2=Posting Small Blind, 3=Posting Big Blind,
4=Dealing Hole Cards, 5=Pre-Flop Betting, 6=Dealing Flop, 7=Flop Betting,
8=Dealing Turn, 9=Turn Betting, 10=Dealing River, 11=River Betting,
12=Showdown, 13=Evaluating Hands, 14=Paying Winners, 15=Hand Complete
```

#### GameManager
- [ ] References `PokerApiClient` and `TableStateManager`
- [ ] `public async Task AdvanceStepAsync()` — process + fetch + setState (guarded by `isProcessing`)
- [ ] `public void ToggleAutoPlay()` — start/stop auto-play coroutine
- [ ] `public void SetAutoPlaySpeed(float intervalSeconds)` — update interval
- [ ] Auto-play intervals available: 0.25f, 0.5f, 1.0f, 2.0f seconds
- [ ] Auto-play stops if `AdvanceStepAsync` returns error (null response)
- [ ] On startup: call `GetHealthAsync()` with 3 retries; fire `OnConnectionStatusChanged` event

#### CardUtils
- [ ] `ParseCard(string card)` → `(string rank, string suit)`
  - `"AH"` → `("A", "H")`
  - `"10D"` → `("10", "D")`
- [ ] `IsRedSuit(string suit)` → `true` for H, D
- [ ] `GetSuitSymbol(string suit)` → `"♥"` / `"♦"` / `"♣"` / `"♠"`
- [ ] `GetDisplayString(string card)` → `"A♥"` / `"10♦"`

#### MoneyFormatter
- [ ] `Format(float amount)` → `"$150.00"`
- [ ] `FormatGain(float amount)` → `"+$24.00"`
- [ ] Handles 0 → `"$0.00"`, negative → `"-$5.00"`

### CardUtils Unit Tests (complete CardUtilsTests.cs)
- [ ] `"AH"` → rank=`"A"`, suit=`"H"`
- [ ] `"10D"` → rank=`"10"`, suit=`"D"`
- [ ] `"2C"` → rank=`"2"`, suit=`"C"`
- [ ] `"KS"` → rank=`"K"`, suit=`"S"`
- [ ] H and D are red suits
- [ ] C and S are black suits
- [ ] `GetDisplayString("AH")` → `"A♥"`
- [ ] `GetDisplayString("10D")` → `"10♦"`
- [ ] `MoneyFormatter.Format(150f)` → `"$150.00"`
- [ ] `MoneyFormatter.FormatGain(24f)` → `"+$24.00"`
- [ ] `MoneyFormatter.Format(0f)` → `"$0.00"`

### Acceptance Criteria
- All CardUtils + MoneyFormatter tests pass
- `GameManager.AdvanceStepAsync()` called twice in sequence: second call is blocked by `isProcessing`
- `TableStateManager.OnTableStateChanged` fires when `SetState` is called
- `GetStepLabel(6)` returns `"Dealing Flop"`
- Auto-play coroutine starts/stops on toggle

---

## Phase 3: Scene Skeleton

**Goal:** Unity scene with canvas hierarchy and positioned seat anchors — no logic, just layout.
**Depends on:** Phase 2

### Scene: `Assets/Scenes/PokerTable.unity`

### Requirements
- [ ] Canvas: Screen Space - Overlay, 1920×1080 reference resolution, scale with screen size
- [ ] Table felt: green oval `Image` component, centered, ~1200×700px
- [ ] 6 seat anchor GameObjects placed around the oval:
  ```
  Seat1: bottom-left  (−430, −270) from canvas center
  Seat2: bottom-right (  70, −270)
  Seat3: right        ( 370,    0)
  Seat4: top-right    (  70,  270)
  Seat5: top-left     (−430,  270)
  Seat6: left         (−630,    0)
  ```
- [ ] Center group: community card area + pot text placeholder
- [ ] HUD panel: top of screen, full width
- [ ] Controls panel: bottom of screen, full width
- [ ] History panel: right side, 300px wide, full height
- [ ] StatusBar: thin strip (40px) at very bottom
- [ ] `GameManager` GameObject with `GameManager`, `PokerApiClient`, `TableStateManager` components
- [ ] All TextMeshPro text components use TMP fonts (not legacy Text)

### Acceptance Criteria
- Scene loads in Play Mode without errors
- All 6 seat positions visible and correctly spaced around oval
- Canvas renders at correct resolution
- Inspector shows all required components wired to GameManager GameObject

---

## Phase 4: Core Views

**Goal:** Every piece of game state is visible on screen. Table renders correctly for any `TableResponse`.
**Depends on:** Phase 3

### Files to Create
| File | Purpose |
|------|---------|
| `Assets/Scripts/UI/TableView.cs` | Subscribes to event, distributes state to child views |
| `Assets/Scripts/UI/SeatView.cs` | Per-seat rendering (name, stack, bet, action, badges, cards) |
| `Assets/Scripts/UI/CardView.cs` | Single card (face-up rank+suit+color, or face-down dark rect) |
| `Assets/Scripts/UI/CommunityCardsView.cs` | 5 card slots, show/hide based on count |
| `Assets/Scripts/UI/HudView.cs` | Phase label, hand number, whose action, pot |

### Requirements

#### TableView
- [ ] Subscribes to `TableStateManager.OnTableStateChanged` in `OnEnable`, unsubscribes in `OnDisable`
- [ ] Holds references to 6 `SeatView` instances (serialized, by seat number 1-6)
- [ ] On state change: maps each player to their seat view by `player.Seat`
- [ ] Hides/clears seat views with no corresponding player

#### SeatView
- [ ] Displays: `player.Username`, `player.Stack` (formatted), `player.Bet` (formatted), `player.Action`
- [ ] D / SB / BB badges: shown when `seat == game.DealerSeat / SmallBlindSeat / BigBlindSeat`
- [ ] Folded state: semi-transparent (alpha 0.4) on entire seat panel when `player.IsFolded`
- [ ] All-in state: gold border or highlighted name color when `player.IsAllIn`
- [ ] Winner state: visible name highlight (handled further in Phase 6 with DOTween)
- [ ] 2 `CardView` children for hole cards
- [ ] Renders cards face-down if `!showCards`, face-up if `showCards`
- [ ] `showCards = game.IsShowdown || player.IsWinner`
- [ ] Active action text: empty string shown as nothing (no placeholder text)

#### CardView
- [ ] Face-up: white/cream background, rank text (TMP), suit symbol (TMP), red or black color
- [ ] Face-down: dark navy/green background, "?" or card back symbol (TMP)
- [ ] `public void SetCard(string cardCode, bool faceUp)`
- [ ] `public void SetEmpty()` — placeholder (gray outline rect)

#### CommunityCardsView
- [ ] 5 `CardView` slots arranged horizontally
- [ ] `Refresh(List<string> cards)` — shows `cards.Count` face-up cards, rest as empty placeholders
- [ ] Placeholders visible from step 0 (5 gray outlines)

#### HudView
- [ ] Phase label text: `TableStateManager.GetStepLabel(game.HandStep)`
- [ ] Hand number: `"Hand #" + game.GameNo`
- [ ] Whose action: `"Seat " + game.Move + " to act"` — hidden when `game.Move == 0`
- [ ] Pot: `MoneyFormatter.Format(game.Pot)` in center area

### Acceptance Criteria
- [ ] Table renders correctly for a sample state injected directly (no API needed)
- [ ] Folded player seat is visibly dimmed
- [ ] All-in player seat has visible highlight
- [ ] Face-down cards show at steps < 12, face-up at steps >= 12
- [ ] Community cards: 0 face-up at step 5, 3 at step 6, 4 at step 8, 5 at step 10
- [ ] Phase label shows "Dealing Flop" at step 6
- [ ] Pot amount matches API response value

---

## Phase 5: Controls + Hand History + Error Handling

**Goal:** Fully playable — user can step through hands, enable auto-play, and see history + errors.
**Depends on:** Phase 4

### Files to Create
| File | Purpose |
|------|---------|
| `Assets/Scripts/UI/ControlsView.cs` | Next Step btn, Auto-play toggle, speed buttons |
| `Assets/Scripts/UI/HandHistoryView.cs` | Scrollable step log + hand summaries |
| `Assets/Scripts/UI/StatusBarView.cs` | Connection status + error feedback |

### Requirements

#### ControlsView
- [ ] "Next Step" button → calls `GameManager.AdvanceStepAsync()`
- [ ] Button disabled while `isProcessing` is true
- [ ] "Auto Play" toggle button → calls `GameManager.ToggleAutoPlay()`
- [ ] Visual indicator when auto-play active (button color change or "⏸ Stop" label)
- [ ] Speed buttons: 4 options — `0.25s`, `0.5s`, `1s`, `2s`
- [ ] Active speed button visually selected (highlighted)
- [ ] Default speed: `1s`

#### HandHistoryView
- [ ] `ScrollRect` with vertical content layout
- [ ] On each `OnTableStateChanged`: append a new `HandHistoryEntry` row
- [ ] Each row shows: step label + hand number (e.g., "Hand #3 — Dealing Flop")
- [ ] On step 15 (Hand Complete): add a summary row — hand#, winner names, pot size
- [ ] Scrolls to bottom on each new entry
- [ ] Max entries: 200 (remove oldest if exceeded to prevent memory growth)

#### StatusBarView
- [ ] Persistent strip at bottom of screen
- [ ] States: `"Connected"` (green), `"Connecting..."` (yellow), `"Error: [message]"` (red)
- [ ] Subscribes to `GameManager.OnConnectionStatusChanged` event
- [ ] Error message auto-clears after 5 seconds

### Acceptance Criteria
- [ ] Clicking Next Step advances hand by exactly 1 step
- [ ] Double-clicking Next Step rapidly: only 1 advance fires (isProcessing guard works)
- [ ] Auto-play runs at selected speed without manual clicks
- [ ] Auto-play stops when toggled off or on API error
- [ ] History shows new entry after each step
- [ ] History scrolls to show latest entry
- [ ] StatusBar shows red error when holdem-processor is not running
- [ ] StatusBar shows green "Connected" after successful health check

---

## Phase 6: Animations + Polish

**Goal:** Visual polish — winner highlights, card reveals, pot/stack tweens, phase label punch.
**Depends on:** Phase 5

### Requirements

#### Winner Highlight (FR-5)
- [ ] On step 13+ when `player.IsWinner`: seat background pulses gold using DOTween
  - `image.DOColor(Color.gold, 0.4f).SetLoops(3, LoopType.Yoyo)`
- [ ] Winner hand rank text appears: `player.HandRank` (e.g., "Full House")
- [ ] Winnings text appears: `MoneyFormatter.FormatGain(player.Winnings)` in green
- [ ] Both texts fade out after 3 seconds using `DOFade`
- [ ] Side pot distribution: if `game.SidePots.Count > 0`, show pot split in HUD

#### Card Reveal Animation (FR-3 / Should Have)
- [ ] On transition to step 12 (showdown): cards flip from face-down to face-up
  - DOTween: scale X from 1 → 0 (0.15s), swap content, scale X from 0 → 1 (0.15s)

#### Stack + Pot Tweens (Should Have)
- [ ] Stack amount text: tween numeric value when it changes (`DOCounter` or lerp via `DOVirtual.Float`)
- [ ] Pot amount text: same tween effect

#### Phase Label Animation (Should Have)
- [ ] On each step change: `phaseLabel.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f)`

#### Error State Polish
- [ ] Next Step button shake animation on failed API call
  - `button.transform.DOShakePosition(0.3f, 5f, 20)`

### Acceptance Criteria
- [ ] Winner seat pulses gold visually at step 13+
- [ ] Hand rank text visible at step 13+
- [ ] Winnings text visible and fades after 3s
- [ ] Cards animate from face-down to face-up at step 12
- [ ] Pot value smoothly counts up/down on change
- [ ] Phase label punches on every step change
- [ ] Button shakes on API error

---

## Phase 7: Docker Verification + README

**Goal:** End-to-end working with Docker backend. Submission-ready.
**Depends on:** Phase 6

### Requirements
- [ ] `docker compose --profile engine up -d` starts holdem-processor at port 3030
- [ ] Unity Play Mode connects on startup (health check succeeds)
- [ ] Clicking Next Step 16 times completes one full hand (steps 0-15)
- [ ] Step 16 starts a new hand (gameNo increments)
- [ ] Multiple consecutive hands play through without errors
- [ ] Auto-play runs a full hand unattended
- [ ] README updated with:
  - How to start the Docker backend
  - How to open and run the Unity project
  - Architecture decisions summary (uGUI, async/await, MVP pattern)
  - Known limitations

### Acceptance Criteria
- [ ] All Must Have acceptance criteria from the spec are met
- [ ] All Should Have criteria met (or explicitly noted as not implemented)
- [ ] Unit tests all pass in Edit Mode
- [ ] No console errors during normal play
- [ ] No memory leaks (history capped at 200 entries)

---

## MVP Validation Checklist

Maps every Must Have requirement from the spec to a phase:

| # | Requirement | Phase | Status |
|---|-------------|-------|--------|
| 1 | Poker table renders with 6 player seats around felt | 3+4 | [ ] |
| 2 | API client connects to holdem-processor at localhost:3030 | 1 | [ ] |
| 3 | POST /process advances hand by one step | 1+5 | [ ] |
| 4 | GET /table/{tableId} fetches and displays current state | 1+4 | [ ] |
| 5 | Community cards appear incrementally (3/4/5) | 4 | [ ] |
| 6 | Hole cards face-down during play, revealed at showdown | 4 | [ ] |
| 7 | Player name, stack, bet, action display at each seat | 4 | [ ] |
| 8 | Pot amount updates in center | 4 | [ ] |
| 9 | Dealer / SB / BB position badges display correctly | 4 | [ ] |
| 10 | Winner highlighting with hand rank text and payout | 6 | [ ] |
| 11 | Stack amounts reflect winnings after payout | 4+6 | [ ] |
| 12 | Next Step button triggers one state advance | 5 | [ ] |
| 13 | Phase label shows current hand step in human-readable text | 4 | [ ] |
| 14 | Unit tests on API client and data model deserialization | 1+2 | [ ] |
| 15 | Docker Compose engine profile starts backend, Unity connects | 7 | [ ] |

---

## Stretch Goals (ordered by impact)

1. **Auto-play with configurable speed** — Phase 5, 3 speed options minimum
2. **Hand history log** — Phase 5
3. **Error handling with user-visible feedback** — Phase 5
4. **Card flip animation at showdown** — Phase 6
5. **Stack + pot tweens** — Phase 6
6. **Hand number display** — Phase 4 (HudView)
7. **Player avatar placeholders** — colored circle with initials, bonus
8. **Sound effects** — card deal, chip clink — bonus
9. **Chip stack visualization** — bonus
