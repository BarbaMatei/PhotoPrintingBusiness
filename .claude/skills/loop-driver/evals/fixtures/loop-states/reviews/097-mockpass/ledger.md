---
type: review-ledger
target: 097-mockpass
updated: 2026-08-30
---

# Ledger — 097-mockpass

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9871 | 🔴 | v1 | The invoice total leaves out the shipping line | `Services/Fixture.cs:88` | fixed | `147fa87` |

## Details

### PPW-9871 — The invoice total leaves out the shipping line

- **What:** The total sums the print lines only, so every invoice under-charges by the
  shipping fee.
- **Evidence:** `Services/Fixture.cs:88`.
- **Suggested fix:** Add the shipping line to the total.
  **Fix brief:** `Services/Fixture.cs:88`; failing path = one print line plus shipping, total
  reads the print line alone; suggested test shape = the total equals lines + shipping.
  **Trigger-list-shaped:** no.
- **History:**
  - v1: found
  - v1: fix round — fixed at `147fa87`
