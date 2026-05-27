---
id: 001-signalr-redis-backplane
unit: 001-redis-backplane
intent: 021-distributed-state-redis
status: draft
priority: must
created: 2026-05-25T10:40:00Z
assigned_bolt: 046-distributed-state-redis
implemented: false
---

# Story: 001-signalr-redis-backplane

## User Story

**As** an admin
**I want** order notifications to arrive regardless of which API replica I'm connected to
**So that** scaling out doesn't fragment the dashboard

## Acceptance Criteria

- [ ] `services.AddSignalR().AddStackExchangeRedis(cfg["Redis:ConnectionString"]!)` wired.
- [ ] Two-replica integration test (Testcontainers Redis): client on replica A receives a `NewOrder` event published from replica B within 100 ms.
- [ ] No regression in single-replica behaviour.

## Technical Notes

- Use channel prefix `fototipar` to avoid collisions in shared Redis.
- Connection string lives in env var; no plaintext in code or appsettings.

## Dependencies

### Requires
- intent 017 + 018 (env-var matrix + secret hygiene)

### Enables
- 002-two-level-cache (shared multiplexer)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Redis temporarily unreachable | SignalR queues messages locally; resumes on reconnect (SDK handles) |
| Network partition | Different replicas may briefly diverge; resyncs on heal |

## Out of Scope

- Per-tenant channel isolation.
