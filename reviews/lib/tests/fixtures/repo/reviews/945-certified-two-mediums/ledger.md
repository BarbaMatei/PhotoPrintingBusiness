---
type: review-ledger
target: 945-certified-two-mediums
updated: 2026-08-22
---

# Ledger — 945-certified-two-mediums

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9451 | 🟠 | v1 | Queued medium 1 — the retry ladder reports nothing on leg 1 | `Jobs/Fixture.cs:11` | open | `0000000` |
| PPW-9452 | 🟠 | v1 | Queued medium 2 — the retry ladder reports nothing on leg 2 | `Jobs/Fixture.cs:12` | open | `0000000` |

## Details

### PPW-9451 — Queued medium 1 — the retry ladder reports nothing on leg 1

- **What:** Leg 1 of the ladder swallows its error, so a partial outage reads as slowness.
- **Evidence:** `Jobs/Fixture.cs:11`.
- **Suggested fix:** Log the leg and its error.
- **History:**
  - v1: found — queued
  - v1: certification — recorded as open at close

### PPW-9452 — Queued medium 2 — the retry ladder reports nothing on leg 2

- **What:** Leg 2 of the ladder swallows its error, so a partial outage reads as slowness.
- **Evidence:** `Jobs/Fixture.cs:12`.
- **Suggested fix:** Log the leg and its error.
- **History:**
  - v1: found — queued
  - v1: certification — recorded as open at close
