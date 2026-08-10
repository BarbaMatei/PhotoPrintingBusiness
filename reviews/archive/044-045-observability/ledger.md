---
type: review-ledger
target: 044-045-observability
updated: 2026-08-10
closed: 2026-08-10 — owner sign-off @`a4eb7e5` after the v6 verification (v5 round judged patch-grade; no certification pass ran, so no commit of this feature has been blind-searched since v1)
---

# Ledger — 044-045-observability

## Findings

| D# | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| D1 | 🔴 | v1 (F1) | `/metrics` allow-list checks the TCP peer, which behind Caddy is the proxy IP | `Middleware/MetricsEndpointIpAllowListMiddleware.cs:41` | verified | `e965c99` |
| D2 | 🔴 | v1 (F2) | Sentry transactions bypass the scrubber (`SetBeforeSendTransaction` absent) | `Program.cs:57` | verified | `e965c99` |
| D3 | 🔴 | v1 (F3) | Scrubber never touches `Request.QueryString`/`Url` — emails and tokens ship | `Configuration/SentryDataScrubbers.cs:44` | verified | `e965c99` |
| D4 | 🔴 | v1 (F4) | Case-sensitive header scrubbing — HTTP/2 lowercase names survive | `Configuration/SentryDataScrubbers.cs:46` | verified | `e965c99` |
| D5 | 🔴 | v1 (F5) | Per-route sample rates can never match; every route uses `Default` | `Observability/Sampling/RouteAwareSampler.cs:63` | verified | `e965c99` |
| D6 | 🔴 | v1 (F6) | "Errors always sampled" is dead code — `OnEnd` skipped for dropped spans | `Observability/ErrorOverrideProcessor.cs:18` | verified | `e965c99` |
| D7 | 🔴 | v1 (F7) | Webhook fall-through / re-delivery branches record no metric and no log | `Controllers/WebhooksController.cs:216` | verified | `e965c99` |
| D8 | 🔴 | v1 (F8) | Sentry e2e test mocks `IHub`, so the scrubber never runs in any test | `Tests/Integration/SentryIntegrationFactory.cs:85` | verified | `e965c99` |
| D9 | 🔴 | v1 (F9) | No test observes any business metric being emitted | `Tests/Integration/MetricsEndpointIntegrationTests.cs:50` | verified | `e965c99` |
| D10 | 🟠 | v1 (F10) | Unparseable allow-list entries silently dropped; validator too weak | `Middleware/MetricsEndpointIpAllowListMiddleware.cs:33` | verified | `e965c99` |
| D11 | 🟠 | v1 (F11) | Middleware registered `Scoped` — deny-log dedupe never fires | `Extensions/ObservabilityExtensions.cs:50` | verified | `e965c99` |
| D12 | 🟠 | v1 (F12) | IPv4-mapped IPv6 peers never match IPv4 allow-list entries | `Middleware/MetricsEndpointIpAllowListMiddleware.cs:42` | verified | `e965c99` |
| D13 | 🟠 | v1 (F13) | Empty `Otlp:Endpoint` silently enables the console span exporter in production | `Extensions/ObservabilityExtensions.cs:78` | verified | `e965c99` |
| D14 | 🟠 | v1 (F14) | Dashboard and `slos.md` query metric names the API never emits | `ops/dashboards/fototipar-overview.json:309` | verified | `e965c99` |
| D15 | 🟠 | v1 (F15) | Mapped 5xx and all Serilog `LogError` bypass Sentry | `Middleware/ExceptionHandlerMiddleware.cs:141` | verified | `e965c99` |
| D16 | 🟠 | v1 (F16) | AwbCreator throw path skips `RecordOutcome` | `Services/Sameday/AwbCreator.cs:45` | verified | `e965c99` |
| D17 | 🟠 | v1 (F17) | Processing-duration histogram recorded before `SaveChanges`, no once-only guard | `Services/AdminOrderService.cs:133` | fixed | `e965c99` |
| D18 | 🟠 | v1 (F18) | Sentry SDK failures wholly silent (`Debug=false` ⇒ no `DiagnosticLogger`) | `Program.cs:56` | verified | `e965c99` |
| D19 | 🟠 | v1 (F19) | Test factories set process-wide env vars in static ctors under parallel xUnit | `Tests/Integration/SentryIntegrationFactory.cs:32` | verified | `e965c99` |
| D20 | 🟠 | v1 (F20) | Cardinality tests are arithmetic over constants | `Tests/Unit/Observability/MetricsCardinalityTests.cs:20` | verified | `e965c99` |
| D21 | 🟠 | v1 (F21) | Scope-enricher unit tests run with no `IHub`; body never executes | `Tests/Unit/Middleware/SentryScopeEnricherMiddlewareTests.cs:17` | verified | `e965c99` |
| D22 | 🟠 | v1 (F22) | Scrubber tests only exercise hand-built events, never SDK-populated ones | `Configuration/SentryDataScrubbers.cs:39` | verified | `e965c99` |
| D23 | 🟠 | v1 (F23) | `DEPLOYMENT.md §14` referenced by config does not exist | `appsettings.json:123` | verified | `e965c99` |
| D24 | 🟡 | v1 (F24) | Scope enricher registered after auth — pre-auth failures reach Sentry untagged | `Program.cs:357` | backlog | `e965c99` |
| D25 | 🟡 | v1 (F25) | EF spans ship full SQL and exception messages to OTLP unscrubbed | `Extensions/ObservabilityExtensions.cs:70` | backlog | `e965c99` |
| D26 | 🟡 | v1 (F26) | `NaN` sample rates pass both validators and silently drop everything | `Validators/ObservabilitySettingsValidator.cs:58` | backlog | `e965c99` |
| D27 | 🟡 | v1 (F27) | `PrometheusEndpoint="/"` passes validation and would gate the whole site | `Validators/ObservabilitySettingsValidator.cs:37` | backlog | `e965c99` |
| D28 | 🟡 | v1 (F28) | `ValidateOnStart` wiring untested | `Program.cs:72` | backlog | `e965c99` |
| D29 | 🟡 | v1 (F29) | Enricher sets `scope.User.Id` instead of the required `user_id` tag | `Middleware/SentryScopeEnricherMiddleware.cs:33` | backlog | `e965c99` |
| D30 | 🟡 | v1 (F30) | Sampler startup log (story 003 AC) not implemented | `Observability/Sampling/DeterministicTraceIdSampler.cs:19` | backlog | `e965c99` |
| D31 | 🟡 | v1 (F31) | Neither subsystem logs its enabled state at boot | `Program.cs:48` | backlog | `e965c99` |
| D32 | 🟡 | v1 (F32) | Unsynchronized capture collections in the shared test fixture | `Tests/Integration/SentryIntegrationFactory.cs:17` | backlog | `e965c99` |
| D33 | ⚪ | v1 (F33) | Magic `"unknown"` label escapes `MetricNames`, docs and the cardinality budget | `Services/OrderService.cs:184` | backlog | `e965c99` |
| D34 | ⚪ | v1 (F34) | `///` blocks on concrete classes citing bolt/ADR/story IDs | `Observability/FotoMetrics.cs:5` | backlog | `e965c99` |
| D35 | ⚪ | v1 (F35) | Comment-sweep residue: dangling `/`, surviving bolt citations, run-on lines | `Program.cs:144` | backlog | `e965c99` |
| D36 | ⚪ | v1 (F36) | `ddd-02` describes the `Random` approach ADR-017 forbids | `memory-bank/bolts/044-tracing-and-metrics/ddd-02-technical-design.md:195` | backlog | `e965c99` |
| D37 | ⚪ | v1 (F37) | Metric vocabulary shipped ahead of emission (ANAF; constant `status` label) | `Observability/MetricNames.cs:74` | backlog | `e965c99` |
| D38 | ⚪ | v1 (F38) | Observability config re-read by string key after binding; duplicated default | `Program.cs:77` | backlog | `e965c99` |
| D39 | ⚪ | v1 (F39) | Sentry wiring inlined in `Program.cs` while bolt 044 got an extension method | `Program.cs:29` | backlog | `e965c99` |
| D40 | 🟠 | v2 (F1) | Redelivered success webhook for an order past `Paid` logs an incident and burns SLO 3 | `Controllers/WebhooksController.cs:287` | verified | `7e28317` |
| D41 | 🟠 | v2 (F2) | One-arg `ParentBasedSampler` lets an inbound `traceparent` decide sampling, so error promotion never runs | `Extensions/ObservabilityExtensions.cs:66` | verified | `7e28317` |
| D42 | 🟠 | v2 (F3) | `RecordOnly` sets `IsAllDataRequested`, so lowering the sample rate saves far less than §14.7 states | `Observability/Sampling/DeterministicTraceIdSampler.cs:42` | verified | `7e28317` |
| D43 | 🟠 | v2 (F4) | A client abort mid Google sign-in becomes a mapped 502 → Error log + Sentry issue | `Services/GoogleTokenValidator.cs:40` | verified | `7e28317` |
| D44 | 🟠 | v2 (F5) | Metric-name test strips `{…}`, so no label is checked, while `slos.md` promises it is | `Tests/Integration/DashboardMetricNamesTests.cs:144` | verified | `7e28317` |
| D45 | 🟠 | v2 (F6) | Nothing checks `ScrapePort` against a bound listener — silent scrape blackout, or silent return of D1 | `Program.cs:378` | fixed | `7e28317` |
| D46 | 🟠 | v2 (F7) | SLO 1 counts `/metrics` scrapes though its prose scopes it to site traffic | `memory-bank/operations/slos.md:30-40` | deferred | `a4eb7e5` |
| D47 | 🟠 | v2 (F8) | `MetricCapture`'s meter filter is a no-op; the isolation its comment claims does not exist | `Tests/Helpers/MetricCapture.cs:22` | verified | `7e28317` |
| D48 | 🟠 | v2 (F9) | The breadcrumb egress hook has no wiring test — deleting it leaves the suite green | `Configuration/SentryDataScrubbers.cs:59` | verified | `7e28317` |
| D49 | 🟠 | v2 (F10) | The `LogWarning → LogError` half of the D15 fix has no test | `Middleware/ExceptionHandlerMiddleware.cs:82` | verified | `7e28317` |
| D50 | 🟠 | v2 (F11) | `slos.md` says "SLOs 1–4 are measured" without SLO 3's throw-before-branch caveat | `memory-bank/operations/slos.md:3` | verified | `7e28317` |
| D51 | 🟡 | v2 (F12) | `TracingExporterSelectionTests` boots live TracerProviders outside `ObservabilityHostCollection` | `Tests/Unit/Observability/TracingExporterSelectionTests.cs:66` | backlog | `dc203c7` |
| D52 | 🟡 | v2 (F13) | `payment_failed` records `failed` unconditionally where its sibling uses `duplicate` | `Controllers/WebhooksController.cs:329` | backlog | `dc203c7` |
| D53 | 🟡 | v2 (F14) | `MaskedForm` suggests an `::ffff:…/112` form the parser then rejects | `Observability/ScrapeIpAllowList.cs:101` | backlog | `dc203c7` |
| D54 | 🟡 | v2 (F15) | `A_mapped_404_is_not_captured_to_sentry` is satisfied by an unrouted request | `Tests/Integration/MappedServerErrorSentryTests.cs` | backlog | `dc203c7` |
| D55 | 🟡 | v2 (F16) | The documented `Sentry__Debug=true` verbosity knob is inert under Serilog's Information floor | `docs/DEPLOYMENT.md:873` | backlog | `a4eb7e5` |
| D56 | 🟡 | v2 (F17) | No volume ceiling on the new Sentry capture site | `Middleware/ExceptionHandlerMiddleware.cs:135` | backlog | `dc203c7` |
| D57 | 🟡 | v2 (F18) | Dashboard extractor ignores nested row panels | `Tests/Integration/DashboardMetricNamesTests.cs:115` | fixed | `7e28317` |
| D58 | 🟡 | v2 (F19) | §13.10 still says a No-Data panel means a name mismatch, contradicting the accepted panel-8 decision | `docs/DEPLOYMENT.md:962` | backlog | `a4eb7e5` |
| D59 | 🟡 | v2 (F20) | AWB shutdown carve-out matches only `OperationCanceledException`; tests run on SQLite, prod is Postgres | `Services/Sameday/AwbCreator.cs:50` | backlog | `52a0cb9` |
| D60 | 🟡 | v2 (F21) | `CapturingSentryTransport.Payloads` is an unsynchronized `List` across threads | `Tests/Helpers/CapturingSentryTransport.cs:12` | backlog | `dc203c7` |
| D61 | 🟡 | v2 (F22) | `wrong_listener` and `not_allowed` denials share one 512-entry log budget | `Middleware/MetricsEndpointIpAllowListMiddleware.cs:19` | backlog | `dc203c7` |
| D62 | 🟡 | v2 (F23) | A throw escaping a webhook endpoint records no metric at all — sibling class resolved the opposite way | `Controllers/WebhooksController.cs:119` | backlog | `dc203c7` |
| D63 | 🟡 | v2 (F24) | `Idempotency-Key` scrubbed, so duplicate-payment triage loses the colliding key | `Configuration/SentryDataScrubbers.cs:12` | backlog | `dc203c7` |
| D64 | 🟡 | v2 (F25) | The fail-closed drop is never exercised through the hook, and has no metric behind it | `Configuration/SentryDataScrubbers.cs:333` | backlog | `dc203c7` |
| D65 | ⚪ | v2 (F26) | Empty allow-list entry error names neither value nor index | `Observability/ScrapeIpAllowList.cs:30` | backlog | `dc203c7` |
| D66 | ⚪ | v2 (F27) | `Scrub(Breadcrumb)` restamps `Timestamp` | `Configuration/SentryDataScrubbers.cs:117` | backlog | `dc203c7` |
| D67 | ⚪ | v2 (F28) | bolt-045 walkthrough lines 39/46 still describe the deleted deny-list | `memory-bank/bolts/045-error-tracking-and-slos/implementation-walkthrough.md:39` | backlog | `dc203c7` |
| D68 | ⚪ | v2 (F29) | Series-count failure never names `DeclaredInstruments()` | `Tests/Unit/Observability/MetricsCardinalityTests.cs:43` | backlog | `52a0cb9` |
| D69 | ⚪ | v2 (F30) | `LogCapture` discards category and exception | `Tests/Helpers/LogCapture.cs:33` | backlog | `dc203c7` |
| D70 | ⚪ | v2 (F31) | Nothing proves `ContractViolations()` ever returns non-empty | `Tests/Helpers/MetricCapture.cs:64` | backlog | `dc203c7` |
| D71 | ⚪ | v2 (F32) | "Background roots stay dropped" holds only below rate 1.0 | `Observability/Sampling/DeterministicTraceIdSampler.cs:41` | backlog | `dc203c7` |
| D72 | ⚪ | v2 (F33) | Stale-`Routes` boot abort sits below the `Enabled` early return, so §14.8 step 1 cannot catch it | `Extensions/ObservabilityExtensions.cs:46` | backlog | `dc203c7` |
| D73 | ⚪ | v2 (F34) | Promotion emits no in-app signal, so "stopped" and "no errors" look identical | `Observability/ErrorOverrideProcessor.cs:17` | backlog | `dc203c7` |
| D74 | 🔴 | v3 (F1) | Scrape guard mis-parses socket/pipe listeners off-Windows: its own test fails on `ubuntu-latest` (CI red since the fix round) and rule 2 cannot fire | `Observability/ScrapeListenerGuard.cs:23` | verified | `dc203c7` |
| D75 | 🟠 | v3 (F2) | The D43 timeout carve-out is dead code on net8.0; a Google outage racing a client abort returns 200 and reaches neither SLO 1 nor Sentry | `Services/GoogleTokenValidator.cs:42` | verified | `dc203c7` |
| D76 | 🟠 | v3 (F3) | The unmapped-500 branch's log level is unpinned — D49 one branch over, on the path handling most 500s | `Middleware/ExceptionHandlerMiddleware.cs:142` | verified | `dc203c7` |
| D77 | 🟠 | v3 (F4) | Sentry honours an inbound `sentry-trace` sampling decision ahead of `TracesSampleRate` — D41's class, one layer over | `Program.cs:48` | verified | `dc203c7` |
| D78 | 🟠 | v3 (F5) | The nested-`MetricCapture` throw, the point of the D47 repair, has no test | `Tests/Helpers/MetricCapture.cs:30` | verified | `dc203c7` |
| D79 | 🟠 | v3 (F6) | Nothing pins the production `BuildSampler` call site, and the recorded reason it cannot be pinned is refuted | `Extensions/ObservabilityExtensions.cs:71` | verified | `dc203c7` |
| D80 | 🟠 | v3 (F7) | SLO 3's query contradicts its prose: correct idempotent handling and anonymous garbage both score as failures | `memory-bank/operations/slos.md:92-97` | verified | `dc203c7` |
| D81 | 🟠 | v3 (F8) | `slos.md` still asserts SLO 1 is measured and now offers it as SLO 3's cross-check — while SLO 1 is the parked, diluted one | `memory-bank/operations/slos.md:5` | verified | `dc203c7` |
| D82 | 🟠 | v3 (F9) | `AwbRetryJob`'s `== Paid` filter drops an order advanced past `Paid` before its AWB exists, silencing the only give-up alarm | `BackgroundJobs/AwbRetryJob.cs:109` | verified | `dc203c7` |
| D83 | 🟠 | v3 (F10) | `metrics.md`'s "a name that nothing emits fails the build" is false; the test seeds the exposition it checks | `memory-bank/operations/metrics.md:104` | verified | `dc203c7` |
| D84 | 🟠 | v3 (F11) | `MetricNamesIn` keeps the first-`}` truncation the D44 fix corrected in `LabelUsagesIn`, same file | `Tests/Integration/DashboardMetricNamesTests.cs:275` | verified | `dc203c7` |
| D85 | 🟡 | v3 (F12) | The breadcrumb test is absence-only — green with every breadcrumb dropped | `Tests/Integration/SentryOptionsWiringTests.cs:103` | backlog | `52a0cb9` |
| D86 | 🟡 | v3 (F13) | `metrics.md`'s add-a-metric procedure never states `MetricCapture`'s execution-context requirement | `memory-bank/operations/metrics.md:99` | backlog | `52a0cb9` |
| D87 | 🟡 | v3 (F14) | ADR-017 still says "a promoted error trace is a single root span" 19 lines below its own amendment | `memory-bank/bolts/044-tracing-and-metrics/adr-017-deterministic-trace-id-sampling.md:269` | backlog | `dc203c7` |
| D88 | 🟡 | v3 (F15) | Walker reach: `templating`/`annotations` queries and library panels unwalked; query-side parser mis-handles an escaped quote the exposition side handles | `Tests/Integration/DashboardMetricNamesTests.cs:107` | backlog | `a4eb7e5` |
| D89 | 🟡 | v3 (F16) | The label test requires every queried metric to be seeded by the test itself, undocumented | `Tests/Integration/DashboardMetricNamesTests.cs:73` | verified | `dc203c7` |
| D90 | 🟡 | v3 (F17) | `OrderPhotoPromoter` hand-rolls `HasBeenPaid` as an ordinal comparison plus two name exclusions | `Services/OrderPhotoPromoter.cs:87` | backlog | `dc203c7` |
| D91 | 🟡 | v3 (F18) | `AdminOrderService` logs client-disconnect cancellations at `Error` on its highest-signal strings | `Services/AdminOrderService.cs:183` | backlog | `dc203c7` |
| D92 | 🟡 | v3 (F19) | The `HasBeenPaid` invariant test excludes `Cancelled` by name, pushing a future author to add a refund status to `PaidStatuses` | `Tests/Unit/Services/OrderStatusMachineTests.cs:27` | backlog | `dc203c7` |
| D93 | 🟡 | v3 (F20) | `Verdict` counts ports, not reachability: a loopback-only API port plus a wildcard scrape port passes both rules | `Observability/ScrapeListenerGuard.cs:21-40` | backlog | `dc203c7` |
| D94 | 🟡 | v3 (F21) | `PrometheusEndpoint` is coupled to the `Caddyfile`'s hard-coded path by comment only | `Configuration/ObservabilitySettings.cs:35` | backlog | `dc203c7` |
| D95 | 🟡 | v3 (F22) | `TracingWired == false` in Production warns and boots — same warn-only class as the admitted `ScrapePort == 0` | `Program.cs:370` | backlog | `dc203c7` |
| D96 | 🟡 | v3 (F23) | Inbound `baggage` rides out to Stripe, Sameday and Google | `Extensions/ObservabilityExtensions.cs:74` | backlog | `dc203c7` |
| D97 | 🟡 | v3 (F24) | Nothing exercises `StartedAsync`: not the addresses read, not the `Critical` line §14.10 tells operators to grep, not the throw | `Observability/ScrapeListenerGuard.cs:100` | verified | `dc203c7` |
| D98 | 🟡 | v3 (F25) | ADR-017's anti-salting rationale is now weaker than the ADR states — the amendment abandoned its only consumer | `memory-bank/bolts/044-tracing-and-metrics/adr-017-deterministic-trace-id-sampling.md` | backlog | `dc203c7` |
| D99 | ⚪ | v3 (F26) | `MetricCapture._outer` is provably always null; `Dispose`'s restore is dead code advertising forbidden nesting | `Tests/Helpers/MetricCapture.cs:37` | backlog | `dc203c7` |
| D100 | ⚪ | v3 (F27) | DEPLOYMENT §14.8 step 2 does not name the `ASPNETCORE_URLS` prerequisite that can now hard-fail boot | `docs/DEPLOYMENT.md:1183` | verified | `dc203c7` |
| D101 | ⚪ | v3 (F28) | bolt-044 ddd docs declare a four-value `result` set and `ParentBasedSampler` as shipped | `memory-bank/bolts/044-tracing-and-metrics/ddd-01-domain-model.md:57` | backlog | `dc203c7` |
| D102 | ⚪ | v3 (F29) | `resolution-v2.md` misdescribes TestServer as reporting the addresses feature "present but empty" — it returns null | `Observability/ScrapeListenerGuard.cs:102` | backlog | `dc203c7` |
| D103 | 🟠 | v4 (F1) | SLO 3's `or vector(0)` guards are pinned by nothing — deleting both leaves 1133 green, restoring the "No Data while healthy" defect this round shipped once | `memory-bank/operations/slos.md:95-97`, `ops/dashboards/fototipar-overview.json:232` | verified | `52a0cb9` |
| D104 | 🟠 | v4 (F2) | Both invariants the D75 fix's discriminator rests on are unpinned: the linked deadline token, and `HttpBackstop > RequestDeadline` — breaking the latter restores D75 invisibly | `Services/GoogleTokenValidator.cs:43-50`, `Extensions/SocialAuthExtensions.cs:17` | verified | `52a0cb9` |
| D105 | 🟠 | v4 (F3) | SLO 4 and SLO 5 put benign `skipped`/`pending` in the denominator — the defect D80 fixed — while the status block now says there are "two caveats that matter" | `memory-bank/operations/slos.md:6`, `:135`, `:158` | verified | `52a0cb9` |
| D106 | 🟡 | v4 (F5) | `Sentry:TracesSampleRate=0` no longer switches performance monitoring off, only its output — `IsPerformanceMonitoringEnabled` is true whenever a sampler is set | `Program.cs:59` | open | `dc203c7` |
| D107 | 🟡 | v4 (F6) | The booted-host sampler test covers only `isSampled: true`; the `-0` blinding half of D77 is unpinned | `Tests/Integration/SentryOptionsWiringTests.cs:38-48` | open | `52a0cb9` |
| D108 | 🟡 | v4 (F7) | The re-enqueue query's `Paid`-only scope — an explicit owner decision — is pinned by nothing, and the new test's second assertion cannot fail for its stated reason | `BackgroundJobs/AwbRetryJob.cs:86`, `Tests/Unit/Services/Sameday/AwbRetryJobTests.cs:244` | open | `dc203c7` |
| D109 | 🟡 | v4 (F8) | Rule 3 now aborts boot on a unix-socket API plus a dedicated TCP metrics port, printing a message that is false for that topology | `Observability/ScrapeListenerGuard.cs:57-63` | open | `dc203c7` |
| D110 | 🟡 | v4 (F9) | The dilution figures now on the operator-facing panel are wrong: 5,760/day is `/metrics` alone and the real floor is ~94.5%, not ~99.7% | `memory-bank/operations/slos.md:8-12`, `ops/dashboards/fototipar-overview.json:60` | verified | `a4eb7e5` |
| D111 | 🟡 | v4 (F10) | SLO 3's documented query has no time window while its heading says "rolling 7 days" and its dashboard twin uses `rate(…[7d])`; SLO 4/5 the same | `memory-bank/operations/slos.md:80`, `:95-97` | open | `a4eb7e5` |
| D112 | 🟡 | v4 (F11) | D75's class unswept: two sibling sites still infer "our own timeout" from `!ct.IsCancellationRequested`, losing a claim release on shutdown | `Services/Sameday/AwbCreator.cs:166`, `BackgroundJobs/ShipmentTrackingJob.cs:184` | open | `52a0cb9` |
| D113 | 🟠 | v4 (F4) | `secret-scan` fails on every pull-request run of this branch — gitleaks flags a fabricated test token `.gitleaks.toml` does not allowlist | `Tests/Unit/Configuration/SentryDataScrubbersTests.cs:16`, `.gitleaks.toml`, `.gitleaksignore` | verified | `52a0cb9` |
| D114 | 🟡 | v4 (F12) | The new real-Kestrel boot test runs un-collectioned in the parallel pool and installs a process-wide console-exporting `TracerProvider` under `ASPNETCORE_ENVIRONMENT=Development` | `Tests/Unit/Observability/ScrapeListenerCheckTests.cs:94-120` | open | `dc203c7` |
| D115 | 🟡 | v4 (F13) | `system-architecture.md` still describes the old 5 s `HttpClient` timeout — the standard CLAUDE.md routes readers to, unchanged by the fix that moved the bound | `memory-bank/standards/system-architecture.md:45` | open | `dc203c7` |
| D116 | ⚪ | v4 (F15) | `DEPLOYMENT.md:950` still reasons from the availability target as if the denominator were customer traffic — the third copy D81's fix left behind | `docs/DEPLOYMENT.md:950` | open | `a4eb7e5` |
| D117 | ⚪ | v4 (F16) | The Availability panel `description` and the `status=` give-up log field are both unpinned; the description cites "D46", an id operators cannot resolve | `ops/dashboards/fototipar-overview.json:60`, `BackgroundJobs/AwbRetryJob.cs:123` | open | `a4eb7e5` |
| D118 | ⚪ | v4 (F17) | Comment-rule residue: two two-line narrating comments and a stray double blank line | `Program.cs:57-61`, `BackgroundJobs/AwbRetryJob.cs:105-106` | open | `dc203c7` |
| D119 | ⚪ | v4 (F18) | `resolution-v3.md`'s F11 note overstates the parser unification — three parsers exist and `LabelUsagesIn` keeps its own regex | `reviews/044-045-observability/resolution-v3.md:20` | open | `dc203c7` |
| D120 | 🟡 | v4 (F14) | The give-up alarm's one-shot registry is per-process, so a restart re-pages every order in the 24 h→32 d window — a population D82's fix enlarged | `BackgroundJobs/AwbGiveUpRegistry.cs:21-23` | open | `dc203c7` |
| D121 | 🟡 | v5 (F1) | The `or vector(0)` guards added to the SLO 4 and SLO 5 numerators are pinned by nothing — D103's class rule skips single-term sides, measured green on deletion | `memory-bank/operations/slos.md:142`, `:173`, `ops/dashboards/fototipar-overview.json:271`, `:310` | verified | `a4eb7e5` |
| D122 | 🟡 | v5 (F2) | The acceptance criterion still says SLO 4 excludes only `skipped`, and gives `retry_later`'s reason for it; `orphaned` is unmentioned | `memory-bank/intents/020-observability-stack/units/002-error-tracking-and-slos/stories/002-slo-documentation-and-dashboard.md:27-29` | verified | `a4eb7e5` |
| D123 | 🟡 | v5 (F3) | The outcome union's doc comment calls the cancelled-order case a plain skip — the one case that must now set `Orphaned: true` — and never mentions the flag | `Services/Sameday/AwbCreationOutcome.cs:9` | verified | `a4eb7e5` |
| D124 | ⚪ | v6 (F1) | The guarded-selector list is hand-maintained, so a fifth hand-named success numerator ships unpinned and nothing notices — and the stated reason for not writing the class rule does not hold for a rule keyed on literal `=` matchers | `Tests/Integration/DashboardMetricNamesTests.cs:29-35` | backlog | `a4eb7e5` |

## Details

### D1 — `/metrics` allow-list checks the TCP peer, which behind Caddy is the proxy IP

- **What:** `/metrics` allow-list checks the TCP peer, which behind Caddy is the proxy IP.
- **History:**
  - v1: found (F1)
  - v2: verified @`e965c99`

### D2 — Sentry transactions bypass the scrubber (`SetBeforeSendTransaction` absent)

- **What:** Sentry transactions bypass the scrubber; `SetBeforeSendTransaction` was never set.
- **History:**
  - v1: found (F2)
  - v2: verified @`e965c99`

### D3 — Scrubber never touches `Request.QueryString`/`Url` — emails and tokens ship

- **What:** The scrubber never touched `Request.QueryString`/`Url`, so emails and tokens shipped to Sentry.
- **History:**
  - v1: found (F3)
  - v2: verified @`e965c99`

### D4 — Case-sensitive header scrubbing — HTTP/2 lowercase names survive

- **What:** Header scrubbing was case-sensitive, so HTTP/2 lowercase header names survived unscrubbed.
- **History:**
  - v1: found (F4)
  - v2: verified @`e965c99`

### D5 — Per-route sample rates can never match; every route uses `Default`

- **What:** Per-route sample rates could never match a configured route; every route fell through to `Default`.
- **History:**
  - v1: found (F5)
  - v2: verified @`e965c99`

### D6 — "Errors always sampled" is dead code — `OnEnd` skipped for dropped spans

- **What:** "Errors always sampled" was dead code — `OnEnd` is skipped for dropped spans.
- **History:**
  - v1: found (F6)
  - v2: verified @`e965c99`

### D7 — Webhook fall-through / re-delivery branches record no metric and no log

- **What:** Webhook fall-through and re-delivery branches recorded no metric and no log.
- **History:**
  - v1: found (F7)
  - v2: verified @`e965c99`

### D8 — Sentry e2e test mocks `IHub`, so the scrubber never runs in any test

- **What:** The Sentry end-to-end test mocked `IHub`, so the scrubber never ran in any test.
- **History:**
  - v1: found (F8)
  - v2: verified @`e965c99`

### D9 — No test observes any business metric being emitted

- **What:** No test observed any business metric being emitted.
- **History:**
  - v1: found (F9)
  - v2: verified @`e965c99`

### D10 — Unparseable allow-list entries silently dropped; validator too weak

- **What:** Unparseable allow-list entries were silently dropped and the validator only checked non-emptiness.
- **History:**
  - v1: found (F10)
  - v2: verified @`e965c99`

### D11 — Middleware registered `Scoped` — deny-log dedupe never fires

- **What:** The allow-list middleware was registered `Scoped`, so its deny-log dedupe never fired.
- **History:**
  - v1: found (F11)
  - v2: verified @`e965c99`

### D12 — IPv4-mapped IPv6 peers never match IPv4 allow-list entries

- **What:** IPv4-mapped IPv6 peers never matched IPv4 allow-list entries.
- **History:**
  - v1: found (F12)
  - v2: verified @`e965c99`

### D13 — Empty `Otlp:Endpoint` silently enables the console span exporter in production

- **What:** An empty `Otlp:Endpoint` silently enabled the console span exporter in production.
- **History:**
  - v1: found (F13)
  - v2: verified @`e965c99`

### D14 — Dashboard and `slos.md` query metric names the API never emits

- **What:** The dashboard and `slos.md` queried metric names the API never emits.
- **History:**
  - v1: found (F14)
  - v2: verified @`e965c99`

### D15 — Mapped 5xx and all Serilog `LogError` bypass Sentry

- **What:** Mapped 5xx responses and all Serilog `LogError` events bypassed Sentry.
- **History:**
  - v1: found (F15)
  - v2: verified @`e965c99`

### D16 — AwbCreator throw path skips `RecordOutcome`

- **What:** `AwbCreator`'s throw path skipped `RecordOutcome`, so failed attempts entered no ratio.
- **History:**
  - v1: found (F16)
  - v2: verified @`e965c99`

### D17 — Processing-duration histogram recorded before `SaveChanges`, no once-only guard

- **What:** `Record()` ran before `SaveChangesAsync`: a failed commit left an observation for a shipment
  that never happened, and a double-clicked Ship button let two concurrent PATCHes both record on a
  cumulative histogram — count and p95 on the SLO dashboard drift permanently wrong.
- **Evidence:** `Services/AdminOrderService.cs:133` records, `:148` commits; no concurrency token exists anywhere in the repo.
- **Suggested fix:** Compute the duration into a local and record only after `SaveChangesAsync` returns;
  make the Shipped stamp a conditional write (`WHERE ShippedAt IS NULL`) and record only when it affected a row.
- **History:**
  - v1: found (F17)
  - v2: declined to verify @`e965c99` — the record-after-commit leg is fixed and revert-proven
    (`UpdateStatusAsync_Shipped_RecordsNoDurationWhenTheCommitFails`); the concurrent double-click leg
    has no guard and no test, so the status stays fixed

### D18 — Sentry SDK failures wholly silent (`Debug=false` ⇒ no `DiagnosticLogger`)

- **What:** Sentry SDK failures were wholly silent: `Debug=false` nulls the `DiagnosticLogger`, so quota drops looked like "no errors".
- **History:**
  - v1: found (F18)
  - v2: verified @`e965c99`

### D19 — Test factories set process-wide env vars in static ctors under parallel xUnit

- **What:** Test factories set process-wide env vars in static constructors under parallel xUnit.
- **History:**
  - v1: found (F19)
  - v2: verified @`e965c99`

### D20 — Cardinality tests are arithmetic over constants

- **What:** The cardinality tests multiplied constant arrays and never looked at what call sites emit.
- **History:**
  - v1: found (F20)
  - v2: verified @`e965c99`

### D21 — Scope-enricher unit tests run with no `IHub`; body never executes

- **What:** The scope-enricher unit tests ran with no `IHub`, so the enrichment body never executed.
- **History:**
  - v1: found (F21)
  - v2: verified @`e965c99`

### D22 — Scrubber tests only exercise hand-built events, never SDK-populated ones

- **What:** Scrubber tests only exercised hand-built events, never SDK-populated ones.
- **History:**
  - v1: found (F22)
  - v2: verified @`e965c99`

### D23 — `DEPLOYMENT.md §14` referenced by config does not exist

- **What:** The config comment pointed operators at `DEPLOYMENT.md §14`, which did not exist.
- **History:**
  - v1: found (F23)
  - v2: verified @`e965c99` — §14.1–14.12 present at `docs/DEPLOYMENT.md:971-1237`

### D24 — Scope enricher registered after auth — pre-auth failures reach Sentry untagged

- **What:** The scope enricher runs after authentication in the pipeline, so exceptions thrown in rate
  limiting, response caching, routing or auth are captured without `correlation_id` or user tags and
  cannot be joined to their Serilog lines.
- **Evidence:** `Program.cs:357` (`UseSentryScopeEnricher`) after `UseAuthentication` at `:350`; capture site `ExceptionHandlerMiddleware.cs:141`.
- **Suggested fix:** Split the enricher: set `correlation_id` right after `UseCorrelationId`, add `user_id` in a second post-auth pass.
- **History:**
  - v1: found (F24)
  - v2: re-affirmed @`e965c99` — enricher still at `Program.cs:357`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D25 — EF spans ship full SQL and exception messages to OTLP unscrubbed

- **What:** `SetDbStatementForText = true` attaches EF command text and `RecordException = true` attaches
  exception messages to spans bound for the OTLP collector; the Sentry scrubber is wired only into
  `BeforeSend`, so nothing scrubs this second egress path. EF 8 inlines some values into command text.
- **Evidence:** `Extensions/ObservabilityExtensions.cs:70`.
- **Suggested fix:** Default `SetDbStatementForText` to false or config-gate it; add a span processor redacting the same key set `SentryDataScrubbers` uses.
- **History:**
  - v1: found (F25)
  - v2: re-affirmed @`e965c99`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D26 — `NaN` sample rates pass both validators and silently drop everything

- **What:** `Observability__Sampling__Default=NaN` binds and passes the `is < 0.0 or > 1.0` check (false
  on both comparisons for `NaN`); in the sampler `ratio < NaN` is always false, so every trace is dropped
  silently. `SentrySettingsValidator` has the same shape for `Sentry:SampleRate`.
- **Evidence:** `Validators/ObservabilitySettingsValidator.cs:58`.
- **Suggested fix:** Add `double.IsFinite` checks to both validators for `Sampling:Default`, the per-route rates, `Sentry:SampleRate` and `Sentry:TracesSampleRate`.
- **History:**
  - v1: found (F26)
  - v2: re-affirmed @`e965c99` — `NaN` still satisfies neither comparison
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D27 — `PrometheusEndpoint="/"` passes validation and would gate the whole site

- **What:** The only check on `PrometheusEndpoint` is `StartsWith('/')`, which `"/"` satisfies;
  `UseWhen(StartsWithSegments("/"))` then matches every path, so all real traffic gets a bodyless 403
  and the exporter serves metrics at the site root.
- **Evidence:** `Validators/ObservabilitySettingsValidator.cs:37`; wiring at `Program.cs:74`.
- **Suggested fix:** Require a single non-root segment (length > 1, no whitespace, no query/fragment characters) and reject `"/"` explicitly.
- **History:**
  - v1: found (F27)
  - v2: re-affirmed @`e965c99` — the validator still only checks `StartsWith('/')`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D28 — `ValidateOnStart` wiring untested

- **What:** All validator tests call `Validate(...)` directly; no test boots the host through the real
  `AddOptions<T>().ValidateOnStart()` wiring, so a regression of that registration ships green.
- **Evidence:** `Program.cs:72`; the validator test files.
- **Suggested fix:** Boot the host with `Enabled=true` and an invalid setting and assert
  `OptionsValidationException`; fall back to `/metrics` when the configured path is blank.
- **History:**
  - v1: found (F28)
  - v2: narrowed @`e965c99` — `An_unparseable_allow_list_entry_aborts_boot` now exercises the wiring; only the blank-`PrometheusEndpoint` leg remains untested
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D29 — Enricher sets `scope.User.Id` instead of the required `user_id` tag

- **What:** The acceptance criterion and the implementation plan require a `user_id` tag on every event;
  the middleware sets `scope.User` instead, so Sentry search and alert rules filtering on `tag:user_id` match nothing.
- **Evidence:** `Middleware/SentryScopeEnricherMiddleware.cs:33`; no `SetTag("user_id", ...)` exists.
- **Suggested fix:** Also `SetTag("user_id", userId)` alongside `scope.User`, asserted in the integration test's captured tags.
- **History:**
  - v1: found (F29)
  - v2: re-affirmed @`e965c99` — file unchanged since `5cac465`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D30 — Sampler startup log (story 003 AC) not implemented

- **What:** Nothing logs the sampler choice at boot, so an operator cannot confirm which rates loaded —
  which is how D5 stayed invisible. The bolt's design docs claim startup logs that do not exist.
- **Evidence:** `Observability/Sampling/DeterministicTraceIdSampler.cs:19`; grep for `Log`/`logger` across the sampler and `ObservabilityExtensions.cs` returns nothing (v1).
- **Suggested fix:** Log the resolved rate once from `AddObservability` at Information; correct `ddd-02`/`ddd-03`.
- **History:**
  - v1: found (F30)
  - v2: changed shape @`e965c99` — `RouteAwareSampler.cs` is gone so there is no "resolved table", but
    nothing logs the sampler choice at boot and `Description_includes_the_rate_for_the_startup_log`
    pins a description for a log that does not exist
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D31 — Neither subsystem logs its enabled state at boot

- **What:** A deploy omitting `Observability__Enabled` and `Sentry__Enabled` boots clean with no tracing,
  no `/metrics` and no error capture — and no log line says so; the gap surfaces only when an incident
  produces no Sentry issue.
- **Evidence:** `Program.cs:48` reads both flags and never logs them.
- **Suggested fix:** One Information line per subsystem at boot: enabled state, service name, endpoint or fallback, metrics path, allow-list size, Sentry environment and release.
- **History:**
  - v1: found (F31)
  - v2: narrowed @`e965c99` — `observability.tracing.disabled` now covers the blank-endpoint case; Sentry's state and the observability master flag are still unlogged
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D32 — Unsynchronized capture collections in the shared test fixture

- **What:** `CapturedEvents` (`List<SentryEvent>`) and `CapturedTags` (`Dictionary`) are mutated from Moq
  callbacks on request threads with no lock while the factory is shared via `IClassFixture`. Safe today
  (one awaited request per class); a future concurrent-request test manifests it.
- **Evidence:** `Tests/Integration/SentryIntegrationFactory.cs:17`.
- **Suggested fix:** Use `ConcurrentBag`/`ConcurrentDictionary`, or lock around the callback bodies.
- **History:**
  - v1: found (F32) — plausible, not confirmed
  - v2: re-affirmed @`e965c99` — still a bare `List`/`Dictionary`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D33 — Magic `"unknown"` label escapes `MetricNames`, docs and the cardinality budget

- **What:** Both switch defaults emitting `"unknown"` are unreachable today (closed enums plus validator
  rejection), but the value exists in code and is absent from `MetricNames.*Values`, the docs and the
  cardinality budget; it goes live the moment either enum grows.
- **Evidence:** `Services/OrderService.cs:184`.
- **Suggested fix:** Add an `Unknown` constant to `ProcessorValues`/`AwbResultValues`, include it in the `All` arrays, reference it from both switch defaults.
- **History:**
  - v1: found (F33) — severity reduced low → cleanup at synthesis; the dashboard-drop scenario cannot occur today
  - v2: re-affirmed @`e965c99` — file unchanged since `5cac465`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D34 — `///` blocks on concrete classes citing bolt/ADR/story IDs

- **What:** `///` blocks on concrete classes cite bolt/ADR/story IDs, breaking both halves of the comment
  rule, across `FotoMetrics`, `MetricNames`, both new middlewares, `SentryDataScrubbers`,
  `ObservabilityExtensions` and more.
- **Evidence:** `Observability/FotoMetrics.cs:5`.
- **Suggested fix:** Cut each to one short why-line and drop the references; the rationale already lives in the bolt docs.
- **History:**
  - v1: found (F34)
  - v2: re-affirmed @`e965c99` — file unchanged since `5cac465`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D35 — Comment-sweep residue: dangling `/`, surviving bolt citations, run-on lines

- **What:** Residue from the repo-wide comment sweep: a dangling `/` where a citation was spliced out, a
  surviving bolt-042 citation, and ~130-column run-on lines in `OrderService.cs:394` and `UploadService.cs:208`.
  Cross-target: the sweep (`09173c4`) belongs to the `system` target's loop; recorded here because this
  branch carries the lines — the fixer decides which target owns the fix.
- **Evidence:** `Program.cs:144`.
- **Suggested fix:** Delete the residue and re-wrap the run-on lines.
- **History:**
  - v1: found (F35)
  - v2: re-affirmed @`e965c99` — line shifted to `Program.cs:144`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D36 — `ddd-02` describes the `Random` approach ADR-017 forbids

- **What:** `ddd-02`'s NFR row says the sampling decision is "a single `Random.Shared.NextDouble() < rate`"
  over a `FrozenDictionary`, while ADR-017 forbids `Random` in the sampling path and the code hashes the
  trace id; a maintainer reading the design doc first implements what the ADR bans.
- **Evidence:** `memory-bank/bolts/044-tracing-and-metrics/ddd-02-technical-design.md:195`.
- **Suggested fix:** Correct the NFR row to describe the trace-id hash, pointing at ADR-017.
- **History:**
  - v1: found (F36)
  - v2: re-affirmed @`e965c99` — file unchanged since `5cac465`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D37 — Metric vocabulary shipped ahead of emission (ANAF; constant `status` label)

- **What:** `FotoMetrics.InvoiceAnafStatus` and `MetricNames.AnafStatusValues` have no increment site
  (reserved for intent 016) yet are pinned by two test files; `orders_created_total`'s `status` label is
  a constant `"created"` — a dimension carrying zero information.
- **Evidence:** `Observability/MetricNames.cs:74`.
- **Suggested fix:** Delete the ANAF instrument and its values until intent 016 emits them; drop the constant `status` label or emit the transitions that justify it.
- **History:**
  - v1: found (F37)
  - v2: re-affirmed @`e965c99` — `invoice_anaf_status_total` still has no production call site
  - v5: re-affirmed @`52a0cb9` — the round added `orphaned`, which is emitted; the ANAF vocabulary is untouched
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D38 — Observability config re-read by string key after binding; duplicated default

- **What:** `Program.cs` re-reads the Observability section by string key with a `?? "/metrics"` fallback
  duplicating the settings-class default; change the default in one place and the other silently disagrees.
  The section is bound four times at boot.
- **Evidence:** `Program.cs:77-79`.
- **Suggested fix:** Have `AddObservability` return the bound settings (or expose them via `IOptions`) and read both values from that single instance.
- **History:**
  - v1: found (F38)
  - v2: re-affirmed @`e965c99` — still re-read by string key with the duplicated fallback
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D39 — Sentry wiring inlined in `Program.cs` while bolt 044 got an extension method

- **What:** Sentry wiring is hand-rolled inline in `Program.cs:29-61` — configure, validator,
  `ValidateOnStart`, flag read, `UseSentry` — spelling the full namespace six times, while bolt 044 got
  an `AddObservability` extension.
- **Evidence:** `Program.cs:29`.
- **Suggested fix:** Extract `Extensions/SentryExtensions.cs` mirroring `AddObservability` and drop the redundant namespace qualification.
- **History:**
  - v1: found (F39)
  - v2: re-affirmed @`e965c99` — wiring still inlined with fully-qualified names
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D40 — Redelivered success webhook for an order past `Paid` logs an incident and burns SLO 3

- **What:** A redelivered success webhook for an order already advanced past `Paid` logged an incident and burned SLO 3.
- **History:**
  - v2: found (F1) — caused by the D7 fix
  - v3: verified @`7e28317`

### D41 — One-arg `ParentBasedSampler` lets an inbound `traceparent` decide sampling, so error promotion never runs

- **What:** The one-arg `ParentBasedSampler` let an inbound `traceparent` decide sampling, so error promotion never ran.
- **History:**
  - v2: found (F2) — pre-existing, missed by v1
  - v3: verified @`7e28317` — seam only; the production call site stayed unpinned (D79)

### D42 — `RecordOnly` sets `IsAllDataRequested`, so lowering the sample rate saves far less than §14.7 states

- **What:** `RecordOnly` sets `IsAllDataRequested`, so lowering the sample rate saved far less than §14.7 stated.
- **History:**
  - v2: found (F3) — caused by the D6 fix
  - v3: verified @`7e28317`

### D43 — A client abort mid Google sign-in becomes a mapped 502 → Error log + Sentry issue

- **What:** A client abort mid Google sign-in became a mapped 502 with an Error log and a Sentry issue.
- **History:**
  - v2: found (F4) — caused by the D15 fix
  - v3: verified @`7e28317` — its own defect only; the fix's timeout carve-out was dead code (D75)

### D44 — Metric-name test strips `{…}`, so no label is checked, while `slos.md` promises it is

- **What:** The metric-name test stripped `{…}` before reading identifiers, so no label was checked, while `slos.md` promised it was.
- **History:**
  - v2: found (F5) — caused by the D14 fix
  - v3: verified @`7e28317`

### D45 — Nothing checks `ScrapePort` against a bound listener — silent scrape blackout, or silent return of D1

- **What:** Nothing verified `ScrapePort` names a listener Kestrel actually bound: a non-compose deployment
  gets a silent scrape blackout (one Information line per peer), and a single-listener host that sets
  `ScrapePort=8080` silently restores D1's peer-is-the-proxy exposure.
- **Evidence:** `Program.cs:378`; the validator only range-checks 0–65535 (`ObservabilitySettingsValidator.cs:43`); no test boots a real two-listener Kestrel.
- **Suggested fix:** Check the port against `IServerAddressesFeature` at `ApplicationStarted`; boot a real two-listener Kestrel in a test.
- **History:**
  - v2: found (F6) — caused by the D1 fix
  - v3: declined to verify @`7e28317` — the guard mis-parses socket/pipe listeners off-Windows and its
    own regression test fails on CI (D74); status stays fixed
  - v4: the guard's platform fork was fixed and verified under D74 @`dc203c7`; this row was never re-verified

### D46 — SLO 1 counts `/metrics` scrapes though its prose scopes it to site traffic

- **What:** SLO 1's prose scopes it to requests to `*.fototipar.ro`, but the query carries no route or host
  filter and the instrumentation sets no `Filter`, so ~8,640 always-200 self-monitoring requests a day
  dominate the denominator; with ~500 customer requests a day the ratio cannot read below ~94.5%.
- **Evidence:** `memory-bank/operations/slos.md:30-40` (query); `ObservabilityExtensions.cs:98` sets no `Filter`; `docs/DEPLOYMENT.md:1048` ships `scrape_interval: 15s`; figures recomputed by v6 under D110.
- **Suggested fix:** The owner-approved fix — exclude self-monitoring at the instrumentation — needs .NET 9; the two remaining routes each change what SLO 1 measures.
- **History:**
  - v2: found (F7) — pre-existing; the D14 fix rewrote the query without adding a filter
  - 2026-08-05: owner parked the fix — deferred
  - v3: re-affirmed @`7e28317` — defect untouched; the file then pointed readers the wrong way (D81)
  - v4: re-affirmed @`dc203c7` — still no route/host filter and no instrumentation `Filter`; D81 verified
    but the stated figures were wrong (D110); cited lines drifted to `slos.md:8-12` (caveat) and `:30-40` (query)
  - v5: re-affirmed @`52a0cb9` — query and dilution prose untouched, still owner-parked
  - v6: re-affirmed @`a4eb7e5` — the prose describing the dilution is now right (D110); the dilution itself is not fixed

### D47 — `MetricCapture`'s meter filter is a no-op; the isolation its comment claims does not exist

- **What:** `MetricCapture`'s meter filter was a no-op; the isolation its comment claimed did not exist.
- **History:**
  - v2: found (F8) — caused by the D9/D20 fixes
  - v3: verified @`7e28317` — the repair's nested-capture throw had no test (D78)

### D48 — The breadcrumb egress hook has no wiring test — deleting it leaves the suite green

- **What:** The breadcrumb egress hook had no wiring test; deleting it left the suite green.
- **History:**
  - v2: found (F9) — caused by the D2 fix
  - v3: verified @`7e28317` — the new test is absence-only (D85)

### D49 — The `LogWarning → LogError` half of the D15 fix has no test

- **What:** The `LogWarning → LogError` half of the D15 fix had no test.
- **History:**
  - v2: found (F10) — caused by the D15 fix
  - v3: verified @`7e28317` — mapped branch only; the unmapped-500 branch stayed unpinned (D76)

### D50 — `slos.md` says "SLOs 1–4 are measured" without SLO 3's throw-before-branch caveat

- **What:** `slos.md` said "SLOs 1–4 are measured" without SLO 3's throw-before-branch caveat.
- **History:**
  - v2: found (F11) — caused by the D14 fix
  - v3: verified @`7e28317` — the caveat landed; two other claims in the same file were still false (D80, D81)

### D51 — `TracingExporterSelectionTests` boots live TracerProviders outside `ObservabilityHostCollection`

- **What:** The class boots the real `AddObservability` with `Enabled=true` and resolves live
  `TracerProvider`s outside `ObservabilityHostCollection`, whose comment asserts no two such providers are
  ever alive at once; while alive it prints parallel tests' requests and raw EF SQL to stdout, and one
  test starts a batch OTLP exporter aimed at `http://collector:4317`.
- **Evidence:** `Tests/Unit/Observability/TracingExporterSelectionTests.cs:66`.
- **Suggested fix:** Put the class (and its later siblings) in `ObservabilityHostCollection` or isolate their providers.
- **History:**
  - v2: found (F12) — caused by the D13 fix
  - v3: re-affirmed @`7e28317`
  - v4: re-affirmed @`dc203c7` — extended: the D79 fix added a second live `TracerProvider` build in the
    same class, and the D74 fix added an un-collectioned real-Kestrel boot in `ScrapeListenerCheckTests` (D114)
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D52 — `payment_failed` records `failed` unconditionally where its sibling uses `duplicate`

- **What:** `payment_failed` records `failed` unconditionally, including for orders already `Paid` or
  already `PaymentFailed`, where the sibling success handler records `duplicate` for the equivalent
  repeat; a card declined then approved on one PaymentIntent produces both events with no ordering guarantee.
- **Evidence:** `Controllers/WebhooksController.cs:329`; the round's test asserts `HaveCount(1)` and never the label value.
- **Suggested fix:** Record `duplicate` for repeats, mirroring the success handler; same webhook-classification map as D40/D50/D62.
- **History:**
  - v2: found (F13) — caused by the D7 fix
  - v3: re-affirmed @`7e28317`
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D53 — `MaskedForm` suggests an `::ffff:…/112` form the parser then rejects

- **What:** `MaskedForm` proposes `::ffff:10.42.0.0/112`, which line 36 then refuses with "write it as an
  IPv4 range" — two boot-failure cycles for one typo, the same class as the octal suggestion fixed in `a054fdd`.
- **Evidence:** `Observability/ScrapeIpAllowList.cs:101`, rejection at `:36`.
- **Suggested fix:** Make the suggestion propose a form the parser accepts (an IPv4 range).
- **History:**
  - v2: found (F14) — caused by the D10 fix
  - v3: re-affirmed @`7e28317`
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D54 — `A_mapped_404_is_not_captured_to_sentry` is satisfied by an unrouted request

- **What:** The test asserts only "status is 404" and "no event mentioning the marker", both of which an
  unrouted request satisfies; delete `/__test/throw-mapped-404` and it stays green while proving nothing
  about suppression.
- **Evidence:** `Tests/Integration/MappedServerErrorSentryTests.cs`.
- **Suggested fix:** Pin that the mapped endpoint actually executed before asserting nothing was captured.
- **History:**
  - v2: found (F15) — caused by the D15 fix
  - v3: re-affirmed @`7e28317`
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D55 — The documented `Sentry__Debug=true` verbosity knob is inert under Serilog's Information floor

- **What:** The documented `Sentry__Debug` knob cannot produce output: Serilog's `MinimumLevel.Default` is
  Information in both appsettings files, so every `SentryLevel.Debug` line the flag unlocks maps to MEL
  `Debug` and is dropped before any sink; an operator turning it on sees byte-identical logs.
  Warning-and-above SDK diagnostics do reach the file sink.
- **Evidence:** `docs/DEPLOYMENT.md:873`, `:885`; `appsettings.json:141`.
- **Suggested fix:** Document the Serilog floor next to the knob, or raise the level for the Sentry category when the flag is on.
- **History:**
  - v2: found (F16) — caused by the D18 fix
  - v3: re-affirmed @`7e28317`
  - v4: re-affirmed @`dc203c7`
  - v6: re-affirmed @`a4eb7e5` — the cited `DEPLOYMENT.md` section untouched
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D56 — No volume ceiling on the new Sentry capture site

- **What:** No dedup window, no per-issue cap, and `Sentry:SampleRate` defaults to 1.0 on the new capture
  site: a Google `tokeninfo` outage emits one event per sign-in attempt against the 5k errors/month free
  tier §13.9 sized on "a handful per day"; once the quota 429s the SDK drops every event, including
  unhandled 500s, and by accepted decision nothing counts drops.
- **Evidence:** `Middleware/ExceptionHandlerMiddleware.cs:135`; D43 made the trigger far more likely.
- **Suggested fix:** Add a dedup window or per-issue cap at the capture site.
- **History:**
  - v2: found (F17) — caused by the D15 fix
  - v3: re-affirmed @`7e28317`
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D57 — Dashboard extractor ignores nested row panels

- **What:** The extractor read only `panels[*].targets`; Grafana row panels nest children under
  `panels[i].panels`, so grouping the dashboard into rows silently dropped every nested query while the
  non-empty guard still passed.
- **Evidence:** `Tests/Integration/DashboardMetricNamesTests.cs:115`.
- **Suggested fix:** Exercise the recursive arm with a dashboard containing a row panel (the gap D88 records).
- **History:**
  - v2: found (F18) — caused by the D14 fix
  - v3: fixed @`7e28317` as a side effect of the D44 fix — `CollectPanelQueries` now recurses into `panels`
  - v3: not verified — no dashboard in the repo has a row panel, so the recursive arm is unexercised (D88); status stays fixed

### D58 — §13.10 still says a No-Data panel means a name mismatch, contradicting the accepted panel-8 decision

- **What:** §13.10 was not swept with §13.1/13.4/13.8 and still says a panel reading "No Data" once the
  target is UP is a metric-name mismatch, contradicting the accepted decision that panel 8 (ANAF) reads
  No Data permanently.
- **Evidence:** `docs/DEPLOYMENT.md:962` (was `:961`; line drifted +1 at v6).
- **Suggested fix:** Sweep §13.10 in line with the panel-8 decision.
- **History:**
  - v2: found (F19) — caused by the D14 fix
  - v3: re-affirmed @`7e28317`
  - v4: re-affirmed @`dc203c7`
  - v6: re-affirmed @`a4eb7e5` — section untouched; line drifts +1 from the added operator row
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D59 — AWB shutdown carve-out matches only `OperationCanceledException`; tests run on SQLite, prod is Postgres

- **What:** The shutdown carve-out matches only `OperationCanceledException`, and both new tests run on
  SQLite while production is Postgres; if Npgsql surfaces a cancelled command as `PostgresException`
  57014 or `NpgsqlException`, the catch-all records `error` and every deploy with in-flight AWB jobs
  depresses SLO 4 — exactly what the carve-out exists to prevent.
- **Evidence:** `Services/Sameday/AwbCreator.cs:50`, catch-all at `:54`.
- **Suggested fix:** Cover the Npgsql cancellation shapes; first finding squarely in the still-owed db-parity lens.
- **History:**
  - v2: found (F20) — caused by the D16 fix
  - v3: re-affirmed @`7e28317`
  - v4: re-affirmed @`dc203c7`
  - v5: re-affirmed @`52a0cb9` — the carve-out at `:50` unchanged; the round edited `:67-72` and `:269-273`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D60 — `CapturingSentryTransport.Payloads` is an unsynchronized `List` across threads

- **What:** `Payloads` is a plain `List<string>` appended from Sentry's background worker thread and read
  from the test thread; safe today only because the worker is single-threaded and `FlushAsync` supplies the barrier.
- **Evidence:** `Tests/Helpers/CapturingSentryTransport.cs:12`, `:18`.
- **Suggested fix:** Use a concurrent collection or lock the append and read.
- **History:**
  - v2: found (F21) — caused by the D8 fix
  - v3: re-affirmed @`7e28317`
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D61 — `wrong_listener` and `not_allowed` denials share one 512-entry log budget

- **What:** Both denial kinds share one 512-entry log budget, so a scan against the wrong listener can
  exhaust the budget for real allow-list denials.
- **Evidence:** `Middleware/MetricsEndpointIpAllowListMiddleware.cs:19`.
- **Suggested fix:** Give each denial kind its own budget.
- **History:**
  - v2: found (F22) — caused by the D11 fix
  - v3: re-affirmed @`7e28317`
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D62 — A throw escaping a webhook endpoint records no metric at all — sibling class resolved the opposite way

- **What:** A throw escaping either webhook endpoint records no `payment_webhook_total` at all, so during a
  database outage `ok/total` freezes near 100% and no burn-rate alert on SLO 3 can fire; the fix round
  closed the same class in `AwbCreator.cs:54-59` with `result=error`, so the two handlers of one class
  now disagree. Deliberate and documented in `metrics.md`.
- **Evidence:** `Controllers/WebhooksController.cs:119`, `:123`.
- **Suggested fix:** Record `result=error` on escaping throws, matching `AwbCreator`; same map as D40/D50/D52.
- **History:**
  - v2: found (F23) — pre-existing
  - v3: doc half closed — the SLO 3 caveat landed at `slos.md:86-94` and `:5-7`; the row carries no open doc half
  - v3: re-affirmed @`7e28317`
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D63 — `Idempotency-Key` scrubbed, so duplicate-payment triage loses the colliding key

- **What:** `Idempotency-Key` is not on `AllowedHeaders`, so it is `<scrubbed>`; an
  `IdempotencyConflictException` surfacing for a duplicate-payment report loses the one field identifying
  which key collided, and it is not logged anywhere else. It is a client-generated opaque token, not PII.
- **Evidence:** `Configuration/SentryDataScrubbers.cs:12`.
- **Suggested fix:** Add `Idempotency-Key` to the allow-list.
- **History:**
  - v2: found (F24) — caused by the D2/D3/D4 fixes
  - v3: re-affirmed @`7e28317`
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D64 — The fail-closed drop is never exercised through the hook, and has no metric behind it

- **What:** The only failure-mode test passes `null!` straight to `Scrub`, so nothing pins "hook returns
  null ⇒ no envelope on the wire" — the SDK behaviour the whole design rests on. A scrubber that starts
  throwing after a data-shape change deletes all error telemetry with one Serilog `Error` per event and
  no metric behind it.
- **Evidence:** `Configuration/SentryDataScrubbers.cs:333`.
- **Suggested fix:** Exercise the drop through the configured hook; add a counter behind it.
- **History:**
  - v2: found (F25) — caused by the D2 fix
  - v3: re-affirmed @`7e28317`
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D65 — Empty allow-list entry error names neither value nor index

- **What:** The empty-entry failure names neither value nor index, unlike every other message; §14.5 tells
  operators indexed env vars merge with the default list, so the natural way to shed the inherited `::1`
  (`Observability__Metrics__AllowedScrapeIps__1=`) aborts boot with a message identifying nothing.
- **Evidence:** `Observability/ScrapeIpAllowList.cs:30`.
- **Suggested fix:** Name the entry index in the message.
- **History:**
  - v2: found (F26) — caused by the D10 fix
  - v3: re-affirmed @`7e28317`
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D66 — `Scrub(Breadcrumb)` restamps `Timestamp`

- **What:** `Scrub(Breadcrumb)` loses the original `Timestamp` (the preserving constructor is internal in
  Sentry 4.13); sub-millisecond and harmless under the hook, it bites only if the public method is reused
  on stored breadcrumbs.
- **Evidence:** `Configuration/SentryDataScrubbers.cs:117`.
- **History:**
  - v2: found (F27) — caused by the D2 fix
  - v3: re-affirmed @`7e28317`
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D67 — bolt-045 walkthrough lines 39/46 still describe the deleted deny-list

- **What:** Commit `44c3e2d` updated only line 81; lines 39 and 46 still describe the deleted
  `SetBeforeSend`-only sensitive-substring deny-list, so the file contradicts itself 40 lines apart.
- **Evidence:** `memory-bank/bolts/045-error-tracking-and-slos/implementation-walkthrough.md:39`, `:46`.
- **Suggested fix:** Update lines 39 and 46 to describe the shipped scrubber.
- **History:**
  - v2: found (F28) — caused by the D2 fix
  - v3: re-affirmed @`7e28317`
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D68 — Series-count failure never names `DeclaredInstruments()`

- **What:** Adding one legitimate label value fails with a count mismatch that never names
  `DeclaredInstruments()` as the place to bump it.
- **Evidence:** `Tests/Unit/Observability/MetricsCardinalityTests.cs:43`.
- **Suggested fix:** Name `DeclaredInstruments()` in the failure message.
- **History:**
  - v2: found (F29) — caused by the D20 fix
  - v3: re-affirmed @`7e28317`
  - v4: re-affirmed @`dc203c7`
  - v5: re-affirmed @`52a0cb9` — only the expected count moved 5 → 6
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D69 — `LogCapture` discards category and exception

- **What:** `CreateLogger` discards `categoryName` and `LogRecord` keeps only level and formatted text, so
  a test attached to a whole `ILoggerFactory` can distinguish sources only by string prefix and cannot
  assert an exception rode along.
- **Evidence:** `Tests/Helpers/LogCapture.cs:33`, `:54`.
- **Suggested fix:** Keep category and exception on the record.
- **History:**
  - v2: found (F30) — caused by the D9 fix
  - v3: re-affirmed @`7e28317`
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D70 — Nothing proves `ContractViolations()` ever returns non-empty

- **What:** No test proves `ContractViolations()` ever returns non-empty, though it is the cardinality
  guard in six files and `metrics.md` step 7 mandates it for every new instrument.
- **Evidence:** `Tests/Helpers/MetricCapture.cs:64` (was `:48`; line drifted in the D47 fix).
- **Suggested fix:** One test feeding a violating emission and asserting the collection is non-empty.
- **History:**
  - v2: found (F31) — caused by the D20 fix
  - v3: re-affirmed @`7e28317` — all 16 call sites assert emptiness, and the new isolation tests never call it
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D71 — "Background roots stay dropped" holds only below rate 1.0

- **What:** The comment reads as unconditional, but line 26 short-circuits to `RecordAndSample` before the
  kind check, so at the shipped `Default = 1.0` every background EF root is exported with `db.statement`;
  the restriction is only ever tested at 0.0.
- **Evidence:** `Observability/Sampling/DeterministicTraceIdSampler.cs:41`, short-circuit at `:26`.
- **Suggested fix:** Apply the kind check ahead of the rate short-circuit, or correct the comment and test at rate 1.0.
- **History:**
  - v2: found (F32) — caused by the D6 fix
  - v3: re-affirmed @`7e28317`
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D72 — Stale-`Routes` boot abort sits below the `Enabled` early return, so §14.8 step 1 cannot catch it

- **What:** The stale-`Routes` boot abort sits below the `Enabled` early return, so §14.8 step 1 ("deploy
  with `Enabled=false`, confirm the API boots clean") passes with the dead key still in place; the abort
  only lands at step 2.
- **Evidence:** `Extensions/ObservabilityExtensions.cs:46` (was `:42`; the D41/D45 edits shifted the block, ordering unchanged).
- **Suggested fix:** Hoist the stale-key abort above the `Enabled` return.
- **History:**
  - v2: found (F33) — caused by the D5 fix
  - v3: re-affirmed @`7e28317`
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D73 — Promotion emits no in-app signal, so "stopped" and "no errors" look identical

- **What:** Promotion emits no in-app signal — the tag is visible only on a span that already reached the
  collector and no counter exists — so "promotion silently stopped" and "no errors happened" are
  indistinguishable, including for the accepted-untested processor-order gap and D41's remote-parent path.
- **Evidence:** `Observability/ErrorOverrideProcessor.cs:17`.
- **Suggested fix:** Add a promotion counter.
- **History:**
  - v2: found (F34) — caused by the D6 fix
  - v3: re-affirmed @`7e28317`
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D74 — Scrape guard mis-parses socket/pipe listeners off-Windows: its own test fails on `ubuntu-latest` (CI red since the fix round) and rule 2 cannot fire

- **What:** The scrape guard mis-parsed socket/pipe listeners off-Windows; its own test failed on
  `ubuntu-latest` and rule 2 could not fire on the deploy platform.
- **History:**
  - v3: found (F1) — caused by the D45 fix; CI red on six consecutive runs since `e791c40`
  - v4: verified @`dc203c7` — socket/pipe classified by prefix before `Parse`, so the platform fork is
    gone; CI green on `ubuntu-latest` after 6 red runs; residual: rule 3 now aborts a unix-socket +
    TCP-metrics topology (D109)

### D75 — The D43 timeout carve-out is dead code on net8.0; a Google outage racing a client abort returns 200 and reaches neither SLO 1 nor Sentry

- **What:** The D43 fix's timeout carve-out was dead code on net8.0 — `TimeoutException` is never the base
  of the chain — so a Google outage racing a client abort returned 200 and reached neither SLO 1 nor Sentry.
- **History:**
  - v3: found (F2) — caused by the D43 fix; measured against a real `HttpClient` in all four scenarios
  - v4: verified @`dc203c7` — the validator now owns the deadline and the discriminator is a flag it sets;
    restoring the old filter reddens exactly one test; both invariants it rests on were unpinned (D104)

### D76 — The unmapped-500 branch's log level is unpinned — D49 one branch over, on the path handling most 500s

- **What:** The unmapped-500 branch's log level was unpinned — D49's gap one branch over, on the path handling most 500s.
- **History:**
  - v3: found (F3) — caused by the D49 fix; `LogError → LogWarning` at `:142` measured 255 green
  - v4: verified @`dc203c7`

### D77 — Sentry honours an inbound `sentry-trace` sampling decision ahead of `TracesSampleRate` — D41's class, one layer over

- **What:** Sentry honoured an inbound `sentry-trace` sampling decision ahead of `TracesSampleRate` — D41's class one layer over, with no `TracesSampler` configured and no header stripping at the edge.
- **History:**
  - v3: found (F4) — pre-existing; settled from the SDK 4.13.0 documentation, not measured
  - v4: verified @`dc203c7` at the wiring — `TracesSampler` answers on every call, the contract that
    outranks an inbound `sentry-trace`; residuals: the `-0` direction unpinned (D107), and
    `TracesSampleRate=0` no longer disables performance monitoring (D106)

### D78 — The nested-`MetricCapture` throw, the point of the D47 repair, has no test

- **What:** The nested-`MetricCapture` throw — the point of the D47 repair — had no test; deleting it measured 738 green.
- **History:**
  - v3: found (F5) — caused by the D47 fix
  - v4: verified @`dc203c7`

### D79 — Nothing pins the production `BuildSampler` call site, and the recorded reason it cannot be pinned is refuted

- **What:** Nothing pinned the production `BuildSampler` call site — re-wrapping it in `ParentBasedSampler`
  restored D41 with 1120 tests green — and the recorded reason it could not be pinned was refuted.
- **History:**
  - v3: found (F6) — caused by the D41 fix
  - v4: verified @`dc203c7` — the reflection pin reads the real `TracerProviderSdk.Sampler`, fails loudly
    on a rename, and re-wrapping the call site reddens it

### D80 — SLO 3's query contradicts its prose: correct idempotent handling and anonymous garbage both score as failures

- **What:** SLO 3's query contradicted its prose: `duplicate` landed in the denominator only, and
  `signature_invalid` on an anonymous endpoint let anyone drive SLO 3 to 0.
- **History:**
  - v3: found (F7) — pre-existing; cited lines later drifted to `slos.md:92-97`
  - v4: verified @`dc203c7` — query matches prose in both copies and `ok`/`duplicate` stay build-checked;
    residuals: the `or vector(0)` guard unpinned (D103), SLO 4/5 carry the same denominator defect (D105),
    neither copy carries the 7-day window its heading claims (D111)

### D81 — `slos.md` still asserts SLO 1 is measured and now offers it as SLO 3's cross-check — while SLO 1 is the parked, diluted one

- **What:** `slos.md` still asserted SLO 1 is measured and offered it as SLO 3's cross-check, while SLO 1
  was the parked, diluted one (D46).
- **History:**
  - v3: found (F8) — caused by the D50 fix
  - v4: verified @`dc203c7` (doc-only) — the status block names the dilution instead of claiming SLO 1 is
    measured, and the caveat also reached the panel operators read; residuals: the stated figures were
    wrong (D110), and a third copy survived at `DEPLOYMENT.md:949` (D116)

### D82 — `AwbRetryJob`'s `== Paid` filter drops an order advanced past `Paid` before its AWB exists, silencing the only give-up alarm

- **What:** `AwbRetryJob`'s `== Paid` filter dropped an order an admin advanced past `Paid` before its AWB
  existed, silencing the only "this order will never get a shipping label" alarm.
- **History:**
  - v3: found (F9) — pre-existing; plausible, not confirmed at filing (dispatcher leg unverified)
  - v4: verified @`dc203c7` for the alarm — widened to `Paid || Printing` only, and narrowing it back
    reddens the new test; the dispatcher leg was confirmed by the fix round's approach-check; residuals:
    the re-enqueue boundary is unpinned (D108), and the alarm re-pages after a restart over the widened
    population (D120)

### D83 — `metrics.md`'s "a name that nothing emits fails the build" is false; the test seeds the exposition it checks

- **What:** `metrics.md` promised "a name that nothing emits fails the build" while the test seeds the
  exposition it checks; `invoice_anaf_status_total` was the live counterexample. Extends D37.
- **History:**
  - v3: found (F10) — pre-existing
  - v4: verified @`dc203c7` (doc-only) — step 10 now states what the test proves, names
    `invoice_anaf_status_total` as the live counterexample, and documents the seeding obligation, which
    also closed D89

### D84 — `MetricNamesIn` keeps the first-`}` truncation the D44 fix corrected in `LabelUsagesIn`, same file

- **What:** `MetricNamesIn` kept the first-`}` truncation the D44 fix corrected in `LabelUsagesIn`, so a
  parameterised-route matcher yielded `payments` and `POST` as metric names.
- **History:**
  - v3: found (F11) — caused by the D44 fix
  - v4: verified @`dc203c7` — `StripBraceGroups` is quote-aware and its own test reddens against the old
    regex; `LabelUsagesIn` still keeps a separate regex (D88)

### D85 — The breadcrumb test is absence-only — green with every breadcrumb dropped

- **What:** `SentryOptionsWiringTests.cs:103` asserts only `NotContain(GuestToken)`, never that the
  scrubbed URL is present; measured: `Scrub(Breadcrumb)` returning null for everything leaves the test
  green. The residual is an input-specific throw inside the scrubber, which drops breadcrumbs silently in
  production; the blanket case is caught by two unit tests.
- **Evidence:** `Tests/Integration/SentryOptionsWiringTests.cs:103`; measured at v3.
- **Suggested fix:** Assert the scrubbed URL is present, not just the token absent.
- **History:**
  - v3: found (F12) — caused by the D48 fix; downgraded from the lens's 🟠 on measurement
  - v4: re-affirmed @`dc203c7`
  - v5: re-affirmed @`52a0cb9` — only the fixture constant was renamed
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D86 — `metrics.md`'s add-a-metric procedure never states `MetricCapture`'s execution-context requirement

- **What:** Step 7 tells future authors to prove emissions with `MetricCapture` and never mentions that a
  measurement emitted outside the test's execution context is silently invisible; `AwbCreator` is driven
  from a hosted-service dispatcher in production, so the first integration test capturing
  `awb_creation_total` through it reads zero and its `BeEmpty()` assertions pass vacuously.
- **Evidence:** `memory-bank/operations/metrics.md:99`.
- **Suggested fix:** State the execution-context requirement in the procedure.
- **History:**
  - v3: found (F13) — caused by the D47 fix
  - v4: re-affirmed @`dc203c7`
  - v5: re-affirmed @`52a0cb9` — the round edited the AWB result-value table, not the add-a-metric procedure
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D87 — ADR-017 still says "a promoted error trace is a single root span" 19 lines below its own amendment

- **What:** ADR-017 still opens "A promoted error trace is a single root span" 19 lines below the amendment
  that corrected exactly that wording; under an inbound `traceparent` the promoted span is remote-parented,
  the case `An_errored_span_under_an_unsampled_traceparent_is_still_promoted` now exercises.
- **Evidence:** `memory-bank/bolts/044-tracing-and-metrics/adr-017-deterministic-trace-id-sampling.md:269`.
- **Suggested fix:** Sweep the stale sentence.
- **History:**
  - v3: found (F14) — caused by the D42 fix
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D88 — Walker reach: `templating`/`annotations` queries and library panels unwalked; query-side parser mis-handles an escaped quote the exposition side handles

- **What:** The dashboard walker reads `panels` only: `templating.list[]` variable queries and
  `annotations.list[].target` are unreached, and a panel converted to a library panel leaves the checked
  set entirely; `LabelUsagesIn` also mis-handles an escaped quote (`foo_total{bar="a\"b",baz="ok"}` yields
  zero matches) while the exposition-side `ClosingBrace` handles it. The D57 row recursion has no dashboard exercising it.
- **Evidence:** `Tests/Integration/DashboardMetricNamesTests.cs:107`.
- **Suggested fix:** Walk `templating`/`annotations`; unify the query-side parsers on `ClosingBrace`.
- **History:**
  - v3: found (F15) — caused by the D44 fix
  - v4: re-affirmed @`dc203c7` — extended: the D84 fix made `MetricNamesIn` quote-aware but left
    `LabelUsagesIn`'s own regex, so the two query-side parsers now disagree on an escaped quote
  - v5: re-affirmed @`52a0cb9` — extended: the new guard test consumes `DashboardQueries()`/`SloQueries()` and inherits their reach limits
  - v6: re-affirmed @`a4eb7e5` — the v5 extension unchanged
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D89 — The label test requires every queried metric to be seeded by the test itself, undocumented

- **What:** The label test requires every queried metric to be seeded by the test itself, and the obligation was undocumented.
- **History:**
  - v3: found (F16), recorded backlog
  - v4: verified @`dc203c7` — closed by the D83 fix; `metrics.md` step 10 now states the seeding obligation, which is what this row asked for

### D90 — `OrderPhotoPromoter` hand-rolls `HasBeenPaid` as an ordinal comparison plus two name exclusions

- **What:** `OrderPhotoPromoter.cs:87` hand-rolls `HasBeenPaid` as `Status < Paid || == PaymentFailed ||
  == Cancelled`; correct today, unsafe by default — a future `Refunded` ordered after `Paid` silently
  passes and gets that order's photos promoted to cloud, the exposure the cancel path purges.
  `HasBeenPaid` fails closed for the same addition.
- **Evidence:** `Services/OrderPhotoPromoter.cs:87`.
- **Suggested fix:** Use the status machine's `HasBeenPaid` instead of the hand-rolled comparison.
- **History:**
  - v3: found (F17) — pre-existing
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D91 — `AdminOrderService` logs client-disconnect cancellations at `Error` on its highest-signal strings

- **What:** `:183`, `:279` and `:305` are `catch (Exception) { LogError }` around work taking the request's
  token, and the comment at `:174` names client-disconnect cancellation as expected; an admin closing the
  tab mid-PATCH produces "Refund failed for cancelled order … manual refund required" at Error. No 5xx and
  no Sentry issue — log noise on the highest-signal string in the file.
- **Evidence:** `Services/AdminOrderService.cs:183`.
- **Suggested fix:** Carve cancellations out of the Error logs.
- **History:**
  - v3: found (F18) — pre-existing
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D92 — The `HasBeenPaid` invariant test excludes `Cancelled` by name, pushing a future author to add a refund status to `PaidStatuses`

- **What:** The test asserts "reachable from Paid ⇒ `HasBeenPaid`" with `Cancelled` skipped by name; add a
  future `Refunded` reachable from `Delivered` and the cheapest green is adding it to `PaidStatuses`,
  turning the charged-but-not-paid alarm into a silent `duplicate` for exactly the status needing a human.
- **Evidence:** `Tests/Unit/Services/OrderStatusMachineTests.cs:27`.
- **Suggested fix:** Encode the intended invariant — "reachable from Paid and still a live fulfilment" — instead of the name exclusion.
- **History:**
  - v3: found (F19) — caused by the D40 fix
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D93 — `Verdict` counts ports, not reachability: a loopback-only API port plus a wildcard scrape port passes both rules

- **What:** `Verdict` counts ports and discards the host part, so `http://127.0.0.1:8080;http://+:9090`
  with `ScrapePort=9090` passes both rules even though the scrape port is the only externally reachable
  listener; the shipped compose gets this right, so it is a residual on the new surface.
- **Evidence:** `Observability/ScrapeListenerGuard.cs:21-40` (was `:36`).
- **Suggested fix:** Consider reachability (the host part), not just distinct port counts.
- **History:**
  - v3: found (F20) — caused by the D45 fix
  - v4: re-affirmed @`dc203c7` — a loopback-only API port plus a wildcard scrape port still yields two ports and passes both rules
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D94 — `PrometheusEndpoint` is coupled to the `Caddyfile`'s hard-coded path by comment only

- **What:** `PrometheusEndpoint` is configurable while `Caddyfile:17` hard-codes `handle /metrics*`; set it
  to `/telemetry` and Caddy proxies the new path straight from the internet, leaving only the IP
  allow-list, which is documented as untrustworthy behind a proxy. Documented in three places, enforced by
  no validator and no test.
- **Evidence:** `Configuration/ObservabilitySettings.cs:35`.
- **Suggested fix:** Validate or test the pairing.
- **History:**
  - v3: found (F21) — pre-existing
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D95 — `TracingWired == false` in Production warns and boots — same warn-only class as the admitted `ScrapePort == 0`

- **What:** `TracingWired == false` outside Development logs `observability.tracing.disabled` and boots, so
  `Observability:Enabled=true` in Production can mean the whole trace pipeline is silently absent — the
  same warn-only class as the `ScrapePort == 0` inconsistency, and the same shape as D45.
- **Evidence:** `Program.cs:370-375`.
- **Suggested fix:** Fail boot, or escalate above a warn, in Production.
- **History:**
  - v3: found (F22) — pre-existing
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D96 — Inbound `baggage` rides out to Stripe, Sameday and Google

- **What:** The default propagator is `TraceContext + Baggage` and `AddHttpClientInstrumentation()` injects
  the current context outbound, so an attacker-supplied `baggage: k=v` rides out to Stripe, Sameday and
  Google on requests made while handling that request; also the carrier for Sentry's frozen
  dynamic-sampling context. Same class as D41.
- **Evidence:** `Extensions/ObservabilityExtensions.cs:74`.
- **Suggested fix:** Drop `Baggage` from the propagator or scrub it at egress.
- **History:**
  - v3: found (F23) — pre-existing
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D97 — Nothing exercises `StartedAsync`: not the addresses read, not the `Critical` line §14.10 tells operators to grep, not the throw

- **What:** Nothing exercised `ScrapeListenerGuard.StartedAsync` — not the addresses read, not the `Critical` line, not the throw.
- **History:**
  - v3: found (F24), recorded backlog and flagged to the owner in `summary-v3` while already fixed
  - fixed @`d1ffee7` — `A_real_host_refuses_to_start_and_names_the_reason_at_critical` boots a real Kestrel and covers all three legs
  - v4: verified @`dc203c7` — downgrading the log level reddens it (measured)

### D98 — ADR-017's anti-salting rationale is now weaker than the ADR states — the amendment abandoned its only consumer

- **What:** ADR-017 rejects salting the trace-id hash to keep a "publicly documented, stable" invariant
  whose stated purpose is that a peer re-derives the same decision — which the D41 amendment abandoned
  ("we no longer agree with a peer, we re-derive"); the recorded reason for keeping the hash unsalted is
  now weaker than the ADR says, and the amendment does not notice.
- **Evidence:** `memory-bank/bolts/044-tracing-and-metrics/adr-017-deterministic-trace-id-sampling.md`.
- **Suggested fix:** Amend the rationale.
- **History:**
  - v3: found (F25) — caused by the D41 fix
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D99 — `MetricCapture._outer` is provably always null; `Dispose`'s restore is dead code advertising forbidden nesting

- **What:** `_outer` is assigned only on the path where that value is null (`:30` throws otherwise), so
  `Dispose`'s restore at `:97` reduces to `Active.Value = null`; harmless, but the field name and the
  restore advertise nesting support the constructor forbids.
- **Evidence:** `Tests/Helpers/MetricCapture.cs:37`.
- **Suggested fix:** Delete the dead field and the restore.
- **History:**
  - v3: found (F26) — caused by the D47 fix
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D100 — DEPLOYMENT §14.8 step 2 does not name the `ASPNETCORE_URLS` prerequisite that can now hard-fail boot

- **What:** §14.8 step 2 did not name the `ASPNETCORE_URLS` prerequisite; anywhere but the shipped compose, following the runbook verbatim hard-failed boot into a restart loop.
- **History:**
  - v3: found (F27), recorded backlog while already fixed
  - fixed @`d1ffee7` — the step now says "`ASPNETCORE_URLS` must already carry `http://+:9090`"
  - v4: verified @`dc203c7`

### D101 — bolt-044 ddd docs declare a four-value `result` set and `ParentBasedSampler` as shipped

- **What:** `ddd-01:57` declares `result ∈ {ok, failed, duplicate, rejected}` — the shipped set has six
  values and no `rejected`; `ddd-01:121` and `ddd-02:137`, `:244` still present `ParentBasedSampler` as
  the shipped outer sampler with no amendment note.
- **Evidence:** `memory-bank/bolts/044-tracing-and-metrics/ddd-01-domain-model.md:57`.
- **Suggested fix:** Correct both ddd docs.
- **History:**
  - v3: found (F28) — pre-existing
  - v4: re-affirmed @`dc203c7`
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D102 — `resolution-v2.md` misdescribes TestServer as reporting the addresses feature "present but empty" — it returns null

- **What:** `Microsoft.AspNetCore.TestHost` 8.0.11 ships no `IServerAddressesFeature` at all, so
  `Features.Get<…>()` returns null and the guard survives on the `?.` + `?? []`; harmless today,
  misleading to anyone who later simplifies that null-conditional away.
- **Evidence:** `Observability/ScrapeListenerGuard.cs:102` (was `:77`).
- **Suggested fix:** None for the immutable record; the correction lives on this row.
- **History:**
  - v3: found (F29) — caused by the D45 fix
  - v4: re-affirmed @`dc203c7` — the guard now keys the carve-out on an empty address list, and
    `?.Addresses ?? []` still turns the null TestServer feature into one; the misdescription stands as a
    record inaccuracy
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`

### D103 — SLO 3's `or vector(0)` guards are pinned by nothing — deleting both leaves 1133 green, restoring the "No Data while healthy" defect this round shipped once

- **What:** SLO 3's `or vector(0)` guards were pinned by nothing — deleting both left 1133 green, restoring
  a "No Data while healthy" panel the round had shipped once already.
- **History:**
  - v4: found (F1) — caused by the D80 fix; the guard was added mid-round by the fixer's second micro-review
  - v5: verified @`52a0cb9` — fixed as a class rule, not an instance check: deleting both guards reddens
    it, and so does collapsing the two-term numerator to one matcher; its disclosed hole — single-term
    sides are skipped — became D121

### D104 — Both invariants the D75 fix's discriminator rests on are unpinned: the linked deadline token, and `HttpBackstop > RequestDeadline` — breaking the latter restores D75 invisibly

- **What:** Both invariants the D75 fix's discriminator rested on were unpinned: passing `ct` instead of the
  deadline token measured 1133 green, and restoring the 5 s registered timeout measured 1133 green — the
  latter restores D75 invisibly.
- **History:**
  - v4: found (F2) — caused by the D75 fix
  - v5: verified @`52a0cb9` — each invariant reddens its own test: the registered timeout behind the
    deadline reddens the ordering test, and unwiring the deadline from `GetAsync` reddens the wall-clock
    test at 32 s, the mutation v4 could only measure 0-red

### D105 — SLO 4 and SLO 5 put benign `skipped`/`pending` in the denominator — the defect D80 fixed — while the status block now says there are "two caveats that matter"

- **What:** SLO 4 and SLO 5 put benign `skipped`/`pending` outcomes in the denominator — the defect D80's
  fix removed from SLO 3 — while the rewritten status block said there were only "two caveats that matter".
- **History:**
  - v4: found (F3) — pre-existing (extends D80's class); the misleading enumeration caused by the D81 fix
  - v5: verified @`52a0cb9` — both denominators exclude the benign values in both copies, the `orphaned`
    value reddens on revert, and the enumeration reads true again; records the fix left behind became
    D122 (acceptance criterion) and D123 (union doc comment); the guards it added became D121

### D106 — `Sentry:TracesSampleRate=0` no longer switches performance monitoring off, only its output — `IsPerformanceMonitoringEnabled` is true whenever a sampler is set

- **What:** `IsPerformanceMonitoringEnabled` is true whenever a `TracesSampler` is set and `EnableTracing`
  is never assigned, so assigning the sampler unconditionally makes performance monitoring enabled
  forever: at rate 0 the SDK still allocates a `TransactionTracer` per request and invokes the sampler,
  then discards the result. The documented off switch now turns off only the output, not the work.
- **Evidence:** `Program.cs:59`; `sentry-dotnet` 4.13.0 `SentryOptions.cs`/`Hub.cs`, read at the installed version's tag — not measured at runtime.
- **Suggested fix:** Assign `EnableTracing`, or document the changed meaning next to the §13 knob.
- **History:**
  - v4: found (F5) — caused by the D77 fix

### D107 — The booted-host sampler test covers only `isSampled: true`; the `-0` blinding half of D77 is unpinned

- **What:** The new test builds exactly one context (`isSampled: true, isParentSampled: true`), so an
  "abstain when the caller asked not to be traced" edit returns the rate for the only tested context and
  stays green while `Hub.StartTransaction` skips its sampling block — the `-0` half of D77 back.
- **Evidence:** `Tests/Integration/SentryOptionsWiringTests.cs:38-48`.
- **Suggested fix:** Add a second row with `isSampled: false, isParentSampled: false`.
- **History:**
  - v4: found (F6) — caused by the D77 fix
  - v5: re-affirmed @`52a0cb9` — rename only

### D108 — The re-enqueue query's `Paid`-only scope — an explicit owner decision — is pinned by nothing, and the new test's second assertion cannot fail for its stated reason

- **What:** The owner's decision was to widen the give-up alarm query only; the alarm side is pinned, but
  widening the re-enqueue query to `Paid || Printing` measured 1133 green, and the new test's second
  assertion seeds `paidAt: T0.AddHours(-25)` against a 24 h window, so it is outside the re-enqueue window
  whatever statuses the query admits. A symmetry-minded author makes every sweep churn `Skipped` jobs
  indefinitely with no test objecting.
- **Evidence:** `BackgroundJobs/AwbRetryJob.cs:86`; `Tests/Unit/Services/Sameday/AwbRetryJobTests.cs:244-245`.
- **Suggested fix:** Pin the `Paid`-only re-enqueue scope with a test seeded inside the window.
- **History:**
  - v4: found (F7) — caused by the D82 fix

### D109 — Rule 3 now aborts boot on a unix-socket API plus a dedicated TCP metrics port, printing a message that is false for that topology

- **What:** With `ASPNETCORE_URLS=http://unix:/run/api.sock;http://+:9090` the socket is skipped, `ports`
  is `{9090}`, and the guard throws "…the scrape-port gate protects nothing" — false for that topology,
  since the proxy talks to the socket. The shipped theory asserts the abort as intended, so it is a
  decision, but it is recorded nowhere and the message is wrong; today's compose proxies TCP, so it is latent.
- **Evidence:** `Observability/ScrapeListenerGuard.cs:57-63`; `Tests/Unit/Observability/ScrapeListenerCheckTests.cs:123-130`.
- **Suggested fix:** Record the decision and correct the abort message for socket topologies.
- **History:**
  - v4: found (F8) — caused by the D74 fix

### D110 — The dilution figures now on the operator-facing panel are wrong: 5,760/day is `/metrics` alone and the real floor is ~94.5%, not ~99.7%

- **What:** The dilution figures the round stamped on the operator-facing panel were wrong: 5,760/day was `/metrics` alone (self-monitoring is ~8,640/day) and the real floor was ~94.5%, not ~99.7%.
- **History:**
  - v4: found (F9) — caused by the D81 fix; the figures inherited from D46, moved onto the wall by the round
  - v5: re-affirmed @`52a0cb9` — still on the operator's wall, the one minor worth the owner's eye
  - round 5: fixed @`9cfbf75` — owner-directed, outside the v5 finding set: ~8,640/day and a ~94.5% floor,
    each figure naming its source; the "Tracked as D46" citation removed too (half of D117)
  - v6: verified @`a4eb7e5` — every figure recomputed from `DEPLOYMENT.md:1049`, `Dockerfile:43` and
    `DEPLOYMENT.md:951`; premise confirmed at `ObservabilityExtensions.cs:98` (no instrumentation filter);
    corrects the claim, not the dilution — D46 stays parked

### D111 — SLO 3's documented query has no time window while its heading says "rolling 7 days" and its dashboard twin uses `rate(…[7d])`; SLO 4/5 the same

- **What:** The documented query is over bare cumulative counters — no `rate`, no `[7d]` — while the heading
  says "rolling 7 days" and the dashboard twin uses `sum(rate(…[7d]))`; an alert built from the documented
  query is an all-time average that after one good month can never fire inside a 7-day breach. SLO 4 and
  SLO 5 carry the same windowless shape.
- **Evidence:** `memory-bank/operations/slos.md:80`, `:95-97`; dashboard twin at `ops/dashboards/fototipar-overview.json:232`.
- **Suggested fix:** Add the window to the documented queries.
- **History:**
  - v4: found (F10) — pre-existing shape; the block was rewritten that round without adding the window
  - v5: re-affirmed @`52a0cb9` — SLO 4 and SLO 5 were rewritten and still carry no time window
  - v6: re-affirmed @`a4eb7e5` — SLO 4's documented query still windowless (`slos.md:145-146`) while the panel uses `rate(…[7d])`

### D112 — D75's class unswept: two sibling sites still infer "our own timeout" from `!ct.IsCancellationRequested`, losing a claim release on shutdown

- **What:** Two sibling sites still infer "our own timeout" from `!ct.IsCancellationRequested` — the
  inference the D75 fix removed. Host shutdown landing after an `HttpClient` timeout skips the timeout
  arm, `AwbDispatcher.cs:69` swallows the rethrow as shutdown, `RetryLater(PreserveClaim: true)` never
  happens, and the claim is neither released nor deliberately held until its TTL — on an order that may
  already carry a billed AWB, with no metric and no log.
- **Evidence:** `Services/Sameday/AwbCreator.cs:166`, `BackgroundJobs/ShipmentTrackingJob.cs:184`; confirmed by reading both catch sites, not measured.
- **Suggested fix:** Sweep the class with the owned-deadline discriminator the D75 fix introduced.
- **History:**
  - v4: found (F11) — pre-existing
  - v5: re-affirmed @`52a0cb9` — `:166` and `ShipmentTrackingJob` untouched

### D113 — `secret-scan` fails on every pull-request run of this branch — gitleaks flags a fabricated test token `.gitleaks.toml` does not allowlist

- **What:** The `secret-scan` workflow failed on every pull-request run of the branch: gitleaks flagged the fabricated test string at `SentryDataScrubbersTests.cs:16` on entropy plus the substring `live`, and `.gitleaks.toml` did not allowlist the file.
- **History:**
  - v4: found (F4) — pre-existing, introduced `44c3e2d` (2026-07-31); red across two full review passes and a fix round without being noticed
  - fixed @`a9c9478` — the first commit carrying `.gitleaksignore`
  - v5: verified by CI only @`52a0cb9` — the PR-event scan was red at `f0aadd7` and every earlier PR run,
    green from `a9c9478` onward; both fingerprints checked byte-for-byte against the commits they name;
    not provable locally (gitleaks not installed), and any history rewrite of the branch invalidates
    commit-pinned fingerprints silently

### D114 — The new real-Kestrel boot test runs un-collectioned in the parallel pool and installs a process-wide console-exporting `TracerProvider` under `ASPNETCORE_ENVIRONMENT=Development`

- **What:** The class carries no `[Collection]`, so it boots a real Kestrel plus a real meter pipeline in
  the parallel unit pool, and `builder.Environment` comes from ambient configuration: CI (Production)
  builds only a metrics pipeline, but a dev machine with `ASPNETCORE_ENVIRONMENT=Development` installs a
  process-wide console-exporting `TracerProvider` with AspNetCore and EF instrumentation onto shared
  `ActivitySource`s — a local run and a CI run become different experiments.
- **Evidence:** `Tests/Unit/Observability/ScrapeListenerCheckTests.cs:94-120`.
- **Suggested fix:** Add the `[Collection]` and pin the environment.
- **History:**
  - v4: found (F12) — caused by the D74 fix; extends D51

### D115 — `system-architecture.md` still describes the old 5 s `HttpClient` timeout — the standard CLAUDE.md routes readers to, unchanged by the fix that moved the bound

- **What:** The line still reads "(5s timeout; unreachable → 502)": the bound is now owned by
  `GoogleTokenValidator.RequestDeadline` with `HttpClient.Timeout` a 15 s backstop, and "unreachable" now
  also covers a caller who disconnects after the deadline; §13.1 records the second half, the standard
  does not. Incompleteness rather than falsehood — 5 s is still the effective wall-clock.
- **Evidence:** `memory-bank/standards/system-architecture.md:45`.
- **Suggested fix:** Update the standard line in step with the code, per CLAUDE.md's descriptive-standards rule.
- **History:**
  - v4: found (F13) — caused by the D75 fix

### D116 — `DEPLOYMENT.md:950` still reasons from the availability target as if the denominator were customer traffic — the third copy D81's fix left behind

- **What:** The line still reads "Availability target ≥ 99.5% → ≤ 1/200 requests is a 5xx → ≤ 0.5% of a few
  hundred req/day daily" — the third copy the D81 fix left, now contradicting `slos.md` and the panel; a
  reader sizing the Sentry budget still reasons on customer requests.
- **Evidence:** `docs/DEPLOYMENT.md:950` (was `:949`).
- **Suggested fix:** Align the line with the corrected copies.
- **History:**
  - v4: found (F15) — caused by the incomplete D81 fix
  - v6: re-affirmed @`a4eb7e5` — the round did not touch §13.9

### D117 — The Availability panel `description` and the `status=` give-up log field are both unpinned; the description cites "D46", an id operators cannot resolve

- **What:** The Availability panel `description` is read by nothing — deleting it returns the wall to its
  pre-fix state silently — and the `status=` field on the give-up log is read by no test while
  `docs/DEPLOYMENT.md:775` promises it to operators. The description also ended "Tracked as D46", an
  identifier that exists only inside `reviews/**`.
- **Evidence:** `ops/dashboards/fototipar-overview.json:60`, `BackgroundJobs/AwbRetryJob.cs:123`; settled by reading, not mutation.
- **Suggested fix:** Pin both surfaces.
- **History:**
  - v4: found (F16) — caused by the D81/D82 fixes
  - v5: re-affirmed @`52a0cb9` — the panel description untouched
  - v6: half-closed, re-affirmed @`a4eb7e5` — the "Tracked as D46" citation is gone (removed by the D110
    fix @`9cfbf75`); the description and the `status=` field are still pinned by nothing

### D118 — Comment-rule residue: two two-line narrating comments and a stray double blank line

- **What:** Two new comments run to two lines against the one-short-line rule, plus a stray double blank
  line; both state genuine non-obvious constraints (the allowed reason), so the issue is length and the
  blank line, not deletion. Same family as D35.
- **Evidence:** `Program.cs:57-61`, `BackgroundJobs/AwbRetryJob.cs:105-106`.
- **Suggested fix:** Cut each comment to one line; drop the blank line.
- **History:**
  - v4: found (F17) — caused by the D77/D82 fixes

### D119 — `resolution-v3.md`'s F11 note overstates the parser unification — three parsers exist and `LabelUsagesIn` keeps its own regex

- **What:** The note says the fix gives "one brace-matching rule for both sides of the file"; three parsers
  exist, and `LabelUsagesIn` — also query-side — keeps its own regex and still drops a matcher containing
  an escaped quote. A re-reviewer trusting the note would confine D88's escaped-quote gap to the
  exposition side. A records-accuracy defect; the correction lives on this row.
- **Evidence:** `reviews/044-045-observability/resolution-v3.md:20`.
- **History:**
  - v4: found (F18)

### D120 — The give-up alarm's one-shot registry is per-process, so a restart re-pages every order in the 24 h→32 d window — a population D82's fix enlarged

- **What:** `MarkOnce` is a per-process `MemoryCache` keyed `sameday.awb.give-up::{orderId:N}` with a 32-day
  sliding expiration, so every restart re-fires the Error log for every order still inside `queryFloor` —
  and `docs/DEPLOYMENT.md` §12.8 says to page on that line — over the population the D82 fix widened to
  `Paid || Printing`. The `status=` value shown is whichever status the order held at the first alarm.
- **Evidence:** `BackgroundJobs/AwbGiveUpRegistry.cs:21-23`.
- **Suggested fix:** Persist the one-shot mark, or dedupe across restarts.
- **History:**
  - v4: found (F14) — pre-existing, amplified by the D82 fix

### D121 — The `or vector(0)` guards added to the SLO 4 and SLO 5 numerators are pinned by nothing — D103's class rule skips single-term sides, measured green on deletion

- **What:** The `or vector(0)` guards the round added to the SLO 4 and SLO 5 numerators were pinned by nothing: D103's class rule opens with `if (terms == 1) continue;`, and deleting the SLO 4 guard in both copies measured green.
- **History:**
  - v5: found (F1) — caused by the D105 fix, same class as D103; the hole was disclosed by the fixer in `resolution-v4.md`
  - round 5: fixed over `796a330`
  - v6: verified @`a4eb7e5` — reddened twice independently, on guard deletion in both copies and on a
    deleted panel a duplicated doc copy tried to cover; residual: the pinned list is hand-maintained (D124)

### D122 — The acceptance criterion still says SLO 4 excludes only `skipped`, and gives `retry_later`'s reason for it; `orphaned` is unmentioned

- **What:** The story file's acceptance criterion understated the exclusion set (`retry_later` missing), attached `retry_later`'s rationale to `skipped`, and never mentioned `orphaned`.
- **History:**
  - v5: found (F2) — caused by the D105 fix; the doc was amended in `b0718d8` before the second owner gate added `retry_later` in `9112aa8`, and the second amendment never landed
  - round 5: fixed @`d8a63a4` — both exclusions stated, the retry-loop rationale moved to the value it belongs to, and `orphaned` named as staying in the denominator
  - v6: verified @`a4eb7e5` — each clause checked against the shipped query and the per-attempt counter at `AwbCreator.cs:61`

### D123 — The outcome union's doc comment calls the cancelled-order case a plain skip — the one case that must now set `Orphaned: true` — and never mentions the flag

- **What:** The union's doc comment still read "`Skipped` — order no longer eligible (cancelled, AWB already exists)" — naming the one case that must now set `Orphaned: true` as the benign example, with no mention of the flag or its metric consequence.
- **History:**
  - v5: found (F3) — caused by the D105 fix
  - round 5: fixed @`3c0a13d` — on the union and in the operator log table at `DEPLOYMENT.md:771`, which
    additionally had no row at all for the `sameday.awb.orphaned` Error log; bolt-037 design docs left as
    point-in-time records
  - v6: verified @`a4eb7e5` — both texts matched against all six `Skipped(...)` sites and the `LogError` at `AwbCreator.cs:269`

### D124 — The guarded-selector list is hand-maintained, so a fifth hand-named success numerator ships unpinned and nothing notices — and the stated reason for not writing the class rule does not hold for a rule keyed on literal `=` matchers

- **What:** `GuardedSuccessSelectors` is four literal strings — today every hand-named literal-value
  selector in `slos.md` and the dashboard, both halves measured red — so a fifth success-ratio numerator
  ships unpinned and its panel reads "No Data" instead of a red 0% on a fresh process where every attempt
  fails. The disclosed reason for not writing the class rule (it would red SLO 1's `!~"5.."` and the
  error-rate panel's `=~"5.."`) does not hold for a rule keyed on a literal `=` matcher on the numerator
  side of a division — checked against every query in both files.
- **Evidence:** `Tests/Integration/DashboardMetricNamesTests.cs:29-35`; coverage measured red at review-v6 M1 and M2.
- **Suggested fix:** Write the literal-`=`-matcher rule when a fifth selector appears (the division split
  must be brace-aware and matcher detection must precede brace stripping), or accept that adding a guarded
  ratio means adding a line to the list.
- **History:**
  - v6: found (F1) — residual of the D121 fix, disclosed by the round-5 fixer
  - 2026-08-10: target closed — row carried to `reviews/backlog.md`
