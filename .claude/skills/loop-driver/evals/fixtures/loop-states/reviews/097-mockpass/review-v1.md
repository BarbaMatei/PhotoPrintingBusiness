---
type: review
target: 097-mockpass
version: 1
supersedes: null
commit: c09675d
branch: fixture/loop-states
pass-type: discovery
date: 2026-08-30
lenses: [correctness, security, requirements, quality, tests-coverage, completeness-critic, db-parity, input-validation, observability, race, frontend-ux]
lenses-not-run: []
verdict: request-changes
blockers: [PPW-9871]
findings: { high: 1, medium: 0, low: 0, cleanup: 0, refuted: 0 }
tests: { dotnet: "12/12", frontend: "4/4" }
---

# Review v1 — 097-mockpass

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-9871 | 🔴 | The invoice total leaves out the shipping line | `Services/Fixture.cs:88` | yes |

## Refuted

| Suspicion | Why it is not real |
|---|---|

## Notes for the fixer

- This fixture carries no runnable code: its verification is judged on the resolution's
  claims, which is why the driver has to hand-stamp the verdict.
