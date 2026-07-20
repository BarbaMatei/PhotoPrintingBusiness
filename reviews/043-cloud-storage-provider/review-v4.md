---
type: review
target: 043-cloud-storage-provider
version: 4
supersedes: 3
commit: 972a8b4
branch: feat/bolt-043-cloud-storage-provider
pass-type: verification
date: 2026-07-20
reviewer: "independent verification (revert-and-rerun + inspection + fix-diff review)"
verifies: resolution-v3.md
verdict: approve-with-followups
blockers: []
verified: [F1, F2, F3, F4, F5, F6, F7, F8, F9, F12, F13, F14, F16, F17]
reopened: []
upheld: [F10, F11, F15, F18]
new: [NF1]
tests: { dotnet: "701/701 (+10 skipped MinIO; 1 flaky fail on first run, green on rerun)", frontend: "438/438 (3 load-timeout flakes under full-parallel load, all non-043, green in isolation)" }
---

# Review v4 — 043-cloud-storage-provider (verification pass)

Independent, anchored verification of [resolution-v3.md](resolution-v3.md) against `fixed_commit`
`972a8b4` (HEAD `9283a5d` is a docs-only commit; src at HEAD is identical to `972a8b4`, tree clean
at start). This is a **verification** pass, not discovery: it checks that the 18 v3 findings' specific
fixes hold and that the accepted deferrals still stand — it is *not* a fresh audit and cannot certify
saturation (README *Two loops*). Per the runbook, a verification pass emits **at most**
`approve-with-followups`.

**Verdict: `approve-with-followups`.** All **14** fixed findings verified non-vacuously; the **4**
deferrals (F10/F11/F15/F18) and the **F6 partial** upheld; **0 reopened**; **1 NEW** Low finding
(NF1) surfaced by the three-question fix-diff review — a fix-generative interaction of this round's
own F1 with the deferred F11 race. No blockers. Feature closure still requires the owed
full-manifest discovery + certification pair (the v1 pass was lean; v3 was a delta).

## How this was verified

Every fix was proven **independently** (the fixer's own revert-checks did not count). The tree was
confirmed clean (`git status --porcelain` empty) before, between, and after every revert.

1. **Revert-and-rerun (the non-vacuity proof) — 14 findings.** For each fix I reverted *only* the
   production change (whole-file `git checkout <fixcommit>~1 -- <file>` where that compiled against
   the HEAD tests; a surgical mutation of the exact guarded line where a whole-file revert would break
   spec compilation), ran the owning test(s), confirmed **RED with clean attribution and no
   collateral**, then restored and confirmed **GREEN**. Test-only findings (F3/F5/F12/F13/F14) have no
   production change, so I reverted the *code they guard* (resolution-v3 named it) and confirmed the
   new test reddens.

   | Finding | What I reverted / mutated | Test(s) that reddened | Result |
   |---|---|---|---|
   | F1 (wiring) | comment out `SafeSweepAsync("boot")` in `PromotionRecoveryScanner` | `PromotionRecoveryScannerTests.ExecuteAsync_StuckOrder_BootSweepEnqueues` | RED (10s timeout) → GREEN |
   | F2 | `git checkout 4674dcd~1 -- UploadCleanupJob.cs` | `UploadCleanupJobTests.Cleanup_cloudRowWithCloudDisabled_skipsItAndStillCleansLocalBatch` | RED (1) → GREEN |
   | F3 | comment out `SafeSweepAsync("boot")` in `OriginalPurgeRecoveryScanner` | `OriginalPurgeRecoveryScannerTests.ExecuteAsync_EnabledCloudOn_BootSweepFiresPurger` | RED (10s timeout) → GREEN |
   | F4 | `git checkout c30d734~1 -- AdminOrderService.cs` | `…UpdateStatusAsync_ProductionCompletePurgeThrows_TransitionStillCommittedAndNotified` | RED → GREEN |
   | F5 | remove the F17 cancel-purge try/catch (the guard F5 tests) | `…CancelOrderAsync_PurgeThrows_OrderStillCancelledAndExceptionSwallowed` | RED (1) → GREEN |
   | F6 | `catchError` in `loadPhotos` → old empty-on-error | 2 tests (photos error+retry; retry button) | RED (2) → GREEN |
   | F7 (parent+grid) | `refreshPhotoUrls()` → no-op | 2 tests (lightbox URL refresh; **grid** thumbnail refresh) | RED (2) → GREEN |
   | F8 | `localUrl()` → un-memoized `createObjectURL` per call | 3 tests (same-url, once, revoke-on-destroy) | RED (3) → GREEN |
   | F9 | `git checkout c30d734~1 -- AdminOrderService.cs` | `…StreamZipAsync_CloudOriginalWithCloudDisabled_FailsBeforeWritingAnyBody` | RED → GREEN |
   | F12 | comment out **both** validator rules | `…Validate_NonPositivePurgeSweepInterval_Fails` + `…Validate_NonPositivePromotionRecoveryInterval_Fails` | RED (2) → GREEN |
   | F13 | mutate `BackfillCommand` filter (Shipped→Cancelled) | `…RunAsync_ExcludedStatus_IsNotPromoted(Cancelled)` + `…IncludedPostPaidStatus_IsPromoted(Shipped)` | RED (2), others green → GREEN |
   | F14 | break the TOCTOU re-resolve-to-Local 200 branch | `…GetPreviewAsync_LocalThumbRegeneratedOnReResolve_Returns200` | RED (1), other 6 green → GREEN |
   | F16 | `loadOrder` catchError → redirect-on-any-error | 2 tests (transient 500 inline; 401 no-redirect) | RED (2) → GREEN |
   | F17 | remove `role/aria-modal` + disable focus effect | 2 tests (labelled dialog; focus move/restore) | RED (2) → GREEN |

   Every mutated case reddened *exactly* the expected test(s) with no collateral, and the tree was
   clean after each restore.

2. **F1 periodicity — verified by inspection.** F1's *boot-sweep wiring* is revert-proven above; its
   *periodic re-scan* (boot sweep → `PeriodicTimer(PromotionRecoverySweepIntervalHours)` loop →
   `SafeSweepAsync("periodic")`) is a structural change with no red-able periodic-tick test, exactly as
   its purge sibling F4 was inspection-verified in v2. I read `PromotionRecoveryScanner.cs` at HEAD and
   confirmed it is now a `BackgroundService` whose `ExecuteAsync` mirrors `OriginalPurgeRecoveryScanner`
   line-for-line: same guards, boot sweep, `PeriodicTimer` loop, `SafeSweepAsync` catch shape. Real.

3. **Class-sweep siblings (the fixer's fresh-eyes fixes) — both hold.**
   - `0fc577a` (**UploadService.GetPreviewAsync** customer-preview guard, F2/F9 class): revert-proven —
     `git checkout 0fc577a~1` reddens `GetPreviewAsync_CloudUploadWithCloudDisabled_ThrowsNotFound`.
   - `972a8b4` (**grid-thumbnail** refresh, F7 class): proven by the F7 no-op mutation above, which
     reddens the dedicated `…refreshes photo URLs when a GRID thumbnail image errors` test.

4. **Full suites** run at the end (below).

## Findings — verification status

| F# | D# | Sev | v3 status | v4 verdict | Method |
|----|----|-----|-----------|------------|--------|
| F1 | D19 | 🟠 | fixed | **verified** | revert-and-rerun (boot-sweep wiring) + inspection (periodic structure) |
| F2 | D24 | 🟠 | fixed | **verified** | revert-and-rerun (git-checkout) |
| F3 | D21 | 🟠 | fixed | **verified** | revert-and-rerun (guarded boot sweep → timeout) |
| F4 | D23 | 🟠 | fixed | **verified** | revert-and-rerun (git-checkout) |
| F5 | D22 | 🟠 | fixed | **verified** | revert-and-rerun (removed F17 cancel try/catch) |
| F6 | D13 | 🟠 | fixed | **verified** (partial upheld) | revert-and-rerun |
| F7 | D5b | 🟠 | fixed | **verified** | revert-and-rerun (lightbox + grid) |
| F8 | D31 | 🟠 | fixed | **verified** | revert-and-rerun |
| F9 | D25 | 🟡 | fixed | **verified** | revert-and-rerun (git-checkout) |
| F10 | D26 | 🟡 | deferred | **upheld** | inspection of rationale |
| F11 | D27 | 🟡 | deferred | **upheld (rationale incomplete → NF1)** | inspection of rationale + code |
| F12 | D28 | 🟡 | fixed | **verified** | revert-and-rerun (both validator rules) |
| F13 | D30 | 🟡 | fixed | **verified** | revert-and-rerun (filter mutation, both boundaries) |
| F14 | D29 | 🟡 | fixed | **verified** | revert-and-rerun (broke 200 branch) |
| F15 | D20 | 🟡 | deferred | **upheld** | inspection of rationale (recurring db-parity) |
| F16 | D32 | 🟡 | fixed | **verified** | revert-and-rerun |
| F17 | D33 | 🟡 | fixed | **verified** | revert-and-rerun |
| F18 | D34 | 🟡 | deferred | **upheld** | inspection of rationale |

**Verified: 14 · Reopened: 0 · Deferrals upheld: 4 (+ F6 partial) · New: 1 (NF1).**
All 14 fixed findings carry a red-able regression test; F1 additionally corroborated by inspection.

## The 4 deferrals + the F6 partial

These are **new v3 deferrals** (no prior affirmation), so I judged each *rationale* for soundness, not
whether it was fixed.

- **F10 / D26 — failed-promotion cross-tier cloud litter: UPHELD.** The reclaim genuinely needs an
  orphan-sweep design (best-effort cloud delete keyed off the deterministic key scheme regardless of
  the row's recorded tier) — a design, not a patch, and the same class deferred on bolt-042
  (D31/M1). No data loss today (local original + row remain; only cloud bytes leak, and only after a
  *persistent* mid-promotion flip failure). Note in the fix's favour: this round's own **F1** (now a
  periodic promotion sweep) *narrows* F10 — a **transient** flip failure now self-heals on the next
  sweep (re-enqueue → idempotent re-write → flip → cleanup then routes to Cloud). Rationale sound.

- **F11 / D27 — dup-promotion re-creates a just-purged original: UPHELD, but rationale INCOMPLETE.**
  The clean fix (re-read live `StorageLocation`/`FilePath` before the flip, or an EF concurrency token
  on `Upload`) is the same concurrency-token work as D9, and folding it into bolt-035 is reasonable.
  **However** the deferral rests on the precondition being *duplicate webhooks* (D9). I confirmed by
  reading `OrderPhotoPromoter.cs` that the promoter does **not** re-read live state before the flip
  (Step 3 flips unconditionally; `FilePath` is left untouched so a purge's null persists), and by
  reading `OrderPhotoPromotionWorker.cs` that the worker has **no per-order in-flight dedup** (a plain
  `List<Task>`, `MaxConcurrentOrders=4`). So F1's own dedup-less periodic sweep can enqueue a *second*
  concurrent promotion of an order that is *still mid-promotion* — an F11 trigger that does **not**
  need dup webhooks. That is **NF1** below; the F11 deferral should carry this second trigger.

- **F15 / D20 — Postgres migration parity: UPHELD.** The recurring db-parity/DB-1 deferral carried
  across bolt-035/042. The `FilePath` NOT-NULL-drop migration is verified only on SQLite; a v3 skeptic
  confirmed it is correct on Postgres today (coverage gap, not a live defect). The cited file is
  unchanged since v3. Lands with the 3-env/Testcontainers track. Rationale sound.

- **F18 / D34 — ngOnInit-only staleness: UPHELD.** Still latent: every entry to
  `/comenzile-mele/:id` comes from the list route (distinct `routeConfig`), which recreates the
  component; there is no detail→detail navigation, and the fixes introduced none. Becomes real only if
  such a link is added. Rationale sound.

- **F6 partial (four-way empty signal): UPHELD as a legitimate follow-up.** The fix correctly splits a
  fetch *failure* (retryable, verified) from a genuine empty 200; distinguishing **not-yet-promoted**
  vs **cloud-off** vs **post-retention-purge** genuinely needs a backend signal on
  `GET /orders/{id}/photos` (an API-contract change), so deferring it is right. The core D13 defect
  (transient error shown as permanent, no retry) is fixed and verified. Remains open as a follow-up.

## Three-question fix-diff review (per cluster, by the owning lens)

**(a) Class or instance — do sibling sites still carry the defect?** I ran independent class scans:

- **`.For(<location>)` unguarded-throw class (F2/F9):** exactly **3** call sites exist —
  `UploadCleanupJob:105` (F2 ✓), `AdminOrderService.StreamZipAsync:198` (F9 pre-flight ✓),
  `UploadService.GetPreviewAsync:162` (0fc577a ✓). All three are guarded. **Class complete.**
- **Boot-only `IHostedService` class (F1/F4):** only `S3BucketVerifier` remains `IHostedService`, and
  it is *correctly* boot-only (a one-shot bucket check). All recovery/worker jobs are
  `BackgroundService`. **Class complete.**
- **Presigned-TTL-`<img>` class (F7):** the two TTL-bound URLs (`OrderPhotoDto.thumbnailUrl` grid,
  `.largeUrl` lightbox) both now refresh on `(error)`. The order-**item** thumbnail
  (`order-detail-page` line ~72) binds `OrderItemDto.previewUrl`, which the backend builds as a
  **stable controller path** `"/api/uploads/{id}/preview"` (not a presigned TTL URL) — correctly **not**
  a sibling. **Class complete.**
- **Blob-URL leak class (F8):** independent scan of all `createObjectURL` sites — `admin.service.ts`
  revokes immediately after download (safe); `photo-thumbnail` is F8-fixed; `upload.service.getPreviewBlob`
  mints a URL handed to **`cart-page.ts`**, which stores it in a `blobUrls` map and (I confirmed —
  `implements OnInit` only, no `ngOnDestroy`/revoke) never releases it. This is the sibling the fixer
  **flagged as out-of-scope**; the flag is substantively accurate (the leak lives in the cart feature,
  outside bolt-043's surface — the createObjectURL is in `getPreviewBlob`, not literally in
  `cart-page.ts` as the note said, but the un-revoked ownership is cart-page's). Correct disposition.

**(b) New surface at the bar (rule 2) — sized defaults, signal, tests?**
- F1's `PromotionRecoverySweepIntervalHours=6h` is sized against the ~1.5h retry envelope, validated
  (`<=0` fails fast — verified), observable (`promotion.recovery.started/enqueued/sweep.error`), and
  the boot-sweep is tested. **One gap:** the sweep has no dedup against in-flight/queued jobs → NF1.
- F2/F9/0fc577a guards each emit an ops signal (`upload.cleanup.unroutable`,
  `uploads.preview.unroutable`) and F9 throws *before* any response byte (verified — pre-flight
  condition `FilePath is not null && Cloud` exactly mirrors the streaming loop's `FilePath is null →
  continue`, so no purged-Cloud edge slips through). Sized/observable/tested. Good.
- F16/F6 add `orderError`/`photosError` signals + retry actions; F7 adds `refreshPhotoUrls` guarded
  against a refresh loop (`urlsRefreshed`, reset per grid-load / per lightbox-open); F17 adds
  effect-based focus management. All tested. Good.

**(c) Regression — did any fix break adjacent behavior?** None found.
- F1's registration change (ordering-dependent → channel-decoupled) is safe: `StartAsync` now returns
  before the boot sweep, the worker blocks on `ReadAllAsync` until the scanner enqueues, so the order
  no longer matters. Startup is also no longer blocked on the full boot sweep — an improvement.
- F16 on a 401 now sets neither order nor error (leaves the redirect to the interceptor) — a brief
  blank state that the interceptor's logout→login resolves; not worse than the old bounce-to-list
  (which the authGuard sent to login anyway).
- F2's return count (`candidates.Count - unroutable`) is log-only. F4's best-effort purge and F9's
  pre-flight both preserve the committed-transition ordering.

## NEW finding

### NF1 · 🟡 Low — F1's periodic promotion sweep has no in-flight dedup, an independent trigger for the deferred F11/D27 orphan race
- **Files:** `PromotionRecoveryScanner.cs` (`RunSweepAsync`), `OrderPhotoPromotionWorker.cs`
  (no per-order dedup), `OrderPhotoPromoter.cs` (no live re-read before the flip).
- **Class:** fix-generative — *new mechanism, new fault* (README rule #2), introduced by **this
  round's** F1.
- **Scenario:** `RunSweepAsync` enqueues *every* paid-or-beyond order still holding a Local upload,
  with no check against jobs already queued or in-flight. The worker fans out to
  `MaxConcurrentOrders=4` with only a `List<Task>` (no active-order set). So if the sweep ticks while
  an order's promotion job is in-flight (row still Local), it enqueues a **second** concurrent
  promotion of that order. Both jobs load the upload as Local (separate `DbContext`s); job A flips to
  Cloud + deletes local; a purge (cancel / production-complete / periodic) deletes the cloud original
  and nulls `FilePath`; job B resumes, idempotently re-writes the cloud object, and its `SaveChanges`
  sets `StorageLocation=Cloud`/thumb/preview but never touches `FilePath` (stays null) → an
  **unreclaimable cloud orphan** (PII past retention). This is exactly the F11/D27 outcome, reached
  **without** the duplicate-webhook precondition (D9) the F11 deferral rests on.
- **Why it's still Low / not a blocker:** the promoter short-circuits already-Cloud uploads and
  reloads per job, so both promotions must load the row while it is Local — a seconds-wide window that
  the 6h sweep cadence rarely lands in (most likely during a recovery storm of many stuck-Local
  orders). No data loss (the original is re-written, just orphaned). Same severity class as F11/D27.
- **Disposition:** folds into the **same** concurrency-token / live-re-read fix as F11/D9 (bolt-035).
  The fixer's fresh-eyes note ("F1 sweep dedup … wasteful, not corrupting … milder relative of F11,
  left as-is") should be revised: via the F11 interleaving it *can* corrupt. Recommend the F11/D27
  ledger row record this second, D9-independent trigger. No new blocker.

## Build & tests (run by the re-reviewer)

- **.NET:** **701/701** passed, **10 skipped** (MinIO `[SkippableFact]`s, run in CI) — matches the
  resolution's claimed count. The first full run showed **1** failure (700/701); it passed on a clean
  rerun with no other change — the known flaky timing test (`ReliableEmailService` class, per v2), not
  a fix regression.
- **Frontend:** **438/438** effective. The full-parallel run showed 3 failures (435/438) in
  `app.spec`, `delivery-step.spec`, `format-selector-page.spec` — **none** bolt-043 files; all three
  pass in isolation (42/42), i.e. load-timeout flakes under full concurrency (the documented FE
  flake). Every bolt-043 spec passed in its isolated run (order-detail 19/19, photo-lightbox 6/6,
  photo-thumbnail 4/4).
- *Green ≠ proven:* all 14 fixed findings were shown non-vacuous by a red-able test above, so the
  green suite is load-bearing for this fix set. What it still cannot reach: the Postgres migration arm
  (F15/D20), and the NF1/F11 concurrency race (no test exercises two concurrent promotions).

## What this pass could NOT see (still owed before feature closure)

A verification pass is anchored to the fix diff. Still owed (README *Two loops*): the **four lenses v1
skipped** beyond the delta (db-parity, observability, input-validation, whole-feature requirements),
and the **certification pair** (two parallel blinded full-manifest passes on a frozen commit) — the
only instrument that may emit `approved` and the only one that catches original-population defects
outside the fix surface. This pass surfaced NF1, so the fix→verify→delta loop is **not** yet quiet.
