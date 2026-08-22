---
type: resolution
target: 944-regression-persists
version: 2
answers: review-v1.md
status: resolved
fixed_commit: 9999992
closed: 2026-08-22
---

# Resolution v2 — 944-regression-persists

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9443 | fixed | `9999992` | The log line carries the tenant; a regression test asserts the field. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A | PPW-9443 | `Services/Fixture.cs` | not needed (fixture) |

## Decisions

### Log the tenant alongside the order (PPW-9443)

The alternative — deriving the tenant at read time — needs a lookup per log line,
so the writer carries it instead.
