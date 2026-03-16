---
name: phase3
description: Build the Unity scene skeleton — Canvas, oval table felt, 6 seat anchor positions, HUD/controls/history panels, GameManager GameObject. Requires Unity MCP connection.
---

# Phase 3: Scene Skeleton

Read CLAUDE.md before starting. Phase 2 must be complete.
**This phase requires Unity MCP connection** (`mcp__unity__*` tools must be available).

## Goal
A Unity scene with correct canvas hierarchy, positioned seat anchors, and all panels laid out. No logic — just the visual skeleton ready for views to be attached.

## Pre-Check
Before starting, verify MCP is connected:
- Use `mcp__unity__read_console` to check for errors
- Use `mcp__unity__get_scene_hierarchy` to see current scene state
- If no MCP tools available → STOP and tell the user to start Unity MCP server

## Scene File
`Assets/Scenes/PokerTable.unity`

Open this scene (create if it doesn't exist) before making any changes.

## Canvas Setup

Create a Canvas GameObject:
- Name: `PokerTableCanvas`
- Canvas component: Render Mode = Screen Space - Overlay
- Canvas Scaler: Scale With Screen Size, Reference Resolution = 1920×1080, Match = 0.5
- Add GraphicRaycaster component

## Table Felt

Inside `PokerTableCanvas`, create:
- GameObject: `TableFelt`
- Add `Image` component
- Color: `#1B5E20` (dark green), or use a soft green felt color
- RectTransform: anchored center, size = 1200×700
- Add the `TableView.cs` script (Phase 4 — leave slot empty for now)

## 6 Seat Anchors

Inside `TableFelt`, create 6 empty GameObjects named `Seat1` through `Seat6`.
Position them around the oval (using anchored position from canvas center):

| Seat | AnchoredPosition | Description |
|------|-----------------|-------------|
| Seat1 | (-430, -270) | Bottom left |
| Seat2 | (70, -270) | Bottom right |
| Seat3 | (370, 0) | Right |
| Seat4 | (70, 270) | Top right |
| Seat5 | (-430, 270) | Top left |
| Seat6 | (-630, 0) | Left |

Each seat anchor:
- Size: 200×160 (placeholder — SeatView will set this)
- Add a child `Panel` Image (white, low alpha) as placeholder

## Center Group

Inside `TableFelt`, create `CenterGroup`:
- Position: (0, 0) (center of table)
- Children:
  - `CommunityCardsArea` — horizontal layout, 5 card slots (60×90 each, 8px spacing)
  - `PotText` — TextMeshProUGUI, text = "Pot: $0.00", font size 24, centered, below cards

## HUD Panel

Inside `PokerTableCanvas`, create `HUDPanel`:
- Anchors: top-stretch (left=0, right=0, top=1, bottom=1)
- Height: 80px, anchored to top
- Background: dark panel `Image`, color `#1A1A2E`
- Children:
  - `PhaseLabel` — TMP, font size 22, centered, text = "Preparing Hand"
  - `HandNumberText` — TMP, font size 16, left-aligned, text = "Hand #1"
  - `ActionText` — TMP, font size 16, right-aligned, text = ""
  - `PotDisplayText` — TMP, font size 20, center-right, text = "Pot: $0.00"

## Controls Panel

Inside `PokerTableCanvas`, create `ControlsPanel`:
- Anchors: bottom-stretch
- Height: 80px, anchored to bottom
- Background: dark `Image`, color `#1A1A2E`
- Children:
  - `NextStepButton` — Button with TMP text "Next Step"
  - `AutoPlayButton` — Button with TMP text "Auto Play"
  - `SpeedGroup` — horizontal group with 4 buttons: "0.25s", "0.5s", "1s", "2s"

## History Panel

Inside `PokerTableCanvas`, create `HistoryPanel`:
- Anchors: right-stretch (right edge)
- Width: 300px, full height minus HUD and Controls
- Background: `#0D0D1A`
- Children:
  - `HistoryHeader` — TMP text "Hand History", font 18
  - `HistoryScrollRect` — ScrollRect component
    - `Viewport` → `HistoryContent` — Vertical Layout Group

## Status Bar

Inside `PokerTableCanvas`, create `StatusBar`:
- Anchors: bottom-stretch, overlaid above Controls panel OR below it
- Height: 30px
- Background: `#2D2D2D`
- Child: `StatusText` — TMP, font 14, left-aligned, text = "Connecting..."

## GameManager GameObject

Create an empty GameObject named `GameManager`:
- Add components:
  - `GameManager.cs`
  - `TableStateManager.cs`
  - `PokerApiClient.cs`
- Wire references in Inspector:
  - `GameManager._apiClient` → the `PokerApiClient` component
  - `GameManager._stateManager` → the `TableStateManager` component

## Verification via MCP

After building:
1. `mcp__unity__get_scene_hierarchy` — verify all GameObjects exist
2. `mcp__unity__read_console` — verify no errors on scene load
3. Enter Play Mode briefly: `mcp__unity__set_gameobject_state` or verify via console

## Acceptance Criteria
- [ ] Scene loads without errors in Play Mode
- [ ] Canvas renders at 1920×1080 reference resolution
- [ ] All 6 seat anchors visible around the oval felt area
- [ ] HUD panel visible at top
- [ ] Controls panel visible at bottom
- [ ] History panel visible on right side
- [ ] Status bar visible
- [ ] `GameManager` GameObject has all 3 components attached
- [ ] No null reference errors in console on Play Mode entry

## Next Phase
→ `/phase4`
