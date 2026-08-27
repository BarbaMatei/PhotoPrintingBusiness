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

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-<n> | <fixed | wont-fix | deferred | disputed | false-positive | backlog> | <sha or —> | <one line, max 240 characters> |

## Scope

| Cluster | Findings | Files | Protocol |
|---|---|---|---|
| A — <label> | PPW-<n>, PPW-<n> | `<paths>` | <label of the cluster's "### Protocol — <label>" block, or —> |

## Decisions

### Protocol — <label>

<The cluster's spec, written at triage BEFORE any of its fixes: the states, the
invariant(s) — each with a quantifier ("never", "at most one", "exactly once") —
and the ordered rules for who mints/retires/cancels what. The approach-check
critiques this block, and the cluster's tests include one invariant test over
the composed flows. Required when two or more serious findings share a stateful
surface; the checks are gated on worklog events (doc-contracts.md).>

### <One-line decision title (PPW-<n>)>

<Why the fix took this shape and not the obvious alternative. What was measured.
Max 15 lines per decision. Longer history belongs on the ledger row.>
