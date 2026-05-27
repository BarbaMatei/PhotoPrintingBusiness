---
unit: 001-redis-backplane
intent: 021-distributed-state-redis
phase: inception
status: draft
created: 2026-05-25T10:40:00Z
updated: 2026-05-25T10:40:00Z
---

# Unit Brief: Redis Backplane

## Purpose

Stand up Redis as a tier-1 dependency: SignalR fan-out, shared cache, distributed rate-limiter, health check.

## Scope

### In Scope
- `Microsoft.AspNetCore.SignalR.StackExchangeRedis` wiring
- `ITwoLevelCache` abstraction + Redis pub/sub invalidation
- Custom Redis rate-limit partition for .NET 8 `RateLimiter`
- `/health` Redis probe + Compose updates

### Out of Scope
- Migration of all `IMemoryCache` callers (only the catalogue + locker callers in this bolt)
- Multi-region Redis topology

---

## Story Summary

| Story ID | Title | Priority |
|----------|-------|----------|
| 001-signalr-redis-backplane | SignalR `.AddStackExchangeRedis(...)` + fan-out test | Must |
| 002-two-level-cache | `ITwoLevelCache` with L1 memory + L2 Redis + pub/sub invalidation | Must |
| 003-distributed-rate-limiter | Redis-backed rate-limit partitions with fallback | Must |
| 004-redis-health-and-compose | `/health` Redis probe + Compose service entries | Must |
