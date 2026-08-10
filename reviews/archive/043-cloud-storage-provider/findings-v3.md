---
type: review-findings
target: 043-cloud-storage-provider
version: 3
commit: 2be8ab8
code-tip: 1e7b9d3
delta-base: 5706580
pass-type: delta-discovery
date: 2026-07-14
---

# Findings detail — 043-cloud-storage-provider v3 (delta-discovery)

Full per-finding record for [review-v3.md](review-v3.md). Backend from the `passType: delta` pass over
`5706580..HEAD` (6 lenses); frontend from the owed full-surface `frontend-ux` pass. `Conv` =
independent lenses; verdicts are the workflow's convergence-weighted skeptics unless noted. `⟳` =
re-find of a prior ledger `D#`.

---

## 🟠 Mediums

### F1 · D19 ⟳ — Promotion recovery left boot-only while its purge sibling was made periodic
- **File:** `src/PhotoPrint.API/BackgroundJobs/PromotionRecoveryScanner.cs` (`IHostedService`; registered `PhotoArchiveExtensions.cs:43`)
- **Conv:** 2 (requirements, completeness-critic) · **Verdict:** confirmed
- **Scenario:** Archive+cloud enabled, always-on prod (no reboot). A paid order's promotion hits
  transient cloud errors on all `MaxAttempts` tries → `OrderPhotoPromotionWorker` logs
  `promotion.failed.terminal`, no re-enqueue; the upload stays `StorageLocation.Local`.
  `PromotionRecoveryScanner` is `IHostedService` (`StartAsync` runs one boot sweep, `StopAsync` no-op)
  with no `PeriodicTimer`, so it never re-scans until the process restarts. The original therefore
  never reaches the durable cloud tier.
- **Evidence:** purge sibling `OriginalPurgeRecoveryScanner.cs:62` uses a `PeriodicTimer` (the F4
  fix); promotion has none. Registration asymmetry: `PhotoArchiveExtensions.cs:43` (promotion,
  hosted) vs `:59` (purge, periodic). Worker terminal cap at `OrderPhotoPromotionWorker.cs:105,126`.
- **Impact / severity:** Medium. This is the always-on case ADR-008's durability goal targets — but
  the local original is *not* deleted when promotion never succeeds, so previews still serve from
  local and there is **no user-visible loss**; the real cost is the cloud-durability intent silently
  unmet and the local disk not reclaimed until reboot. (v2 carry-forward called this "lower severity
  than F4"; the delta trace sharpens it but the local-copy-serves fact keeps it Medium, not High.)
- **Fix:** make `PromotionRecoveryScanner` a periodic `BackgroundService` (boot sweep + `PeriodicTimer`)
  mirroring `OriginalPurgeRecoveryScanner`. This is the **class-sweep (#1)** F4 should have done.
  Regression test must drive `ExecuteAsync` (see F3 — don't repeat the reflection bypass).

### F2 · D24 — UploadCleanupJob `router.For(Cloud)` throws when cloud disabled, wedging all cleanup
- **File:** `src/PhotoPrint.API/BackgroundJobs/UploadCleanupJob.cs:92`
- **Conv:** 1 (correctness) · **Verdict:** confirmed
- **Scenario:** Cloud was enabled, uploads promoted (`StorageLocation=Cloud`), then `Storage:Provider`
  set back to local (`StorageRouter._cloud` null). After `ReferencedRetentionDays`, a Cloud row
  matches the cutoff and — being old — sits in the `OrderBy(UploadedAt).Take(500)` batch. Line 92
  `router.For(Cloud)` throws `InvalidOperationException` (`StorageRouter.cs:24-33`) **outside**
  `TryDeleteAsync`'s try/catch, before `DeletedAt`/`SaveChanges`. `ExecuteAsync` catches+logs and
  retries the identical deterministic batch every hour → **nothing is ever soft-deleted, cleanup
  stalls indefinitely** (including the local orphans in that batch).
- **Evidence:** candidate query `UploadCleanupJob.cs:73-82` has no `StorageLocation` filter; the tier
  resolve is the first unguarded call. Introduced by the F2 routing fix (previously the local default
  no-op'd).
- **Fix:** guard the resolve — if `upload.StorageLocation==Cloud && !router.CloudEnabled`, log+skip
  that upload (or move `For()` inside the per-upload try) so one unroutable row can't abort the batch.
  Regression test: Cloud row + cloud disabled → job soft-deletes routable rows and skips (not throws)
  the unroutable one.

### F3 · D21 — OriginalPurgeRecoveryScanner sweep untested (ExecuteAsync bypassed by reflection)
- **File:** `src/PhotoPrint.API/BackgroundJobs/OriginalPurgeRecoveryScanner.cs:60` · tests `OriginalPurgeRecoveryScannerTests.cs`
- **Conv:** 2 (tests-coverage, completeness-critic) · **Verdict:** confirmed
- **Scenario:** F4 converted the scanner to `BackgroundService`; the tests were rewritten to call
  `RunSweepAsync` **directly via reflection** (`OriginalPurgeRecoveryScannerTests.cs:53-58`), and the
  two `ExecuteAsync` tests (114-142) set `enabled=false`/`CloudEnabled=false` so they return at the
  guards (46/51) before the boot sweep (line 60) or the `PeriodicTimer` loop (62-64). **No test enters
  the happy path.** Delete line 60 or gut the loop and the whole suite stays green; `SafeSweepAsync`'s
  catch branches are also undriven.
- **Impact:** a coverage **regression** the F4 fix introduced — the headline fixed behavior (periodic
  purge) is now unverified, so a future break silently lets cloud originals linger past GDPR window.
- **Fix:** a test that seeds a stuck order, calls `StartAsync` (Enabled+Cloud on), polls until
  `purger.PurgeOrderOriginalsAsync` fires (proving the boot sweep runs), then `StopAsync`.

### F4 · D23 — Production-complete purge lacks the try/catch its cancel sibling got this delta
- **File:** `src/PhotoPrint.API/Services/AdminOrderService.cs:135` (`UpdateStatusAsync`)
- **Conv:** 1 (completeness-critic) · **Verdict:** confirmed
- **Scenario:** Admin PATCHes an order to Shipped. Line 121 commits Shipped, 124 fires the shipped
  email, 128 awaits SignalR. Line 135 `IsProductionCompleteStatus(Shipped)==true` → line 136 awaits
  `PurgeOrderOriginalsAsync` **with no try/catch**. Inside, `OriginalPurger` wraps only the per-upload
  cloud-delete (`:103`) and row-update (`:119`) — the order DB load (`:51`) and
  `ct.ThrowIfCancellationRequested` (`:65`) are uncaught, so a transient DB error or a client
  disconnect propagates out as a **500** to the admin, even though the transition already committed +
  notified.
- **Evidence:** the **cancel** sibling wraps the identical call (`AdminOrderService.cs:235-244`, the
  F17 fix); production-complete does not — a **class-sweep (#1) miss** of F17. A verification reviewer
  praising the new cancel try/catch would miss this sibling.
- **Fix:** decide whether the production-complete purge should be non-fatal (sweep-backstopped) like
  cancel; if so, wrap it the same way + add the throwing-purger test.

### F5 · D22 — Purge-on-cancel try/catch is untested (throwing purger never exercised)
- **File:** `src/PhotoPrint.API/Services/AdminOrderService.cs:235` · tests `AdminOrderServiceTests.cs`
- **Conv:** 1 (tests-coverage) · **Verdict:** confirmed
- **Scenario:** `CancelOrderAsync` wraps the purge in try/catch (F17) so a purge failure can't fail the
  already-committed cancel+refund. But the setup (`AdminOrderServiceTests.cs:49-50`) stubs the purger
  to return `PurgeOutcome.Empty`, and the only Throws setups are refund (484) / ZIP (405) — none makes
  the cancel-path purger throw. Remove the try/catch (`:235-244`) and a throwing purger 500s after
  refund+email+SignalR already fired, with **no red test**.
- **Fix:** test — `purger.PurgeOrderOriginalsAsync` `ThrowsAsync(...)`, assert `CancelOrderAsync` still
  returns the DTO (does not throw) and the order persists `Cancelled`.

### F6 · D13 ⟳ — Order-photos empty-state conflates error / 401 / expired / gone, with no retry
- **File:** `src/PhotoPrint.UI/src/app/features/orders/pages/order-detail-page.ts` (`getOrderPhotos` catchError ~:371; copy at ~:104)
- **Conv:** 1 (frontend-ux) · **Verdict:** confirmed
- **Scenario:** `getOrderPhotos` `catchError(() => of({ photos: [] }))` maps **any** error (500,
  network blip status 0, 401-expired, guest stale-token) to empty photos with no status
  discrimination, no error signal, no retry — driving the permanent-sounding "Fotografiile… nu mai
  sunt disponibile". On a 500 that message shows **alongside** the error interceptor's "Eroare de
  server" toast (contradictory). A guest with a stale token hits the interceptor's silent
  `clearGuestToken` self-heal, but this request is never retried.
- **Prior:** v1 **D13** was Low and scoped only to "not-yet-promoted"; the owed frontend-ux lens
  re-found and **expanded** it (also transient error / 401 / no-retry / contradictory toast) → Medium.
- **Fix:** track a distinct error signal separate from empty; show a retry on error; reserve "no
  longer available" for a real empty 200. Pairs with a backend API signal distinguishing
  pending-promotion / cloud-off / post-retention (the original D13 fix direction).

### F7 · D5b ⟳ — Lightbox presigned URL captured at list-fetch expires (1h TTL), no refresh/fallback
- **File:** `src/PhotoPrint.UI/src/app/features/orders/pages/order-detail-page.ts` (`openLightbox` ~:379) · `photo-lightbox.component.ts` · `order.model.ts:47-48`
- **Conv:** 1 (frontend-ux) · **Verdict:** confirmed · **hinted** (presign/TTL topic seeded by the shared hints)
- **Scenario:** `ngOnInit`'s `getOrderPhotos` stores `largeUrl` (a presigned 1h-TTL URL, per
  `order.model.ts:47-48`) in the `photos` signal. User idles ~65 min, clicks a thumbnail;
  `openLightbox` sets `lightboxSrc` to the **stale** `largeUrl`; the lightbox `<img [src]>`
  (`photo-lightbox.component.ts:18-23`) fetches the now-expired URL → cloud 403 → broken-image icon.
  The `<img>` has **no `(error)` handler, no refresh, no fallback**. (Lazy-load defers the browser
  fetch, not the presigning.)
- **Prior:** v1 **D5 part-b**, deferred to this lens; now confirmed with a concrete trace.
- **Fix:** fetch a fresh presigned `largeUrl` on open (or re-fetch `getOrderPhotos` when stale), and
  add an `(error)` fallback on the lightbox + thumbnail `<img>`. *(Also fold in the misleading
  `order-detail-page.ts:145-146` comment the v2 pass flagged — it claims lazy per-open URL loading;
  only the byte fetch is lazy.)*

### F8 · D31 — `localUrl()` mints an unrevoked blob URL every change-detection cycle
- **File:** `src/PhotoPrint.UI/src/app/features/upload/components/photo-thumbnail/photo-thumbnail.component.ts:86` *(finding's line 652 was a mis-anchor; real line 86)*
- **Conv:** 1 (frontend-ux) · **Verdict:** confirmed
- **Scenario:** During upload each progress event calls `updateUpload` which rebuilds `state` as a new
  reference; the OnPush thumbnail runs CD; `[src]="localUrl()"` re-invokes `localUrl()`, which calls
  `URL.createObjectURL(state.file)` with no memoization (the only guard, `state.previewUrl ??`, is set
  solely for *restored* uploads — `format-selector-page.ts:405`; fresh uploads have no `previewUrl`).
  Result: a fresh **unrevoked** blob URL every event → memory leak + `<img>` churn/flicker.
  `(click)=preview.emit(localUrl())` leaks another.
- **Scope note:** a **pre-existing upload-flow issue**, not a 043 regression — surfaced because
  `photo-thumbnail` was in the full-surface pack. Real and worth fixing regardless.
- **Fix:** create the object URL once (computed/memoized field or on `state` assignment), reuse it,
  revoke on destroy — never mint it inside a template-evaluated method.

---

## 🟡 Lows

### F9 · D25 — StreamZipAsync `For(Cloud)` throws mid-stream when cloud disabled → corrupt ZIP
- **File:** `src/PhotoPrint.API/Services/AdminOrderService.cs:171`
- **Conv:** 1 (correctness) · **Verdict:** confirmed
- **Scenario:** Same misconfig as F2 (a Cloud upload un-purged, cloud reverted to local). During ZIP
  fulfilment the response ContentType/headers are already written and the `ZipArchive` opened on
  `Response.Body`; `For(item.Upload.StorageLocation)` then throws `InvalidOperationException`. The
  response has started (`HasStarted=true`), so no clean 500 — admin gets a truncated/broken ZIP and
  can't print. **Sibling class of F2/D24** (same `For(Cloud)`-throws-uncaught root; introduced by the
  F1 routing fix).
- **Fix:** before streaming, if any item is Cloud and `!router.CloudEnabled`, fail early with a clear
  error *before* writing headers; or skip/log unroutable items. Fix as one class with F2.

### F10 · D26 — Cleanup routes by StorageLocation, so failed-promotion cross-tier litter is never reclaimed
- **File:** `src/PhotoPrint.API/BackgroundJobs/UploadCleanupJob.cs:92`
- **Conv:** 2 (requirements, completeness-critic) · **Verdict:** confirmed
- **Scenario:** `OrderPhotoPromoter` writes 3 cloud objects (`:168-178`) then fails the row-flip
  `SaveChanges` (`:196`); retries + recovery keep failing; the row stays Local with
  `ThumbnailPath`/`LargePreviewPath` null (never persisted). After `ReferencedRetentionDays`,
  `CleanupAsync` resolves `For(Local)` and deletes only the local original; the null preview paths are
  skipped and the **cloud** original/thumb/preview (written under cloud keys) are never touched. Row
  soft-deleted → 3 cloud objects leak with no row to reclaim them.
- **Note:** residual of the F2 routing fix (which assumes `StorageLocation` reflects all blobs).
  Interacts with F1/D19 (stuck-Local promotions) and F11/D27 (orphan creation).
- **Fix:** decide whether the failed-promotion cloud-litter case needs a reclaim path (idempotent
  best-effort cloud delete on cleanup regardless of tier) or is knowingly deferred; record the call.

### F11 · D27 — Duplicate concurrent promotion re-creates a just-purged cloud original as an unreclaimable orphan
- **File:** `src/PhotoPrint.API/Services/OrderPhotoPromoter.cs:168`
- **Conv:** 1 (race) · **Verdict:** confirmed
- **Scenario:** Duplicate webhooks enqueue 2 jobs for one order; the worker runs P1+P2 in parallel
  (both loaded the upload as Local, separate DbContexts). P2 reads local bytes then pauses. P1 flips
  the row → Cloud, deletes local. Admin cancels → purge deletes cloud original X and nulls `FilePath`.
  P2 resumes, re-writes X at line 168; its `SaveChanges` sets `StorageLocation`/thumb/preview but
  leaves `FilePath` unmodified (stays null in DB). Purger, cleanup, and the recovery scanner all key
  on `FilePath != null`, so X is **never reclaimed** — PII past its retention window.
- **Note:** the delta's new/more-frequent purge triggers (purge-on-cancel F17 + periodic sweep F4)
  **widen** this window. The dup-webhook precondition overlaps **D9** (deferred → bolt-035).
- **Fix:** before the cloud writes+flip, re-read the row's live `StorageLocation`/`FilePath` (or add an
  EF concurrency token on `Upload`) so a promotion that lost the race to a purge aborts instead of
  resurrecting the original.

### F12 · D28 — New `PurgeSweepIntervalHours <= 0` validation rule has no test
- **File:** `src/PhotoPrint.API/Configuration/ArchiveSettings.cs:86` · tests `ArchiveSettingsValidatorTests.cs`
- **Conv:** 1 (tests-coverage) · **Verdict:** confirmed
- **Scenario:** The delta added the validator branch but no test (the validator tests cover
  `RetentionMonths`/`JobIntervalHours`/`BatchSize`, not `PurgeSweepIntervalHours`). Drop lines 86-87
  and the suite still passes; then `Archive:PurgeSweepIntervalHours=0` boots fine and
  `OriginalPurgeRecoveryScanner.cs:62` `new PeriodicTimer(TimeSpan.FromHours(0))` throws
  `ArgumentOutOfRangeException` at runtime — a host crash instead of the intended fail-fast boot error.
- **Fix:** validator test asserting `Validate` fails for `=0` and passes for `>0`.

### F13 · D30 — BackfillCommand filter-drift test never exercises the exclusion boundary
- **File:** `src/PhotoPrint.Tests/Unit/Cli/BackfillCommandTests.cs:40`
- **Conv:** 1 (completeness-critic) · **Verdict:** confirmed
- **Scenario:** The test's docstring says filter drift vs `PromotionRecoveryScanner` "must not ship
  untested", yet it only seeds Paid (113,127) and Printing (141) — both **included**. No case seeds
  Cancelled/PaymentFailed (assert excluded) or Shipped/Delivered (assert included). Add
  `|| o.Status==Cancelled` to the filter and all 6 tests stay green, yet a real cancelled order's
  purged/refunded photos get re-promoted. The stated filter-parity guarantee is unbacked at the
  boundary. *(Residual of the F18 fix.)*
- **Fix:** add cases seeding Cancelled + PaymentFailed (assert `PromoteOrderAsync` never called) and
  Shipped/Delivered (assert called).

### F14 · D29 — Preview TOCTOU re-resolve-to-Local success (200) branch untested
- **File:** `src/PhotoPrint.API/Controllers/UploadsController.cs:200`
- **Conv:** 1 (tests-coverage) · **Verdict:** plausible (coverage gap, no live bug)
- **Scenario:** F8's TOCTOU tests cover first-resolve-Local→re-resolve-Cloud (302) and both-resolves-
  gone (404), but not first-resolve-Local→re-resolve-Local-thumb-regenerated (200) at line 200 (both
  tests make the local read always throw). If the inner `StreamLocalAsync` regressed the 200 path
  would break with no red test. **Trace:** no defect today — line 200 is identical to the working
  line-184 call; pure coverage gap.
- **Fix:** unit test — `GetPreviewAsync` `SetupSequence` returns Local then Local; local
  `GetStreamAsync` throws once then returns a stream; assert a 200 `FileStreamResult`.

### F15 · D20 — FilePath NOT-NULL drop migration verified only on SQLite, not Postgres
- **File:** `src/PhotoPrint.Tests/Unit/Data/UploadThumbnailPathMigrationTests.cs:48`
- **Conv:** 1 (tests-coverage) · **Verdict:** plausible · **hinted** (dual-DB seeded by shared hints)
- **Scenario:** F7's test asserts nullable `FilePath` on **SQLite** only; purger unit tests use
  InMemory (null allowed regardless of DDL). If `MakeUploadFilePathNullable` emitted wrong/no DDL for
  **Postgres** (prod), the purger's `FilePath=null` `SaveChanges` would throw `DbUpdateException`,
  caught and counted `Failed` → originals silently never purge in prod, invisible to the suite.
  **Trace:** the migration *is* correct on Postgres today (`AlterColumn nullable:true` → `DROP NOT
  NULL`, `type:"TEXT"` valid) — so this is a real **coverage gap, not a live defect**.
- **Disposition:** the recurring **db-parity / DB-1** deferral seen on bolt-035/042 → Testcontainers
  in the 3-env track. Not new work for this bolt; tracked so it isn't re-litigated blindly.
- **Fix (when the 3-env arm lands):** a Postgres-provider migration/round-trip test (Testcontainers)
  asserting `Uploads.FilePath` is nullable, or a Postgres purger test persisting `FilePath=null`.

### F16 · D32 — getOrderDetail blanket catchError redirect (Medium strand refuted; Low no-retry residual)
- **File:** `src/PhotoPrint.UI/src/app/features/orders/pages/order-detail-page.ts:357`
- **Conv:** 1 (frontend-ux) · **Verdict:** plausible (Medium **refuted**, Low residual stands)
- **Claimed (Medium):** a mid-session-expired user clicking an order 401s; the interceptor logs out +
  navigates to `/auth/login`, then the component's `catchError` navigates to `/comenzile-mele`,
  "stranding" a logged-out user on a 401-ing list.
- **Refutation:** `/comenzile-mele` has `canActivate:[authGuard]` (`app.routes.ts:41`). The
  interceptor runs first (`logout()` → `isAuthenticated()=false`), then the component navigates to
  `/comenzile-mele`, which the authGuard redirects to `/auth/login`. The user reaches login, never a
  401-ing list — **the strand is impossible.**
- **Residual (Low):** on a **transient network blip / 5xx** the unconditional `catchError` bounces a
  still-authenticated user off the page with **no retry** (no status/auth check).
- **Fix:** don't redirect on transient/5xx/network errors — show an inline error + retry; let the
  interceptor own the 401→login redirect rather than overriding it.

### F17 · D33 — Lightbox modal lacks focus trap / role=dialog / aria-modal / focus restore
- **File:** `src/PhotoPrint.UI/src/app/shared/components/photo-lightbox/photo-lightbox.component.ts` *(finding's line 503 was a mis-anchor; file is 77 lines; template ~14-26, 68-76)*
- **Conv:** 1 (frontend-ux) · **Verdict:** confirmed
- **Scenario:** A keyboard/SR user opens the lightbox: the overlay `<div>` has no
  `role=dialog`/`aria-modal`, so it isn't announced as a dialog; no autofocus/tabindex and no focus
  trap, so Tab walks through the page content behind the backdrop; only Escape is handled; on close
  focus isn't returned to the triggering thumbnail. (The accessibility/UX lens the manifest requires
  for a frontend change — first-time coverage from the owed pass.)
- **Fix:** add `role=dialog` + `aria-modal=true`, move focus into the dialog on open, trap Tab within
  it, restore focus to the trigger on close.

### F18 · D34 — Order/photos loaded only in ngOnInit despite route-bound orderId input (latent staleness)
- **File:** `src/PhotoPrint.UI/src/app/features/orders/pages/order-detail-page.ts:357` (load path ~:351)
- **Conv:** 1 (frontend-ux) · **Verdict:** plausible (**latent — not triggerable today**)
- **Scenario:** `orderId` is a route-bound input (`withComponentInputBinding`) but the order + photos
  load only in `ngOnInit`; no `effect()` or `paramMap` subscription watches it. If a future
  detail→detail link is added, Angular's default `RouteReuseStrategy` would reuse the component
  without re-running `ngOnInit` → stale previous-order data + photos.
- **Refutation of "live today":** every entry to `/comenzile-mele/:id` currently comes from the list
  route (distinct `routeConfig`), which destroys/recreates the component; there is no detail→detail
  navigation. No failing trace is constructible against the current code — a **latent trap**, recorded
  so a future nav change doesn't reintroduce it silently.
- **Fix:** react to `orderId` via an `effect()` or the route param observable, re-fetching order +
  photos, rather than loading once in `ngOnInit`.

---

## Refuted — recorded so it isn't re-raised

### RV3-1 — Promotion worker drain defeated by a long in-flight S3 upload vs host shutdown timeout
- **File:** `src/PhotoPrint.API/BackgroundJobs/OrderPhotoPromotionWorker.cs:79`
- **Was:** 🟡 Low (completeness-critic, conv 1) · **Verdict:** **refuted**
- **Claim:** F6's `await Task.WhenAll(inFlight)` drain could still abandon a mid-write promotion if a
  `TransferUtility` multipart upload exceeds the host shutdown timeout and the host force-terminates.
- **Why refuted:** `OrderPhotoPromoter` is **Confirmed-Write-Then-Delete** — the row flips to Cloud
  only *after* all cloud writes succeed (`OrderPhotoPromoter.cs:188-192`), the same-key PUT is
  idempotent (`:159-161`), and the worker leaves an incomplete one for the recovery scan
  (`Worker.cs:118`). So a force-terminated mid-write leaves the row **Local** and is
  harmless/re-doable regardless of the shutdown timeout. The cancellation token also flows all the way
  to `TransferUtility.UploadAsync`. The claimed "abandoned-mid-write" outcome is already prevented.
