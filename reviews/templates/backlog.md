---
type: review-backlog
updated: <yyyy-mm-dd>
---

# Backlog — unfixed minors from closed targets

A row enters when its target closes, or when the owner routes a defect noticed
outside any pass here at a round's gate — that row takes the next number from
`reviews/id-counter`. A
row leaves only two ways, and only after the terminal state is written back to
its home ledger row: fixed (with the normal verification a backlogged minor
requires) or owner-ruled wont-fix. An owner-routed row has no ledger row until a
loop opens for its area; until then it leaves on the owner's ruling alone,
recorded in that round's resolution and in this file's git history. Empty file
means nothing is owed. The pre-deployment regression phase requires it empty.

| ID | Target | Sev | What | Area |
|---|---|---|---|---|
| PPW-<n> | <target> | 🟡 | <one plain line> | <code area, e.g. uploads, orders, payments> |
