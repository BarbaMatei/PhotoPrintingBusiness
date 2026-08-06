---
type: owner-summary
target: 044-045-observability
pass: 4
pass-type: verification
commit: dc203c7
date: 2026-08-06
decisions-needed: 4
---

# Owner summary — v4 verification of the 044-045 fix round

**All 11 fixes held.** Every one proven by breaking it and watching the right test fail
([how](review-v4.md#how-each-fix-was-proven)). Last round's blocker is genuinely gone: the test
workflow is **green on Linux** at the branch tip after six red runs
([`ci` runs](review-v4.md#tests)). Nothing was reopened.

## Needs your decision

1. **The guard your last round added to SLO 3 can be deleted without any test noticing.** Mid-round,
   a check caught that the new payment-webhook query would show "No Data" whenever nothing was wrong
   — the fix was `or vector(0)` on both halves. I deleted both and all 1133 tests still passed
   ([D103](findings-v4.md#f1--🟠--d103--slo-3s-or-vector0-guards-are-pinned-by-nothing)).
   *Suggested: fix now, ~30 min — add one test asserting both queries contain the guard. Same test
   closes the panel-description gap ([D117](ledger.md)).*

2. **The new 5-second deadline on Google sign-in rests on two things, and breaking either is
   invisible.** Putting the old 5-second HTTP timeout back in the registration leaves all 1133 tests
   green — and it quietly brings back the bug this round just fixed (a real Google outage a user
   gives up on stops reaching Sentry). Removing the deadline from the request itself is also green,
   and just makes users wait 15 seconds instead of 5
   ([D104](findings-v4.md#f2--🟠--d104--both-invariants-f2s-discriminator-rests-on-are-unpinned)).
   *Suggested: fix now, ~45 min — one test on the registered timeout, one on the deadline's bound.*

3. **Two more SLOs have the exact defect you decided about for SLO 3 — and the document now implies
   they don't.** SLO 4 (shipping labels) and SLO 5 (e-invoicing) count harmless outcomes as failures,
   so SLO 4 can read 94% against a 98% target in a week where every order got its label. The
   rewritten status note says "Two caveats that matter" and this is not one of them
   ([D105](findings-v4.md#f3--🟠--d105--slo-4-and-slo-5-carry-the-defect-f7-fixed-and-slosmd-now-implies-there-are-only-two-caveats)).
   *Suggested: answer the same one-line question you answered for SLO 3, twice — should `skipped`
   and `pending` count as failures? Then a 10-minute doc+query edit. Or defer and add a third bullet
   now (~5 min) so the list stops being misleading.*

4. **A second CI gate is red and has been for six days.** The secret scanner fails on **every**
   pull-request run of this branch — nine runs, going back before the last fix round — because it
   flags a made-up test token (`"5f0c-live-guest-guid"`) in a bolt-045 test file that
   `.gitleaks.toml` does not allow-list. It passes on push runs because those only scan new commits,
   which is why nobody saw it
   ([D113](findings-v4.md#f4--🟠--d113--secret-scan-fails-on-every-pull-request-run-of-this-branch-and-has-since-before-the-fix-round)).
   *Suggested: ~5 min, but it's a scanner-policy call — allow-list the file, record the fingerprint,
   or rename the constant to something low-entropy.*

## Reasons to doubt

- **Two manifest lenses are still owed, not waived** — `db-parity` and `frontend-ux` have never run
  on this target ([metrics](metrics.jsonl)). Nothing here proves anything against PostgreSQL.
- **A verification pass cannot certify.** Its ceiling is "these fixes held", never "this code is
  clean" ([README](../README.md)).
- **New serious findings per pass: 23 → 11 → 11 → 4** (🔴+🟠, from [metrics.jsonl](metrics.jsonl)).
  The drop is real, but v2 and v3 were flat and this pass was narrower than v1's discovery, so 4 is
  not yet evidence the code is converging.
- **I got one prediction wrong and one lens agreed with me.** I predicted deleting the socket/pipe
  check would change nothing; it reddens a test, because .NET gives a named-pipe address port 80
  rather than 0. Measurement corrected both of us
  ([recorded](findings-v4.md#corrections-to-lens-claims-on-measurement)) — but it shows reasoning
  about this file has been wrong twice now.
- **Neither review agent had the code-navigation tool this session** (`LSP` was unavailable), so
  their symbol-level claims rest on text search. Everything I marked "measured" was run by me.
- **Local tests are Windows-only and scoped** (1133 of the suite, observability namespaces). Linux
  coverage comes from CI, which is exactly the gap that produced last round's blocker.

## Filed automatically

**14 minors** (10🟡 / 4⚪) went to the [ledger backlog](ledger.md#v4-findings-d103d120). Three older
backlog rows **closed** — they were already fixed when they were written down, and one of them
(D97) is what last round's summary told you to look at.

The one worth your eye anyway: **[D110](findings-v4.md#f9--🟡--d110--the-dilution-numbers-now-stamped-on-the-operator-facing-panel-are-wrong)** — the
availability caveat this round put **on the dashboard panel** states the wrong numbers. It says
5,760 self-monitoring requests a day (that is `/metrics` alone; health checks add ~2,880) and that
the number "cannot read below about 99.7%". Using this repo's own traffic figure, the real floor is
about **94.5%**. An operator seeing 95% would conclude the instrumentation is broken when in fact
every customer request is failing.

## State

The loop **re-arms on the four 🟠 above** (three are caused by this round's own fixes), so the router
sends this to a **fix round** next — no blocker, so the branch is not stuck. Records are clean and
the auditor passes.
