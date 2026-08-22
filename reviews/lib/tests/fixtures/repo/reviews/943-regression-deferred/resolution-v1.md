---
type: resolution
target: 943-regression-deferred
version: 1
answers: review-v1.md
status: resolved
fixed_commit: 8888881
closed: 2026-08-22
---

# Resolution v1 — 943-regression-deferred

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9431 | fixed | `8888881` | The cancel path refunds; a regression test cancels a captured order. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A | PPW-9431 | `Services/Fixture.cs` | not needed (fixture) |

## Decisions

### Refund on cancel (PPW-9431)

Leaving the capture in place and reconciling later would keep money on a cancelled
order for a day, so the cancel refunds inline.
