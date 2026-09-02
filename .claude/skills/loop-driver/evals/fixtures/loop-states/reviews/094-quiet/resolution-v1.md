---
type: resolution
target: 094-quiet
version: 1
answers: review-v1.md
status: resolved
fixed_commit: 147fa87
closed: 2026-08-29
---

# Resolution v1 — 094-quiet

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9841 | fixed | `147fa87` | The merge keeps both sides; a regression test adds a line on each side and asserts both survive. |

## Scope

| Cluster | Findings | Files | Protocol |
|---|---|---|---|
| cart | PPW-9841 | `Services/Fixture.cs` | — |

## Decisions

### Merge by line rather than by cart (PPW-9841)

Replacing one cart with the other is what loses items, so the fix merges the lines and leaves
the cart identity alone. The alternative — keeping the account cart and discarding the guest
one — loses the other side instead.
