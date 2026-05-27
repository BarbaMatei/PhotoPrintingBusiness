---
stage: test
bolt: 042-thumbnail-cache
created: 2026-05-27T11:45:00Z
---

## Test Report: 001-thumbnail-cache

### Summary

- **Tests**: 460/460 passed, 0 failed, 0 skipped (`dotnet test PhotoPrint.sln`) — was 457; +3 new.
- **Build**: clean (0 errors).

### Test Files

- [x] `Unit/Services/UploadServiceTests.cs` — 3 new tests for the caching + pixel-bomb behaviour, plus a constructor tweak (stub `IStorageService.GetStreamAsync`) so preview tests exercise the new cached-stream path.
- [x] `Integration/UploadFactory.cs` — `FakeStorageService.ExistsAsync` keeps the upload/preview integration tests green under the new flow.

### New tests

1. `GetPreviewAsync_SecondCall_StreamsCacheWithoutRegenerating` — two consecutive previews; `IImageProcessor.GenerateThumbnailAsync` is verified `Times.Once` (the second call is a cache hit → no ImageSharp work). **Covers story 002 AC.**
2. `GetPreviewAsync_CachedFileMissing_RegeneratesThumbnail` — `ThumbnailPath` set but `ExistsAsync` returns false → regenerates. **Covers the ops-deletion edge case.**
3. `UploadAsync_ImageDimensionsExceedLimit_ThrowsUnprocessableEntityException` — a (mocked) 30000×30000 image is rejected with `UnprocessableEntityException` → 422. **Covers story 003 AC.**

### Acceptance Criteria Validation

- ✅ **001** `Uploads.ThumbnailPath` nullable column added (migration `AddUploadThumbnailPath`); `Upload` exposes it; existing rows NULL.
- ✅ **002** First preview generates + persists + records the path; second streams the cached file with no `IImageProcessor` call (test 1); missing cached file regenerates (test 2); `Cache-Control: public, max-age=2592000, immutable` set on the response.
- ✅ **003** Oversized images rejected with 422 — enforced at upload (test 3) and, defence-in-depth, at decode in `ImageProcessor` before `Image.Load`.
- ✅ Full suite stays green (460/460).

### Issues Found

- None new. (The pre-existing SQLite-typed migration snapshot is documented in the walkthrough + DEPLOYMENT.md §7; not in scope here.)

### Notes

- **Pixel-bomb test is at the service guard, not a hand-crafted PNG.** Test 3 drives the dimension check via a mocked `GetInfoAsync` returning >25000 dims — this reliably verifies *our* threshold + 422 mapping. A real oversized-PNG integration test was deliberately not added: a hand-built PNG with a bad CRC would be rejected by ImageSharp as *corrupt* (right result, wrong reason), and reading real image dimensions is upstream ImageSharp behaviour. The decode-path guard in `ImageProcessor` is covered by inspection.
- Container/CI/live concerns are unchanged from bolt 040; this bolt added no infra.
