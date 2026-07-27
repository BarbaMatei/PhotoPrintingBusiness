---
type: review
target: 043-cloud-storage-provider
version: 1
supersedes: null
commit: 5706580
branch: feat/bolt-043-cloud-storage-provider
pass-type: discovery
date: 2026-07-14
reviewer: multi-lens (lean discovery)
lenses: [correctness, security, race, tests-coverage, completeness-critic]
lenses-not-run: [db-parity, observability, input-validation, requirements, frontend-ux]
verdict: request-changes
blockers: [F1]
findings: { high: 1, medium: 6, low: 11, cleanup: 0, refuted: 1 }
tests: { dotnet: "661/661 (+7 skipped MinIO)", frontend: "423/423" }
---

# Review v1 — 043-cloud-storage-provider (lean discovery pass)

**Scope.** The whole 4-bolt stack on this branch (main…HEAD, commit `5706580`): **043** cloud
storage (two-tier `StorageRouter` + `S3StorageService`/`LocalStorageService` behind `IStorageService`),
**051** promote-on-paid (`OrderPhotoPromoter`, in-process `PromotionQueue`/worker + recovery scanner),
**052** archive retention + original purge (`OriginalPurger`, `ArchiveRetentionJob`), **053** the new
`GET /api/orders/{id}/photos` endpoint + order-detail photo grid/lightbox.

**Pass type.** First review of this branch → **discovery** (blinded, whole-feature). Five lenses ran
in one blinded parallel batch → in-pass dedup → convergence-weighted adversarial verify
([discovery-review.wf.js](../lib/discovery-review.wf.js)); the main agent synthesized.

**This was a LEAN pass** (owner's choice). Only correctness, security, race, tests-coverage, and the
completeness-critic ran. **db-parity, observability, input-validation, requirements, and frontend-ux
were deliberately not run** — so their surface is under-reviewed. The completeness critic and several
tests-coverage findings point straight at those gaps (F5, F7, F12, F13). **This pass therefore cannot
certify saturation** — a full-manifest discovery pass is still owed before feature closure.

**Verdict: `request-changes`.** One confirmed **High** blocker (**F1**) breaks admin order fulfilment
once the cloud tier is enabled. The stack's core promote/purge design is otherwise sound by
construction (deterministic keys make double-promotion idempotent; complementary `StorageLocation`
guards + per-upload EF updates keep promotion and purge from clobbering each other). **Security is
strong** — the new `/photos` endpoint is correctly ownership-scoped (no IDOR), presigned URLs cover
only thumbnail/preview keys (never the original), and no S3 credentials or signed URLs leak into logs
or responses.

**The theme.** The single most important pattern: the two-tier storage rework left **collaborators
still bound to the local-only default `IStorageService`** and never routed by `Upload.StorageLocation`.
`StorageExtensions.cs:72-74` even comments that this interim wiring exists — F1 (admin ZIP) and F2
(cleanup job) are the two consumers where it actually bites once `Storage:Provider=S3`.

> Full per-finding detail (scenario · evidence · fix) is in [findings-v1.md](findings-v1.md).
> Canonical IDs for cross-pass tracking are in [ledger.md](ledger.md) (v1: F# ↔ D# 1:1).

## Build & tests (run by the reviewer at this commit)

- **.NET:** `661/661` passed, **7 skipped** — the MinIO `[SkippableFact]` S3 integration tests skip
  locally (no Docker). *They do run in CI* (see F-refuted below).
- **Frontend (Vitest):** `423/423` passed.
- Green suite, but see the tests-coverage findings: several "cloud" behaviours pass **because a fake
  mimics the local exception contract rather than S3's** (F3), and the migration DDL, the S3 adapter
  logic, and the `/photos` auth pipeline are effectively unproven by the local suite.

## Findings

Ranked most-severe first. Convergence = independent lenses that raised it (max 2 this pass).
Verdict from the adversarial skeptics: **confirmed** (trace built, no guard) · **plausible** (realistic,
not proven live) · **refuted** (dropped, recorded).

| ID | D# | Sev | Conv | Verdict | Finding | File |
|----|----|-----|------|---------|---------|------|
| **F1** | D1 | 🔴 High | 2 | confirmed | **Admin ZIP fulfilment download reads promoted originals from the local tier only → `FileNotFoundException` mid-ZIP once cloud promotion runs. Admin cannot fetch photos to print.** *(BLOCKER)* | `Services/AdminOrderService.cs:168` |
| F2 | D2 | 🟠 Med | 1 | confirmed | `UploadCleanupJob` resolves the local default for Cloud uploads: `DeleteAsync` no-ops on disk and `LargePreviewPath` is never deleted → cloud blobs orphaned with no row to reclaim them (cost leak). Same root class as F1. | `BackgroundJobs/UploadCleanupJob.cs:67` |
| F3 | D3 | 🟠 Med | 1 | confirmed | Cloud `GetStreamAsync` throws `AmazonS3Exception(NotFound)`, but `UploadService.GetPreviewAsync` catches only `FileNotFoundException` → a missing cloud original returns **500 not 404** in prod. The cloud fake throws the *local* exception type, so no test reddens. | `Services/S3StorageService.cs:91` · `Services/UploadService.cs:182` |
| F4 | D4 | 🟠 Med | 1 | confirmed | Purge fires **once** on the Shipped transition and skips any upload whose promotion hasn't finished (`StorageLocation=Local`). A later-completing promotion is then never re-purged (recovery scanner is boot-only) → cloud original lingers past its retention/GDPR window until reboot. | `Services/AdminOrderService.cs:136` · `OriginalPurger.cs:89` |
| F5 | D5 | 🟠 Med | 1 | confirmed | Presigned-URL TTL (`PresignTtlMinutes`, operator-tunable, validated only `>0`) diverges from the hardcoded preview `Cache-Control: max-age=3600`; and the lightbox large-URL is minted at page load. Set TTL `<60` (or open the lightbox late) → browser replays a cached redirect to an expired URL → broken images. | `Controllers/UploadsController.cs:185` |
| F6 | D6 | 🟠 Med | 2 | confirmed | Promotion worker disposes its `SemaphoreSlim` (`using var`) on shutdown while fire-and-forget `ProcessAsync` tasks are still in flight → their `concurrency.Release()` throws `ObjectDisposedException` (unobserved) and the in-flight promotion is abandoned mid-write, despite the "drain in-flight slots" comment. | `BackgroundJobs/OrderPhotoPromotionWorker.cs:108` |
| F7 | D7 | 🟠 Med | 2 | plausible *(hinted)* | Migration DDL (`FilePath` NOT-NULL drop) is unverified: purger tests use the InMemory provider (null always allowed) and the SQLite migration test asserts only `ThumbnailPath`. A Postgres DDL regression would be caught by the purger and silently counted `Failed` — suite stays green. *Test-coverage gap, not a live defect.* | `Migrations/…MakeUploadFilePathNullable.cs` |
| F8 | D8 | 🟡 Low | 1 | confirmed | Preview GET TOCTOU: `GetPreviewAsync` returns `Location=Local`, then a concurrent promotion deletes the local thumb before the controller opens it → uncaught `FileNotFoundException` → **500** instead of the clean 404/302. Narrow window. | `Controllers/UploadsController.cs:190` |
| F9 | D9 | 🟡 Low | 1 | confirmed | Concurrent duplicate `payment_intent.succeeded` webhooks race `Order.Status` (no `RowVersion`) → double confirmation email + double promotion enqueue. Promotion is idempotent so no data loss. *Overlaps bolt-035 payment-idempotency territory — check whether that work already covers it.* | `Controllers/WebhooksController.cs:218` |
| F10 | D10 | 🟡 Low | 1 | confirmed | 403-vs-404 order-existence oracle: `/photos` & `/detail` return 404 for a nonexistent order but 403 for another user's → an authenticated attacker can distinguish real order GUIDs. Impact negligible (GUID v4 unguessable). | `Services/OrderService.cs:468` |
| F11 | D11 | 🟡 Low | 1 | plausible | `/photos` returns per-user presigned URLs with **no `Cache-Control: private`**; the sibling preview endpoint deliberately sets it (SEC-1). Needs an out-of-repo caching proxy or a future cookie-auth switch to actually leak — speculative, but the inconsistency is real. | `Controllers/OrdersController.cs:82` |
| F12 | D12 | 🟡 Low | 1 | confirmed *(hinted)* | Guest-placed orders (UserId null, GuestSessionId set) are unreachable from the new `/photos` endpoint — it is `[Authorize]` user-only with a `UserId==userId` gate and no guest branch, unlike Uploads/Payments. **Confirm whether guest order history is in scope.** | `Controllers/OrdersController.cs:10` |
| F13 | D13 | 🟡 Low | 1 | confirmed | FE empty-state copy "Fotografiile … nu mai sunt disponibile" (no longer available) also shows for a freshly-paid order whose async promotion hasn't finished, and when the cloud tier is off — implies deletion when photos are merely *not archived yet*. | `PhotoPrint.UI/…/order-detail-page.ts:103` |
| F14 | D14 | 🟡 Low | 1 | confirmed | Cloud preview **regen** branch never exercised: every cloud test seeds `ThumbnailPath` + stores the thumb, so `GetPreviewAsync` returns at the cache-hit early-return; the cloud regenerate→save→persist path ships untested (break the persist and all 4 tests still pass). | `Tests/Integration/CloudPreviewIntegrationTests.cs:225` |
| F15 | D15 | 🟡 Low | 1 | confirmed | `GET /api/orders/{id}/photos` has **no integration test** — the HTTP auth pipeline (401 no-auth, 403 cross-user, guest behaviour) is unproven; dropping `[Authorize]` or the null-userId guard would redden nothing. The sibling preview route has this coverage. | `Controllers/OrdersController.cs:73` |
| F16 | D16 | 🟡 Low | 1 | confirmed | Promoter row-update-failure (Step-3 `SaveChanges` catch) and preview-generation-failure branches are untested — InMemory `SaveChanges` never throws and `GenerateLargePreviewAsync` is always mocked. A mis-count regression (Failed counted as Promoted → row flipped on a failed write) ships green. | `Services/OrderPhotoPromoter.cs:198` |
| F17 | D17 | 🟡 Low | 1 | confirmed | No test covers the original never being purged for a **paid-then-cancelled** order (purge fires only at Shipped/Delivered; retention nulls only preview/thumb). The original blob may leak in cloud indefinitely — or this is intended; decide + document + test. | `BackgroundJobs/OriginalPurgeRecoveryScanner.cs:54` |
| F18 | D18 | 🟡 Low | 1 | confirmed | `BackfillCommand` (order-selection filter is a hand-copy of the tested `PromotionRecoveryScanner`; exit codes drive ops automation) and `S3BucketVerifier` (boot-abort on missing bucket) have **zero tests**. Filter drift or a swallowed boot exception ships undetected. | `Cli/BackfillCommand.cs:42` |

### Refuted (dropped, recorded so it isn't re-raised)

| Was | Sev | Claim | Why refuted |
|-----|-----|-------|-------------|
| tests / completeness | 🟡 Low | "S3StorageService coverage hinges on the MinIO gate; regressions invisible" | **`.github/workflows/ci.yml` runs on every PR + non-main push, starts MinIO, health-checks it, and sets `STORAGE_TEST_ENDPOINT/ACCESS_KEY/SECRET_KEY/BUCKET` on the Test step** → `_fx.Available` is true and every `[SkippableFact]` S3 test (save, presign, exists, round-trip) executes in CI. Local skip is by-design with a CI backstop. *(Caveat the skeptic added and F14/F16 keep: `IsTransient` classification and multipart upload are unproven **even with MinIO up** — no fault injection, tiny payloads. That's a real gap, but it's a different finding, not this one.)* |

## Recommended order of work

1. **Fix F1 before merge** (the blocker). The clean fix is the same for F1 + F2 + F8: route storage reads/
   deletes through `IStorageRouter.For(upload.StorageLocation)` instead of the local default
   `IStorageService`. Add F3's cloud-exception mapping while you're in `UploadService`. Each needs the
   regression test the finding names (promoted-order ZIP; cloud-aged cleanup; cloud-404 preview).
2. **Then the remaining Mediums** F4–F7 by owner priority. F4 (retention leak) and F7 (migration DDL)
   have a compliance/parity flavour and pair naturally with the **db-parity** lens that this lean pass
   skipped.
3. **Lows** are batchable; F12 and F17 are **decisions, not bugs** — confirm intended behaviour first.
4. **Owed before closing the feature:** a **full-manifest discovery pass** (this lean one skipped 5
   lenses) and — per the two-loops rule — it must be a *saturated* discovery pass, not a verification
   round, that finally stamps the stack done.

## Convergence / saturation note

Max convergence this pass was **2** (F1 and F6 each raised by two lenses). That is low agreement —
consistent with a lean 5-lens pass sampling a large 4-bolt surface. Per the README's recall model, one
lean pass is a *sample*, not a sweep: treat this as a first draw. The refute rate was 1/19 (~5%), in
line with the calibration data. **Do not read the absence of more Highs as cleanliness** — the deliberately
unrun lenses (db-parity, observability, input-validation, requirements, frontend-ux) are exactly where a
second, fuller pass would draw next.
