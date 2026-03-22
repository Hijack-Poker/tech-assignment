# ADR-002: Custom Tweener Over DOTween

## Status
Accepted

## Context
Animation tweening is needed throughout the client: chip movements, card flips, fade transitions, button presses, and avatar reactions. DOTween is the de-facto standard in Unity projects, but it introduces a compiled DLL dependency, has its own setup wizard, and is licensed under a dual free/pro model that complicates redistribution.

We wanted zero third-party runtime dependencies in the client.

## Decision
Built a custom `Tweener` class supporting float, color, scale, rotation, and alpha interpolation with standard easing functions (ease-in, ease-out, ease-in-out, bounce, elastic). A fluent `Timeline` API allows sequencing and parallel grouping of tweens, with support for cancellation tokens and snap-to-end on interrupt.

All tweens are driven by a single MonoBehaviour update loop to avoid per-tween coroutine overhead.

## Consequences

### Positive
- Zero third-party dependencies; the client compiles from source alone.
- Full control over cancellation semantics -- interrupted tweens snap to their target value, preventing visual glitches.
- Smaller build size compared to bundling DOTween.
- Tween logic is plain C# and can be tested outside Unity with a mock time source.

### Negative
- Upfront development cost to implement and test the easing library.
- Feature set is narrower than DOTween (no path tweens, no shader property tweens).
- New contributors familiar with DOTween's API need to learn the custom API.
