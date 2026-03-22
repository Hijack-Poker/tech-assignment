# HijackPoker — 5-Minute Demo Script

## INTRO (0:00 – 0:30)

> "Hi, I'm [name]. I built **HijackPoker** — a Unity game client for Texas Hold'em poker. It connects to a backend engine called holdem-processor via REST API and renders a fully interactive poker table with 6 players, animations, AI coaching, and auto-play modes."
>
> "This was built in Unity 2022.3 LTS using C#, following an event-driven MVP architecture. 42 source files, 107 unit tests, 11 test files."

**SHOW:** The home screen. Type a name, pick an avatar, click Play.

---

## S — SITUATION (0:30 – 1:00)

> "The assignment was Option D: build a Unity poker client that connects to a Docker-hosted game engine at localhost:3030. The engine handles all game logic — dealing, betting rounds, winner evaluation. My job was to build everything the player sees and interacts with."
>
> "The requirements included: rendering a 6-seat table, connecting via REST API, stepping through hands, showing community cards incrementally, winner highlights, and unit tests."

**SHOW:** Docker running (`docker compose --profile engine up -d`), then the game loading.

---

## T — TASK (1:00 – 1:30)

> "I needed to deliver across 7 phases: API layer with tests, state management, scene layout, core views, controls with hand history, animations and polish, and Docker verification. Plus stretch goals like auto-play, sound effects, chip visualization, and card animations."

**SHOW:** Quickly flash the project structure in VS Code/IDE — `Api/`, `Models/`, `Managers/`, `UI/`, `Utils/`, `Tests/`.

---

## A — ACTION: Core Gameplay (1:30 – 3:00)

**Step through a hand manually using NEXT STEP:**

> "Let me walk through a full hand."

1. **Click NEXT** — *"Blinds post automatically — you can see the SB and BB badges, chip fly animations, and the hand history logging each action."*
2. **Cards deal** — *"Cards deal clockwise from the dealer with arc animations and a shuffle sound. Each player gets face-down cards."*
3. **Betting round** — *"During betting, the action panel appears — Fold, Call, Raise, All-In, plus 2X/3X quick bets. There's a 20-second turn timer with a warning sound at 5 seconds."*
4. **Flop/Turn/River** — *"Community cards appear incrementally — 3 on the flop, 1 on the turn, 1 on the river. Pot chips stack up below the cards — more chips as the pot grows."*
5. **Showdown** — *"At showdown, cards flip face-up, hand ranks display, winner gets a gold pulse, winnings animate, and the hand history logs the winner with the amount."*

**SHOW:** Point out the HUD (hand number, phase label, pot), seat badges (D/SB/BB), folded player dimming, all-in pulsing border.

---

## A — ACTION: Advanced Features (3:00 – 4:00)

**Demo FOCUSED mode:**

> "I added a Focused mode — click it and the other players play automatically, but it pauses on MY turn so I can make real decisions. It cycles through Safe, Small, and Hard AI styles."

**Click FOCUS Safe** → watch others play → action panel appears on your turn → make a decision.

**Demo AUTO mode:**

> "Auto mode plays everyone including me. Same style cycling — Safe just calls, Small occasionally raises, Hard goes aggressive with folds, raises, and all-ins."

**Change speed** to 0.25x → *"Speed controls work for both modes."*

**Click the ? button:**

> "There's a help popup explaining every control and poker basics — hand rankings, terminology, all scrollable."

**Open AI Coach tab:**

> "The AI Coach tracks your session — VPIP, PFR, aggression factor, win rate, streaks, position stats. It generates coaching tips based on your play patterns and per-hand reviews analyzing your decisions."

**Click RESTART:**

> "Restart resets everything fresh — the AI coach stats clear too."

---

## A — ACTION: Code Quality (4:00 – 4:30)

**Switch to IDE briefly:**

> "On the engineering side — 107 unit tests covering betting math, auto-play decisions, seat resolution, card parsing, money formatting, input validation, and hand history formatting. All Edit Mode, NUnit."
>
> "I refactored the codebase to keep every file under 600 lines. TableView went from 1,623 lines to 484 by extracting 6 sub-controllers — audio, pot display, turn timer, tip, winner celebration, and animations. Shared utilities eliminate duplicated logic across 5+ files."
>
> "Security: player names are truncated to 20 characters before storage."

**SHOW:** Run tests in Unity Test Runner → all green.

---

## R — RESULT (4:30 – 5:00)

> "The result: every Must Have requirement is met, plus all stretch goals — auto-play with configurable speed, scrollable hand history, error handling with status bar, card flip animations, pot and stack tweens, avatars, sound effects, chip stacks, and card preview on tap."
>
> "Beyond the requirements, I added: Focused play mode, AI coaching with leak detection, a home screen with animated entrance, a help guide, WebSocket support for real-time updates, and a tip-the-dealer feature."
>
> "42 source files, 107 tests, clean architecture, all files under 600 lines. Thanks for watching."

---

## Recording Tips

1. **Screen record at 1920x1080** — use OBS or QuickTime
2. **Have Docker running before you start** — `docker compose --profile engine up -d`
3. **Pre-load the game** so you don't wait on the loading screen
4. **Use Focused mode for the manual play demo** — it looks more natural
5. **Keep Unity Game view visible**, not Scene view
6. **Talk over the gameplay** — don't go silent while clicking
7. **Cut/edit** if a hand takes too long — you can speed up auto-play sections

---

## What Was Built — Full Feature List

### Must Have (all met)

- 6-seat poker table with felt layout
- REST API client connecting to holdem-processor at localhost:3030
- POST /process advances hand by one step
- GET /table/{tableId} fetches and displays current state
- Community cards appear incrementally (3/4/5)
- Hole cards face-down during play, revealed at showdown
- Player name, stack, bet, action display at each seat
- Pot amount updates in center
- Dealer / SB / BB position badges
- Winner highlighting with hand rank text and payout
- Stack amounts reflect winnings after payout
- Next Step button triggers one state advance
- Phase label shows current hand step
- Unit tests on API client and data model deserialization
- Docker Compose engine starts backend, Unity connects

### Stretch Goals (all met)

- Auto-play with configurable speed (0.25x, 0.5x, 1x, 2x)
- Hand history log with scrollable view
- Error handling with status bar (green/yellow/red)
- Card flip animation at showdown
- Stack + pot amount tweens
- Hand number display
- Player avatar placeholders with color rings
- Sound effects (shuffle, deal, fold, chip bet, turn start, time warning, crowd clap)
- Chip stack visualization scaling with pot size

### Beyond Requirements

- **Home screen** with animated entrance, name input, avatar selection
- **Focused play mode** — others auto-play, you decide your own cards
- **AI Coach** — VPIP, PFR, AF, WTSD, WSD, C-bet tracking, per-hand review, coaching tips
- **Help popup** — full control guide + poker basics + hand rankings
- **Card preview** — tap hole cards during your turn for large flip preview
- **Tip dealer** button with chip fly animation
- **WebSocket** support for real-time table updates
- **Turn timer** with 20s countdown, low-time warning, auto-fold
- **Chip fly animations** from seats to pot on bets
- **Shuffle animation** with riffle effect on new hands
- **Card deal animation** clockwise from dealer with arc paths
- **All-in pulsing border** and label animation
- **Winner celebration** with confetti burst
- **Restart** button for fresh game reset

### Code Quality

- 42 source files, all under 600 lines
- 107 unit tests across 11 test files
- Event-driven MVP architecture
- Shared utilities (PokerConstants, BettingCalculator, SeatResolver, CardSpriteLoader)
- Sub-controller extraction (TableView 1623→484 lines, SeatView 635→490 lines)
- Input validation (name length truncation)
- InternalsVisibleTo for test access to internal methods

