---
type: review
target: 044-045-observability
version: 3
supersedes: review-v2.md
pass-type: verification
commit: 7e28317
code_tip: c92ad77
answers: resolution-v2.md
verdict: request-changes
date: 2026-08-05
---

# Review v3 — 044-045-observability (verification of the v2 fix round)

Anchored, per-fix verification of [resolution-v2.md](resolution-v2.md) at `7e28317`. The v2 pass
named 34 findings; the fix round took the 11 🟠 (D40–D50), fixed 10, and the owner **parked
F7/D46**. This pass asks only one question per fix — *did it hold?* — plus the runbook's three
fix-diff questions per cluster. It is **not** a fresh review of the feature.

**Independence.** Run from a fresh session with no fix-round context, which is what
[resolution-v2.md](resolution-v2.md#process-note-the-re-reviewer-must-weigh) asked for: the v2
pass and the v2 fix round shared one session, so this is the first independent look at both.
Source at `c92ad77` is byte-identical to `7e28317` (only `reviews/**` and `reviews/lib/**`
changed since), so verification ran in place rather than on a detached checkout.

## Verdict: request-changes

**Nine of the ten fixes hold, every one proven by reverting it and watching the predicted tests
go red.** But the round shipped one blocker and ten new 🟠, and the blocker is not subtle:

> **CI has been red on this branch since the fix round, and the failing test is one the round
> added.** `ScrapeListenerCheckTests.An_address_with_no_port_is_not_counted_as_a_listener` passes
> on Windows and fails on `ubuntu-latest`. CI was green at `8daa977` (the v2 review commit) and
> has failed on every run since `e791c40`, the first push after the fix code landed — six
> consecutive red runs, including the branch tip `c92ad77`.

The fix round's own records report `final: {passed: 1120, failed: 0}`. That measurement is real
and I reproduced it — on Windows. It was never true on the platform the app deploys to.

| Fix | Ledger | Outcome |
|---|---|---|
| F1 webhook duplicate classification | D40 | **verified** |
| F2 sampler posture | D41 | **verified** (at the seam; production call site unpinned — F6 below) |
| F3 sampling cost claim (doc) | D42 | **verified** |
| F4 client abort ≠ 502 | D43 | **verified** for its own defect; the fix's timeout carve-out is dead code (F2 below) |
| F5 dashboard label net | D44 | **verified** |
| F6 scrape-listener guard | D45 | **DECLINED — not verified.** Its own test fails in CI and rule 2 is defeated off-Windows |
| F7 SLO 1 self-monitoring traffic | D46 | **deferred, upheld** — owner parked it; defect confirmed untouched |
| F8 metric-capture isolation | D47 | **verified** (the repair's nested-capture throw has no test — F5 below) |
| F9 breadcrumb egress hook | D48 | **verified** (test is absence-only — F12 below) |
| F10 mapped-5xx log level | D49 | **verified** (the unmapped-500 branch is unpinned — F3 below) |
| F11 SLO claims (doc) | D50 | **verified** for the caveat it added; two claims in the same file are still false (F7, F8) |

**9 verified · 1 declined · 1 deferral upheld · 0 reopened · 29 new findings (1🔴/10🟠/14🟡/4⚪).**

## How each fix was proven

Eleven mutations, each with its failing set **predicted before the run** and a run wide enough to
show collateral (the scoped observability namespaces, 1130 tests). Every prediction matched
exactly; no mutation produced a failure outside its own finding.

| # | Mutation | Predicted | Measured |
|---|---|---|---|
| 1 | D40: `HasBeenPaid(...)` → `== OrderStatus.Paid`, both handlers | 6 | 6 — the 3 Stripe + 3 eu-platesc theory cases |
| 2 | D40: drop `Printing` from `PaidStatuses` | 4 | 4 — incl. `HasBeenPaid_CoversEveryStatusReachableFromPaid_ExceptCancelled` |
| 3 | D41: `ParentBasedSampler` restored **inside** `BuildSampler` | 3 | 3 — the two traceparent tests + the error-promotion test |
| 4 | D41: `ParentBasedSampler` restored at the **call site** | 0 (claimed gap) | **0 — 1120 green.** The disclosed residual is real |
| 5 | D43: both cancellation rethrows removed | 1 | 1 — `ValidateAsync_CallerCancelled_…` |
| 6 | D43: discriminator reduced to `ct.IsCancellationRequested` | 1 | 1 — `ValidateAsync_TimeoutThenCallerCancelled_…` |
| 7 | D43: body-read guard removed only | 0 (claimed no test) | **0 green.** Disclosed gap confirmed |
| 8 | D45: `ScrapeListenerCheck.Verdict` always returns null | ~8 | 8 |
| 9 | D45: guard registration removed | 1 | 1 — the hook-pinning test |
| 10 | D47: `AsyncLocal` context gate removed | 1 | 1 — `A_capture_does_not_see_what_another_test_emits` |
| 11 | D44: `WebhookResultValues.Ok` renamed `ok`→`okay` | 1 | 1 — `Every_queried_label_exists_on_the_series_it_filters` |
| 12 | D48: `SetBeforeBreadcrumb` deleted | 1 | 1 |
| 13 | D49: mapped-5xx `LogError`→`LogWarning` | 1 | 1 |

Three further mutations were run to test **claims**, not fixes, and all three found a gap: the
nested-capture throw (0 red), the unmapped-500 log level (0 red), and dropping every breadcrumb
(F9's own test stayed green). They are findings F5, F3 and F12.

**One measurement in this pass was thrown away and redone.** The first attempt at mutations 13
and the unmapped-500 probe used a PowerShell text replacement, which re-encoded the file and
mangled the Romanian message `A apărut o eroare neașteptată`; the failing test was reacting to
corrupted text, not to the log level. Both were redone with encoding-safe edits. The tell was
collateral — a failure outside the mutation's own finding — which is exactly what the runbook's
"clean attribution, zero collateral" bar is for.

## The three fix-diff questions

Asked by three anchored lenses over the saved fix diff (behaviour; OTel-SDK/hosting;
tests-contracts-docs), plus one anchored checker over the deferrals whose cited files the fix
round touched. Their claims are recorded as findings **only where this pass could confirm them**;
two were corrected on measurement and are recorded as corrected.

- **Class or instance** — the fixer's `== OrderStatus.Paid` sweep declared ten sibling sites safe.
  Nine are; `AwbRetryJob` is the exception (F9). The `LogError` fix was an instance fix: the
  unmapped-500 branch, which handles most 500s, is still unpinned (F3). F2's class is open one
  layer over, in Sentry (F4).
- **New surface at the bar** — `ScrapeListenerCheck.Verdict` is the only new mechanism with a
  platform bug (F1) and it counts ports rather than reachability (F20). `BuildSampler` has no
  call-site pin (F6). The nested-capture throw has no test (F5). Nothing exercises
  `StartedAsync` at all — not the addresses read, not the `Critical` line §14.10 tells operators
  to grep for, not the throw (F24).
- **Regression** — no fix broke adjacent behaviour that this pass could find. Two fixes changed
  what a *reader* is told without changing what the code does, and the docs are now wrong in new
  places (F7, F8, F10, F14).

## Corrections to lens claims, on measurement

Recorded because a verification pass that passes agent claims through unmeasured is doing the
thing this loop exists to prevent.

1. **The behaviour lens's central F4 claim was right, and stronger than it could show.** It
   argued from documented .NET behaviour that `GetBaseException() is not TimeoutException` can
   never discriminate. Measured on net8.0 (`8.0.29`) with a real `HttpClient` and a hanging
   handler: the chain is `TaskCanceledException → TimeoutException → TaskCanceledException`, so
   `GetBaseException()` returns `TaskCanceledException` in **every** case, including a pure
   timeout. The shipped filter and the naive filter it replaced behave identically in all four
   timing scenarios. See F2.
2. **The tests lens rated F9's absence-only assertion 🟠 on the theory that a scrubber throw
   would silently drop every breadcrumb in production with the suite green.** Measured: making
   `Scrub(Breadcrumb)` return null for everything leaves F9's integration test green — the
   vacuity is real — but reddens two `SentryDataScrubbersTests` unit tests, so the blanket case
   is caught elsewhere. Downgraded to 🟡 (F12); the residual is an input-specific throw.
3. **The hosting lens's 🔴 was confirmed and then upgraded from inference to fact.** It reasoned
   from assembly string heaps that `BindingAddress.Parse` is OS-dependent. Confirmed three ways:
   the released source (`GetUnixPipePath` decrements the prefix length off-Windows), a local
   probe (the address throws on Windows; a unix address that parses yields `Port=0`), and the CI
   log itself. See F1.

## Findings

Full detail, evidence and failure scenarios in [findings-v3.md](findings-v3.md); canonical
identities in [ledger.md](ledger.md).

| F# | Sev | D# | Title | Cause |
|---|---|---|---|---|
| F1 | 🔴 | D74 | The scrape guard mis-parses socket/pipe listeners off-Windows; its own test fails on CI and rule 2 cannot fire | fix-caused (D45) |
| F2 | 🟠 | D75 | F4's timeout carve-out is dead code on net8.0; a real Google outage racing a client abort now returns 200 and reaches neither SLO 1 nor Sentry | fix-caused (D43) |
| F3 | 🟠 | D76 | The unmapped-500 branch's log level is unpinned — D49 one branch over, on the path that handles most 500s | fix-caused (D49) |
| F4 | 🟠 | D77 | Sentry honours an inbound `sentry-trace` sampling decision ahead of `TracesSampleRate` — F2's class, one layer over | pre-existing |
| F5 | 🟠 | D78 | The nested-`MetricCapture` throw, the whole point of the F8 repair, has no test | fix-caused (D47) |
| F6 | 🟠 | D79 | Nothing pins F2's production call site, and the recorded reason it cannot be pinned is refuted | fix-caused (D41) |
| F7 | 🟠 | D80 | SLO 3's query contradicts SLO 3's prose: correct idempotent handling and anonymous garbage both count as failures | pre-existing |
| F8 | 🟠 | D81 | `slos.md` still asserts SLO 1 is measured, and now offers it as SLO 3's reliable cross-check — while SLO 1 is the parked, diluted one | fix-caused (D50) |
| F9 | 🟠 | D82 | `AwbRetryJob`'s `== Paid` filter drops an order advanced past `Paid` before its AWB exists, silencing the only give-up alarm | pre-existing |
| F10 | 🟠 | D83 | `metrics.md`'s "a name that nothing emits fails the build" is false, with a live counterexample the test itself seeds | pre-existing (extends D37) |
| F11 | 🟠 | D84 | `MetricNamesIn` keeps the first-`}` truncation F5 fixed in `LabelUsagesIn`, same file | fix-caused (D44) |
| F12 | 🟡 | D85 | F9's breadcrumb test is absence-only — measured green with every breadcrumb dropped | fix-caused (D48) |
| F13 | 🟡 | D86 | `metrics.md`'s add-a-metric procedure never states `MetricCapture`'s new execution-context requirement | fix-caused (D47) |
| F14 | 🟡 | D87 | ADR-017 still says "a promoted error trace is a single root span" 19 lines below the amendment that corrected it | fix-caused (D42) |
| F15 | 🟡 | D88 | Dashboard walker reach: `templating`/`annotations` queries and library panels unwalked; the query-side parser mis-handles an escaped quote the exposition side handles | fix-caused (D44) |
| F16 | 🟡 | D89 | The label test now requires every queried metric to be seeded by the test itself, undocumented | fix-caused (D44) |
| F17 | 🟡 | D90 | `OrderPhotoPromoter` hand-rolls `HasBeenPaid` as an ordinal comparison plus two name exclusions | pre-existing |
| F18 | 🟡 | D91 | `AdminOrderService` logs client-disconnect cancellations at `Error` on its highest-signal strings | pre-existing |
| F19 | 🟡 | D92 | The `HasBeenPaid` invariant test excludes `Cancelled` **by name**, pushing a future author to add a refund status to `PaidStatuses` | fix-caused (D40) |
| F20 | 🟡 | D93 | `Verdict` counts ports, not reachability: a loopback-only API port plus a wildcard scrape port passes both rules | fix-caused (D45) |
| F21 | 🟡 | D94 | `PrometheusEndpoint` is coupled to the `Caddyfile`'s hard-coded path by comment only — no validator, no test | pre-existing |
| F22 | 🟡 | D95 | `TracingWired == false` in Production warns and boots — the same warn-only class as the admitted `ScrapePort == 0` | pre-existing |
| F23 | 🟡 | D96 | Inbound `baggage` rides out to Stripe, Sameday and Google | pre-existing |
| F24 | 🟡 | D97 | Nothing exercises `StartedAsync`: not the addresses read, not the `Critical` log line, not the throw | fix-caused (D45) |
| F25 | 🟡 | D98 | ADR-017's anti-salting rationale is now weaker than the ADR states — the amendment abandoned its only consumer | fix-caused (D41) |
| F26 | ⚪ | D99 | `MetricCapture._outer` is provably always null; `Dispose`'s restore is dead code advertising nesting the ctor forbids | fix-caused (D47) |
| F27 | ⚪ | D100 | DEPLOYMENT §14.8 step 2 does not name the `ASPNETCORE_URLS` prerequisite that can now hard-fail boot | fix-caused (D45) |
| F28 | ⚪ | D101 | bolt-044 ddd docs still declare a four-value `result` set and `ParentBasedSampler` as shipped | pre-existing |
| F29 | ⚪ | D102 | `resolution-v2.md` misdescribes TestServer as reporting the addresses feature "present but empty" — it returns null | fix-caused (D45) |

## Deferrals

All 23 standing deferrals re-affirmed; **one closes**. D46 confirmed untouched and exactly as
described (SLO 1's query still carries no route or host filter, the instrumentation still sets no
`Filter`), and the file now points a reader the wrong way — see F8. Rows whose cited files the
fix round did not touch stand unchanged since `e965c99`, verified mechanically from the diff.

- **D57 closes** — the F5 fix added the row-panel recursion as a side effect, as the fixer
  claimed. Recorded `fixed`, not `verified`: no dashboard in the repo has a row panel, so the
  recursive arm is unexercised (folded into F15).
- Line-reference drift corrected in the ledger: D72 (`:46`, not `:42`), D70
  (`MetricCapture.cs:64`, not `:48`), D46 (`slos.md:35-36` query, `:27-28` prose, not `:29`).
- D62's rationale carried a stale cross-link to F11/D50 as an open doc half; the caveat landed
  at `slos.md:86-94`, so the link is de-linked in the ledger.

## Tests

- Local, Windows, scoped to the observability namespaces (`Integration` +
  `Unit.{Observability,Middleware,Configuration,Validators,Services,Controllers}`): **1120
  passed / 0 failed**, 10 MinIO skips, at `c92ad77` with a clean tree — before, between and after
  every mutation.
- **CI, `ubuntu-latest`, at the branch tip: RED.** One failure,
  `ScrapeListenerCheckTests.An_address_with_no_port_is_not_counted_as_a_listener`. Six
  consecutive red runs since `e791c40`; green at `8daa977`.
- Frontend not run — backend-only change, per the repo's scoped-run rule.
- Manifest lenses `db-parity` and `frontend-ux` remain **owed, not waived**.
