---
type: review-ledger
target: 947-certified-open-blocker
updated: 2026-08-22
---

# Ledger — 947-certified-open-blocker

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9471 | 🔴 | v1 | The webhook replay refunds twice | `Services/Fixture.cs:44` | open | `0000000` |
| PPW-9472 | 🟠 | v1 | Queued medium 1 — the retry ladder reports nothing on leg 1 | `Jobs/Fixture.cs:11` | open | `0000000` |

## Details

### PPW-9471 — The webhook replay refunds twice

- **What:** A replayed refund webhook credits the card a second time.
- **Evidence:** `Services/Fixture.cs:44`.
- **Suggested fix:** Key the refund on the webhook id.
- **History:**
  - post-certification: found by the hand-check of the payment log

### PPW-9472 — Queued medium 1 — the retry ladder reports nothing on leg 1

- **What:** Leg 1 of the ladder swallows its error, so a partial outage reads as slowness.
- **Evidence:** `Jobs/Fixture.cs:11`.
- **Suggested fix:** Log the leg and its error.
- **History:**
  - v1: found — queued
  - v1: certification — recorded as open at close
