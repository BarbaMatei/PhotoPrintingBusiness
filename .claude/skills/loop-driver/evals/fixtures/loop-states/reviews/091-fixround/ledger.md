---
type: review-ledger
target: 091-fixround
updated: 2026-08-25
---

# Ledger — 091-fixround

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9811 | 🔴 | v1 | The discount clamp is missing, so a coupon can pay the customer | `Services/Fixture.cs:31` | open |  |

## Details

### PPW-9811 — The discount clamp is missing, so a coupon can pay the customer

- **What:** A discount larger than the order total produces a negative total, which the
  payment call reads as a refund.
- **Evidence:** `Services/Fixture.cs:31`.
- **Suggested fix:** Clamp the discounted total to zero and refuse a negative discount.
  **Fix brief:** `Services/Fixture.cs:31`; failing path = total 100, discount 150 → -50;
  suggested test shape = the clamp holds at both ends (150 → 0, -10 → 100).
  **Trigger-list-shaped:** no.
- **History:**
  - v1: found
