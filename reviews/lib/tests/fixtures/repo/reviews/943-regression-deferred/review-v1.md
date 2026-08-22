---
type: review
target: 943-regression-deferred
version: 1
supersedes: null
commit: 8888880
branch: fixture/router-tests
pass-type: discovery
date: 2026-08-22
lenses: [correctness]
lenses-not-run: []
verdict: request-changes
blockers: [PPW-9431]
findings: { high: 1, medium: 0, low: 0, cleanup: 0, refuted: 0 }
tests: { dotnet: "12/12", frontend: "4/4" }
---

# Review v1 — 943-regression-deferred

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-9431 | 🔴 | The cancel path leaves the order paid | `Services/Fixture.cs:17` | yes |

## Refuted

| Suspicion | Why it is not real |
|---|---|

## Notes for the fixer

- One blocker, one cluster.
