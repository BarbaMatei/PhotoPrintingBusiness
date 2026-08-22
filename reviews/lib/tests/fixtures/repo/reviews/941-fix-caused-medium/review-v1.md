---
type: review
target: 941-fix-caused-medium
version: 1
supersedes: null
commit: 6666660
branch: fixture/router-tests
pass-type: discovery
date: 2026-08-22
lenses: [correctness]
lenses-not-run: []
verdict: request-changes
blockers: [PPW-9411]
findings: { high: 1, medium: 0, low: 0, cleanup: 0, refuted: 0 }
tests: { dotnet: "12/12", frontend: "4/4" }
---

# Review v1 — 941-fix-caused-medium

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-9411 | 🔴 | The order total ignores the discount cap | `Services/Fixture.cs:23` | yes |

## Refuted

| Suspicion | Why it is not real |
|---|---|

## Notes for the fixer

- One blocker, one cluster.
