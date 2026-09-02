---
type: review
target: 095-postcert
version: 2
supersedes: review-v1.md
commit: c09675d
branch: fixture/loop-states
pass-type: certification
date: 2026-08-31
lenses: [correctness, security, requirements, quality, tests-coverage, completeness-critic, db-parity, input-validation, observability, race, frontend-ux]
lenses-not-run: []
verdict: approved
blockers: []
findings: { high: 0, medium: 1, low: 0, cleanup: 0, refuted: 1 }
tests: { dotnet: "12/12", frontend: "4/4" }
---

# Review v2 — 095-postcert (certification, single pass)

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-9852 | 🟠 | The retry counter is read before the row is refreshed | `Services/Fixture.cs:73` | yes |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| The certification missed the sweep path | The sweep is covered by two of the eleven lenses |

## Notes for the fixer

- Certified with one 🟠 open: it is a stale read, not a lost write.
