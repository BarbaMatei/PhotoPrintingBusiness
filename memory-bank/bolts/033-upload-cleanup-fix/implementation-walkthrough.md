---
stage: implement
bolt: 033-upload-cleanup-fix
created: 2026-05-25T11:15:00Z
---

## Implementation Walkthrough: 001-upload-cleanup-job-fix

### Summary

Patched `UploadCleanupJob` so its candidate query excludes uploads that any active `CartItem` or `OrderItem` references, with a separate long-window branch that still cleans up truly stale referenced data. Introduced a small validated configuration class for the two retention windows, wired into `Program.cs` with `ValidateOnStart`. The constructor change cascaded into the existing test file, which now binds the new options dependency through a shared helper.

### Structure Overview

A single new file (the settings class) and three modified files (the job, options registration, default config) plus a constructor-signature touch-up in the existing test file. No new background job, no new interface, no NuGet additions.

### Completed Work

- [x] `src/PhotoPrint.API/Configuration/UploadCleanupSettings.cs` — new options class with the two retention windows and the `SectionName` constant.
- [x] `src/PhotoPrint.API/BackgroundJobs/UploadCleanupJob.cs` — constructor now takes `IOptionsMonitor<UploadCleanupSettings>`; `CleanupAsync` runs the two-window reference-aware query with a 500-row batch cap, logs effective retention on first tick, and tick log now reports batch size. Method visibility relaxed from `private` to `internal` so reflection in tests is still satisfied while making intent clearer.
- [x] `src/PhotoPrint.API/Program.cs` — added an `AddOptions<UploadCleanupSettings>().Bind(...).Validate(...).ValidateOnStart()` block immediately above the existing `AddHostedService<UploadCleanupJob>()` registration.
- [x] `src/PhotoPrint.API/appsettings.json` — added the `UploadCleanup` section with the documented defaults of 24 h / 365 d.
- [x] `src/PhotoPrint.Tests/Unit/Services/UploadCleanupJobTests.cs` — added a `Settings(...)` helper that returns a Mock `IOptionsMonitor<UploadCleanupSettings>`; updated the five existing constructor calls to pass the new parameter. **No new test methods added in this stage**; those belong to Stage 3.

### Key Decisions

- **`IOptionsMonitor` over `IOptions`**: chosen so an admin can change retention values without restarting the API and the next tick picks them up — directly satisfies the "hot reload" edge case in story 002.
- **`internal` instead of `private` for `CleanupAsync`**: tests still use reflection (the existing pattern), but `internal` documents the testable seam more honestly. Reflection `BindingFlags.NonPublic` accepts both, so no test rewrite was needed at this stage.
- **First-tick log via a `_loggedRetentionOnce` flag** rather than logging on every tick: keeps the operational log clean while still surfacing the resolved values for audit at startup, matching the story's acceptance criterion.
- **Deterministic ordering** (`OrderBy(UploadedAt).Take(500)`): oldest go first across ticks, so even if a backlog accumulates the system drains the riskiest data first.

### Deviations from Plan

None affecting scope. One small extra: the constructor change forced an unplanned but obvious update to all five existing constructor calls in the test file. The plan only listed appending new tests; updating the existing five calls is an unavoidable consequence of the signature change and is logically part of Stage 2 (compile must stay green by stage end).

### Dependencies Added

None. No new NuGet packages. `Microsoft.Extensions.Options` was already a transitive dependency.

### Developer Notes

- The `UploadCleanupJobTests.cs` file is now ready for the Stage 3 additions (three new methods exercising the new query branches).
- Pre-existing build failure in the test project: `src/PhotoPrint.Tests/Integration/UploadFactory.cs` line 197 — `FakeStorageService` does not implement an `IStorageService.SaveAsync(...)` overload added in an earlier intent. **This breakage predates bolt 033 and is not in scope here.** It must be fixed (or `UploadFactory.cs` excluded from the test compile) before any tests in the project can run. Recommend tracking as a separate ops follow-up; do not widen this bolt.
- API project builds clean (`dotnet build src/PhotoPrint.API`) with zero new warnings.
