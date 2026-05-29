---
id: 003-promote-on-paid
unit: 001-order-photo-promotion
intent: 024-order-photo-archive
status: draft
priority: must
created: 2026-05-27T13:05:00Z
assigned_bolt: 051-order-photo-promotion
implemented: false
---

# Story: 003-promote-on-paid

## User Story

**As** the platform
**I want** an order's photos promoted to cloud storage as soon as it's paid, then removed from the local server
**So that** only fulfilled orders incur cloud storage and the deployment server stays lean

## Acceptance Criteria

- [ ] On order → **Paid** (Stripe webhook / EuPlatesc IPN), promotion is **enqueued** — the webhook returns immediately (no blocking upload on the hot path).
- [ ] A background `OrderPhotoPromoter` processes the queue: for each upload in the order it uploads the **original** (`uploads/`), generates + uploads the **large preview** (`previews/`), ensures the **thumbnail** (`thumbs/`), sets `StorageLocation = Cloud` and the three keys.
- [ ] **Local files are deleted only after** all cloud writes for that upload are confirmed.
- [ ] **Idempotent**: an upload already at `StorageLocation = Cloud` is skipped; re-processing an order is a no-op.
- [ ] Transient failures retried (Polly); a permanently failing upload is logged at Error and **leaves the local copy intact** for retry/investigation.
- [ ] Each upload row is updated **atomically** after its objects are written.

## Technical Notes

- Queue: in-process `Channel<Guid>` (order id) + a hosted `BackgroundService` worker; bounded concurrency (e.g. 4).
- Survive restarts: on startup, re-enqueue Paid orders that still have `StorageLocation = Local` uploads (recovery scan) — so a crash between Paid and promotion self-heals.
- Hook point: the order-status transition to Paid (confirm exact location in the status machine during design).

## Dependencies

### Requires
- 001-archive-schema, 002-large-preview-generation; bolt 043 (S3 adapter, `StorageKeys`, `StorageLocation`).

### Enables
- unit 002 (retention), unit 003 (viewing), 004-backfill-paid-orders

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Cloud write fails mid-order | Promoted uploads stay promoted; failed ones keep local copy; retried |
| Local source file missing | Logged Warning; row flagged for manual investigation; not deleted-twice |
| Crash after upload, before local delete | Next run sees `StorageLocation = Cloud` + lingering local file → deletes local (idempotent) |

## Out of Scope
- Purging the original after printing (unit 002).
