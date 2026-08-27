---
type: review-ledger
target: 921-gates-bad
updated: 2026-08-30
---

# Ledger — 921-gates-bad

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9211 | 🔴 | v1 | The retry ladder re-posts blind | `Services/Trigger.cs:10` | fixed | `abcd122` |
| PPW-9212 | 🟠 | v1 | The key is retired too early | `Services/Overlap.cs:20` | fixed | `abcd122` |
| PPW-9213 | 🟠 | v1 | The stale intent stays chargeable | `Services/Overlap.cs:40` | fixed | `abcd122` |
| PPW-9214 | 🟠 | v1 | The poll timer leaks | `Services/Late.cs:12` | fixed | `abcd122` |
| PPW-9215 | 🟠 | v1 | A late success double-fulfils | `Services/Late.cs:30` | fixed | `abcd122` |

## Details

### PPW-9211 — The retry ladder re-posts blind

- **What:** A retry with no budget re-posts an unknown outcome.
- **Evidence:** `Services/Trigger.cs:10`.
- **Suggested fix:** Cap the blind re-posts. **Test shape:** budget spent, claim kept. **Trigger-list-shaped:** yes (retry semantics change) — no approach pre-check run.
- **History:**
  - v1: found

### PPW-9212 — The key is retired too early

- **What:** The client retires the key on a decline.
- **Evidence:** `Services/Overlap.cs:20`.
- **Suggested fix:** Retire only on confirmation. **Test shape:** decline keeps the key. Not trigger-list-shaped.
- **History:**
  - v1: found

### PPW-9213 — The stale intent stays chargeable

- **What:** The server never re-sees the key, so the cancel never runs.
- **Evidence:** `Services/Overlap.cs:40`.
- **Suggested fix:** Cancel on hand-over. **Test shape:** hand-over cancels the intent. Not trigger-list-shaped.
- **History:**
  - v1: found

### PPW-9214 — The poll timer leaks

- **What:** The poll timer is never cleared on destroy.
- **Evidence:** `Services/Late.cs:12`.
- **Suggested fix:** Clear it on destroy. **Test shape:** destroy clears the timer. Not trigger-list-shaped.
- **History:**
  - v1: found

### PPW-9215 — A late success double-fulfils

- **What:** A late success on a failed order fulfils it a second time.
- **Evidence:** `Services/Late.cs:30`.
- **Suggested fix:** Gate on the terminal status. **Test shape:** late success is a no-op. Not trigger-list-shaped.
- **History:**
  - v1: found
