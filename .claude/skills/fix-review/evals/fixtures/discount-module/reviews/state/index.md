---
type: review-index
updated: 2026-08-31
---

# Review index — fix-review eval fixture

One synthetic target. The fixer never writes here: the index row belongs to the driver's
verification step, which is outside a fix round's contract.

## Targets at a glance

| Target | State |
|---|---|
| discount-module | v1 found 2 🔴, 1 🟡, 1 ⚪; the round that answers them is what the eval runs. |

## Passes

| Date | Target | Pass | Verdict | New H/M/L/C | Outcome | Files |
|---|---|---|---|---|---|---|
| 2026-08-31 | discount | v1 discovery (6 lenses) | request-changes | 2/0/1/1 | Two blockers: the discount is unclamped and the cart lookup ignores its user | [review](../discount-module/review-v1.md) · [ledger](../discount-module/ledger.md) |
