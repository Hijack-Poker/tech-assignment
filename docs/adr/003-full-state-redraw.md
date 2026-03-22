# ADR-003: Full State Redraw vs Delta Patching

**Status:** Accepted

## Context

When the game state changes, the UI can either (a) compare old vs new state and only update changed elements (delta patching), or (b) redraw all UI elements from the new state.

## Decision

Full redraw on every state change. Each view reads the complete `TableResponse` and sets all its UI elements accordingly.

## Consequences

### Positive

- No state synchronization bugs
- Impossible to have stale UI
- Simpler code with fewer edge cases
- Easier to test

### Negative

- Slightly more CPU work per update
- More UI element assignments per frame

### Trade-off Accepted

With ~6 seats, 5 community cards, and 1 pot display, the full redraw involves ~50 UI element updates — negligible performance cost. Delta patching would add ~200 lines of diffing code for zero user-visible benefit.
