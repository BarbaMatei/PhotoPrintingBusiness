---
type: review-ledger
target: 955-pre-cutoff-resolved
updated: 2026-07-04
---

# Ledger — 955-pre-cutoff-resolved

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9551 | 🔴 | v1 | The refund posts twice on a retried webhook | `Services/Fixture.cs:12` | verified | `5555562` |
| PPW-9552 | 🟠 | v1 | The retry count is never logged | `Services/Fixture.cs:88` | open | `5555560` |

## Details

### PPW-9551 — The refund posts twice on a retried webhook

- **What:** A retried webhook posts the refund a second time.
- **Evidence:** `Services/Fixture.cs:12`.
- **Suggested fix:** Key the refund on the idempotency key.
- **History:**
  - v1: found
  - v1: fix round — fixed at `5555561`
  - v1: verification — held at `5555562`

### PPW-9552 — The retry count is never logged

- **What:** Nothing records how many retries a payment took.
- **Evidence:** `Services/Fixture.cs:88`.
- **Suggested fix:** Log the count with the payment id.
- **History:**
  - v1: found
