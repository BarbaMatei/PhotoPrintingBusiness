---
type: review-ledger
target: 916-medium-batch
updated: 2026-08-22
---

# Ledger — 916-medium-batch

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9161 | 🔴 | v1 | A duplicate webhook charges the card twice | `Services/Fixture.cs:31` | verified | `2222221` |
| PPW-9162 | 🟠 | v1 | The retry ladder never logs its total | `Jobs/Fixture.cs:14` | open | `0000000` |
| PPW-9163 | 🟠 | v1 | The status filter ignores a trailing space | `Services/Fixture.cs:58` | in-progress | `0000000` |
| PPW-9164 | 🟠 | v1 | A locker order shows the courier address | `Services/Fixture.cs:72` | open | `0000000` |

## Details

### PPW-9161 — A duplicate webhook charges the card twice

- **What:** The handler has no idempotency check, so a retried webhook charges again.
- **Evidence:** `Services/Fixture.cs:31`.
- **Suggested fix:** Key the charge on the idempotency key.
- **History:**
  - v1: found
  - v1: fix round — fixed at `2222221`
  - v1: verification — held

### PPW-9162 — The retry ladder never logs its total

- **What:** Three retries report nothing, so a partial outage reads as slowness.
- **Evidence:** `Jobs/Fixture.cs:14`.
- **Suggested fix:** Log the retry total once.
- **History:**
  - v1: found — queued

### PPW-9163 — The status filter ignores a trailing space

- **What:** The filter compares raw strings, so " paid" never matches.
- **Evidence:** `Services/Fixture.cs:58`.
- **Suggested fix:** Trim before comparing.
- **History:**
  - v1: found — queued
  - v2: fix round — picked up in the batch

### PPW-9164 — A locker order shows the courier address

- **What:** The invoice reads the courier address for a locker delivery.
- **Evidence:** `Services/Fixture.cs:72`.
- **Suggested fix:** Read the locker address for locker orders.
- **History:**
  - v1: found — queued
