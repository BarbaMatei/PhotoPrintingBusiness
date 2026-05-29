---
unit: 002-archive-retention
bolt: 052-archive-retention
stage: design
status: complete
updated: 2026-05-29T12:20:00Z
---

# Technical Design — Archive Retention

## Architecture Pattern

**Two independent destructive surfaces**, each as simple as it can be:

1. **Story 001 (original purge)** — synchronous call from `AdminOrderService.UpdateStatusAsync`
   after the order transitions to the configured "production complete" status. No queue,
   no background worker — admin-driven, low-frequency, blocking-OK.
2. **Story 002 (retention cleanup)** — `BackgroundService` on a `PeriodicTimer`, identical
   shape to the existing `UploadCleanupJob` (bolt 033). Batches of 500, idempotent.

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│  ─── Story 001: synchronous purge on production-complete transition ──       │
│                                                                              │
│  PATCH /api/admin/orders/{id}/status  (admin UI)                             │
│      └── AdminOrderService.UpdateStatusAsync(...)                            │
│            ├── OrderStatusMachine.Transition(order, Shipped)                 │
│            ├── _db.SaveChangesAsync()         ← Shipped is durable           │
│            ├── _orderEmailService.FireOrderShippedEmail(...)                 │
│            ├── _hub.Clients.All.SendAsync(...)                               │
│            └── await _purger.PurgeOrderOriginalsAsync(order.Id, ct)  ← NEW   │
│                  ├── Cloud.DeleteAsync(upload.FilePath)                      │
│                  └── upload.FilePath = null; OriginalPurgedAt = UtcNow       │
│                                                                              │
│  ─── Story 002: periodic retention cleanup ──                                │
│                                                                              │
│  ArchiveRetentionJob : BackgroundService (PeriodicTimer, 6 h)                │
│      └── per tick: SELECT TOP N uploads                                      │
│                       where (LargePreviewPath != null OR ThumbnailPath != null)│
│                         AND parent Order.PaidAt < UtcNow - RetentionWindow   │
│         └── for each: Cloud.DeleteAsync(preview/thumb keys)                  │
│                       null both keys; SaveChanges                            │
│                                                                              │
│  ─── Crash-recovery for the synchronous purge ──                             │
│                                                                              │
│  OriginalPurgeRecoveryScanner : IHostedService (StartAsync, runs once)       │
│      └── SELECT orders where Status in (Shipped, Delivered)                  │
│                          AND any Upload.FilePath != null                     │
│                          AND any Upload.StorageLocation == Cloud             │
│         └── for each: _purger.PurgeOrderOriginalsAsync(id)                   │
└──────────────────────────────────────────────────────────────────────────────┘
```

**Why no queue:** Story 001's trigger is one admin clicking a button. Synchronous purge
inside the request adds maybe 100–500 ms (one S3 `DeleteObjects` call per upload, ~6
uploads per order); the admin UI gets a slightly slower response but the alternative
(queue + worker) is a lot of moving parts for traffic measured in single-digit-per-hour.
Story 002 is a periodic sweep; queueing would mean processing thousands of items through
a channel that exists only to immediately drain — pointless.

## Layer Structure

```text
┌───────────────────────────────────────────────────────────┐
│  Presentation                                             │
│    AdminOrdersController                  (no change)     │
├───────────────────────────────────────────────────────────┤
│  Application                                              │
│    AdminOrderService.UpdateStatusAsync    (3 new lines:   │
│                                            purger call)   │
│    IOriginalPurger                        (interface)     │
│    OriginalPurger                         (impl)          │
├───────────────────────────────────────────────────────────┤
│  Domain / Infrastructure                                  │
│    ArchiveRetentionJob                    (BackgroundSvc) │
│    OriginalPurgeRecoveryScanner           (IHostedService)│
│    ArchiveSettings + validator            (configuration) │
│    PhotoArchiveExtensions.AddPhotoArchive (registration   │
│                                            extended)      │
├───────────────────────────────────────────────────────────┤
│  Persistence                                              │
│    (no schema changes — uses PaidAt + Upload columns      │
│     bolt 051 already shipped)                             │
└───────────────────────────────────────────────────────────┘
```

## Hook Points (Resolved Stage 1 Q2)

After reading `AdminOrderService.UpdateStatusAsync`:

- `OrderStatusMachine.Transition(order, OrderStatus.Shipped)` is called from **exactly one
  place** — `AdminOrderService.UpdateStatusAsync` (line 100 in current code), driven by
  admin `PATCH /api/admin/orders/{id}/status`.
- The current sequence is: transition → set AWB/tracking → `SaveChangesAsync` → email →
  SignalR broadcast.

**Design choice:** Add `await _purger.PurgeOrderOriginalsAsync(order.Id, ct)` as the
**last** await in the method — after `SaveChangesAsync` and after the email + SignalR fire.
The purger is **synchronous** (no enqueue indirection), so the admin's PATCH response is
delayed by the purge duration. Acceptable because:
- Purge happens once per order, by an admin in a tooling UI (no customer impact).
- Per-upload latency is a single S3 `DeleteObject` (~50–100 ms each).
- An order with 6 uploads ≈ 600 ms added to a request that already does email + SignalR.

Also fires on `OrderStatus.Delivered` if `PurgeOriginalAtStatus = Delivered`. The
shape of the conditional is:
```csharp
if (settings.IsProductionCompleteStatus(newStatus))
    await _purger.PurgeOrderOriginalsAsync(order.Id, ct);
```

**`OrderStatusMachine` stays pure** (no DI dependencies, same reasoning as bolt 051).

## Retention Anchor — Resolved (Stage 1 Q1)

Stage 1 recommended **adding a `CompletedAt` column** to `Order`. After reading the code,
**I'm reversing that recommendation. Use `Order.PaidAt` instead.** Three reasons:

1. **`PaidAt` is always set** on every order that ever reached `Paid` — including the ones
   that go `Paid → Cancelled` (which still have promoted photos to clean up). A new
   `CompletedAt` would be null for cancelled orders, breaking the retention query.
2. **`PaidAt` is never re-set** by any code path — unlike `UpdatedAt`, which the status
   machine touches on every transition. Stable anchor.
3. **The customer-facing story is honest**: "we keep your photos for 12 months after you
   paid us." A photo-printing business with 1–4 week typical fulfilment loses very little
   archive lifetime by anchoring on `PaidAt` rather than `DeliveredAt`.

**Trade-off accepted:** an order that takes 3 months to fulfil gives the customer 9 months
of archive instead of 12. The mitigation is operational (fulfilment SLA), not architectural.

**Net schema change for bolt 052: one column nullability flip.** (Updated during Stage 4
— see §Implementation Correction at the bottom.) Bolt 051's columns plus the
`FilePath` nullability change are all we need; the retention *anchor* (`Order.PaidAt`)
itself is unchanged.

## API Design

**No new HTTP endpoints.** All triggers live behind existing surfaces:
- Story 001 — `PATCH /api/admin/orders/{id}/status` (existing endpoint).
- Story 002 — internal `BackgroundService`, no HTTP surface.

## Configuration (`ArchiveSettings`)

```jsonc
"Archive": {
  "_comment": "Intent 024 retention lifecycle (bolt 052). PurgeOriginalAtStatus must be Shipped or Delivered. RetentionMonths is measured from Order.PaidAt — see ddd-02 §Retention Anchor.",
  "Enabled": true,
  "PurgeOriginalAtStatus": "Shipped",
  "RetentionMonths": 12,
  "JobIntervalHours": 6,
  "BatchSize": 500
}
```

Validator (`IValidateOptions<ArchiveSettings>`, `.ValidateOnStart()`):
- `PurgeOriginalAtStatus` must parse to `OrderStatus.Shipped` or `OrderStatus.Delivered` — nothing else.
- `RetentionMonths` > 0 (allowing `1` for test environments).
- `JobIntervalHours` > 0.
- `BatchSize` > 0.

```csharp
public class ArchiveSettings
{
    public const string SectionName = "Archive";
    public bool Enabled { get; set; } = true;
    public string PurgeOriginalAtStatus { get; set; } = "Shipped";  // "Shipped" | "Delivered"
    public int RetentionMonths { get; set; } = 12;
    public int JobIntervalHours { get; set; } = 6;
    public int BatchSize { get; set; } = 500;

    public bool IsProductionCompleteStatus(OrderStatus s)
        => Enum.TryParse<OrderStatus>(PurgeOriginalAtStatus, true, out var target) && s == target;

    public OrderStatus[] ProductionCompleteFloor()
    {
        // The query "include this status and everything past it" needs an explicit list
        // because OrderStatus enum order doesn't match lifecycle order (PaymentFailed=5,
        // Cancelled=6 are after Delivered=4).
        return PurgeOriginalAtStatus.Equals("Delivered", StringComparison.OrdinalIgnoreCase)
            ? [OrderStatus.Delivered]
            : [OrderStatus.Shipped, OrderStatus.Delivered];
    }
}
```

## Purge Algorithm — Story 001

`IOriginalPurger.PurgeOrderOriginalsAsync(orderId, ct)`:

```text
1. If !_settings.Enabled || !_router.CloudEnabled:
     log Error "purge.refused order_id=X reason=archive-disabled|cloud-tier-off"
     return PurgeOutcome.Empty

2. Load order + items + uploads (one EF query with Include/ThenInclude).
     If null: log Warning "purge.skipped order_id=X reason=order-not-found"
     return PurgeOutcome.Empty

3. For each upload in order.Items.Select(i => i.Upload).Distinct():
     a. If u.FilePath == null: count Skipped, continue (already purged — idempotent)
     b. If u.StorageLocation != Cloud: count Skipped, continue + log Warning
          ("purge.unexpected upload_id=X location=Local")
     c. Try: await _router.Cloud.DeleteAsync(u.FilePath, ct)
        Catch any: log Warning, count Failed, continue (next sweep retries)
     d. Update row atomically:
          u.FilePath = null
          u.OriginalPurgedAt = DateTimeOffset.UtcNow
          await _db.SaveChangesAsync(ct)
     e. count Purged + accumulate FileSizeBytes
     f. log Information "OriginalPurged upload_id=X order_id=Y bytes=N"

4. Return PurgeOutcome { Purged, Skipped, Failed, BytesFreed }
```

**Per-upload atomic SaveChanges** (one row update per upload, not batched). This mirrors
bolt 051's per-upload atomicity (ADR-011): partial failure across an order's uploads is
allowed; failed ones get retried next sweep without affecting successfully-purged ones.

**Confirmed-Delete-Then-Update ordering** (mirror of ADR-011, inverted):
- Cloud delete first → row update second.
- Crash between leaves the row claiming a blob that is gone. The recovery scanner observes
  `FilePath != null` on a Shipped order and re-attempts delete. S3 `DeleteObject` on a
  non-existent key is a successful no-op, so the second attempt completes and the row
  updates correctly. **No silent data loss case.**

## Retention Cleanup Algorithm — Story 002

`ArchiveRetentionJob : BackgroundService`:

```text
ExecuteAsync(stoppingToken):
    if !_settings.Enabled or !_router.CloudEnabled:
        log "retention.disabled" + return

    using timer = PeriodicTimer(TimeSpan.FromHours(_settings.JobIntervalHours))

    while await timer.WaitForNextTickAsync(stoppingToken):
        try:
            (cleaned, failed) = await SweepAsync(stoppingToken)
            log Information "retention.tick orders_cleaned=X blobs_deleted=Y failed=Z"
        catch OperationCanceledException when shutdown: break
        catch other: log Error "retention.tick.error" + continue

SweepAsync(ct):
    using scope = _scopeFactory.CreateScope()
    var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>()
    var router = scope.ServiceProvider.GetRequiredService<IStorageRouter>()

    var cutoff = DateTimeOffset.UtcNow.AddMonths(-_settings.RetentionMonths)

    var rows = await db.Uploads
        .Where(u => u.StorageLocation == StorageLocation.Cloud)
        .Where(u => u.LargePreviewPath != null || u.ThumbnailPath != null)
        .Where(u => db.OrderItems
            .Where(oi => oi.UploadId == u.Id)
            .Any(oi => oi.Order.PaidAt != null && oi.Order.PaidAt < cutoff))
        .OrderBy(u => u.UploadedAt)
        .Take(_settings.BatchSize)
        .ToListAsync(ct)

    var blobsDeleted = 0, failures = 0

    foreach (var u in rows):
        ct.ThrowIfCancellationRequested()
        try:
            if u.LargePreviewPath != null:
                await router.Cloud.DeleteAsync(u.LargePreviewPath, ct)
                u.LargePreviewPath = null
                blobsDeleted++
            if u.ThumbnailPath != null:
                await router.Cloud.DeleteAsync(u.ThumbnailPath, ct)
                u.ThumbnailPath = null
                blobsDeleted++
            log Information "ArchiveExpired upload_id=X"
        catch ex:
            log Warning "retention.delete-failed upload_id=X" + ex
            failures++

    if rows.Count > 0:
        await db.SaveChangesAsync(ct)

    return (rows.Count, failures)
```

**Notes:**
- **Per-tick batched SaveChanges.** Unlike story 001 (per-upload atomic), the retention job
  batches because there's no "row update unlocks customer-visible state" semantic — the
  blobs are gone either way; the SaveChanges only matters for queryability next sweep.
- **Order/OrderItem metadata never touched.** The `Upload` row stays (with all three blob
  keys null) so unit 003's order-history endpoint can render "this upload is no longer
  viewable" instead of 404.
- **`Order.PaidAt` is the anchor** (resolved above).
- **`StorageLocation` stays `Cloud`** even after expiry. There's no `Expired` value on the
  enum (that was rejected in bolt-051 design to keep the enum minimal); state is derived
  from "all three keys null."

## Stuck-Purge Recovery — Story 001 reliability

`OriginalPurgeRecoveryScanner : IHostedService` (runs once at `StartAsync`):

```csharp
public async Task StartAsync(CancellationToken ct)
{
    if (!_settings.Enabled || !_router.CloudEnabled)
        { _log.LogInformation("purge.recovery.skipped"); return; }

    using var scope = _scopes.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
    var purger = scope.ServiceProvider.GetRequiredService<IOriginalPurger>();

    var statuses = _settings.ProductionCompleteFloor();
    var orderIds = await db.Orders
        .Where(o => statuses.Contains(o.Status))
        .Where(o => o.Items.Any(i =>
            i.Upload.StorageLocation == StorageLocation.Cloud &&
            i.Upload.FilePath != null))
        .Select(o => o.Id)
        .ToListAsync(ct);

    foreach (var id in orderIds)
        await purger.PurgeOrderOriginalsAsync(id, ct);

    _log.LogInformation("purge.recovery.processed count={Count}", orderIds.Count);
}
```

Mirrors bolt 051's `PromotionRecoveryScanner`. Runs once on startup. Closes the crash
window between `OrderStatusMachine.Transition` flushing the row and the purger finishing.

## Cloud-Tier-Off Safety

Same fail-loudly posture as bolt 051:

| Surface | When `!CloudEnabled` or `!Enabled` | Effect |
|---------|------------------------------------|--------|
| `OriginalPurger.PurgeOrderOriginalsAsync` | logs Error, returns empty outcome | Defence in depth — a paid order whose photos reached cloud should never be purgeable in a deployment where cloud is off, but if it happens we scream. |
| `ArchiveRetentionJob.ExecuteAsync` | logs Information ("retention.disabled"), exits the loop | Configuration-time decision; not a runtime surprise → Information not Error. |
| `OriginalPurgeRecoveryScanner.StartAsync` | logs Information ("purge.recovery.skipped"), returns | Same reasoning as the retention job. |
| `AdminOrderService.UpdateStatusAsync` | the purger call still happens — it self-refuses | Two layers: the purger guards itself; the caller doesn't need to know. |

## DI Wiring

Extend `PhotoArchiveExtensions.AddPhotoArchive` (bolt 051's extension) to also register
the retention services. Keeps "intent 024 wiring" in one place:

```csharp
public static IServiceCollection AddPhotoArchive(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ── bolt 051 (already there) ────────────────────────────────────────
    services.Configure<OrderPhotoArchiveSettings>(...);
    services.AddSingleton<IPromotionQueue, PromotionQueue>();
    services.AddScoped<IOrderPhotoPromoter, OrderPhotoPromoter>();
    services.AddHostedService<PromotionRecoveryScanner>();
    services.AddHostedService<OrderPhotoPromotionWorker>();

    // ── bolt 052 (new) ──────────────────────────────────────────────────
    services.Configure<ArchiveSettings>(configuration.GetSection(ArchiveSettings.SectionName));
    services.AddSingleton<IValidateOptions<ArchiveSettings>, ArchiveSettingsValidator>();
    services.AddOptions<ArchiveSettings>().ValidateOnStart();

    services.AddScoped<IOriginalPurger, OriginalPurger>();
    services.AddHostedService<OriginalPurgeRecoveryScanner>();
    services.AddHostedService<ArchiveRetentionJob>();

    return services;
}
```

## Security Design

| Concern | Approach |
|---------|----------|
| Storage credentials | Reused from bolt 043 (`StorageSettings`). No new secrets. |
| Authorization | Story 001's purge is triggered by admin role (already enforced on `AdminOrdersController`). Background job has no caller — runs under the API's identity. |
| GDPR — bounded archive | The retention job's existence is the bounded-archive guarantee; the purger's existence enforces "originals not retained post-fulfilment." Both are config-driven. |
| Log content | Order/upload IDs and byte counts only; no PII. |

## NFR Implementation

| Requirement | Approach |
|-------------|----------|
| Purge latency on admin status change | Order-bounded by `O(uploads) × DeleteObject` (~50–100 ms each). 6-photo order ≈ 600 ms added to a request that was already 200–400 ms for email + SignalR. Acceptable. |
| Retention job impact | Bounded by `BatchSize = 500` per tick; one S3 DeleteObject per blob = ~500 × 100 ms ≈ 50 s every 6 h. Negligible. |
| Crash safety | Recovery scanner + per-upload atomicity (story 001); retention job re-runs every 6 h naturally (story 002). |
| Idempotency | Per-upload short-circuit on `FilePath == null` (story 001) and `LargePreviewPath == null && ThumbnailPath == null` (story 002). |
| Observability | `OriginalPurged` / `ArchiveExpired` events at Information; failures at Warning/Error. |

## Integration Points

| With | How |
|------|-----|
| **Bolt 043 (storage layer)** | Deletes go via `IStorageRouter.Cloud.DeleteAsync`. No direct AWS SDK. |
| **Bolt 051 (promotion)** | Consumes the schema 051 shipped (`OriginalPurgedAt`, `LargePreviewPath`); honours the `StorageLocation = Cloud` invariant 051's promoter set. |
| **Existing `AdminOrderService`** | 3 new lines: inject `IOriginalPurger`; call after `SaveChangesAsync` when `IsProductionCompleteStatus(newStatus)`. |
| **`OrderStatusMachine`** | Untouched (same posture as bolt 051). |
| **Unit 003 (viewing)** | Will surface "your photos are no longer available" using the three observable states (Archived / OriginalPurged / Expired). No coupling here. |

## Open Questions for Implementation (Stage 4)

1. **Whether to add a fallback for cancelled orders' purge.** Currently cancelled orders
   keep their original (no Shipped transition ever fires). They get retention-cleaned in
   12 months along with the rest. **Decision: leave as-is.** A cancelled order's original
   sitting in cloud for up to 12 months is fine for a refund scenario — admin may still
   need to inspect what was ordered. Surfaced as a Stage-4 confirmation rather than a
   new code path.
2. **Bolt-033's `UploadCleanupJob` interaction.** That job soft-deletes `Upload` rows after
   their referenced-retention window. We deliberately don't touch `DeletedAt` here — the
   `Upload` row needs to outlive its blobs so unit 003 can render "no longer available."
3. **Stripe.net version warning** — pre-existing build warning unaffected by this bolt.

## Completion Criteria — Stage 2

- [x] Architecture pattern selected and documented (synchronous purger + periodic job).
- [x] All layers designed with responsibilities.
- [x] Integration points named (`AdminOrderService.UpdateStatusAsync`, line 100).
- [x] **Minimal schema delta** — retention anchor is `Order.PaidAt` (resolved Stage 1 Q1).
      One nullability migration discovered during Stage 4 — see §Implementation Correction below.
- [x] NFRs addressed (latency, idempotency, crash recovery, observability).
- [x] Cloud-off safety, configuration, DI all documented.
- [x] Stage 1 open questions resolved or explicitly deferred to Stage 4.

## Implementation Correction (Stage 4)

The Stage-2 design claimed "net schema change: zero." That turned out to be wrong: making
the original-purge nul `Upload.FilePath` requires the column to *be* nullable, which it
wasn't (the original bolt-042 `UploadConfiguration` had `.IsRequired()` on it). Discovered
when the build surfaced a `CS8625 Cannot convert null literal to non-nullable reference
type` warning from `OriginalPurger.cs`.

**Fix applied during Stage 4 (no design rethink):**

- `Upload.FilePath` → `string?` (model + Fluent config).
- New migration `20260529123952_MakeUploadFilePathNullable` — one `AlterColumn` to
  remove NOT NULL. Type stays `TEXT`, works on both Postgres and SQLite without
  hand-editing (unlike bolt 051's `OriginalPurgedAt` which needed the timestamp-type
  override).
- Three readers updated: `UploadService.GetPreviewAsync` (throws `NotFoundException`
  if `FilePath` is null — the "your photos are no longer available" case unit 003 will
  surface gracefully); `OrderPhotoPromoter.PromoteUploadAsync` (only runs on Local
  uploads which can't be purged — `!` operator + a comment); `UploadCleanupJob`
  (skips the file-delete step when `FilePath` is null, still soft-deletes the row).

The retention anchor (`PaidAt`) remains unchanged — ADR-012 stands. This correction
is purely about the *column nullability* that enables nulling `FilePath` in the first
place. Recorded for accuracy; no design re-debate needed.
