---
id: 003-cleanup-regression-test
unit: 001-upload-cleanup-job-fix
intent: 013-upload-cleanup-fix
status: implemented
priority: must
created: 2026-05-25T10:00:00Z
assigned_bolt: 033-upload-cleanup-fix
implemented: true
implemented_at: 2026-05-25T11:35:00Z
---

# Story: 003-cleanup-regression-test

## User Story

**As** the backend team
**I want** an integration test that fails with the old query and passes with the new one
**So that** the upload-deletion regression cannot return unnoticed

## Acceptance Criteria

- [ ] **Given** a test API + EF Core test database, **And** an upload referenced by a `CartItem`, **When** the test fast-forwards a fake clock past `OrphanRetentionHours` and calls `UploadCleanupJob.CleanupAsync(ct)` once, **Then** the upload row still has `DeletedAt IS NULL` and the file remains in the test storage root.
- [ ] **Given** an upload not referenced by any cart or order, **And** the same fast-forward, **When** the cleanup runs, **Then** the upload row has `DeletedAt` set and the file is removed.
- [ ] **Given** an upload referenced only by an `OrderItem` of a `Paid` order, **When** the cleanup runs, **Then** the upload is retained.
- [ ] Test does NOT mock `IUploadRepository`, `ICartRepository`, `IOrderRepository`, `DbContext`, or `IStorageService`. Use the real test DB + `LocalStorageService` against a temp directory.

## Technical Notes

- File: `src/PhotoPrint.Tests/Integration/BackgroundJobs/UploadCleanupJobTests.cs`
- Use the existing `IntegrationTestFixture` (Testcontainers Postgres if available; SQLite fallback acceptable for this case).
- Inject `IClock` for time control: production binds `SystemClock`; test binds `FakeClock`.
- Three test methods: `Cleanup_skips_upload_referenced_by_cart`, `Cleanup_skips_upload_referenced_by_order_item`, `Cleanup_deletes_orphan_upload`.

## Dependencies

### Requires
- 001-skip-referenced-uploads, 002-retention-config

### Enables
- Bolt completion gate

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Test runs on SQLite (no JSONB) | Use minimal schema seed; do not rely on Postgres-only types |
| File system isolation | Test fixture creates and disposes a per-test temp dir |

## Out of Scope

- Load test or batch-size benchmark (covered by NFR but not asserted in CI).
