---
id: 045-error-tracking-and-slos
unit: 002-error-tracking-and-slos
intent: 020-observability-stack
type: simple-construction-bolt
status: planned
stories:
  - 001-sentry-aspnet-integration
  - 002-slo-documentation-and-dashboard
created: 2026-05-25T10:35:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [040-containers-and-pipelines]
enables_bolts: [044-tracing-and-metrics]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 2
  max_dependencies: 1
  testing_scope: 2
---

# Bolt: 045-error-tracking-and-slos

## Overview

Sentry + SLO documentation + sample dashboard.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Plan | `implementation-plan.md` — Sentry config + scrubber list + SLO table |
| 2 | Implement | SDK wiring, README + ops docs, dashboard JSON |
| 3 | Test | synthetic-error → Sentry-event integration test |

## Dependencies

- **Requires**: 040-containers-and-pipelines (release SHA from deploy).
- **Enables**: 044-tracing-and-metrics (shared observability story).
