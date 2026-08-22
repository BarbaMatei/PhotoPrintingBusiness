---
type: review-ledger
target: 917-sweep-before-cert
updated: 2026-08-22
---

# Ledger — 917-sweep-before-cert

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9171 | 🔴 | v1 | The refund path double-credits a split payment | `Services/Fixture.cs:19` | verified | `3333331` |
| PPW-9172 | 🟠 | v1 | The export writes the wrong VAT rate for services | `Services/Fixture.cs:61` | open | `0000000` |
| PPW-9173 | 🟠 | v1 | The nightly job runs before the rate table loads | `Jobs/Fixture.cs:8` | deferred | `0000000` |
| PPW-9174 | 🟡 | v1 | The error page loses the correlation id | `Services/Fixture.cs:90` | open | `0000000` |

## Details

### PPW-9171 — The refund path double-credits a split payment

- **What:** A split payment refunds each leg against the full total.
- **Evidence:** `Services/Fixture.cs:19`.
- **Suggested fix:** Refund per leg, capped at that leg.
- **History:**
  - v1: found
  - v1: fix round — fixed at `3333331`
  - v1: verification — held

### PPW-9172 — The export writes the wrong VAT rate for services

- **What:** The export writes the goods rate for a service line.
- **Evidence:** `Services/Fixture.cs:61`.
- **Suggested fix:** Read the rate off the line type.
- **History:**
  - v1: found — queued under the threshold

### PPW-9173 — The nightly job runs before the rate table loads

- **What:** The job and the loader race at boot; the job wins about one night in ten.
- **Evidence:** `Jobs/Fixture.cs:8`.
- **Suggested fix:** Wait on the loader.
- **History:**
  - v1: found — queued
  - v1: fix round — deferred behind the boot-order rewrite

### PPW-9174 — The error page loses the correlation id

- **What:** The page renders without the id, so a report cannot be traced.
- **Evidence:** `Services/Fixture.cs:90`.
- **Suggested fix:** Render the id in the footer.
- **History:**
  - v1: found — backlog
