---
type: owner-summary
target: 044-045-observability
pass: 5
pass-type: verification
commit: 52a0cb9
date: 2026-08-07
decisions-needed: 0
---

# Owner summary — v5 verification of the 044-045 v4 fix round

**All four fixes held**, and for the first time on this branch **all four CI checks are green on both
event types** — the test workflow and the secret scanner, on push and on pull request
([runs](review-v5.md#f4-and-the-limit-of-what-ci-can-prove)). Each of the three code fixes was proven
by breaking it and watching the one predicted test fail; all seven mutations behaved exactly as
predicted ([how](review-v5.md#how-each-fix-was-proven)). Nothing was reopened.

The one worth knowing: last round's fix for the Google sign-in deadline is now genuinely pinned.
The v4 pass had tried breaking it and **nothing failed** — that hole is what the round was sent to
close. Breaking it now fails one test, and takes 32 seconds to do so, which is the proof that the
right clock is being measured ([D104](ledger.md#v4-findings-d103d120)).

## Needs your decision

**Nothing.** No blocker and no serious finding: the three new items are all minor and all filed
automatically below. The only open judgment is whether you want this loop closed — that is your call
and the router will ask for it, not assume it.

## Reasons to doubt

- **The secret-scanner fix is green by pinning two commit hashes, not by removing the cause.**
  `.gitleaksignore` names the two commits where the fake token sits. If this branch's history is ever
  rewritten — a rebase, or a squash-merge — those hashes stop matching and the scanner can go red
  again with nothing changed in the code ([review](review-v5.md#f4-and-the-limit-of-what-ci-can-prove)).
- **That same fix is the one I could not measure here.** `gitleaks` is not installed on this machine,
  so its verification rests entirely on reading CI ([D113](ledger.md#v4-findings-d103d120)).
- **Two manifest lenses are still owed, not waived** — `db-parity` and `frontend-ux` have never run on
  this target ([metrics](metrics.jsonl)). Nothing on this branch has been proven against PostgreSQL.
- **A verification pass cannot certify.** Its ceiling is "these fixes held", never "this code is
  clean" ([README](../README.md)).
- **New serious findings per pass: 23 → 11 → 11 → 4 → 0** (🔴+🟠, from [metrics.jsonl](metrics.jsonl)).
  Zero is the first, but this pass was the narrowest yet — four fixes, ~175k tokens of review against
  v1's discovery — so it is evidence the *fixes* are sound, not that the feature is.
- **One review agent gave line numbers that were wrong** (it cited `slos.md:68` for a query at
  `:142`), so every claim I kept was re-read at the real commit, and two of its claims were dropped
  rather than filed ([recorded](review-v5.md#the-three-fix-diff-questions)).
- **Local tests are Windows-only and scoped** — 1137 of the suite, the observability namespaces. Linux
  coverage comes from CI, which is the gap that produced the v3 blocker.

## Filed automatically

**3 minors** (3🟡) went to the [ledger backlog](ledger.md#v5-findings-d121d123). All three are the
same shape: the round added something — a guard, an exclusion, a flag — and one of its records still
describes the world before it.

The one worth your eye anyway is **not** new: **[D110](findings-v4.md#f9--🟡--d110--the-dilution-numbers-now-stamped-on-the-operator-facing-panel-are-wrong)**
is untouched for the third pass running. The availability panel on the dashboard tells an operator the
number "cannot read below about 99.7%"; the real floor is about **94.5%**. Someone seeing 95% would
conclude the monitoring is broken when in fact every customer request is failing. It is a ~15-minute
edit to two files.

## State

Four of four fixed and verified, no blocker, no serious finding — so the loop **does not re-arm**.
Records are clean and the auditor passes. What comes next is yours to pick: close the loop here, or
spend a certification pass (the only pass type that can say "no serious defect survives", and the
most expensive thing the loop can do). The router will put that question to you rather than answer
it.
