---
stage: plan
bolt: 042-thumbnail-cache
created: 2026-05-27T11:00:00Z
---

## Implementation Plan: 001-thumbnail-cache

### Objective

Generate each upload's preview thumbnail once and cache it, so repeat preview requests stream a
stored file instead of re-decoding the full-resolution image on every call; add the schema column
to track it; and harden ImageSharp against decompression-bomb (pixel-bomb) images.

### Deliverables (by story)

1. **001 — schema**: add `Upload.ThumbnailPath` (`string?`, nullable) + an **Npgsql-flavoured** EF migration adding `Uploads."ThumbnailPath" character varying(512) NULL` + snapshot update. Existing rows stay NULL.
2. **002 — cache on first request**: rework `UploadService.GetPreviewAsync` to generate→store→record on a miss and stream the cached file on a hit; add `IStorageService.ExistsAsync`; add a `Cache-Control` header on the preview response.
3. **003 — pixel-bomb defence**: cap decoded image dimensions (25000×25000) and reject oversized images with **422**.

### Grounding corrections (real code vs. the story snippets)

- **Storage API** is `GetStreamAsync(path)` / `SaveAsync(stream, ownerId, ext, ct, fileId?)` / `DeleteAsync(path)` — there is **no `LoadAsync` and no `ExistsAsync`**, and `SaveAsync` generates a **UUID path** (`{ownerId}/{fileId:N}.{ext}`); it can't write a `thumbs/{id}.jpg` path. → Thumbnails will be stored under the owner dir with their **own fresh UUID**, and that returned path tracked in `Upload.ThumbnailPath`. I'll **add `ExistsAsync`** to the interface (also useful for bolt 043).
- **`IImageProcessor.GenerateThumbnailAsync(string storagePath, ct)`** already exists (max-dimension **300px**, JPEG q85). Keep its behaviour — the story's "800px" is illustrative; no change to thumbnail size.
- **Caching belongs in `UploadService.GetPreviewAsync`** (the service), not the controller — that's where preview generation + ownership checks already live. The controller stays thin.
- The controller (`UploadsController.GetPreviewAsync`) already sets an **ETag + 304** path; I'll add `Cache-Control: public, max-age=2592000, immutable` there.
- **ImageSharp 3.1.11**: `Configuration.Default.MaxImageWidth/MaxImageHeight` (per story 003) **likely does not exist** in this version. Primary approach: a dimension guard in `ImageProcessor` using the already-present `Image.IdentifyAsync` (header-only, cheap) *before* `Image.LoadAsync`, throwing on oversize. If a built-in cap does exist in 3.1.11, prefer it; confirm at implement.
- **Migration provider**: generate/author the migration under the **Npgsql** design-time provider so the store types match the model.

### Dependencies

- **Requires**: 012-photo-upload-backend (the upload/preview/storage code — present) and 040 (deploy base — present).
- **Enables**: 043-cloud-storage-provider (the cache is path-based, so it's portable to an S3 backend).
- No new NuGet packages (ImageSharp 3.1.11 already referenced).

### Technical Approach

- **Schema**: add the property to `Upload`; author migration `AddUploadThumbnailPath` (Npgsql types) + update `PhotoPrintDbContextModelSnapshot`.
- **`IStorageService.ExistsAsync(string storagePath, ct)`**: add to the interface; `LocalStorageService` implements it via `File.Exists(Path.Combine(_basePath, storagePath))`. (S3 impl in bolt 043.)
- **`UploadService.GetPreviewAsync`** (rework):
  1. Load the `Upload` (tracked when a write may occur), authorise ownership (unchanged).
  2. If `ThumbnailPath` is null **or** `!ExistsAsync(ThumbnailPath)` → `GenerateThumbnailAsync(FilePath)`, `SaveAsync(thumb, ownerId, "jpg", ct, fileId: Guid.NewGuid())`, set `ThumbnailPath` to the returned path, `SaveChangesAsync`.
  3. Return `(GetStreamAsync(ThumbnailPath), "image/jpeg")` — **no ImageSharp call on the hit path**.
  - `ownerId` = `upload.UserId ?? upload.GuestSessionId!` (one is always set).
- **Controller**: add the `Cache-Control` header (thumbnails are UUID-keyed and immutable).
- **Pixel-bomb guard** (`ImageProcessor`): before `Image.LoadAsync` in `GenerateThumbnailAsync` (and on the `GetInfoAsync`/upload-validate path), `Image.IdentifyAsync` and if `Width > 25000 || Height > 25000` throw `UnprocessableEntityException("Image dimensions exceed limits.")` (already mapped to 422). Const `MaxDecodeDimension = 25000`.

### Open points to confirm at implement

- Whether ImageSharp 3.1.11 exposes a built-in dimension cap (use it if so; otherwise the Identify-guard above — which is version-independent and provably correct).
- Whether `dotnet ef` tooling is available to scaffold the migration; if not, hand-author it with Npgsql types (verified against the snapshot).
- Concurrent first-requests for the same id: last-writer-wins (per story edge case) — acceptable; a transient duplicate thumbnail file may be orphaned, which is harmless.

### Acceptance Criteria

- [ ] `Uploads.ThumbnailPath` nullable column added (Npgsql migration); existing rows NULL; `Upload` exposes the property.
- [ ] First `GET /api/uploads/{id}/preview` generates + persists the thumbnail and sets `ThumbnailPath`; the second call streams the cached file with **no `IImageProcessor` invocation** (counter-mock test).
- [ ] If `ThumbnailPath` is set but the file is missing, the endpoint regenerates and overwrites.
- [ ] Preview response carries `Cache-Control: public, max-age=2592000, immutable`.
- [ ] An image whose dimensions exceed 25000×25000 is rejected with **422** ("Image dimensions exceed limits").
- [ ] Full `dotnet test` stays green (incl. existing upload/preview tests adjusted for the new flow).
