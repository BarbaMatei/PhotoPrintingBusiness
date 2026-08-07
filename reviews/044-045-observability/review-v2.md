---
type: review
target: 044-045-observability
version: 2
supersedes: 1
commit: e965c99
code_tip: 8865d61
branch: feat/bolt-045-error-tracking-slos
pass-type: verification
date: 2026-08-05
reviewer: loop-driver (anchored, per-fix) + 6 anchored cluster lenses
lenses: [security-topology, security-privacy, otel-sdk, correctness-observability, tests-coverage, observability-requirements]
verdict: approve-with-followups
blockers: []
findings: { high: 0, medium: 11, low: 14, cleanup: 9 }
verified: 22
reopened: 0
declined_to_verify: [D17]
tests: { dotnet_scoped: "1081/1081 (+10 skipped MinIO)", frontend: "not run — backend-only change" }
---

# Review v2 — 044-045-observability (verification pass)

Answers [resolution-v1.md](resolution-v1.md) at its `fixed_commit` `e965c99`. Source at the tree
tip `8865d61` is identical to `e965c99` — the three commits since are `reviews/**` documentation
only, confirmed by `git diff e965c99..HEAD -- src/ ops/ docs/ memory-bank/` being empty.

**A quiet verification means "the fixes held", never "the code is clean."** The verdict is capped
at `approve-with-followups` by the README, and this one earns the "with-followups" half: the fixes
held, and asking the three per-cluster questions the runbook mandates turned up 34 new findings,
11 of them 🟠. Four of those are caused by the fixes themselves.

## Part 1 — did each fix hold?

**Yes, all 23.** Every finding marked `fixed` was revert-and-rerun tested: the production code was
put back to its pre-fix behaviour, the scoped test filter run, the failure set compared against a
prediction made before the revert, then restored. 26 separate mutations. Full per-finding evidence
is in [findings-v2.md](findings-v2.md#part-1--revert-and-rerun-evidence).

Baseline before and after: **1081 passed / 0 failed / 10 skipped** across
`Integration` + `Unit.{Observability, Middleware, Configuration, Validators, Services, Controllers}`.
Per this repo's scoped-run rule the full suite and the frontend suite were not run — the change is
backend-only.

Three results worth naming:

- **The mutations the resolution claimed reproduce exactly.** Deleting `SendDefaultPii = false`
  reddens 1 test; deleting `SentryDataScrubbers.Register(o)` reddens 2; the review's own
  unbounded-`user_id` tag on `OrderService`'s `TagList` reddens 1 through `ContractViolations()`;
  putting a static-ctor env var back reddens both isolation tests. The fixer's reporting was
  accurate.
- **F22 has no source of its own to revert** — it is "the scrubber tests only exercise hand-built
  events". It is verified indirectly and soundly: the SDK-shape tests it added
  (`Scrub_redacts_an_event_populated_by_the_aspnetcore_sdk`,
  `Scrub_redacts_a_transaction_request_and_its_spans`) both reddened under the F3 and F4 source
  mutations, which is exactly what the finding asked for.
- **F23 is a documentation fix** — `docs/DEPLOYMENT.md` §14.1–14.12 exists at lines 971–1237, and
  §14.5's boot-abort promise is pinned by `An_unparseable_allow_list_entry_aborts_boot`, which
  reddened under the F10 mutation.

### D17 (v1's F17) — declined to verify

`resolution-v1.md` records it as `fixed` but **PARTIAL**, and asks the re-review to decide whether
the second leg is closed. **It is not, and this pass declines to verify it.** Pass-local `F#` below
are v2's own; the v1 finding is referred to by its ledger id `D17` throughout to keep them apart.

The leg that was fixed holds: reverting the record-after-`SaveChangesAsync` ordering reddens
`UpdateStatusAsync_Shipped_RecordsNoDurationWhenTheCommitFails`. The leg that was not fixed is the
one the finding named first — two concurrent `PATCH`es to Ship the same order can both commit and
both `Record` on a monotonic histogram. There is no conditional write (`WHERE ShippedAt IS NULL`),
no once-only guard, and no test. **D17 stays `fixed`, not `verified`.**

### Deferred rows re-affirmed

All 16 🟡/⚪ deferrals (F24–F39 of v1) were re-checked at the tip. Four cite files unchanged since
`5cac465` and stand with no further work (D29, D33, D34, D36). The other twelve were re-read by
hand. Three changed shape and their ledger rows are annotated:

- **D28 narrowed** — `ValidateOnStart` is now exercised by `An_unparseable_allow_list_entry_aborts_boot`;
  only the blank-`PrometheusEndpoint` boot-abort leg remains untested.
- **D30 changed shape** — `RouteAwareSampler.cs` no longer exists, so "the resolved table" cannot be
  logged. The underlying gap survives: nothing logs the sampler choice at startup, and
  `DeterministicTraceIdSamplerTests.Description_includes_the_rate_for_the_startup_log` pins a
  description for a log that does not exist.
- **D31 narrowed** — `observability.tracing.disabled` now logs for the blank-endpoint case only;
  Sentry's enabled/disabled state and the observability master flag are still unlogged at boot.

## Part 2 — the fix diffs, three questions per cluster

Six anchored lenses, one per fix cluster, each given its cluster's diff rather than the repo.
Every finding below was independently re-checked by the main agent against the code before being
recorded; two agent claims were downgraded on that check and one was re-attributed (see
*Reasons to doubt*).

### 🟠 Medium

| ID | File | Title | Cause |
|---|---|---|---|
| F1 | `Controllers/WebhooksController.cs:287` (and `:223`) | The new `else` treats every non-`Paid` status as "customer charged, order not Paid" — but `Printing`/`Shipped`/`Delivered` are all downstream of `Paid`, so a redelivered success webhook pages ops and burns SLO 3 for a healthy order | fix-caused (D7) |
| F2 | `Extensions/ObservabilityExtensions.cs:66` | One-arg `ParentBasedSampler` hands the sampling decision to the caller: a request carrying `traceparent: …-00` is dropped by `AlwaysOffSampler`, so `ErrorOverrideProcessor.OnEnd` never runs and its 500 is never exported at any rate | pre-existing, missed by v1 |
| F3 | `Observability/Sampling/DeterministicTraceIdSampler.cs:42` | `RecordOnly` sets `IsAllDataRequested`, so every out-of-rate request now pays the full ASP.NET span cost the old `Drop` skipped — lowering `Sampling:Default` saves far less than §14.7 sells, and ADR-017 states only that memory does not grow | fix-caused (D6) |
| F4 | `Services/GoogleTokenValidator.cs:40` with `Middleware/ExceptionHandlerMiddleware.cs:80,135` | The app's only `BadGatewayException` is thrown when `TaskCanceledException` is caught on a call using the request's own token, so a user closing the tab mid Google sign-in now produces an Error log and a Sentry issue — and the middleware's own client-abort guard cannot catch it, because the cancellation was already translated | fix-caused (D15) |
| F5 | `Tests/Integration/DashboardMetricNamesTests.cs:144` | The extractor strips `{…}` before reading identifiers, so no label name and no label value is ever checked — while `slos.md:6-7` now promises "a rename that breaks a panel fails the build" | fix-caused (D14) |
| F6 | `Program.cs:378`, `Validators/ObservabilitySettingsValidator.cs:43` | Nothing checks that a non-zero `ScrapePort` names a listener Kestrel actually bound: one way every scrape 404s forever with no boot warning, the other way (pointing it at the proxied port) silently restores D1 | fix-caused (D1) |
| F7 | `memory-bank/operations/slos.md:29` | SLO 1's prose scopes it to requests to `*.fototipar.ro`, but the query has no route filter and `AddAspNetCoreInstrumentation()` sets no `Filter`, so 5,760 always-200 `/metrics` scrapes a day sit in the denominator | pre-existing, query rewritten by the D14 fix |
| F8 | `Tests/Helpers/MetricCapture.cs:22` | The meter filter is a no-op — `ReferenceEquals(instrument.Meter, FotoMetrics.Meter)` is true for every emission in the process — so the isolation its comment claims does not exist and a live capture can see a parallel test's measurement | fix-caused (D9/D20) |
| F9 | `Configuration/SentryDataScrubbers.cs:59` | The breadcrumb hook is the one egress hook with no wiring test: deleting `SetBeforeBreadcrumb` leaves the suite green, and Sentry's HttpClient integration puts the Google `id_token` URL in a breadcrumb | fix-caused (D2) |
| F10 | `Middleware/ExceptionHandlerMiddleware.cs:82` | The `LogWarning → LogError` half of the F15 fix has no test — reverting it leaves the suite green, while §13.1 and §13.8 tell operators to reconcile Error-level logs against Sentry issues | fix-caused (D15) |
| F11 | `memory-bank/operations/slos.md:3` | The new status block says "SLOs 1–4 are measured" with no caveat, but SLO 3's counter increments only inside a terminal branch — a throw before any branch records nothing, so a Postgres outage reads 100% healthy while customers are charged | fix-caused (D14) |

### 🟡 Low

| ID | File | Title |
|---|---|---|
| F12 | `Tests/Unit/Observability/TracingExporterSelectionTests.cs:66` | Boots real `TracerProvider`s with the console exporter and EF `SetDbStatementForText` outside `ObservabilityHostCollection`, whose own comment asserts no two are ever alive at once |
| F13 | `Controllers/WebhooksController.cs:329` | `payment_failed` records `failed` unconditionally, including for an already-`Paid` order, where its sibling handler uses `duplicate` |
| F14 | `Observability/ScrapeIpAllowList.cs:101` | `MaskedForm` suggests an `::ffff:…/112` form that line 36 then refuses — two boot-failure cycles for one typo, the same class as the octal suggestion already fixed |
| F15 | `Tests/Integration/MappedServerErrorSentryTests.cs` | `A_mapped_404_is_not_captured_to_sentry` is satisfied by an unrouted request — deleting the test endpoint keeps it green |
| F16 | `docs/DEPLOYMENT.md:873` | The documented `Sentry__Debug=true` verbosity knob is inert: Serilog's `MinimumLevel.Default` is `Information`, so every line it unlocks is dropped before any sink |
| F17 | `Middleware/ExceptionHandlerMiddleware.cs:135` | The new capture site has no volume ceiling — a Google `tokeninfo` outage emits one event per sign-in attempt against a 5k/month tier, and by accepted decision nothing counts drops |
| F18 | `Tests/Integration/DashboardMetricNamesTests.cs:115` | The extractor reads only `panels[*].targets`, so grouping the dashboard into rows silently drops every nested query while the non-empty guard still passes |
| F19 | `docs/DEPLOYMENT.md:961` | §13.10 was not swept with §13.1/13.4/13.8 and still says a No-Data panel is a metric-name mismatch, contradicting the accepted permanent No-Data on panel 8 |
| F20 | `Services/Sameday/AwbCreator.cs:50` | The shutdown carve-out matches only `OperationCanceledException`; both new tests run on SQLite, and if Npgsql surfaces a cancelled command as `PostgresException` 57014 every deploy with in-flight AWB jobs depresses SLO 4 |
| F21 | `Tests/Helpers/CapturingSentryTransport.cs:12` | `Payloads` is a plain `List<string>` appended from Sentry's worker thread and read from the test thread |
| F22 | `Middleware/MetricsEndpointIpAllowListMiddleware.cs:19` | `wrong_listener` and `not_allowed` denials share one 512-entry log budget, so a scan on the wrong listener can exhaust the budget for real denials |
| F23 | `Controllers/WebhooksController.cs:119` | A throw escaping either webhook endpoint records no `payment_webhook_total` at all — the same class the fix closed in `AwbCreator` with `result=error`, resolved the opposite way in the sibling handler |
| F24 | `Configuration/SentryDataScrubbers.cs:12` | `Idempotency-Key` is not allow-listed, so a duplicate-payment Sentry issue no longer carries the one field identifying which key collided; it is a client-generated opaque token, not PII |
| F25 | `Configuration/SentryDataScrubbers.cs:333` | The fail-closed drop is never exercised through the hook, so nothing pins "hook returns null ⇒ no envelope on the wire", and its only signal is one Serilog `Error` with no metric behind it |

### ⚪ Cleanup

| ID | File | Title |
|---|---|---|
| F26 | `Observability/ScrapeIpAllowList.cs:30` | The empty-entry failure names neither the value nor the index, unlike every other message |
| F27 | `Configuration/SentryDataScrubbers.cs:117` | `Scrub(Breadcrumb)` restamps `Timestamp` — the preserving constructor is internal in 4.13; harmless under the hook, wrong if the public method is reused |
| F28 | `memory-bank/bolts/045-error-tracking-and-slos/implementation-walkthrough.md:39` | Lines 39 and 46 still describe the deleted sensitive-substring deny-list; the same file contradicts itself 40 lines later |
| F29 | `Tests/Unit/Observability/MetricsCardinalityTests.cs:43` | The series-count failure never names `DeclaredInstruments()` as the place to bump the number |
| F30 | `Tests/Helpers/LogCapture.cs:33` | `CreateLogger` discards `categoryName` and `LogRecord` keeps no exception, so a test cannot assert which source logged or that an exception rode along |
| F31 | `Tests/Helpers/MetricCapture.cs:48` | No test proves `ContractViolations()` ever returns non-empty, though `metrics.md` step 7 now mandates it for every new instrument |
| F32 | `Observability/Sampling/DeterministicTraceIdSampler.cs:41` | "Background roots stay dropped" reads as unconditional but only holds below rate 1.0 — at the shipped default every background EF root is exported with `db.statement` |
| F33 | `Extensions/ObservabilityExtensions.cs:42` | The stale-`Routes` boot abort sits below the `Enabled` early return, so §14.8 step 1's `Enabled=false` pre-flight cannot catch it |
| F34 | `Observability/ErrorOverrideProcessor.cs:17` | Promotion emits no in-app signal, so "promotion silently stopped" and "no errors happened" look identical |

## Reasons to doubt this pass

- **F8 is a confirmed mechanism with unproven impact.** The lens rated it High and predicted random
  reds across six test files. The mechanism is certain — `FotoMetrics.Meter` is a single
  process-wide static, so the `ReferenceEquals` guard cannot exclude anything. But five consecutive
  runs of the colliding sets returned 133/133 green every time. Contamination needs another test to
  emit inside a live capture's millisecond-scale window; five runs is weak evidence of absence, and
  weak evidence of presence would have been a red. Recorded at 🟠 on the mechanism, not the
  prediction.
- **F2 is not a regression, and v1 missed it.** The lens presented it as a finding against this fix
  cluster. `git show 5cac465` shows `ParentBasedSampler` already wrapping the old `RouteAwareSampler`,
  so the hole predates the fix round and belongs to the v1 pass's miss column, not the fixer's.
- **Nothing here re-runs the full suite.** The scoped filter covers every namespace the fix round
  touched, but a fix-caused break in an untouched area would not have been seen.
- **The three cluster questions are a discovery posture inside a verification pass.** That is what
  the runbook prescribes, and it is also why this pass produced 34 findings where a pure
  "did the fix hold" check would have produced zero. The 🟡/⚪ went straight to the ledger backlog
  per the router; only the 🟠 are live work.
- **`db-parity` and `frontend-ux` are still owed**, unchanged from v1. F20 is the first finding to
  land squarely in the parity gap — the AWB cancellation tests run on SQLite while production is
  Postgres — and a `db-parity` lens is exactly what would have caught it earlier.

## Reconciliation

D1–D16, D18–D23 flip to `verified`. **D17 stays `fixed`** (declined, above). The 16 deferred rows
keep `backlog` with their last-affirmed commit moved to `e965c99`; D28, D30 and D31 carry the
annotations above. F1–F34 of this pass are minted as **D40–D73**, with fix lineage recorded per row
in the ledger.

## Notes for the fixer

- **F1 is the one to fix first** and it is small: the two `else` branches need to distinguish
  "downstream of Paid" (a duplicate, already handled for `Paid` itself) from "genuinely not paid".
  `OrderStatusMachine` already knows the ordering.
- **F5, F10, F15 and F9 are one class**: four places where a test or a document asserts a guarantee
  nothing enforces. Fixing them individually invites a fifth. The general shape is that each new
  assertion needs its own revert-proof, which is the same rule v1's F8/F9/F20/F21 were about.
- **F3, F7 and F11 are corrections to claims, not to code** — the cheapest honest fix for each is to
  make the document say what the system does. F3 additionally deserves a measured number.
- **F2 needs a design decision, not a patch.** Trusting an inbound `traceparent` is the correct
  default for a service inside a trusted mesh and the wrong one for a public edge. Whichever way it
  goes, `ParentBasedSampler`'s four-arm constructor is where it is expressed.
