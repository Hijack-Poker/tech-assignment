# ADR-005: Procedural Assets Over Binary Files

## Status
Accepted

## Context
Traditional Unity projects include sprite sheets, audio clips, texture atlases, and font assets as binary files checked into the repository. These files bloat the repo (often hundreds of MB), produce meaningless diffs, slow down clones, and may carry third-party license obligations.

This project targets a small, self-contained repository that compiles from source with no external asset dependencies.

## Decision
All visual and audio assets are generated procedurally at runtime:

- `AvatarPatternGenerator` -- creates unique player avatars from deterministic hash-based geometric patterns and color palettes.
- `AudioManager` -- synthesizes chip sounds, card flicks, and UI feedback tones using oscillator and noise primitives via `AudioClip.Create`.
- `TextureGenerator` -- produces card faces, table felt, and button textures from code using `Texture2D.SetPixels`.

No binary asset files (PNG, WAV, PSD, etc.) exist in the repository.

## Consequences

### Positive
- Repository is tiny; the entire client is plain C# source.
- No asset licenses to track or comply with.
- Every player avatar is procedurally unique, adding visual variety without an art pipeline.
- Build size is minimal since no asset bundles are included.

### Negative
- Visual fidelity is limited to what can be generated algorithmically; photorealistic or hand-crafted art is not feasible.
- Procedural audio lacks the richness of recorded samples.
- Asset generation adds a small startup cost on first launch (mitigated by caching generated textures).
- Requires developers with both programming and visual/audio design intuition.
