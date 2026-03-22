# ADR-002: Use Event-Driven MVP Pattern

**Status:** Accepted

## Context

The Unity client needs a clear architecture for managing game state and UI updates. Options considered: MVC, MVVM, raw MonoBehaviour spaghetti, Event-driven MVP.

## Decision

Chose Event-driven MVP. TableStateManager holds the canonical game state and fires `OnTableStateChanged`. All views subscribe to this event and redraw themselves. GameManager orchestrates API calls and feeds state updates to TableStateManager.

## Consequences

### Positive

- Views are decoupled and independently testable
- Adding new views is trivial (just subscribe)
- No circular dependencies
- Easy to reason about data flow

### Negative

- Full redraw on every state change (no delta patching) may be less efficient for high-frequency updates
- Event-driven debugging can be harder to trace

### Trade-off Accepted

At poker game update rates (~1 state change per second max), full redraw performance cost is negligible. Simplicity wins over optimization.
