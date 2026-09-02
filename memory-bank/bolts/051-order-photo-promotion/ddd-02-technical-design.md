---
unit: 001-order-photo-promotion
bolt: 051-order-photo-promotion
stage: design
status: complete
updated: 2026-05-29T10:00:00Z
---

# Technical Design — Order Photo Promotion

## Architecture Pattern

**Producer/consumer with in-process channel + a single hosted worker**, augmented by a **startup recovery scan** that re-builds the queue from durable DB state.

```text
┌──────────────────────────────────────────────────────────────────────────┐
│                  ─── Producers ───                                       │
│                                                                          │
│  WebhooksController.HandleStripePaymentSucceededAsync                    │
│       ├── OrderStatusMachine.Transition(..., Paid)                       │
│       ├── _db.SaveChangesAsync()              ← Paid is durable          │
│       └── _promoter.EnqueueAsync(order.Id)    ← NEW: fire-and-forget     │
│                                                                          │
│  WebhooksController.LegacyProcessorIpnAsync                                    │
│       └── (same three calls, same order)                                 │
│                                                                          │
│  BackfillCommand (CLI)                                                   │
│       └── _promoter.PromoteOrderAsync(orderId)   ← direct call, no queue │
│                                                                          │
│  PromotionRecoveryScanner (IHostedService, StartAsync)                   │
│       └── Orders.Where(Status≥Paid && Uploads.Any(StorageLocation==Local)│
│           .ForEach(o => _promoter.EnqueueAsync(o.Id))                    │
│                                                                          │
│                                                                          │
│                  ─── Queue ───                                           │
│                                                                          │
│  Channel<PromotionJob>  (unbounded, single-reader)                       │
│       ├── PromotionJob { OrderId, Attempt = 1 }                          │
│                                                                          │
│                                                                          │
│                  ─── Consumer ───                                        │
│                                                                          │
│  OrderPhotoPromotionWorker : BackgroundService                           │
│       ├── ExecuteAsync: read channel, dispatch to N concurrent slots    │
│       └── For each job: _promoter.PromoteOrderAsync(orderId) + retry    │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

**Why this shape:**

- The webhook returns immediately after the channel write — payment providers see a millisecond-level ack.
- The worker is the only consumer; concurrency is bounded by a `SemaphoreSlim` (default 4) inside the worker loop.
- The recovery scan closes every crash window: if the API died between channel write and consumption, the next startup re-enqueues. *No durable queue table needed* (resolved Stage 1 Q2: chose Channel over work-table; recovery scan is the crash-safety story).

## Layer Structure

```text
┌───────────────────────────────────────────────────────────┐
│  Presentation                                             │
│    WebhooksController         (existing; 2 new lines:     │
│                                _promoter.EnqueueAsync)    │
│    Program.cs                 (CLI dispatch; 1 new check) │
├───────────────────────────────────────────────────────────┤
│  Application                                              │
│    IOrderPhotoPromoter        (orchestrator interface)    │
│    OrderPhotoPromoter         (orchestrator impl)         │
│    BackfillCommand            (CLI verb)                  │
├───────────────────────────────────────────────────────────┤
│  Domain / Infrastructure                                  │
│    OrderPhotoPromotionWorker  (BackgroundService)         │
│    PromotionRecoveryScanner   (IHostedService)            │
│    IPromotionQueue            (Channel writer/reader)     │
│    PromotionQueue             (Channel<PromotionJob>)     │
│    PromotionJob               (record)                    │
│    PromotionOutcome           (record)                    │
│    ImageProcessor             (extended)                  │
├───────────────────────────────────────────────────────────┤
│  Persistence                                              │
│    Upload                     (+ LargePreviewPath,        │
│                                  OriginalPurgedAt)        │
│    UploadConfiguration        (Fluent API additions)      │
│    AddUploadArchiveFields     (EF migration)              │
└───────────────────────────────────────────────────────────┘
```

## Hook Points (Resolved Stage 1 Q1)

After reading the existing payment surface:

- `OrderStatusMachine.Transition(order, OrderStatus.Paid)` is called from **exactly two places** — both in `WebhooksController`:
  - `HandleStripePaymentSucceededAsync` (private helper for `payment_intent.succeeded`).
  - `LegacyProcessorIpnAsync` (the `action == "0"` branch when current status is `AwaitingPayment`).
- Both call sites already perform `_db.SaveChangesAsync` immediately after the transition.

**Design choice:** Add `await _promoter.EnqueueAsync(order.Id, ct)` as the **last line** of each branch, after `SaveChangesAsync`. Two new lines total. Why not embed in `OrderStatusMachine.Transition`?

- The state machine is currently a pure static helper with zero DI dependencies — keeping it that way preserves test ergonomics (it's used in unit tests of order status logic).
- The promoter call has a side effect (channel write); the state machine should remain pure.
- Idempotent enqueue means duplicates are harmless even if a future code path adds a third call site that forgets.

**Idempotency of enqueue:** `EnqueueAsync` does **not** dedupe — the channel allows duplicates. The promoter's own per-upload `StorageLocation == Cloud` check absorbs duplicates at the only place that matters (the actual work). This keeps the queue trivially simple.

## API Design

**No new HTTP endpoints.** The preview endpoint already branches on `StorageLocation` (bolt 043). After promotion runs, the per-upload `StorageLocation = Cloud` flip causes the existing endpoint to return presigned URLs instead of streaming local bytes — zero controller changes needed.

**One new CLI verb** in `Program.cs` (story 004):

```text
dotnet run --project src/PhotoPrint.API -- backfill-archive [--dry-run]
```

- Detected by sniffing `args[0] == "backfill-archive"` before the normal `WebApplication.CreateBuilder` flow.
- Builds a minimal `Host` (no Kestrel) wired with `AddPhotoStorage`, `IOrderPhotoPromoter`, `DbContext`.
- Exits with code 0 on success, 1 on any per-order failure.

## Data Model

### Migration: `AddUploadArchiveFields`

| Column | Type | Nullable | Default | Notes |
|--------|------|----------|---------|-------|
| `Upload.LargePreviewPath` | `varchar(512)` (Postgres) / `TEXT` (PostgreSQL) | YES | NULL | Mirrors `ThumbnailPath`. Populated by promoter; null while `StorageLocation = Local`. |
| `Upload.OriginalPurgedAt` | `timestamptz` (Postgres) / `TEXT` (PostgreSQL) | YES | NULL | Written by unit-002 post-printing purge. Column exists now so a single migration covers both fields. |

Both columns are `nullable` ⇒ no default-value back-fill ⇒ migration is **instant** on existing rows. Equivalent on both providers (no provider-specific quirks; same pattern bolt 043 used for `StorageLocation`).

### EF Fluent Config (`UploadConfiguration`)

```csharp
builder.Property(u => u.LargePreviewPath).HasMaxLength(512);
builder.Property(u => u.OriginalPurgedAt);
```

No data annotations (ADR-002 — Fluent API only). No defaults — both columns default to `NULL` naturally.

### Query the Recovery Scan + Backfill use

```csharp
_db.Orders
   .Where(o => o.Status >= OrderStatus.Paid          // Paid, Printing, Shipped, Delivered
            && o.Status != OrderStatus.PaymentFailed // PaymentFailed/Cancelled excluded
            && o.Status != OrderStatus.Cancelled)
   .Where(o => o.Items.SelectMany(i => i.Uploads)    // (or however Order→Upload joins live;
              .Any(u => u.StorageLocation == StorageLocation.Local))  // confirmed in Stage 4)
   .Select(o => o.Id)
   .AsAsyncEnumerable();
```

> **Note for Stage 4:** the exact `Order → Upload` traversal (direct nav property vs. `OrderItem → Upload` join) is confirmed during implementation by reading the existing model — the design here is data-shape correct; the LINQ shape is finalized in code.

## Promotion Algorithm

`IOrderPhotoPromoter.PromoteOrderAsync(orderId, ct)` — top-level orchestration:

```text
1. If !_router.CloudEnabled:
     log Error, return PromotionOutcome { Failed = 0, Skipped = 0 }  (no-op, see safety check)

2. Load Order + Items + Uploads (one EF query, AsNoTracking=false because we'll update)
     If null: log Warning, return PromotionOutcome.Empty
     If Status < Paid: log Warning ("not paid"), return PromotionOutcome.Empty

3. For each Upload u where u.StorageLocation == Local:
     a. PromoteUploadAsync(u, ct) → outcome
     b. Aggregate counters into PromotionOutcome

4. Return PromotionOutcome { Promoted, Skipped, Failed, TotalBytes }
```

`PromoteUploadAsync(Upload u, ct)` — per-upload, the meat of it:

```text
1. (Pre-check) If u.StorageLocation == Cloud:
       return Skipped  (idempotent re-entry, no work)

2. (Read source) Open the local original from u.FilePath:
       sourceStream = await _router.Local.GetStreamAsync(u.FilePath, ct)
       If FileNotFound: log Warning ("missing local source"), return Failed

3. (Buffer for re-reads) Copy to MemoryStream (we need source bytes 3 times):
       sourceBuffer = new MemoryStream(); sourceStream.CopyTo(sourceBuffer)

4. (Compute cloud keys) — all via StorageKeys.* (ADR-007 caller-supplied keys)
       originalKey  = StorageKeys.Original(u.Id, u.UploadedAt, Path.GetExtension(u.OriginalFileName))
       thumbKey     = StorageKeys.Thumbnail(u.Id)
       previewKey   = StorageKeys.Preview(u.Id)

5. (Upload original) sourceBuffer.Position = 0
       await _router.Cloud.SaveAsync(sourceBuffer, originalKey, ct)

6. (Generate + upload thumbnail) — Stage 1 Q4 resolved: regenerate if missing
       Stream thumbBytes;
       if u.ThumbnailPath != null && await _router.Local.ExistsAsync(u.ThumbnailPath):
           thumbBytes = await _router.Local.GetStreamAsync(u.ThumbnailPath, ct)
       else:
           sourceBuffer.Position = 0
           thumbBytes = await _imageProcessor.GenerateThumbnailAsync(sourceBuffer, ct)
       await _router.Cloud.SaveAsync(thumbBytes, thumbKey, ct)

7. (Generate + upload large preview) — NEW (story 002)
       sourceBuffer.Position = 0
       var previewBytes = await _imageProcessor.GenerateLargePreviewAsync(sourceBuffer, ct)
       await _router.Cloud.SaveAsync(previewBytes, previewKey, ct)

8. (Atomic row update) — Confirmed-Write-Then-Delete, step 1 of 2
       u.FilePath = originalKey
       u.ThumbnailPath = thumbKey
       u.LargePreviewPath = previewKey
       u.StorageLocation = StorageLocation.Cloud
       await _db.SaveChangesAsync(ct)

9. (Delete local files) — Confirmed-Write-Then-Delete, step 2 of 2
       (Each delete is best-effort; logged on failure but does NOT fail the upload.)
       try { await _router.Local.DeleteAsync(oldFilePath) } catch (log Warning)
       try { if oldThumbnailPath != null) await _router.Local.DeleteAsync(oldThumbnailPath) } catch (log Warning)

10. Return Promoted, log UploadPromoted at Information level
```

**Why the buffer (step 3):** the original stream is consumed three times (original upload, possibly thumbnail regen, large-preview generation). The S3 SDK's `TransferUtility` rewinds buffered/seekable streams; an unbuffered `FileStream` from disk would be cheaper but disqualifies the regen-if-missing thumbnail branch. Buffering once is acceptable — uploads are bounded by the existing upload-size validator (bolt 042) at typically ≤ 20 MB.

**Why local deletes are best-effort:** the cloud writes + row update are the durability boundary. If a stray local file isn't deleted, the next sweep (recovery scan) observes `StorageLocation = Cloud` + lingering local file and *does nothing* — the existing code path for cloud-located uploads doesn't read from local anyway. The only cost is a few KB of disk until garbage collection or a manual cleanup. **Not data loss; just litter.**

### Failure & Retry (Resolved Stage 1 Q3)

Two layers stacked on top of each other:

1. **Low-level (existing):** `S3StorageService` is wrapped in a Polly pipeline (3 attempts, exponential backoff with jitter, retries 5xx/throttling). A single transient S3 hiccup is invisible to the promoter.

2. **High-level (new):** the worker catches any exception escaping `PromoteOrderAsync` and re-enqueues with `Attempt + 1` after `Task.Delay(BackoffSeconds(Attempt))`:

   ```csharp
   private static TimeSpan BackoffSeconds(int attempt) => attempt switch
   {
       1 => TimeSpan.FromSeconds(30),
       2 => TimeSpan.FromSeconds(120),
       3 => TimeSpan.FromMinutes(5),
       4 => TimeSpan.FromMinutes(15),
       _ => TimeSpan.FromMinutes(60),  // attempt 5
   };
   ```

   On `Attempt > 5`: log `UploadPromotionFailed` at Error, **do not** re-enqueue. The next startup's recovery scan will pick it up again — manual operator action is the only further escalation.

**Per-upload partial failure:** if upload A succeeds and upload B fails inside the same order, A stays promoted (row updated) and B stays `Local`. The order is re-enqueued; the next pass skips A (idempotent) and retries B. No all-or-nothing rollback.

## Recovery Scan (Resolved Stage 1 Q6)

`PromotionRecoveryScanner : IHostedService` — registered before the worker so it runs on `StartAsync`:

```csharp
public async Task StartAsync(CancellationToken ct)
{
    if (!_router.CloudEnabled) { _log.LogInformation("Recovery scan skipped (cloud tier off)"); return; }

    using var scope = _scopes.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();

    var stuckIds = await db.Orders
        .Where(o => o.Status >= OrderStatus.Paid && o.Status != OrderStatus.Cancelled)
        .Where(o => o.Items.SelectMany(i => i.Uploads).Any(u => u.StorageLocation == StorageLocation.Local))
        .Select(o => o.Id)
        .ToListAsync(ct);

    foreach (var id in stuckIds)
        await _queue.EnqueueAsync(new PromotionJob(id, Attempt: 1), ct);

    _log.LogInformation("Recovery scan re-enqueued {Count} stuck order(s)", stuckIds.Count);
}
```

Runs **once** on startup, not periodic. Periodic would duplicate the channel's job; the recovery scan exists only for the crash-window case, and a crash means restart.

## Cloud-Tier-Off Safety (Resolved Stage 1 Q5)

Three guard rails, all "fail loudly":

| Surface | When `CloudEnabled == false` | Effect |
|---------|------------------------------|--------|
| `IOrderPhotoPromoter.EnqueueAsync` | logs `Error` and returns without writing to the queue | Webhook still returns 200 (payment is independent of promotion); the order sits `Local`, alert fires on log dashboard. |
| `IOrderPhotoPromoter.PromoteOrderAsync` | logs `Error`, returns empty outcome | Defence in depth — even if something queued a job, the work doesn't run. |
| `PromotionRecoveryScanner.StartAsync` | logs `Information` ("recovery scan skipped"), returns | This one is `Information` not `Error` because at startup we know the config; it isn't a runtime surprise. |

The dev-environment user running `Provider = Local` sees an informational message at startup. The prod-environment misconfiguration (`Provider = Local` but payments fire) screams via `Error` logs every time it happens.

## ImageProcessor: `GenerateLargePreviewAsync` (Story 002)

Mirror of the existing `GenerateThumbnailAsync` with three differences:

| Aspect | `GenerateThumbnailAsync` (bolt 042) | `GenerateLargePreviewAsync` (this bolt) |
|--------|-------------------------------------|------------------------------------------|
| Max dimension constant | `ThumbnailMaxDimension = 300` | `LargePreviewMaxDimension = 2000` |
| JPEG quality | 85 | 85 |
| Decompression-bomb guard | `MaxDecodeDimension = 25_000` | **same** (shared constant) |
| Resize mode | `ResizeMode.Max` | `ResizeMode.Max` (preserves aspect; never upscales) |
| Header check before decode | yes | yes |

```csharp
public async Task<MemoryStream> GenerateLargePreviewAsync(Stream source, CancellationToken ct = default)
{
    if (source.CanSeek) source.Position = 0;
    var info = await Image.IdentifyAsync(source, ct);
    if (info is not null && (info.Width > MaxDecodeDimension || info.Height > MaxDecodeDimension))
        throw new UnprocessableEntityException("Image dimensions exceed limits.");

    if (source.CanSeek) source.Position = 0;
    using var image = await Image.LoadAsync(source, ct);

    image.Mutate(ctx => ctx.Resize(new ResizeOptions
    {
        Size = new Size(LargePreviewMaxDimension, LargePreviewMaxDimension),
        Mode = ResizeMode.Max,
    }));

    var ms = new MemoryStream();
    await image.SaveAsync(ms, new JpegEncoder { Quality = LargePreviewJpegQuality }, ct);
    ms.Position = 0;
    return ms;
}
```

**Never upscale:** `ResizeMode.Max` is "scale to fit *inside* (W,H) but never enlarge." A 1500×1000 input passes through at 1500×1000.

## Backfill CLI (Story 004)

In `Program.cs`, before `WebApplication.CreateBuilder`:

```csharp
if (args.Length > 0 && args[0] == "backfill-archive")
{
    return await BackfillCommand.RunAsync(args, builder.Environment, builder.Configuration);
}
```

`BackfillCommand.RunAsync`:

```text
1. Build minimal Host (no Kestrel): AddDbContext, AddPhotoStorage, AddSingleton<IOrderPhotoPromoter,…>
2. Resolve services; query the same "Paid + Local uploads" filter as the recovery scan
3. If --dry-run: print "would promote: <orderId> (<n> uploads, <size> MB)" per row, return 0
4. Else: for each order id (sequentially, no parallel — backfill is an ops task, log clarity > speed):
     outcome = await _promoter.PromoteOrderAsync(orderId)
     accumulate counters; log per-order result
5. Print summary: "promoted=N skipped=M failed=K total_mb=…"
6. Ctrl+C: token cancels; the cancellation lands at the next `await`. Already-completed orders stay promoted (idempotent); the resume on next run picks up the rest.
```

**Why sequential, not parallel:** the live worker runs at concurrency 4 because webhook bursts demand it. The backfill is one-off, typically run once before a release. Sequential makes the log trail trivial to read, and the durations involved (an order with 6 photos completes in ~30s) make 100 orders a 50-minute task — acceptable.

## Security Design

| Concern | Approach |
|---------|----------|
| Storage credentials | Already handled by bolt 043 (`StorageSettings`, env-loaded, ADR-006 secrets posture). No new secrets. |
| Background worker auth | None — runs in-process under the API's identity. Cannot reach external surfaces it shouldn't already reach. |
| Backfill CLI auth | None — it's a local CLI invocation on the deployment host. Same security boundary as `dotnet ef database update`. |
| GDPR — only paid orders promoted | The promoter's `Status >= Paid` check is the data-minimization invariant. Abandoned guest uploads **never** touch cloud, by construction. |
| Log content | Order IDs and upload IDs are logged; original filenames and customer PII are NOT. Already the codebase's standing convention; no new exposure. |

## NFR Implementation

| Requirement | Approach |
|-------------|----------|
| Webhook latency unaffected | The added line is a synchronous `Channel.Writer.TryWrite` (in-memory) — sub-microsecond. Payment provider sees the same response time. |
| Throughput | 4 concurrent orders × ~5–15s per photo upload × ~6 photos avg = a few orders per minute sustained — comfortably above realistic peak. |
| Memory | Each worker slot holds one buffered `MemoryStream` (≤20 MB) + one ImageSharp decode buffer (capped by `MaxDecodeDimension = 25_000`). Worst-case footprint: 4 × ~150 MB = 600 MB transient. Acceptable on a single-VM production target. |
| Crash recovery | Per-upload row is the source of truth; recovery scan rebuilds the queue from durable state on every startup. No work lost. |
| Cancellation | All `await` calls accept the `CancellationToken`. `BackgroundService` stop-token cascades naturally. CLI Ctrl+C maps to the same token. |
| Idempotency | Per-upload `StorageLocation == Cloud` check at step 1 of `PromoteUploadAsync`. Re-running anything is free. |
| Observability | Per-upload: Debug "skipped (already cloud)" or Information "promoted Bytes={n} Order={id} Upload={id}". Per-order: Information "promote-summary {outcome}". On terminal failure: Error "promote-failed Attempt={n} Upload={id} LastError={…}". |

## Integration Points

| With | How |
|------|-----|
| **Bolt 042 (thumbnail cache)** | Promoter calls `_imageProcessor.GenerateThumbnailAsync` if local thumb missing; reuses `MaxDecodeDimension`. |
| **Bolt 043 (storage layer)** | All byte movements go via `IStorageRouter.Local` / `.Cloud`; keys built via `StorageKeys.*`. No direct `IAmazonS3` or `File.*` calls in promoter code. |
| **Existing payments code** | Two new lines: `_promoter.EnqueueAsync(order.Id, ct)` after `SaveChangesAsync` in `HandleStripePaymentSucceededAsync` and `LegacyProcessorIpnAsync`. |
| **`OrderStatusMachine`** | Untouched. Promoter respects the machine (only runs on `Status ≥ Paid`) but doesn't drive transitions. |
| **Existing `IHostedService` registrations** | Worker + recovery scanner registered via `services.AddHostedService<T>()` from inside `AddPhotoArchive(IConfiguration)` (new extension method, parallel to bolt-043's `AddPhotoStorage`). |
| **Unit 002 (retention)** | Provides `OriginalPurgedAt` column. The unit-002 purge job will populate it. |
| **Unit 003 (viewing)** | Will query `LargePreviewPath` to serve the customer-facing full view. No code coupling in this bolt. |

## Configuration

New section in `appsettings.json` (defaults shown):

```jsonc
"OrderPhotoArchive": {
  "Enabled": true,                  // master switch; false → behave as if cloud tier off
  "MaxConcurrentOrders": 4,         // worker semaphore size
  "MaxAttempts": 5,                 // retry ceiling before logging UploadPromotionFailed
  "BackoffSeconds": [30, 120, 300, 900, 3600]  // index-by-attempt; clamps to last on overflow
}
```

Bound via `OrderPhotoArchiveSettings` + `IValidateOptions<OrderPhotoArchiveSettings>` (consistent with `StorageSettingsValidator` from bolt 043).

## Open Questions for Implementation (Stage 4)

1. **Order → Upload navigation shape.** The query in §Data Model uses `o.Items.SelectMany(i => i.Uploads)`; the actual relationship traversal (direct `Order.Uploads`? via `OrderItem.UploadId`? join table?) is confirmed by reading the existing model in Stage 4. The design is data-shape correct; the LINQ shape may be one line different.

2. **Channel bound.** The design says "unbounded". If we want backpressure (e.g. a runaway recovery scan creating millions of jobs), we can bound to N × `MaxConcurrentOrders`. Default unbounded is fine until proven otherwise.

3. **Promotion-summary email.** Out of scope — the order-confirmed email already fires from the webhook. A separate "your archive is ready" email could be added in unit 003 if we want, but it's not in any story here.

## Completion Criteria — Stage 2

- [x] Architecture pattern selected and documented (producer/consumer + recovery scan).
- [x] All layers designed with responsibilities.
- [x] Integration points named (with specific line/method references in webhooks).
- [x] Database schema designed (`AddUploadArchiveFields` migration, two nullable columns).
- [x] NFRs addressed (throughput, memory, latency, crash recovery, cancellation, observability).
- [x] Security posture documented (no new secrets, no new endpoints, GDPR invariant pinned).
- [x] Stage 1 open questions resolved or explicitly deferred to Stage 4.
