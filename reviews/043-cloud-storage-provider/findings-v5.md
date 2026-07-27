---
type: review-findings
target: 043-cloud-storage-provider
version: 5
commit: 972a8b4
delta-base: 151abef
pass-type: delta-discovery
date: 2026-07-20
---

# Findings detail — 043-cloud-storage-provider v5 (delta-discovery)

Full per-finding record for [review-v5.md](review-v5.md). Blinded 5-lens delta pass over the v3 fix
round (`151abef..972a8b4`). `Conv` = independent lenses; verdicts are the workflow's
convergence-weighted skeptics. `⟳` = re-find of a prior `D#`. Line numbers are the lenses' anchors
(some flagged as approximate; the evidence text is authoritative).

---

## 🟠 Mediums

### F1 · D36 — Stale `lightboxPhotoId` re-opens a *closed* lightbox on a thumbnail error (regression, my F7 fix)
- **File:** `src/PhotoPrint.UI/src/app/features/orders/pages/order-detail-page.ts` (`refreshPhotoUrls`; close handler)
- **Conv:** 4 (correctness, race, tests-coverage, frontend-ux) · **Verdict:** confirmed
- **Scenario:** User opens photo A in the lightbox, then closes it — the close handler sets
  `lightboxSrc.set(null)` but leaves `lightboxPhotoId = A`. Later a lazy-loaded grid thumbnail whose
  presigned URL has expired fires `(error)` → `onThumbnailError` → `refreshPhotoUrls`, which reads
  `openPhotoId = this.lightboxPhotoId` (still A), finds fresh A, and calls
  `lightboxSrc.set(A.largeUrl)`. The lightbox renders on any non-null `src()` → **the modal re-opens
  with no user action.**
- **Evidence:** close sets only `lightboxSrc=null`; `refreshPhotoUrls` gates only on
  `openPhotoId !== null` and "fresh found", never on the lightbox being open; `urlsRefreshed` only
  caps loops. 4 independent lenses converged (no skeptic could find a guard).
- **Fix:** clear `lightboxPhotoId = null` in the close handler (make it a method that resets both),
  or gate the lightbox re-point in `refreshPhotoUrls` on `lightboxSrc() !== null`. Regression test:
  open→close, then dispatch a grid `(error)`, assert `lightboxSrc()` stays null.

### F2 · D38 — Unroutable-Cloud cleanup skip starves the batch (my F2 fix; the edge I dismissed)
- **File:** `src/PhotoPrint.API/BackgroundJobs/UploadCleanupJob.cs` (query lines ~74-83; skip ~95-99)
- **Conv:** 2 (correctness, completeness-critic) · **Verdict:** confirmed
- **Scenario:** `Storage:Provider=local` (cloud disabled) while ≥500 aged Cloud uploads match the
  retention predicate. The candidate query is `OrderBy(UploadedAt).Take(500)` with **no** `WHERE`
  excluding Cloud rows; the F2 skip runs *post-fetch* and never sets `DeletedAt`. So every hourly
  sweep re-selects the same oldest 500 unroutable Cloud rows (skipped), and **local orphans sorted
  after them are never reached → disk cleanup wedges indefinitely.** My "left for a later sweep"
  comment is wrong at scale.
- **Fix:** exclude unroutable Cloud rows in the query itself (`.Where(u => cloudEnabled || u.StorageLocation != Cloud)`)
  so the `Take` window advances to routable rows. Regression test: seed >BatchSize unroutable Cloud
  rows + one aged local orphan with cloud disabled; assert the local orphan is soft-deleted.
- **Note:** during the F2 fix I explicitly considered this and judged it out-of-scope ("requires >500
  aged Cloud rows"). The blinded lens shows the wedge is real once that population exists; the
  query-filter is the correct fix (I chose the weaker loop-skip). Correcting the call.

### F3 · D37 — F1's periodic re-scan (its whole purpose) is untested and untestable
- **File:** `src/PhotoPrint.API/BackgroundJobs/PromotionRecoveryScanner.cs` (`ExecuteAsync` periodic loop) · tests `PromotionRecoveryScannerTests.cs`
- **Conv:** 1 (tests-coverage) · **Verdict:** confirmed
- **Scenario:** The only `ExecuteAsync` test (`ExecuteAsync_StuckOrder_BootSweepEnqueues`) awaits the
  **boot** sweep's enqueue then `StopAsync` — it passes even if the `while (WaitForNextTickAsync)`
  loop is deleted. The interval is whole-hours (`PromotionRecoverySweepIntervalHours`, no seconds
  override), so no test can trigger a periodic tick. **Delete the periodic loop and the suite stays
  green — the exact F1 bug (boot-only) returns invisibly.**
- **Fix:** an internal `TimeSpan` interval seam (or `Func<TimeSpan>`) defaulting to
  `FromHours(PromotionRecoverySweepIntervalHours)`; a test sets ~20 ms, seeds a stuck Paid+Local
  order, and asserts `EnqueueAsync` fires ≥2× (boot + one periodic tick). *(This is the red-able test
  I declined during the F1 fix — verified-by-inspection then; the lens makes the case the seam is
  worth it. A `TimeProvider`/`FakeTimeProvider` seam would serve both this and OriginalPurgeRecoveryScanner.)*

---

## 🟡 Lows

### F4 · D35 ⟳ — Periodic sweep re-enqueues in-flight orders (no dedup) → duplicate promotion + false failure signal
- **File:** `src/PhotoPrint.API/BackgroundJobs/PromotionRecoveryScanner.cs` · `OrderPhotoPromotionWorker.cs` · `OrderPhotoPromoter.cs`
- **Conv:** 2 (correctness, race) · **Verdict:** confirmed · **re-find of D35 (NF1, v4)**
- **Scenario:** A still-Local paid order is mid-retry (webhook/backoff) when the sweep fires and
  re-enqueues it (the queue has no dedup, the worker fans out with a plain `List<Task>`,
  `MaxConcurrentOrders=4`). Two slots promote it concurrently: job A flips to Cloud + best-effort
  deletes the local original; job B (past the already-Cloud short-circuit) then calls
  `GetStreamAsync(localKey)` → `FileNotFound` → logs `promotion.upload.failed
  reason=local-original-missing` + a wasted retry, **for an order that actually promoted fine** — a
  misleading ops signal.
- **Relation:** same root as **D35/NF1** (F1 sweep has no dedup); the v4 verifier framed it as the
  D27 orphan race, this blinded pass independently re-found it via the false-signal consequence →
  convergence corroboration. Fix folds into the D35/D27/D9 concurrency work (bolt-035); a cheap
  interim is to downgrade the `local-original-missing` log to Info once the row is already Cloud.

### F5 · D46 — Periodic sweep re-enqueues permanently-terminal promotions forever
- **File:** `src/PhotoPrint.API/BackgroundJobs/PromotionRecoveryScanner.cs` (`RunSweepAsync` query)
- **Conv:** 1 (completeness-critic) · **Verdict:** confirmed
- **Scenario:** A paid order whose local original was lost pre-promotion: the worker exhausts
  `MaxAttempts`, logs `promotion.failed.terminal`, row stays Paid+Local. There is no terminal marker,
  so every `PromotionRecoverySweepIntervalHours` the sweep re-selects it → another `MaxAttempts`
  burst + terminal Error log, **perpetually per stuck order** (the old boot-only scanner never
  repeated this). Fix-generated by F1's periodicity.
- **Fix:** a row-level give-up marker (attempt count / `PromotionAbandonedAt`) excluded from the sweep
  query, or throttle re-sweep of known-terminal orders.

### F6 · D45 — F9's ZIP pre-flight throw is unmapped → misleading 500
- **File:** `src/PhotoPrint.API/Services/AdminOrderService.cs` (`StreamZipAsync` guard) · `ExceptionHandlerMiddleware`
- **Conv:** 1 (completeness-critic) · **Verdict:** confirmed
- **Scenario:** Cloud disabled, promoted order with a Cloud item; admin downloads the ZIP → the F9
  guard throws `InvalidOperationException`. The middleware's exact-type mapping has no entry for it →
  the else branch logs "Unhandled exception" at Error + returns a generic 500. Ops can't distinguish
  a config error from a real crash.
- **Fix:** throw a mapped domain exception (409/422) or add an `InvalidOperationException` mapping;
  log the cloud-tier-off reason at Warning. *(F9's v3 rationale accepted "a clean 500 beats a
  truncated ZIP"; the improvement is a mapped status + a diagnostic log.)*

### F7 · D47 — `CloudEnabled` fixed at boot; a runtime Provider flip needs a restart
- **File:** `src/PhotoPrint.API/BackgroundJobs/PromotionRecoveryScanner.cs` · `StorageRouter`
- **Conv:** 1 (completeness-critic) · **Verdict:** confirmed
- **Scenario:** Boot with `Provider=local` → `StorageRouter._cloud=null`, `CloudEnabled` permanently
  false; `ExecuteAsync` logs `cloud-tier-off` and returns (the task completes). Operator later sets
  `Provider=S3`; nothing re-runs `ExecuteAsync`, `_cloud` stays null → **no sweep until a full
  restart.** This contradicts the "retried when cloud returns / set back to S3" claim in the F2
  cleanup skip comment (D24) and the recovery design intent.
- **Fix:** document the restart requirement in the settings comments, or use `IOptionsMonitor` +
  re-evaluate `CloudEnabled` per sweep so a config change takes effect without a restart.

### F8 · D43 — 401 on order fetch strands a guest/anon on a blank page  *(hinted)*
- **File:** `src/PhotoPrint.UI/src/app/features/orders/pages/order-detail-page.ts` (`loadOrder` 401 path) · `error.interceptor.ts`
- **Conv:** 1 (frontend-ux) · **Verdict:** confirmed · **hinted** (guest-auth topic seeded by the shared hints)
- **Scenario:** A non-authenticated user (guest / expired guest token) opens an order URL →
  `getOrderDetail` 401s. The interceptor's guest branch only `clearGuestToken()` (no navigate) and
  rethrows; `loadOrder`'s `catchError` skips the 403/404 navigate and the `!==401` `orderError`
  branch, running only `loading.set(false)`. Result: `loading()=false`, `order()=null`,
  `orderError()=false` → all `@if` blocks false → **blank body, no retry, no redirect.** The F16 fix
  comment assumes the interceptor always redirects, but it only does for *authenticated* 401s.
- **Fix:** for a 401 on a non-authenticated user, surface the retryable order-error (or redirect);
  don't rely on the interceptor navigating.

### F9 · D42 — Auto-heal shows a misleading "reload the page" error before silently recovering
- **File:** `src/PhotoPrint.UI/src/app/shared/components/photo-lightbox/photo-lightbox.component.ts` (`onImgError`)
- **Conv:** 1 (frontend-ux) · **Verdict:** confirmed
- **Scenario:** An expired lightbox `largeUrl` errors → `failed.set(true)` synchronously renders
  "Imaginea nu a putut fi încărcată… Reîncarcă pagina", while the parent's `onLightboxError` →
  `refreshPhotoUrls` does an async `getOrderPhotos` round-trip and re-sets a fresh `largeUrl`. On
  success the effect clears `failed` and the image appears — the user was told to reload for an error
  the app auto-recovered from a moment later.
- **Fix:** on the first `(error)` show a neutral "Se reîncarcă…" state; only show the reload-copy
  after the single refresh attempt has failed.

### F10 · D48 — Lightbox `failed()` reset keyed on src inequality; an identical refreshed URL stays stuck
- **File:** `src/PhotoPrint.UI/src/app/shared/components/photo-lightbox/photo-lightbox.component.ts` (`failed` effect)
- **Conv:** 1 (completeness-critic) · **Verdict:** confirmed (confidence 4 — narrow)
- **Scenario:** Lightbox img errors → parent refreshes → re-sets `largeUrl`. If the refreshed
  presigned URL string equals the failed one (same signing second, or a stable non-presigned dev path)
  then `src===lastSrc`, the effect never clears `failed`, and the `urlsRefreshed` guard blocks a
  second attempt → the error persists until a full page reload.
- **Fix:** reset `failed()` on every open/refresh-driven `src` assignment regardless of string
  equality (or cache-bust the URL).

### F11 · D40 — Anti-refresh-loop guard (`urlsRefreshed`) has no test
- **File:** `src/PhotoPrint.UI/src/app/features/orders/pages/order-detail-page.ts` · spec `order-detail-page.spec.ts`
- **Conv:** 1 (tests-coverage) · **Verdict:** confirmed
- **Scenario:** `urlsRefreshed` caps refreshes to one per load/open so a permanently-bad URL can't
  storm `getOrderPhotos`. The specs dispatch only *one* `(error)` and assert `getOrderPhotos` called
  2×; none fires a second error to assert no third fetch. A regression resetting the guard per-error
  would loop unbounded and ship green.
- **Fix:** a spec that dispatches a second `(error)` (refresh resolved to a still-bad URL) and asserts
  `getOrderPhotos` was called exactly twice.

### F12 · D41 — Lightbox focus-trap (`trapFocus`) has no spec
- **File:** `src/PhotoPrint.UI/src/app/shared/components/photo-lightbox/photo-lightbox.component.ts` (`trapFocus`)
- **Conv:** 1 (tests-coverage) · **Verdict:** confirmed
- **Scenario:** `trapFocus` (Tab/Shift+Tab `preventDefault` + refocus the close button) is the F17
  focus trap, but the new spec only tests open/close focus move. Drop `preventDefault` (or the
  refocus) and Tab escapes the modal to the page behind the backdrop — no test reddens.
- **Fix:** a spec that opens the lightbox, dispatches `keydown.tab`, and asserts
  `event.defaultPrevented` + `document.activeElement` stays the close button.

### F13 · D39 — Renamed F1 guard tests seed no data → pass for the wrong reason
- **File:** `src/PhotoPrint.Tests/Unit/Services/PromotionRecoveryScannerTests.cs`
- **Conv:** 1 (tests-coverage) · **Verdict:** confirmed
- **Scenario:** `ExecuteAsync_ArchiveDisabled_DoesNothing` / `_CloudTierOff_DoesNothing` use an empty
  `CreateDb()`. Delete the `if(!Enabled) return;` / cloud-off guard and `ExecuteAsync` runs the boot
  sweep over zero orders → enqueues nothing → `queue.VerifyNoOtherCalls()` still passes. Guard removal
  ships green.
- **Fix:** seed one stuck Paid+Local order in both guard tests so a removed guard would enqueue and
  redden the assertion.

---

## ⚪ Cleanup

### F14 · D44 — Order retries + parallel inits have no in-flight dedup or teardown
- **File:** `src/PhotoPrint.UI/src/app/features/orders/pages/order-detail-page.ts`
- **Conv:** 1 (frontend-ux) · **Verdict:** unverified-cleanup (⚪ — skeptics skipped)
- **Scenario:** Rapid clicks on "Reîncearcă" fire overlapping `getOrderDetail`/`getOrderPhotos`; out-of-order
  responses mean a slow stale response can overwrite a newer one (last-arriving wins). `ngOnInit`
  subscriptions lack `takeUntilDestroyed`, so a late response can set signals after destroy.
- **Fix:** disable the retry button while `loading()`/`photosLoading()`; use `switchMap` +
  `takeUntilDestroyed` to cancel superseded/orphaned requests.

---

## Notes for the fixer / next pass

- **D36 (Medium regression) and D38 (Medium batch-starvation)** are self-contained and should be
  fixed now, each with the regression test named above.
- **The F1 periodic-sweep cluster — D35/D37/D46/D47** — is one design concern, not four patches:
  the periodic promotion-recovery model needs dedup (D35), a testable interval seam (D37), a terminal
  give-up marker (D46), and runtime-config awareness (D47). Per fixer rule #3 (design-check
  escalation), run one adversarial design pass on the model before patching, and fold the concurrency
  half into the **bolt-035** `Order`/`Upload` concurrency-token work already carrying D9/D27.
- **The frontend-refresh cluster — D40/D42/D48 + coverage D39/D40/D41** — batch into one frontend fix.
- No High, no blocker; nothing here blocks the branch, but the delta is **not quiet**, so certification
  is not yet in reach.
