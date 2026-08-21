---
stage: model
bolt: 044-tracing-and-metrics
created: 2026-06-03T01:00:00Z
---

## Static Model: tracing-and-metrics

> **Note on framing.** Observability bolts don't introduce business entities;
> they introduce a measurement vocabulary. The "domain" here is the set of
> things we measure (metrics) and the set of operations whose lifecycle we
> trace (spans). The model below documents both as a controlled taxonomy.

### Domain entities

The observability stack adds three observable concerns to existing domain
entities. No new persisted entities. The relevant existing entities and what
this bolt observes about them:

- **`Order`** — observed via:
  - `orders_created_total{processor, status}` counter (incremented at order
    creation in `OrderService.CreateAsync`)
  - `order_processing_duration_seconds` histogram (Paid → Shipped wall-clock,
    sampled at the moment `Order.Status` transitions to `Shipped`)
- **`PaymentWebhook` events** — observed via:
  - `payment_webhook_total{processor, result}` counter (incremented in
    `WebhooksController.Stripe` and `WebhooksController.EuPlatesc`)
- **`Upload`** — observed via:
  - `upload_size_bytes` histogram (recorded in `UploadService.UploadAsync`)
- **`AwbCreation`** — observed via:
  - `awb_creation_total{result}` counter (incremented in `AwbCreator`'s
    terminal branches — `Created`, `RetryLater`, `GiveUp`, `Skipped`, `RaceLost`)
- **`AnafSubmission`** (future, bolt 039) — observed via:
  - `invoice_anaf_status_total{status}` counter (call site lives in the
    ANAF SPV client; this bolt defines the meter only, the increment lands
    when bolt 039 ships)

### Value objects

#### Metric

```text
Metric
├── Name           : string (snake_case_total / _seconds / _bytes)
├── Type           : Counter | Histogram | UpDownCounter
├── Unit           : string (`1` for counters, `seconds`, `bytes`, `By`)
├── Labels         : IReadOnlyDictionary<string, ValueSet>
└── Description    : string
```

**Invariants**:
- Name follows the Prometheus convention: lowercase snake_case, suffix
  encodes type (`_total` for counters, `_seconds` for time histograms,
  `_bytes` for byte histograms).
- Label keys are stable (no free-form values that would explode cardinality).
- `ValueSet` for a label is enumerated, not arbitrary. Examples:
  `processor ∈ {stripe, euplatesc}`, `result ∈ {ok, failed, duplicate, rejected}`.

#### Span

```text
Span
├── OperationName  : string (e.g. "POST /api/payments/stripe/intent")
├── Kind           : Server | Client | Internal
├── Attributes     : IReadOnlyDictionary<string, object>
├── Status         : Unset | Ok | Error
├── ParentSpanId   : string? (W3C trace-context)
└── Duration       : TimeSpan
```

**Invariants**:
- Span `Status` is `Error` if and only if the underlying operation produced
  a 5xx response OR threw an unhandled exception.
- `http.route` attribute is the **route template**, not the resolved URL
  (`/api/orders/{id}` not `/api/orders/9e3f...`), to bound cardinality.
- Outbound HTTP calls (Stripe, Sameday, ANAF) **inherit** the parent span's
  trace id via W3C trace-context (`traceparent` + `tracestate` headers).

#### SamplingDecision

```text
SamplingDecision
├── Sampled        : bool
├── Reason         : ConfigDefault | RouteOverride | ParentSampled
│                  | ErrorOverride | DropDuplicate
└── Rate           : double (the sample rate that drove the decision)
```

**Invariants**:
- A 5xx response **always** flips `Sampled = true` regardless of
  configured rate (the "error-always-sampled" override is non-negotiable
  — see story 003).
- A request with a sampled parent span inherits the parent's decision
  unless the route is in the always-drop set (none currently).
- Default decision is `Bernoulli(rate)` where rate comes from the route
  table (`Observability:Sampling:Routes`) with fallback to
  `Observability:Sampling:Default`.

### Aggregates

#### Metric registry (aggregate root: `FotoMetrics`)

A single static class owns every Meter and Instrument in the application.
Centralising prevents:
- Multiple Meters with the same name (silent metric collision)
- Drift between instrument definitions and their increment call sites
- Free-form label values appearing because someone wrote a `Counter.Add(1,
  new TagList { { "foo", whateverString } })` without thinking

**Members**: `Meter`, `OrdersCreated`, `PaymentWebhook`, `UploadSize`,
`OrderProcessingDuration`, `AwbCreation`, `AnafSubmission`.

**Invariants**:
- Meter name is `"PhotoPrint.API"` (matches `WithMetrics` registration)
- Meter version is the assembly version (release tagging consistency)
- All instruments are created at static init — no lazy/conditional creation

#### Sampler chain (aggregate root: `RouteAwareSampler`)

A `Sampler` implementation that composes:
1. **`ParentBasedSampler` outer**: respects upstream sampling decision
2. **`RouteAwareSampler` inner**: reads route from `Activity.GetTagItem("http.route")`,
   looks up the configured rate, applies `Bernoulli(rate)`
3. **`ErrorOverride` post-decision hook**: any sample that completes
   with `Status = Error` is upgraded to sampled regardless of inner decision

**Invariants**:
- The sampler is a singleton (one decision tree per process).
- Configuration is read once at startup; changes require process restart
  (matches the project's posture on every other settings-bound class —
  fail-fast at boot, no hot reload).
- Unknown routes fall back to `Default` rate; a "route key not found in
  config" is logged at Debug level once per process per route, not per
  request.

### Domain events

This bolt is observation-only — it does not raise new domain events. But
several existing events become observable:

- **`OrderCreated`** — increments `orders_created_total{processor=PaymentProcessor}`
- **`PaymentWebhookReceived`** — increments `payment_webhook_total{processor, result}`
- **`UploadCompleted`** — records `upload_size_bytes` histogram observation
- **`OrderShipped`** — records `order_processing_duration_seconds` histogram
  observation (PaidAt → ShippedAt)
- **`AwbCreationOutcome`** (bolt 037's discriminated union) — branches map
  to `awb_creation_total{result}` label values

### Domain services

#### `IObservabilityBootstrapper`

Single responsibility: register the OpenTelemetry pipeline (traces + metrics)
with the `IServiceCollection`. Hides the SDK details from `Program.cs`.

**Operations**:
- `AddObservability(IServiceCollection, IConfiguration) : IServiceCollection`
  - Reads `Observability:Enabled` (master flag, default `false`)
  - When off → registers nothing; the `Meter` static still exists but no
    exporter consumes it (instruments are a no-op except for trivial CPU cost)
  - When on → wires `AddOpenTelemetry().WithTracing(...).WithMetrics(...)`
    with ASP.NET / HttpClient / EF Core auto-instrumentation + OTLP exporter
    (if endpoint configured) + Prometheus exporter (always when on)

**Dependencies**: `IConfiguration`, the `FotoMetrics.Meter` (registered with
the SDK).

#### `IObservabilityBootstrapper` is intentionally NOT a domain service per
DDD. It's an infrastructure concern. Listed here for completeness because
it's the seam between Program.cs and the rest of the stack.

### Repository interfaces

None. Metrics and traces are pushed by the SDK to exporters; we don't store
them in our DB.

### Ubiquitous language

| Term | Definition |
|---|---|
| **Span** | A unit of work with start/end timestamps, attributes, and an optional parent span id. Equivalent to a Sentry transaction at the network level. |
| **Trace** | A directed acyclic graph of spans sharing a `trace_id`. A trace stitches "incoming HTTP request" → "EF query" → "outbound Stripe call" → "outbound Sameday call". |
| **Metric** | A numeric measurement. Three kinds: counter (monotonic), histogram (bucketed observations), up-down-counter (gauge-like). |
| **Sampler** | A decision function: "should this span be recorded?". Trades observability completeness for cost. |
| **Exporter** | A SDK pipeline component that ships spans/metrics to a backend. We use OTLP (traces, to a collector) and Prometheus scrape (metrics, pulled by Prometheus). |
| **Cardinality** | The product of a label's distinct values. `processor × result` = 2 × 7 = 14 series. Free-form label values (e.g. user emails) → unbounded cardinality → metrics backend collapses. |
| **W3C trace-context** | The `traceparent` + `tracestate` HTTP headers that propagate trace identity across service boundaries. Standard at <https://www.w3.org/TR/trace-context/>. |
| **Hot endpoint** | A high-RPS endpoint that, if traced 100%, dominates trace storage cost. Examples in this codebase: `GET /api/uploads/{id}/preview`, `GET /api/products`. |
| **Hot path** | A code path on the request critical-line where allocations matter. The metric record sites (`counter.Add(1, tags)`) are on hot paths; the tag-list construction must be allocation-light. |
| **Resource** | OTel concept: static attributes attached to every span/metric from this process. We set `service.name="PhotoPrint.API"`, `service.version=<assembly version>`. |

### Stories coverage check

- ✅ Story **001 (OTel tracing instrumentation)** — entities, value objects
  (Span, SamplingDecision), bootstrapper service, ubiquitous language covered.
- ✅ Story **002 (Business metrics + Prometheus)** — entities, Metric value
  object, FotoMetrics aggregate, label cardinality invariants covered.
- ✅ Story **003 (Per-route sampling)** — RouteAwareSampler aggregate,
  ErrorOverride invariant, SamplingDecision value object covered.

### Forward references

- Bolt 045's SLO doc enumerates metric NAMES that this bolt produces. The
  list above matches that doc 1:1. Both should be edited together if a
  name changes.
- Future intent 021 (Redis) may introduce two-level cache hit/miss metrics
  using the same `FotoMetrics` aggregate. That intent should add its
  instruments to the existing static class, not create a new one.
- Future ANAF bolt (039) increments `invoice_anaf_status_total` defined
  here. This bolt ships the meter; that bolt ships the increments.
