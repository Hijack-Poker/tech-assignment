# PokerClient — Claude Code Context

Unity 2022.3 LTS poker table client for the Gauntlet tech assignment (Option D).
Connects to holdem-processor REST API at `http://localhost:3030`.

---

## Project Identity

- **Namespace**: `HijackPoker`
- **Unity version**: 2022.3 LTS
- **Scene**: `Assets/Scenes/PokerTable.unity`
- **Backend**: holdem-processor at `localhost:3030` (Docker: `docker compose --profile engine up -d`)
- **MCP**: CoplayDev/unity-mcp v9.5.3, HTTP transport at `http://127.0.0.1:8080/mcp`

---

## Locked Architecture Decisions

| Concern | Decision | Reason |
|---------|----------|--------|
| UI system | **uGUI** (Canvas-based) | Battle-tested, more examples than UI Toolkit |
| Async | **UnityWebRequest + TaskCompletionSource** | No extra package, built-in |
| JSON | **Newtonsoft JSON** (`com.unity.nuget.newtonsoft-json`) | Handles nested arrays + nullables |
| Text | **TextMeshPro** | Required |
| Animation | **DOTween** | Best Unity tween library |
| Pattern | **Event-driven MVP** | Decoupled views, testable managers |
| Tests | **NUnit Edit Mode only** | No Play Mode tests required |
| Redraw | **Full redraw on every state change** | Simple, correct, no delta patching |

---

## Directory Structure

```
Assets/
├── Scripts/
│   ├── Api/
│   │   └── PokerApiClient.cs
│   ├── Models/
│   │   ├── GameState.cs
│   │   ├── PlayerState.cs
│   │   ├── TableResponse.cs
│   │   └── HandHistoryEntry.cs
│   ├── Managers/
│   │   ├── TableStateManager.cs
│   │   └── GameManager.cs
│   ├── UI/
│   │   ├── TableView.cs
│   │   ├── SeatView.cs
│   │   ├── CardView.cs
│   │   ├── CommunityCardsView.cs
│   │   ├── HudView.cs
│   │   ├── ControlsView.cs
│   │   ├── HandHistoryView.cs
│   │   └── StatusBarView.cs
│   └── Utils/
│       ├── CardUtils.cs
│       └── MoneyFormatter.cs
├── Scenes/
│   └── PokerTable.unity
├── Resources/
└── Tests/
    └── EditMode/
        ├── ApiClientTests.cs
        ├── GameStateTests.cs
        └── CardUtilsTests.cs
```

---

## Key Logic Rules

### Card Visibility
```csharp
bool isShowdown = game.HandStep >= 12;
bool showCards  = isShowdown || player.Winnings > 0;
```

### Community Card Count
Use `game.CommunityCards.Count` directly from API — do NOT derive from handStep.

### Card Parsing
```csharp
string suit = card[^1..];   // last char
string rank = card[..^1];   // everything before last
bool isRed  = suit == "H" || suit == "D";
```

### Player Status Codes
```
"1"  = Active     "11" = Folded
"2"  = Sitting Out  "12" = All-In
"3"  = Leaving
"4"  = Show Cards
"5"  = Post Blind
"6"  = Wait for BB
```

### Step Labels
```
0=Preparing Hand       8=Dealing Turn
1=Setting Up Dealer    9=Turn Betting
2=Posting Small Blind  10=Dealing River
3=Posting Big Blind    11=River Betting
4=Dealing Hole Cards   12=Showdown
5=Pre-Flop Betting     13=Evaluating Hands
6=Dealing Flop         14=Paying Winners
7=Flop Betting         15=Hand Complete
```

### isProcessing Guard
`GameManager` owns an `isProcessing` bool. Set `true` before `AdvanceStepAsync()`, clear in `finally`. Prevents double-advance on Next Step button and auto-play.

---

## API Endpoints

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/health` | Connection check |
| POST | `/process` | Advance one step, body: `{"tableId": 1}` |
| GET | `/table/1` | Full table state |

---

## Event Flow

```
GameManager.AdvanceStepAsync()
  → PokerApiClient.ProcessStepAsync(1)
  → PokerApiClient.GetTableStateAsync(1)
  → TableStateManager.SetState(TableResponse)
  → OnTableStateChanged(TableResponse) fires
  → TableView + HudView + HandHistoryView + StatusBarView all redraw
```

---

## DO NOT

- Do NOT use `JsonUtility` — it breaks on nested arrays
- Do NOT put API calls in `MonoBehaviour.Update()`
- Do NOT use UI Toolkit — uGUI only
- Do NOT write Play Mode tests — Edit Mode only
- Do NOT add beta or experimental Unity packages
- Do NOT delta-patch state — always full redraw

---

## Phase Status

Track which phases are complete. Update this as you finish each one.

- [ ] Phase 0: Packages installed (Newtonsoft JSON, DOTween, TMP Essentials)
- [x] Phase 1: API layer + models + unit tests
- [x] Phase 2: Managers + CardUtils + MoneyFormatter
- [x] Phase 3: Scene skeleton
- [x] Phase 4: Core views (Table, Seat, Card, HUD)
- [x] Phase 5: Controls + Hand History + Status Bar
- [x] Phase 6: Animations + polish
- [ ] Phase 7: Docker verification + README

---

## Reference Files

- Full presearch + decisions: `~/Desktop/Gauntlet/tech-assignment/presearch.md`
- Full PRD with acceptance criteria: `~/Desktop/Gauntlet/tech-assignment/PRD.md`
- Skeleton scripts: `~/Desktop/Gauntlet/tech-assignment/unity-client/Scripts/`
- Reference JS implementation: `~/Desktop/Gauntlet/tech-assignment/ui/index.html`
