---
id: 003-upload-preview-and-cleanup
unit: 001-upload-and-cart-backend
intent: 004-checkout-payment
status: draft
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 012-photo-upload-backend
implemented: false
---

# Story: 003-upload-preview-and-cleanup

## User Story

**As a** customer
**I want** to see a fast-loading thumbnail of my uploaded photo, and as a developer I want stale uploads removed automatically
**So that** the UI feels responsive and server storage doesn't grow unbounded

## Acceptance Criteria

- [ ] **Given** a valid upload ID owned by the current user/guest, **When** `GET /api/uploads/{id}/preview` is called, **Then** a JPEG thumbnail (max 300px on longest dimension) is returned with `Content-Disposition: inline` and `Cache-Control: max-age=3600` + `ETag`
- [ ] **Given** an unknown or soft-deleted upload ID, **When** the preview endpoint is called, **Then** 404 is returned
- [ ] **Given** an upload not linked to any `OrderItem` that is older than 24 hours, **When** `UploadCleanupJob` runs, **Then** the `Upload.DeletedAt` is set (soft delete) and the physical file is deleted via `IStorageService.DeleteAsync`
- [ ] **Given** an upload linked to a completed order, **When** `UploadCleanupJob` runs (even if upload is older than 24 h), **Then** the upload is NOT deleted
- [ ] **Given** `UploadCleanupJob` runs, **When** all eligible uploads are cleaned, **Then** the count of deleted files is logged via Serilog

## Technical Notes

- `GET /api/uploads/{id}/preview`: use `ImageSharp.Resize` with `ResizeMode.Max` and max dimension 300px; output as JPEG quality 85
- ETag: hash of `{uploadId}:{fileSizeBytes}` (stable, no file re-read needed)
- Ownership check: `Upload.UserId == currentUserId` OR `Upload.GuestSessionId == currentGuestSessionId`
- `UploadCleanupJob`: implement as `BackgroundService` with hourly timer
- Soft-delete query: `UPDATE Uploads SET DeletedAt = NOW() WHERE UploadedAt < NOW() - INTERVAL '24 hours' AND Id NOT IN (SELECT UploadId FROM OrderItems) AND DeletedAt IS NULL`
- Run physical deletes after soft-delete transaction commits (avoid partial state)

## Dependencies

### Requires
- Story 001-upload-entity-schema (IStorageService, Upload entity)
- Story 002-upload-endpoint (Upload records must exist to preview)
- Bolt 001 (Serilog configuration for cleanup logs)

### Enables
- Story 004-cart-item-entity (preview URL used in cart display)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| `IStorageService.DeleteAsync` fails during cleanup | Log error, continue with next upload — do not re-soft-delete |
| Preview requested for upload belonging to another user | Return 404 (not 403, to avoid enumeration) |
| Cleanup job runs while upload is being created | Newly created upload is < 24h old — not cleaned up |
| Image file corrupt / unreadable by ImageSharp | Return 500, log error, do not cache |

## Out of Scope

- Full-resolution file download
- Admin access to any user's uploads
