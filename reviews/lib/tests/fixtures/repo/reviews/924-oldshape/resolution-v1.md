---
type: resolution
target: 924-oldshape
version: 1
answers: review-v1.md
status: resolved
fixed_commit: abcd421
closed: 2026-08-30
---

# Resolution v1 — 924-oldshape

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9241 | fixed | `abcd421` | Key retired on confirmation only. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — payment key | PPW-9241 | `Services/Fixture.cs` | not needed (one-line fix) |

## Decisions

### Protocol — vague

The service now routes the retire call through the confirmation handler and
updates the stored key state via the new lifecycle method.
