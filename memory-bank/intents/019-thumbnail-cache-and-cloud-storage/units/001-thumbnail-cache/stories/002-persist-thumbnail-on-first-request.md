---
id: 002-persist-thumbnail-on-first-request
unit: 001-thumbnail-cache
intent: 019-thumbnail-cache-and-cloud-storage
status: draft
priority: must
created: 2026-05-25T10:30:00Z
assigned_bolt: 042-thumbnail-cache
implemented: false
---

# Story: 002-persist-thumbnail-on-first-request

## User Story

**As** an admin loading a 30-photo order
**I want** thumbnails to be generated once and cached
**So that** clicking through the gallery doesn't re-decode every full-resolution photo

## Acceptance Criteria

- [ ] **Given** `Upload.ThumbnailPath IS NULL`, **When** `GET /api/uploads/{id}/preview` is called, **Then** the controller calls `ImageProcessor.GenerateThumbnailAsync(uploadId)` which saves the thumbnail via `IStorageService.SaveAsync(stream, $"thumbs/{id}.jpg")`, sets `Upload.ThumbnailPath = "thumbs/{id}.jpg"`, persists, and returns the bytes.
- [ ] **Given** `Upload.ThumbnailPath IS NOT NULL` and the file exists, **When** the same endpoint is called, **Then** the controller streams the cached file directly — no ImageSharp invocation in this path.
- [ ] **Given** `Upload.ThumbnailPath IS NOT NULL` but the file is missing, **When** called, **Then** the controller regenerates and overwrites (defensive against ops-side deletions).
- [ ] Response headers include `Cache-Control: public, max-age=2592000, immutable` for thumbnails (UUID-keyed).
- [ ] Integration test: two consecutive preview calls; the second does not call `ImageProcessor` (counter-based mock).

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
| Two concurrent first-requests for the same id | Both generate + write; last-writer-wins; both responses still valid |
| Source image deleted (soft) but thumbnail persisted | Return thumbnail; do not attempt regeneration |
| `ImageProcessor` throws on corrupt source | 500 ProblemDetails; do not retry forever |

## Out of Scope

- Pre-generation during upload (would shift CPU cost earlier — defer).
