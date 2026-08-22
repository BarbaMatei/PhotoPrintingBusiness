---
type: review-ledger
target: 942-resolved-unverified
updated: 2026-08-22
---

# Ledger — 942-resolved-unverified

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9421 | 🔴 | v1 | The payment webhook trusts an unsigned body | `Services/Fixture.cs:15` | in-progress | `0000000` |

## Details

### PPW-9421 — The payment webhook trusts an unsigned body

- **What:** The handler acts on the body before checking the signature.
- **Evidence:** `Services/Fixture.cs:15`.
- **Suggested fix:** Verify the signature first.
- **History:**
  - v1: found
