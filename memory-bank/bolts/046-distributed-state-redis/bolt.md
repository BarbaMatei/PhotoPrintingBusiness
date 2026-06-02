---
id: 046-distributed-state-redis
unit: 001-redis-backplane
intent: 021-distributed-state-redis
type: ddd-construction-bolt
status: planned
priority: could
stories:
  - 001-signalr-redis-backplane
  - 002-two-level-cache
  - 003-distributed-rate-limiter
  - 004-redis-health-and-compose
created: 2026-05-25T10:40:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [040-containers-and-pipelines, 041-secrets-management]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 3
  avg_uncertainty: 3
  max_dependencies: 2
  testing_scope: 4
---

# Bolt: 046-distributed-state-redis

> ## ⏸ Deprioritized (decision 2026-06-03)
>
> This bolt is scaling infrastructure. It only pays off when the API runs
> on more than one server. As of 2026-06-03 the application is **not yet
> deployed** and current/foreseeable traffic fits comfortably on a single
> server (well under 1 req/s sustained per the SLOs in bolt 045).
>
> **Do not start this bolt** until at least one of these is true:
>
> 1. The app is in production AND a real scaling pressure exists
>    (sustained latency from a single-server bottleneck, a marketing
>    push expected to multiply traffic, etc.).
> 2. A zero-downtime-deploy requirement is on the roadmap (you can't do
>    zero-downtime deploys with one server).
> 3. A multi-region availability requirement appears.
>
> Until then, ADRs 010 / 013 / 015 explicitly accept the single-server
> trade-offs they describe — those decisions remain correct and don't
> need revisiting.

## Overview

Redis as backplane: SignalR fan-out, two-level cache, distributed rate-limiter, health/compose updates.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Domain Model | `ddd-01-domain-model.md` — `ITwoLevelCache` contract, rate-limit partition semantics, graceful-degradation policy |
| 2 | Technical Design | `ddd-02-technical-design.md` — Redis key namespaces, pub/sub channels, fallback strategy |
| 3 | Implement | Code + Compose updates |
| 4 | Test | `ddd-03-test-report.md` — multi-instance integration tests via Testcontainers |

## Dependencies

- **Requires**: 040-containers-and-pipelines, 041-secrets-management.
- **Enables**: production multi-instance rollout.
