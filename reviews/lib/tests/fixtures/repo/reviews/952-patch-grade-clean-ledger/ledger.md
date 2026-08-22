---
type: review-ledger
target: 952-patch-grade-clean-ledger
updated: 2026-08-22
---

# Ledger — 952-patch-grade-clean-ledger

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9521 | 🟡 | v1 | The upload error page drops the correlation id |  | verified |  |

## Details

### PPW-9521 — The upload error page drops the correlation id

- **What:** The page renders without the id, so a user's report cannot be traced to a request.
- **Evidence:** .
- **Suggested fix:** Render the id in the footer.
- **History:**
  - v1: found
  - v1: fix round — fixed at   - v1: verification — held
