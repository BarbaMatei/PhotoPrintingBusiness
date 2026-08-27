---
type: resolution
target: 918-design-capped
version: 3
answers: review-v3.md
status: resolved
fixed_commit: ddddd20
---

# Resolution v3 — 918-design-capped

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9813 | fixed | `ddddd20` | Design pass: the payments protocol respecified and reimplemented. |

## Scope

| Cluster | Findings | Files | Protocol |
|---|---|---|---|
| A — payments protocol | PPW-9813 | `Services/Fixture.cs` | payments key lifecycle |

## Decisions

### Protocol — payments key lifecycle

One key exists per basket, never two; a settled key is retired exactly once.
