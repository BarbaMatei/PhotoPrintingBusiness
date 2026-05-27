---
id: 001-skip-referenced-uploads
unit: 001-upload-cleanup-job-fix
intent: 013-upload-cleanup-fix
status: implemented
priority: must
created: 2026-05-25T10:00:00Z
assigned_bolt: 033-upload-cleanup-fix
implemented: true
implemented_at: 2026-05-25T11:35:00Z
---

# Story: 001-skip-referenced-uploads

## User Story

**As** the platform operator
**I want** the upload cleanup job to skip every upload referenced by a cart or order item
**So that** paying customers never lose the source photos we are about to print

## Acceptance Criteria

- [ ] **Given** an upload `U` referenced by exactly one `CartItem`, **When** the cleanup tick runs after `OrphanRetentionHours`, **Then** `U.DeletedAt IS NULL` and `U`'s file is still on disk.
- [ ] **Given** an upload `U` referenced by exactly one `OrderItem` (order status `Paid`), **When** the cleanup tick runs, **Then** `U` is retained.
- [ ] **Given** an upload `U` never referenced and older than `OrphanRetentionHours`, **When** the cleanup tick runs, **Then** `U.DeletedAt` is set to `UtcNow` and the file is removed via `IStorageService.DeleteAsync`.
- [ ] **Given** an upload `U` referenced by an order older than `ReferencedRetentionDays`, **When** the cleanup tick runs, **Then** `U` is eligible for deletion (covers stale archives).

## Technical Notes

```csharp
// src/PhotoPrint.API/BackgroundJobs/UploadCleanupJob.cs
var orphanCutoff     = _clock.UtcNow.AddHours(-_settings.OrphanRetentionHours);
var referencedCutoff = _clock.UtcNow.AddDays (-_settings.ReferencedRetentionDays);

var candidates = await db.Uploads
    .Where(u => u.DeletedAt == null)
    .Where(u =>
        // orphans past the short window
        (u.UploadedAt < orphanCutoff &&
            !db.CartItems .Any(ci => ci.UploadId == u.Id) &&
            !db.OrderItems.Any(oi => oi.UploadId == u.Id))
        // OR referenced but past the long window
        || u.UploadedAt < referencedCutoff)
    .Take(500)                    // batch
    .ToListAsync(ct);
```

- Log each tick at Information: `cleanup tick — deleted={deleted}, skipped_referenced={skipped}, batch_size={batch}`.
- Skipped references logged at Debug (per-upload) for diagnosis only.

## Dependencies

### Requires
- 002-retention-config (provides `UploadCleanupSettings`)

### Enables
- 003-cleanup-regression-test

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Upload referenced by both a CartItem and an OrderItem | Retained (either reference is sufficient) |
| Cart row deleted between query and update | Acceptable — next tick will re-evaluate; no destructive race |
| > 500 candidates in one tick | Process batch, next tick handles remainder |
| Concurrent uploads during cleanup | Untouched (filtered by `UploadedAt < cutoff`) |

## Out of Scope

- Reconciling disk files orphaned by previous buggy ticks (handled by separate ops script).
- Metrics export (deferred to intent 020).
