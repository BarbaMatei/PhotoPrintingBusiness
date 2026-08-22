---
type: review-ledger
target: 948-verification-files-mediums
updated: 2026-08-22
---

# Ledger — 948-verification-files-mediums

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9481 | 🔴 | v1 | The invoice series can hand out one number twice | `Services/Fixture.cs:33` | verified | `bbbbbc1` |
| PPW-9482 | 🟠 | v1 | The series reset is not covered by any test | `Services/Fixture.cs:39` | open | `0000000` |
| PPW-9483 | 🟠 | v1 | The number is logged at Debug, under the floor | `Services/Fixture.cs:41` | open | `0000000` |

## Details

### PPW-9481 — The invoice series can hand out one number twice

- **What:** Two concurrent invoices read the same last number.
- **Evidence:** `Services/Fixture.cs:33`.
- **Suggested fix:** Take the number from a unique-keyed insert.
- **History:**
  - v1: found
  - v1: fix round — fixed at `bbbbbc1`
  - v1: verification — held

### PPW-9482 — The series reset is not covered by any test

- **What:** Nothing exercises the yearly reset, so a regression there would ship unnoticed.
- **Evidence:** `Services/Fixture.cs:39`.
- **Suggested fix:** Add a test that rolls the year over.
- **History:**
  - v1: verification — found while reading the fix

### PPW-9483 — The number is logged at Debug, under the floor

- **What:** The issued number is never recorded, so a duplicate cannot be traced after the fact.
- **Evidence:** `Services/Fixture.cs:41`.
- **Suggested fix:** Log it at Information.
- **History:**
  - v1: verification — found while reading the fix
