---
type: resolution
target: 941-fix-caused-medium
version: 1
answers: review-v1.md
status: resolved
fixed_commit: 6666661
closed: 2026-08-22
---

# Resolution v1 — 941-fix-caused-medium

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9411 | fixed | `6666661` | The discount is capped at the total; a regression test stacks two discounts. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A | PPW-9411 | `Services/Fixture.cs` | not needed (fixture) |

## Decisions

### Cap the discount, do not clamp the total (PPW-9411)

Clamping the total would hide the arithmetic error rather than fix it, so the cap
sits on the discount where the value is computed.
