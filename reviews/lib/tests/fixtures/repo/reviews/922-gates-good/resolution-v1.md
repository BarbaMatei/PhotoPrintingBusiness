---
type: resolution
target: 922-gates-good
version: 1
answers: review-v1.md
status: resolved
fixed_commit: abcd222
closed: 2026-08-30
---

# Resolution v1 — 922-gates-good

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9221 | fixed | `abcd222` | Blind re-posts capped. |
| PPW-9222 | fixed | `abcd222` | Key retired on confirmation only. |
| PPW-9223 | fixed | `abcd222` | Hand-over cancels the stale intent. |

## Scope

| Cluster | Findings | Files | Protocol |
|---|---|---|---|
| A — payment key | PPW-9221, PPW-9222, PPW-9223 | `Services/Trigger.cs`, `Services/Overlap.cs` | payment key lifecycle |

## Decisions

### Protocol — payment key lifecycle

At most one confirmable intent exists per basket, ever. The key is retired
exactly once, on confirmation; a hand-over always cancels the prior intent
before the new one is minted. The invariant test drives the composed flow:
decline, retry, hand-over, late success.
