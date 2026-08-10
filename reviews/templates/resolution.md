---
type: resolution
target: <target>
version: <n>
answers: review-v<n>.md
status: <open | in-progress | resolved>
fixed_commit: <sha of the round's final commit>
closed: <yyyy-mm-dd>
findings:
  D<#>: { status: <fixed | wont-fix | deferred | disputed | false-positive | backlog>, commit: <sha>, note: "<one line, max 240 characters>" }
---

# Resolution v<n> — <target>

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — <label> | D<#>, D<#> | `<paths>` | <needed: link, or "not needed (<reason>)"> |

## Decisions

### <One-line decision title (D#)>

<Why the fix took this shape and not the obvious alternative. What was measured.
Max 15 lines per decision. Longer history belongs on the ledger row.>
