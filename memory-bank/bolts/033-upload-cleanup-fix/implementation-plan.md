---
stage: plan
bolt: 033-upload-cleanup-fix
created: 2026-05-25T11:00:00Z
---

## Implementation Plan: 001-upload-cleanup-job-fix

### Objective

Stop `UploadCleanupJob.CleanupAsync` from soft-deleting uploads that any active `CartItem` or `OrderItem` still references, expose its retention windows as validated configuration, and pin the behaviour with three regression tests that fail against the current query.

### Deliverables

1. New configuration class `src/PhotoPrint.API/Configuration/UploadCleanupSettings.cs` with two fields and section name.
2. `UploadCleanupJob` patched to:
   - Inject `IOptionsMonitor<UploadCleanupSettings>` (live-reload on next tick).
   - Replace the candidates LINQ with a reference-aware query in two windows (orphan short / referenced long).
   - Apply a `.Take(500)` batch cap.
   - Log effective retention on first tick and per-tick deleted/skipped/batch counts.
3. `Program.cs` registers the options via `AddOptions<UploadCleanupSettings>().Bind(...).Validate(...).ValidateOnStart()`.
4. `appsettings.json` adds the `UploadCleanup` section with the documented defaults.
5. Three new test methods appended to the existing `src/PhotoPrint.Tests/Unit/Services/UploadCleanupJobTests.cs`:
   - `Cleanup_skips_upload_referenced_by_cart`
   - `Cleanup_skips_upload_referenced_by_order_item`
   - `Cleanup_deletes_orphan_upload_past_referenced_window`  *(covers the long-window eligibility branch)*

### Dependencies

- `Microsoft.EntityFrameworkCore.InMemory` — already on the test project.
- `Moq`, `FluentAssertions`, `xUnit` — already used by the existing `UploadCleanupJobTests`.
- No new NuGet packages.

### Technical Approach

**1. `UploadCleanupSettings` (new)**

Plain init-only record with two `int` fields and the `SectionName = "UploadCleanup"` constant, defaulting to `OrphanRetentionHours = 24` and `ReferencedRetentionDays = 365`. Validation lives at registration time via `.Validate(s => s.OrphanRetentionHours > 0, "...")`.

**2. `UploadCleanupJob` patch**

- Constructor gains an `IOptionsMonitor<UploadCleanupSettings>` parameter and a `_loggedRetentionOnce` flag (private bool).
- Inside `CleanupAsync`:
  - Read `_settings.CurrentValue` once at the top of the method (snapshot per tick).
  - Compute `orphanCutoff = DateTimeOffset.UtcNow.AddHours(-settings.OrphanRetentionHours)` and `referencedCutoff = DateTimeOffset.UtcNow.AddDays(-settings.ReferencedRetentionDays)`.
  - First time the method runs after the process starts, log `UploadCleanupJob effective retention — orphan_hours={oh}, referenced_days={rd}` at Information and set the flag.
  - Replace the existing query with:
    ```csharp
    var candidates = await db.Uploads
        .Where(u => u.DeletedAt == null)
        .Where(u =>
            (u.UploadedAt < orphanCutoff
                && !db.CartItems .Any(ci => ci.UploadId == u.Id)
                && !db.OrderItems.Any(oi => oi.UploadId == u.Id))
            || u.UploadedAt < referencedCutoff)
        .OrderBy(u => u.UploadedAt)
        .Take(500)
        .ToListAsync(ct);
    ```
- Tick logging extended to `Upload cleanup: {Deleted} deleted, {Errors} file errors, batch_size={Batch}` so the integration test can observe batching behaviour through logs if needed (no assertion on log text — just operational visibility).

**3. `Program.cs`**

Replace nothing; add a single block above the `AddHostedService<UploadCleanupJob>()` line:

```csharp
builder.Services
    .AddOptions<UploadCleanupSettings>()
    .Bind(builder.Configuration.GetSection(UploadCleanupSettings.SectionName))
    .Validate(s => s.OrphanRetentionHours    > 0, "UploadCleanup:OrphanRetentionHours must be > 0")
    .Validate(s => s.ReferencedRetentionDays > 0, "UploadCleanup:ReferencedRetentionDays must be > 0")
    .ValidateOnStart();
```

**4. `appsettings.json`**

Append the section above (or alphabetically near other ops sections — `RateLimit`, `Storage`):

```json
"UploadCleanup": {
  "OrphanRetentionHours": 24,
  "ReferencedRetentionDays": 365
}
```

Defaults match the options class so omitting it is harmless.

**5. Tests**

Stay in the existing in-memory pattern (deviation note below). For each new test:

- `Cleanup_skips_upload_referenced_by_cart`: seed one `Upload` (UploadedAt = now − 25 h), one `CartItem` pointing at it, invoke `CleanupAsync` via reflection (same as siblings), assert `DeletedAt IS NULL` and `IStorageService.DeleteAsync` never called.
- `Cleanup_skips_upload_referenced_by_order_item`: seed one `Upload`, one `Order` with status `Paid`, one `OrderItem` pointing at the upload. Same assertion as above.
- `Cleanup_deletes_orphan_upload_past_referenced_window`: seed one `Upload` (UploadedAt = now − 400 d) referenced by a `CartItem`. Even though it's referenced, it's past the long window, so it must be deleted. This exercises the `|| u.UploadedAt < referencedCutoff` branch.

Each test uses an `IOptionsMonitor<UploadCleanupSettings>` stub returning defaults (`OrphanRetentionHours = 24`, `ReferencedRetentionDays = 365`).

### Deviations from Stories

The two below are intentional and were forced by the actual codebase state.

1. **No `IClock` abstraction introduced.**
   Story 001 technical notes referenced `_clock.UtcNow.AddHours(...)`, but no `IClock` exists in the project and the existing tests don't use one — they shift the *upload's* timestamp instead of the clock. Adding `IClock` would touch peer jobs (`GuestSessionCleanupJob`, `EmailRetryJob`, `AccountDeletionJob`) for consistency and widen this bolt's scope. Defer to a separate cleanup intent. Tests pass without it.

2. **No `IntegrationTestFixture`; tests append to the existing unit-test file.**
   Story 003 said "Use the existing `IntegrationTestFixture`" — no such fixture exists. The repo has per-feature factories under `Tests/Integration/` for `WebApplicationFactory`-based tests, but the existing `UploadCleanupJobTests.cs` is a unit test using `InMemoryDatabase` + a tiny `IServiceScopeFactory` built from `ServiceCollection`. That pattern already meets the story's spirit ("use the real `PhotoPrintDbContext`; only mock `IStorageService`"). Adding the three new tests there keeps the suite cohesive. If a real integration fixture lands later, the tests can be promoted; the assertions are the same.

### Acceptance Criteria (verbatim from stories, consolidated)

**From story 001 (skip referenced uploads):**

- [ ] An upload referenced by a `CartItem` is retained past `OrphanRetentionHours`.
- [ ] An upload referenced by an `OrderItem` (any order status) is retained.
- [ ] An unreferenced upload past `OrphanRetentionHours` is soft-deleted and its file removed.
- [ ] An upload referenced by something but older than `ReferencedRetentionDays` is eligible for deletion.

**From story 002 (retention config):**

- [ ] `appsettings.json` values resolve into `IOptions<UploadCleanupSettings>`.
- [ ] Missing section → defaults of 24 / 365 apply with no boot failure.
- [ ] Non-positive values cause boot to throw (`ValidateOnStart`).
- [ ] First tick logs `UploadCleanupJob effective retention — orphan_hours={oh}, referenced_days={rd}` at Information.

**From story 003 (regression test):**

- [ ] Three test methods exist with the exact responsibilities listed above.
- [ ] Tests use the real `PhotoPrintDbContext` (in-memory provider acceptable); only `IStorageService` is mocked.
- [ ] All three new tests pass; existing five tests in the file continue to pass; full `dotnet test` suite stays green.

### Risk Notes

- `IOptionsMonitor` vs. `IOptions`: chosen to support the "hot reload picks up on next tick" edge case in story 002. Resolves the same way in tests via a stub.
- The new query relies on EF's in-memory provider supporting correlated `Any()` sub-queries — it does, and the existing tests already exercise similar patterns elsewhere in the suite.
- `OrderBy(UploadedAt)` added before `Take(500)` so batching has deterministic order across ticks; oldest go first, which matches operator intent.
