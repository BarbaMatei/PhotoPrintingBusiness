---
type: review
target: 096-rotten
version: 1
supersedes: null
commit: c09675d
branch: fixture/loop-states
pass-type: discovery
date: 2026-08-28
lenses: [correctness, security, requirements, quality, tests-coverage, completeness-critic, db-parity, input-validation, observability, race, frontend-ux]
lenses-not-run: []
verdict: approve
blockers: []
findings: { high: 0, medium: 0, low: 0, cleanup: 0, refuted: 1 }
tests: { dotnet: "12/12", frontend: "4/4" }
---

# Review v1 — 096-rotten

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|

Nothing survived the trace step, so this target has no ledger.

## Refuted

| Suspicion | Why it is not real |
|---|---|
| The formatter loses the currency symbol | The symbol comes from the culture, which is set |
