---
type: resolution
target: 097-mockpass
version: 1
answers: review-v1.md
status: resolved
fixed_commit: 147fa87
closed: 2026-08-30
---

# Resolution v1 — 097-mockpass

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9871 | fixed | `147fa87` | The total adds the shipping line; the test asserts lines + shipping against a hand-computed figure. |

## Scope

| Cluster | Findings | Files | Protocol |
|---|---|---|---|
| invoicing | PPW-9871 | `Services/Fixture.cs` | — |

## Decisions

### Sum the lines rather than special-case shipping (PPW-9871)

Shipping is a line like any other, so the total sums every line instead of adding one field
that a second fee would have to be added to again.

### Revert proofs

- PPW-9871: dropping the shipping line back out of the sum in `Services/Fixture.cs:88` turns
  the total test red on the hand-computed figure.
