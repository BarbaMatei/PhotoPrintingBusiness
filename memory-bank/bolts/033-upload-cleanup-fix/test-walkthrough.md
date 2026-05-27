---
stage: test
bolt: 033-upload-cleanup-fix
created: 2026-05-25T11:30:00Z
---

## Test Report: 001-upload-cleanup-job-fix

### Summary

- **Tests**: 8/8 passed (5 pre-existing + 3 new)
- **Duration**: ~1 s
- **Coverage tool**: not run (suite is filtered, see Notes)

### Test Files

- [x] `src/PhotoPrint.Tests/Unit/Services/UploadCleanupJobTests.cs` — adds three reference-aware cleanup tests to the existing file; updates the existing five tests' constructor calls to pass the new `IOptionsMonitor<UploadCleanupSettings>` parameter via a shared `Settings(...)` helper.

### New Test Methods

- ✅ `Cleanup_skips_upload_referenced_by_cart` — seeds an `Upload` aged 25 h plus a `CartItem` pointing at it; asserts `DeletedAt` stays null and `IStorageService.DeleteAsync` is never called.
- ✅ `Cleanup_skips_upload_referenced_by_order_item` — seeds an `Upload` aged 48 h plus a `Paid` `Order` and an `OrderItem` pointing at it (with realistic `ShippingAddressSnapshot` + `ProductSnapshot`); asserts the upload is retained.
- ✅ `Cleanup_deletes_orphan_upload_past_referenced_window` — seeds an `Upload` aged 400 days, still referenced by a `CartItem`; asserts the long-window branch makes it eligible and the storage delete is called exactly once. This is the only branch that proves the `|| u.UploadedAt < referencedCutoff` clause works.

### Acceptance Criteria Validation

**Story 001 — skip-referenced-uploads**

- ✅ Upload referenced by `CartItem` past `OrphanRetentionHours` → retained.
- ✅ Upload referenced by `OrderItem` (status `Paid`) → retained.
- ✅ Orphan upload past `OrphanRetentionHours` → soft-deleted, file removed. *(Covered by the pre-existing `OldUndeleted_Upload_IsSoftDeletedAndFileRemoved` test, which continues to pass under the new query.)*
- ✅ Referenced upload past `ReferencedRetentionDays` → eligible for deletion.

**Story 002 — retention-config**

- ✅ `appsettings.json` values resolve through `IOptions<UploadCleanupSettings>` *(verified via `dotnet build` against the new section; runtime resolution exercised when the API hosts the job).*
- ✅ Missing section → defaults of 24 / 365 apply *(default property initialisers).*
- ✅ Non-positive values → boot throws *(via `.Validate(...).ValidateOnStart()`; verified by inspection of the `Program.cs` block — runtime smoke would be a follow-up integration test that intent 017 enables).*
- ✅ First tick logs `UploadCleanupJob effective retention — orphan_hours={oh}, referenced_days={rd}` *(by code inspection — log line and one-shot flag both present in the patched job).*

**Story 003 — cleanup-regression-test**

- ✅ Three test methods exist with the responsibilities described above.
- ✅ Tests use the real `PhotoPrintDbContext` (EF Core In-Memory provider); only `IStorageService` is mocked. No repository or DbContext mocks.
- ✅ All new tests pass; existing five tests continue to pass.

### Issues Found

None in the bolt's surface area. All blockers below predate this bolt and are documented as separate follow-up work — they did NOT block the bolt-033 acceptance because the bolt's tests are isolated and pass when run via filter.

### Notes

**Test execution strategy.** The full test project does not currently build because of pre-existing drift in three unrelated files:

- `src/PhotoPrint.Tests/Unit/Services/CartServiceTests.cs` (42 errors) — `CartResponseDto.Items` rename and `CartRequest.FinishName` required parameter added by the cart-finishes migration `20260524131359_AddFinishNameToCartItem`.
- `src/PhotoPrint.Tests/Integration/CartControllerIntegrationTests.cs` (26 errors) — same DTO/record drift.
- `src/PhotoPrint.Tests/Unit/Services/UploadServiceTests.cs` (2 errors) — Moq `It.IsAny<>()` expression-tree cannot bind to the optional `Guid? fileId` parameter added to `IStorageService.SaveAsync`.

To validate this bolt's tests, a temporary `src/PhotoPrint.Tests/Directory.Build.targets` was added that excluded those three files from compilation, the suite was run with `dotnet test ... --filter "FullyQualifiedName~UploadCleanupJobTests"`, and the targets file was deleted immediately after. The repo state at bolt completion is identical to what the user approved at the Stage 2 checkpoint plus the new tests in `UploadCleanupJobTests.cs`.

**Follow-up bolt (option B from the Stage 2 review).** A separate bolt should repair the three broken test files above so the full test project builds and the bolt-033 tests run in CI without the isolation trick. Suggested name: `bolt-049-test-project-drift-repair` (out of scope for intent 013; lives under a new ops/test-debt intent that the team can wedge in after intent 014 cart-related work settles).

### Filter Command for Future Verification

```text
dotnet test src/PhotoPrint.Tests/PhotoPrint.Tests.csproj \
  --filter "FullyQualifiedName~UploadCleanupJobTests"
```

(This will start passing end-to-end without the targets-file trick once the test-project-drift repair bolt lands.)
