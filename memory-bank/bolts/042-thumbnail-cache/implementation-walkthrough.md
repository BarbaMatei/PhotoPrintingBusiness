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
- **Defence**: a shared `ImageProcessor.MaxDecodePixels` (100 MP total-pixel area — BUG-1/NEW-1) cap enforced at upload (cheap Identify dims) and again before full decode.

### Completed Work

- [x] `Models/Upload.cs` — added `string? ThumbnailPath`.
- [x] `Data/Configurations/UploadConfiguration.cs` — `ThumbnailPath` nullable, `HasMaxLength(512)` (matches `FilePath`).
- [x] `Migrations/20260527102718_AddUploadThumbnailPath.cs` (+ Designer + snapshot) — provider-aware `AddColumn Uploads.ThumbnailPath` (nullable; `character varying(512)` on Npgsql, `TEXT` on PostgreSQL — DB-1, review 042-v1).
- [x] `Services/IStorageService.cs` — new `ExistsAsync(path, ct)`.
- [x] `Services/LocalStorageService.cs` — `ExistsAsync` via `File.Exists`.
- [x] `Services/ImageProcessor.cs` — `public const MaxDecodePixels = 100_000_000` (100 MP total-pixel area cap — BUG-1/NEW-1); `GenerateThumbnailAsync` `Identify`s and rejects over-cap images before `Image.Load` (`UnprocessableEntityException` → 422) and decodes with `MaxFrames=1`.
- [x] `Services/UploadService.cs` — (1) reject oversized images at upload (dims from `GetInfoAsync`); (2) reworked `GetPreviewAsync` to generate-store-record on a miss / stream cached on a hit / regenerate if the cached file is gone.
- [x] `Controllers/UploadsController.cs` — `Cache-Control: private, max-age=2592000` on preview (`private`, no `immutable`: a per-user, ownership-checked, regenerable resource — SEC-1/QUAL-4, review 042-v1).
- [x] `Tests/Integration/UploadFactory.cs` — `FakeStorageService.ExistsAsync` (in-memory).

### Key Decisions

- **Thumbnail path scheme**: thumbnails are saved under a **deterministic, id-keyed path** in a distinct `thumbs/{owner}/{id}.jpg` namespace (BUG-3/REQ-2, review 042-v1) so it can't collide with the original (`{owner}/{id}.jpg`) and a racing/cancelled write overwrites the same key instead of leaking a random file; the cleanup job targets that exact key, and the path is stored in `ThumbnailPath`.
- **Caching in the service**: the entity is read `AsNoTracking` (QUAL-1) and the miss path `Attach`es it + marks only `ThumbnailPath` modified so the write persists without tracking the whole graph; the hit path does no `SaveChanges` and no ImageSharp call.
- **Pixel-bomb defence in two places**: reject at **upload** (best — never store a bomb; uses the header-only `Identify` dims already fetched) and again at **decode** in `ImageProcessor` (defence-in-depth for any pre-existing oversized file). Both surface 422.
- **`ExistsAsync` added to `IStorageService`** — needed here and by bolt 043 (cloud storage).

### Deviations from Plan

- **Migration is provider-aware** (DB-1, review 042-v1): it emits `character varying(512)` on Npgsql and `TEXT` on PostgreSQL (mirroring the sibling `AddOrderIdempotencyKey`), so the shipped column matches the Npgsql runtime model and no phantom `AlterColumn` is scaffolded on a Postgres `migrations add`. The model **snapshot remains Npgsql-typed** (bolt-035 legacy), so an Npgsql regeneration would still diff `TEXT`→`varchar(512)` for every column — the **whole-history / per-provider-assembly remediation stays the documented follow-up** (DEPLOYMENT.md §7; deferred to the 3-env phase — v4 L10/DB-1).
- Caching placed in the **service**, not the controller (story snippet); thumbnail long edge is **800px** per stories 001/002 (C7, review 042-v4 — was 300px pre-review).
- **ImageSharp `Configuration.MaxImageWidth/Height` (story 003) is not used** — that API isn't present in ImageSharp 3.1.11; the `Identify`-dimension guard is the version-independent equivalent.

### Dependencies Added

- None (ImageSharp 3.1.11 already referenced; no new packages).

### Developer Notes

- Solution builds clean (0 errors).
- The decode guard `Identify`s then `stream.Position = 0` before `Load` — relies on a **seekable** stream (local `FileStream` is). Bolt 043's S3 stream must be seekable or buffered.
- **Stage 3 will test**: (a) two consecutive previews — second makes no `IImageProcessor` call (counter mock); (b) `ThumbnailPath` set but file missing → regenerates; (c) oversized image → 422 (upload-time via a mocked `GetInfoAsync` returning an over-100 MP area, e.g. 30000×30000). Then full `dotnet test` green.
