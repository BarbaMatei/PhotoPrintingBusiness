---
type: review-ledger
target: 941-fix-caused-medium
updated: 2026-08-22
---

# Ledger — 941-fix-caused-medium

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9411 | 🔴 | v1 | The order total ignores the discount cap | `Services/Fixture.cs:23` | verified | `6666661` |
| PPW-9412 | 🟠 | v1 | The capped discount is written to the invoice uncapped | `Services/Fixture.cs:29` | open | `0000000` |

## Details

### PPW-9411 — The order total ignores the discount cap

- **What:** A stacked discount can take the total below zero.
- **Evidence:** `Services/Fixture.cs:23`.
- **Suggested fix:** Cap the discount at the total.
- **History:**
  - v1: found
  - v1: fix round — fixed at `6666661`
  - v1: verification — held

### PPW-9412 — The capped discount is written to the invoice uncapped

- **What:** The cap is applied to the total but not to the line the invoice prints, so the two disagree.
- **Evidence:** `Services/Fixture.cs:29`.
- **Suggested fix:** Cap once, then read the capped value in both places.
- **History:**
  - v1: verification — found, caused by the fix for PPW-9411
