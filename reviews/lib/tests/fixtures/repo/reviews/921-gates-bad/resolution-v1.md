---
type: resolution
target: 921-gates-bad
version: 1
answers: review-v1.md
status: resolved
fixed_commit: abcd122
closed: 2026-08-30
---

# Resolution v1 — 921-gates-bad

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9211 | fixed | `abcd122` | Blind re-posts capped. |
| PPW-9212 | fixed | `abcd122` | Key retired on confirmation only. |
| PPW-9213 | fixed | `abcd122` | Hand-over cancels the stale intent. |
| PPW-9214 | fixed | `abcd122` | Timer cleared on destroy. |
| PPW-9215 | fixed | `abcd122` | Late success gated on the terminal status. |

## Scope

| Cluster | Findings | Files | Protocol |
|---|---|---|---|
| A — payment key | PPW-9211, PPW-9212, PPW-9213, PPW-9214, PPW-9215 | `Services/Trigger.cs`, `Services/Overlap.cs`, `Services/Late.cs` | — |

## Decisions

### None this round

No decision was needed.
