---
type: owner-summary
target: 044-045-observability
pass: 2
pass-type: verification
commit: e965c99
date: 2026-08-05
decisions-needed: 4
---

# Summary v2 — 044-045-observability

**The fixes held.** All 23 were tested by putting the old broken code back and checking the new
test fails — 26 times, every predicted failure matched. 22 are now `verified`.
[Evidence table](findings-v2.md#part-1--revert-and-rerun-evidence).

Then I asked the three questions the runbook requires of every fix — does the bug survive
elsewhere, is the new machinery up to standard, did anything adjacent break — and that turned up
**34 new findings, 11 of them 🟠. Twenty-one were created by the fixes themselves.**

## Needs your decision

1. **A repeat payment notification for a healthy order now raises a false alarm.** When Stripe or
   EuPlatesc re-sends a "payment succeeded" message for an order that has already been paid *and
   moved on* to Printing, Shipped or Delivered, the new code logs "customer charged, order not
   Paid, manual reconciliation required" and counts it as a failed webhook against the 99.9%
   target. The order is fine. — **Suggested: fix now, ~30 min, small.** The order state machine
   already knows Printing/Shipped/Delivered come after Paid; the check just needs to say "at or
   past Paid" instead of "exactly Paid". [D40](findings-v2.md#-f1--d40--a-redelivered-success-webhook-pages-ops-for-a-healthy-order) ·
   [WebhooksController.cs:287](../../src/PhotoPrint.API/Controllers/WebhooksController.cs#L287) ·
   [OrderStatusMachine.cs:22-26](../../src/PhotoPrint.API/Services/OrderStatusMachine.cs#L22-L26)

2. **A customer closing the tab during Google sign-in will create a Sentry error.** The only place
   the app raises the "external service unavailable" error is when the Google call is cancelled —
   and a user losing signal cancels it exactly the same way a real outage does. The new rule
   ("report every 5xx to Sentry") can't tell them apart, and the middleware's own guard for
   abandoned requests can't catch it because the cancellation was already renamed upstream. On a
   free 5,000-events-a-month plan this is also the most likely way to exhaust the quota.
   — **Suggested: fix now, ~1h.** Separate a genuine timeout from a client abort at the point the
   error is raised. [D43](findings-v2.md#-f4--d43--closing-the-tab-mid-google-sign-in-creates-a-sentry-issue) ·
   [GoogleTokenValidator.cs:40](../../src/PhotoPrint.API/Services/GoogleTokenValidator.cs#L40) · related: [D56](ledger.md)

3. **Anyone can turn your tracing on or off from outside.** A request that arrives carrying a
   standard `traceparent` header has its trace sampling decided by whoever sent it, not by your
   configuration — so a caller can suppress the trace for a request that then fails (defeating the
   "errors are always sampled" guarantee you just paid for), or force full tracing including
   database statements past whatever rate you set. **This one is not the fixer's fault** — it was
   already there when the review started and v1 missed it. — **Suggested: your call on posture,
   then ~1h.** Trusting that header is correct inside a private network and wrong at a public
   edge; you need to pick one. [D41](findings-v2.md#-f2--d41--the-caller-decides-the-sampling-rate) ·
   [ObservabilityExtensions.cs:66](../../src/PhotoPrint.API/Extensions/ObservabilityExtensions.cs#L66)

4. **Four places now promise a guarantee that nothing actually enforces.** Measured, not argued:
   deleting the Sentry breadcrumb scrubbing hook leaves 358 tests green
   ([D48](findings-v2.md#-f9--d48--the-breadcrumb-hooks-wiring-is-unproven)); changing the new
   error-level logging back leaves 24 tests green
   ([D49](findings-v2.md#-f10--d49--the-logerror-half-of-the-d15-fix-has-no-test)); the dashboard
   test that `slos.md` says "fails the build on a rename" never checks any label
   ([D44](findings-v2.md#-f5--d44--the-metric-name-test-checks-no-labels-but-the-docs-promise-it-does));
   and the "a 404 is not sent to Sentry" test passes even if the endpoint doesn't exist
   ([D54](ledger.md)). This is the same class as v1's F8/F9/F20/F21. — **Suggested: fix as one
   batch, ~2h**, with the rule that each new assertion carries its own revert-proof.

The other four 🟠 — [D42](findings-v2.md#-f3--d42--lowering-the-sample-rate-saves-much-less-than-documented)
(lowering the trace rate saves far less than the runbook claims),
[D45](findings-v2.md#-f6--d45--nothing-checks-the-scrape-port-against-a-bound-listener) (nothing
checks the metrics port is actually open, so a non-Compose deployment either goes dark or silently
re-opens the original hole), [D46](findings-v2.md#-f7--d46--slo-1-counts-its-own-scrapes) (the
availability number counts its own 5,760 daily monitoring requests) and
[D50](findings-v2.md#-f11--d50--slos-14-are-measured-hides-slo-3s-hole) ("SLOs 1–4 are measured"
hides that a database outage makes the payment SLO read 100% healthy) — are all corrections to
claims rather than to behaviour. Cheapest honest fix for three of them is to make the document say
what the system does.

## Reasons to doubt

- **`db-parity` and `frontend-ux` are still owed**, unchanged since v1 — and
  [D59](ledger.md) is the first finding to land squarely in the parity gap: the new AWB
  cancellation tests run on SQLite while production is PostgreSQL, and the two report a cancelled
  command differently.
- **One finding was downgraded on re-check and one re-attributed.** The tests lens rated the metric
  capture helper's broken isolation filter High and predicted random failures across six files. The
  mechanism is certain, but five consecutive runs of the colliding tests went 133/133 green, so it
  is recorded at 🟠 on the mechanism, not the prediction ([D47](findings-v2.md#-f8--d47--the-metric-capture-helpers-isolation-filter-does-nothing)).
  D41 was presented as a fix defect; `git show 5cac465` proves it pre-dates the round.
- **A verification pass cannot certify** — the README caps it at `approve-with-followups`. "The
  fixes held" is not "the code is clean", and this pass is the evidence: 34 findings surfaced from
  the fix diffs alone.
- **The full test suite was not run.** The scoped filter covers every namespace the fix round
  touched (1081 passed / 0 failed / 10 skipped); the frontend suite did not run because nothing
  frontend changed. A fix-caused break in an untouched area would not have been seen.
- **New findings are rising, not decaying** — v1 named 39, v2 names 34. That is expected for a
  verification pass that asks discovery-shaped questions of a large fix round, but it means the
  loop is not converging yet ([metrics.jsonl](metrics.jsonl)).

## Filed automatically

23 🟡/⚪ went to the [ledger](ledger.md) backlog as D51–D73; they do not re-arm the loop. One
deserves your eye anyway: **[D59](ledger.md)** — if PostgreSQL reports a cancelled command
differently from SQLite, every deployment with AWB jobs in flight will record false failures
against the AWB success target.

## State

Loop **re-armed** — fix-caused 🟠 regressions and a declined verification both trigger it. Router
says **fix round** next, on D40–D50. [D17](ledger.md) stays `fixed`, not `verified`: its concurrent
double-click leg has no guard and no test, and the resolution asked this pass to decide.
