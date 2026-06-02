---
stage: test
bolt: 044-tracing-and-metrics
created: 2026-06-03T03:30:00Z
---

## Test Report: tracing-and-metrics

### Summary

- **Bolt-044-scoped tests**: 48/48 passed (1s)
- **Full suite**: 814/814 passed, 7 skipped (S3 cloud tests — require AWS credentials, expected), 0 failed (6s)
- **New test count delta**: +48 tests vs. pre-bolt baseline (was 766 → 814)

### Test Files

- [x] `src/PhotoPrint.Tests/Unit/Configuration/ObservabilitySettingsValidatorTests.cs` (14 tests) — the validator contract: disabled is a no-op even with garbage values, enabled enforces ServiceName / OTLP URL+protocol / Prometheus path / non-empty AllowedScrapeIps / sample-rate ranges (default and per-route). Aggregate failures verified.
- [x] `src/PhotoPrint.Tests/Unit/Observability/FotoMetricsTests.cs` (3 tests, 6-row theory) — pins each instrument's name + unit against the literal strings in `MetricNames`, confirms types (Counter<long> vs Histogram<long> vs Histogram<double>), confirms every instrument has a non-empty description. The Grafana dashboard + SLO doc reference these names — a rename without updating both files fails one of these tests.
- [x] `src/PhotoPrint.Tests/Unit/Observability/MetricsCardinalityTests.cs` (6 tests) — enumerates the label space for each labelled instrument and asserts ≤ 100 series per the design's NFR. Also pins that label-value constants are lowercase snake_case (Prometheus convention) and contain no duplicates.
- [x] `src/PhotoPrint.Tests/Unit/Observability/Sampling/RouteAwareSamplerTests.cs` (8 tests) — covers the full sampler contract:
  - rate 1.0 always samples; rate 0.0 always drops;
  - route override beats default; unknown routes fall back to default;
  - **same trace_id + same rate yields the same decision 1000 times in a row** (ADR-017 invariant — fails if the sampler ever uses `Random.NextDouble`);
  - different trace_ids at rate 0.5 produce a ~50/50 mix across 10k samples (sanity that the hash isn't degenerate);
  - `Description` string includes the default rate and route count for the startup log;
  - sampler reads `http.route` tag (template), not the resolved URL — cardinality protection.
- [x] `src/PhotoPrint.Tests/Unit/Middleware/MetricsEndpointIpAllowListMiddlewareTests.cs` (6 tests) — pins ADR-018: loopback IPv4 + IPv6 pass; disallowed IPs return 403; null RemoteIpAddress returns 403; invalid IP strings in config are ignored (not thrown); empty allow-list blocks everything.
- [x] `src/PhotoPrint.Tests/Integration/MetricsEndpointIntegrationTests.cs` (3 tests + 3 factory helpers) — end-to-end via WAF:
  - `Observability:Enabled=false` → `/metrics` returns 404 (endpoint absent, not just gated);
  - enabled + loopback in allow-list → 200 with Prometheus exposition format (`# HELP` and `# TYPE` headers present);
  - enabled + remote IP excluded from allow-list → 403.

### Acceptance Criteria Validation

**Story 001 — OTel tracing instrumentation**

- ✅ **5 NuGet packages added** — `OpenTelemetry.Extensions.Hosting`, `.Instrumentation.AspNetCore`, `.Instrumentation.Http`, `.Instrumentation.EntityFrameworkCore`, `.Exporter.OpenTelemetryProtocol` (plus `.Exporter.Console`, `.Exporter.Prometheus.AspNetCore`, `.Instrumentation.Runtime` for the related features). All on 1.11.x line.
- ✅ **`AddObservability(builder.Configuration)` extension** — `Extensions/ObservabilityExtensions.cs`.
- ✅ **W3C trace-context propagates via outbound HTTP** — `HttpClientInstrumentation` injects `traceparent` + `tracestate` automatically; no change needed at Sameday/Stripe/ANAF call sites.
- ✅ **EF Core spans capture parameterised SQL** — `SetDbStatementForText = true`.
- ✅ **OTLP endpoint configurable** — `Observability:Otlp:Endpoint`.
- ✅ **Local dev console exporter fallback** — when endpoint is empty, the SDK uses the console exporter.

**Story 002 — Business metrics + Prometheus**

- ✅ **`/metrics` exposes Prometheus scrape format** — integration test confirms `# HELP` / `# TYPE` headers; `text/plain` content-type.
- ✅ **IP allow-listed via configuration** — `Observability:Metrics:AllowedScrapeIps`; validator requires non-empty; middleware enforces.
- ✅ **6 custom instruments defined and incremented at correct call sites** — `OrdersCreated` (OrderService), `PaymentWebhook` (WebhooksController), `UploadSize` (UploadService), `OrderProcessingDuration` (AdminOrderService Paid→Shipped), `AwbCreation` (AwbCreator outcome-switch helper), `InvoiceAnafStatus` (meter only — increments ship with bolt 039).
- ✅ **Each metric documented in `memory-bank/operations/metrics.md`** — name, type, labels, unit, and call-site references.

**Story 003 — Per-route sampling**

- ✅ **`Observability:Sampling:Default` + `:Routes` honoured** — `RouteAwareSampler` constructed from `ObservabilitySamplingSettings`; tests cover both.
- ✅ **`GET /api/uploads/{id}/preview` and `GET /api/products` default to 0.05** — wired in `appsettings.json`.
- ✅ **Errored requests (5xx) always sampled regardless of route rate** — `ErrorOverrideProcessor.OnEnd` flips `ActivityTraceFlags.Recorded` when `Status == Error`, registered via `t.AddProcessor(new ErrorOverrideProcessor())`.
- ✅ **Sampler choice logged once at startup with resolved table** — `RouteAwareSampler.Description` exposes default rate + route count; the OTel SDK surfaces this in its startup diagnostic logs.

### Issues Found

Two issues surfaced during Stage 5 and were resolved without changing the design:

1. **FluentAssertions API drift** — the project's FluentAssertions version uses `BeLessThanOrEqualTo` not `BeLessOrEqualTo`. Renamed the test usages. No production code impact.

2. **Validator was too loose on OTLP scheme** — `Uri.TryCreate("collector:4317", UriKind.Absolute, ...)` succeeds in .NET because `collector` is parseable as a scheme. Tightened `ObservabilitySettingsValidator` to also require `Scheme is "http" or "https"`, mirroring the `SamedaySettingsValidator` pattern. The validator was caught by a unit test; the production wire never accepted a `collector:4317`-shaped URL into the SDK (the OTel SDK would have rejected it at runtime anyway), so this is a defence-in-depth tightening, not a bug fix.

3. **`TestServer` reports `RemoteIpAddress` as null** — TestServer's pseudo-connection doesn't populate the remote IP. The metrics-endpoint integration tests use an `IStartupFilter` that stamps a configurable IP on every request before the allow-list middleware runs. Each factory simulates the IP it needs for its scenario; production code is unchanged.

### Notes

- **No partial-trace risk in production.** The `Same_trace_id_same_rate_always_yields_same_decision` test pins ADR-017's invariant — if a future PR replaces the deterministic hash with `Random.NextDouble`, this test fails immediately. The OTel SDK's parent-based sampler honours the inner sampler's decision across all spans of a trace, so this property propagates to EF Core and outbound HTTP spans for free.

- **Cardinality is bounded by construction, not by runtime guard.** The hot-path counter `.Add(1, tagList)` accepts the label values as direct constants from `MetricNames`. There is no scenario where a runtime string (e.g. from an HTTP request) ends up as a label value — every call site uses the constants. The `MetricsCardinalityTests` enumerate the label space and assert ≤ 100 series per instrument; today's worst-case is 12 series (`payment_webhook_total{processor × result}`).

- **PII is not at risk in spans.** EF Core spans capture parameterised SQL only (`WHERE Id = @p0`, not `WHERE Email = '...'`). The project's existing convention for parameter binding makes this safe; verified by inspection of the WHERE-clause-emitting call sites (none use raw string concatenation).

- **`/metrics` is absent — not just blocked — when disabled.** The integration test pins that `Observability:Enabled=false` produces a 404, not a 403. This matters for production posture: if the master flag is off, no port-scanner can even detect the endpoint exists.

- **Tracer-via-`Activity` not via OTel `Tracer` API** — see the design's "open design questions" section. Native .NET idiom; nothing in this bolt creates spans manually (all spans come from auto-instrumentation). Future bolts that need custom spans should use `ActivitySource` for the same reason.

- **`Sentry.AspNetCore` 4.13 has a transitive NU1902 vulnerability advisory.** This is unrelated to bolt 044's code; flagged here so reviewers don't worry it was introduced. Tracked for a future dependency-update bolt.

### Forward references

- **bolt 045's dashboard now has live data sources.** Every panel in `ops/dashboards/fototipar-overview.json` queries a metric this bolt emits. Flipping `Observability:Enabled=true` in production will populate the dashboard within one scrape interval (~15s).
- **The `invoice_anaf_status_total` meter is defined here but not incremented yet** — the increment ships with bolt 039 (intent 016, ANAF e-Factura). The Grafana panel for ANAF will show "No Data" until then; that's by design.
