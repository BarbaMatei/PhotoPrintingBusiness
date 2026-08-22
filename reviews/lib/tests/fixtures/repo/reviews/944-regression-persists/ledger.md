---
type: review-ledger
target: 944-regression-persists
updated: 2026-08-22
---

# Ledger — 944-regression-persists

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9441 | 🔴 | v1 | The stock release runs outside the order transaction | `Services/Fixture.cs:24` | verified | `9999991` |
| PPW-9442 | 🟠 | v1 | The transactional release double-counts a partial cancel | `Services/Fixture.cs:27` | open | `0000000` |
| PPW-9443 | 🟡 | v1 | The job logs the order id without the tenant | `Jobs/Fixture.cs:11` | verified | `9999992` |

## Details

### PPW-9441 — The stock release runs outside the order transaction

- **What:** A failed order keeps its stock reserved forever.
- **Evidence:** `Services/Fixture.cs:24`.
- **Suggested fix:** Release inside the same transaction.
- **History:**
  - v1: found
  - v1: fix round — fixed at `9999991`
  - v1: verification — held

### PPW-9442 — The transactional release double-counts a partial cancel

- **What:** Moving the release inside the transaction made a partial cancel release the whole line.
- **Evidence:** `Services/Fixture.cs:27`.
- **Suggested fix:** Release only the cancelled quantity.
- **History:**
  - v1: verification — found, caused by the fix for PPW-9441
  - v2: verification — still open, nothing has answered it

### PPW-9443 — The job logs the order id without the tenant

- **What:** A shared log line cannot be attributed to a tenant.
- **Evidence:** `Jobs/Fixture.cs:11`.
- **Suggested fix:** Log the tenant too.
- **History:**
  - v1: found — backlog
  - v2: fix round — fixed at `9999992`
  - v2: verification — held
