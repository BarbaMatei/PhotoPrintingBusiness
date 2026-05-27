---
stage: test
bolt: 049-test-project-drift-repair
created: 2026-05-25T12:15:00Z
---

## Test Report: 001-test-project-drift-repair

### Summary

- **Tests**: 433 / 433 passed
- **Duration**: 7 s
- **Failures owned by bolt 049**: 0
- **Failures from hidden pre-existing drift unmasked by the build fix**: 1 — fixed inline at user direction during Stage 3 review (one-line test update)

### Test Files

- [x] `src/PhotoPrint.Tests/Unit/Services/UploadServiceTests.cs` — Moq setup for `IStorageService.SaveAsync` updated for the new optional `fileId` parameter.
- [x] `src/PhotoPrint.Tests/Unit/Services/CartServiceTests.cs` — `SeedCartItemAsync` helper now takes `sizeId`; every `CartRequest` ctor migrated to the 4-arg form with `FinishName: null`; every `result.Items` rewritten against `result.Groups[].Items`.
- [x] `src/PhotoPrint.Tests/Integration/CartControllerIntegrationTests.cs` — same `CartRequest` and `CartResponseDto` rewrites; reads `size.Id` from `product.Sizes.First()`.
- [x] `src/PhotoPrint.Tests/Integration/CartFactory.cs` — `SeedProductAsync` hydrates the `Sizes` / `PricingTiers` / `Finishes` navigation collections; `SeedCartItemAsync` accepts `sizeId`.

### Acceptance Criteria Validation

**Story 001 — UploadServiceTests Moq setup**

- ✅ `dotnet build src/PhotoPrint.Tests` reports no CS0854 in `UploadServiceTests.cs`.
- ✅ Every `Setup`/`Verify` on `IStorageService.SaveAsync` binds to the new 5-arg overload.

**Story 002 — CartServiceTests grouped DTO**

- ✅ Zero compile errors in `CartServiceTests.cs`.
- ✅ All 12 test methods pass against the new contract.
- ✅ Each rewritten assertion verifies the same behaviour as before (per-method audit recorded in the implementation walkthrough's "Completed Work" section).

**Story 003 — CartControllerIntegrationTests grouped DTO**

- ✅ Zero compile errors in `CartControllerIntegrationTests.cs`.
- ✅ All 8 test methods pass.
- ✅ Every `CartRequest(...)` call passes `FinishName: null`.

**Story 004 — Suite green verification**

- ✅ No `Directory.Build.targets` or other `<Compile Remove>` mechanism exists under `src/PhotoPrint.Tests/`.
- ✅ `dotnet build src/PhotoPrint.Tests` exits 0.
- ✅ `dotnet test src/PhotoPrint.Tests` discovers and runs all 433 test methods (zero `Skipped`, zero `Failed`).
- ✅ Bolt-033's three new tests (`Cleanup_skips_upload_referenced_by_cart`, `Cleanup_skips_upload_referenced_by_order_item`, `Cleanup_deletes_orphan_upload_past_referenced_window`) all pass with no filter.

### Hidden Drift Found and Fixed Inline

`UploadServiceTests.UploadAsync_GuestAtUploadCap_ThrowsTooManyRequestsException` was initially failing.

**Root cause** — pre-existing production drift, NOT a bolt-049 regression:

- The test ([UploadServiceTests.cs:98](src/PhotoPrint.Tests/Unit/Services/UploadServiceTests.cs#L98)) was seeding **30** active uploads and expecting `TooManyRequestsException`.
- Production code ([UploadService.cs:12](src/PhotoPrint.API/Services/UploadService.cs#L12)) declares `private const int MaxUploadsPerSession = 100;`.
- The cap was raised from 30 → 100 in an earlier intent. The test never got updated. The test project hadn't built for weeks, so the failure stayed silent.

**Resolution**

User opted (during Stage 3 review) to fix inline rather than spin a separate bolt. One-line change: `for (int i = 0; i < 30; i++)` → `for (int i = 0; i < 100; i++)` plus comment update. Suite re-run reported 433 / 433 passed.

This was the only scope-creep concession in bolt 049 and is documented here as such. Production code remains untouched.

### Filter Command Comparison (vs. Bolt 033)

| Before bolt 049 | After bolt 049 |
|---|---|
| `dotnet test --filter "FullyQualifiedName~UploadCleanupJobTests"` *(required `Directory.Build.targets` trick)* | `dotnet test` *(no filter, no exclusions, 432 / 433 pass)* |

### Notes

- The Stripe NuGet `NU1603` warnings persist (Stripe 46.3.0 → 47.0.0 resolution). Not introduced by this bolt; out of scope.
- The `CS1998` warning on `RazorTemplateServiceTests.cs:82` (async method without await) also predates this bolt.
- No tests were quarantined with `[Fact(Skip = ...)]`. Every original test in the three repaired files has an equivalent assertion against the new contract.
