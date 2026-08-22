---
type: review-ledger
target: 919-reopened-latest
updated: 2026-08-22
---

# Ledger — 919-reopened-latest

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9191 | 🟠 | v1 | The AWB call runs twice for one order | `Services/Fixture.cs:37` | open | `0000000` |
| PPW-9192 | 🟠 | v1 | The thumbnail job skips the last page | `Jobs/Fixture.cs:45` | open | `0000000` |

## Details

### PPW-9191 — The AWB call runs twice for one order

- **What:** Two shipments are created when the button is double-clicked.
- **Evidence:** `Services/Fixture.cs:37`.
- **Suggested fix:** Guard on the existing AWB.
- **History:**
  - v1: found
  - v1: fix round — fixed at `5555551`
  - v1: verification — reopened (test-never-red)

### PPW-9192 — The thumbnail job skips the last page

- **What:** The page loop stops one short, so the last page never gets a thumbnail.
- **Evidence:** `Jobs/Fixture.cs:45`.
- **Suggested fix:** Include the last page.
- **History:**
  - v1: found
  - v1: fix round — fixed at `5555551`
  - v1: verification — reopened (still-reproducible)
