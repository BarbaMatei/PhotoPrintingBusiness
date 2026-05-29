---
unit: 002-archive-retention
bolt: 052-archive-retention
stage: test
status: complete
updated: 2026-05-29T13:25:00Z
---

# Test Report — Archive Retention

## Summary

| Surface | Passed | Failed | Skipped | Total |
|---------|-------:|-------:|--------:|------:|
| Unit (new to bolt 052) | 50 | 0 | 0 | 50 |
| **Full suite** | **583** | **0** | **7** (CI-gated MinIO) | **590** |

`dotnet test PhotoPrint.sln --no-build -c Release` — Duration: 7 s.

Bolt 051 left the project at 540 tests (533 passing + 7 CI-gated). Bolt 052 added **50
new** without churning any of the existing 540, after a small handful of cross-cutting
adjustments (Upload.FilePath nullability ripple — caught immediately by the build and
fixed before any test touched it).

## New Test Files

| File | Tests | What it covers |
|------|------:|----------------|
| `Unit/Services/OriginalPurgerTests.cs` | 9 | Pre-flight refusals (missing order, cloud off, archive disabled), idempotency (FilePath already null), defence-in-depth (Local upload skipped), happy path (delete + null FilePath + set OriginalPurgedAt), large preview + thumbnail explicitly preserved, single-upload cloud-delete failure, two-upload partial failure |
| `Unit/Services/ArchiveRetentionJobTests.cs` | 8 | Sweep filter correctness — no-op when nothing expired, happy path (both blobs deleted + both keys nulled), already-expired rows filtered out, half-blob state (preview but no thumb), per-upload delete failure handling, cloud-off no-op, Local-tier uploads ignored, configurable retention window picks recent rows when short |
| `Unit/Services/OriginalPurgeRecoveryScannerTests.cs` | 11 | Archive-disabled + cloud-off skip, no-stuck-orders no-op, **Theory:** Shipped/Delivered orders with non-null FilePath fire the purger, **Theory:** AwaitingPayment/Paid/Printing/Cancelled/PaymentFailed never fire, `PurgeOriginalAtStatus=Delivered` config narrows the floor to Delivered-only |
| `Unit/Configuration/ArchiveSettingsValidatorTests.cs` | 18 | `.ValidateOnStart()` surface: defaults pass, allowed-statuses Theory (Shipped/Delivered/case-variants), disallowed-statuses Theory (Paid/Printing/Cancelled/PaymentFailed/empty/bogus), negative retention/interval/batch all fail with named-property messages, `IsProductionCompleteStatus` + `ProductionCompleteFloor` helper behavior under both default and Delivered-only configs |
| `Unit/Services/AdminOrderServiceTests.cs` *(extended)* | +4 | Printing→Shipped triggers purger, Paid→Printing does NOT trigger purger, Shipped→Delivered does NOT re-trigger purger with default config, `PurgeOriginalAtStatus=Delivered` flips the trigger so only Delivered fires it |

## Acceptance Criteria Validation

### Story 001 — Purge Original on Shipped

- ✅ Order transition to configurable production-complete status (default Shipped) triggers cloud-original delete for each upload, nulls `FilePath`, sets `OriginalPurgedAt` — verified by `OriginalPurgerTests.HappyPath_DeletesCloudOriginal_FlipsRow` + `AdminOrderServiceTests.UpdateStatusAsync_PrintingToShipped_TriggersOriginalPurge`.
- ✅ Large preview + thumbnail retained — verified by `OriginalPurgerTests.LargePreviewAndThumbnailPreserved` (asserts both paths still non-null after purge AND no `DeleteAsync` calls on either key).
- ✅ Idempotent — verified by `OriginalPurgerTests.AlreadyPurged_Skips` (no cloud calls, outcome reports `Skipped = 1`).
- ✅ Configurable via `ArchiveSettings:PurgeOriginalAtStatus` — verified by `AdminOrderServiceTests.UpdateStatusAsync_ConfigSetToDelivered_OnlyDeliveredTriggersPurge` + `ArchiveSettingsValidatorTests.IsProductionCompleteStatus_ConfiguredDelivered_OnlyDeliveredMatches`.

### Story 002 — Retention Cleanup Job

- ✅ Background job finds uploads whose order paid > retention window ago and deletes their large preview + thumbnail — verified by `ArchiveRetentionJobTests.ExpiredUpload_DeletesBothBlobs_NullsBothKeys`.
- ✅ Window configurable via `ArchiveSettings:RetentionMonths` — verified by `ShortRetentionWindow_HitsRecentOrders` (RetentionMonths=1 sweep picks up an order paid 2 months ago).
- ✅ Order + order-item metadata retained — implicit in design (we never touch `Order` or `OrderItem`); the retention job only mutates `Upload.LargePreviewPath` and `Upload.ThumbnailPath`.
- ✅ Orders within the window are never touched — verified by `NoExpiredUploads_NoOp` (paid yesterday, default 12-month window → zero work).
- ✅ Per-run summary returned (cleaned, blobs, failed counters) — verified across multiple sweep tests.

## Bugs Caught by Tests

1. **`Upload.FilePath` non-nullable column.** The model + EF Fluent config had `IsRequired()` on `FilePath`; the story-001 purge needs to null it. Caught at *build time* (CS8625 from `OriginalPurger.cs:114`), not at test time — fixed via a one-column-nullability migration + three readers updated (`UploadService`, `OrderPhotoPromoter`, `UploadCleanupJob`) before any test ran. Recorded in the Stage-4 correction footer of `ddd-02-technical-design.md`.

## Test Patterns Used

- **InMemory EF provider** for everything DbContext-shaped, same as bolts 042/043/051.
- **`Mock<IStorageService>` in strict mode for cloud** — any unexpected call fails the test. Used for the "no cloud activity when cloud off / archive disabled" assertions.
- **Reflection-based access to `ArchiveRetentionJob.SweepAsync`** — matches the existing `UploadCleanupJob`-test pattern of test-driving the inner work loop without exposing it on the public API.
- **`xUnit Theory` for status-filter exhaustiveness** — `OrderStatus` has 7 values; `OriginalPurgeRecoveryScanner` should treat each one precisely. A `Theory` with the 5 pre-purge statuses + a separate one with the 2 at-or-past statuses gives full coverage in 7 lines.

## Issues Found

None. The purger behaves to spec; the retention job sweep filters correctly across the
combinations checked; the recovery scanner's status floor follows the config; the
validator catches every malformed-config category we care about.

## Recommendations

1. **CI-gated integration test for end-to-end retention against MinIO.** Mirror of the
   recommendation from bolt 051's test report. Worth its own small follow-up bolt rather
   than padding 052.
2. **Production smoke verify** — once deployed, manually flip a paid test-order to Shipped
   in admin and confirm the cloud-original delete + `OriginalPurgedAt` write. The unit
   tests give high confidence in the algorithm; the integration check against real S3 /
   R2 closes the residual configuration-gap concern.
3. **Run the retention job with `RetentionMonths = 1`** in staging first when this is
   deployed for the first time. The default 12-month window means production won't
   exercise the job's "actually delete something" path until well into 2027 otherwise.

## Completion Criteria — Stage 5

- [x] All unit tests passing (583/590; 7 skipped are CI-gated MinIO, 0 failed).
- [x] All integration tests passing — bolt 052 added no integration tests (justified above); existing 540 + bolt-052's 50 unit tests all green.
- [x] Security tests — no new security surface (no new HTTP endpoints; admin-role required for the purge hook is unchanged); inherited from bolt 043.
- [x] Performance — purger is `O(uploads)` × DeleteObject; retention job is `O(BatchSize)` per tick; both bounded. No microbenchmark needed.
- [x] Coverage — every new public method exercised by at least one dedicated test; the per-status `Theory` ensures the recovery scanner's status-floor logic is exhaustively checked.
- [x] All acceptance criteria validated against the two stories.
