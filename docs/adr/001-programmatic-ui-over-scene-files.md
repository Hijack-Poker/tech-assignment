# ADR-001: Programmatic UI Over Scene Files

## Status
Accepted

## Context
Unity scene files (.unity) and prefabs are serialized as binary or verbose YAML. In any team environment, these files cause frequent and unresolvable merge conflicts because even trivial changes (reordering a sibling, toggling a checkbox) rewrite large sections of the file. Code review of scene diffs is effectively impossible.

We needed a UI approach that is fully version-control friendly, deterministic, and does not depend on the Unity visual editor.

## Decision
All UI hierarchy is constructed in code at runtime. A `UIFactory` utility class provides static `Create()` factory methods for common elements (panels, buttons, labels, cards, chip stacks). Entry points use `[RuntimeInitializeOnLoadMethod]` so the entire UI tree is built before the first frame without relying on any serialized scene data.

The project ships with only a minimal bootstrap scene containing a camera and an empty root GameObject.

## Consequences

### Positive
- Zero merge conflicts on UI changes; all diffs are plain C#.
- UI layout is self-documenting -- reading the code shows exactly what gets created and where.
- Portable across Unity versions since there is no editor-version-specific serialization.
- Easy to unit-test layout logic without entering Play mode.

### Negative
- No visual preview in the Unity Editor; layout must be verified at runtime.
- More verbose than dragging widgets onto a canvas -- every RectTransform is configured in code.
- Onboarding developers unfamiliar with programmatic UI takes longer than pointing at a scene hierarchy.
