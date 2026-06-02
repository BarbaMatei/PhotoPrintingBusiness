---
unit: 003-order-history-photos
intent: 024-order-photo-archive
phase: construction
status: complete
unit_type: frontend
default_bolt_type: simple-construction-bolt
created: 2026-05-27T13:05:00Z
updated: 2026-05-29T15:00:00Z
---

# Unit Brief: Order History Photos

## Purpose

Let a logged-in customer review the photos they ordered, from the account order section:
a thumbnail grid per order, with a full-size (~2000 px) lightbox view — served from the
cloud archive via presigned URLs.

## Scope

### In Scope
- Backend read endpoint: list an order's photos with presigned large-preview + thumbnail URLs (1 h TTL), gated by owner / claimed-guest authorization.
- Frontend: order-detail thumbnail grid → click opens a large-preview lightbox.
- Graceful handling when photos have aged out of the retention window (simply absent).

### Out of Scope
- Guest (unauthenticated) viewing via tokenized link — explicitly deferred.
- Re-download of the original (purged after printing).

## Key Entities
- `Order` / `OrderItem` → `Upload` (`LargePreviewPath`, `ThumbnailPath`).
- `IStorageService.GetPresignedUrlAsync`.

## Dependencies
- **bolt 051**: cloud-located large preview + thumbnail to serve.

## Story Summary

| Story ID | Title | Priority |
|----------|-------|----------|
| 001-order-photos-endpoint | `GET /api/orders/{id}/photos` presigned URLs | Must |
| 002-order-detail-photo-grid | Order-detail thumbnail grid + lightbox | Must |
