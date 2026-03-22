# ADR-001: Choose Option D — Unity Game Client

**Status:** Accepted

## Context

The tech assignment offers 4 options (A: rewards system, B: multi-table tournaments, C: streak challenges, D: Unity game client). Option D requires building a 6-seat poker table client in Unity that connects to the holdem-processor REST API.

## Decision

Chose Option D because it exercises full-stack skills (Unity C# client + Node.js backend), touches the most surface area of the existing codebase, and produces a visually demonstrable deliverable. It also tests comfort with unfamiliar territory (Unity) which aligns with the "AI-first developer" evaluation criteria.

## Consequences

### Positive

- Tangible, visual output
- Exercises frontend architecture skills
- Demonstrates ability to learn new tools quickly

### Negative

- Unity tests can't run in headless CI easily
- Review requires Unity Editor installed
- Larger asset footprint
