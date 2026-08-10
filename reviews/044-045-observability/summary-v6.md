---
type: owner-summary
target: 044-045-observability
pass: 6
pass-type: verification
commit: a4eb7e5
date: 2026-08-10
decisions-needed: 0
---

# Owner summary — v6 verification of the 044-045 v5 fix round

**All four fixes held** ([verdict](review-v6.md#verdict-approve-with-followups)). One was proven by
breaking it: deleting the "show a red 0% instead of blank" guard from the AWB success ratio — in
**both** the SLO document and the Grafana panel — now fails exactly one test, and that is the same
break the previous pass measured as *harmless* ([M1](review-v6.md#how-each-fix-was-proven)). The
round's own hardest case was re-run independently too: delete the dashboard panel, duplicate the
document's copy of the query, and the test still fails naming the dashboard — the shape the fixer's
first attempt let through ([M2](review-v6.md#how-each-fix-was-proven)).

The other three fixes are wording — an acceptance criterion, a code doc comment, an operator log
table and the availability panel's caption. Nothing in the build checks wording, so each sentence was
read against the code it describes: the five harmless reasons an AWB is skipped and the one that
isn't ([AwbCreator.cs:91-272](review-v6.md#the-judgment-items-read-against-the-code)), and every
number in the corrected availability caption recomputed from its source — a 15-second metrics scrape,
a 30-second container health check, ~500 customer requests a day → the panel cannot read below
**~94.5%** ([D110](ledger.md#v4-findings-d103d120)). The premise was checked too, not assumed: the
app applies **no filter** to its request metrics, so those self-checks really are counted.

## Needs your decision

**Nothing.** No blocker, no serious finding. The one new item is ⚪ and filed below. The only open
judgment is whether to close this loop — your call, and the router will ask rather than assume.

## Reasons to doubt

- **This pass used no subagents**, so one reader did the finding, the measuring and the judging.
  The runbook wants an anchored agent for changed wording fixes and separate lenses for the
  fix-diff questions; the session's standing no-subagent instruction ruled that out. Recorded in the
  review ([method deviation](review-v6.md#verdict-approve-with-followups)). Two independent
  measurements back the one code-shaped fix; the three wording fixes rest on one reader's eyes.
- **A verification pass cannot certify** — it asks "did these four fixes hold?", not "is the
  feature clean?". Only a full pass can say the latter, and the owner called the v4 round
  patch-grade, so no delta discovery ran.
- **Two manifest lenses are still owed, not waived** — `db-parity` and `frontend-ux` have never run
  on this target ([v6 tests section](review-v6.md#tests)).
- **New findings per pass keep falling** — 34 (v2) → 29 (v3) → 18 (v4) → 3 (v5) → 1 (v6)
  ([metrics.jsonl](metrics.jsonl)). That is the expected shape of a loop running out of defects, and
  it is also what a tiring reviewer looks like; this pass reviewed a six-file diff, so the small
  number is at least proportionate.
- **Test scope was narrow on purpose** — 282 tests (integration + observability), not v5's 1137,
  because the diff is documents, one panel caption, one doc comment and one test file. CI was not
  re-read: no workflow or source file changed since `d37f867`, where all four gates were green.

## Filed automatically

**1 ⚪ to the ledger backlog** — [D124](ledger.md#v6-findings-d124): the list of four queries the new
test protects is maintained by hand, so a fifth one added later ships unprotected and nothing
notices. Worth your eye only for the reason recorded: the fixer said a general rule would break two
other panels, and that turns out **not** to be true of a narrower rule — those two panels use
pattern matchers, not hand-named values ([why it is still ⚪](findings-v6.md#f1---the-guarded-selector-list-is-hand-maintained-d124)).

## State

Four rows flip to `verified` (D110, D121, D122, D123); **0 open serious findings, 0 reopened**. The
loop is quiet: nothing re-arms it, and the next step is yours — say the word and it closes, or leave
it under watch.
