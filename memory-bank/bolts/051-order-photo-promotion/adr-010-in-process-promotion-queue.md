---
bolt: 051-order-photo-promotion
created: 2026-05-29T10:15:00Z
status: accepted
superseded_by: null
---

# ADR-010: In-Process `Channel<T>` + Startup Recovery Scan Instead of a Durable Work-Queue Table

## Context

Bolt 051 introduces **promotion-on-paid**: when an order reaches `OrderStatus.Paid`, its photos must be uploaded from the local deployment server's disk to the cloud archive (original + ~2000 px large preview + thumbnail). Two facts shape the queueing problem:

1. The **webhook hot path** (`WebhooksController.HandleStripePaymentSucceededAsync`, `LegacyProcessorIpnAsync`) must return in milliseconds — payment providers consider slow webhooks failed and retry. So promotion runs asynchronously off the hot path.
2. We are deployed on a **single VM** (bolt 040 recommendation; multi-replica is a future concern, and pre-payment uploads are bound to one VM by ADR-008 anyway).

We needed a mechanism to carry "this order needs promotion" from the webhook to a background worker. Two shapes were viable:

- **Durable work-queue table** — `PromotionJobs(OrderId, Status, Attempt, NextAttemptAt, LastError)` in Postgres, polled by the worker.
- **In-memory channel** — `System.Threading.Channels.Channel<PromotionJob>`, written by the webhook, read by a `BackgroundService`. Survives nothing on its own; relies on the recovery scan for crash-safety.

## Decision

**Use `System.Threading.Channels.Channel<PromotionJob>` for the live queue, and rebuild it from durable upload state at startup via `PromotionRecoveryScanner`.**

- `IPromotionQueue` wraps a `Channel<PromotionJob>` (unbounded, single-reader, multi-writer).
- `OrderPhotoPromotionWorker : BackgroundService` is the only reader; concurrency-bounded internally by a `SemaphoreSlim`.
- `PromotionRecoveryScanner : IHostedService` runs once on `StartAsync`, queries `Orders.Where(Status ≥ Paid && Status != Cancelled).Where(o => o.Items.SelectMany(i => i.Uploads).Any(u => u.StorageLocation == Local))`, and re-enqueues each match. Closes every crash window between webhook receipt and successful promotion.
- The webhook handlers call `await _promoter.EnqueueAsync(order.Id, ct)` — a near-instant in-memory write — as the final step after `_db.SaveChangesAsync`.
- Failed promotions re-enqueue themselves with backoff up to 5 attempts; on exhaustion the upload stays `StorageLocation = Local` and the next recovery scan will retry on the next deploy.

## Rationale

The work-queue table buys two things: **process-crash survival** of in-flight jobs and **operator visibility** (`SELECT * FROM PromotionJobs WHERE Status = 'Failed'`). We get both differently:

- **Process-crash survival:** the recovery scanner re-derives "what needs promoting" from the upload rows themselves. The DB upload row is *already* the source of truth — `StorageLocation = Local` on a paid order means "needs promotion," full stop. A separate jobs table would duplicate that signal, with the attendant drift risk (job marked Done but upload still Local, or vice versa).
- **Operator visibility:** `Orders.Where(Status >= Paid).Include(o => o.Uploads).Where(u => u.StorageLocation = Local)` is the same query the recovery scan runs — operators can run it ad-hoc. Failed promotions are surfaced via structured logs (`UploadPromotionFailed` Error events).

The savings are substantial:

- No new migration (would need `PromotionJobs` schema + indices).
- No orphan-row cleanup (work table grows forever unless we add a sweeper).
- No two-source-of-truth bug class.
- Worker code reads from a `Channel<T>` (4 lines) instead of a polling loop with row-locking (~50 lines, plus migrations, plus tests).

The trade-off accepted: in a multi-VM future, an in-process channel doesn't shard across nodes. We confront that when we get there (likely by routing payment webhooks to one node, or by upgrading to a distributed queue at the same time as bolt 046's Redis introduction).

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|-------------|------|------|--------------|
| **In-memory Channel + recovery scan (chosen)** | Minimal code; recovery scan is the durability story; no new table; single source of truth (Upload rows) | Doesn't shard across multiple API instances; jobs in flight at crash are re-done from the top | **Accepted** for the single-VM target; the recovery scan's re-enqueue is cheap because the per-upload Cloud-check skips already-promoted work |
| Durable `PromotionJobs` table | Survives crashes natively; operator-friendly `SELECT` | Two sources of truth (jobs vs. uploads) drift over time; orphan rows; new migration + tests; polling cost | Rejected — duplicates what `Upload.StorageLocation` already tells us, with strictly more code |
| External queue (Redis Streams, RabbitMQ) | Distributes naturally across replicas | Heavy infrastructure dependency for a feature that only needs to deliver one message ID per paid order; not present in current stack | Premature; revisit when bolt 046 adds Redis |
| `IHostedService` polling loop on `Upload.StorageLocation = Local` every N seconds (no queue at all) | Even less code than chosen | High constant DB load; large latency between Paid and promotion start; can't easily back-pressure | Rejected — the channel is essentially free, latency matters for customer perception |

## Consequences

### Positive

- **Code surface stays small.** Worker + recovery scanner + channel wrapper = ~150 lines of new code total, plus 2 lines in the webhook.
- **No new persistence concerns.** Zero migrations beyond the `LargePreviewPath`/`OriginalPurgedAt` columns story 001 already needs.
- **Trivially testable.** Channel is in-memory; worker is a `BackgroundService` whose `ExecuteAsync` can be invoked directly in tests with a fake `IOrderPhotoPromoter`.
- **Single source of truth.** `Upload.StorageLocation` is the only "is this done?" signal; impossible to diverge.

### Negative

- **In-flight jobs on a crash are re-tried from the top.** Acceptable because `PromoteUploadAsync` is per-upload idempotent (the `StorageLocation == Cloud` check skips already-promoted items, so the re-try only redoes whatever wasn't finished).
- **Multi-VM scale-out is blocked until the queue is replaced.** Recorded as a known future migration; ADR-008 already pins single-VM pre-payment serving as the current target.
- **Operator visibility of pending work is "go run this LINQ query against the DB," not "look at this table."** A small CLI verb could be added later if it proves painful; the recovery-scan query is already in the codebase to be reused.

### Risks

- **Risk**: the recovery scan query becomes expensive as the order table grows. **Mitigation**: it runs once per process start, on indexed columns (`OrderStatus`, `StorageLocation`). Even a million paid orders is a fast indexed scan.
- **Risk**: a future contributor adds a third producer (e.g. an admin "re-promote" endpoint) and forgets to make it idempotent. **Mitigation**: the promoter's per-upload `StorageLocation == Cloud` check absorbs duplicates from any producer; producers don't need to dedupe.
- **Risk**: someone adds a `BackgroundService` that *also* reads from the channel. **Mitigation**: `IPromotionQueue.Reader` is exposed only to the single worker via DI; not made public.

## Related

- **Stories**: 003-promote-on-paid, 004-backfill-paid-orders (intent 024).
- **Standards**: complements ADR-008 (two-tier storage); this ADR explains the *promotion mechanism* that ADR-008's `StorageLocation` enum exists to be flipped by.
- **Previous ADRs**: ADR-008 (two-tier storage and `IStorageRouter`); ADR-007 (caller-supplied keys — the promoter writes via `StorageKeys.*`, the same way `UploadService` does).
- **Future**: when bolt 046 (Redis / distributed state) lands, revisit whether the channel should be replaced with a Redis-Streams-backed `IPromotionQueue`. This ADR remains accepted; a future ADR would supersede it.
