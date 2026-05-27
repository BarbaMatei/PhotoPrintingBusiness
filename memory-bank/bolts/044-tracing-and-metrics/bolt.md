---
id: 044-tracing-and-metrics
unit: 001-tracing-and-metrics
intent: 020-observability-stack
type: ddd-construction-bolt
status: planned
stories:
  - 001-otel-tracing-instrumentation
  - 002-business-metrics-and-prometheus
  - 003-per-route-sampling
created: 2026-05-25T10:35:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [040-containers-and-pipelines, 045-error-tracking-and-slos]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 3
  avg_uncertainty: 3
  max_dependencies: 2
  testing_scope: 3
---

# Bolt: 044-tracing-and-metrics

## Overview

OTel SDK + custom metrics + Prometheus endpoint + per-route sampling.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Domain Model | metric taxonomy, sampler decision rules |
| 2 | Technical Design | extension method shape, exporter wiring, sampler implementation |
| 3 | Implement | code + endpoints + `metrics.md` |
| 4 | Test | counter increment tests, scrape-format snapshot test |

## Dependencies

- **Requires**: 040-containers-and-pipelines (deploy host runs the collector/Grafana stack).
- **Enables**: SRE practices and incident-response improvements.
