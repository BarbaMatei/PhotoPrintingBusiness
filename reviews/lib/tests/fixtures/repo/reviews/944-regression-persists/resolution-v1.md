---
type: resolution
target: 944-regression-persists
version: 1
answers: review-v1.md
status: resolved
fixed_commit: 9999991
closed: 2026-08-22
---

# Resolution v1 — 944-regression-persists

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9441 | fixed | `9999991` | The release joins the order transaction; a regression test fails the order mid-flight. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A | PPW-9441 | `Services/Fixture.cs` | not needed (fixture) |

## Decisions

### Release inside the transaction (PPW-9441)

A compensating job would leave a window where the stock is reserved for a failed
order, so the release joins the transaction that owns the order.
