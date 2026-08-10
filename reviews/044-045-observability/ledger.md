---
type: review-ledger
target: 044-045-observability
updated: 2026-08-10
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
| D46 | 🟠 | v2 (F7) | SLO 1 counts `/metrics` scrapes though its prose scopes it to site traffic | `memory-bank/operations/slos.md:35-36` query, `:27-28` prose (was `:29`) | pre-existing | **deferred** — owner parked 2026-08-05: the approved fix (exclude at the instrumentation) needs .NET 9; the two remaining routes each change what SLO 1 measures. Availability still cannot read below ~99.7%. Re-affirmed at `7e28317` by v3: defect untouched, and the file now points a reader the wrong way (D81). Re-affirmed again at `dc203c7` by v4: the query still carries no route or host filter and the instrumentation still sets no `Filter`; the document no longer misleads (D81 verified) but the figures it now states are wrong (D110). Cited lines drifted to `slos.md:8-12` (status caveat), `:30-40` (query) |
| D47 | 🟠 | v2 (F8) | `MetricCapture`'s meter filter is a no-op; the isolation its comment claims does not exist | `Tests/Helpers/MetricCapture.cs:22` | fix-caused (D9/D20) | verified (the repair's nested-capture throw has no test: D78) |
| D48 | 🟠 | v2 (F9) | The breadcrumb egress hook has no wiring test — deleting it leaves the suite green | `Configuration/SentryDataScrubbers.cs:59` | fix-caused (D2) | verified (new test is absence-only: D85) |
| D49 | 🟠 | v2 (F10) | The `LogWarning → LogError` half of the D15 fix has no test | `Middleware/ExceptionHandlerMiddleware.cs:82` | fix-caused (D15) | verified (mapped branch only — the unmapped-500 branch is still unpinned: D76) |
| D50 | 🟠 | v2 (F11) | `slos.md` says "SLOs 1–4 are measured" without SLO 3's throw-before-branch caveat | `memory-bank/operations/slos.md:3` | fix-caused (D14) | verified (the caveat landed; two other claims in the same file are still false: D80, D81) |
| D51 | 🟡 | v2 (F12) | `TracingExporterSelectionTests` boots live TracerProviders outside `ObservabilityHostCollection` | `Tests/Unit/Observability/TracingExporterSelectionTests.cs:66` | fix-caused (D13) | backlog — **extended** at `dc203c7`: the D79 fix added a second live `TracerProvider` build in the same class, and the D74 fix added an un-collectioned real-Kestrel boot in `ScrapeListenerCheckTests` (D114) |
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

Verification pass v4 (2026-08-06) revert-and-rerun tested all 11 `fixed` rows against `dc203c7`.
**All 11 flip to `verified`** — 13 revert proofs plus CI green on `ubuntu-latest`, which is the only
platform that could prove D74. 0 declined, 0 reopened. **D89, D97 and D100 close** (each was
recorded `backlog` while already fixed by the round); 54 terminal decisions re-affirmed.

| D# | Sev | First seen | Title | File | Cause | Status |
|---|---|---|---|---|---|---|
| D74 | 🔴 | v3 (F1) | Scrape guard mis-parses socket/pipe listeners off-Windows: its own test fails on `ubuntu-latest` (CI red since the fix round) and rule 2 cannot fire | `Observability/ScrapeListenerGuard.cs:23` | fix-caused (D45) | **verified** at `dc203c7` — socket/pipe classified by prefix *before* `Parse`, so the platform fork is gone; CI green on `ubuntu-latest` after 6 red runs. Rule 3 now aborts a unix-socket + TCP-metrics topology (D109) |
| D75 | 🟠 | v3 (F2) | The F4 timeout carve-out is dead code on net8.0; a Google outage racing a client abort returns 200 and reaches neither SLO 1 nor Sentry | `Services/GoogleTokenValidator.cs:42` | fix-caused (D43) | **verified** — the validator now owns the deadline and the discriminator is a flag it sets; restoring the old filter reddens exactly one test. Both invariants it rests on are unpinned (D104) |
| D76 | 🟠 | v3 (F3) | The unmapped-500 branch's log level is unpinned — D49 one branch over, on the path handling most 500s | `Middleware/ExceptionHandlerMiddleware.cs:142` | fix-caused (D49) | **verified** |
| D77 | 🟠 | v3 (F4) | Sentry honours an inbound `sentry-trace` sampling decision ahead of `TracesSampleRate` — D41's class, one layer over | `Program.cs:48` | pre-existing | **verified** at the wiring — `TracesSampler` answers on every call, which the SDK 4.13.0 source confirms is the contract that outranks an inbound `sentry-trace`. The `-0` direction is unpinned (D107) and `TracesSampleRate=0` no longer disables performance monitoring (D106) |
| D78 | 🟠 | v3 (F5) | The nested-`MetricCapture` throw, the point of the D47 repair, has no test | `Tests/Helpers/MetricCapture.cs:30` | fix-caused (D47) | **verified** |
| D79 | 🟠 | v3 (F6) | Nothing pins the production `BuildSampler` call site, and the recorded reason it cannot be pinned is refuted | `Extensions/ObservabilityExtensions.cs:71` | fix-caused (D41) | **verified** — the reflection pin reads the real `TracerProviderSdk.Sampler` and fails loudly on a rename; re-wrapping the call site reddens it |
| D80 | 🟠 | v3 (F7) | SLO 3's query contradicts its prose: correct idempotent handling and anonymous garbage both score as failures | `memory-bank/operations/slos.md:72-83` (now `:92-97`) | pre-existing | **verified** — query matches prose in both copies and `ok`/`duplicate` stay build-checked (mistyping one reddens the label test). The `or vector(0)` guard is unpinned (D103); SLO 4/5 carry the same denominator defect (D105); neither copy carries the 7-day window its heading claims (D111) |
| D81 | 🟠 | v3 (F8) | `slos.md` still asserts SLO 1 is measured and now offers it as SLO 3's cross-check — while SLO 1 is the parked, diluted one | `memory-bank/operations/slos.md:5` | fix-caused (D50) | **verified** (doc-only) — the status block names the dilution instead of claiming SLO 1 is measured, and the caveat also reached the panel operators read. The figures it states are wrong (D110) and a third copy survives at `DEPLOYMENT.md:949` (D116) |
| D82 | 🟠 | v3 (F9) | `AwbRetryJob`'s `== Paid` filter drops an order advanced past `Paid` before its AWB exists, silencing the only give-up alarm | `BackgroundJobs/AwbRetryJob.cs:109` | pre-existing | **verified** for the alarm — widened to `Paid \|\| Printing` only, and narrowing it back reddens the new test. The dispatcher leg the v3 pass could not confirm was confirmed by the fix round's approach-check. The re-enqueue boundary is unpinned (D108); the alarm re-pages after a restart over the population this widened (D120) |
| D83 | 🟠 | v3 (F10) | `metrics.md`'s "a name that nothing emits fails the build" is false; the test seeds the exposition it checks | `memory-bank/operations/metrics.md:104` | pre-existing (extends D37) | **verified** (doc-only) — step 10 now states what the test proves, names `invoice_anaf_status_total` as the live counterexample, and documents the seeding obligation, which also closes D89 |
| D84 | 🟠 | v3 (F11) | `MetricNamesIn` keeps the first-`}` truncation the D44 fix corrected in `LabelUsagesIn`, same file | `Tests/Integration/DashboardMetricNamesTests.cs:275` | fix-caused (D44) | **verified** — `StripBraceGroups` is quote-aware and its own test reddens against the old regex. `LabelUsagesIn` still keeps a separate regex (D88) |
| D85 | 🟡 | v3 (F12) | The breadcrumb test is absence-only — green with every breadcrumb dropped | `Tests/Integration/SentryOptionsWiringTests.cs:103` | fix-caused (D48) | backlog |
| D86 | 🟡 | v3 (F13) | `metrics.md`'s add-a-metric procedure never states `MetricCapture`'s execution-context requirement | `memory-bank/operations/metrics.md:99` | fix-caused (D47) | backlog |
| D87 | 🟡 | v3 (F14) | ADR-017 still says "a promoted error trace is a single root span" 19 lines below its own amendment | `memory-bank/bolts/044-tracing-and-metrics/adr-017-deterministic-trace-id-sampling.md:269` | fix-caused (D42) | backlog |
| D88 | 🟡 | v3 (F15) | Walker reach: `templating`/`annotations` queries and library panels unwalked; query-side parser mis-handles an escaped quote the exposition side handles | `Tests/Integration/DashboardMetricNamesTests.cs:107` | fix-caused (D44) | backlog — **extended** at `dc203c7`: the D84 fix made `MetricNamesIn` quote-aware but left `LabelUsagesIn`'s own regex, so the two query-side parsers now disagree with each other on an escaped quote |
| D89 | 🟡 | v3 (F16) | The label test requires every queried metric to be seeded by the test itself, undocumented | `Tests/Integration/DashboardMetricNamesTests.cs:73` | fix-caused (D44) | **closed** at `dc203c7` by the D83 fix — `metrics.md` step 10 now states the seeding obligation, which is what this row asked for (v4) |
| D90 | 🟡 | v3 (F17) | `OrderPhotoPromoter` hand-rolls `HasBeenPaid` as an ordinal comparison plus two name exclusions | `Services/OrderPhotoPromoter.cs:87` | pre-existing | backlog |
| D91 | 🟡 | v3 (F18) | `AdminOrderService` logs client-disconnect cancellations at `Error` on its highest-signal strings | `Services/AdminOrderService.cs:183` | pre-existing | backlog |
| D92 | 🟡 | v3 (F19) | The `HasBeenPaid` invariant test excludes `Cancelled` by name, pushing a future author to add a refund status to `PaidStatuses` | `Tests/Unit/Services/OrderStatusMachineTests.cs:27` | fix-caused (D40) | backlog |
| D93 | 🟡 | v3 (F20) | `Verdict` counts ports, not reachability: a loopback-only API port plus a wildcard scrape port passes both rules | `Observability/ScrapeListenerGuard.cs:21-40` (was `:36`) | fix-caused (D45) | backlog — re-affirmed at `dc203c7`: a loopback-only API port plus a wildcard scrape port still yields two ports and passes both rules |
| D94 | 🟡 | v3 (F21) | `PrometheusEndpoint` is coupled to the `Caddyfile`'s hard-coded path by comment only | `Configuration/ObservabilitySettings.cs:35` | pre-existing | backlog |
| D95 | 🟡 | v3 (F22) | `TracingWired == false` in Production warns and boots — same warn-only class as the admitted `ScrapePort == 0` | `Program.cs:370` | pre-existing | backlog |
| D96 | 🟡 | v3 (F23) | Inbound `baggage` rides out to Stripe, Sameday and Google | `Extensions/ObservabilityExtensions.cs:74` | pre-existing | backlog |
| D97 | 🟡 | v3 (F24) | Nothing exercises `StartedAsync`: not the addresses read, not the `Critical` line §14.10 tells operators to grep, not the throw | `Observability/ScrapeListenerGuard.cs:100` (was `:70`) | fix-caused (D45) | **closed** at `d1ffee7` — `A_real_host_refuses_to_start_and_names_the_reason_at_critical` boots a real Kestrel and covers all three legs; downgrading the log level reddens it (v4 measured). Was recorded `backlog` and flagged to the owner in `summary-v3` while already fixed |
| D98 | 🟡 | v3 (F25) | ADR-017's anti-salting rationale is now weaker than the ADR states — the amendment abandoned its only consumer | `memory-bank/bolts/044-tracing-and-metrics/adr-017-deterministic-trace-id-sampling.md` | fix-caused (D41) | backlog |
| D99 | ⚪ | v3 (F26) | `MetricCapture._outer` is provably always null; `Dispose`'s restore is dead code advertising forbidden nesting | `Tests/Helpers/MetricCapture.cs:37` | fix-caused (D47) | backlog |
| D100 | ⚪ | v3 (F27) | DEPLOYMENT §14.8 step 2 does not name the `ASPNETCORE_URLS` prerequisite that can now hard-fail boot | `docs/DEPLOYMENT.md:1183` | fix-caused (D45) | **closed** at `d1ffee7` — the step now says "`ASPNETCORE_URLS` must already carry `http://+:9090`". Was recorded `backlog` while already fixed (v4) |
| D101 | ⚪ | v3 (F28) | bolt-044 ddd docs declare a four-value `result` set and `ParentBasedSampler` as shipped | `memory-bank/bolts/044-tracing-and-metrics/ddd-01-domain-model.md:57` | pre-existing | backlog |
| D102 | ⚪ | v3 (F29) | `resolution-v2.md` misdescribes TestServer as reporting the addresses feature "present but empty" — it returns null | `Observability/ScrapeListenerGuard.cs:102` (was `:77`) | fix-caused (D45) | backlog — re-affirmed at `dc203c7`: the guard now keys the carve-out on an empty address list, and `?.Addresses ?? []` still turns the null TestServer feature into one, so the misdescription in `resolution-v2` stands as a record inaccuracy |

## v4 findings (D103–D120)

Minted by the [v4 verification pass](review-v4.md) at `dc203c7`. Detail per row in
[findings-v4.md](findings-v4.md). `Cause` records whether the defect was created by a v3 fix (with
the parent `D#`) or pre-dates the fix round. **11 of the 18 are fix-caused.** The dominant shape is
not a wrong behaviour but an **unpinned mechanism**: five rows are "the point of a fix, deletable
with a green suite", and five of those seven surfaces were measured rather than argued.

Verification pass v5 (2026-08-07) revert-and-rerun tested all four `fixed` rows against `52a0cb9`
(source-identical to the branch tip `d37f867`). **All four flip to `verified`** — three on local
mutation, D113 on CI, the only place it can be proven.

| D# | Sev | First seen | Title | File | Cause | Status |
|---|---|---|---|---|---|---|
| D103 | 🟠 | v4 (F1) | SLO 3's `or vector(0)` guards are pinned by nothing — deleting both leaves 1133 green, restoring the "No Data while healthy" defect this round shipped once | `memory-bank/operations/slos.md:95-97`, `ops/dashboards/fototipar-overview.json:232` | fix-caused (D80) | **verified** at `52a0cb9` (v5) — a class rule, not an instance check: deleting both guards reddens it, and so does collapsing the two-term numerator to one matcher. Its disclosed hole — single-term sides are skipped — is now D121 |
| D104 | 🟠 | v4 (F2) | Both invariants F2's discriminator rests on are unpinned: the linked deadline token, and `HttpBackstop > RequestDeadline` — breaking the latter restores D75 invisibly | `Services/GoogleTokenValidator.cs:43-50`, `Extensions/SocialAuthExtensions.cs:17` | fix-caused (D75) | **verified** at `52a0cb9` (v5) — each invariant reddens its own test: the registered timeout behind the deadline reddens the ordering test, and unwiring the deadline from `GetAsync` reddens the wall-clock test at 32 s, the mutation v4 could only measure 0-red |
| D105 | 🟠 | v4 (F3) | SLO 4 and SLO 5 put benign `skipped`/`pending` in the denominator — the defect D80 fixed — while the status block now says there are "two caveats that matter" | `memory-bank/operations/slos.md:6`, `:135`, `:158` | pre-existing; misleading enumeration fix-caused (D81) | **verified** at `52a0cb9` (v5) — both denominators exclude the benign values in both copies, the `orphaned` value reddens on revert, and the enumeration reads true again. Two records the fix left behind are D122 (acceptance criterion) and D123 (union doc comment); the guards it also added are D121 |
| D106 | 🟡 | v4 (F5) | `Sentry:TracesSampleRate=0` no longer switches performance monitoring off, only its output — `IsPerformanceMonitoringEnabled` is true whenever a sampler is set | `Program.cs:59` | fix-caused (D77) | open |
| D107 | 🟡 | v4 (F6) | The booted-host sampler test covers only `isSampled: true`; the `-0` blinding half of D77 is unpinned | `Tests/Integration/SentryOptionsWiringTests.cs:38-48` | fix-caused (D77) | open |
| D108 | 🟡 | v4 (F7) | The re-enqueue query's `Paid`-only scope — an explicit owner decision — is pinned by nothing, and the new test's second assertion cannot fail for its stated reason | `BackgroundJobs/AwbRetryJob.cs:86`, `Tests/Unit/Services/Sameday/AwbRetryJobTests.cs:244` | fix-caused (D82) | open |
| D109 | 🟡 | v4 (F8) | Rule 3 now aborts boot on a unix-socket API plus a dedicated TCP metrics port, printing a message that is false for that topology | `Observability/ScrapeListenerGuard.cs:57-63` | fix-caused (D74) | open |
| D110 | 🟡 | v4 (F9) | The dilution figures now on the operator-facing panel are wrong: 5,760/day is `/metrics` alone and the real floor is ~94.5%, not ~99.7% | `memory-bank/operations/slos.md:8-12`, `ops/dashboards/fototipar-overview.json:60` | fix-caused (D81) | **verified** at `a4eb7e5` (v6) — fixed at `9cfbf75` (round 5, owner-directed outside the v5 finding set): ~8,640/day and a ~94.5% floor, each figure naming its source; the `Tracked as D46` citation is gone too (half of D117). v6 recomputed every figure from `DEPLOYMENT.md:1049`, `Dockerfile:43` and `DEPLOYMENT.md:951`, and confirmed the premise at `ObservabilityExtensions.cs:98` (no instrumentation filter). Corrects the claim, not the dilution: D46 stays parked |
| D111 | 🟡 | v4 (F10) | SLO 3's documented query has no time window while its heading says "rolling 7 days" and its dashboard twin uses `rate(…[7d])`; SLO 4/5 the same | `memory-bank/operations/slos.md:80`, `:95-97` | pre-existing shape | open |
| D112 | 🟡 | v4 (F11) | D75's class unswept: two sibling sites still infer "our own timeout" from `!ct.IsCancellationRequested`, losing a claim release on shutdown | `Services/Sameday/AwbCreator.cs:166`, `BackgroundJobs/ShipmentTrackingJob.cs:184` | pre-existing | open |
| D113 | 🟠 | v4 (F4) | `secret-scan` fails on every pull-request run of this branch — gitleaks flags a fabricated test token `.gitleaks.toml` does not allowlist | `Tests/Unit/Configuration/SentryDataScrubbersTests.cs:16`, `.gitleaks.toml`, `.gitleaksignore` | pre-existing (`44c3e2d`) | **verified by CI only** at `52a0cb9` (v5) — the PR-event scan was red at `f0aadd7` and every earlier PR run, green from `a9c9478` (the first commit carrying `.gitleaksignore`) onward; both fingerprints checked byte-for-byte against the commits they name. Not provable locally: gitleaks is not installed here, and any history rewrite of this branch invalidates commit-pinned fingerprints silently |
| D114 | 🟡 | v4 (F12) | The new real-Kestrel boot test runs un-collectioned in the parallel pool and installs a process-wide console-exporting `TracerProvider` under `ASPNETCORE_ENVIRONMENT=Development` | `Tests/Unit/Observability/ScrapeListenerCheckTests.cs:94-120` | fix-caused (D74), extends D51 | open |
| D115 | 🟡 | v4 (F13) | `system-architecture.md` still describes the old 5 s `HttpClient` timeout — the standard CLAUDE.md routes readers to, unchanged by the fix that moved the bound | `memory-bank/standards/system-architecture.md:45` | fix-caused (D75) | open |
| D116 | ⚪ | v4 (F15) | `DEPLOYMENT.md:949` still reasons from the availability target as if the denominator were customer traffic — the third copy D81's fix left behind | `docs/DEPLOYMENT.md:949` | fix-caused (incomplete D81) | open |
| D117 | ⚪ | v4 (F16) | The Availability panel `description` and the `status=` give-up log field are both unpinned; the description cites "D46", an id operators cannot resolve | `ops/dashboards/fototipar-overview.json:60`, `BackgroundJobs/AwbRetryJob.cs:123` | fix-caused (D81/D82) | open |
| D118 | ⚪ | v4 (F17) | Comment-rule residue: two two-line narrating comments and a stray double blank line | `Program.cs:57-61`, `BackgroundJobs/AwbRetryJob.cs:105-106` | fix-caused (D77/D82) | open |
| D119 | ⚪ | v4 (F18) | `resolution-v3.md`'s F11 note overstates the parser unification — three parsers exist and `LabelUsagesIn` keeps its own regex | `reviews/044-045-observability/resolution-v3.md:20` | records accuracy | open |
| D120 | 🟡 | v4 (F14) | The give-up alarm's one-shot registry is per-process, so a restart re-pages every order in the 24 h→32 d window — a population D82's fix enlarged | `BackgroundJobs/AwbGiveUpRegistry.cs:21-23` | pre-existing, amplified (D82) | open |

## v5 findings (D121–D123)

Minted by the [v5 verification pass](review-v5.md) at `52a0cb9`. Detail per row in
[findings-v5.md](findings-v5.md). **All three are fix-caused by D105**, and all three are one shape:
the round added a mechanism and left one of its records — a test, an acceptance criterion, a type
comment — describing the world before it. None changes today's behaviour; each hides the next
author's mistake.

**Round 5 (2026-08-07) fixed all three rather than backlogging them**, on the owner's instruction to
let the loop end naturally, and cleared **D110** in the same round by owner request; see
[resolution-v5.md](resolution-v5.md). **All four flip to `verified` in the
[v6 verification pass](review-v6.md)** at `a4eb7e5` — D121 by measurement (two mutations, both
predicted red and measured red), D110/D122/D123 by reading each prose claim against the code it
describes.

| D# | Sev | First seen | Title | File | Cause | Status |
|---|---|---|---|---|---|---|
| D121 | 🟡 | v5 (F1) | The `or vector(0)` guards added to the SLO 4 and SLO 5 numerators are pinned by nothing — D103's class rule skips single-term sides, measured green on deletion | `memory-bank/operations/slos.md:142`, `:173`, `ops/dashboards/fototipar-overview.json:271`, `:310`, `Tests/Integration/DashboardMetricNamesTests.cs:133` | fix-caused (D105), same class as D103 | **verified** at `a4eb7e5` (v6) — fixed in round 5 over `796a330`; the v6 pass reddened it twice independently, on guard deletion in both copies and on a deleted panel a duplicated doc copy tried to cover. Residual: the pinned list is hand-maintained (D124) |
| D122 | 🟡 | v5 (F2) | The acceptance criterion still says SLO 4 excludes only `skipped`, and gives `retry_later`'s reason for it; `orphaned` is unmentioned | `memory-bank/intents/020-observability-stack/units/002-error-tracking-and-slos/stories/002-slo-documentation-and-dashboard.md:27-29` | fix-caused (D105) | **verified** at `a4eb7e5` (v6) — fixed at `d8a63a4`; both exclusions stated, the retry-loop rationale moved to the value it belongs to, and `orphaned` named as staying in the denominator. v6 checked each clause against the shipped query and against the per-attempt counter at `AwbCreator.cs:61` |
| D123 | 🟡 | v5 (F3) | The outcome union's doc comment calls the cancelled-order case a plain skip — the one case that must now set `Orphaned: true` — and never mentions the flag | `Services/Sameday/AwbCreationOutcome.cs:9` | fix-caused (D105) | **verified** at `a4eb7e5` (v6) — fixed at `3c0a13d` on the union AND in the operator log table at `DEPLOYMENT.md:771`, which additionally had no row at all for the `sameday.awb.orphaned` Error log; bolt-037 design docs left as point-in-time records. v6 matched both texts against all six `Skipped(...)` sites and the `LogError` at `AwbCreator.cs:269` |

## v6 findings (D124)

Minted by the [v6 verification pass](review-v6.md) at `a4eb7e5`. Detail in
[findings-v6.md](findings-v6.md). One row, ⚪, the residual the round-5 fixer disclosed.

| D# | Sev | First seen | Title | File | Cause | Status |
|---|---|---|---|---|---|---|
| D124 | ⚪ | v6 (F1) | The guarded-selector list is hand-maintained, so a fifth hand-named success numerator ships unpinned and nothing notices — and the stated reason for not writing the class rule does not hold for a rule keyed on literal `=` matchers | `Tests/Integration/DashboardMetricNamesTests.cs:29-35` | fix-residual of D121 | backlog |

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
