---
type: review
target: 902-broken-target
version: 1
supersedes: null
commit: aaaaaaa
branch: fixture/gate-tests
pass-type: verification
date: 2026-08-11
lenses: [security]
verdict: request-changes
blockers: [PPW-9101]
findings: { high: 1, medium: 0, low: 0, cleanup: 0, refuted: 0 }
tests: { dotnet: "12/12", frontend: "4/4" }
---

# Review v1 — 902-broken-target

## Findings (2)

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| F3 | High | The sweep skips a stalled row | `Services/Fixture.cs:12` | yes |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| The cap is unenforced | The cap is asserted in the public call |

## Notes for the fixer

- Deliberately broken fixture. Every violation here is asserted by run-tests.mjs.
