---
type: resolution
target: 948-verification-files-mediums
version: 1
answers: review-v1.md
status: resolved
fixed_commit: bbbbbc1
closed: 2026-08-22
---

# Resolution v1 — 948-verification-files-mediums

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9481 | fixed | `bbbbbc1` | The number comes from a unique-keyed insert; a regression test runs two invoices in parallel. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A | PPW-9481 | `Services/Fixture.cs` | not needed (fixture) |

## Decisions

### Let the unique key hand out the number (PPW-9481)

Reading the last number and adding one races by construction; the insert's unique
key is the mechanism the rest of the codebase already relies on.
