---
unit: 001-order-photo-promotion
intent: 024-order-photo-archive
phase: inception
status: draft
unit_type: backend
default_bolt_type: ddd-construction-bolt
created: 2026-05-27T13:05:00Z
updated: 2026-05-27T13:05:00Z
---

# Unit Brief: Order Photo Promotion

## Purpose

When an order is paid, promote its photos from the local deployment server to durable
cloud storage: upload the original, generate + upload a ~2000 px large preview, ensure the
thumbnail, flip `Upload.StorageLocation` to `Cloud`, and delete the local copies **only
after** the cloud writes are confirmed. Runs asynchronously off the webhook hot path.

## Scope

### In Scope
- Schema: `Upload.LargePreviewPath`, `Upload.OriginalPurgedAt` (+ archive bookkeeping).
- `ImageProcessor` large-preview generation (~2000 px, q85, `previews/` prefix).
- `OrderPhotoPromoter` background worker triggered on order → Paid; idempotent + retried.
- Confirmed-write-then-delete-local semantics.
- One-off **backfill** CLI verb promoting pre-existing paid orders (FR-7).

### Out of Scope
- Original purge after printing (unit 002).
- Retention cleanup (unit 002).
- Customer-facing viewing UI (unit 003).

## Key Entities
- `Upload` (gains `LargePreviewPath`, `OriginalPurgedAt`, `StorageLocation` [added in 043]).
- `Order` / order status machine (Paid trigger).
- `IStorageService` (both Local + S3), `ImageProcessor`.

## Dependencies
- **bolt 043**: `S3StorageService`, `StorageKeys`, presigned URLs, `StorageLocation`.

## Story Summary

| Story ID | Title | Priority |
|----------|-------|----------|
| 001-archive-schema | `LargePreviewPath` + lifecycle fields migration | Must |
| 002-large-preview-generation | Generate ~2000 px large web preview | Must |
| 003-promote-on-paid | Async promote-on-Paid worker (+ delete local) | Must |
| 004-backfill-paid-orders | One-off backfill of pre-existing paid orders | Should |
