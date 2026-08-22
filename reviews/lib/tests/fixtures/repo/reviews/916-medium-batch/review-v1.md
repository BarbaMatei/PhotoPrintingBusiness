---
type: review
target: 916-medium-batch
version: 1
supersedes: null
commit: 2222220
branch: fixture/router-tests
pass-type: discovery
date: 2026-08-22
lenses: [correctness]
lenses-not-run: []
verdict: request-changes
blockers: [PPW-9161]
findings: { high: 1, medium: 3, low: 0, cleanup: 0, refuted: 0 }
tests: { dotnet: "12/12", frontend: "4/4" }
---

# Review v1 — 916-medium-batch

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-9161 | 🔴 | A duplicate webhook charges the card twice | `Services/Fixture.cs:31` | yes |
| PPW-9162 | 🟠 | The retry ladder never logs its total | `Jobs/Fixture.cs:14` | queue |
| PPW-9163 | 🟠 | The status filter ignores a trailing space | `Services/Fixture.cs:58` | queue |
| PPW-9164 | 🟠 | A locker order shows the courier address | `Services/Fixture.cs:72` | queue |

## Refuted

| Suspicion | Why it is not real |
|---|---|

## Notes for the fixer

- The blocker goes first; the three mediums are a batch of their own.
