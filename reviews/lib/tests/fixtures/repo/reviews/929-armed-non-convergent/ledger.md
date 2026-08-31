---
type: review-ledger
target: 929-armed-non-convergent
updated: 2026-07-06
---

# Ledger — 929-armed-non-convergent

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9291 | 🔴 | v3 | The refund path double-credits a split payment | `Services/Fixture.cs:19` | open | `0000000` |

## Details

### PPW-9291 — The refund path double-credits a split payment

- **What:** The refund path double-credits a split payment.
- **Evidence:** `Services/Fixture.cs:19`.
- **Suggested fix:** Rework the refund.
- **History:**
  - v3: found
