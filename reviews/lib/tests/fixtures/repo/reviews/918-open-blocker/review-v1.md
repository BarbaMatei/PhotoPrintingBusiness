---
type: review
target: 918-open-blocker
version: 1
supersedes: null
commit: 4444440
branch: fixture/router-tests
pass-type: discovery
date: 2026-08-22
lenses: [correctness]
lenses-not-run: []
verdict: request-changes
blockers: [PPW-9181, PPW-9182]
findings: { high: 2, medium: 0, low: 0, cleanup: 0, refuted: 0 }
tests: { dotnet: "12/12", frontend: "4/4" }
---

# Review v1 — 918-open-blocker

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-9181 | 🔴 | A guest merge overwrites the signed-in cart | `Services/Fixture.cs:26` | yes |
| PPW-9182 | 🔴 | The upload delete ignores the storage router | `Services/Fixture.cs:53` | yes |

## Refuted

| Suspicion | Why it is not real |
|---|---|

## Notes for the fixer

- Two blockers, two clusters; the second one is the smaller.
