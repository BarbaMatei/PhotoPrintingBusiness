---
type: review
target: 955-pre-cutoff-resolved
version: 1
supersedes: null
commit: 5555560
branch: fixture/router-tests
pass-type: discovery
date: 2026-07-01
lenses: [correctness]
lenses-not-run: []
verdict: request-changes
blockers: [PPW-9551]
findings: { high: 1, medium: 1, low: 0, cleanup: 0, refuted: 0 }
tests: { dotnet: "12/12", frontend: "4/4" }
---

# Review v1 — 955-pre-cutoff-resolved

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-9551 | 🔴 | The refund posts twice on a retried webhook | `Services/Fixture.cs:12` | yes |
| PPW-9552 | 🟠 | The retry count is never logged | `Services/Fixture.cs:88` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|

## Notes for the fixer

- One blocker; the medium waits in the queue.
