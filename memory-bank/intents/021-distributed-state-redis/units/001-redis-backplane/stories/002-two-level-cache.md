---
id: 002-two-level-cache
unit: 001-redis-backplane
intent: 021-distributed-state-redis
status: draft
priority: must
created: 2026-05-25T10:40:00Z
assigned_bolt: 046-distributed-state-redis
implemented: false
---

# Story: 002-two-level-cache

## User Story

**As** the API
**I want** a two-level cache combining per-instance memory and shared Redis
**So that** I get fast local reads with cross-instance freshness

## Acceptance Criteria

- [ ] `ITwoLevelCache.GetOrSetAsync<T>(key, ttl, loader)` implemented.
- [ ] Read order: L1 → L2 → loader. On loader execution, both layers are populated.
- [ ] On write or explicit invalidation, a Redis pub/sub message clears the L1 entry on every replica within 1 s.
- [ ] Locker list (`/api/shipping/lockers`) and product catalogue (`/api/products`) migrated to `ITwoLevelCache`.
- [ ] Integration test asserts cross-instance invalidation.

## Technical Notes

- Use `IDistributedCache` (StackExchange Redis) as the L2; `IMemoryCache` as the L1.
- Pub/sub channel `cache:invalidate`; payload is the cache key.
- TTL on L1 ≤ TTL on L2 to bound staleness even on missed messages.

## Dependencies

### Requires
- 001-signalr-redis-backplane (shared multiplexer setup)

### Enables
- 003-distributed-rate-limiter

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Concurrent loader hits | Add `SemaphoreSlim` per key to dedupe loads |
| Loader throws | Cache nothing; surface exception to caller |
| Redis down | Bypass L2; L1 still functional |

## Out of Scope

- Compression of cached values.
