---
type: review
target: 952-patch-grade-queued
version: 1
supersedes: null
commit: 5555550
branch: fixture/router-tests
pass-type: discovery
date: 2026-08-22
lenses: [correctness]
lenses-not-run: []
verdict: request-changes
blockers: []
findings: { high: 0, medium: 3, low: 0, cleanup: 0, refuted: 0 }
tests: { dotnet: "12/12", frontend: "4/4" }
---

# Review v1 — 952-patch-grade-queued

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-9520 | 🟠 | The export drops the header row | `Services/Fixture.cs:8` | yes |
| PPW-9521 | 🟠 | The retry counter never resets | `Jobs/Fixture.cs:31` | queue |
| PPW-9522 | 🟠 | A stale cache entry survives a rename | `Services/Fixture.cs:57` | queue |

## Refuted

| Suspicion | Why it is not real |
|---|---|

## Notes for the fixer

- One medium is taken now; the other two go to the queue.
