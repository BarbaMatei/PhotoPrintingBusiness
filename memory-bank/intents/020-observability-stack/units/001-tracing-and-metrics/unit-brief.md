---
unit: 001-tracing-and-metrics
intent: 020-observability-stack
phase: inception
status: complete
created: 2026-05-25T10:35:00.000Z
updated: 2026-05-25T10:35:00.000Z
---

# Unit Brief: Tracing & Metrics

## Purpose

Wire OpenTelemetry traces across ASP.NET + HttpClient + EF Core, define business metrics, and expose Prometheus scrape — with per-route sampling so high-volume endpoints don't dominate cost.

## Scope

### In Scope
- OpenTelemetry NuGet packages
- ASP.NET / HttpClient / EF Core auto-instrumentation
- Custom counters and histograms
- OTLP exporter + Prometheus endpoint
- Sampler config

### Out of Scope
- Sentry (002)
- Log shipping (Serilog file logs remain)

---

## Story Summary

| Story ID | Title | Priority |
|----------|-------|----------|
| 001-otel-tracing-instrumentation | Add OTel SDK and ASP.NET/HTTP/EF instrumentation | Should |
| 002-business-metrics-and-prometheus | Define counters/histograms + `/metrics` endpoint | Should |
| 003-per-route-sampling | Per-route sampler (5 % for hot endpoints) | Should |
