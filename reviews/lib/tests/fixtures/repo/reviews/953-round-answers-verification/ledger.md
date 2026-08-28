---
type: review-ledger
target: 953-round-answers-verification
updated: 2026-08-22
---

# Ledger — 953-round-answers-verification

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9531 | 🟠 | v1 | The AWB call runs twice for one order | `Services/Fixture.cs:37` | open | `0000000` |
| PPW-9532 | 🟠 | v1 | The thumbnail job skips the last page | `Jobs/Fixture.cs:45` | open | `0000000` |

## Details

### PPW-9531 — The AWB call runs twice for one order

- **What:** Two shipments are created when the button is double-clicked.
- **Evidence:** `Services/Fixture.cs:37`.
- **Suggested fix:** Guard on the existing AWB.
- **History:**
  - v1: found
  - v1: fix round — fixed at `5555551`
  - v1: verification — reopened (test-never-red)

### PPW-9532 — The thumbnail job skips the last page

- **What:** The page loop stops one short, so the last page never gets a thumbnail.
- **Evidence:** `Jobs/Fixture.cs:45`.
- **Suggested fix:** Include the last page.
- **History:**
  - v1: found
  - v1: fix round — fixed at `5555551`
  - v1: verification — reopened (still-reproducible)
