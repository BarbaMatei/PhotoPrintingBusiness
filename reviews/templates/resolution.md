---
type: resolution
target: <target>
version: <n — the pass that raised the findings>
answers: <review-v<n>.md, or "pass v<n> (verification — index row)" when that pass wrote no file>
status: <open | in-progress | resolved>
fixed_commit: <sha of the round's final commit>
closed: <yyyy-mm-dd>
---

# Resolution v<n> — <target>

## Findings

| D# | Status | Commit | Note |
|---|---|---|---|
| D<#> | <fixed | wont-fix | deferred | disputed | false-positive | backlog> | <sha or —> | <one line, max 240 characters> |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — <label> | D<#>, D<#> | `<paths>` | <needed: link, or "not needed (<reason>)"> |

## Decisions

### <One-line decision title (D#)>

<Why the fix took this shape and not the obvious alternative. What was measured.
Max 15 lines per decision. Longer history belongs on the ledger row.>
