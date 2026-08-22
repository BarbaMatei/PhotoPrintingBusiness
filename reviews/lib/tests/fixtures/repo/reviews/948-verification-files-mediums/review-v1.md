---
type: review
target: 948-verification-files-mediums
version: 1
supersedes: null
commit: bbbbbc0
branch: fixture/router-tests
pass-type: discovery
date: 2026-08-22
lenses: [correctness]
lenses-not-run: []
verdict: request-changes
blockers: [PPW-9481]
findings: { high: 1, medium: 0, low: 0, cleanup: 0, refuted: 0 }
tests: { dotnet: "12/12", frontend: "4/4" }
---

# Review v1 — 948-verification-files-mediums

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-9481 | 🔴 | The invoice series can hand out one number twice | `Services/Fixture.cs:33` | yes |

## Refuted

| Suspicion | Why it is not real |
|---|---|

## Notes for the fixer

- One blocker, one cluster.
