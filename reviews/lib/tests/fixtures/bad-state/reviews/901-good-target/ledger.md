---
type: review-ledger
target: 901-good-target
updated: 2026-08-11
---

# Ledger — 901-good-target

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| PPW-9001 | 🔴 | v1 | A parallel init drops the guest token | `Services/Fixture.cs:41` | fixed | `ccccccc` |

## Details

### PPW-9001 — A parallel init drops the guest token

- **What:** This stub exists so the bad-state root has one target folder, which
  is what gives the index lint its set of known target keys.
- **Evidence:** `Services/Fixture.cs:41`.
- **Suggested fix:** Share one in-flight init across callers.
- **History:**
  - v1: found
