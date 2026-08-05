---
type: findings
target: 044-045-observability
version: 2
answers: review-v2.md
commit: e965c99
date: 2026-08-05
---

# Findings v2 — 044-045-observability (verification pass)

Per-finding detail for [review-v2.md](review-v2.md). Part 1 is the revert-and-rerun ledger for the
23 fixes; Part 2 is the 34 new findings the per-cluster questions produced.

## Part 1 — revert-and-rerun evidence

Method: for each finding, put the production source back to its pre-fix behaviour, predict the
failing set, run the scoped filter, compare, restore, confirm the tree clean. Baseline before and
after all 26 mutations: **1081 passed / 0 failed / 10 skipped**.

| v1 F# | Source reverted | Went red | Verdict |
|---|---|---|---|
| F1 | scrape-port gate deleted from `MetricsEndpointIpAllowListMiddleware.InvokeAsync` | 4 — `Scrape_port_configured_makes_metrics_absent_on_the_public_listener`, `Request_on_a_listener_other_than_the_scrape_port_is_404_even_from_an_allowed_ip`, `Wrong_port_wins_over_the_allow_list_so_a_denied_ip_also_sees_404`, `A_wrong_listener_request_logs_once_so_the_404_is_diagnosable` | held, zero collateral |
| F2 | `SetBeforeSendTransaction` removed from `Register` | 2 — `The_booted_host_scrubs_pii_before_the_sdk_sends_a_transaction`, `Register_scrubs_both_events_and_transactions_before_they_leave_the_sdk` | held |
| F3 | `request.QueryString` / `request.Url` scrubbing removed | 10 — incl. `Scrub_redacts_query_string_values_and_keeps_parameter_names`, `Scrub_strips_query_fragment_and_credentials_from_the_url`, both booted-host envelope tests | held |
| F4 | allow-list header matching replaced by the pre-fix case-sensitive deny-list | 9 — incl. all three HTTP/2 lowercase cases (`x-guest-token`, `cookie`, `authorization`) and `Referer` | held; reproduces the finding's exact scenario |
| F5a | boot abort on `Observability:Sampling:Routes` removed | 1 — `A_leftover_per_route_rate_aborts_boot_instead_of_being_ignored` | held |
| F5b | sampler made steerable by activity name | `Nothing_about_the_span_identity_can_steer_the_rate` (+4 pipeline tests as collateral of a crude mutation) | held |
| F6a | out-of-rate server spans `Drop` instead of `RecordOnly` | 5 — incl. `An_errored_span_is_exported_at_a_rate_that_keeps_nothing`, `Rate_zero_records_without_exporting_so_errors_stay_rescuable` | held |
| F6b | `ErrorOverrideProcessor.OnEnd` body removed | 3 — incl. `An_errored_unrecorded_span_is_promoted_and_marked` | held |
| F7 | `WebhooksController.cs` restored to `5cac465` | 6 of 8 `WebhooksControllerMetricsTests` (the 2 that stay green cover branches that already recorded pre-fix) | held |
| F8a | `o.SendDefaultPii = false` deleted | 1 — `The_booted_host_keeps_send_default_pii_off_even_when_configuration_asks_for_it` | held; matches the resolution's claim exactly |
| F8b | `SentryDataScrubbers.Register(o)` deleted | 2 — both `The_booted_host_scrubs_pii_before_the_sdk_sends_*` | held; matches exactly |
| F9a | `OrdersCreated.Add` and `UploadSize.Record` disabled at their call sites | 3 — `CreateFromCartAsync_RecordsOrdersCreatedWithTheRequestedProcessor`, `CreateFromCartAsync_IdempotentReplay_DoesNotDoubleCountOrdersCreated`, `UploadAsync_RecordsTheStoredByteCountOnUploadSizeBytes` | held |
| F9b | `m.AddMeter(MetricNames.Meter)` commented out | 3 — `A_business_metric_reaches_the_exposition` + both `DashboardMetricNamesTests` | held |
| F10 | `ScrapeIpAllowList.Parse` made to drop bad entries silently | 13 — incl. `An_unparseable_allow_list_entry_aborts_boot` and every `Enabled_with_an_unparseable_allowed_scrape_ip_fails` case | held |
| F11 | `AddSingleton` → `AddScoped` | 3 `MetricsScrapeGateRegistrationTests` | held |
| F12 | IPv4-mapped canonicalization removed from `Canonicalize` | 2 — `Ipv4_mapped_ipv6_peer_matches_an_ipv4_allow_list_entry`, `Cidr_entry_admits_an_ipv4_mapped_ipv6_peer_in_range` | held |
| F13 | `TracingWired` forced to `true` | 3 `Without_an_otlp_endpoint_no_trace_pipeline_is_built_outside_development` cases | held |
| F14a | dashboard metric name → `http_request_total` | 1 — `Every_dashboard_query_names_a_metric_the_api_actually_exposes` | held |
| F14b | `slos.md` metric name → `http_request_total` | 1 — `Every_slo_query_names_a_metric_the_api_actually_exposes` | held |
| F15 | `CaptureException` on mapped 5xx removed | 1 — `A_mapped_502_is_captured_to_sentry` | held (capture half only — see v2 F10) |
| F16 | the try/catch recording `awb_creation_total{result=error}` removed | 1 — `A_thrown_db_failure_records_an_error_outcome_and_rethrows` | held |
| F17 | duration recorded before `SaveChangesAsync` again | 1 — `UpdateStatusAsync_Shipped_RecordsNoDurationWhenTheCommitFails` | **partial — see below** |
| F18 | `o.Debug = true` + level ternary → `o.Debug = sentryConfig.Debug` | 1 — `The_booted_host_reports_sdk_failures_even_with_sentry_debug_off`. `Sentry_debug_on_lowers_the_diagnostic_level_rather_than_switching_logging_on` stayed **green** | held; companion test does not discriminate (see v2 F16 for the related inertness) |
| F19 | a static ctor setting `Sentry__Enabled` / `Sentry__Dsn` put back | 2 — both `TestHostConfigurationIsolationTests` | held |
| F20 | `{ "user_id", order.UserId?.ToString() }` added to `OrderService`'s `TagList` | 1 — `CreateFromCartAsync_RecordsOrdersCreatedWithTheRequestedProcessor` via `ContractViolations()` | held; the review's exact mutation |
| F21 | `ClaimTypes.NameIdentifier` → a claim that never exists | 1 — `Authenticated_request_stamps_correlation_id_and_user_id_on_the_scope` | held |
| F22 | none — the finding is about test shape | its two SDK-shape tests reddened under the F3 and F4 source mutations | held indirectly, soundly |
| F23 | none — documentation | `docs/DEPLOYMENT.md` §14.1–14.12 present (lines 971–1237); §14.5's boot-abort promise pinned by the test verified under F10 | held |

### F17 / D17 — why this pass declines to verify

The finding named two legs. The record-after-commit leg is fixed and revert-proven. The
concurrent-double-click leg is not: `AdminOrderService.UpdateStatusAsync` still reads, mutates and
saves with no conditional write and no once-only guard, so two simultaneous `PATCH`es that both
pass `OrderStatusMachine.Transition` can both commit and both `Record` on a cumulative histogram.
There is no test for it. The resolution asked the re-review to decide; the decision is that the
finding is not closed.

### Deferred rows — evidence for the re-affirmations

Unchanged since `5cac465`, so the deferral stands with no further check: **D29**
(`SentryScopeEnricherMiddleware.cs`), **D33** (`OrderService.cs`), **D34** (`FotoMetrics.cs`),
**D36** (`ddd-02-technical-design.md`).

Files that changed, re-read by hand at the tip:

- **D24 stands** — `app.UseSentryScopeEnricher()` is `Program.cs:357`, after `UseAuthentication()` (350).
- **D25 stands** — `SetDbStatementForText = true` at `ObservabilityExtensions.cs:70`.
- **D26 stands** — the validator tests `Sampling.Default is < 0.0 or > 1.0`; `NaN` satisfies neither comparison.
- **D27 stands** — the validator only checks `StartsWith('/')`, so `"/"` still passes.
- **D28 narrowed** — see review-v2.
- **D30 changed shape** — see review-v2.
- **D31 narrowed** — see review-v2.
- **D32 stands** — `SentryIntegrationFactory` still exposes a bare `List` / `Dictionary`.
- **D35 stands, line shifted** — the dangling `/` and a surviving bolt-042 citation are now at
  `Program.cs:144` (`story 003 AC#1 /`).
- **D37 stands** — `invoice_anaf_status_total` still has no production call site.
- **D38 stands** — `Program.cs:77-79` still re-reads `Metrics:PrometheusEndpoint` by string key with
  a duplicated `?? "/metrics"`.
- **D39 stands** — Sentry wiring still inlined in `Program.cs` with fully-qualified names.

## Part 2 — new findings

### 🟠 F1 → D40 — a redelivered success webhook pages ops for a healthy order

`src/PhotoPrint.API/Controllers/WebhooksController.cs:287` (Stripe), `:223` (EuPlatesc).
**Fix-caused, from the D7 fix.**

The duplicate guard at `:264` matches `OrderStatus.Paid` only. `OrderStatusMachine` allows
`Paid → Printing → Shipped → Delivered` ([OrderStatusMachine.cs:22-26]), so an order that has been
paid and moved on is in none of the handled states and falls into the new `else`, which logs
`LogError` *"customer charged, order not Paid, manual reconciliation required"* and records
`payment_webhook_total{result="failed"}`.

**Scenario.** `HandleStripePaymentSucceededAsync` records `ok` at `:276`, then
`BroadcastNewOrderAsync` (`:278`) or `FireOrderConfirmedEmailAsync` (`:279`, which hits the DB)
throws → 500 → Stripe redelivers. An admin has meanwhile advanced the order to `Printing`. The
redelivery scores a second increment, this one `failed`, against the 99.9% SLO 3 target, and raises
a reconciliation alert for an order that is fine. Any Stripe redelivery after an admin advances the
order has the same effect. Identical shape on the EuPlatesc side (`:210` guard vs `:223` else).

**Fix.** Treat "at or past `Paid`" as the duplicate case. The state machine already encodes the
ordering.

### 🟠 F2 → D41 — the caller decides the sampling rate

`src/PhotoPrint.API/Extensions/ObservabilityExtensions.cs:66`. **Pre-existing — present at
`5cac465`, missed by v1.**

`new ParentBasedSampler(root)` uses the one-argument constructor, whose remote-parent arms are
`AlwaysOnSampler` and `AlwaysOffSampler`. For any request arriving with a `traceparent` header the
inner `DeterministicTraceIdSampler` is never consulted at all. Caddy forwards the header unchanged
(`Caddyfile:23`, no `header_up -traceparent`).

**Scenario.** `curl -H 'traceparent: 00-<32hex>-<16hex>-00' https://…` → `AlwaysOffSampler` → `Drop`
→ the activity is never `IsAllDataRequested` → `ErrorOverrideProcessor.OnEnd` never runs → a 500 on
that request is exported at **no** rate, including `Default = 1.0`. That falsifies ADR-017's
"errors are always sampled" amendment and DEPLOYMENT.md:1129's "`0.0` exports errored spans only".
The `-01` variant is the mirror: an anonymous caller forces `AlwaysOn` for the whole trace — server
span plus EF children carrying `db.statement` — past whatever rate is configured, which is the cost
lever §14.7 sells.

Every case in `DeterministicTraceIdSamplerTests` and all five in `SamplingPipelineTests` use root
spans; nothing passes a non-default `parentContext`.

**Fix.** A design decision, not a patch — trusting an inbound `traceparent` is right inside a
trusted mesh and wrong at a public edge. Either way, `ParentBasedSampler`'s four-arm constructor is
where it is expressed.

### 🟠 F3 → D42 — lowering the sample rate saves much less than documented

`src/PhotoPrint.API/Observability/Sampling/DeterministicTraceIdSampler.cs:42`.
**Fix-caused, from the D6 fix.**

`SamplingDecision.RecordOnly` maps to `ActivitySamplingResult.AllData`, which sets
`IsAllDataRequested = true` — the exact flag `OpenTelemetry.Instrumentation.AspNetCore`'s
`HttpInListener` gates all its `OnStart`/`OnStop` work on. So the tag writes, the boxed port and
status-code values, the `DisplayName` rewrite, status resolution, the `RecordException = true` path
and the composite-processor walk now run on **every** out-of-rate request, where the old `Drop`
skipped them.

**Scenario.** An operator follows DEPLOYMENT.md §14.8 and lowers `Sampling:Default` to `0.1` to cut
tracing overhead. They keep essentially all the per-request span cost and save only child spans and
egress. The cost is bounded per request — nothing accumulates, `BatchActivityExportProcessor.OnEnd`
returns on `!Recorded` — but it is stated nowhere: ADR-017 says only "one root span per out-of-rate
request … memory does not grow", and §14.7 talks only about export volume. There is no switch that
restores `Drop` short of `Observability:Enabled=false`, which also kills metrics.

### 🟠 F4 → D43 — closing the tab mid Google sign-in creates a Sentry issue

`src/PhotoPrint.API/Services/GoogleTokenValidator.cs:40-43` with
`src/PhotoPrint.API/Middleware/ExceptionHandlerMiddleware.cs:80,135`. **Fix-caused, from the D15 fix.**

The app's only production `BadGatewayException` is thrown from a `catch (… TaskCanceledException)`
on `client.GetAsync(…, ct)`, where `ct` is the controller's cancellation token — i.e.
`HttpContext.RequestAborted`. A client abort and an HttpClient timeout funnel into the same catch
and are indistinguishable.

**Scenario.** A user on a phone loses signal during Google sign-in. The request aborts,
`GetAsync` throws `TaskCanceledException`, `GoogleTokenValidator` translates it to
`BadGatewayException`, and the new `mapping.StatusCode >= 500` branch logs it at `Error` **and**
captures it to Sentry. `ExceptionHandlerMiddleware`'s own client-abort guard at `:53-64` — which
exists to keep exactly this out of the error channel — cannot fire, because the cancellation was
already translated into a different exception type upstream.
`GoogleTokenValidatorTests.ValidateAsync_TaskCanceledException_ThrowsBadGatewayException` asserts
only the exception type.

### 🟠 F5 → D44 — the metric-name test checks no labels, but the docs promise it does

`src/PhotoPrint.Tests/Integration/DashboardMetricNamesTests.cs:144`. **Fix-caused, from the D14 fix.**

`MetricNamesIn` opens with `Regex.Replace(expr, "\\{[^}]*\\}", " ")`, stripping every label matcher
before identifiers are read. Only metric family names are ever checked. Meanwhile `slos.md:6-7` and
the dashboard's `description` now assert *"A test holds every query below against a real `/metrics`
exposition, so a rename that breaks a panel fails the build."*

**Scenario.** A one-line edit to `MetricNames.cs:30` (`Result = "result"`) or `:53` (`Ok = "ok"`)
leaves panels 7 and 8 and SLOs 3/4/5 permanently empty with a green build. So would an OTel semconv
rename of `http.response.status_code`, which five of eight panels filter on. This is the same
over-promise class the D14 fix was written to remove.

### 🟠 F6 → D45 — nothing checks the scrape port against a bound listener

`src/PhotoPrint.API/Program.cs:378` with `src/PhotoPrint.API/Validators/ObservabilitySettingsValidator.cs:43`.
**Fix-caused, from the D1 fix.**

The only boot signal about `ScrapePort` fires when it is `0`; the validator only range-checks
0–65535. Nothing verifies the port names a listener Kestrel actually bound. The shipped
`docker-compose.prod.yml` binds both `8080` and `9090` and the docs warn to keep them in step, but
that pairing is a documentation-only invariant.

**Two scenarios, opposite directions.** (a) A deployment that is not the shipped compose — a
Kubernetes Deployment, a systemd unit, anything setting `ASPNETCORE_URLS=http://+:8080` alone —
takes `ScrapePort=9090` from the §14.11 table. Every scrape hits the wrong-listener branch,
Prometheus sits DOWN forever, dashboards stay dark, and the only clue is one
`metrics.scrape.wrong_listener` **Information** line per peer in the production rolling file. (b) On
a single-listener host an operator sets `ScrapePort=8080` so scraping works at all; the port gate
then passes for every proxied request and D1's peer-is-the-proxy problem is silently back — again
with no boot warning. `IServerAddressesFeature` at `ApplicationStarted` makes both checkable, and no
test boots a real two-listener Kestrel.

### 🟠 F7 → D46 — SLO 1 counts its own scrapes

`memory-bank/operations/slos.md:29-30`, `ops/dashboards/fototipar-overview.json:54,127,165`.
**Pre-existing; the query was rewritten by the D14 fix without adding a filter.**

SLO 1's prose defines it as *"the share of HTTP requests to `*.fototipar.ro`"*, but the query is
`sum(rate(http_server_request_duration_seconds_count{http_response_status_code!~"5.."}[30d])) / sum(rate(…[30d]))`
with no route or host filter, and `AddAspNetCoreInstrumentation()` at
`ObservabilityExtensions.cs:93` sets no `Filter`.

**Scenario.** `docs/DEPLOYMENT.md:1048` ships `scrape_interval: 15s` — 5,760 always-200 `/metrics`
requests a day against the "few hundred req/day" §13.9 assumes. A day on which *every real request*
5xx'd moves the 30-day panel from 1.000 to ≈0.997, still inside the yellow band, instead of ≈0.967.
The p50/p95/p99 latency panels and the RPS panel are diluted the same way.

### 🟠 F8 → D47 — the metric capture helper's isolation filter does nothing

`src/PhotoPrint.Tests/Helpers/MetricCapture.cs:22-28`. **Fix-caused, from the D9/D20 fixes.**

The constructor filters with `ReferenceEquals(instrument.Meter, meter)` where `meter` is
`FotoMetrics.Meter` — the single process-wide static every instrument in the app is created on
(`FotoMetrics.cs:27`). The predicate is true for every emission in the process, so it excludes
nothing, and the comment above it — *"xUnit runs test classes in parallel, so match the meter
instance too"* — describes isolation that does not exist. Six test files rely on
`ContainSingle`-shaped assertions over a shared listener.

**Impact is unproven.** Five consecutive runs of the colliding sets
(`DashboardMetricNamesTests`, which emits one observation on all six instruments, together with
`UploadServiceTests`, `AwbCreatorTests`, `OrderServiceTests`, `AdminOrderServiceTests`,
`WebhooksControllerMetricsTests` and `MetricsEndpointIntegrationTests`) returned 133/133 green every
time. Contamination requires another test to emit inside a live capture's millisecond window.
Recorded on the confirmed mechanism, not on a predicted failure rate.

**Fix.** Give each test its own `Meter` or capture token; filtering on `FotoMetrics.Meter` can never work.

### 🟠 F9 → D48 — the breadcrumb hook's wiring is unproven

`src/PhotoPrint.API/Configuration/SentryDataScrubbers.cs:59`. **Fix-caused, from the D2 fix.**

Deleting `options.SetBeforeBreadcrumb(…)` leaves the suite green — **measured**: 358 passed / 0
failed across `Integration` + `Unit.Configuration` + `Unit.Middleware`. The two envelope-level
wiring tests push only an event and a transaction; the three breadcrumb tests call
`Scrub(Breadcrumb)` directly and so prove the function, never the wiring.

**Scenario.** This is a live path. `AddHttpClient` is used for the Google token check
(`SocialAuthExtensions.cs:14`) and for Sameday, and Sentry auto-attaches
`SentryHttpMessageHandler` via `SentryHttpMessageHandlerBuilderFilter`, which adds a breadcrumb
carrying the request URL verbatim. If line 59 is ever dropped in a refactor,
`https://oauth2.googleapis.com/tokeninfo?id_token=<live token>` ships to Sentry on every error
event and nothing turns red — which is exactly the D2 defect this cluster fixed for transactions.

### 🟠 F10 → D49 — the LogError half of the D15 fix has no test

`src/PhotoPrint.API/Middleware/ExceptionHandlerMiddleware.cs:82-89`. **Fix-caused, from the D15 fix.**

**Measured**: reverting `LogError` to `LogWarning` on the mapped-5xx branch leaves 24 passed / 0
failed across `ExceptionHandlerMiddleware`, `MappedServerErrorSentryTests`, `SentryIntegrationTests`
and `SentryOptionsWiringTests`. `MappedServerErrorSentryTests` asserts capture only, and
`ExceptionHandlerMiddlewareTests` verifies `LogLevel.Warning` only for the 4xx reserved events.

**Scenario.** A refactor restores the single `LogWarning` for all mapped exceptions. Nothing fails,
and `docs/DEPLOYMENT.md` §13.8's last alert row plus §13.1 — both of which tell operators to
reconcile Error-level logs against Sentry issues — quietly stop working.

### 🟠 F11 → D50 — "SLOs 1–4 are measured" hides SLO 3's hole

`memory-bank/operations/slos.md:3-5`. **Fix-caused, from the D14 fix.**

The new status block states *"SLOs 1–4 are measured"* with no caveat. SLO 3's counter is incremented
only inside a terminal decision branch of the handler (`WebhooksController.cs:332-341`), a hole
`memory-bank/operations/metrics.md:25` documents and `slos.md` does not.

**Scenario.** Postgres is down. Every Stripe webhook throws before reaching any branch, returns 500,
and increments nothing. `payment_webhook_total{result="ok"} / payment_webhook_total` reads 100%
healthy while customers are charged and their orders stay in `AwaitingPayment` — verbatim the
failure SLO 3 exists to catch. See also **F23/D62**, the mechanism.

### 🟡 Low

- **F12 → D51** `Tests/Unit/Observability/TracingExporterSelectionTests.cs:66` — the class boots the
  real `AddObservability` with `Enabled=true` and resolves a live `TracerProvider`, but is not in
  `ObservabilityHostCollection`, whose comment asserts no two such providers are ever alive at once.
  `Development_keeps_the_console_fallback` builds ASP.NET + EF instrumentation with
  `SetDbStatementForText` and a console exporter at rate 1.0, so while it lives it prints every
  parallel integration test's request and raw EF SQL to stdout.
  `An_otlp_endpoint_wires_tracing_in_any_environment` additionally starts a batch OTLP exporter
  aimed at `http://collector:4317`. Fix-caused (D13).
- **F13 → D52** `Controllers/WebhooksController.cs:329` — `payment_failed` records `failed`
  unconditionally, including for orders already `Paid` or already `PaymentFailed`, where the sibling
  success handler uses `duplicate` for the equivalent repeat. A card declined then approved on the
  same PaymentIntent produces both events with no ordering guarantee. The new test asserts
  `HaveCount(1)` and never the label value. Fix-caused (D7).
- **F14 → D53** `Observability/ScrapeIpAllowList.cs:101` — `MaskedForm` still proposes a form the
  parser rejects: `::ffff:10.42.0.5/112` fails `IPNetwork.TryParse`, the suggestion says write
  `::ffff:10.42.0.0/112`, and line 36 then refuses exactly that with "write it as an IPv4 range".
  Two boot-failure cycles for one typo — the same class as the octal suggestion fixed in `a054fdd`.
  Fix-caused (D10).
- **F15 → D54** `Tests/Integration/MappedServerErrorSentryTests.cs` —
  `A_mapped_404_is_not_captured_to_sentry` asserts only "status is 404" and "no event mentioning the
  marker", both of which an *unrouted* request satisfies. Delete `/__test/throw-mapped-404` and it
  stays green while proving nothing about suppression. Fix-caused (D15).
- **F16 → D55** `docs/DEPLOYMENT.md:873,885` — the documented `Sentry__Debug` verbosity knob cannot
  produce output: Serilog's `MinimumLevel.Default` is `Information` in both `appsettings.json:141`
  and `appsettings.Development.json`, so every `SentryLevel.Debug` line the flag unlocks maps to MEL
  `Debug` and is dropped before any sink. An operator diagnosing wiring turns it on, restarts, and
  sees byte-identical logs. The half that matters — Warning-and-above SDK diagnostics reaching the
  file sink — does work. Fix-caused (D18).
- **F17 → D56** `Middleware/ExceptionHandlerMiddleware.cs:135` — no volume ceiling on the new capture
  site: no dedup window, no per-issue cap, `Sentry:SampleRate` defaults to `1.0`. A Google
  `tokeninfo` outage emits one event per sign-in attempt against the 5k errors/month free tier §13.9
  sized on "a handful per day"; once the quota 429s the SDK drops every event including unhandled
  500s, and by accepted decision nothing counts drops. Adjacent to that accepted decision, recorded
  because F4/D43 makes the trigger far more likely. Fix-caused (D15).
- **F18 → D57** `Tests/Integration/DashboardMetricNamesTests.cs:115` — the extractor reads only
  `panels[*].targets`; Grafana row panels nest children under `panels[i].panels`, so grouping the
  dashboard into rows silently drops every nested query while the non-empty guard still passes.
  Fix-caused (D14).
- **F19 → D58** `docs/DEPLOYMENT.md:961` — §13.10 was not swept with §13.1/13.4/13.8 and still says
  *"A panel still reading 'No Data' once Prometheus shows the target UP is a metric-name mismatch,
  not a missing feature"*, contradicting the accepted decision that panel 8 (ANAF) reads No Data
  permanently. Fix-caused (D14).
- **F20 → D59** `Services/Sameday/AwbCreator.cs:50` — the shutdown carve-out matches only
  `OperationCanceledException`, and both new tests run on SQLite while production is Postgres. If
  Npgsql surfaces a cancelled command as `PostgresException` 57014 or `NpgsqlException` rather than
  an OCE, the catch-all at `:54` records `error` and every deploy with in-flight AWB jobs depresses
  SLO 4 — exactly what the carve-out exists to prevent. This is the first finding to land squarely
  in the still-owed `db-parity` lens. Fix-caused (D16).
- **F21 → D60** `Tests/Helpers/CapturingSentryTransport.cs:12,18` — `Payloads` is a plain
  `List<string>` appended from Sentry's background worker thread and read from the test thread. Safe
  today only because the worker is single-threaded and `FlushAsync` supplies the barrier. Fix-caused (D8).
- **F22 → D61** `Middleware/MetricsEndpointIpAllowListMiddleware.cs:19` — `wrong_listener` and
  `not_allowed` denials share one 512-entry log budget, so a scan against the wrong listener can
  exhaust the budget for real allow-list denials. Fix-caused (D11).
- **F23 → D62** `Controllers/WebhooksController.cs:119,123` — a throw escaping either endpoint
  records no `payment_webhook_total` at all, so during a database outage `ok/total` freezes near 100%
  instead of dropping and no burn-rate alert on SLO 3 can fire. This is the same defect class the fix
  closed in `AwbCreator.cs:54-59` with `result=error`, resolved the opposite way here. Deliberate and
  documented in `metrics.md`, but the two handlers of one class now disagree, and `slos.md` does not
  carry the caveat (**F11/D50**). Pre-existing.
- **F24 → D63** `Configuration/SentryDataScrubbers.cs:12` — `Idempotency-Key` is not on
  `AllowedHeaders`, so it is now `<scrubbed>` where it previously reached Sentry intact. An
  `IdempotencyConflictException` surfacing for a duplicate-payment report loses the one field
  identifying which key collided, and it is not logged anywhere else. It is a client-generated
  opaque token, not PII — the allow-list can take it. Fix-caused (D2/D3/D4).
- **F25 → D64** `Configuration/SentryDataScrubbers.cs:333` — the fail-closed drop is never exercised
  through the hook: the only failure-mode test passes `null!` straight to `Scrub`, so nothing pins
  "hook returns null ⇒ no envelope on the wire", the SDK behaviour the whole design rests on. A
  scrubber that starts throwing after a data-shape change deletes all error telemetry with one
  Serilog `Error` per event and no metric behind it. Fix-caused (D2).

### ⚪ Cleanup

- **F26 → D65** `Observability/ScrapeIpAllowList.cs:30` — the empty-entry failure names neither value
  nor index, unlike every other message. §14.5 tells operators indexed env vars merge with the
  default list, so the natural way to shed the inherited `::1` is
  `Observability__Metrics__AllowedScrapeIps__1=`, which aborts boot with a message identifying nothing.
- **F27 → D66** `Configuration/SentryDataScrubbers.cs:117` — `Scrub(Breadcrumb)` loses the original
  `Timestamp` (the preserving constructor is internal in 4.13). Sub-millisecond and harmless under
  the hook; only bites if the public method is reused on stored breadcrumbs.
- **F28 → D67** `memory-bank/bolts/045-error-tracking-and-slos/implementation-walkthrough.md:39,46` —
  commit `44c3e2d` updated only line 81; lines 39 and 46 still describe the deleted
  `SetBeforeSend`-only, sensitive-substring deny-list. The file contradicts itself 40 lines apart.
- **F29 → D68** `Tests/Unit/Observability/MetricsCardinalityTests.cs:43` — adding one legitimate label
  value fails with a count mismatch that never names `DeclaredInstruments()` as the place to bump it.
- **F30 → D69** `Tests/Helpers/LogCapture.cs:33,54` — `CreateLogger` discards `categoryName` and
  `LogRecord` keeps only level and formatted text, so a test attached to a whole `ILoggerFactory` can
  distinguish sources only by string prefix and cannot assert an exception rode along.
- **F31 → D70** `Tests/Helpers/MetricCapture.cs:48` — no test proves `ContractViolations()` ever
  returns non-empty, though it is now the cardinality guard in six files and `metrics.md` step 7
  mandates it for every new instrument.
- **F32 → D71** `Observability/Sampling/DeterministicTraceIdSampler.cs:41` — "Background roots stay
  dropped: their EF spans carry SQL text" reads as unconditional, but line 26 short-circuits to
  `RecordAndSample` before the kind check, so at the shipped default `Default = 1.0` every background
  EF root is exported with `db.statement`. The restriction is only ever tested at `0.0`.
- **F33 → D72** `Extensions/ObservabilityExtensions.cs:42` — the stale-`Routes` boot abort sits below
  the `Enabled` early return, so DEPLOYMENT.md §14.8 step 1 ("deploy with `Enabled=false`, confirm
  the API boots clean") passes with the dead key still in place; the abort only lands at step 2.
- **F34 → D73** `Observability/ErrorOverrideProcessor.cs:17` — promotion emits no in-app signal. The
  tag is visible only on a span that already reached the collector, and no counter exists, so
  "promotion silently stopped" and "no errors happened" are indistinguishable — including for the
  accepted-untested processor-order gap and for F2/D41's remote-parent path.

## What the lenses cleared

Recorded so a later pass does not re-derive it:

- **No Sentry egress bypass exists.** Attachments, user feedback and sessions never occur
  (`AutoSessionTracking` defaults to false, verified by reflection; no `AddAttachment` /
  `CaptureUserFeedback` anywhere). Client reports and envelope headers carry no request data. Event
  processors run before `BeforeSend`, so `ServerName`, `Modules` and contexts are all covered. The
  drop path cannot recurse — Serilog has no Sentry sink and swallows sink faults.
- **No webhook double-increment.** Every return in both endpoints and every exit of
  `CreateForOrderAsync` records exactly one measurement.
- **The EuPlatesc IPN response is unchanged** by the new `else` (it still falls to the same
  `BuildIpnResponse`), the payment-failed rewrite keeps the identical `AwaitingPayment` transition
  guard, and `AwbCreator`'s try/catch wraps the internal call from outside, leaving claim-release and
  dispatcher handling untouched.
- **Promotion does reach the production batch exporter** — `BatchActivityExportProcessor.OnEnd`
  filters on `data.Recorded` exactly as the simple processor does, and the deferred `ConfigureBuilder`
  callbacks keep `AddProcessor` ahead of the exporter.
- **A `RecordOnly` span cannot distort HTTP metrics** — `http.server.request.duration` comes from the
  `Microsoft.AspNetCore.Hosting` meter and never reads the Activity.
- **No double-capture via Serilog** — `writeToProviders` stays false, so the new `LogError` does not
  also ship through MEL.
- **`SentryStartupFilter` pushes the request scope ahead of the pipeline**, so the new capture site
  does carry `correlation_id`.
- **No `UseForwardedHeaders` exists anywhere in `src/PhotoPrint.API`**, so `Connection.RemoteIpAddress`
  really is the peer and ADR-018's amendment premise holds.
- **The singleton scrape gate cannot capture staler config than the scoped one did** — it reads
  `IOptions<T>`, a boot snapshot under both registrations, and has no captive dependency.
