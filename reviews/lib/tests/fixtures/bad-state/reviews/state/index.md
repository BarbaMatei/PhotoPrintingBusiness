---
type: review-index
updated: 2026-08-11
---

# Review Index

Deliberately broken fixture: one oversized glance cell, one malformed counts
cell, one unknown target key, one description over the word cap, and one pass
row with six cells.

## Targets at a glance

| Target | State |
|---|---|
| 901 good target | Line one. <br> Line two. <br> Line three. <br> Line four. <br> Line five. <br> Line six, one past the cap. |

## Passes

| Date | Target | Pass | Verdict | New H/M/L/C | Outcome | Files |
|---|---|---|---|---|---|---|
| 2026-08-11 | 901 | v2 verification | approve-with-followups | 0/0/0 | Three counts where four belong | — |
| 2026-08-11 | 901 | v3 verification | approve-with-followups | 0/0/0/0 | Six cells, one short of seven |
| 2026-08-11 | 999 | v1 discovery | request-changes | 1/0/1/0 | A target key no folder matches | — |
| 2026-08-11 | 901 | v1 discovery | request-changes | 1/0/1/0 | This description runs past the fifty word cap on purpose so the fixture proves the word counter fires: it retells the whole pass, names every lens that ran, lists the findings one by one, repeats the verdict, thanks the reader, and then keeps going for a few more words to be sure the limit is crossed | — |
