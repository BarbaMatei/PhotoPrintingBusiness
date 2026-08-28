---
type: resolution
target: 955-pre-cutoff-resolved
version: 1
answers: review-v1.md
status: resolved
fixed_commit: 5555561
closed: 2026-07-04
---

# Resolution v1 — 955-pre-cutoff-resolved

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9551 | fixed | `5555561` | The refund is keyed on the idempotency key; a regression test replays the webhook. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A | PPW-9551 | `Services/Fixture.cs` | not needed (fixture) |

## Decisions

### One key, not a lock (PPW-9551)

The key column is unique already, so the guard reads it rather than taking a lock.
