---
type: review
target: <target>
version: <n>
supersedes: <n-1 or null>
commit: <short sha reviewed>
branch: <branch>
pass-type: <discovery | delta-discovery | certification>
date: <yyyy-mm-dd>
lenses: [<lenses run>]
lenses-not-run: [<lenses owed>]
verdict: <request-changes | approve-with-followups>
blockers: [<PPW ids, or empty>]
findings: { high: <n>, medium: <n>, low: <n>, cleanup: <n>, refuted: <n> }
tests: { dotnet: "<passed/total>", frontend: "<passed/total>" }
---

# Review v<n> — <target>

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-<n> | 🔴 | <one-line title, same wording as the ledger row> | `<path:line>` | yes |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| <one line> | <one line> |

## Notes for the fixer

<Order of work, coupling warnings, traps. Bullets. No re-describing defects — reference ids.>
