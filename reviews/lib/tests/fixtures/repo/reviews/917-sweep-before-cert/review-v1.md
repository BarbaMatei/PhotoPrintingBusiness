---
type: review
target: 917-sweep-before-cert
version: 1
supersedes: null
commit: 3333330
branch: fixture/router-tests
pass-type: discovery
date: 2026-08-22
lenses: [correctness]
lenses-not-run: []
verdict: request-changes
blockers: [PPW-9171]
findings: { high: 1, medium: 2, low: 1, cleanup: 0, refuted: 0 }
tests: { dotnet: "12/12", frontend: "4/4" }
---

# Review v1 — 917-sweep-before-cert

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-9171 | 🔴 | The refund path double-credits a split payment | `Services/Fixture.cs:19` | yes |
| PPW-9172 | 🟠 | The export writes the wrong VAT rate for services | `Services/Fixture.cs:61` | queue |
| PPW-9173 | 🟠 | The nightly job runs before the rate table loads | `Jobs/Fixture.cs:8` | queue |
| PPW-9174 | 🟡 | The error page loses the correlation id | `Services/Fixture.cs:90` | backlog |

## Refuted

| Suspicion | Why it is not real |
|---|---|

## Notes for the fixer

- One blocker now; the rest wait on the queue.
