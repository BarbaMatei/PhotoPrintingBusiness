---
type: review-ledger
target: 918-open-blocker
updated: 2026-08-22
---

# Ledger — 918-open-blocker

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9181 | 🔴 | v1 | A guest merge overwrites the signed-in cart | `Services/Fixture.cs:26` | open | `0000000` |
| PPW-9182 | 🔴 | v1 | The upload delete ignores the storage router | `Services/Fixture.cs:53` | verified | `4444441` |

## Details

### PPW-9181 — A guest merge overwrites the signed-in cart

- **What:** The merge replaces the account cart with the guest one, losing the earlier items.
- **Evidence:** `Services/Fixture.cs:26`.
- **Suggested fix:** Merge the two lists instead of replacing.
- **History:**
  - v1: found
  - v1: fix round — out of the round's scope

### PPW-9182 — The upload delete ignores the storage router

- **What:** The delete assumes local disk, so a remote upload is never removed.
- **Evidence:** `Services/Fixture.cs:53`.
- **Suggested fix:** Route the delete through the storage router.
- **History:**
  - v1: found
  - v1: fix round — fixed at `4444441`
  - v1: verification — held
