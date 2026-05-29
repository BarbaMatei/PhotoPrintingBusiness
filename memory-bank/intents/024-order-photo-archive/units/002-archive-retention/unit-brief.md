---
unit: 002-archive-retention
intent: 024-order-photo-archive
phase: construction
status: complete
unit_type: backend
default_bolt_type: ddd-construction-bolt
created: 2026-05-27T13:05:00Z
updated: 2026-05-29T13:30:00Z
---

# Unit Brief: Archive Retention

## Purpose

Enforce the data-minimization lifecycle on cloud-archived photos: delete the original once
printing completes, and delete the large preview + thumbnail after the retention window.

## Scope

### In Scope
- Purge the cloud **original** (+ null its key, set `OriginalPurgedAt`) when an order reaches the configurable "production complete" status (default **Shipped**).
- Background retention job: after a configurable window (default **12 months** from order completion), delete the large preview + thumbnail and null their keys.
- Per-order logging + summary counters; idempotent and safe to re-run.

### Out of Scope
- Promotion (unit 001).
- Viewing UI (unit 003).
- Deleting order metadata (always retained).

## Key Entities
- `Upload` (`FilePath`, `LargePreviewPath`, `ThumbnailPath`, `OriginalPurgedAt`).
- `Order` (completion timestamp, status).
- `IStorageService` (cloud delete).

## Dependencies
- **bolt 051**: cloud-located uploads to purge/clean.

## Story Summary

| Story ID | Title | Priority |
|----------|-------|----------|
| 001-purge-original-on-shipped | Delete cloud original when order ships | Must |
| 002-retention-cleanup-job | 12-month configurable archive cleanup | Must |
