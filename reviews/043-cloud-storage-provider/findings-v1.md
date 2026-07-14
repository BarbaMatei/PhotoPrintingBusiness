---
type: review-findings
target: 043-cloud-storage-provider
version: 1
commit: 5706580
pass-type: discovery
date: 2026-07-14
---

# Findings detail — 043-cloud-storage-provider v1

Full per-finding record (scenario · evidence · suggested fix · verdict) for every finding in
[review-v1.md](review-v1.md), including the Lows the review file lists only as one-liners. Each finding
was adversarially verified by the discovery workflow's convergence-weighted skeptics; the main agent
re-confirmed F1/F2 against the source directly. `Conv` = independent lenses that raised it.

---

## F1 · D1 — 🔴 High — Admin ZIP fulfilment download reads promoted originals from the local tier only  ·  **BLOCKER**
- **File:** `src/PhotoPrint.API/Services/AdminOrderService.cs:168` (`StreamZipAsync`)
- **Conv:** 2 (correctness, completeness-critic) · **Verdict:** confirmed (reviewer re-verified)
- **Scenario:** Cloud tier on (`Storage:Provider=S3`). Order paid → `OrderPhotoPromoter` uploads each
  original to cloud, sets `StorageLocation=Cloud`, best-effort-deletes the local copy. Admin hits
  `GET /api/admin/orders/{id}/download-zip` during Printing to get the photos to print. `StreamZipAsync`
  streams the ZIP header first (`ContentType`/`Content-Disposition` set at lines 151-154), then for each
  item calls the **local-only** `_storage.GetStreamAsync(item.Upload.FilePath)` → `FileNotFoundException`
  mid-stream → aborted/corrupt download. Admin cannot fulfil the order.
- **Evidence:** `StorageExtensions.cs:75-76` binds the default `IStorageService` to the `"local"` keyed
  adapter; `AdminOrderService` injects that default. `StreamZipAsync` never branches on `StorageLocation`.
  The line-161 `FilePath is null` skip does **not** fire: promotion keeps `FilePath` non-null (same key,
  Cloud tier — `OrderPhotoPromoter.cs:192-193`) and only Shipped-time purge nulls it. So a promoted-but-
  not-yet-purged upload reaches line 168 and `LocalStorageService.GetStreamAsync` (line 73-74) throws.
  `StorageExtensions.cs:72-74` explicitly comments that some callers still inject `IStorageService`
  directly — this is one.
- **Why High:** breaks the admin's ability to fulfil paid orders — the core operational promise — for
  **every** promoted order once the cloud tier is enabled (the point of shipping bolt-043/051). It does
  *not* affect the current local-only deployment (cloud off → uploads stay Local → works), which is why
  the green suite misses it. This is a "breaks when the feature is turned on" deploy blocker.
- **Fix:** inject `IStorageRouter`; read via `_router.For(item.Upload.StorageLocation).GetStreamAsync(FilePath)`.
  Regression test: promote an order's uploads to a cloud fake, delete the local bytes, assert the ZIP
  streams all entries.

## F2 · D2 — 🟠 Medium — UploadCleanupJob deletes Cloud uploads against the local tier; never deletes LargePreviewPath
- **File:** `src/PhotoPrint.API/BackgroundJobs/UploadCleanupJob.cs:67` (`CleanupAsync`)
- **Conv:** 1 (correctness) · **Verdict:** confirmed (reviewer re-verified)
- **Scenario:** A promoted (`StorageLocation=Cloud`) upload older than `ReferencedRetentionDays` (365d) is
  selected by the `u.UploadedAt < referencedCutoff` branch. The job resolves the **default (local)**
  `IStorageService`, so `DeleteAsync(FilePath)`/`DeleteAsync(ThumbnailPath)` no-op on disk and
  `LargePreviewPath` is never referenced at all. Row is soft-deleted. The three cloud objects
  (`uploads/…`, `thumbs/…`, `previews/…`) are orphaned with no row to ever reclaim them → cloud storage
  cost leak.
- **Evidence:** `UploadCleanupJob.cs:67` = `GetRequiredService<IStorageService>()` (the local default,
  `StorageExtensions.cs:75`), not `IStorageRouter`. `LocalStorageService.DeleteAsync` (line 61) no-ops
  when `File.Exists` is false. The candidate query (lines 73-82) selects Cloud rows unconditionally.
  `LargePreviewPath` (`Upload.cs`) is never touched by the job.
- **Fix:** route deletes via `IStorageRouter.For(upload.StorageLocation)`; also delete
  `upload.LargePreviewPath` when non-null. Regression test: aged Cloud upload → cloud fake sees all three
  keys deleted. *(Same root class as F1 — fix them together.)*

## F3 · D3 — 🟠 Medium — Cloud missing-original throws AmazonS3Exception, not FileNotFoundException → preview 500 not 404
- **File:** `src/PhotoPrint.API/Services/S3StorageService.cs:91` · `src/PhotoPrint.API/Services/UploadService.cs:182`
- **Conv:** 1 (tests-coverage) · **Verdict:** confirmed
- **Scenario:** Prod S3. An upload row has `FilePath` set but the blob is gone (e.g. the purge crash
  window: cloud delete succeeded, row update failed, `FilePath` still set). `GetPreviewAsync` calls
  `GetStreamAsync(FilePath)` → `_s3.GetObjectAsync` on a missing key throws `AmazonS3Exception(NotFound)`.
  `IsTransient` is false (not 5xx), so no retry; the type is **not** `FileNotFoundException`, so the
  `catch (FileNotFoundException)` at `UploadService.cs:182` misses it → escapes as **500**, not the clean
  404 the code intends.
- **Evidence:** `S3StorageService.GetStreamAsync` (91-98) does no exception translation. Note the
  asymmetry: `ExistsAsync` (line 111) *does* catch `AmazonS3Exception NotFound`, but `GetStreamAsync`
  doesn't. The cloud fake (`UploadFactory.cs:229`) throws `FileNotFoundException`, mimicking the *local*
  contract — so the test is green and the gap is invisible. A provider-symmetry defect: the two adapters
  don't share a missing-object exception contract.
- **Fix:** either translate in `S3StorageService.GetStreamAsync` (catch `AmazonS3Exception NotFound` →
  throw `FileNotFoundException`) or widen `GetPreviewAsync`'s catch. Then fix the fake to throw
  `AmazonS3Exception(NotFound)` and add a test asserting 404.

## F4 · D4 — 🟠 Medium — Purge on Shipped is one-shot; skips in-flight promotion → cloud original never purged until reboot
- **File:** `src/PhotoPrint.API/Services/AdminOrderService.cs:136` · `src/PhotoPrint.API/Services/OriginalPurger.cs:89`
- **Conv:** 1 (race) · **Verdict:** confirmed
- **Scenario:** Order paid → promotion enqueued but still in-flight/backed-off (upload
  `StorageLocation=Local`). Admin marks Shipped → `PurgeOrderOriginalsAsync` loads the upload, sees
  not-Cloud, skips it (`OriginalPurger.cs:89`, defence-in-depth). Promotion later completes, flips to
  Cloud, writes the original. Nothing re-fires purge — the recovery scanner is `IHostedService.StartAsync`
  (boot-only) and `ArchiveRetentionJob` deletes only preview/thumb keys, never the original `FilePath`.
  The archived original lingers past its retention/GDPR window indefinitely on an always-on server.
- **Evidence:** `OrderStatusMachine` gates the status graph only, never `StorageLocation`, so
  Printing→Shipped works while promotion is Local. `OriginalPurgeRecoveryScanner.cs:37` is boot-only.
  `ArchiveRetentionJob` nulls only `LargePreviewPath`/`ThumbnailPath`. `OrderPhotoPromoter` never re-fires
  purge on completion.
- **Fix:** re-check/re-fire purge at promotion completion when the order is already at a production-
  complete status, or have the retention sweep purge originals for shipped orders. Regression test:
  ship-while-Local, then complete promotion → assert original purged.

## F5 · D5 — 🟠 Medium — Presigned-URL TTL vs hardcoded Cache-Control max-age divergence → expired/broken images
- **File:** `src/PhotoPrint.API/Controllers/UploadsController.cs:185`
- **Conv:** 1 (completeness-critic) · **Verdict:** confirmed
- **Scenario (two parts):** (a) The preview 302 hardcodes `Cache-Control: max-age=3600`, but the
  presigned URL TTL is `PresignTtlMinutes` (operator-tunable, validated only `>0`). Set it `<60` → the
  browser replays its still-fresh cached redirect to an already-expired URL → 403/broken thumbnail.
  (b) `GetOrderPhotosAsync` mints the large-preview URL at **page load**; a user who opens the lightbox
  after the TTL elapses gets an expired link.
- **Evidence:** `StorageSettings.cs:74-75` validates `PresignTtlMinutes > 0` only — nothing binds it to
  the `max-age=3600` at `UploadsController.cs:185`. The appsettings default (60) is coincidental, not
  enforced. `OrderService.cs:493` mints the lightbox URL at load.
- **Fix:** derive `max-age` from `PresignTtlMinutes`; for the lightbox, fetch the large URL on open (or
  use a longer dedicated TTL) rather than at page load. *(This one wants the frontend-ux lens that the
  lean pass skipped.)*

## F6 · D6 — 🟠 Medium — Promotion worker disposes concurrency semaphore under in-flight tasks on shutdown
- **File:** `src/PhotoPrint.API/BackgroundJobs/OrderPhotoPromotionWorker.cs:108`
- **Conv:** 2 (correctness, race) · **Verdict:** confirmed
- **Scenario:** `MaxConcurrent=4`, several slow `ProcessAsync` tasks mid-`PromoteOrderAsync` (S3 upload).
  Shutdown cancels `stoppingToken` → `ReadAllAsync` throws OCE (line 48), caught at line 58, `ExecuteAsync`
  returns → `using var concurrency` disposes the semaphore (line 44). The in-flight **detached** tasks
  (line 55, `_ =`) then reach their `finally` → `concurrency.Release()` (line 108) on a disposed semaphore
  → `ObjectDisposedException`, unobserved (outside the inner catches at 92/96), and the promotion is
  abandoned mid-write — despite the line-60 "drain in-flight slots" comment, which nothing implements.
- **Evidence:** no `WhenAll`/join of the fire-and-forget tasks before disposal. The line-58 catch is a
  bare comment.
- **Fix:** track the in-flight tasks and `await Task.WhenAll(inFlight)` after the loop before the
  semaphore is disposed (or guard `Release()` against `ObjectDisposedException`). Regression test:
  shutdown with an in-flight job → no unobserved exception, job completes or is cleanly re-queued.

## F7 · D7 — 🟠 Medium *(plausible, hinted)* — Migration DDL (FilePath NOT-NULL drop) unverified by tests/CI
- **File:** `src/PhotoPrint.API/Migrations/20260529123952_MakeUploadFilePathNullable.cs`
- **Conv:** 2 (tests-coverage, completeness-critic) · **Verdict:** plausible (hinted — the dual-DB gap was
  planted by the shared prompt context, so the cross-lens agreement isn't independent)
- **Scenario:** `OriginalPurger` sets `FilePath=null` then `SaveChanges`. Every purger test uses the
  InMemory provider (null allowed regardless of DDL); `UploadThumbnailPathMigrationTests` runs a real
  SQLite `Migrate()` but asserts only `ThumbnailPath`. If the Postgres NOT-NULL drop ever regresses,
  `SaveChanges` throws, the purger catches it and counts `Failed`, and originals silently never purge —
  the suite stays green. **Not a live defect** (the migration is correct today); a real test-coverage gap.
- **Fix:** extend the SQLite migration test to assert `Uploads.FilePath notNull=false` after `Migrate()`;
  defer the Npgsql arm to Testcontainers per the existing DB-1 note (bolt-035/042). *This is the
  db-parity lens surfacing through tests-coverage — the lean pass skipped db-parity.*

## F8 · D8 — 🟡 Low — Preview GET TOCTOU: promotion deletes local thumb between service read and stream-open → 500
- **File:** `src/PhotoPrint.API/Controllers/UploadsController.cs:190`
- **Conv:** 1 (race) · **Verdict:** confirmed
- **Scenario:** Owner GETs `/uploads/{id}/preview`. `GetPreviewAsync` reads the row pre-promotion (Local),
  returns `Location=Local, thumbKey` without opening a stream. A concurrent promotion commits Cloud and
  best-effort-deletes that local thumb (Step 4). The controller then calls
  `_storageRouter.Local.GetStreamAsync(thumbKey)` at line 190 → `FileNotFoundException` (uncaught, not in
  the exception middleware map) → **500** instead of the clean 404/302 the service path would give.
- **Fix:** wrap the controller's local `GetStreamAsync` in a `FileNotFound` catch that re-resolves via
  `GetPreviewAsync` (now Cloud → 302) or returns 404. Regression test: promote between service return and
  stream open. Narrow window; low.

## F9 · D9 — 🟡 Low — Concurrent duplicate payment webhooks race Order.Status (no concurrency token)
- **File:** `src/PhotoPrint.API/Controllers/WebhooksController.cs:218`
- **Conv:** 1 (race) · **Verdict:** confirmed
- **Scenario:** Stripe delivers `payment_intent.succeeded` twice with overlapping timing. `Order` has no
  `RowVersion`; two scoped contexts both read `Status=AwaitingPayment`, both transition to Paid, both
  `SaveChanges`, both send the confirmation email and both `EnqueueAsync`. Result: duplicate order-confirmed
  email + two concurrent promotions. Promotion is idempotent (deterministic keys) so no data loss.
- **Fix:** add a concurrency token on `Order` (or a paid-transition unique guard) so the losing duplicate
  no-ops, and/or dedup the promotion queue by `orderId`. **Check first whether bolt-035 (payment
  idempotency) already addresses this** — it overlaps that feature's remit; may be a re-raise of accepted
  scope rather than new work.

## F10 · D10 — 🟡 Low — 403-vs-404 order-existence oracle on /photos and /detail
- **File:** `src/PhotoPrint.API/Services/OrderService.cs:468` (and identically 407-411)
- **Conv:** 1 (security) · **Verdict:** confirmed
- **Scenario:** `GetOrderPhotosAsync`/`GetOrderDetailAsync` throw 404 for a nonexistent order but 403 for
  one owned by another user. An authenticated attacker probing order GUIDs can distinguish real IDs from
  fake ones. Impact negligible — IDs are unguessable GUID v4 — but the enumeration signal is real.
- **Fix:** return 404 (not 403) when the order exists but the caller isn't the owner, so existence isn't
  disclosed. *(Reviewer note: this is a deliberate consistency call — the codebase may prefer 403 for
  clarity elsewhere; confirm the convention.)*

## F11 · D11 — 🟡 Low *(plausible)* — /photos returns presigned URLs without Cache-Control: private
- **File:** `src/PhotoPrint.API/Controllers/OrdersController.cs:82`
- **Conv:** 1 (security) · **Verdict:** plausible
- **Scenario:** `GET /orders/{id}/photos` returns JSON with 60-min presigned URLs and sets no
  `Cache-Control`. A downstream CDN/reverse proxy that caches by URL (or a future cookie-auth switch)
  could store and replay another user's signed preview URLs within the TTL. The sibling preview endpoint
  deliberately sets `private` (SEC-1); this one omits it.
- **Why only plausible:** the endpoint is `[Authorize]` Bearer, and RFC 7234 bars compliant shared caches
  from replaying Authorization-bearing responses — so the leak needs an out-of-repo misconfigured/cookie
  cache. The header omission and the inconsistency with the sibling endpoint are real, though.
- **Fix:** set `Cache-Control: private, no-store` on the photos action, matching the preview endpoint.

## F12 · D12 — 🟡 Low *(decision)* — Guest-placed orders unreachable from the new /photos endpoint
- **File:** `src/PhotoPrint.API/Controllers/OrdersController.cs:10`
- **Conv:** 1 (completeness-critic) · **Verdict:** confirmed (behaviour) — **needs scope decision**
- **Scenario:** A guest (UserId null, GuestSessionId set) pays, later wants their photos. `OrdersController`
  is `[Authorize]` (user-only): `GetUserIdOrNull()` is null → 401; and `GetOrderPhotosAsync` gates on
  `order.UserId==userId`, which never matches a guest order. Guests can never view archived photos —
  unlike `UploadsController`/`PaymentsController`, which have a `guestSessionId` branch.
- **Fix (if in scope):** switch to the DualAuth policy and add a `guestSessionId` ownership branch
  mirroring `UploadsController.GetPreviewAsync`. **First confirm guest order history is in scope for
  bolt-053** — it may be deliberately user-only. The requirements/frontend-ux lenses that would settle
  this were not run.

## F13 · D13 — 🟡 Low — FE empty-state copy "no longer available" misfires for not-yet-promoted orders
- **File:** `src/PhotoPrint.UI/src/app/features/orders/pages/order-detail-page.ts:103`
- **Conv:** 1 (completeness-critic) · **Verdict:** confirmed
- **Scenario:** Promotion is async. A customer opening the order right after payment (before the worker
  finishes) gets `photos()=[]` and sees "Fotografiile pentru această comandă nu mai sunt disponibile" —
  implying deletion when the photos are simply not archived yet. Same copy also shows when the cloud tier
  is off or after retention purge — three different states collapsed into one "no longer available".
- **Fix:** distinguish pending-promotion / cloud-off / post-retention in the API response or UI, and show
  "being prepared" vs "no longer available" accordingly. *(frontend-ux lens territory — not run.)*

## F14 · D14 — 🟡 Low — Cloud preview regen branch never exercised (fake presets ThumbnailPath)
- **File:** `src/PhotoPrint.Tests/Integration/CloudPreviewIntegrationTests.cs:225`
- **Conv:** 1 (tests-coverage) · **Verdict:** confirmed
- **Scenario:** Every cloud test calls `SeedCloudUploadAsync`, which stores the thumb **and** sets
  `ThumbnailPath`, so `GetPreviewAsync` returns at the `ExistsAsync` cache-hit early return. The cloud
  regenerate path (`GetStreamAsync` original → `GenerateThumbnail` → `SaveAsync` → persist) never runs
  against the cloud tier — break the persist line and all 4 tests still pass.
- **Fix:** add a test seeding a Cloud upload with `ThumbnailPath=null` and only the original stored,
  asserting a thumb is generated, saved to cloud, and persisted.

## F15 · D15 — 🟡 Low — GET /api/orders/{id}/photos has no integration test (auth pipeline untested)
- **File:** `src/PhotoPrint.API/Controllers/OrdersController.cs:73`
- **Conv:** 1 (tests-coverage) · **Verdict:** confirmed
- **Scenario:** `GetOrderPhotosAsync` ownership is unit-tested, but the endpoint's HTTP wiring (401 no-auth,
  403 cross-user, guest-no-userId) has no integration coverage. `OrdersControllerIntegrationTests` covers
  `/orders` and `/orders/{id}` but nothing hits `/photos`. Dropping `[Authorize]` or the null-userId guard
  would redden nothing.
- **Fix:** add integration tests for `/photos`: no-auth→401, other-user→403, owner→200 with presigned URLs,
  mirroring `CloudPreviewIntegrationTests`. *(Closing F15 also covers the regression test F1's fix wants
  at the HTTP layer.)*

## F16 · D16 — 🟡 Low — Promoter row-update-failure and preview-generation-failure branches untested
- **File:** `src/PhotoPrint.API/Services/OrderPhotoPromoter.cs:198`
- **Conv:** 1 (tests-coverage) · **Verdict:** confirmed
- **Scenario:** The Step-3 `SaveChanges` catch (line 198 → Failed) never runs — InMemory `SaveChanges`
  doesn't throw. `GenerateLargePreviewAsync` is always mocked, so the corrupt-image throw → cloud-write-
  error → retry-to-terminal path is unverified. A mis-count regression (Failed counted as Promoted, or the
  row left flipped after a failed write) ships green.
- **Fix:** add a test with a throwing `DbContext` (or SQLite + forced failure) asserting `Failed` and the
  row stays Local; add a test where `GenerateLargePreviewAsync` throws.

## F17 · D17 — 🟡 Low *(decision)* — No test covers the original never being purged for a paid-then-cancelled order
- **File:** `src/PhotoPrint.API/BackgroundJobs/OriginalPurgeRecoveryScanner.cs:54`
- **Conv:** 1 (tests-coverage) · **Verdict:** confirmed — **needs behaviour decision**
- **Scenario:** Purge fires only at Shipped/Delivered (`ProductionCompleteFloor` excludes
  Cancelled/PaymentFailed). A paid order later cancelled/refunded never reaches those, so its cloud
  original (`FilePath`) is never purged; retention only nulls preview/thumb. The original blob may leak in
  cloud indefinitely — **or retaining cancelled-order originals is intended** (refund/dispute evidence).
- **Fix:** decide the intended behaviour, then add a test asserting the original is (or is deliberately
  not) purged/retained; document it.

## F18 · D18 — 🟡 Low — BackfillCommand and S3BucketVerifier have zero tests
- **File:** `src/PhotoPrint.API/Cli/BackfillCommand.cs:42`
- **Conv:** 1 (tests-coverage) · **Verdict:** confirmed
- **Scenario:** `BackfillCommand`'s order-selection filter is a hand-copy of the tested
  `PromotionRecoveryScanner` and its exit codes (0/1/2) drive ops automation, but nothing tests them —
  filter drift silently backfills the wrong set. `S3BucketVerifier`'s boot-abort-on-missing-bucket is
  likewise unverified, so a swallowed exception would let a misconfigured host start.
- **Fix:** unit-test `BackfillCommand` selection/exit codes (dry-run vs live, cloud-off→2) and
  `S3BucketVerifier.StartAsync` throwing when the bucket is absent (mocked `IAmazonS3`).

---

## Refuted — recorded so it isn't re-raised

### R1 — S3StorageService coverage hinges on the MinIO gate (regressions invisible)
- **File:** `src/PhotoPrint.Tests/Integration/S3StorageServiceIntegrationTests.cs:47`
- **Was:** 🟡 Low (tests-coverage, completeness-critic; conv 2, hinted) · **Verdict:** **refuted**
- **Why:** `.github/workflows/ci.yml` runs on every PR + non-main push (L5-9), starts MinIO (L44-51),
  health-checks it (L54-65), and sets `STORAGE_TEST_ENDPOINT/ACCESS_KEY/SECRET_KEY/BUCKET` on the Test
  step (L78-81). So `_fx.Available` is true in CI and every `[SkippableFact]` S3 test (save, presign,
  exists, round-trip) executes — a regression in the presign scheme, retry set, or 404 handling fails CI.
  The local skip is by-design with a CI backstop. **Kept caveat:** `IsTransient` classification and
  multipart upload are unproven *even with MinIO up* (no fault injection, tiny payloads) — that residual
  is real but it is a different gap, tracked under F3/F16, not this finding.
