---
type: review-ledger
target: discount-module
updated: 2026-08-31
---

# Ledger — discount-module

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9701 | 🔴 | v1 | A discount above the total pays the customer | `src/discount.mjs:3` | open |  |
| PPW-9702 | 🔴 | v1 | A cart lookup returns every user's lines | `src/cart.mjs:8` | open |  |
| PPW-9703 | 🟡 | v1 | The pricing doc states the unclamped behaviour as the rule | `docs/pricing.md:9` | open |  |
| PPW-9704 | ⚪ | v1 | The discount ceiling is declared in two modules | `src/cart.mjs:1` | open |  |

## Details

### PPW-9701 — A discount above the total pays the customer

- **What:** `applyDiscount` subtracts the discount as given, so a discount larger than the
  total returns a negative number, and a negative discount raises the total.
- **Evidence:** `src/discount.mjs:3` — `return total - discount`.
- **Suggested fix:** Clamp the result to zero and refuse a negative discount.
  **Fix brief:** `src/discount.mjs:3`; traced failing path = `applyDiscount(100, 150)`
  returns `-50`, and `applyDiscount(100, -10)` returns `110`; suggested test shape = assert
  the clamp at both ends against literals, 0 for the over-discount and the untouched total
  for the negative one.
  **Trigger-list-shaped:** no.
- **History:**
  - v1: found

### PPW-9702 — A cart lookup returns every user's lines

- **What:** `getUserCart` ignores its `userId` argument and returns the whole store, so one
  customer's basket is served to another.
- **Evidence:** `src/cart.mjs:8` — `return CARTS`.
- **Suggested fix:** Key the lookup on the user.
  **Fix brief:** `src/cart.mjs:8`; traced failing path = `getUserCart('bob')` returns
  Alice's line too; suggested test shape = assert Bob's lookup carries only Bob's lines and
  Alice's only Alice's, by userId, never by count alone.
  **Trigger-list-shaped:** yes — the lookup gains a key it did not have.
- **History:**
  - v1: found

### PPW-9703 — The pricing doc states the unclamped behaviour as the rule

- **What:** The pricing rules describe the negative total as intended behaviour, so the doc
  will contradict the fix for PPW-9701.
- **Evidence:** `docs/pricing.md:9`.
- **Suggested fix:** State the clamp as the rule; doc drift is fixed token-wide, so check
  every sentence that describes the discount, not only this line.
  **Trigger-list-shaped:** no.
- **History:**
  - v1: found

### PPW-9704 — The discount ceiling is declared in two modules

- **What:** `MAX_DISCOUNT_PERCENT` is exported from two modules with the same value; a change
  to one leaves the other stating the old ceiling.
- **Evidence:** `src/cart.mjs:1` and `src/discount.mjs:1`.
- **Suggested fix:** One home, re-exported or imported by the other.
  **Trigger-list-shaped:** no.
- **History:**
  - v1: found
