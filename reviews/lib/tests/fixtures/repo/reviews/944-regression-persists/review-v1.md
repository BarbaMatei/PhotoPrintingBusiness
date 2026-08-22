---
type: review
target: 944-regression-persists
version: 1
supersedes: null
commit: 9999990
branch: fixture/router-tests
pass-type: discovery
date: 2026-08-22
lenses: [correctness]
lenses-not-run: []
verdict: request-changes
blockers: [PPW-9441]
findings: { high: 1, medium: 0, low: 1, cleanup: 0, refuted: 0 }
tests: { dotnet: "12/12", frontend: "4/4" }
---

# Review v1 — 944-regression-persists

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-9441 | 🔴 | The stock release runs outside the order transaction | `Services/Fixture.cs:24` | yes |
| PPW-9443 | 🟡 | The job logs the order id without the tenant | `Jobs/Fixture.cs:11` | backlog |

## Refuted

| Suspicion | Why it is not real |
|---|---|

## Notes for the fixer

- One blocker; the low goes to the queue.
