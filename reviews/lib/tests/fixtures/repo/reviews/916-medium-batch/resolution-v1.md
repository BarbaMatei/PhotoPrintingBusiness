---
type: resolution
target: 916-medium-batch
version: 1
answers: review-v1.md
status: resolved
fixed_commit: 2222221
closed: 2026-08-22
---

# Resolution v1 — 916-medium-batch

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9161 | fixed | `2222221` | The charge is keyed on the idempotency key; a regression test replays the webhook. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A | PPW-9161 | `Services/Fixture.cs` | not needed (fixture) |

## Decisions

### Key the charge, do not lock the row (PPW-9161)

A row lock would serialize every payment. The unique key on the order rejects the
replay instead, which is the mechanism the rest of the codebase already uses.
