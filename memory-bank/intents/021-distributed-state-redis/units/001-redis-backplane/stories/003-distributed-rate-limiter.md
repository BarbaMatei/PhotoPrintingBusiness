---
id: 003-distributed-rate-limiter
unit: 001-redis-backplane
intent: 021-distributed-state-redis
status: draft
priority: could
created: 2026-05-25T10:40:00Z
assigned_bolt: 046-distributed-state-redis
implemented: false
---

# Story: 003-distributed-rate-limiter

## User Story

**As** the platform
**I want** rate limits enforced globally across all replicas
**So that** a brute-force attacker cannot bypass the 10/min auth limit by hopping replicas

## Acceptance Criteria

- [ ] Custom `RedisRateLimiterPartition` keyed by IP / endpoint enforces fixed-window counts in Redis (`INCR` + `EXPIRE` script).
- [ ] Both global (100 req/min/IP) and auth-specific (10 req/min/IP) limits use the Redis partition.
- [ ] Integration test: two replicas behind a test harness; 11 auth requests across both → 11th is `429`.
- [ ] On Redis outage, falls back to per-instance `FixedWindowLimiter`; logs Warning `"rate-limit fallback: per-instance"` every 30 s while degraded.

## Technical Notes

```lua
-- Lua script for atomic increment + ttl set
local current = redis.call('INCR', KEYS[1])
if current == 1 then
  redis.call('EXPIRE', KEYS[1], ARGV[1])
end
return current
```

- Key format: `rl:{partition}:{ip}:{window-bucket}`.
- TTL = window length; window bucket = floor(`now / window`).

## Dependencies

### Requires
- 002-two-level-cache (shares multiplexer + outage handling pattern)

### Enables
- Hardened rate limiter

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Clock skew across replicas | Bucketing on `now / window` keeps within ±1 window; acceptable |
| Redis cluster failover | SDK reconnects; counters preserved post-recovery (AOF + replication) |

## Out of Scope

- Sliding-window / token-bucket variants (fixed window matches today's behaviour).
