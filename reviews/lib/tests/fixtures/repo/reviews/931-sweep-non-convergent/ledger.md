---
type: review-ledger
target: 931-sweep-non-convergent
updated: 2026-07-06
---

# Ledger — 931-sweep-non-convergent

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9311 | 🟠 | v3 | The export writes the wrong VAT rate | `Services/Fixture.cs:61` | open | `0000000` |

## Details

### PPW-9311 — The export writes the wrong VAT rate

- **What:** The export writes the wrong VAT rate.
- **Evidence:** `Services/Fixture.cs:61`.
- **Suggested fix:** Rework the export.
- **History:**
  - v3: found
