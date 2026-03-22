# ADR-003: WebSocket Plus REST Dual Transport

## Status
Accepted

## Context
The poker client needs real-time table state updates (player actions, pot changes, card reveals) with minimal latency. WebSocket is the natural choice, but several deployment targets present problems:

- Unity WebGL builds run inside a browser sandbox with strict CORS rules.
- Corporate firewalls and proxy servers sometimes block or downgrade WebSocket connections.
- During development, a simple REST fallback simplifies debugging with curl.

## Decision
The primary transport is a WebSocket connection to the `cash-game-broadcast` service, which pushes table state snapshots and event deltas. If the WebSocket handshake fails or the connection drops and cannot reconnect within a timeout, the client falls back to REST polling against the `holdem-processor` GET endpoints on a configurable interval.

Transport selection is handled by an abstraction layer (`ITableTransport`) so game logic is unaware of the underlying mechanism.

## Consequences

### Positive
- Low-latency updates (~50ms) when WebSocket is available.
- Graceful degradation in restricted network environments; the game remains playable via polling.
- Works across all Unity build targets (standalone, WebGL, mobile).
- REST fallback doubles as a health-check mechanism.

### Negative
- Two code paths to maintain and test.
- REST polling introduces higher latency and increased server load compared to push.
- State reconciliation logic must handle both incremental deltas (WS) and full snapshots (REST).
