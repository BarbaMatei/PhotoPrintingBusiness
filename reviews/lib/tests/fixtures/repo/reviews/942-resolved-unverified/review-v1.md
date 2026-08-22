---
type: review
target: 942-resolved-unverified
version: 1
supersedes: null
commit: 7777770
branch: fixture/router-tests
pass-type: discovery
date: 2026-08-22
lenses: [correctness]
lenses-not-run: []
verdict: request-changes
blockers: [PPW-9421]
findings: { high: 1, medium: 0, low: 0, cleanup: 0, refuted: 0 }
tests: { dotnet: "12/12", frontend: "4/4" }
---

# Review v1 — 942-resolved-unverified

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-9421 | 🔴 | The payment webhook trusts an unsigned body | `Services/Fixture.cs:15` | yes |

## Refuted

| Suspicion | Why it is not real |
|---|---|

## Notes for the fixer

- One blocker, one cluster.
