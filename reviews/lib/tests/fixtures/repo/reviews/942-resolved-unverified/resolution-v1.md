---
type: resolution
target: 942-resolved-unverified
version: 1
answers: review-v1.md
status: resolved
fixed_commit: 7777771
closed: 2026-08-22
---

# Resolution v1 — 942-resolved-unverified

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9421 | fixed | `7777771` | The signature is checked first; a regression test posts an unsigned body. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A | PPW-9421 | `Services/Fixture.cs` | not needed (fixture) |

## Decisions

### Check the signature before anything else (PPW-9421)

Any ordering that reads the body first leaves a window where an unsigned payload
has already been acted on.
