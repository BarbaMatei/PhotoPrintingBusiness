---
stage: design
bolt: 044-tracing-and-metrics
created: 2026-06-03T01:30:00Z
---

## Technical Design: tracing-and-metrics

### Architecture Pattern

**Composition over configuration**, behind a master feature flag, with all
SDK wiring hidden by a single extension method.

Rationale:
- Matches the precedent set by `AddPhotoStorage`, `AddSecurityBaselines`,
  `AddAuthCore`, `AddEmailInfrastructure`, etc. — Program.cs reads as a
  table of capabilities, not as an SDK manual.
- The OpenTelemetry SDK has a deeply nested fluent surface
  (`AddOpenTelemetry().WithTracing(t => t.AddXxx().AddYyy())...`); keeping
  it inside one extension class avoids cluttering Program.cs.
- Two-stage rollout posture (master `Observability:Enabled` flag) mirrors
  bolt 036 (Sameday) and bolt 045 (Sentry). With the flag off, the SDK is
  never registered, no exporters are wired, the `/metrics` endpoint is
  absent, boot is byte-identical to baseline.

### Layer Structure

```text
┌─────────────────────────────────────────────────────────────┐
│   Presentation        Program.cs · MapMetricsEndpoint       │
│                       MetricsIpAllowListMiddleware          │
├─────────────────────────────────────────────────────────────┤
│   Application         FotoMetrics (Meter + Instruments)     │
│                       Call sites in existing services:      │
│                         OrderService · WebhooksController   │
│                         UploadService · AwbCreator          │
│                         AdminOrderService                   │
├─────────────────────────────────────────────────────────────┤
│   Domain              No change — observation is layered    │
│                       on top of existing domain.            │
├─────────────────────────────────────────────────────────────┤
│   Infrastructure      Extensions/ObservabilityExtensions    │
│                         AddObservability()                  │
│                       Sampling/RouteAwareSampler            │
│                       OTel SDK + OTLP + Prometheus exports  │
└─────────────────────────────────────────────────────────────┘
```

**Responsibilities**:

- **Presentation** — Single new endpoint (`GET /metrics`) gated by an IP
  allow-list middleware. Endpoint registration is conditional on the
  master flag.
- **Application** — `FotoMetrics` is a static class consumed by existing
  service classes at their natural call sites. No new application
  services. The increment is a one-line addition next to existing
  domain operations (e.g. `FotoMetrics.OrdersCreated.Add(1, ...)` next
  to `await db.SaveChangesAsync()` in `OrderService.CreateAsync`).
- **Domain** — Untouched. Observability is a cross-cutting concern, not a
  domain concept.
- **Infrastructure** — All OTel SDK wiring lives here. Configuration
  binding, exporter selection, sampler installation.

### Project structure additions

```text
src/PhotoPrint.API/
├── Configuration/
│   ├── ObservabilitySettings.cs              ← new
│   └── ObservabilitySamplingSettings.cs      ← new (nested)
├── Validators/
│   └── ObservabilitySettingsValidator.cs     ← new
├── Extensions/
│   └── ObservabilityExtensions.cs            ← new (AddObservability)
├── Observability/                            ← new directory
│   ├── FotoMetrics.cs                        ← static Meter + Instruments
│   ├── MetricNames.cs                        ← const strings (single source)
│   └── Sampling/
│       └── RouteAwareSampler.cs              ← composed sampler
├── Middleware/
│   └── MetricsEndpointIpAllowListMiddleware.cs ← gates /metrics
└── Program.cs                                ← +AddObservability(), +Map endpoint
```

### Configuration shape

```jsonc
"Observability": {
  "Enabled": false,
  "ServiceName": "PhotoPrint.API",
  "Otlp": {
    "Endpoint": "",     // empty → console exporter only (dev)
    "Protocol": "Grpc"  // Grpc | HttpProtobuf
  },
  "Metrics": {
    "PrometheusEndpoint": "/metrics",
    "AllowedScrapeIps": [ "127.0.0.1", "::1" ]
  },
  "Sampling": {
    "Default": 1.0,
    "Routes": {
      "GET /api/uploads/{id}/preview": 0.05,
      "GET /api/products": 0.05
    }
  }
}
```

**Validation** (`ObservabilitySettingsValidator : IValidateOptions<…>`):
- `Enabled=false` → all rules skipped (no-op validator, same pattern as
  `SamedaySettingsValidator` and `SentrySettingsValidator`).
- `Enabled=true` →
  - `ServiceName` not empty
  - `Otlp.Protocol ∈ {"Grpc", "HttpProtobuf"}`
  - If `Otlp.Endpoint` is non-empty, must be an absolute URI
  - `Sampling.Default ∈ [0.0, 1.0]`
  - Each `Sampling.Routes` value `∈ [0.0, 1.0]`
  - `Metrics.PrometheusEndpoint` starts with `/`
  - `Metrics.AllowedScrapeIps` non-empty (else `/metrics` is unreachable)

### Extension method shape

```text
ObservabilityExtensions.AddObservability(IServiceCollection, IConfiguration)

  1. Bind + validate ObservabilitySettings (with ValidateOnStart)
  2. Read Observability:Enabled once — return early if false
  3. Register FotoMetrics as a hosted-singleton-friendly type
     (instruments are static, but a "registration" no-op makes the
     existence of the class visible to DI for documentation)
  4. Register MetricsEndpointIpAllowListMiddleware as scoped
  5. Wire AddOpenTelemetry().ConfigureResource(r => r.AddService(...))
     - WithTracing(t => …)
         · AddAspNetCoreInstrumentation(o => o.RecordException = true)
         · AddHttpClientInstrumentation()
         · AddEntityFrameworkCoreInstrumentation(o => o.SetDbStatementForText = true)
         · SetSampler(new ParentBasedSampler(
             new RouteAwareSampler(samplingConfig)))
         · If Otlp.Endpoint configured → AddOtlpExporter(…)
         · Else → AddConsoleExporter()  (dev visibility)
     - WithMetrics(m => …)
         · AddMeter(FotoMetrics.MeterName)
         · AddAspNetCoreInstrumentation()
         · AddHttpClientInstrumentation()
         · AddRuntimeInstrumentation()
         · AddPrometheusExporter()
```

### API Design

Single new endpoint:

| Endpoint | Method | Auth | Response | Notes |
|---|---|---|---|---|
| `GET /metrics` | GET | IP allow-list (not JWT) | `200` Prometheus text format | Path configurable via `Observability:Metrics:PrometheusEndpoint`; default `/metrics` |

**Request flow**:

```text
GET /metrics
   ↓
MetricsEndpointIpAllowListMiddleware
   - If RemoteIpAddress ∈ AllowedScrapeIps → next
   - Else → 403 (no body — defence in depth; the existence of the
     endpoint is not a secret, but its content is)
   ↓
Prometheus exporter middleware (from OpenTelemetry.Exporter.Prometheus.AspNetCore)
   - Serialises current snapshot of all instruments to Prometheus text format
   - Content-Type: text/plain; version=0.0.4
```

No JWT, no antiforgery, no CORS — the endpoint is server-to-server scrape.

### Data Model

No schema changes. Metrics push to the exporters; nothing in our DB.

### Security Design

**Concerns and mitigations**:

| Concern | Mitigation |
|---|---|
| `/metrics` exposes internal cardinality (orders/day, error counts) — competitive intel | IP allow-list middleware; production allow-list contains only the Prometheus scraper's IP / pod selector |
| Cardinality blowup via free-form label values → metric backend collapse | All label values are enumerated in `MetricNames.cs` constants; no instrument call site passes a runtime string as a label value |
| OTLP endpoint over-shares span attributes (SQL with PII) | `EntityFrameworkCoreInstrumentation` SetDbStatementForText is **on** for parameterised SQL; we never put PII in WHERE clauses by convention (auth uses `UserId`, etc., never email/phone) — verify in code review |
| Outbound HTTP spans leak credentials in URLs | OTel's HttpClientInstrumentation captures `http.url` — Sameday/Stripe URLs are credential-free; verify code review on any new outbound integration that puts secrets in query strings (none currently) |
| `/metrics` always-on attack surface | Wrapped in master flag — `Observability:Enabled=false` → endpoint not mapped at all |

### NFR Implementation

| Requirement | Design Approach |
|---|---|
| **Hot-path allocation budget** | Counter `.Add(1, tagList)` accepts `TagList` (a stack-allocated struct) — no heap allocation per increment. Verified by Microsoft documentation; we use this shape consistently. |
| **Sampler cost < 50µs per request** | `RouteAwareSampler` reads `Activity.GetTagItem("http.route")` (O(1)) and looks up rate in a `FrozenDictionary` (O(1)). Decision is single `Random.Shared.NextDouble() < rate`. |
| **Cardinality bound: < 100 series per instrument** | Enforced by label invariants in the domain model. Reviewed in CI? No — pinned by `MetricsCardinalityTests` (Stage 5) that enumerate the label combinations for each instrument and assert ≤ 100. |
| **`/metrics` scrape latency < 100ms p99 at 1000 series** | Prometheus exporter is in-process, in-memory snapshot of the OTel SDK's metric store. No I/O. |
| **OTLP exporter failure resilience** | SDK queues with bounded buffer; drops oldest on overflow. Never blocks request handling. |
| **Graceful boot when OTel endpoint unreachable** | OTLP exporter is fire-and-forget on background; first-time misconfig manifests as "no traces in backend" not as boot failure. Acceptance criterion explicit. |

### Integration Points

```text
┌──────────────────────────┐
│  PhotoPrint.API          │
│                          │  W3C trace-context
│  ┌────────────────────┐  │  (traceparent + tracestate headers)
│  │ OTel SDK           │  ├──────────────────────► Stripe API
│  │  - Tracing         │  ├──────────────────────► Sameday API
│  │  - Metrics         │  ├──────────────────────► ANAF SPV (future)
│  │  - Sampling        │  │
│  └────────┬───────────┘  │
│           │              │
│  ┌────────┴─────┐ ┌──────┴────────┐
│  │ OTLP exporter│ │ Prometheus    │
│  │ (push)       │ │ exporter      │
│  └────┬─────────┘ │ (scrape)      │
│       │           └──────┬────────┘
└───────┼──────────────────┼─────────┘
        │                  │
        ▼                  ▼
   OTel Collector     Prometheus
   (Grafana Tempo,    (Grafana
    Jaeger, …)         dashboard
                       from bolt 045)
```

- **W3C trace-context propagation**: `HttpClientInstrumentation` automatically
  injects `traceparent` + `tracestate` on outbound HTTP. We don't need to
  touch the existing Stripe/Sameday/ANAF code — the instrumentation hooks
  at the `HttpClient.SendAsync` level.
- **OTLP collector**: deployment concern; the API just emits, the collector
  routes to whatever backend (Tempo, Jaeger, Honeycomb, etc.). Document in
  `DEPLOYMENT.md` §14.
- **Prometheus**: scrapes `/metrics` on a schedule (typically 15s). The
  Grafana dashboard from bolt 045 then queries Prometheus for the panels.

### Sampler logic

```text
RouteAwareSampler.ShouldSample(SamplingParameters params):

  1. If params.Kind != SpanKind.Server → return parent's decision (delegate
     to ParentBasedSampler outer)
  2. Read route = params.Tags["http.route"] (or fallback to params.Name)
  3. rate = config.Routes.GetValueOrDefault(route, config.Default)
  4. If rate == 1.0 → SamplingResult(RecordAndSample, "rate=1.0")
  5. If rate == 0.0 → SamplingResult(Drop, "rate=0.0 — but may be promoted
     by ErrorOverride at span close")
  6. Else → use the deterministic hash of trace_id (NOT Random) so the
     same trace_id always yields the same decision across the system:
       hash = trace_id.GetHashCode() (lower 31 bits)
       sampled = (hash / int.MaxValue) < rate
     Return SamplingResult based on `sampled`.

ErrorOverride:
  - Hooked via SpanProcessor.OnEnd
  - If span.Status == Error → force-mark sampled (overrides any earlier
    "drop" decision)
  - Implementation: a custom `BaseProcessor<Activity>.OnEnd` that, when
    Activity.Status == ActivityStatusCode.Error, sets `ActivityTraceFlags.Recorded`
    on the parent span as well (so the whole error trace is preserved)
```

**Why deterministic hashing instead of `Random.Shared.NextDouble()`**: a
single user's request must produce the same sampling decision across all
spans in the trace, otherwise we get partial traces (root span sampled,
EF child span dropped). Deterministic hashing of trace_id is the
industry-standard approach (see OpenTelemetry sampling SIG).

### Wiring order in Program.cs

```text
// (existing)
builder.AddSerilogLogging();
... Sentry registration ...

// NEW: observability — must come before everything that wants to be traced
//      (which is everything, so this goes early)
builder.Services.AddObservability(builder.Configuration);

// (existing) DB, middleware, controllers, hosted services, etc.

var app = builder.Build();

// (existing) middleware pipeline
app.UseCorrelationId();
app.UseGlobalExceptionHandler();
app.UseSerilogRequestLogging();
app.UseSecurityBaselines();
app.UseResponseCaching();
// ...
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// NEW: metrics endpoint — registered conditionally
if (observabilityEnabled)
{
    // IP allow-list runs before the exporter middleware
    app.UseWhen(
        ctx => ctx.Request.Path.StartsWithSegments(metricsPath),
        branch => branch.UseMiddleware<MetricsEndpointIpAllowListMiddleware>());
    app.UseOpenTelemetryPrometheusScrapingEndpoint(metricsPath);
}

app.MapControllers();
```

### Failure modes considered

| Failure | Behaviour |
|---|---|
| `Observability:Enabled=true` but no OTLP endpoint configured | Console exporter is used for traces (dev visibility); Prometheus exporter still works. No boot failure. |
| OTLP exporter cannot reach endpoint | Spans queued in bounded buffer, dropped on overflow. App unaffected. |
| `/metrics` scraped from non-allowed IP | 403 with empty body. No log spam (one-per-IP-per-process log entry at Info). |
| A new instrument is added but never incremented | Appears in `/metrics` as `0` — that's correct Prometheus semantics. |
| Counter call site uses a free-form string as a label value | Caught in code review; not enforceable in the language. Mitigated by `MetricNames.cs` constants — adding a new label value requires editing the constants file. |
| Sampler config refers to a route that doesn't exist | Logged once at Debug, fallback to `Default` rate. No crash. |
| Multiple call sites for the same metric name | Compile error — `FotoMetrics` exposes the instrument as a single static property; "creating a second counter with the same name" is impossible if the call site uses `FotoMetrics.X` not `Meter.CreateCounter<long>("name")`. |

### Test plan (preview of Stage 5)

| Layer | Tests |
|---|---|
| Unit | `ObservabilitySettingsValidatorTests` (disabled-noop / enabled-required) · `RouteAwareSamplerTests` (route-override / default-fallback / deterministic-by-trace-id / error-always-sampled) · `FotoMetricsTests` (instrument names + types match the spec) |
| Integration | `MetricsEndpointTests` (allow-listed IP → 200 + Prometheus text format · disallowed IP → 403 · disabled flag → endpoint absent) · `MetricsIncrementTests` (hit /api/uploads and assert `upload_size_bytes` increased; hit /webhooks/stripe with a stub and assert `payment_webhook_total{processor="stripe"}` increased) |
| Cardinality | `MetricsCardinalityTests` — enumerate the label space for each instrument and assert ≤ 100 series |
| Snapshot | `PrometheusScrapeFormatSnapshotTest` — pin the exposition format so an upstream OTel breaking change is caught |

### Open design questions (resolved before Stage 4)

1. **Push vs scrape for metrics?** → Scrape (Prometheus). Push (OTLP metrics)
   is also supported by the SDK but Prometheus's pull model is more
   conventional and the dashboard from bolt 045 was written for it.

2. **OTLP vs Jaeger native for traces?** → OTLP. Jaeger has its own
   protocol, but OTLP is the vendor-neutral standard; collectors can
   route OTLP to Jaeger downstream. We don't tie ourselves to one trace
   backend.

3. **Should we use `System.Diagnostics.Activity` directly or the OTel
   `Tracer` API?** → `Activity`. It's the native .NET shape and what the
   auto-instrumentation produces. The OTel `Tracer` is a thin wrapper
   that adds nothing useful; idiomatic .NET code uses `Activity` and the
   instrumentation makes it work.

4. **Cardinality enforcement: runtime guard or compile-time?** → Constants
   in `MetricNames.cs`. Runtime guards (e.g. `if (label not in allowed)
   throw`) add latency on the hot path; constants enforce at code-review
   time which is sufficient for our scale.

### Forward references

- `MetricNames.cs` will be the single source of truth for all metric
  names + label values. Bolt 045's dashboard JSON references these
  literal strings; any rename needs both files changed together.
- The `ErrorOverride` mechanism is implemented via a custom
  `BaseProcessor<Activity>`. If a future bolt needs another
  cross-cutting span post-processing concern (e.g. PII redaction in
  span attributes), it should add another processor — don't reuse
  `ErrorOverride`'s class for an unrelated concern.

### Acceptance criteria mapped to design

**Story 001 — OTel tracing instrumentation**
- ✅ 5 NuGet packages — listed in implementation plan (Stage 4)
- ✅ `AddObservability(Configuration)` extension — `ObservabilityExtensions.cs`
- ✅ W3C trace-context propagation — `HttpClientInstrumentation` automatic
- ✅ EF Core parameterised SQL — `SetDbStatementForText = true` in `AddEntityFrameworkCoreInstrumentation`
- ✅ OTLP endpoint configurable — `Observability:Otlp:Endpoint`
- ✅ Local-dev console exporter fallback — when `Otlp.Endpoint` empty, use console exporter

**Story 002 — Business metrics + Prometheus**
- ✅ `/metrics` Prometheus format — `AddPrometheusExporter` + `UseOpenTelemetryPrometheusScrapingEndpoint`
- ✅ IP allow-list — `MetricsEndpointIpAllowListMiddleware`
- ✅ 6 custom instruments — all defined in `FotoMetrics.cs`, names match SLO doc
- ✅ `metrics.md` documentation — Stage 4 artifact

**Story 003 — Per-route sampling**
- ✅ `Observability:Sampling:Default` and `:Routes` honoured — `ObservabilitySettings.Sampling`
- ✅ `/api/uploads/{id}/preview` and `/api/products` at 0.05 — config default
- ✅ Errored requests always sampled — `ErrorOverride` processor
- ✅ Sampler choice logged at startup — `RouteAwareSampler` constructor logs the resolved table once
