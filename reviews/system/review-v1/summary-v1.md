---
type: owner-summary
target: review-system
pass: 1
pass-type: system-review
commit: 1a9c3ad
date: 2026-07-29
decisions-needed: 3
---

# Summary v1 — the review system itself

I reviewed the review system end to end, computed the first cross-target numbers, and had
two independent agents attack my conclusions before you saw them. **14 findings stand
(5 orange), 2 were killed by the defense, 3 were added by the checkers.** Full detail:
[review-v1.md](review-v1.md).

## Needs your decision

1. **🟠 Push the review evidence off this machine — approve the push?**
   The three branches holding every commit your ledgers cite
   (`feat/bolt-036…`, `feat/bolt-042…`, `feat/bolt-043…`) exist **only locally**, and no
   tags exist ([SF4](review-v1.md#sf4---the-evidence-chain-lives-on-one-machine)). One
   deleted branch or one disk failure and no revert-proof for a certified feature can be
   reproduced. *Suggested action:* let me push the three branches (or tags) to origin —
   one command, reversible.
2. **🟠 One calibration sitting.** Four rulings only you can make, each a doc edit:
   what the standard certification close *is* (the written path has completed
   [zero times](review-v1.md#sf1---the-undeviated-certification-path-has-never-completed));
   fixing the README sentence that oversells
   ["certified"](review-v1.md#sf2---certified--what-the-front-page-promises);
   whether seeded run 2 must precede the next certification
   ([SF14](review-v1.md#sf14---certified-under-an-untested-stop-rule-checker-added));
   the fixer==verifier exemption at its recorded expiry
   ([SF15](review-v1.md#sf15---the-independence-rule-bent-exactly-at-closure-pressure-checker-added)).
   *Suggested action:* ~1 h together, or I draft the four rulings for your yes/no.
3. **🟠 Fund seeded-bug run 2 (~2M+ tokens) — now or later?** Both closures so far ran
   under a stop rule whose own design doc says it should not be trusted before this
   experiment runs. It needs a non-Claude bug-planter (your earlier open question).

## Reasons to doubt

- I graded and reviewed my own review process's records — same-author risk. Mitigation:
  the two checkers were independent and did change the result (2 kills, 2 corrections,
  3 additions), but all three of us are the same model ([the system's own weakest
  assumption](../../notes/self-driving-loop-design.md)).
- The roll-up numbers count **pass tokens only** — fix rounds and synthesis were never
  metered ([SF7](review-v1.md#-tail)), so true per-feature cost is higher than the table.
- Two of my findings did not survive the defense; others may be softer than they read.
  Killed findings are kept in the file, per your never-suppress rule.

## Filed automatically

4 ⚪ hygiene items (stale stats, number drift, cost labeling, model check) ride in
[review-v1.md's ⚪ tail](review-v1.md#-tail) with fixes named; recommendations 3–6 there
are afternoon-sized builds (records auditor, schema v2, post-cert escape marker,
loop-driver) that need no ruling, just sequencing.

## State

First system pass done; baseline scorecard locked in [review-v1.md](review-v1.md) for
before/after comparison. Nothing committed or pushed yet. Next per your call: the push
(decision 1), the calibration sitting (decision 2), then the auditor/schema builds.
