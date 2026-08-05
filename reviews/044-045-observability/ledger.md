---
type: review-ledger
target: 044-045-observability
updated: 2026-08-05
---

# Canonical finding ledger — 044-045-observability

Stable `D#` identities for this target, per the README's persistent-ledger standard. Each real
defect gets a `D#` that lives forever; each pass's pass-local `F#` maps onto a `D#` **after**
the blinded pass completes (finders never see `D#`).

**Status vocabulary.** `open` = named, not yet fixed · `in-progress` · `fixed` = fixed with a
regression test, awaiting verification · `verified` = a re-review proved the fix holds ·
terminal: `wont-fix` · `deferred` · `disputed` · `false-positive` · `backlog` = triaged
Low/Cleanup that does not re-arm the loop.

**Affirmed** = the commit at which the row's status was last checked against the code.

## v1 findings (D1–D39)

Verification pass v2 (2026-08-05) revert-and-rerun tested all 23 `fixed` rows against `e965c99`.
**22 flip to `verified`. D17 does not** — its fix is partial and this pass declined to verify it
(see [review-v2.md](review-v2.md#d17-v1s-f17--declined-to-verify)).

| D# | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| D1 | 🔴 | v1 (F1) | `/metrics` allow-list checks the TCP peer, which behind Caddy is the proxy IP | `Middleware/MetricsEndpointIpAllowListMiddleware.cs:41` | verified | e965c99 |
| D2 | 🔴 | v1 (F2) | Sentry transactions bypass the scrubber (`SetBeforeSendTransaction` absent) | `Program.cs:57` | verified | e965c99 |
| D3 | 🔴 | v1 (F3) | Scrubber never touches `Request.QueryString`/`Url` — emails and tokens ship | `Configuration/SentryDataScrubbers.cs:44` | verified | e965c99 |
| D4 | 🔴 | v1 (F4) | Case-sensitive header scrubbing — HTTP/2 lowercase names survive | `Configuration/SentryDataScrubbers.cs:46` | verified | e965c99 |
| D5 | 🔴 | v1 (F5) | Per-route sample rates can never match; every route uses `Default` | `Observability/Sampling/RouteAwareSampler.cs:63` | verified | e965c99 |
| D6 | 🔴 | v1 (F6) | "Errors always sampled" is dead code — `OnEnd` skipped for dropped spans | `Observability/ErrorOverrideProcessor.cs:18` | verified | e965c99 |
| D7 | 🔴 | v1 (F7) | Webhook fall-through / re-delivery branches record no metric and no log | `Controllers/WebhooksController.cs:216` | verified | e965c99 |
| D8 | 🔴 | v1 (F8) | Sentry e2e test mocks `IHub`, so the scrubber never runs in any test | `Tests/Integration/SentryIntegrationFactory.cs:85` | verified | e965c99 |
| D9 | 🔴 | v1 (F9) | No test observes any business metric being emitted | `Tests/Integration/MetricsEndpointIntegrationTests.cs:50` | verified | e965c99 |
| D10 | 🟠 | v1 (F10) | Unparseable allow-list entries silently dropped; validator too weak | `Middleware/MetricsEndpointIpAllowListMiddleware.cs:33` | verified | e965c99 |
| D11 | 🟠 | v1 (F11) | Middleware registered `Scoped` — deny-log dedupe never fires | `Extensions/ObservabilityExtensions.cs:50` | verified | e965c99 |
| D12 | 🟠 | v1 (F12) | IPv4-mapped IPv6 peers never match IPv4 allow-list entries | `Middleware/MetricsEndpointIpAllowListMiddleware.cs:42` | verified | e965c99 |
| D13 | 🟠 | v1 (F13) | Empty `Otlp:Endpoint` silently enables the console span exporter in production | `Extensions/ObservabilityExtensions.cs:78` | verified | e965c99 |
| D14 | 🟠 | v1 (F14) | Dashboard and `slos.md` query metric names the API never emits | `ops/dashboards/fototipar-overview.json:309` | verified | e965c99 |
| D15 | 🟠 | v1 (F15) | Mapped 5xx and all Serilog `LogError` bypass Sentry | `Middleware/ExceptionHandlerMiddleware.cs:141` | verified | e965c99 |
| D16 | 🟠 | v1 (F16) | AwbCreator throw path skips `RecordOutcome` | `Services/Sameday/AwbCreator.cs:45` | verified | e965c99 |
| D17 | 🟠 | v1 (F17) | Processing-duration histogram recorded before `SaveChanges`, no once-only guard | `Services/AdminOrderService.cs:133` | **fixed** (partial — commit-ordering leg proven, concurrent double-click leg has no guard and no test; v2 declined to verify) | e965c99 |
| D18 | 🟠 | v1 (F18) | Sentry SDK failures wholly silent (`Debug=false` ⇒ no `DiagnosticLogger`) | `Program.cs:56` | verified | e965c99 |
| D19 | 🟠 | v1 (F19) | Test factories set process-wide env vars in static ctors under parallel xUnit | `Tests/Integration/SentryIntegrationFactory.cs:32` | verified | e965c99 |
| D20 | 🟠 | v1 (F20) | Cardinality tests are arithmetic over constants | `Tests/Unit/Observability/MetricsCardinalityTests.cs:20` | verified | e965c99 |
| D21 | 🟠 | v1 (F21) | Scope-enricher unit tests run with no `IHub`; body never executes | `Tests/Unit/Middleware/SentryScopeEnricherMiddlewareTests.cs:17` | verified | e965c99 |
| D22 | 🟠 | v1 (F22) | Scrubber tests only exercise hand-built events, never SDK-populated ones | `Configuration/SentryDataScrubbers.cs:39` | verified | e965c99 |
| D23 | 🟠 | v1 (F23) | `DEPLOYMENT.md §14` referenced by config does not exist | `appsettings.json:123` | verified | e965c99 |
| D24 | 🟡 | v1 (F24) | Scope enricher registered after auth — pre-auth failures reach Sentry untagged | `Program.cs:357` | backlog | e965c99 |
| D25 | 🟡 | v1 (F25) | EF spans ship full SQL and exception messages to OTLP unscrubbed | `Extensions/ObservabilityExtensions.cs:70` | backlog | e965c99 |
| D26 | 🟡 | v1 (F26) | `NaN` sample rates pass both validators and silently drop everything | `Validators/ObservabilitySettingsValidator.cs:58` | backlog | e965c99 |
| D27 | 🟡 | v1 (F27) | `PrometheusEndpoint="/"` passes validation and would gate the whole site | `Validators/ObservabilitySettingsValidator.cs:37` | backlog | e965c99 |
| D28 | 🟡 | v1 (F28) | `ValidateOnStart` wiring untested — **narrowed at v2**: now exercised by `An_unparseable_allow_list_entry_aborts_boot`; only the blank-`PrometheusEndpoint` leg remains untested | `Program.cs:72` | backlog | e965c99 |
| D29 | 🟡 | v1 (F29) | Enricher sets `scope.User.Id` instead of the required `user_id` tag | `Middleware/SentryScopeEnricherMiddleware.cs:33` | backlog | e965c99 (file unchanged since 5cac465) |
| D30 | 🟡 | v1 (F30) | Sampler startup log (story 003 AC) not implemented — **changed shape at v2**: `RouteAwareSampler.cs` is gone so there is no "resolved table", but nothing logs the sampler choice at boot and `Description_includes_the_rate_for_the_startup_log` pins a description for a log that does not exist | `Observability/Sampling/DeterministicTraceIdSampler.cs:19` | backlog | e965c99 |
| D31 | 🟡 | v1 (F31) | Neither subsystem logs its enabled state at boot — **narrowed at v2**: `observability.tracing.disabled` now covers the blank-endpoint case; Sentry's state and the observability master flag are still unlogged | `Program.cs:48` | backlog | e965c99 |
| D32 | 🟡 | v1 (F32) | Unsynchronized capture collections in the shared test fixture | `Tests/Integration/SentryIntegrationFactory.cs:17` | backlog | e965c99 |
| D33 | ⚪ | v1 (F33) | Magic `"unknown"` label escapes `MetricNames`, docs and the cardinality budget | `Services/OrderService.cs:184` | backlog | e965c99 (file unchanged since 5cac465) |
| D34 | ⚪ | v1 (F34) | `///` blocks on concrete classes citing bolt/ADR/story IDs | `Observability/FotoMetrics.cs:5` | backlog | e965c99 (file unchanged since 5cac465) |
| D35 | ⚪ | v1 (F35) | Comment-sweep residue: dangling `/`, surviving bolt citations, run-on lines | `Program.cs:144` | backlog | e965c99 |
| D36 | ⚪ | v1 (F36) | `ddd-02` describes the `Random` approach ADR-017 forbids | `memory-bank/bolts/044-tracing-and-metrics/ddd-02-technical-design.md:195` | backlog | e965c99 (file unchanged since 5cac465) |
| D37 | ⚪ | v1 (F37) | Metric vocabulary shipped ahead of emission (ANAF; constant `status` label) | `Observability/MetricNames.cs:74` | backlog | e965c99 |
| D38 | ⚪ | v1 (F38) | Observability config re-read by string key after binding; duplicated default | `Program.cs:77` | backlog | e965c99 |
| D39 | ⚪ | v1 (F39) | Sentry wiring inlined in `Program.cs` while bolt 044 got an extension method | `Program.cs:29` | backlog | e965c99 |

## v2 findings (D40–D73)

Minted by the [v2 verification pass](review-v2.md). `Cause` records whether the defect was created
by a v1 fix (with the parent `D#`) or pre-dates the fix round. Detail per row is in
[findings-v2.md](findings-v2.md#part-2--new-findings).

Verification pass v3 (2026-08-05) revert-and-rerun tested all 10 `fixed` rows against `7e28317`.
**9 flip to `verified`. D45 does not** — its guard mis-parses socket/pipe listeners off-Windows and
its own regression test fails on CI, so this pass declined to verify it (see
[review-v3.md](review-v3.md#d45-v2s-f6--declined-to-verify)). D46 stays `deferred` per the owner's
2026-08-05 decision. 23 deferrals re-affirmed; **D57 closes** as a side effect of the D44 fix.

| D# | Sev | First seen | Title | File | Cause | Status |
|---|---|---|---|---|---|---|
| D40 | 🟠 | v2 (F1) | Redelivered success webhook for an order past `Paid` logs an incident and burns SLO 3 | `Controllers/WebhooksController.cs:287` | fix-caused (D7) | verified |
| D41 | 🟠 | v2 (F2) | One-arg `ParentBasedSampler` lets an inbound `traceparent` decide sampling, so error promotion never runs | `Extensions/ObservabilityExtensions.cs:66` | pre-existing (v1 miss) | verified (seam only — the production call site is unpinned: D79) |
| D42 | 🟠 | v2 (F3) | `RecordOnly` sets `IsAllDataRequested`, so lowering the sample rate saves far less than §14.7 states | `Observability/Sampling/DeterministicTraceIdSampler.cs:42` | fix-caused (D6) | verified |
| D43 | 🟠 | v2 (F4) | A client abort mid Google sign-in becomes a mapped 502 → Error log + Sentry issue | `Services/GoogleTokenValidator.cs:40` | fix-caused (D15) | verified (its own defect only — the fix's timeout carve-out is dead code: D75) |
| D44 | 🟠 | v2 (F5) | Metric-name test strips `{…}`, so no label is checked, while `slos.md` promises it is | `Tests/Integration/DashboardMetricNamesTests.cs:144` | fix-caused (D14) | verified |
| D45 | 🟠 | v2 (F6) | Nothing checks `ScrapePort` against a bound listener — silent scrape blackout, or silent return of D1 | `Program.cs:378` | fix-caused (D1) | **fixed** (v3 DECLINED to verify — the guard mis-parses socket/pipe listeners off-Windows and its own test fails on CI: D74) |
| D46 | 🟠 | v2 (F7) | SLO 1 counts `/metrics` scrapes though its prose scopes it to site traffic | `memory-bank/operations/slos.md:35-36` query, `:27-28` prose (was `:29`) | pre-existing | **deferred** — owner parked 2026-08-05: the approved fix (exclude at the instrumentation) needs .NET 9; the two remaining routes each change what SLO 1 measures. Availability still cannot read below ~99.7%. Re-affirmed at `7e28317` by v3: defect untouched, and the file now points a reader the wrong way (D81) |
| D47 | 🟠 | v2 (F8) | `MetricCapture`'s meter filter is a no-op; the isolation its comment claims does not exist | `Tests/Helpers/MetricCapture.cs:22` | fix-caused (D9/D20) | verified (the repair's nested-capture throw has no test: D78) |
| D48 | 🟠 | v2 (F9) | The breadcrumb egress hook has no wiring test — deleting it leaves the suite green | `Configuration/SentryDataScrubbers.cs:59` | fix-caused (D2) | verified (new test is absence-only: D85) |
| D49 | 🟠 | v2 (F10) | The `LogWarning → LogError` half of the D15 fix has no test | `Middleware/ExceptionHandlerMiddleware.cs:82` | fix-caused (D15) | verified (mapped branch only — the unmapped-500 branch is still unpinned: D76) |
| D50 | 🟠 | v2 (F11) | `slos.md` says "SLOs 1–4 are measured" without SLO 3's throw-before-branch caveat | `memory-bank/operations/slos.md:3` | fix-caused (D14) | verified (the caveat landed; two other claims in the same file are still false: D80, D81) |
| D51 | 🟡 | v2 (F12) | `TracingExporterSelectionTests` boots live TracerProviders outside `ObservabilityHostCollection` | `Tests/Unit/Observability/TracingExporterSelectionTests.cs:66` | fix-caused (D13) | backlog |
| D52 | 🟡 | v2 (F13) | `payment_failed` records `failed` unconditionally where its sibling uses `duplicate` | `Controllers/WebhooksController.cs:329` | fix-caused (D7) | backlog |
| D53 | 🟡 | v2 (F14) | `MaskedForm` suggests an `::ffff:…/112` form the parser then rejects | `Observability/ScrapeIpAllowList.cs:101` | fix-caused (D10) | backlog |
| D54 | 🟡 | v2 (F15) | `A_mapped_404_is_not_captured_to_sentry` is satisfied by an unrouted request | `Tests/Integration/MappedServerErrorSentryTests.cs` | fix-caused (D15) | backlog |
| D55 | 🟡 | v2 (F16) | The documented `Sentry__Debug=true` verbosity knob is inert under Serilog's Information floor | `docs/DEPLOYMENT.md:873` | fix-caused (D18) | backlog |
| D56 | 🟡 | v2 (F17) | No volume ceiling on the new Sentry capture site | `Middleware/ExceptionHandlerMiddleware.cs:135` | fix-caused (D15) | backlog |
| D57 | 🟡 | v2 (F18) | Dashboard extractor ignores nested row panels | `Tests/Integration/DashboardMetricNamesTests.cs:115` | fix-caused (D14) | **fixed** at `7e28317` as a side effect of the D44 fix (`CollectPanelQueries` now recurses into `panels`); not `verified` — no dashboard in the repo has a row panel, so the recursive arm is unexercised (D88) |
| D58 | 🟡 | v2 (F19) | §13.10 still says a No-Data panel means a name mismatch, contradicting the accepted panel-8 decision | `docs/DEPLOYMENT.md:961` | fix-caused (D14) | backlog |
| D59 | 🟡 | v2 (F20) | AWB shutdown carve-out matches only `OperationCanceledException`; tests run on SQLite, prod is Postgres | `Services/Sameday/AwbCreator.cs:50` | fix-caused (D16) | backlog |
| D60 | 🟡 | v2 (F21) | `CapturingSentryTransport.Payloads` is an unsynchronized `List` across threads | `Tests/Helpers/CapturingSentryTransport.cs:12` | fix-caused (D8) | backlog |
| D61 | 🟡 | v2 (F22) | `wrong_listener` and `not_allowed` denials share one 512-entry log budget | `Middleware/MetricsEndpointIpAllowListMiddleware.cs:19` | fix-caused (D11) | backlog |
| D62 | 🟡 | v2 (F23) | A throw escaping a webhook endpoint records no metric at all — sibling class resolved the opposite way | `Controllers/WebhooksController.cs:119` | pre-existing | backlog |
| D63 | 🟡 | v2 (F24) | `Idempotency-Key` scrubbed, so duplicate-payment triage loses the colliding key | `Configuration/SentryDataScrubbers.cs:12` | fix-caused (D2/D3/D4) | backlog |
| D64 | 🟡 | v2 (F25) | The fail-closed drop is never exercised through the hook, and has no metric behind it | `Configuration/SentryDataScrubbers.cs:333` | fix-caused (D2) | backlog |
| D65 | ⚪ | v2 (F26) | Empty allow-list entry error names neither value nor index | `Observability/ScrapeIpAllowList.cs:30` | fix-caused (D10) | backlog |
| D66 | ⚪ | v2 (F27) | `Scrub(Breadcrumb)` restamps `Timestamp` | `Configuration/SentryDataScrubbers.cs:117` | fix-caused (D2) | backlog |
| D67 | ⚪ | v2 (F28) | bolt-045 walkthrough lines 39/46 still describe the deleted deny-list | `memory-bank/bolts/045-error-tracking-and-slos/implementation-walkthrough.md:39` | fix-caused (D2) | backlog |
| D68 | ⚪ | v2 (F29) | Series-count failure never names `DeclaredInstruments()` | `Tests/Unit/Observability/MetricsCardinalityTests.cs:43` | fix-caused (D20) | backlog |
| D69 | ⚪ | v2 (F30) | `LogCapture` discards category and exception | `Tests/Helpers/LogCapture.cs:33` | fix-caused (D9) | backlog |
| D70 | ⚪ | v2 (F31) | Nothing proves `ContractViolations()` ever returns non-empty | `Tests/Helpers/MetricCapture.cs:64` (was `:48` — line drifted in the D47 fix) | fix-caused (D20) | backlog — re-affirmed at `7e28317`: all 16 call sites assert emptiness, and the new isolation tests never call it |
| D71 | ⚪ | v2 (F32) | "Background roots stay dropped" holds only below rate 1.0 | `Observability/Sampling/DeterministicTraceIdSampler.cs:41` | fix-caused (D6) | backlog |
| D72 | ⚪ | v2 (F33) | Stale-`Routes` boot abort sits below the `Enabled` early return, so §14.8 step 1 cannot catch it | `Extensions/ObservabilityExtensions.cs:46` (was `:42`; the D41/D45 edits shifted the block, ordering unchanged) | fix-caused (D5) | backlog |
| D73 | ⚪ | v2 (F34) | Promotion emits no in-app signal, so "stopped" and "no errors" look identical | `Observability/ErrorOverrideProcessor.cs:17` | fix-caused (D6) | backlog |

## v3 findings (D74–D102)

Minted by the [v3 verification pass](review-v3.md) at `7e28317`. Detail per row in
[findings-v3.md](findings-v3.md). `Cause` records whether the defect was created by a v2 fix
(with the parent `D#`) or pre-dates the fix round. **19 of the 29 are fix-caused.**

| D# | Sev | First seen | Title | File | Cause | Status |
|---|---|---|---|---|---|---|
| D74 | 🔴 | v3 (F1) | Scrape guard mis-parses socket/pipe listeners off-Windows: its own test fails on `ubuntu-latest` (CI red since the fix round) and rule 2 cannot fire | `Observability/ScrapeListenerGuard.cs:23` | fix-caused (D45) | open |
| D75 | 🟠 | v3 (F2) | The F4 timeout carve-out is dead code on net8.0; a Google outage racing a client abort returns 200 and reaches neither SLO 1 nor Sentry | `Services/GoogleTokenValidator.cs:42` | fix-caused (D43) | open |
| D76 | 🟠 | v3 (F3) | The unmapped-500 branch's log level is unpinned — D49 one branch over, on the path handling most 500s | `Middleware/ExceptionHandlerMiddleware.cs:142` | fix-caused (D49) | open |
| D77 | 🟠 | v3 (F4) | Sentry honours an inbound `sentry-trace` sampling decision ahead of `TracesSampleRate` — D41's class, one layer over | `Program.cs:48` | pre-existing | open |
| D78 | 🟠 | v3 (F5) | The nested-`MetricCapture` throw, the point of the D47 repair, has no test | `Tests/Helpers/MetricCapture.cs:30` | fix-caused (D47) | open |
| D79 | 🟠 | v3 (F6) | Nothing pins the production `BuildSampler` call site, and the recorded reason it cannot be pinned is refuted | `Extensions/ObservabilityExtensions.cs:71` | fix-caused (D41) | open |
| D80 | 🟠 | v3 (F7) | SLO 3's query contradicts its prose: correct idempotent handling and anonymous garbage both score as failures | `memory-bank/operations/slos.md:72-83` | pre-existing | open |
| D81 | 🟠 | v3 (F8) | `slos.md` still asserts SLO 1 is measured and now offers it as SLO 3's cross-check — while SLO 1 is the parked, diluted one | `memory-bank/operations/slos.md:5` | fix-caused (D50) | open |
| D82 | 🟠 | v3 (F9) | `AwbRetryJob`'s `== Paid` filter drops an order advanced past `Paid` before its AWB exists, silencing the only give-up alarm | `BackgroundJobs/AwbRetryJob.cs:109` | pre-existing | open (plausible — dispatcher leg unverified) |
| D83 | 🟠 | v3 (F10) | `metrics.md`'s "a name that nothing emits fails the build" is false; the test seeds the exposition it checks | `memory-bank/operations/metrics.md:104` | pre-existing (extends D37) | open |
| D84 | 🟠 | v3 (F11) | `MetricNamesIn` keeps the first-`}` truncation the D44 fix corrected in `LabelUsagesIn`, same file | `Tests/Integration/DashboardMetricNamesTests.cs:275` | fix-caused (D44) | open |
| D85 | 🟡 | v3 (F12) | The breadcrumb test is absence-only — green with every breadcrumb dropped | `Tests/Integration/SentryOptionsWiringTests.cs:103` | fix-caused (D48) | backlog |
| D86 | 🟡 | v3 (F13) | `metrics.md`'s add-a-metric procedure never states `MetricCapture`'s execution-context requirement | `memory-bank/operations/metrics.md:99` | fix-caused (D47) | backlog |
| D87 | 🟡 | v3 (F14) | ADR-017 still says "a promoted error trace is a single root span" 19 lines below its own amendment | `memory-bank/bolts/044-tracing-and-metrics/adr-017-deterministic-trace-id-sampling.md:269` | fix-caused (D42) | backlog |
| D88 | 🟡 | v3 (F15) | Walker reach: `templating`/`annotations` queries and library panels unwalked; query-side parser mis-handles an escaped quote the exposition side handles | `Tests/Integration/DashboardMetricNamesTests.cs:107` | fix-caused (D44) | backlog |
| D89 | 🟡 | v3 (F16) | The label test requires every queried metric to be seeded by the test itself, undocumented | `Tests/Integration/DashboardMetricNamesTests.cs:73` | fix-caused (D44) | backlog |
| D90 | 🟡 | v3 (F17) | `OrderPhotoPromoter` hand-rolls `HasBeenPaid` as an ordinal comparison plus two name exclusions | `Services/OrderPhotoPromoter.cs:87` | pre-existing | backlog |
| D91 | 🟡 | v3 (F18) | `AdminOrderService` logs client-disconnect cancellations at `Error` on its highest-signal strings | `Services/AdminOrderService.cs:183` | pre-existing | backlog |
| D92 | 🟡 | v3 (F19) | The `HasBeenPaid` invariant test excludes `Cancelled` by name, pushing a future author to add a refund status to `PaidStatuses` | `Tests/Unit/Services/OrderStatusMachineTests.cs:27` | fix-caused (D40) | backlog |
| D93 | 🟡 | v3 (F20) | `Verdict` counts ports, not reachability: a loopback-only API port plus a wildcard scrape port passes both rules | `Observability/ScrapeListenerGuard.cs:36` | fix-caused (D45) | backlog |
| D94 | 🟡 | v3 (F21) | `PrometheusEndpoint` is coupled to the `Caddyfile`'s hard-coded path by comment only | `Configuration/ObservabilitySettings.cs:35` | pre-existing | backlog |
| D95 | 🟡 | v3 (F22) | `TracingWired == false` in Production warns and boots — same warn-only class as the admitted `ScrapePort == 0` | `Program.cs:370` | pre-existing | backlog |
| D96 | 🟡 | v3 (F23) | Inbound `baggage` rides out to Stripe, Sameday and Google | `Extensions/ObservabilityExtensions.cs:74` | pre-existing | backlog |
| D97 | 🟡 | v3 (F24) | Nothing exercises `StartedAsync`: not the addresses read, not the `Critical` line §14.10 tells operators to grep, not the throw | `Observability/ScrapeListenerGuard.cs:70` | fix-caused (D45) | backlog |
| D98 | 🟡 | v3 (F25) | ADR-017's anti-salting rationale is now weaker than the ADR states — the amendment abandoned its only consumer | `memory-bank/bolts/044-tracing-and-metrics/adr-017-deterministic-trace-id-sampling.md` | fix-caused (D41) | backlog |
| D99 | ⚪ | v3 (F26) | `MetricCapture._outer` is provably always null; `Dispose`'s restore is dead code advertising forbidden nesting | `Tests/Helpers/MetricCapture.cs:37` | fix-caused (D47) | backlog |
| D100 | ⚪ | v3 (F27) | DEPLOYMENT §14.8 step 2 does not name the `ASPNETCORE_URLS` prerequisite that can now hard-fail boot | `docs/DEPLOYMENT.md` §14.8 | fix-caused (D45) | backlog |
| D101 | ⚪ | v3 (F28) | bolt-044 ddd docs declare a four-value `result` set and `ParentBasedSampler` as shipped | `memory-bank/bolts/044-tracing-and-metrics/ddd-01-domain-model.md:57` | pre-existing | backlog |
| D102 | ⚪ | v3 (F29) | `resolution-v2.md` misdescribes TestServer as reporting the addresses feature "present but empty" — it returns null | `Observability/ScrapeListenerGuard.cs:77` | fix-caused (D45) | backlog |

**Record note (v3):** `findings-v2.md`'s F23 rationale ends with "`slos.md` does not carry the
caveat (F11/D50)". That half is closed — the caveat landed at `slos.md:86-94` and `:5-7`. The
findings file is left as written; read D62 as carrying no open doc half.

## Clusters worth fixing together

- **Webhook classification (D40, D52, D62, and D50 as its doc):** all four are about which terminal
  branch a webhook receipt lands in and what that means for SLO 3. D40 is the live defect; the other
  three are the same map drawn wrong in three other places.
- **Guarantees nothing enforces (D44, D48, D49, D54, D64, and D57 as its blind spot):** six places
  where a test or a document asserts something no assertion covers. Every one of them is a
  revert-proof that was never taken. Fixing them one at a time invites a seventh.
- **Sampling reach (D41, D42, D71, D73):** what the sampler actually decides, what it costs, and
  whether anyone can tell. D41 is the one needing an owner decision.
- **Config the deployment can get wrong silently (D45, D53, D65, D72):** the scrape-port pairing,
  the CIDR suggestion loop, the anonymous entry error, and the pre-flight that cannot fire.
- **Test-harness isolation (D47, D51, D60, and v1's D32):** four shared-state hazards in the new
  test helpers, none reproduced, all mechanically real.

## Cross-target note

**D35** lands on residue from the repo-wide comment sweep (`09173c4`), which belongs to the
`system` target's loop. Recorded here because this branch carries the lines; the fixer decides
which target owns the fix.
