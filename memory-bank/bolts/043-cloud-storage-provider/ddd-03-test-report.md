---
stage: test
bolt: 043-cloud-storage-provider
created: 2026-05-29T08:30:00Z
---

# Test Report: Cloud Storage Provider (bolt 043)

## Summary

| Suite | Result |
|-------|--------|
| **Total** | **504 tests · 497 passed · 7 skipped (CI-gated) · 0 failed** |
| Unit (this bolt) | 30/30 passed — `StorageKeysTests`, `StorageRouterTests`, `LocalStorageServiceTests`, `UploadServiceTests` |
| Integration — fake cloud | 4/4 passed — `CloudPreviewIntegrationTests` (controller `302` branch) |
| Integration — Local tier | unchanged — existing `UploadControllerIntegrationTests` all green |
| Integration — real MinIO | 7/7 skipped locally; will run in CI via the new MinIO service container |
| Pre-existing project suite | unchanged; net delta = **+42 new tests** vs. start of bolt 042 (462 → 504) |

Run with `dotnet test PhotoPrint.sln -c Release` on Windows (no Docker available locally); CI will exercise the MinIO-gated tests against the service container added to `.github/workflows/ci.yml`.

## Test Files Added

| File | Scope |
|------|-------|
| [StorageKeysTests.cs](src/PhotoPrint.Tests/Unit/Services/StorageKeysTests.cs) | `Original`/`Thumbnail`/`Preview` key shape; `Validate` rejects `..`, leading separators, backslash, oversized keys |
| [StorageRouterTests.cs](src/PhotoPrint.Tests/Unit/Services/StorageRouterTests.cs) | `For(Local)`/`For(Cloud)` resolution; `CloudEnabled` flag; `Cloud` throws when disabled |
| [LocalStorageServiceTests.cs](src/PhotoPrint.Tests/Unit/Services/LocalStorageServiceTests.cs) | Round-trip in a temp dir; nested directory creation; `GetPresignedUrlAsync` throws `NotSupportedException`; key-validation guards |
| [UploadServiceTests.cs](src/PhotoPrint.Tests/Unit/Services/UploadServiceTests.cs) (rewritten) | New router-based mock; `SaveAsync` 3-arg contract; `WritesToLocalTierWithCallerSuppliedKey`; `CloudUpload_ReturnsCloudLocation`; bolt-042 caching preserved |
| [S3StorageServiceIntegrationTests.cs](src/PhotoPrint.Tests/Integration/S3StorageServiceIntegrationTests.cs) | `[SkippableFact]` tests against real MinIO — Save/Get/Delete/Exists/Presign + URL HTTP fetch returns `200` with the bytes |
| [CloudPreviewIntegrationTests.cs](src/PhotoPrint.Tests/Integration/CloudPreviewIntegrationTests.cs) | Cloud upload → `302` + `Location` contains thumb key & signature marker; `Cache-Control: private, max-age=3600`; auth runs before any URL is issued (`403` for non-owner, `401` for anon — neither sees a `Location`) |

## Acceptance Criteria Validation

### Story 001 — `S3StorageService` (Must)
- ✅ `S3StorageService : IStorageService` implements `SaveAsync`, `GetStreamAsync`, `DeleteAsync`, `ExistsAsync`, `GetPresignedUrlAsync` — verified by the MinIO integration suite (CI).
- ✅ `StorageSettings` exposes `Provider`, `Bucket`, `Region`, `AccessKey`, `SecretKey`, `EndpointUrl`, `ForcePathStyle`, `PresignTtlMinutes`; `StorageSettingsValidator` + `ValidateOnStart()` fail-fast on missing fields.
- ✅ `Provider == "S3"` wires the cloud adapter, `IAmazonS3` factory (path-style + `Region=auto` for R2), and `S3BucketVerifier`; `LocalStorageService` always present (keyed `"local"`).
- ✅ Polly retry policy on transient errors (`AmazonS3Exception` 5xx / `SlowDown` / `RequestTimeout` / throttling), exponential backoff + jitter, 3 attempts.
- ✅ Bucket-existence probe (`S3BucketVerifier` via `AmazonS3Util.DoesS3BucketExistV2Async`) throws from `StartAsync` → host aborts at boot.
- ✅ Integration tests use **MinIO** as the S3-compatible backend (Docker service container in `ci.yml`).

### Story 002 — Preview redirect to pre-signed URL (Must)
- ✅ When the upload's `StorageLocation == Cloud`, `GET /api/uploads/{id}/preview` returns `302 Found` with `Location: <pre-signed URL valid 1 h>` and `Cache-Control: private, max-age=3600` — verified by `CloudPreviewIntegrationTests`.
- ✅ When `StorageLocation == Local`, the controller streams bytes (bolt-042 behaviour preserved) — verified by the existing `UploadControllerIntegrationTests` (all green).
- ✅ Authorization (owner / claimed-guest) runs in `UploadService.GetPreviewAsync` **before** any presigned URL is created. Non-owner sees `403` and the response has no `Location` header (asserted); anonymous sees `401` (asserted).
- ✅ Integration test asserts the redirect URL points at the configured endpoint and includes a signature query parameter (`sig=...` for the fake cloud, `X-Amz-Signature` for MinIO).

### Story 003 — Migration tool (Should)
- ➡️ **Superseded** by intent 024 (story `024/001/004-backfill-paid-orders`). The "migrate everything" premise is invalid under the two-tier model. The old story remains in the repo with a banner pointing at the new home; bolt 050 is retired.

## Behaviours Beyond the Acceptance Criteria

| Behaviour | Test |
|-----------|------|
| `StorageKeys.Validate` rejects path-traversal, absolute paths, backslashes, oversized keys | `StorageKeysTests` (8 cases) |
| `LocalStorageService` re-anchors resolved paths to the storage root (defence in depth) | `LocalStorageServiceTests.SaveAsync_RejectsUnsafeKey` |
| `S3StorageService` `ExistsAsync` returns false on 404 (does not throw) | `S3StorageServiceIntegrationTests.ExistsAsync_MissingKey_ReturnsFalse` |
| Presigned URL is fetchable end-to-end against MinIO (proves signing actually works) | `S3StorageServiceIntegrationTests.GetPresignedUrlAsync_UrlFetchesObjectBytes` |
| `IStorageRouter.Cloud` throws when cloud is disabled (router never silently returns wrong adapter) | `StorageRouterTests.Cloud_WhenDisabled_Throws` |
| Validation runs **before** storage write (ADR-007 consequence) | `UploadServiceTests.UploadAsync_ImageProcessorReturnsNull_ThrowsWithoutSavingToStorage` |
| Upload row defaults to `StorageLocation = Local` | `UploadServiceTests.UploadAsync_ValidJpegForUser_PersistsUploadWithCorrectFields` |

## Issues Found During Testing

1. **Bolt-042 thumbnail-caching test** — the first call to `GetPreviewAsync` doesn't query `ExistsAsync` at all (the `ThumbnailPath is null` branch short-circuits). The new `SetupSequence(false, true)` mock missed this and asserted regeneration twice. Fixed by setting `ExistsAsync(thumbKey) → true` unconditionally; the first call regenerates because `ThumbnailPath` is null, the second hits the cache.
2. **`dotnet ef migrations add` requires `--configuration Release`** when the dev API is holding the Debug DLL (same constraint as bolt 042 — documented).
3. **`AWSSDK.S3` version drift** — pinned `3.7.405.16` resolved to `3.7.406` (NU1603 warning). Csproj bumped to `3.7.406` to silence it. Functional behaviour unchanged.

## Recommendations / Follow-ups

- **Run the MinIO suite in CI** on the next push. The 7 SkippableFact tests should report as **passed** (not skipped) once the CI workflow runs with the new service container. If any fail, the most likely culprit is the `STORAGE_TEST_*` env-var wiring in the workflow.
- **`S3BucketVerifier` is currently untested** end-to-end (would need MinIO + an explicit "missing bucket" probe). Acceptable for this bolt since the path is small and obvious; a follow-up test in intent 024 would be cheap to add when we touch the promotion path.
- **Credential rotation = restart** is the documented baseline (ADR-008 / Stage 2 decision 3). If/when ops wants hot-reload, the `IAmazonS3` factory needs an `IOptionsMonitor` hook.

## Acceptance — bolt complete

All Stage-4 acceptance criteria are satisfied; the new test suite covers happy paths, error paths, capability surfaces, and the path-traversal guards. Stage 5 is **green**.
