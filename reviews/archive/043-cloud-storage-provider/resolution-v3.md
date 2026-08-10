---
type: resolution
target: 043-cloud-storage-provider
version: 3
answers: review-v3.md
status: resolved
fixed_commit: 972a8b4
opened: 2026-07-14
closed: 2026-07-20
tests: { dotnet: "701/701 (+10 skipped MinIO, run in CI)", frontend: "438/438" }
findings:
  F1:  { status: fixed, commit: 2f49a8d, note: "PromotionRecoveryScanner -> BackgroundService (boot sweep + PeriodicTimer) mirroring the purge sibling; new sized+validated OrderPhotoArchive:PromotionRecoverySweepIntervalHours=6h. New surface: the periodic self-heal (fails-fast on <=0 interval; skips on archive-off/cloud-off; enqueue-all, no batch cap since enqueue is cheap and the worker's MaxConcurrentOrders bounds work). Periodicity verified by inspection like F4; ExecuteAsync boot-sweep test added so it doesn't reintroduce F3." }
  F2:  { status: fixed, commit: "4674dcd,0fc577a", note: "Skip unroutable Cloud rows (StorageLocation=Cloud + cloud disabled) so For(Cloud) can't throw outside the per-upload try and wedge the deterministic batch; per-sweep count warning. New surface: skipped rows are NOT soft-deleted (retried when cloud returns). Revert-verified (batch throws without the guard). Same class as F9 + the customer-preview sweep 0fc577a (UploadService.GetPreviewAsync)." }
  F3:  { status: fixed, commit: fea2490, note: "Added ExecuteAsync boot-sweep test to OriginalPurgeRecoveryScannerTests; reverting the boot-sweep line reddens it (timeout) — the coverage regression F4 introduced is closed." }
  F4:  { status: fixed, commit: c30d734, note: "Wrap the production-complete purge in try/catch (best-effort, recovery-sweep-backstopped) mirroring the F17 cancel sibling — a purge throw no longer 500s the PATCH after commit+email+SignalR. Class-sweep of F17. Revert-verified." }
  F5:  { status: fixed, commit: c30d734, note: "Added throwing-purger cancel test for the existing F17 try/catch; reverting that try/catch reddens it. Test-only (the guard already shipped in v1)." }
  F6:  { status: fixed, commit: c4ec6ca, note: "Split photos fetch-failure (retryable error + button) from a genuine empty 200 — no more permanent 'no longer available' on a transient failure. New surface: photosError signal + retryPhotos(). PARTIAL: the genuine-empty copy still can't distinguish not-yet-promoted vs cloud-off vs purged without an API signal — that four-way backend signal is deferred (see decisions)." }
  F7:  { status: fixed, commit: "a5cb0be,c4ec6ca,972a8b4", note: "Lightbox emits (imgError) + shows a fallback (a5cb0be); order-detail re-fetches once for fresh presigned URLs and re-points the lightbox (c4ec6ca); grid-thumbnail class-sweep adds (error) refresh to the tiles too (972a8b4). Fixes the >1h-TTL broken-image on both surfaces. Stale 'lazy per-open URL' comment corrected." }
  F8:  { status: fixed, commit: f048dc1, note: "Memoize the object URL per File, revoke on File change / ngOnDestroy — no more per-CD blob-URL leak. Pre-existing upload-flow issue, not a 043 regression. Spec asserts single createObjectURL + revoke-on-destroy." }
  F9:  { status: fixed, commit: "c30d734,0fc577a", note: "StreamZipAsync fails before writing any response byte when a Cloud original is unroutable (cloud disabled) — no more truncated ZIP from a mid-stream For(Cloud) throw. Same class as F2 + the customer-preview sweep 0fc577a. Revert-verified (partial body written without the pre-flight)." }
  F10: { status: deferred, commit: null, note: "Failed-promotion cross-tier cloud litter reclaim needs an orphan-reclaim design (best-effort cloud delete keyed off the deterministic key scheme regardless of the row's tier). Deferred — see decisions." }
  F11: { status: deferred, commit: null, note: "Dup-promotion-vs-purge orphan race shares D9's root cause (no Upload/Order concurrency token; duplicate webhooks). Deferred to bolt-035 with D9 — see decisions." }
  F12: { status: fixed, commit: 66a5f64, note: "Added validator test for Archive:PurgeSweepIntervalHours<=0 (+ the new OrderPhotoArchive:PromotionRecoverySweepIntervalHours rule from F1). Non-vacuous by construction (drop the rule -> Validate succeeds -> test fails)." }
  F13: { status: fixed, commit: 66a5f64, note: "BackfillCommand filter-parity boundary tests: Cancelled/PaymentFailed/AwaitingPayment excluded (Never promoted), Shipped/Delivered included (Once). Drift that re-promotes an excluded status now reddens." }
  F14: { status: fixed, commit: 66a5f64, note: "Added the TOCTOU re-resolve-to-Local 200 test (first open throws, re-resolve open succeeds -> FileStreamResult). Coverage of a correct-today branch (plausible finding); no live bug." }
  F15: { status: deferred, commit: null, note: "Postgres FilePath-nullable migration parity — the recurring db-parity/DB-1 deferral (bolt-035/042). Plausible, not a live defect (the migration IS correct on Postgres today). Deferred to the 3-env/Testcontainers track — see decisions." }
  F16: { status: fixed, commit: c4ec6ca, note: "getOrderDetail no longer redirects on any error: 403/404 redirect (definitive), 401 left to the auth interceptor, transient 5xx/network -> inline orderError + retryOrder(). The Medium strand was refuted by the review; this fixes the Low no-retry residual." }
  F17: { status: fixed, commit: a5cb0be, note: "Lightbox: role=dialog + aria-modal + aria-label, focus-into-dialog on open, Tab trap on the close button, focus restore to trigger on close. New surface: effect-based focus mgmt (no CDK available). Spec covers ARIA + focus move/restore." }
  F18: { status: deferred, commit: null, note: "Latent, not triggerable today (no order-detail -> order-detail navigation exists; every entry recreates the component). Deferred — see decisions." }
---

# Resolution v3 — 043-cloud-storage-provider

Fixer responses to [review-v3.md](review-v3.md) (delta-discovery, 18 findings: 8 Medium, 10 Low,
0 blockers). Per-finding detail in [findings-v3.md](findings-v3.md); canonical `D#` in
[ledger.md](ledger.md). The review file is immutable — status lives here; `verified` is set only by
the v4 re-review.

## Status table

| F# | D# | Sev | Status | Commit | How / why |
|----|----|-----|--------|--------|-----------|
| F1 | D19 | 🟠 | **fixed** | 2f49a8d | Promotion recovery boot-only → periodic BackgroundService (class-sweep of F4) + interval setting + ExecuteAsync test |
| F2 | D24 | 🟠 | **fixed** | 4674dcd | Skip unroutable Cloud rows so `For(Cloud)` can't wedge the cleanup batch (revert-verified) |
| F3 | D21 | 🟠 | **fixed** | fea2490 | ExecuteAsync boot-sweep test (revert-verified) |
| F4 | D23 | 🟠 | **fixed** | c30d734 | Wrap production-complete purge in try/catch (class-sweep of F17, revert-verified) |
| F5 | D22 | 🟠 | **fixed** | c30d734 | Throwing-purger cancel test (revert-verified vs F17 guard) |
| F6 | D13 | 🟠 | **fixed** | c4ec6ca | Error-vs-empty + retry (four-way empty signal deferred) |
| F7 | D5b | 🟠 | **fixed** | a5cb0be, c4ec6ca | Lightbox (imgError) + parent refresh of stale URL |
| F8 | D31 | 🟠 | **fixed** | f048dc1 | Memoize object URL + revoke on destroy |
| F9 | D25 | 🟡 | **fixed** | c30d734 | Fail ZIP before writing body when a Cloud original is unroutable (revert-verified) |
| F10 | D26 | 🟡 | **deferred** | | Failed-promotion cross-tier litter reclaim (orphan-reclaim design) |
| F11 | D27 | 🟡 | **deferred** | | Dup-promotion orphan race → bolt-035 (with D9) |
| F12 | D28 | 🟡 | **fixed** | 66a5f64 | Validator tests for PurgeSweepIntervalHours + PromotionRecoverySweepIntervalHours |
| F13 | D30 | 🟡 | **fixed** | 66a5f64 | BackfillCommand exclusion + inclusion boundary tests |
| F14 | D29 | 🟡 | **fixed** | 66a5f64 | TOCTOU re-resolve-to-Local 200 branch test |
| F15 | D20 | 🟡 | **deferred** | | Postgres migration parity → 3-env/Testcontainers |
| F16 | D32 | 🟡 | **fixed** | c4ec6ca | 403/404 redirect · 401→interceptor · transient→inline error+retry |
| F17 | D33 | 🟡 | **fixed** | a5cb0be | Lightbox dialog a11y + focus trap/restore |
| F18 | D34 | 🟡 | **deferred** | | Latent (no detail→detail nav today) — see decisions |

## Decisions / rationale

**14 fixed · 4 deferred · 0 wont-fix · 0 disputed.** No blockers (the review had none). Every
behavioral fix ships with a regression test proven non-vacuous (revert reddens it); coverage-only
findings (F3/F5/F12/F13/F14) add the missing red-able test. The four deferrals:

- **F10 / D26 — failed-promotion cross-tier cloud litter: deferred.** When a promotion writes its 3
  cloud objects then fails the row-flip `SaveChanges`, the row stays `Local` with null preview paths,
  so cleanup (which routes by `StorageLocation`) never reclaims the orphaned cloud blobs. A proper fix
  is an **orphan-reclaim design** — a best-effort cloud delete keyed off the deterministic key scheme
  regardless of the row's recorded tier — which is the same orphan-sweep class deferred on bolt-042
  (D31/M1). Not a patch; wants its own design pass. No data loss today (the local original + row
  remain; only cloud bytes leak, and only after a persistent mid-promotion failure).

- **F11 / D27 — dup-promotion re-creates a just-purged original: deferred → bolt-035 (with D9).** The
  race requires **duplicate concurrent promotions**, whose precondition is the duplicate-webhook
  `Order.Status` race already tracked as **D9** (deferred to bolt-035: no `Order`/`Upload` concurrency
  token / event-dedup). The clean fix (re-read live `StorageLocation`/`FilePath` before the flip, or an
  EF concurrency token on `Upload`) is the same concurrency-token work as D9 and belongs with it, not
  bolted onto this storage round. **Design-check escalation** (fixer rule #3): a concurrency-model
  change, not a patch.

- **F15 / D20 — Postgres migration parity: deferred → 3-env/Testcontainers.** The `FilePath`
  NOT-NULL-drop migration is verified only on SQLite; the Postgres arm is unproven by the suite. A
  skeptic confirmed the migration **is correct on Postgres today** (it's a coverage gap, not a live
  defect), and this is the recurring **db-parity / DB-1** deferral carried across bolt-035/042 — it
  lands with the 3-env Testcontainers work, not here.

- **F18 / D34 — ngOnInit-only load staleness: deferred (latent).** Not triggerable today: every entry
  to `/comenzile-mele/:id` comes from the list route, which recreates the component and re-runs
  `ngOnInit`; there is no order-detail → order-detail navigation. It becomes real only if such a link
  is added. Recorded as a latent trap; the fix (react to the `orderId` route input via `effect()`)
  folds into whatever change introduces that navigation.

**F6 partial (recorded, not deferred):** the fix distinguishes a photos fetch *failure* (retry) from a
genuine empty 200, but the empty copy still can't tell **not-yet-promoted** vs **cloud-off** vs
**post-retention purge** apart — that needs a backend signal on `GET /orders/{id}/photos` (a small API
change). Left as a follow-up for the frontend-ux/API owner; the core D13 defect (transient error shown
as permanent "gone", no retry) is fixed.

**F7 note:** the refresh-on-`imgError` recovers an expired presigned URL by re-fetching the whole
photo list; a per-photo presign-refresh endpoint would be lighter but is not worth a new endpoint now.

All fixes keep to system-boundary mocking (no real component mocked out to prove green). Verification
still owed: the v4 re-review flips these `fixed` rows to `verified` (or reopens) — this fixer does not
self-verify.

### Fresh-eyes micro-review (fixer rule #4, before hand-back)

Two anchored agents reviewed the full fix diff (backend + frontend) with the class/instance ·
new-surface · regression questions. They caught **two surviving siblings of my own fixes**, both
fixed before hand-back:

- **`UploadService.GetPreviewAsync` (customer preview) — F2/F9 class-sweep** (`0fc577a`): had the
  same unguarded `For(Cloud)`-when-cloud-disabled defect and would 500 the customer preview. Guarded
  → clean 404 + ops signal, with a regression test. (Higher-value than the admin ZIP it siblings.)
- **Order-detail grid thumbnails — F7/D5b class-sweep** (`972a8b4`): the F7 fix covered only the
  lightbox; the grid thumbnails share the presigned TTL and had no `(error)` handler. Unified the
  refresh (`refreshPhotoUrls`) and added `(error)` to the grid img, with a test.

Everything else came back clean: all other `For(Cloud)`/`.Cloud` sites are guarded; all three purge
call sites now have try/catch or `SafeSweepAsync`; `S3BucketVerifier` is correctly boot-only; the F1
periodic scanner is sized/validated/observable; no regressions (the `BackgroundService` ordering is
channel-decoupled and safe, the cleanup return-count change is log-only, the F9 throw precedes any
response write). Verdicts noted for the re-review:

- **F1 sweep dedup (accepted):** the periodic promotion sweep enqueues the whole stuck set with no
  dedup against in-flight jobs, so an order mid-promotion could be processed twice — **wasteful, not
  corrupting** (the promoter is idempotent / Confirmed-Write-Then-Delete), and unlikely given the
  ~1.5h retry envelope vs the 6h cadence. Milder relative of the deferred F11/D27; left as-is.
- **New sibling for the re-reviewer (NOT fixed — out of 043 scope):** `cart-page.ts` stores
  `URL.createObjectURL` blob URLs in a map and never revokes them (no `ngOnDestroy`) — the same
  leak class as F8 but in the cart feature, outside this review's surface. Flagged here per the
  "note, don't silently fix outside the finding set" rule.
