---
type: review-ledger
target: 902-broken-target
updated: 2026-08-11
---

# Ledger — 902-broken-target

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9102 | 🟠 | v1 | The sweep skips a stalled row | `Services/Fixture.cs:12` | fixed at v1, see the history | `ddddddd` |
| PPW-9103 | 🟡 | v1 | The cap is proven through the helper only | `Services/Fixture.cs:31` | fixed | `ddddddd` |

## Details

### PPW-9102 — The sweep skips a stalled row

- **What:** A row that stalls before the sweep's status check leaves the window
  and never returns to it.
- **Evidence:** `Services/Fixture.cs:12`.
- **Suggested fix:** Widen the status predicate.
- **History:**
  - v1: found

### PPW-9103 — The cap is proven through the helper only

- **What:** The cap is asserted against the internal helper, so the public call
  could lose the cap with every test still green. This block runs past the
  twenty-line cap on purpose, and the lines below exist only to reach it.
- **Evidence:** `Services/Fixture.cs:31`.
- **Suggested fix:** Drive the public call and assert the cap there.
- **History:**
  - v1: found
  - v1: re-read, unchanged
  - v1: filler line one
  - v1: filler line two
  - v1: filler line three
  - v1: filler line four
  - v1: filler line five
  - v1: filler line six
  - v1: filler line seven
  - v1: filler line eight
  - v1: filler line nine
  - v1: filler line ten
  - v1: filler line eleven
  - v1: filler line twelve
  - v1: filler line thirteen
