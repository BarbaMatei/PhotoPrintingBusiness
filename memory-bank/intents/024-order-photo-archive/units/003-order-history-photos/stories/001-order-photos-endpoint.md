---
id: 001-order-photos-endpoint
unit: 003-order-history-photos
intent: 024-order-photo-archive
status: complete
priority: must
created: 2026-05-27T13:05:00Z
assigned_bolt: 053-order-history-photos
implemented: true
---

# Story: 001-order-photos-endpoint

## User Story

**As** a logged-in customer
**I want** an endpoint that returns my order's photos as viewable links
**So that** the order-detail page can show what I ordered

## Acceptance Criteria

- [ ] `GET /api/orders/{id}/photos` returns, per upload in the order: a **presigned thumbnail URL** and a **presigned large-preview URL** (1 h TTL), plus the original file name.
- [ ] **Authorization**: caller must own the order (or have claimed it); otherwise `403`/`404`. URLs are never issued to non-owners.
- [ ] Uploads whose blobs have aged out (post-retention) are **omitted**, not errored.
- [ ] If a photo isn't yet promoted (order not paid) the endpoint reflects current state gracefully.

## Technical Notes

- Uses `IStorageService.GetPresignedUrlAsync` for the `LargePreviewPath` + `ThumbnailPath` keys.
- Response shape: `{ photos: [{ fileName, thumbnailUrl, largeUrl }] }`.

## Dependencies

### Requires
- bolt 051 (cloud large preview + thumbnail).

### Enables
- 002-order-detail-photo-grid

## Out of Scope
- Serving the original (purged after printing).
- Guest tokenized access.
