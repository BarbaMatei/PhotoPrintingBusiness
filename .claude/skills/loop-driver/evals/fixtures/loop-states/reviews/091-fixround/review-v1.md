---
type: review
target: 091-fixround
version: 1
supersedes: null
commit: c09675d
branch: fixture/loop-states
pass-type: discovery
date: 2026-08-25
lenses: [correctness, security, requirements, quality, tests-coverage, completeness-critic, db-parity, input-validation, observability, race, frontend-ux]
lenses-not-run: []
verdict: request-changes
blockers: [PPW-9811]
findings: { high: 1, medium: 0, low: 0, cleanup: 0, refuted: 0 }
tests: { dotnet: "12/12", frontend: "4/4" }
---

# Review v1 — 091-fixround

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-9811 | 🔴 | The discount clamp is missing, so a coupon can pay the customer | `Services/Fixture.cs:31` | yes |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| The coupon lookup is unbounded | It stops at the first match |

## Notes for the fixer

- One blocker, one cluster. Nothing else in the file is in scope.
