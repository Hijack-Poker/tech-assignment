# ADR-004: Client-Side Analytics

## Status
Accepted

## Context
The client displays player profiling stats (VPIP, PFR, aggression factor), board texture analysis (wet/dry, straight and flush draw counts), and basic strategy hints. These could be computed server-side as a dedicated analytics or "AI" microservice, or derived locally from the table state transitions the client already receives.

Adding a server-side service would require a new deployment, API contract, and ongoing infrastructure cost.

## Decision
All analytics are computed entirely client-side in the `HijackPoker.Analytics` namespace. Stats are derived from diffs between consecutive table state snapshots that arrive over the existing transport layer. No additional network requests are made.

Key modules:
- `PlayerProfiler` -- tracks per-player action frequencies across observed hands.
- `BoardAnalyzer` -- evaluates board texture from the community cards.
- `StrategyAdvisor` -- combines profiling and board data into simple recommendations.

## Consequences

### Positive
- Zero backend changes or new services required.
- Computation is instant with no network round-trip.
- No additional server cost or scaling concern.
- Works identically in offline/replay mode.

### Negative
- Stats are session-scoped; they reset when the client disconnects and cannot persist across sessions without a backend.
- Accuracy is limited to hands the client has observed at the current table.
- Computations run on the player's device, though the overhead is negligible for this data volume.
