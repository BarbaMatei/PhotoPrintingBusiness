---
type: resolution
target: 095-postcert
version: 3
answers: review-v2.md
status: resolved
fixed_commit: b3f98e2
closed: 2026-08-31
---

# Resolution v3 — 095-postcert

The post-certification round. It answers the one 🟠 the certification filed, so it carries no
review file of its own: its number is the next free resolution version.

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9852 | fixed | `b3f98e2` | The retry branch re-reads the row; the test asserts the counter through a fresh context. |

## Scope

| Cluster | Findings | Files | Protocol |
|---|---|---|---|
| retry | PPW-9852 | `Services/Fixture.cs` | — |

## Decisions

### Re-read rather than pass the counter down (PPW-9852)

Threading the counter through the call would give two sources for one number. The re-read
costs one query on the retry path only.
