---
type: review-ledger
target: 094-quiet
updated: 2026-08-29
---

# Ledger — 094-quiet

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9841 | 🔴 | v1 | The guest cart merge drops the signed-in items | `Services/Fixture.cs:44` | verified | `147fa87` |

## Details

### PPW-9841 — The guest cart merge drops the signed-in items

- **What:** The merge replaces the account cart with the guest cart, so items added before
  signing in disappear.
- **Evidence:** `Services/Fixture.cs:44`.
- **Suggested fix:** Merge the two lists by line, keeping both sides.
  **Fix brief:** `Services/Fixture.cs:44`; failing path = account cart with one line, guest
  cart with another, merge keeps only the guest line; suggested test shape = both lines
  survive the merge, in either order.
  **Trigger-list-shaped:** no.
- **History:**
  - v1: found
  - v1: fix round — fixed at `147fa87`
  - v2: verification — held
