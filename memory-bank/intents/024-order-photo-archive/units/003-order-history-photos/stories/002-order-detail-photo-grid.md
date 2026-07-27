---
id: 002-order-detail-photo-grid
unit: 003-order-history-photos
intent: 024-order-photo-archive
status: complete
priority: must
created: 2026-05-27T13:05:00Z
assigned_bolt: 053-order-history-photos
implemented: true
---

# Story: 002-order-detail-photo-grid

## User Story

**As** a logged-in customer
**I want** to see thumbnails of the photos I ordered and open them full-size
**So that** I can remember and review past orders

## Acceptance Criteria

- [ ] The account order-detail page renders a **thumbnail grid** of the order's photos (from `GET /api/orders/{id}/photos`).
- [ ] Clicking a thumbnail opens a **large-preview lightbox** (~2000 px).
- [ ] Loading and empty states handled (e.g. "Photos for this order are no longer available" once past retention).
- [ ] Lazy-load / on-demand presigned URLs so the page doesn't request every large preview up front.

## Technical Notes

- Reuse existing shared components (lightbox from intent 010 if present; shared loading/empty states from intent 012).
- Romanian copy for empty/expired states.

## Dependencies

### Requires
- 001-order-photos-endpoint.

### Enables
- Customer order-history review (intent goal).

## Out of Scope
- Backend changes (covered by 001-order-photos-endpoint).
- Re-ordering / re-printing from history.
