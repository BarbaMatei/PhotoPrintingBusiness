---
type: review
target: 915-queued-mediums
version: 1
supersedes: null
commit: 1111110
branch: fixture/router-tests
pass-type: discovery
date: 2026-08-22
lenses: [correctness]
lenses-not-run: []
verdict: request-changes
blockers: [PPW-9151]
findings: { high: 1, medium: 2, low: 0, cleanup: 0, refuted: 0 }
tests: { dotnet: "12/12", frontend: "4/4" }
---

# Review v1 — 915-queued-mediums

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-9151 | 🔴 | The invoice total drops the rounding remainder | `Services/Fixture.cs:12` | yes |
| PPW-9152 | 🟠 | The sweep job logs below the level floor | `Jobs/Fixture.cs:20` | queue |
| PPW-9153 | 🟠 | A cancelled upload leaves its temp file behind | `Services/Fixture.cs:44` | queue |

## Refuted

| Suspicion | Why it is not real |
|---|---|

## Notes for the fixer

- The blocker is the only row this round takes; the two mediums go to the queue.
