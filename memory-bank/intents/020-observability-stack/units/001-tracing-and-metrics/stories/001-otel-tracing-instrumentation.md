---
id: 001-otel-tracing-instrumentation
unit: 001-tracing-and-metrics
intent: 020-observability-stack
status: draft
priority: should
created: 2026-05-25T10:35:00Z
assigned_bolt: 044-tracing-and-metrics
implemented: false
---

# Story: 001-otel-tracing-instrumentation

## User Story

**As** an oncall engineer
**I want** distributed traces stitching API → Stripe / Sameday / ANAF calls
**So that** I can see where a slow checkout actually spent its time

## Acceptance Criteria

- [ ] NuGet packages added: `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`, `OpenTelemetry.Instrumentation.EntityFrameworkCore`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`.
- [ ] `AddObservability(builder.Configuration)` extension in `Extensions/ObservabilityExtensions.cs` wires tracing + metrics.
- [ ] Trace context propagates via W3C trace-context headers on outbound HTTP calls.
- [ ] EF Core spans capture parameterised SQL (`OTEL_INSTRUMENTATION_ENTITYFRAMEWORK_INCLUDE_SQL_STATEMENT=true`).
- [ ] OTLP exporter target configurable via `Observability:Otlp:Endpoint`.
- [ ] Local dev: spans visible in console exporter when no endpoint configured.

## Technical Notes

```csharp
services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("PhotoPrint.API", serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString()))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation();
        t.AddHttpClientInstrumentation();
        t.AddEntityFrameworkCoreInstrumentation();
        t.AddOtlpExporter(o => o.Endpoint = new Uri(cfg["Observability:Otlp:Endpoint"]!));
    });
```

## Dependencies

### Requires
- intent 017 (deploy artefacts to host an OTel collector)

### Enables
- 002-business-metrics-and-prometheus, 003-per-route-sampling

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| OTLP endpoint unreachable | Spans dropped silently; no app failure |
| High-volume endpoint dominates traces | Per-route sampler (story 003) cuts to 5% |

## Out of Scope

- Frontend OTel instrumentation.
