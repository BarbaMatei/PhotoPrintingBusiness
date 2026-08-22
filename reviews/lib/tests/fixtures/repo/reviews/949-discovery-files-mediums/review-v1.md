---
type: review
target: 949-discovery-files-mediums
version: 1
supersedes: null
commit: cccccd0
branch: fixture/router-tests
pass-type: discovery
date: 2026-08-22
lenses: [correctness]
lenses-not-run: []
verdict: approve-with-followups
blockers: []
findings: { high: 0, medium: 2, low: 0, cleanup: 0, refuted: 0 }
tests: { dotnet: "12/12", frontend: "4/4" }
---

# Review v1 — 949-discovery-files-mediums

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-9491 | 🟠 | The cart merge keeps the older price | `Services/Fixture.cs:50` | queue |
| PPW-9492 | 🟠 | The gallery page size is unbounded | `Services/Fixture.cs:64` | queue |

## Refuted

| Suspicion | Why it is not real |
|---|---|

## Notes for the fixer

- Two mediums, both under the queue threshold.
