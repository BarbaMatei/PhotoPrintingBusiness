---
type: review
target: 044-045-observability
version: 5
supersedes: review-v4.md
pass-type: verification
commit: 52a0cb9
code_tip: d37f867
answers: resolution-v4.md
verdict: approve-with-followups
date: 2026-08-07
---

# Review v5 — 044-045-observability (verification of the v4 fix round)

Anchored, per-fix verification of [resolution-v4.md](resolution-v4.md) at its `fixed_commit`
`52a0cb9`. The v4 pass named 18 findings; the fix round took the four 🟠 (D103, D104, D105, D113)
and fixed all four, deferring the 14 🟡/⚪ to the ledger backlog. This pass asks one question per
fix — *did it hold?* — plus the runbook's three fix-diff questions per cluster. It is **not** a
fresh review of the feature.

**Independence.** Run from a fresh session with no fix-round context, which is what
[resolution-v4.md](resolution-v4.md#process-note-the-re-reviewer-must-weigh) asked for: the v4
verification pass and the v4 fix round shared one session, and three of the four findings in scope
were raised *and* measured by the same agent that then fixed them. Source at the branch tip
`d37f867` is byte-identical to `52a0cb9` (the two commits since touch `reviews/**` only), so
verification ran in place rather than on a detached checkout.

## Verdict: approve-with-followups

**Four of four fixes hold.** Three were proven locally by reverting the mechanism and watching the
predicted test go red with clean attribution; the fourth — the gitleaks gate, which by its nature
cannot be proven on this machine — is proven by CI, which is what the resolution said would settle
it.

| Fix | Ledger | Outcome |
|---|---|---|
| F1 SLO 3's `or vector(0)` guards unpinned | D103 | **verified** — the class rule reddens on guard deletion *and* on the "nothing left to check" case; its disclosed single-term hole is now its own row (F1/D121) |
| F2 both deadline invariants unpinned | D104 | **verified** — each invariant reddens its own test, including the wall-clock one v4 could only measure 0-red |
| F3 SLO 4/5 denominators + the orphaned label | D105 | **verified** — the new metric surface reddens on revert; the guard half it also shipped is unpinned (F1/D121) |
| F4 `secret-scan` red on every PR run | D113 | **verified by CI only** — the PR-event scan flips green at the first commit carrying `.gitleaksignore` and is green on the two runs since |

**4 verified · 0 declined · 0 reopened · 0 backlog rows close · 3 new findings (0🔴/0🟠/3🟡/0⚪).**

**The loop does not re-arm on severity.** No blocker, no serious finding. All three new findings are
🟡 and all three are the *same shape as each other*: this round added a mechanism (a guard, an
exclusion, a flag) and left one of its three records — a test, an acceptance criterion, a type
comment — describing the world before it. None of them makes today's behaviour wrong; each makes the
next author's mistake invisible.

**Two things the owner should know before closing anything.** First, F4 is the one fix no local
measurement can touch: it is verified by a green CI job, and if the branch history is ever rewritten
(rebase, squash-merge) the two pinned fingerprints stop matching and the gate can go red again with
nothing changed in the code. Second, D110 — the wrong dilution figures on the operator-facing
availability panel — is still open, still 🟡, and this round did not touch it; it has now been
flagged in three consecutive summaries.

## How each fix was proven

Seven mutations: five revert-and-rerun proofs and two claim probes, plus one CI reading and one
mechanical deferral sweep. Each failing set was **predicted before the run**, and the five
revert-proofs were run wide enough to show collateral — the scoped observability namespaces,
**1137 tests** (v4's 1133 plus this round's four new ones), green before and after every mutation,
10 MinIO skips throughout. Mutations were applied with byte-level, BOM- and CRLF-aware replacement.

| # | Mutation | Predicted | Measured |
|---|---|---|---|
| M1 | D103: both `or vector(0)` guards deleted from SLO 3, doc **and** dashboard | 1 | 1 — `An_added_sum_term_always_carries_an_absent_series_guard`, 1136 others green |
| M1b | D103: SLO 3's two-term numerator collapsed to one `result=~"ok\|duplicate"`, both copies | 1 (the floor) | 1 — same test, failing on `addedTermsSeen >= 4`, i.e. it refuses to pass by finding nothing |
| M2 | D105: `or vector(0)` deleted from the **SLO 4** numerator, both copies | 0 (claim probe — the fixer disclosed this hole) | **0 — green** → F1/D121 |
| M3 | D104: registered `"Google"` client `Timeout` → 3 s, i.e. behind our own 5 s deadline | 1 | 1 — `The_registered_google_client_keeps_its_backstop_behind_our_own_deadline` |
| M4 | D104: `ct` passed to `GetAsync` instead of the linked deadline token | 1 | 1 — `Our_own_deadline_and_not_the_http_backstop_ends_a_hanging_request`, **red at 32 s**: the backstop, measured |
| M5 | D105: `Orphaned: true` dropped from the orphaned-label return | 1 | 1 — `An_orphaned_label_records_its_own_outcome_rather_than_skipped` |
| M6 | D105: negative-matcher **value** typo (`result!="skippedX"`), both copies | 0 (claim probe) | 0 — green, as `slos.md`'s status block documents |
| M7 | D105: negative-matcher **label name** typo (`resultx!="skipped"`), both copies | ≥1 | 1 — `Every_queried_label_exists_on_the_series_it_filters` |

**Seven of seven predictions matched**, including the two probes whose value is the zero.

**M4 is the measurement this pass existed for.** v4 ran the same mutation (its #7), predicted 0 red
and measured 0 red across 1133 tests — the gap that became D104. The same mutation now reddens
exactly one test, and the 32 s it takes to do it is the proof that the wall clock is what separates
the two paths: with the deadline unwired, only the 30 s backstop can end the hang, and the test bars
at 15 s. The third attempt at that bar is the one that works.

**F4, and the limit of what CI can prove.** `secret-scan` on the `pull_request` event was red at
`f0aadd7` (the v4 tip) and on every earlier PR run of this branch; it is green at `a9c9478` — the
first commit containing `243625c`, which adds `.gitleaksignore` — and green on both runs since. The
`ci` workflow is green on push and pull_request at `d37f867` too, so **all four gates are green on
both events**, which is what the round set out to achieve. What CI cannot prove is durability: the
two entries are commit-pinned fingerprints, so any history rewrite of this branch invalidates them
silently. Both fingerprints were also verified byte-for-byte against the commits they name.

## The three fix-diff questions

Asked by three anchored lenses over the saved fix diff (AWB metric surface; SLO documents and
queries; auth deadline plus the gitleaks gate). Their claims are recorded here **only where this
pass could confirm them by reading HEAD or by measurement** — two claims were dropped for that
reason, recorded below rather than filed.

- **Class or instance** — the `Skipped` class *is* swept: all six construction sites in
  `AwbCreator.cs` read, five genuinely benign, only the orphaned one flagged, and no other path
  creates a billable label it then loses. The `or vector(0)` class is **not**: the two numerators
  this round guarded are unpinned (F1/D121). The `HttpClient.Timeout`-vs-own-deadline class has one
  sibling registration (`SamedayClient`), which owns no deadline of its own — and the *other* half
  of that class, the Sameday timeout sitting inside its own retry ladder, is already filed to
  [inbox.md](../inbox.md) by the fix round rather than swept in.
- **New surface at the bar** — the `orphaned` label value is the round's one new production
  mechanism and it is the best-covered thing in the diff: constant, `All` array, cardinality
  expectation, `metrics.md` row, a failure-mode test that reddens on revert, and a dispatcher whose
  retry decision provably did not move. Its **documentation** is the gap (F3/D123). The two doc-side
  mechanisms fare worse: the guards are unpinned (F1/D121) and the acceptance criterion still
  describes the pre-gate exclusion set (F2/D122).
- **Regression** — none found, and this was checked rather than assumed: no positional pattern
  (`Skipped(var r)`), no record-equality comparison and no serialization of `AwbCreationOutcome`
  exists, so the added flag cannot change any existing match; `AwbDispatcher`'s
  `case Skipped skipped:` still catches both flag states, so an orphaned label is still not retried;
  the new `httpTimeout` parameter on the test helper is optional and all 11 existing call sites are
  unaffected; and the renamed fixture constant is only ever asserted for absence, never for shape.
  The one real cost is wall clock: the round added ~30 s of deliberate waiting to the unit suite.

**Two lens claims dropped, recorded so they are not re-raised.** One lens reported the `slos.md`
queries for SLO 3/4/5 as inconsistent with the dashboard for lacking `rate(…[7d])` — true, but that
is D111, already open, and the rewrite neither caused nor worsened it. The same lens's line
references were unreliable (it cited `slos.md:68` for a query at `:142`), so its claims were
re-checked against HEAD before any were kept. A second lens argued the container test does not
"prove" the optional `TimeSpan?` parameter can be filled; resolving `IGoogleTokenValidator` does
construct the object, so what the test proves is that construction succeeds with the parameter
defaulted — which is the gap v4 had probed with a throwaway. No finding either way.

## Findings

Full detail, evidence and failure scenarios in [findings-v5.md](findings-v5.md); canonical
identities in [ledger.md](ledger.md).

| F# | Sev | D# | Title | Cause |
|---|---|---|---|---|
| F1 | 🟡 | D121 | The `or vector(0)` guards this round added to the SLO 4 and SLO 5 numerators are pinned by nothing — the guard rule skips single-term sides | fix-caused (D105) |
| F2 | 🟡 | D122 | The acceptance criterion still says SLO 4 excludes only `skipped`, and gives `retry_later`'s reason for it | fix-caused (D105) |
| F3 | 🟡 | D123 | The outcome union's doc comment still calls the cancelled-order case a plain skip — the one case that must now set `Orphaned: true` | fix-caused (D105) |

## Deferrals

**All standing terminal decisions re-affirmed — 57 stand, none closes.** The fix round changed 14
source files; every deferred/backlog row citing one of them was re-read against the diff (twelve
rows, table in [findings-v5.md](findings-v5.md#deferrals--all-stand)), and the rest stand
mechanically at `dc203c7` from [review-v4](review-v4.md#deferrals). Notable:

- **D110 stands and is untouched** — the availability panel still tells the operator ~99.7% is the
  floor when it is ~94.5%. Third consecutive pass flagging it.
- **D111 stands** — SLO 4 and SLO 5 were rewritten this round and still carry no time window while
  their dashboard twins use `rate(…[7d])` / `[30d]`.
- **D88 stands, extended** — the new guard test consumes the same two query walkers and inherits
  their reach limits; left in D88's family rather than minted as a new row, as the fixer proposed.
- **D46 stands, still owner-parked** — SLO 1's query and its dilution prose were not touched.

## Tests

- Local, Windows, scoped to the observability namespaces: **1137 passed / 0 failed**, 10 MinIO
  skips, at `d37f867` with a tree clean of source changes — before and after every mutation. This is
  v4's 1133 plus the round's four new tests, which is exactly what the resolution claims.
- **CI, `ubuntu-latest`, at `d37f867`: `ci` and `secret-scan` both GREEN on both the push and the
  pull-request event** — four green gates, the first time this branch has had that. `ci` was red on
  the push run at `a9c9478`, which is the flake `52a0cb9` fixed by widening the deadline bar.
- No new flakes observed. The email-area flake filed in v4 did not reappear in five wide runs.
- Frontend not run — backend-only change, per the repo's scoped-run rule.
- Manifest lenses `db-parity` and `frontend-ux` remain **owed, not waived**.
