---
type: owner-summary
target: 038-039-invoicing
pass: 17
pass-type: delta-discovery
commit: e935fbb
date: 2026-08-27
decisions-needed: 1
---

# Owner summary — 038-039-invoicing v17

This pass read only the changes made by the previous round of fixing — 35 files, 3,705 lines — and
asked one question: what did those fixes break? It found **24 defects, 4 of them serious**, and 11
of the 24 had two or more independent reviewers agreeing. Every one is damage the fixing did, not a
fault of the original feature.

**You closed the review after this pass**, without certification and without fixing these rows.
That was a deliberate call: seventeen passes and two weeks on invoicing, against a ledger already
carrying 106 verified fixes. This page records what is still standing so whoever returns to this
feature does not have to rediscover it.

## Needs your decision

- **One path charges a customer twice and fulfils both orders (PPW-687 … PPW-690).** A declined
  card makes the app throw away the key that stops duplicate orders, so the server never gets the
  chance to cancel the abandoned payment — and a rule added in the same round now lets a late
  success on that abandoned payment complete the *first* order too. Two paid orders from one
  basket, both invoiced with the tax authority, both given a shipping label. A mistyped card number
  is enough to begin it. **Nothing is deployed, so no customer can hit this today.** It is not an emergency, and it is the one thing on this page that must be fixed
  before this feature meets a real card. Suggested: fix it as a single designed change — write the rule
  *"one basket yields at most one chargeable payment and at most one paid order"* first, derive the
  four fixes from it, and test the decline → retry → late-success sequence end to end.

## Reasons to doubt

- **Only five of eleven reviewer types ran** (`correctness`, `db-parity`, `frontend-ux`,
  `tests-coverage`, `completeness-critic`), because a scoped pass is capped at five. `security` and
  `race` did not look at this diff, and on this target a reviewer's first run has twice produced
  serious findings nobody else saw.
- **Nothing was refuted.** Eight of the 24 faced a skeptic; the four serious rows were accepted on
  three independent reviewers agreeing, without one. A pass that refutes nothing is either honest or
  uncritical — the worst row was verified by hand against the code, which is why it reads honest.
- **The tests left behind by the previous round cannot be trusted uniformly.** Three of them pass
  for reasons unconnected to the bug they name. Two are recorded here (PPW-692, PPW-698); one was
  found by hand during the round itself.
- **This pass reviewed the fixer's work, and the fixer wrote this summary.** The independent judge
  was the reviewing pass, not the author of this page.

## Filed automatically

Thirteen 🟡/⚪ rows went to the backlog: PPW-698 … PPW-710 — test-database helper behaviour, a
migration default the model does not declare, an untested gateway branch, a comment citing a
finding ID, and a doc comment left stale.

## State

The ledger closes with **11 rows open** (4 🔴, 7 🟠), 106 verified, 107 backlogged, 4 `wont-fix`,
4 deferred. The loop is closed by owner ruling at `e935fbb` — **not certified**. The router would
have refused certification anyway until the previous round's seed rate was measured, a rule this
same session adopted. Reopening means starting from the 11 open rows, and PPW-687 … PPW-690 first.
