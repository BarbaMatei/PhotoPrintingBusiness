---
intent: 021-distributed-state-redis
phase: inception
status: complete
created: 2026-05-25T10:40:00Z
updated: 2026-05-25T10:40:00Z
source: docs/architecture-analysis-2026-05-25.md#9
priority_score: 17
---

# Requirements: Distributed State (Redis Backplane)

## Intent Overview

`AddSignalR()` and `AddMemoryCache()` are single-process. With two API replicas, admin SignalR clients only see notifications from the replica they connected to, the in-memory locker / catalog cache loses hit rate, and the in-process rate limiter is trivially bypassable by alternating replicas. This intent adds Redis as a tier-1 dependency and routes SignalR, cache, and rate-limit state through it.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Enable horizontal API scaling | Two replicas behind LB exhibit consistent SignalR + cache behaviour | Must |
| Restore intended rate-limit enforcement | 10 req/min auth rate enforced globally, not per-instance | Must |
| Improve product / locker cache hit rate on multi-instance | > 80% L2 hit rate after warmup | Should |

---

## Functional Requirements

### FR-1: Redis backplane for SignalR
- **Description**: `Microsoft.AspNetCore.SignalR.StackExchangeRedis` registered. `AdminOrderHub` notifications fan out across all replicas.
- **Acceptance Criteria**:
  - Two-replica integration test: client connected to replica A receives an event published by replica B.
  - Connection string from `Redis:ConnectionString`; AOF persistence + RDB documented in compose.
- **Priority**: Must
- **Related Stories**: US-021-1

### FR-2: Two-level cache (L1 in-memory + L2 Redis)
- **Description**: New `ITwoLevelCache` abstraction. L1 fast per-instance memory cache; L2 shared Redis. Read: L1 → L2 → loader. Write: both. Eviction: L2 invalidation message pub/sub notifies L1.
- **Acceptance Criteria**:
  - Existing in-process callers (`IMemoryCache` consumers for product catalog and locker list) migrated to `ITwoLevelCache`.
  - Cross-instance invalidation works (write to A → B's L1 entry purged within 1 s).
  - Misses route through to existing repository loaders.
- **Priority**: Must
- **Related Stories**: US-021-2

### FR-3: Distributed rate limiter
- **Description**: Replace in-process `FixedWindowLimiter` with a Redis-backed implementation (e.g. `AspNetCoreRateLimit` Redis store, or custom partition with `RedisRateLimiterPartition`).
- **Acceptance Criteria**:
  - 10 req/min auth limit enforced even when traffic alternates replicas.
  - Global 100 req/min/IP limit similarly enforced.
  - On Redis outage, falls back to per-instance limiter with a Warning log; never drops to "unlimited".
- **Priority**: Must
- **Related Stories**: US-021-3

### FR-4: Redis health check and Compose wiring
- **Description**: `/health` includes Redis ping. `docker-compose.yml` and `docker-compose.prod.yml` provision Redis with persistence.
- **Acceptance Criteria**:
  - `/health` returns 503 if Redis ping fails (configurable to "degraded" if app must continue).
  - Compose mounts a named volume `redisdata`; `appendonly yes` and `appendfsync everysec` set.
- **Priority**: Must
- **Related Stories**: US-021-4

---

## Non-Functional Requirements

### Performance
| Requirement | Metric | Target |
|-------------|--------|--------|
| Cache hit p95 | L1 hit | < 1 ms |
| Cache hit p95 | L2 hit | < 5 ms |
| SignalR fan-out latency | p95 | < 50 ms |

### Reliability
| Requirement | Metric | Target |
|-------------|--------|--------|
| Redis outage tolerance | Graceful degradation | App continues; rate limit and cache fall back to per-instance |
| Persistence | Restart durability | AOF + RDB |

### Security
| Requirement | Standard | Notes |
|-------------|----------|-------|
| Connection | TLS in production | `Redis:UseSsl=true`; managed Redis preferred |
| Auth | Password / ACL | Connection string carries credentials |

---

## Constraints

### Technical Constraints
- Must depend on intent 017 (Compose update) and intent 018 (secrets out of repo).
- Must not regress single-instance dev experience — Redis is optional locally with sensible defaults.

### Business Constraints
- Needed before any traffic-driven scale-out; otherwise lower priority.

---

## Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| Managed Redis (Upstash / Redis Cloud / DO Managed Redis) acceptable | Self-host required | Compose provides self-host path |
| Pub/sub invalidation message acceptable for cache eviction | Latency too high | Document accepted ≤ 1 s stale window |

---

## Open Questions

| Question | Owner | Due Date | Resolution |
|----------|-------|----------|------------|
| Q1: Custom Redis rate-limiter partition vs. AspNetCoreRateLimit library | Backend | 2026-08-01 | Pending — recommend custom (~50 LoC) to stay on .NET 8 native API |
| Q2: Where do Stripe idempotency keys live long-term? | Backend | 2026-08-01 | Pending — currently DB; consider Redis with TTL when L2 cache lands |
