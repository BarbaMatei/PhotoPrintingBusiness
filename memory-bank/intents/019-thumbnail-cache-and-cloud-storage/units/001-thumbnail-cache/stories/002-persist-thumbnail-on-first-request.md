---
id: 002-persist-thumbnail-on-first-request
unit: 001-thumbnail-cache
intent: 019-thumbnail-cache-and-cloud-storage
status: complete
priority: must
created: 2026-05-25T10:30:00Z
assigned_bolt: 042-thumbnail-cache
implemented: true
---

# Story: 002-persist-thumbnail-on-first-request

## User Story

**As** an admin loading a 30-photo order
**I want** thumbnails to be generated once and cached
**So that** clicking through the gallery doesn't re-decode every full-resolution photo

## Acceptance Criteria

- [x] **Given** `Upload.ThumbnailPath IS NULL`, **When** `GET /api/uploads/{id}/preview` is called, **Then** the controller calls `ImageProcessor.GenerateThumbnailAsync(uploadId)` which saves the thumbnail via `IStorageService.SaveAsync(stream, fileId: uploadId, prefix: "thumbs")` → deterministic path `thumbs/{ownerId}/{uploadId:N}.jpg`, sets `Upload.ThumbnailPath`, persists, and returns the bytes.
  - **AC amended (REQ-2, review 042-v1):** the path is owner-scoped and namespaced (`thumbs/{ownerId}/{uploadId:N}.jpg`), not `thumbs/{id}.jpg`. A distinct namespace avoids colliding with the original (`{ownerId}/{uploadId:N}.jpg`); keying by the upload id makes a racing/cancelled write overwrite the same key rather than orphan a random file (BUG-3).
- [x] **Given** `Upload.ThumbnailPath IS NOT NULL` and the file exists, **When** the same endpoint is called, **Then** the controller streams the cached file directly — no ImageSharp invocation in this path.
- [x] **Given** `Upload.ThumbnailPath IS NOT NULL` but the file is missing, **When** called, **Then** the controller regenerates and overwrites (defensive against ops-side deletions).
- [x] Response headers set `Cache-Control` for the preview.
  - **AC amended (SEC-1, review 042-v1):** the directive is `private, max-age=2592000` (browser-only), **not** `public, …, immutable`. The preview is ownership-checked, so a `public` response would be stored by `ResponseCaching` (which runs before authentication, keyed only on the URL) and served to a different guest/anonymous client — a cross-user disclosure. `immutable` is dropped because a thumbnail can be regenerated.
- [x] Integration test: two consecutive preview calls; the second does not call `ImageProcessor` (counter-based mock). Reinforced by a fresh-context persistence test (TEST-3): the second request uses a new DbContext, proving `SaveChanges` ran rather than a shared tracker masking it.

## Technical Notes

```csharp
// UploadsController.GetPreview (excerpt)
var upload = await _uploads.GetAsync(id, ct);
if (upload is null) return NotFound();

if (upload.ThumbnailPath is null || !await _storage.ExistsAsync(upload.ThumbnailPath, ct))
{
    var thumbStream = await _imageProcessor.GenerateThumbnailAsync(upload, maxWidth: 800, ct);
    var thumbPath   = $"thumbs/{upload.Id}.jpg";
    await _storage.SaveAsync(thumbStream, thumbPath, ct);
    upload.ThumbnailPath = thumbPath;
    await _uploads.SaveAsync(upload, ct);
}

return File(
    await _storage.LoadAsync(upload.ThumbnailPath!, ct),
    contentType: "image/jpeg",
    fileDownloadName: null);

Response.Headers.CacheControl = "public, max-age=2592000, immutable";
```

## Dependencies

### Requires
- 001-thumbnail-path-schema

### Enables
- 002-cloud-storage-provider (cache is portable across providers)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Two concurrent first-requests for the same id | Both generate + write to the **same deterministic key** (overwrite, no orphan); last-writer-wins on the row is benign (identical path). No RowVersion needed (BUG-3). |
| Source image deleted (soft) but thumbnail persisted | **404.** *AC amended (REQ-3, review 042-v1):* `GetPreviewAsync` filters `DeletedAt == null`, and `GetPreviewAsync_SoftDeletedUpload_ThrowsNotFoundException` locks this in. Once the source is soft-deleted the upload is on its way out (cleanup deletes both files, BUG-2), so refusing to serve it is the defensible behavior — the original "return the thumbnail" was never implemented. |
| `ImageProcessor` throws on corrupt source | 422 (not 500): `GenerateThumbnailAsync` catches `ImageFormatException` and surfaces `UnprocessableEntityException` (BUG-4, review 042-v1). Do not retry forever. |

## Out of Scope

- Pre-generation during upload (would shift CPU cost earlier — defer).
