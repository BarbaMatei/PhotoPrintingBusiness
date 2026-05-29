---
id: 051-order-photo-promotion
unit: 001-order-photo-promotion
intent: 024-order-photo-archive
type: ddd-construction-bolt
status: planned
stories:
  - 001-archive-schema
  - 002-large-preview-generation
  - 003-promote-on-paid
  - 004-backfill-paid-orders
created: 2026-05-27T13:10:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [043-cloud-storage-provider]
enables_bolts: [052-archive-retention, 053-order-history-photos]
requires_units: []
blocks: false

complexity:
  avg_complexity: 3
  avg_uncertainty: 2
  max_dependencies: 2
  testing_scope: 3
---

# Bolt: 051-order-photo-promotion

## Overview

Promote a paid order's photos from local disk to the cloud archive: upload original, generate + upload a ~2000 px large preview, ensure the thumbnail, flip `StorageLocation`, then delete local after confirmed writes. Includes a one-off backfill CLI for pre-existing paid orders.

## Stories Included

- **001-archive-schema** (Must): `LargePreviewPath` + `OriginalPurgedAt` migration.
- **002-large-preview-generation** (Must): `ImageProcessor` ~2000 px preview.
- **003-promote-on-paid** (Must): async `OrderPhotoPromoter` + delete-local-after-confirm.
- **004-backfill-paid-orders** (Should): one-off backfill CLI (supersedes intent-019 story 003).

## Bolt Type

**Type**: DDD Construction Bolt — `.specsmd/aidlc/templates/construction/bolt-types/ddd-construction-bolt.md`

## Dependencies

### Requires
- 043-cloud-storage-provider (S3 adapter, `StorageKeys`, `StorageLocation`, presigned URLs).

### Enables
- 052-archive-retention, 053-order-history-photos.

## Notes

- Promotion runs **off the webhook hot path** (enqueue → `BackgroundService`).
- Crash-safe: startup recovery re-enqueues Paid orders with `StorageLocation = Local` uploads.
- Backfill reuses the promoter (CLI verb), not a second implementation.
