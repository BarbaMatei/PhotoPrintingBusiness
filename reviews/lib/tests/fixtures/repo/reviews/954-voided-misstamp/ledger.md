---
type: review-ledger
target: 954-voided-misstamp
updated: 2026-08-30
---

# Ledger — 954-voided-misstamp

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9541 | 🔴 | v1 | The retry ladder re-posts blind | `Services/Trigger.cs:10` | fixed | `abcd222` |
| PPW-9542 | 🟠 | v1 | The key is retired too early | `Services/Overlap.cs:20` | fixed | `abcd222` |
| PPW-9543 | 🟠 | v1 | The stale intent stays chargeable | `Services/Overlap.cs:40` | fixed | `abcd222` |

## Details

### PPW-9541 — The retry ladder re-posts blind

- **What:** A retry with no budget re-posts an unknown outcome.
- **Evidence:** `Services/Trigger.cs:10`.
- **Suggested fix:** Cap the blind re-posts. **Test shape:** budget spent, claim kept. **Trigger-list-shaped:** yes (retry semantics change) — no approach pre-check run.
- **History:**
  - v1: found

### PPW-9542 — The key is retired too early

- **What:** The client retires the key on a decline.
- **Evidence:** `Services/Overlap.cs:20`.
- **Suggested fix:** Retire only on confirmation. **Test shape:** decline keeps the key. Not trigger-list-shaped.
- **History:**
  - v1: found

### PPW-9543 — The stale intent stays chargeable

- **What:** The server never re-sees the key, so the cancel never runs.
- **Evidence:** `Services/Overlap.cs:40`.
- **Suggested fix:** Cancel on hand-over. **Test shape:** hand-over cancels the intent. Not trigger-list-shaped.
- **History:**
  - v1: found
