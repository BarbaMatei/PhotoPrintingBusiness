---
type: resolution
target: 901-good-target
version: 1
answers: review-v1.md
status: resolved
fixed_commit: ccccccc
closed: 2026-07-15
---

# Resolution v1 — 901-good-target

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9001 | fixed | `ccccccc` | One init runs at a time; a regression test drives two parallel calls. |
| PPW-9002 | backlog | — | Carried to the queue as a cleanup. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — guest init | PPW-9001 | `Services/Fixture.cs` | not needed (one-line guard) |

## Decisions

### Share one init across a burst of calls (PPW-9001)

A lock around the whole init would serialize every visitor. The fix shares the
in-flight call instead, so a burst pays one round trip.

### Send the retry-count gap to the queue (PPW-9002)

Low severity, no user impact — send it to the queue behind higher-priority work.
