---
unit: 001-order-photo-promotion
bolt: 051-order-photo-promotion
stage: model
status: complete
updated: 2026-05-29T09:30:00Z
---

# Static Model — Order Photo Promotion

## Bounded Context

**Order Photo Archive** — the part of the order lifecycle responsible for taking the photos a customer ordered and moving them from the ephemeral, on-server staging area into the durable cloud archive that backs the customer's order history. This bolt owns the **promotion** half of that lifecycle (`Local → Cloud`); unit 002 owns retention and original-purge; unit 003 owns customer-facing viewing.

Two adjacent contexts are referenced but not owned here:

- **File Storage** (bolt 043) — supplies the byte-persistence primitives (`IStorageService`, `IStorageRouter`, `StorageKeys`, `StorageLocation`) the promoter calls. This bolt **uses** that boundary; it does not modify it.
- **Order** (existing) — supplies the `Paid` transition that triggers promotion. We hook into the existing status machine; we do not redefine it.

The new domain concept this bolt introduces is **photo promotion as a first-class lifecycle step** of an order — it is not a side-effect of payment, it is its own thing that runs *after* payment, asynchronously, with its own success/failure surface and its own crash-recovery posture.

## Domain Entities

| Entity | Properties | Business Rules |
|--------|------------|----------------|
| **Upload** *(existing aggregate root — touched, not introduced)* | Adds `LargePreviewPath: StorageKey?` and `OriginalPurgedAt: DateTimeOffset?`; already has `FilePath`, `ThumbnailPath`, `StorageLocation` (bolt 043). | `LargePreviewPath` is populated by the promoter when the Cloud large-preview write succeeds. It is **always null while** `StorageLocation = Local` (the large preview is a Cloud-only artifact — we don't generate one until the order is paid). `OriginalPurgedAt` is **not set by this bolt** (it belongs to unit 002 — purge-after-printing); the *column* is added here because it is the same migration as `LargePreviewPath`. After successful promotion an Upload must satisfy: `StorageLocation = Cloud` ∧ `FilePath`, `ThumbnailPath`, `LargePreviewPath` all non-null ∧ all three objects exist at those Cloud keys ∧ no local copy remains. Pre-promotion: `StorageLocation = Local` ∧ `LargePreviewPath` null ∧ local files present. There is **no half-state on disk** — see the *Confirmed-Write-Then-Delete* invariant below. |
| **Order** *(existing aggregate root — referenced, not modified)* | Existing status field reaches `Paid` on Stripe webhook / EuPlatesc IPN. | The Paid transition is the **only** trigger for promotion in production flow (backfill is the ops-side equivalent for pre-existing data). An order in any other status MUST NOT have its uploads promoted. |

## Value Objects

| Value Object | Properties | Constraints |
|--------------|------------|-------------|
| **StorageKey** *(inherited from bolt 043)* | the relative key string | Carried unchanged. The promoter writes to `uploads/{yyyy}/{MM}/{uploadId:N}{ext}` (original), `previews/{uploadId:N}.jpg` (large preview), `thumbs/{uploadId:N}.jpg` (thumbnail) — all produced via `StorageKeys.*`. |
| **PromotionOutcome** | `Promoted: int`, `Skipped: int`, `Failed: int`, `TotalBytes: long` (per-order or per-run aggregate) | Monotonic within a run. `Skipped` = upload already at `StorageLocation = Cloud` (idempotent re-entry). `Failed` = at least one of the three Cloud writes failed for that upload (counted; the upload stays Local and is retried on the next sweep / next manual trigger). `TotalBytes` sums the original sizes successfully promoted. Used for log-line summary and for the backfill CLI's final report. |
| **PromotionJob** | `OrderId: Guid`, `EnqueuedAt: DateTimeOffset`, `Attempt: int` (defaults 1) | Conceptual envelope around a queued work item. In Stage 2 this is likely realized as a `Channel<PromotionJob>` payload; the model only requires that "the order id and an attempt count travel together so retries can be bounded." Attempt > N (configurable, default 5) → give up; the upload(s) stay Local, an Error is logged, and only an operator (or the recovery scan + manual re-trigger) can drive it forward. |

## Aggregates

| Aggregate Root | Members | Invariants |
|----------------|---------|------------|
| **Order** *(existing)* | its `Uploads` (existing relationship) | **Order-level idempotency**: promoting an already-fully-promoted order is a no-op (returns immediately, `Skipped == upload count`). Partial promotion is allowed — if 4 of 6 uploads promote and 2 fail, the 4 stay promoted, the order's overall status does not regress, and the 2 failures are retried on the next attempt. The order itself never has a "promoted" status field — the property is derived: `Order.IsArchived ⇔ all uploads have StorageLocation = Cloud`. |
| **Upload** *(existing)* | `FilePath`, `ThumbnailPath`, `LargePreviewPath`, `StorageLocation`, `OriginalPurgedAt` | **Per-upload atomicity**: the row update that flips `StorageLocation` from `Local` to `Cloud` AND sets the three path columns is one EF SaveChanges. Local files are deleted **after** that SaveChanges returns success. Therefore the row is the source of truth: if the row says Cloud, the bytes are in Cloud; if the row says Local, the local files exist. **Confirmed-Write-Then-Delete**: cloud writes (original, preview, thumb) → DB update (`StorageLocation = Cloud` + three keys) → local file deletes. A crash at any point either leaves Local intact (operation is retried as if nothing happened) or leaves Cloud + Local both present (the next sweep observes `StorageLocation = Cloud` + lingering local files and *just deletes the local files*; the cloud writes are not re-done). |

## Domain Events

| Event | Trigger | Payload |
|-------|---------|---------|
| **OrderPaid** *(existing — already emitted by the payment hook)* | Order transitions to `Paid` via Stripe webhook or EuPlatesc IPN | `OrderId`, `PaidAt`. The promoter **subscribes** to this; it does not own it. The exact subscription mechanism (in-process event handler, status-machine post-action, or a thin "enqueue on Paid" call site embedded in the webhook handler) is a Stage 2 decision. |
| **UploadPromoted** *(new — observational)* | An upload's three Cloud writes have succeeded, its row has been updated, and its local files have been deleted | `OrderId`, `UploadId`, `OriginalSizeBytes`, `PromotedAt`. Emitted at Information log level; no in-process subscribers in this bolt. Reserved for future operational dashboards / audit. |
| **UploadPromotionFailed** *(new — observational)* | An upload reached its retry ceiling without all three Cloud writes succeeding | `OrderId`, `UploadId`, `Attempt`, `LastError: string`, `FailedAt`. Emitted at Error log level. No in-process subscribers; the upload remains `StorageLocation = Local` and is eligible for the recovery scan / a manual re-trigger. |

Why these are *events* and not just log calls: they pin the contract for unit 002 (retention) and unit 003 (viewing) to subscribe to in the future without re-deriving "is this order archived?" from raw column values. They are emitted *into the log surface only* in this bolt — a real bus is not introduced.

## Domain Services

| Service | Operations | Dependencies |
|---------|------------|--------------|
| **IOrderPhotoPromoter** *(new — the orchestrator)* | `PromoteOrderAsync(orderId, ct)` → `PromotionOutcome` · `EnqueueAsync(orderId, ct)` (fire-and-forget queue write) | `IStorageRouter` (read Local, write Cloud, delete Local), `IImageProcessor` (for large preview), `DbContext` (atomic row update), `ILogger`. **Idempotent**: per-upload skip on `StorageLocation = Cloud`; per-order re-entry safe. |
| **OrderPhotoPromotionWorker** *(new — the queue runner)* | `BackgroundService` consuming a bounded `Channel<PromotionJob>`; bounded concurrency (configurable, default 4 orders in flight); drains on shutdown with a CancellationToken. | `IOrderPhotoPromoter`, hosted-service lifecycle. The worker is the **only** in-process subscriber to the queue — there is no separate "schedule a retry" path. Retries happen by re-enqueueing the same `PromotionJob` with `Attempt + 1`. |
| **PromotionRecoveryScanner** *(new — startup self-heal)* | On application start, find orders in `Paid` (or beyond) that still have at least one upload with `StorageLocation = Local` and enqueue them | `DbContext` (query), `IOrderPhotoPromoter.EnqueueAsync`. Closes the window between "payment hook fires → queue write" and "promoter actually finishes." If we crashed mid-queue or mid-promotion, this rebuilds the queue from durable state on the next boot. Runs once on startup; not periodic. |
| **IImageProcessor** *(existing — extended)* | Adds `GenerateLargePreviewAsync(Stream source, CancellationToken ct)` returning a `~2000 px` long-edge, q85 JPEG stream | Same dependencies as the existing thumbnail path. Reuses the bolt-042 decompression-bomb guard (`MaxDecodeDimension`). **No upscale** — images smaller than 2000 px on the long edge pass through at native size. |
| **BackfillCommand** *(new — CLI verb)* | `dotnet run -- backfill-archive [--dry-run]` enumerates Paid orders with `StorageLocation = Local` uploads and runs `IOrderPhotoPromoter.PromoteOrderAsync` on each | `IOrderPhotoPromoter`, `DbContext`, `ILogger`. Reuses the promoter — **same code path** as the live worker. `--dry-run` prints what would be promoted without writing. |

## Repository Interfaces

| Repository | Entity | Methods |
|------------|--------|---------|
| **`ApplicationDbContext.Orders` / `.Uploads`** *(existing)* | Order, Upload | The promoter loads `Order` + its `Uploads`, filters on `StorageLocation = Local`, updates `Upload` rows atomically. The recovery scanner and backfill command both query `Orders.Where(o => o.Status == Paid && o.Uploads.Any(u => u.StorageLocation == Local))`. No new repository interface is added — direct DbContext is consistent with the rest of the codebase. |

## Ubiquitous Language

| Term | Definition |
|------|------------|
| **Promotion** | The act of moving an upload's bytes from the local-disk tier to the cloud tier, generating the large preview en route, and updating the upload row. Driven by an order reaching `Paid`. |
| **Large preview** | The ~2000 px long-edge, q85 JPEG generated during promotion and stored at `previews/{uploadId}.jpg`. The customer-facing "full view" representation in the order history (unit 003). Not the original, not the thumbnail — a third sibling artifact. |
| **Original** | The unmodified file the customer uploaded. Lives at `FilePath`. Promoted to Cloud at `uploads/{yyyy}/{MM}/{uploadId}{ext}`. Purged post-printing by unit 002 (the column `OriginalPurgedAt` is the audit field for that). |
| **Thumbnail** | The bolt-042 thumbnail (`thumbs/{uploadId}.jpg`). Already exists on disk for any upload that has been previewed; promotion uploads the existing bytes to Cloud (no regeneration). If the thumb cache miss happened to be filed never, the promoter generates it just-in-time. |
| **Confirmed write** | A cloud write that has returned success from the SDK (no exception, no 5xx). The local copy of *that artifact* is eligible for deletion **only after** the matching confirmed write succeeds AND the row update with the new cloud keys has been persisted. |
| **Promote-on-Paid** | The trigger contract: promotion runs because an order is `Paid`, period. Never on add-to-cart, never on order-placed-but-unpaid, never on guest browse. |
| **Recovery scan** | Startup-time query that re-enqueues any Paid orders whose uploads are still Local. Closes the crash window between "Paid" and "promoter finished." |
| **Backfill** | The ops-side equivalent of promote-on-Paid for orders that were already Paid before this feature shipped. Single CLI command, idempotent, resumable. **Supersedes** intent-019 story 003 (the broad "migrate everything" tool, which was the wrong premise under two-tier). |
| **Idempotent (per upload)** | Re-processing an upload that is already `StorageLocation = Cloud` is a no-op — the promoter checks the column first and returns `Skipped`. |
| **Recovery sweep** | Synonym for *recovery scan* in some user stories; same concept. |
| **Bounded concurrency** | The worker processes at most N orders in flight (default 4). Within an order, uploads are processed sequentially — keeps the per-order log trail readable and bounds memory (one image-decode buffer per worker, not per upload). |

## Story Coverage

- ✅ **001-archive-schema** → `Upload.LargePreviewPath`, `Upload.OriginalPurgedAt` value columns modeled on Upload aggregate; same migration covers both.
- ✅ **002-large-preview-generation** → `IImageProcessor.GenerateLargePreviewAsync` added to the domain services table; constraints (no upscale, q85, decompression-bomb guard, `previews/` prefix) captured.
- ✅ **003-promote-on-paid** → `IOrderPhotoPromoter` + `OrderPhotoPromotionWorker` + `PromotionRecoveryScanner` modeled; per-upload atomicity, confirmed-write-then-delete, idempotency, retry-with-attempt-ceiling, and the recovery scan all stated as invariants.
- ✅ **004-backfill-paid-orders** → `BackfillCommand` modeled as a CLI entry point that reuses `IOrderPhotoPromoter` (same code path, not a parallel implementation).

## Open Questions for Technical Design (Stage 2)

1. **Where exactly does Paid fire?** The webhook hot path in `PaymentsController` (Stripe) and the EuPlatesc IPN handler both transition the order. Pick one *single* call site for "enqueue promotion" (idempotent enqueue is fine — duplicates dedup at the per-upload level), or fan out from both — confirm during Stage 2 by reading the existing payment surface.
2. **In-process `Channel<T>` vs. a durable work table.** Channel is simpler and the recovery scanner already provides crash recovery. A DB work table buys per-order retry budget and an operator-visible queue at the cost of more code. We will likely choose Channel + recovery scan; capture as an ADR if the rationale isn't obvious.
3. **Retry policy.** Polly is already wired in `S3StorageService` for transient S3 errors. The promoter's retry concern is *higher-level* — "this whole order failed; try again in a minute." Stage 2 decides: re-enqueue with backoff (`Task.Delay` before write) vs. a periodic sweep that picks up `Attempt < N` failures. Either is fine; the model is silent on the mechanism.
4. **Thumbnail regeneration during promotion.** Bolt 042 only writes a thumbnail when the preview endpoint is hit. An order can be paid without any of its uploads ever having been previewed (rare, but possible). Stage 2 confirms: promoter calls `_imageProcessor.GenerateThumbnailAsync` (or equivalent) when the local thumbnail is missing, *before* the cloud upload.
5. **Recovery scan scope.** Just "Paid orders with Local uploads," or broader (any order whose status sits past Paid)? Likely the former — model says "Paid is the trigger." Confirm in Stage 2.
6. **Backfill argument shape.** `dotnet run --project src/PhotoPrint.API -- backfill-archive --dry-run` is the story's literal acceptance text. Stage 2 chooses the actual command-parsing approach (manual `args[]` sniff vs. a small `System.CommandLine` setup) and whether the API host stays up while the verb runs or runs as a separate hosted-service.
7. **Behavior when `Storage:Provider = Local` (cloud tier off).** Promotion is meaningless without a Cloud adapter. The promoter checks `IStorageRouter.CloudEnabled` and: (a) refuses to enqueue at all and logs Warning, or (b) silently no-ops per upload. Stage 2 picks one; the safe default is (a) — fail loudly, because a Paid order whose photos *weren't* archived is the kind of silent data loss we want to catch.
8. **Migration target — Postgres vs. SQLite.** Story 001 requires it applies cleanly on both. The same EF migration pattern bolt 043 used (`AddUploadStorageLocation`) works; Stage 2 verifies no provider-specific quirks for `nullable text(512)` and `nullable timestamptz`.
