---
id: 002-retention-cleanup-job
unit: 002-archive-retention
intent: 024-order-photo-archive
status: complete
priority: must
created: 2026-05-27T13:05:00Z
assigned_bolt: 052-archive-retention
implemented: true
---

# Story: 002-retention-cleanup-job

## User Story

**As** the platform
**I want** archived previews + thumbnails deleted after the retention window
**So that** the cloud archive stays bounded and we don't keep customer photos longer than promised

## Acceptance Criteria

- [ ] A background job (periodic) finds uploads whose order completed more than the configured window ago (default **12 months**) and deletes their **large preview + thumbnail**, nulling `LargePreviewPath` / `ThumbnailPath`.
- [ ] Window configurable via `ArchiveSettings:RetentionMonths` (default `12`, e.g. settable to `6`).
- [ ] Order + order-item **metadata is retained** (only the image blobs go).
- [ ] Orders **within** the window are never touched.
- [ ] Per-run summary logged: `orders_cleaned=N, blobs_deleted=M, failed=K`.

## Technical Notes

- Retention measured from the **order completion** timestamp (Q2 decision).
- Run as a hosted `BackgroundService` on a daily-ish cadence (config interval); idempotent.
- Reuse the bolt-033 cleanup-job patterns (referenced-upload safety, retention config).

## Dependencies

### Requires
- bolt 051; 001-purge-original-on-shipped (consistent lifecycle ordering).

### Enables
- Bounded, compliant archive.

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Blob already deleted | No-op; still nulls the key |
| Order completion timestamp missing | Skip + log Warning (don't guess) |

## Out of Scope
- Purging the original (story 001).
