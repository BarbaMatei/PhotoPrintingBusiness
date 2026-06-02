---
id: 002-business-metrics-and-prometheus
unit: 001-tracing-and-metrics
intent: 020-observability-stack
status: complete
priority: should
created: 2026-05-25T10:35:00.000Z
assigned_bolt: 044-tracing-and-metrics
implemented: true
---

# Story: 002-business-metrics-and-prometheus

## User Story

**As** the team
**I want** business KPIs exposed as Prometheus metrics
**So that** we can graph conversion, payment failures, and AWB success rates over time

## Acceptance Criteria

- [ ] `/metrics` endpoint exposes Prometheus scrape format; IP allow-listed via configuration.
- [ ] Custom metrics defined and incremented at the correct call sites:
  - `orders_created_total{processor,status}` counter
  - `payment_webhook_total{processor,result}` counter
  - `upload_size_bytes` histogram
  - `order_processing_duration_seconds` histogram (Paid → Shipped)
  - `awb_creation_total{result}` counter (intent 015 hooks)
  - `invoice_anaf_status_total{status}` counter (intent 016 hooks)
- [ ] Each metric documented in `memory-bank/operations/metrics.md` with type, labels, and unit.

## Technical Notes

```csharp
public sealed class FotoMetrics
{
    public static readonly Meter Meter = new("PhotoPrint.API", "1.0");
    public static readonly Counter<long> OrdersCreated =
        Meter.CreateCounter<long>("orders_created_total");
    // ... others
}
```

- `MeterProvider` wired via `AddOpenTelemetry().WithMetrics(m => m.AddMeter("PhotoPrint.API"))`.
- Prometheus exporter via `OpenTelemetry.Exporter.Prometheus.AspNetCore` package.

## Dependencies

### Requires
- 001-otel-tracing-instrumentation

### Enables
- 003-per-route-sampling, intent 020 unit 002 dashboards

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| `/metrics` from unauthorised IP | 403 |
| Counter cardinality blowup | Limit label values to known enums; avoid free-form |

## Out of Scope

- Histogram quantile tuning (defaults are fine to start).
