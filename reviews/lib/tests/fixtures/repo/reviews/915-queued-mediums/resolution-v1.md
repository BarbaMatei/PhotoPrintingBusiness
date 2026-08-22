---
type: resolution
target: 915-queued-mediums
version: 1
answers: review-v1.md
status: resolved
fixed_commit: 1111111
closed: 2026-08-22
---

# Resolution v1 — 915-queued-mediums

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9151 | fixed | `1111111` | The total rounds once; a regression test drives a three-line invoice. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A | PPW-9151 | `Services/Fixture.cs` | not needed (fixture) |

## Decisions

### Round the total, not the lines (PPW-9151)

Rounding per line is what loses the remainder, so the fix moves the single
rounding step to the total.
