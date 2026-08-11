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
| PPW-9002 | 🟡 | v1 | The retry count is never logged | `Services/Fixture.cs:88` | backlog | `ccccccc` |

## Details

### PPW-9001 — A parallel init drops the guest token

- **What:** Two calls to the init endpoint race. The second stores its token over
  the first, so the first caller's uploads lose their session.
- **Evidence:** `Services/Fixture.cs:41` writes the token with no check for an
  in-flight call.
- **Suggested fix:** Share one in-flight init across callers.
- **History:**
  - v1: found
  - v1: fixed `ccccccc`

### PPW-9002 — The retry count is never logged

- **What:** A retry ladder runs three times and reports nothing, so a partial
  outage reads as ordinary slowness.
- **Evidence:** `Services/Fixture.cs:88`.
- **Suggested fix:** Count the retries and log the total once.
- **History:**
  - v1: found — sent to the queue
