---
type: review-ledger
target: 095-postcert
updated: 2026-08-31
---

# Ledger — 095-postcert

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9852 | 🟠 | v2 | The retry counter is read before the row is refreshed | `Services/Fixture.cs:73` | open |  |

## Details

### PPW-9852 — The retry counter is read before the row is refreshed

- **What:** The handler reads the retry counter from the entity it loaded before the retry
  was written, so the third attempt is treated as the first.
- **Evidence:** `Services/Fixture.cs:73`.
- **Suggested fix:** Re-read the row inside the retry branch.
  **Fix brief:** `Services/Fixture.cs:73`; failing path = two retries, counter still 0;
  suggested test shape = the counter reads 2 through a fresh context after two retries.
  **Trigger-list-shaped:** no.
- **History:**
  - v2: found by the certification pass
