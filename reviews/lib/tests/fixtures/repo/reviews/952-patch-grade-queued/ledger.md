---
type: review-ledger
target: 952-patch-grade-queued
updated: 2026-08-22
---

# Ledger — 952-patch-grade-queued

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9520 | 🟠 | v1 | The export drops the header row | `Services/Fixture.cs:8` | verified | `5555551` |
| PPW-9521 | 🟠 | v1 | The retry counter never resets | `Jobs/Fixture.cs:31` | open | `0000000` |
| PPW-9522 | 🟠 | v1 | A stale cache entry survives a rename | `Services/Fixture.cs:57` | open | `0000000` |

## Details

### PPW-9520 — The export drops the header row

- **What:** The CSV export streams rows before the header, so the header never lands.
- **Evidence:** `Services/Fixture.cs:8`.
- **Suggested fix:** Write the header in the same stream open.
- **History:**
  - v1: found
  - v1: fix round — fixed at `5555551`
  - v1: verification — held

### PPW-9521 — The retry counter never resets

- **What:** A successful send leaves the counter at its last value, so the next failure backs off too far.
- **Evidence:** `Jobs/Fixture.cs:31`.
- **Suggested fix:** Reset on success.
- **History:**
  - v1: found — queued under the threshold

### PPW-9522 — A stale cache entry survives a rename

- **What:** The rename path never invalidates the old key.
- **Evidence:** `Services/Fixture.cs:57`.
- **Suggested fix:** Invalidate both keys on rename.
- **History:**
  - v1: found — queued under the threshold
