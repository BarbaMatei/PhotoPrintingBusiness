---
type: review
target: 919-reopened-latest
version: 1
supersedes: null
commit: 5555550
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

# Review v1 — 919-reopened-latest

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-9191 | 🟠 | The AWB call runs twice for one order | `Services/Fixture.cs:37` | yes |
| PPW-9192 | 🟠 | The thumbnail job skips the last page | `Jobs/Fixture.cs:45` | yes |

## Refuted

| Suspicion | Why it is not real |
|---|---|

## Notes for the fixer

- Two mediums, one round.
