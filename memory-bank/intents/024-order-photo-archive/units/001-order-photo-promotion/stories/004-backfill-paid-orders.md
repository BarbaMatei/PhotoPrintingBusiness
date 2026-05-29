---
id: 004-backfill-paid-orders
unit: 001-order-photo-promotion
intent: 024-order-photo-archive
status: draft
priority: should
created: 2026-05-27T13:05:00Z
assigned_bolt: 051-order-photo-promotion
implemented: false
supersedes: 019-thumbnail-cache-and-cloud-storage/002-cloud-storage-provider/003-local-to-cloud-migration-tool
---

# Story: 004-backfill-paid-orders

## User Story

**As** the operator
**I want** a one-off command that promotes photos for orders that were already paid before this feature shipped
**So that** existing fulfilled orders also get a cloud archive, without uploading abandoned uploads

## Acceptance Criteria

- [ ] `dotnet run --project src/PhotoPrint.API -- backfill-archive --dry-run` lists paid orders whose uploads are still `StorageLocation = Local`.
- [ ] Without `--dry-run`, it runs the **same promotion path** as `OrderPhotoPromoter` for each such order.
- [ ] **Idempotent + resumable**: re-running skips already-promoted uploads; Ctrl+C stops cleanly and the next run continues.
- [ ] Per-order outcome logged; final summary `promoted=N, skipped=M, failed=K, total_mb=...`.

## Technical Notes

- Reuse the promoter — this is a CLI entry point, not a second implementation.
- **Supersedes** intent-019 story `003-local-to-cloud-migration-tool`: we promote *paid orders*, never every upload. Bolt 050 is retired.

## Dependencies

### Requires
- 003-promote-on-paid (the promoter it drives).

### Enables
- Production cutover for an existing dataset.

## Out of Scope
- Migrating unpaid/abandoned uploads (deliberately never sent to cloud).
