---
type: review
target: 901-good-target
version: 1
supersedes: null
commit: aaaaaaa
branch: fixture/gate-tests
pass-type: discovery
date: 2026-08-11
lenses: [security, race]
lenses-not-run: [db-parity]
verdict: request-changes
blockers: [PPW-9001]
findings: { high: 1, medium: 0, low: 1, cleanup: 0, refuted: 1 }
tests: { dotnet: "12/12", frontend: "4/4" }
---

# Review v1 — 901-good-target

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-9001 | 🔴 | A parallel init drops the guest token | `Services/Fixture.cs:41` | yes |
| PPW-9002 | 🟡 | The retry count is never logged | `Services/Fixture.cs:88` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| The sweep deletes live rows | The candidate query filters on the deleted stamp |

## Notes for the fixer

- Take PPW-9001 first. PPW-9002 sits in the same file and moves its line numbers.
