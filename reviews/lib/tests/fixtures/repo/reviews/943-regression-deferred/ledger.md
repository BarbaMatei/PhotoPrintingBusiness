---
type: review-ledger
target: 943-regression-deferred
updated: 2026-08-22
---

# Ledger — 943-regression-deferred

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9431 | 🔴 | v1 | The cancel path leaves the order paid | `Services/Fixture.cs:17` | verified | `8888881` |
| PPW-9432 | 🟠 | v1 | The cancelled order keeps its reserved stock | `Services/Fixture.cs:21` | deferred | `0000000` |

## Details

### PPW-9431 — The cancel path leaves the order paid

- **What:** Cancelling after capture leaves the payment recorded against the order.
- **Evidence:** `Services/Fixture.cs:17`.
- **Suggested fix:** Refund on cancel.
- **History:**
  - v1: found
  - v1: fix round — fixed at `8888881`
  - v1: verification — held

### PPW-9432 — The cancelled order keeps its reserved stock

- **What:** The refund-on-cancel fix returns before the stock reservation is released.
- **Evidence:** `Services/Fixture.cs:21`.
- **Suggested fix:** Release the reservation in the same transaction.
- **History:**
  - v1: verification — found, caused by the fix for PPW-9431
  - v1: settled as deferred behind the reservation rewrite
