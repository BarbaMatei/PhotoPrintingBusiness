---
type: review-ledger
target: 915-queued-mediums
updated: 2026-08-22
---

# Ledger — 915-queued-mediums

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9151 | 🔴 | v1 | The invoice total drops the rounding remainder | `Services/Fixture.cs:12` | verified | `1111111` |
| PPW-9152 | 🟠 | v1 | The sweep job logs below the level floor | `Jobs/Fixture.cs:20` | open | `0000000` |
| PPW-9153 | 🟠 | v1 | A cancelled upload leaves its temp file behind | `Services/Fixture.cs:44` | open | `0000000` |

## Details

### PPW-9151 — The invoice total drops the rounding remainder

- **What:** Each line rounds down, so the total is short by up to a leu per line.
- **Evidence:** `Services/Fixture.cs:12`.
- **Suggested fix:** Round the total once, not per line.
- **History:**
  - v1: found
  - v1: fix round — fixed at `1111111`
  - v1: verification — held

### PPW-9152 — The sweep job logs below the level floor

- **What:** The sweep logs at Debug, under the configured floor, so nothing is recorded.
- **Evidence:** `Jobs/Fixture.cs:20`.
- **Suggested fix:** Log at Information.
- **History:**
  - v1: found — queued under the threshold

### PPW-9153 — A cancelled upload leaves its temp file behind

- **What:** The cancel path returns before the temp file is deleted.
- **Evidence:** `Services/Fixture.cs:44`.
- **Suggested fix:** Delete in a finally block.
- **History:**
  - v1: found — queued under the threshold
