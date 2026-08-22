---
type: resolution
target: 918-open-blocker
version: 1
answers: review-v1.md
status: resolved
fixed_commit: 4444441
closed: 2026-08-22
---

# Resolution v1 — 918-open-blocker

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9182 | fixed | `4444441` | The delete goes through the router; a regression test deletes a remote upload. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A | PPW-9182 | `Services/Fixture.cs` | not needed (fixture) |

## Decisions

### Route the delete, do not special-case remote (PPW-9182)

A branch on the location would need the same branch at every other call site, so
the fix uses the router the rest of the code already goes through.
