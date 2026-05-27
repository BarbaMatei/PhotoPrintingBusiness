---
stage: implement
bolt: 042-thumbnail-cache
created: 2026-05-27T11:30:00Z
---

## Implementation Walkthrough: 001-thumbnail-cache

### Summary

Preview thumbnails are now generated once and cached: a miss generates + stores the thumbnail and
records its path; a hit streams the stored file with no ImageSharp work. Added the `ThumbnailPath`
column + migration, an `ExistsAsync` storage primitive, a `Cache-Control` header, and a
decompression-bomb guard that rejects oversized images at both upload and decode.

### Structure Overview

- **Schema**: `Upload.ThumbnailPath` (nullable, max 512) + a single-column EF migration.
- **Storage**: `IStorageService` gains `ExistsAsync` (used to detect ops-side deletions).
- **Caching**: lives in `UploadService.GetPreviewAsync` (the service, where preview + auth already are); the controller stays thin and just adds the cache header.
- **Defence**: a shared `ImageProcessor.MaxDecodeDimension` cap enforced at upload (cheap Identify dims) and again before full decode.

### Completed Work

- [x] `Models/Upload.cs` — added `string? ThumbnailPath`.
- [x] `Data/Configurations/UploadConfiguration.cs` — `ThumbnailPath` nullable, `HasMaxLength(512)` (matches `FilePath`).
- [x] `Migrations/20260527102445_AddUploadThumbnailPath.cs` (+ Designer + snapshot) — `AddColumn Uploads.ThumbnailPath` (nullable, len 512).
- [x] `Services/IStorageService.cs` — new `ExistsAsync(path, ct)`.
- [x] `Services/LocalStorageService.cs` — `ExistsAsync` via `File.Exists`.
- [x] `Services/ImageProcessor.cs` — `public const MaxDecodeDimension = 25_000`; `GenerateThumbnailAsync` now `Identify`s and rejects > cap before `Image.Load` (`UnprocessableEntityException` → 422).
- [x] `Services/UploadService.cs` — (1) reject oversized images at upload (dims from `GetInfoAsync`); (2) reworked `GetPreviewAsync` to generate-store-record on a miss / stream cached on a hit / regenerate if the cached file is gone.
- [x] `Controllers/UploadsController.cs` — `Cache-Control: public, max-age=2592000, immutable` on preview.
- [x] `Tests/Integration/UploadFactory.cs` — `FakeStorageService.ExistsAsync` (in-memory).

### Key Decisions

- **Thumbnail path scheme**: the storage API generates UUID paths and can't write `thumbs/{id}.jpg`; and the original is saved with `fileId = uploadId`, so reusing it would collide. → thumbnails are saved with a **fresh UUID** under the owner dir, and the returned path is stored in `ThumbnailPath`.
- **Caching in the service**, with the entity loaded **tracked** (dropped `AsNoTracking`) so the `ThumbnailPath` write persists; the hit path does no `SaveChanges` and no ImageSharp call.
- **Pixel-bomb defence in two places**: reject at **upload** (best — never store a bomb; uses the header-only `Identify` dims already fetched) and again at **decode** in `ImageProcessor` (defence-in-depth for any pre-existing oversized file). Both surface 422.
- **`ExistsAsync` added to `IStorageService`** — needed here and by bolt 043 (cloud storage).

### Deviations from Plan

- **Migration is SQLite-typed (`TEXT`, len 512), not Npgsql `varchar(500)`.** Scaffolding under the Npgsql provider produced a **destructive 86 KB migration** (alter every column `TEXT`→Npgsql, drop the idempotency index) because the model **snapshot is already SQLite-typed** — collateral from bolt-035's SQLite-generated migration. I matched the existing snapshot instead → a clean single `AddColumn`. `TEXT` is valid on Postgres, so this is functionally correct; the **whole-history remediation stays the documented follow-up** (DEPLOYMENT.md §7), now with more evidence (the snapshot itself is SQLite-flavoured).
- Caching placed in the **service**, not the controller (story snippet); thumbnail stays **300px** (existing behaviour), not 800.
- **ImageSharp `Configuration.MaxImageWidth/Height` (story 003) is not used** — that API isn't present in ImageSharp 3.1.11; the `Identify`-dimension guard is the version-independent equivalent.

### Dependencies Added

- None (ImageSharp 3.1.11 already referenced; no new packages).

### Developer Notes

- Solution builds clean (0 errors).
- The decode guard `Identify`s then `stream.Position = 0` before `Load` — relies on a **seekable** stream (local `FileStream` is). Bolt 043's S3 stream must be seekable or buffered.
- **Stage 3 will test**: (a) two consecutive previews — second makes no `IImageProcessor` call (counter mock); (b) `ThumbnailPath` set but file missing → regenerates; (c) oversized image → 422 (upload-time via a mocked `GetInfoAsync` returning >25000 dims). Then full `dotnet test` green.
