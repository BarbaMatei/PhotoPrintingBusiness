---
type: resolution
target: 923-newshape
version: 1
answers: review-v1.md
status: resolved
fixed_commit: abcd321
closed: 2026-08-30
---

# Resolution v1 — 923-newshape

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9231 | fixed | `abcd321` | Key retired on confirmation only; invariant test drives the composed flow. |
| PPW-9232 | fixed | `abcd321` | Hand-over cancels the stale intent. |

## Scope

| Cluster | Findings | Files | Protocol |
|---|---|---|---|
| A — payment key | PPW-9231, PPW-9232 | `Services/Fixture.cs` | payment key lifecycle |

## Decisions

### Protocol — payment key lifecycle

At most one confirmable intent exists per basket, ever. The key is retired
exactly once, on confirmation; a hand-over always cancels the prior intent
before a new one is minted.
