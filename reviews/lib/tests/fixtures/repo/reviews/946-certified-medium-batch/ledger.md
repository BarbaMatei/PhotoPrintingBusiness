---
type: review-ledger
target: 946-certified-medium-batch
updated: 2026-08-22
---

# Ledger — 946-certified-medium-batch

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9461 | 🟠 | v1 | Queued medium 1 — the retry ladder reports nothing on leg 1 | `Jobs/Fixture.cs:11` | open | `0000000` |
| PPW-9462 | 🟠 | v1 | Queued medium 2 — the retry ladder reports nothing on leg 2 | `Jobs/Fixture.cs:12` | open | `0000000` |
| PPW-9463 | 🟠 | v1 | Queued medium 3 — the retry ladder reports nothing on leg 3 | `Jobs/Fixture.cs:13` | open | `0000000` |

## Details

### PPW-9461 — Queued medium 1 — the retry ladder reports nothing on leg 1

- **What:** Leg 1 of the ladder swallows its error, so a partial outage reads as slowness.
- **Evidence:** `Jobs/Fixture.cs:11`.
- **Suggested fix:** Log the leg and its error.
- **History:**
  - v1: found — queued
  - v1: certification — recorded as open at close

### PPW-9462 — Queued medium 2 — the retry ladder reports nothing on leg 2

- **What:** Leg 2 of the ladder swallows its error, so a partial outage reads as slowness.
- **Evidence:** `Jobs/Fixture.cs:12`.
- **Suggested fix:** Log the leg and its error.
- **History:**
  - v1: found — queued
  - v1: certification — recorded as open at close

### PPW-9463 — Queued medium 3 — the retry ladder reports nothing on leg 3

- **What:** Leg 3 of the ladder swallows its error, so a partial outage reads as slowness.
- **Evidence:** `Jobs/Fixture.cs:13`.
- **Suggested fix:** Log the leg and its error.
- **History:**
  - v1: found — queued
  - v1: certification — recorded as open at close
