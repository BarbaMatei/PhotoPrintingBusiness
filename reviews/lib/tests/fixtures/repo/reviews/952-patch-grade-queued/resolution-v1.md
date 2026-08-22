---
type: resolution
target: 952-patch-grade-queued
version: 1
answers: review-v1.md
status: resolved
fixed_commit: 5555551
closed: 2026-08-22
---

# Resolution v1 — 952-patch-grade-queued

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9520 | fixed | `5555551` | The header row is written first; a regression test reads it back. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A | PPW-9520 | `Services/Fixture.cs` | not needed (fixture) |

## Decisions

### Write the header before the rows (PPW-9520)

The exporter streamed rows before the header; the fix writes the header in the
same stream open.
