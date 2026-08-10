---
type: resolution
target: 043-cloud-storage-provider
version: 1
answers: review-v1.md
commit: 5706580
branch: feat/bolt-043-cloud-storage-provider
status: resolved
fixed_commit: 319d7b3
opened: 2026-07-14
closed: 2026-07-14
findings:
  F1: { status: fixed, commit: ec94fca, note: "AdminOrderService now injects IStorageRouter; StreamZipAsync reads via For(upload.StorageLocation). Regression test seeds a Cloud upload, makes the local tier throw, asserts the ZIP streams from cloud (revert-verified red)." }
  F2: { status: fixed, commit: 6b63bd7, note: "UploadCleanupJob routes deletes via For(upload.StorageLocation) and now deletes LargePreviewPath too (via TryDeleteAsync helper). Regression test: aged Cloud upload → cloud tier sees all 3 keys deleted, local tier untouched (revert-verified). Class sweep: F1+F2 were the only two prod callers of the local default; stale StorageExtensions comment corrected in 665ed9a." }
  F3: { status: fixed, commit: 0f85f56, note: "S3StorageService.GetStreamAsync now catches AmazonS3Exception(NotFound) and throws FileNotFoundException (uniform missing-object contract; ExistsAsync already did the same). New surface: exception-translation catch — mocked-IAmazonS3 unit test proves 404→FileNotFound and 403 stays AmazonS3Exception; MinIO SkippableFact proves the real round-trip. Non-vacuous by construction (without the catch the AmazonS3Exception propagates)." }
  F4: { status: fixed, commit: cc69025, note: "OriginalPurgeRecoveryScanner converted from boot-only IHostedService to a periodic BackgroundService (boot sweep + every Archive:PurgeSweepIntervalHours, default 6h). Chosen over Option A (promoter→purger call) which the adversarial check showed would ship a stale-identity-map status read + cross-request tracker contamination. New surface: PurgeSweepIntervalHours setting (validated >0), Take(BatchSize) bound, per-sweep count log. Regression test: stuck Cloud+FilePath order at Shipped/Delivered → sweep fires purger (reflection-tested internal RunSweepAsync)." }
  F5: { status: fixed, commit: d15b9af, note: "Part (a): preview 302 max-age now = (int)PresignTtlMinutes*60 (was hardcoded 3600); controller unit test asserts 1800 for TTL=30. Part (b) (lightbox large-URL minted at page load can expire before open) DEFERRED to the frontend-ux lens — see decisions; it needs a fetch-on-open flow, a frontend feature the lean pass explicitly skipped." }
  F6: { status: fixed, commit: 3d97258, note: "Worker tracks fire-and-forget ProcessAsync tasks in a list (pruned each iteration), drains via Task.WhenAll in a finally before the SemaphoreSlim is disposed. Approach adversarially checked (list outside try, drain in finally, logging catch). New surface: the drain is bounded by the host shutdown timeout — PromoteOrderAsync already honours stoppingToken. Regression test: shutdown with a gated in-flight promotion → StopAsync blocks until drained, promotion completes (revert-verified red)." }
  F7: { status: fixed, commit: 3326607, note: "SQLite migration-chain test now asserts Uploads.FilePath.notNull==false (the MakeUploadFilePathNullable DDL). Npgsql arm stays deferred to Testcontainers/DB-1 as the finding noted." }
  F8: { status: fixed, commit: 881547f, note: "Preview controller catches FileNotFoundException on the local-thumb open and re-resolves once via GetPreviewAsync (Cloud→302 / regenerated), else 404. New surface: bounded single re-resolve — controller unit tests cover the 302 re-resolve and the double-race 404. Extracted CloudRedirectAsync/StreamLocalAsync." }
  F9: { status: deferred, commit: null, note: "Confirmed no Stripe event-dedup and no Order RowVersion exist anywhere. Fixing needs a schema change (Order concurrency token or processed-events table) squarely in bolt-035 payment-idempotency's remit. Impact is a duplicate confirmation email + a second (idempotent, deterministic-key) promotion enqueue — no data loss. Deferred to the payment-idempotency remit; see decisions." }
  F10: { status: wont-fix, commit: null, note: "403-for-non-owner is the established codebase convention (ForbiddenException across AccountService/AdminOrderService/etc., and the sibling GetOrderDetailAsync). Switching only these two endpoints to 404 would be inconsistent; enumeration risk is negligible (unguessable GUID v4). Kept 403; see decisions." }
  F11: { status: fixed, commit: 751894b, note: "GetOrderPhotos sets Cache-Control: private, no-store (matches preview SEC-1). Integration test asserts the header on the owner 200." }
  F12: { status: wont-fix, commit: cda3685, note: "Owner decision: keep /photos user-only — guest order-history photos out of scope for bolt-053. No code change; a guest-token-only request → 401 test pins the intended behaviour. See decisions." }
  F13: { status: deferred, commit: null, note: "The empty-state has four distinct causes (promotion pending / cloud tier off / retention-purged / genuinely no photos) that the client cannot tell apart without an API state signal — a small DTO/contract change best designed under the frontend-ux lens the lean pass skipped (the finding says so). Deferred to that pass; see decisions." }
  F14: { status: fixed, commit: 0ceabf8, note: "New CloudPreview test seeds a Cloud upload with ThumbnailPath=null + only the original in the cloud store, asserts the thumb is regenerated, saved to cloud, and persisted (ThumbnailPath in DB + cloud store has the key). Added SeedCloudUploadWithoutThumbAsync/GetUploadAsync/CloudHasObject helpers." }
  F15: { status: fixed, commit: cda3685, note: "Added /photos integration tests: 401 no-auth, 403 cross-user, 404 unknown, guest-token-only 401. Also covers F1's fix at the HTTP layer for the ownership gate." }
  F16: { status: fixed, commit: a770a13, note: "Two promoter tests: (1) ThrowingSaveDbContext proves a Step-3 SaveChanges failure counts Failed, leaves the row Local, skips local-litter cleanup; (2) GenerateLargePreviewAsync throwing counts Failed + row Local." }
  F17: { status: fixed, commit: 2fcdf3d, note: "Owner decision = purge on cancel. CancelOrderAsync fires the purger after the refund; periodic sweep's OriginalPurgeSweepStatuses adds Cancelled to backstop the promotion-in-flight-at-cancel race. Tests: cancel-fires-purge + Cancelled-stuck-order-swept. Hardened in 957f61a per fresh-eyes: gated on Enabled && CloudEnabled (no false-alarm Error log on a local-only deployment) + try/catch (never fail the committed cancel+refund)." }
  F18: { status: fixed, commit: 682f1e2, note: "BackfillCommand unit tests: cloud-off=2, no-work=0, dry-run-doesn't-promote, live-success=0, any-failure=1. S3BucketVerifier covered by MinIO SkippableFacts (existing bucket boots clean; missing bucket throws to abort boot) — real-protocol probe, not a mock of the static AmazonS3Util (SDK 3.7.406)." }
---

# Resolution v1 — 043-cloud-storage-provider

Fixer response to [review-v1.md](review-v1.md). The review file is immutable; this file records
per-finding status + fix commit + note. Nothing here is `verified` — that flips only on the
verification re-review (`review-v2.md`).

## Owner decisions taken before fixing

- **F12 (guest order photos): keep user-only.** The `/photos` endpoint stays `[Authorize]`
  user-only by intent — guest order-history photos are out of scope for bolt-053. No code change;
  documented as intended behaviour with a regression test asserting the guest path is unreachable.
- **F17 (paid-then-cancelled originals): purge on cancel.** Cancelled/refunded orders' cloud
  originals must be purged to minimise storage/GDPR exposure. Implemented a fast-path purge on
  admin cancel plus a periodic-sweep backstop (shared with the F4 fix).

## Findings

| ID | Sev | Status | Commit | How |
|----|-----|--------|--------|-----|
| F1 | 🔴 High | fixed | ec94fca | Inject `IStorageRouter`; ZIP reads via `For(upload.StorageLocation)`. Cloud-promoted-order regression test (revert-verified). |
| F2 | 🟠 Med | fixed | 6b63bd7 | Route deletes via `For(location)`; delete `LargePreviewPath`. Cloud-tier regression test (revert-verified). |
| F3 | 🟠 Med | fixed | 0f85f56 | Translate S3 `NotFound`→`FileNotFoundException` at the adapter. Mocked-IAmazonS3 unit test + MinIO SkippableFact. |
| F4 | 🟠 Med | fixed | cc69025 | Recovery scanner → periodic `BackgroundService` (boot + every `PurgeSweepIntervalHours`). Stuck-order sweep test. |
| F5 | 🟠 Med | fixed | d15b9af | (a) max-age derived from presign TTL (unit test). (b) lightbox-URL-at-page-load deferred → frontend-ux (decisions). |
| F6 | 🟠 Med | fixed | 3d97258 | Track + `WhenAll`-drain in-flight promotions in a `finally` before disposing the semaphore. Shutdown-drain test (revert-verified). |
| F7 | 🟠 Med | fixed | 3326607 | SQLite migration test asserts `FilePath.notNull==false`. Npgsql arm → Testcontainers/DB-1. |
| F8 | 🟡 Low | fixed | 881547f | Catch local-thumb `FileNotFoundException`, re-resolve once (Cloud→302 / regen), else 404. Controller unit tests. |
| F9 | 🟡 Low | deferred | — | Needs schema change (Order token / event-dedup) in bolt-035's remit; no data loss. See decisions. |
| F10 | 🟡 Low | wont-fix | — | 403-for-non-owner is the codebase convention; negligible GUID-enumeration risk. See decisions. |
| F11 | 🟡 Low | fixed | 751894b | `Cache-Control: private, no-store` on `/photos`. Integration test. |
| F12 | 🟡 Low | wont-fix | cda3685 | Owner: keep user-only. Guest-token-only→401 test pins it. See decisions. |
| F13 | 🟡 Low | deferred | — | Four empty-state causes need an API state signal → frontend-ux pass. See decisions. |
| F14 | 🟡 Low | fixed | 0ceabf8 | Cloud regenerate→save→persist branch now exercised (no-thumb seed). |
| F15 | 🟡 Low | fixed | cda3685 | `/photos` integration tests: 401 / 403 / 404 / guest-401. |
| F16 | 🟡 Low | fixed | a770a13 | Promoter row-update-failure (throwing ctx) + preview-generation-failure tests. |
| F17 | 🟡 Low | fixed | 2fcdf3d | Owner: purge on cancel. Fast-path purge in `CancelOrderAsync` + sweep covers `Cancelled`. Two tests. |
| F18 | 🟡 Low | fixed | 682f1e2 | BackfillCommand exit-code unit tests + S3BucketVerifier MinIO SkippableFacts. |

## Decisions / rationale

**F12 — guest order-history photos: `wont-fix` (owner ruling).** Keep `/photos` `[Authorize]`
user-only; guest order-history photos are out of scope for bolt-053. A guest-token-only request
returns 401 (test `GetOrderPhotos_GuestTokenOnly_Returns401`). If the owner later wants guests to
view archived photos, the change is: switch to the DualAuth policy + a `guestSessionId` branch in
`GetOrderPhotosAsync` mirroring `UploadsController`.

**F17 — paid-then-cancelled originals: purge (owner ruling), implemented.** See F17 above.

**F10 — 403-vs-404 existence oracle: `wont-fix`.** `ForbiddenException` (403) for a non-owner is the
codebase-wide convention (AccountService, AdminOrderService, the sibling `GetOrderDetailAsync`, the
`/uploads` preview). Switching only `GetOrderPhotosAsync`/`GetOrderDetailAsync` to 404 to hide
existence would make these two endpoints inconsistent with everything else for a negligible gain —
order IDs are unguessable GUID v4. Re-reviewer: push back if you'd rather standardise on 404
*everywhere* (that's a separate, codebase-wide change, not a bolt-043 fix).

**F9 — duplicate-webhook Order.Status race: `deferred`.** Verified there is no Stripe event-dedup and
no `Order` concurrency token anywhere in the API. Closing it needs a schema change (an `Order`
`RowVersion` or a processed-events table) — squarely bolt-035 payment-idempotency territory, not
storage (bolt-043). Observable impact today is a duplicate order-confirmed email + a second promotion
enqueue that is idempotent by deterministic key (no data loss). Recommend folding into the
payment-idempotency remit rather than bolting a token on here.

**F13 — misleading empty-state copy: `deferred` → frontend-ux.** The `/photos` empty state has four
distinct causes (promotion still pending, cloud tier off, retention-purged, genuinely no photos) that
the Angular page cannot distinguish without an API state signal. Adding that signal is a small
DTO/contract change best designed under the **frontend-ux** lens, which this lean pass deliberately
skipped (the finding itself flags it as frontend-ux territory). No code change this round.

**F5(b) — lightbox large-URL minted at page load: `deferred` → frontend-ux.** F5(a) (the deterministic
TTL-vs-cache bug) is fixed. Part (b) — a lightbox opened after the presign TTL elapses replays an
expired URL — needs a fetch-the-large-URL-on-open flow (a frontend feature) or a dedicated longer TTL.
That is frontend-ux work the lean pass skipped; deferred to it.

## New surface introduced by fixes

Mechanisms added by these fixes (where the re-review's owning lens should look):

- **F3 — S3 `NotFound`→`FileNotFoundException` translation** (`S3StorageService.GetStreamAsync`).
  Failure mode: a non-404 `AmazonS3Exception` must NOT be translated (else a real error reads as
  "absent"). Covered by the mocked-IAmazonS3 unit tests (404→translated, 403→passes through).
- **F4 — periodic purge sweep** (`OriginalPurgeRecoveryScanner` as `BackgroundService`; new
  `Archive:PurgeSweepIntervalHours`, default 6h). Failure modes: sweep must be idempotent (purger
  self-guards; S3 delete on a missing key is a no-op), bounded (`Take(BatchSize)`), and cloud/archive
  guarded (ExecuteAsync). Observability: `purge.recovery.started/processed/sweep.error` logs.
- **F6 — in-flight promotion drain** (`OrderPhotoPromotionWorker`). Failure mode: `WhenAll` is bounded
  by the host shutdown timeout; `PromoteOrderAsync` already honours `stoppingToken`. `promotion.worker.drain-error` logs a fault.
- **F8 — bounded preview re-resolve** (`UploadsController`). Failure mode: exactly one re-resolve, then
  404 (`uploads.preview.local_thumb_vanished`); no unbounded retry.
- **F17 — purge-on-cancel** (`AdminOrderService.CancelOrderAsync` + `Cancelled` added to the sweep's
  status set). Failure mode: fires after the refund so the money path is never blocked; gated on
  `Enabled && CloudEnabled` and wrapped in try/catch (957f61a) so it neither false-alarms nor fails
  the committed cancel; the sweep backstops a failed purge.

## Fresh-eyes micro-review (before hand-back)

Two anchored Explore agents (fresh context) audited the full fix diff `5706580..HEAD` — one over
the storage/exception/preview cluster, one over the lifecycle/concurrency cluster — each asked the
three questions (class-or-instance · new-surface-at-the-bar · regression). **Both returned "No
blocking issues."** Confirmed: no production caller injects the default `IStorageService` any more
(class cleared); the missing-object exception contract is uniform; no other presign-TTL-tied
`max-age` remains; the `UploadsController` refactor preserves ETag/304/`File(200)` byte-for-byte and
the re-resolve is a bounded two-attempt ladder (no recursion/stream-leak); the drain matches the
approved shape; the sweep is idempotent/bounded/guarded/observable; adding `Cancelled` to the sweep
is safe (a never-promoted cancelled order keeps `Local` uploads the sweep never matches).

**Non-blocking items they surfaced were fixed before hand-back:** purge-on-cancel gated + try/caught
(957f61a); `PurgeSweepIntervalHours` surfaced in `appsettings.json`, the `GetStreamAsync`
missing-key contract documented on `IStorageService`, and the stale pre-F5 `max-age=3600` wording in
the CloudPreview test summary/comment corrected (319d7b3).

## New observation for the re-reviewer (outside the v1 finding set — NOT fixed)

- **`PromotionRecoveryScanner` is still boot-only** (`BackgroundJobs/PromotionRecoveryScanner.cs`,
  `IHostedService.StartAsync`). It is the same *class* as F4 — a promotion that exhausts
  `MaxAttempts` at runtime leaves the upload `Local` and is not re-enqueued until the next reboot.
  Deliberately **not** fixed here: F4 was scoped to the purge side, and converting the promotion
  scanner is a separate mini-feature (its own retry/exhaustion semantics, config, tests). Lower
  severity than F4 (a not-yet-archived upload still serves previews from local and is recovered by
  the `backfill-archive` CLI or the next boot). Flagged so the re-review / next discovery pass can
  decide whether to apply the same periodic-sweep treatment.
