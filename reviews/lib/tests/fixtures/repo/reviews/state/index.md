---
type: review-index
updated: 2026-08-11
---

# Review Index

Fixture copy of the two tables the state lint reads: one row per target, one row
per pass.

## Targets at a glance

| Target | State |
|---|---|
| 901 good target | Open: one discovery pass at `aaaaaaa`, one fix round resolved. <br> Residue: 🟡 PPW-9002 stands as a queue row. <br> Re-arms on a new 🔴, a fix-caused 🟠 regression, or a reopened fix. |
| 903 closed target | Closed 2026-08-11 on owner sign-off, archived the same day. <br> Residue: ⚪ PPW-9004 in the queue. |

## Passes

| Date | Target | Pass | Verdict | New H/M/L/C | Outcome | Files |
|---|---|---|---|---|---|---|
| 2026-08-11 | 908 | v1 verification | approve-with-followups | 0/0/0/0 | Every fix held and nothing reopened | — |
| 2026-08-11 | 901 | v1 discovery (2 lenses) | request-changes | 1/0/1/0 | Worst is PPW-9001, a parallel init that drops the guest token; one low row went to the queue | — |
