---
intent: 020-observability-stack
phase: inception
status: units-decomposed
created: 2026-05-25T10:35:00Z
updated: 2026-05-25T10:35:00Z
---

# Units: Observability Stack

## Decomposition

| Unit | Type | Stories | Default Bolt Type |
|------|------|---------|-------------------|
| 001-tracing-and-metrics | backend / ops | US-020-1, US-020-2, US-020-5 | ddd-construction-bolt |
| 002-error-tracking-and-slos | ops | US-020-3, US-020-4 | simple-construction-bolt |

## Rationale

OTel + metrics are one cohesive instrumentation effort (both SDK-based). Sentry is a separate library/integration plus SLO documentation. Split lets the Sentry "must" land quickly without waiting for OTel exporter / collector decisions.

## Execution Order

1. Days 1–3: 002-error-tracking-and-slos (Sentry + SLO docs).
2. Days 3–8: 001-tracing-and-metrics (OTel + Prometheus + sampling).
