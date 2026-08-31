---
type: review-ledger
target: 930-batch-non-convergent
updated: 2026-07-06
---

# Ledger — 930-batch-non-convergent

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9301 | 🟠 | v3 | The export writes the wrong VAT rate | `Services/Fixture.cs:61` | open | `0000000` |
| PPW-9302 | 🟠 | v3 | The invoice number skips a value on retry | `Services/Fixture.cs:72` | open | `0000000` |
| PPW-9303 | 🟠 | v3 | The nightly job runs before the rate table loads | `Services/Fixture.cs:84` | open | `0000000` |

## Details

### PPW-9301 — The export writes the wrong VAT rate

- **What:** The export writes the wrong VAT rate.
- **Evidence:** `Services/Fixture.cs:61`.
- **Suggested fix:** Rework the export.
- **History:**
  - v3: found

### PPW-9302 — The invoice number skips a value on retry

- **What:** The invoice number skips a value on retry.
- **Evidence:** `Services/Fixture.cs:72`.
- **Suggested fix:** Rework the invoice.
- **History:**
  - v3: found

### PPW-9303 — The nightly job runs before the rate table loads

- **What:** The nightly job runs before the rate table loads.
- **Evidence:** `Services/Fixture.cs:84`.
- **Suggested fix:** Rework the nightly.
- **History:**
  - v3: found
