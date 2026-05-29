---
unit: 002-archive-retention
bolt: 052-archive-retention
stage: model
status: complete
updated: 2026-05-29T12:00:00Z
---

# Static Model — Archive Retention

## Bounded Context

Still **Order Photo Archive** (introduced in bolt 051). Unit 001 owns the *constructive*
half — bytes flow from the local server into the cloud when an order is paid. **This unit
owns the destructive half** — bytes flow *out of* the cloud on two schedules:

1. **The original** (`uploads/…`) is deleted as soon as production completes. Driven by the
   order status machine.
2. **The large preview and thumbnail** (`previews/…`, `thumbs/…`) are deleted after the
   retention window. Driven by a periodic background job.

Together with bolt 051 this closes the data-minimization lifecycle the intent promised:
unpaid photos never reach the cloud (bolt 043 + 051); the original lives in the cloud only
between Paid and Shipped; the customer-facing preview tier lives only for the retention
window (default 12 months from order completion).

We do not own:
- The bytes themselves (`IStorageRouter` / `IStorageService` — bolt 043).
- The cloud writes that put them there (`OrderPhotoPromoter` — bolt 051).
- The customer-facing read path (unit 003 — bolt 053).

We add **no new entities**. Every operation in this bolt is a destructive update on the
existing `Upload` row.

## Domain Entities

| Entity | Properties | Business Rules |
|--------|------------|----------------|
| **Upload** *(existing — bolt 051 schema)* | Fields touched: `FilePath` (nulled by *original purge*), `OriginalPurgedAt` (set by *original purge*), `LargePreviewPath` + `ThumbnailPath` (both nulled by *retention cleanup*). `StorageLocation` is **not** changed here — it stays `Cloud`. | A row may exist in three observable post-promotion states: **Archived** (`FilePath != null` ∧ both preview keys non-null), **OriginalPurged** (`FilePath == null` ∧ `OriginalPurgedAt != null` ∧ preview keys still non-null), **Expired** (all three blob keys null). The states form a forward-only progression — Expired never reverts to OriginalPurged, etc. |
| **Order** *(existing — referenced, not modified)* | The status transition to the configured "production complete" status (default `Shipped`) triggers original-purge. The order's *completion timestamp* (see Stage-2 question) anchors the retention window for its uploads. | An order whose `Status` is `PaymentFailed` or `Cancelled` is **never** purged on the production-complete trigger — it never reached that status. |

## Value Objects

| Value Object | Properties | Constraints |
|--------------|------------|-------------|
| **PurgeOutcome** *(per-order)* | `Purged: int`, `Skipped: int`, `Failed: int`, `BytesFreed: long` | Monotonic within a run. `Skipped` = upload already in the target state (original already null, or expired blob keys already null). `Failed` = cloud delete error; the row stays in its pre-attempt state so the next sweep retries. |
| **RetentionAnchor** *(per upload)* | The order-side timestamp from which the retention window is measured. **Open question for Stage 2** — see §"Open Questions" below. | Must be a timestamp on the parent `Order`. Same value for every upload in a given order. |
| **RetentionWindow** *(global, configurable)* | `TimeSpan` (months) | Default 12 months; configurable down to as low as 1 month for testing. Applied as `Order.{anchor} + RetentionWindow < UtcNow ⇒ eligible`. |
| **ProductionCompleteStatus** *(global, configurable)* | one of `OrderStatus.{Shipped, Delivered}` | Default `Shipped`; configurable to allow ops to defer original-purge until delivery confirmation if that ever becomes useful. Never `Paid` or `Printing` — those are pre-completion. |

## Aggregates

| Aggregate Root | Members | Invariants |
|----------------|---------|------------|
| **Order** *(existing)* | its `Items` (each with `Upload`) | **Original purge is order-driven**: on entry to the configured production-complete status, every upload of that order has its original purged in one go. Partial failure across the order's uploads is allowed — same model as 051's per-upload atomicity, inverted for delete. |
| **Upload** *(existing — bolt 051)* | `FilePath`, `LargePreviewPath`, `ThumbnailPath`, `OriginalPurgedAt`, `StorageLocation` | **Confirmed-Delete-Then-Update** (mirror of ADR-011's promotion rule, inverted for delete): cloud `DeleteAsync` first → DB row update second. A crash between cloud delete and row update leaves the cloud bytes gone but the row still claiming them; the next sweep observes `FilePath != null` on a Shipped order, attempts delete (idempotent), and updates the row. **No silent data loss case** — only "the row says we still have it, we'll re-check" cases. |

## Domain Events

| Event | Trigger | Payload |
|-------|---------|---------|
| **OrderEnteredProductionComplete** *(new — observational)* | Order status transitions to the configured production-complete status | `OrderId`, `EnteredAt`. Drives original-purge for that order. The status-machine call site is the actual trigger; this event is the conceptual handle. |
| **OriginalPurged** *(new — observational)* | An upload's original has been deleted from cloud and the row's `FilePath` nulled + `OriginalPurgedAt` set | `OrderId`, `UploadId`, `OriginalSizeBytes`, `PurgedAt`. Information log level. No in-process subscribers in this bolt. |
| **OriginalPurgeFailed** *(new — observational)* | Cloud delete failed permanently after Polly retries | `OrderId`, `UploadId`, `LastError`, `FailedAt`. Error log level. The next sweep retries. |
| **ArchiveExpired** *(new — observational)* | An upload's large preview + thumbnail have been deleted from cloud and the row's `LargePreviewPath` + `ThumbnailPath` nulled | `UploadId`, `OrderId`, `ExpiredAt`, `RetentionWindowMonths`. Information log level. |

All events are **log-emitted observations**, not bus messages. The same reasoning as
bolt 051: pinning a vocabulary that downstream code can subscribe to later, without
introducing an event bus this bolt doesn't need.

## Domain Services

| Service | Operations | Dependencies |
|---------|------------|--------------|
| **IOriginalPurger** *(new — story 001)* | `PurgeOrderOriginalsAsync(orderId, ct)` → `PurgeOutcome` | `IStorageRouter` (Cloud delete), `DbContext` (row update), `IOptions<ArchiveSettings>`, `ILogger`. **Idempotent** per upload: skips uploads whose `FilePath` is already null. Skips entirely if the cloud tier is off. |
| **ArchiveRetentionJob** *(new — story 002, BackgroundService)* | Periodic `ExecuteAsync` loop on a `PeriodicTimer`; per tick, query expired uploads and delete their preview + thumb keys. Bounded batch size. | `IServiceScopeFactory`, `IStorageRouter`, `DbContext`, `IOptions<ArchiveSettings>`, `ILogger`. **Idempotent** per upload: skips uploads whose preview/thumb keys are already null. |
| **ArchiveSettings** *(new — configuration)* | `Enabled: bool`, `PurgeOriginalAtStatus: OrderStatus` (default `Shipped`), `RetentionMonths: int` (default 12), `JobIntervalHours: int` (default 6), `BatchSize: int` (default 500) | `IValidateOptions<ArchiveSettings>` fails fast on bad values. |

**Note on the *promoter* vs *purger* shape:** bolt 051 has a producer/consumer queue
because promotion has bursty triggers (a wave of orders paying at the same time can
arrive on the webhook) and needs to drain off the hot path. **This bolt does not need a
queue.** Story 001's purge fires synchronously from an admin-driven status transition
(low-frequency, blocking is acceptable); story 002's cleanup is a slow periodic sweep.
Same channel infrastructure would be over-engineering for this surface.

## Repository Interfaces

| Repository | Entity | Methods |
|------------|--------|---------|
| **`ApplicationDbContext.Orders` / `.Uploads`** *(existing)* | Order, Upload | Story 001: `Orders.Include(o => o.Items).ThenInclude(i => i.Upload).First(o => o.Id == orderId)`. Story 002: `Uploads.Where(u => u.LargePreviewPath != null && Order.<anchor> < cutoff)` — exact LINQ shape settled in Stage 2 once the anchor is chosen. No new repository abstraction; direct `DbContext` matches the surrounding codebase. |

## Ubiquitous Language

| Term | Definition |
|------|------------|
| **Purge** | The act of deleting one or more cloud blobs and nulling the corresponding `Upload` columns. Always destructive; never archives, never moves. |
| **Original purge** | The bolt-052 story-001 operation: delete only the *original* blob (`FilePath`), keep the preview + thumbnail. Triggered by the order-status transition to *production complete*. |
| **Retention cleanup** | The bolt-052 story-002 operation: delete the *large preview + thumbnail* (`LargePreviewPath`, `ThumbnailPath`), keep nothing image-shaped from the upload. Triggered by the periodic job once the retention window has elapsed. |
| **Production complete status** | The configurable order status at which printing has finished. Default `Shipped`; configurable to `Delivered`. The single trigger for original-purge. |
| **Retention window** | The configurable duration from a point-in-time on the order after which the preview + thumbnail are eligible for deletion. Default 12 months. |
| **Retention anchor** | The timestamp on the order from which the retention window is measured. Open question — see §"Open Questions". |
| **Archived** | The post-promotion, pre-purge state of an upload: `StorageLocation = Cloud`, `FilePath` and both preview keys all non-null. |
| **Original-purged** | The state after story 001: cloud original gone, `FilePath` null, `OriginalPurgedAt` set, preview + thumbnail still in cloud. |
| **Expired** | The state after story 002: all three blob keys null. The row remains so order-history (unit 003) can render "your photos are no longer available" cleanly rather than 404. |
| **Confirmed-Delete-Then-Update** | Bolt 052's mirror of ADR-011: cloud delete first (the durability boundary becomes "no longer there"), then row update. Crashes between leave the row claiming a blob that may already be gone; next sweep observes and re-converges. |
| **Bounded archive** | The property this bolt guarantees: no upload's blobs live in the cloud beyond `PurgeOriginalAtStatus + (anchor + RetentionWindow)`. Cost and privacy exposure both bounded. |

## Story Coverage

- ✅ **001-purge-original-on-shipped** → `IOriginalPurger` modeled as a domain service; hook into `OrderStatusMachine.Transition` (or a thin wrapper at the call site) on entry to `PurgeOriginalAtStatus`; per-upload idempotency via `FilePath == null` short-circuit; Confirmed-Delete-Then-Update invariant pinned on the Upload aggregate.
- ✅ **002-retention-cleanup-job** → `ArchiveRetentionJob` `BackgroundService` modeled; query shape sketched; per-upload idempotency via `LargePreviewPath == null && ThumbnailPath == null` short-circuit; batch size + interval are configuration.

## Open Questions for Technical Design (Stage 2)

1. **Retention anchor.** The most consequential choice this bolt makes. Three reasonable options:
   - **`Order.PaidAt`** — always set on every reachable post-Paid status; never re-set; trivial to query against. **Downside**: the customer's 12-month clock starts when they paid, not when they got their photos. Slow fulfilment shortens the customer-visible archive lifetime.
   - **The status transition timestamp into the `PurgeOriginalAtStatus`** — closer to "when the customer got their photos" but the `Order` model doesn't have a dedicated `ShippedAt` / `DeliveredAt` column today. Either we **add one** (clean) or we approximate via `Order.UpdatedAt` filtered by current status (fragile — `UpdatedAt` is touched on any update).
   - **`Order.UpdatedAt`** filtered post-hoc — works without schema change but drifts on any later mutation; loses the "12 months from completion" semantic.
   - **Recommended for Stage 2:** add a `CompletedAt` column to `Order`, set it when the status transition to `PurgeOriginalAtStatus` fires. Single migration; clean semantic; story 001 already touches this transition so the write is free.
2. **Where the production-complete transition fires in code.** Probably `AdminOrdersController` (admin marking an order shipped) — needs confirmation by reading. If yes, the purge hook lives there as `await _purger.PurgeOrderOriginalsAsync(...)` right after `SaveChangesAsync`. Same shape as the bolt-051 enqueue hook in `WebhooksController`.
3. **Synchronous vs queued purge for story 001.** Original-purge fires from an admin action — single-user, low-frequency, blocking-OK. Recommend synchronous; no need to import the bolt-051 queue here.
4. **Should we soft-delete the `Upload` row** once all blobs are gone? Currently `Upload.DeletedAt` exists (the `UploadCleanupJob` soft-delete column). Unit 003 will need to render "this upload no longer exists" — leaving the row gives us metadata for that. **Recommend: keep the row, do not touch `DeletedAt`.** Story 002 explicitly says metadata is retained.
5. **Retention job cadence + batch size.** Daily-ish per the story; 6 h interval, 500-row batch matches `UploadCleanupJob`. Confirm in Stage 2.
6. **Cloud-tier-off behavior.** Same as bolt 051: the original-purger and the retention job both check `_router.CloudEnabled`. The retention job logs Information ("disabled") and exits; the purger logs Error if called when the cloud tier is off (a paid order in this state shouldn't exist, but defence in depth).
7. **Failed deletes — high-level retry.** The retention job re-runs every 6 h naturally — a transient failure becomes the next sweep's first attempt. Story 001's purge is one-shot per status transition; if it fails, the recovery scan from bolt 051 won't help (that scanner looks for Local uploads, not "FilePath still set on a shipped order"). Stage 2 decides: extend the recovery scanner to also re-enqueue stuck purges, or rely on the next retention-job tick to surface stuck originals via a Warning log.
8. **Postgres-typed migration if we add `CompletedAt`.** Same gotcha as bolt 051 — the SQLite scaffold generates `INTEGER`, needs hand-edit to `timestamp with time zone` for Postgres. Stage 4 concern; flagged here so it isn't missed.
