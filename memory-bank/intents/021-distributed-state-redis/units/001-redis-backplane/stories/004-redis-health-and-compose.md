---
id: 004-redis-health-and-compose
unit: 001-redis-backplane
intent: 021-distributed-state-redis
status: draft
priority: must
created: 2026-05-25T10:40:00Z
assigned_bolt: 046-distributed-state-redis
implemented: false
---

# Story: 004-redis-health-and-compose

## User Story

**As** an operator
**I want** Redis included in health checks and provisioned in Compose
**So that** deploys self-attest the dependency and dev parity is automatic

## Acceptance Criteria

- [ ] `/health` exposes `redis: ok|down` (always 200; sub-status only). Aligns with ADR-001.
- [ ] `docker-compose.yml` adds a `redis` service with `redis:7-alpine`, `--appendonly yes`, named volume `redisdata`.
- [ ] `docker-compose.prod.yml` either runs the same OR documents the managed-Redis env var pattern (one of these — config-only swap).
- [ ] README updated with Redis env-var requirement.

## Technical Notes

- Health check uses `IConnectionMultiplexer.GetDatabase().PingAsync()` with 500 ms timeout.
- On dev without Redis: app still boots but Sub-systems fall back to per-instance behaviour with a Warning at startup.

## Dependencies

### Requires
- 001 / 002 / 003

### Enables
- Production rollout

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Redis password rotation | Connection string env var swap → restart |
| Volume restored from backup | AOF replay on boot |

## Out of Scope

- Redis Sentinel / Cluster topology (out of MVP).
