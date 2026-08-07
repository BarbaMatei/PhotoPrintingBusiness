---
id: 003-per-route-sampling
unit: 001-tracing-and-metrics
intent: 020-observability-stack
status: complete
priority: should
created: 2026-05-25T10:35:00.000Z
assigned_bolt: 044-tracing-and-metrics
implemented: true
---

# Story: 003-per-route-sampling

## User Story

**As** the operator
**I want** high-RPS read endpoints traced at a low sample rate
**So that** tracing cost stays bounded while important paths remain fully visible

## Acceptance Criteria

- [ ] `Observability:Sampling:Default` (default 1.0) and `Observability:Sampling:Routes` (dictionary route → rate) honoured.
- [ ] `GET /api/uploads/{id}/preview` and `GET /api/products` default to 0.05.
- [ ] Errored requests (5xx) always sampled regardless of route rate.
- [ ] Sampler choice logged once at startup with the resolved table.

## Technical Notes

- Implement `ParentBasedSampler` + a custom `RouteAwareSampler` reading the config dictionary.
- `Activity.GetTagItem("http.route")` provides the route key.

## Dependencies

### Requires
- 001-otel-tracing-instrumentation, 002-business-metrics-and-prometheus

### Enables
- Final tuning before production-scale rollout

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Unknown route | Falls back to `Default` rate |
| 5xx response on sampled-out route | Forced sample (always-on for errors) |

## Out of Scope

- Tail-based sampling at the collector (out of app scope).
