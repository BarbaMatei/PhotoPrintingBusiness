---
type: resolution
target: 917-sweep-before-cert
version: 1
answers: review-v1.md
status: resolved
fixed_commit: 3333331
closed: 2026-08-22
---

# Resolution v1 — 917-sweep-before-cert

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9171 | fixed | `3333331` | Each leg refunds against its own amount; a regression test splits a payment in two. |
| PPW-9173 | deferred | — | Waits on the boot-order rewrite. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A | PPW-9171, PPW-9173 | `Services/Fixture.cs` | not needed (fixture) |

## Decisions

### Refund per leg (PPW-9171)

Capping the whole refund at the order total would still let one leg over-refund,
so the cap moves onto the leg.

### Wait for the boot-order rewrite (PPW-9173)

The job's start-up ordering is the rewrite's subject; fixing it here would be
undone by that work.
