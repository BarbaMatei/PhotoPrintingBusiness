---
type: owner-summary
target: system
pass: 2
pass-type: discovery
commit: 94eff97
date: 2026-08-12
decisions-needed: 4
---

# Owner summary — review system v2

Second review of the review system itself, run blind from v1's content. It found 21
findings — none high, 7 medium — and re-graded the system at **~5.1/10 overall**
([scorecard](scorecard.md)). The headline: the machinery's texts and scripts drifted apart
during the August redesign — six checks are dead or mis-wired, including one v1 fix that
silently regressed ([SF16](review-v2.md#findings)). Verdict: approve-with-followups.

## Needs your decision

- **Approve the repair fix round** — recommendations 1–2 in the
  [review](review-v2.md#recommendations-ranked): fix six dead/mis-wired checks and make the
  contradicting documents agree. Roughly two sessions, near-zero run tokens. Suggested: yes,
  before any further building on top.
- **The blinding hole** ([SF17](review-v2.md#findings)): the runbook claims lenses are
  barred from git history; the actual prompt doesn't bar it, and fix commits carry finding
  ids. Suggested: take the one-line prompt fix in the same round.
- **Commit the test-quality audit** ([SF25](review-v2.md#findings)): 309 findings including
  4 confirmed high sit in an uncommitted file — one cleanup command from gone. Suggested:
  commit now, decide its process later.
- **Seeded run 2** ([SF14](review-v2.md#findings), deferred by you 2026-07-29): the new
  numbers make the case stronger — no target ever stopped producing new serious findings
  before close, so "certified" still rests on an untested rule. ~2–2.5M tokens. Suggested:
  schedule it after the repair round; saying "not yet" again is fine, the gate stays visible.

## Reasons to doubt

- Same model family reviewed, checked, and reconciled everything — a shared blind spot
  would survive all three layers ([notes, assumption 1](../../notes/self-driving-loop-design.md)).
- The reviewer was blinded from v1's files, but two git commit subjects seen during
  file navigation leaked v1 grade fragments for two dimensions (Speed, autonomy) before
  grading; the grades were set from evidence, but the leak existed.
- The metrics readout's lens table rests on a single pass's per-finding records
  ([SF24](review-v2.md#findings)) — no cross-target conclusion is sound yet.
- Grading dimensions came from v1 via a grade-stripping agent; wording arrived intact,
  but one dimension (Speed) carried its definition and the rest did not — the grader
  re-derived those meanings, so v1↔v2 comparability is approximate on some rows.

## Filed automatically

Nothing — the system target has no ledger to file into ([SF25](review-v2.md#findings)
asks for one). All 21 findings live in the [review file](review-v2.md).

## State

v1's resolution closed 13 of 14 standing findings; this pass reopens SF16, re-finds SF14
(quantified), and mints SF17–SF35. Open now: 7 🟠, 10 🟡, 4 ⚪. Next per the loop's own
router logic: a fix round on the mechanical repairs, then re-review. The scorecard's
v1-to-v2 comparison is a separate step you can request — the grader stayed blind to v1's
numbers and has not computed it.
