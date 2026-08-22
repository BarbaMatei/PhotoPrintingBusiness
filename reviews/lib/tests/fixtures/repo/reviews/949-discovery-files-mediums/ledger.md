---
type: review-ledger
target: 949-discovery-files-mediums
updated: 2026-08-22
---

# Ledger — 949-discovery-files-mediums

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9491 | 🟠 | v1 | The cart merge keeps the older price | `Services/Fixture.cs:50` | open | `0000000` |
| PPW-9492 | 🟠 | v1 | The gallery page size is unbounded | `Services/Fixture.cs:64` | open | `0000000` |

## Details

### PPW-9491 — The cart merge keeps the older price

- **What:** A guest cart merged after a price change charges the stale price.
- **Evidence:** `Services/Fixture.cs:50`.
- **Suggested fix:** Re-price on merge.
- **History:**
  - v1: found — queued under the threshold

### PPW-9492 — The gallery page size is unbounded

- **What:** A caller can ask for every row in one request.
- **Evidence:** `Services/Fixture.cs:64`.
- **Suggested fix:** Cap the page size.
- **History:**
  - v1: found — queued under the threshold
