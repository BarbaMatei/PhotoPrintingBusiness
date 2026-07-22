---
type: review-ledger
target: 043-cloud-storage-provider
updated: 2026-07-14
---

# Canonical finding ledger — 043-cloud-storage-provider

Stable `D#` identities for this target, per the README's persistent-ledger standard. Each real defect
gets a `D#` that lives forever; each pass's pass-local `F#` maps onto a `D#` **after** the blinded pass
completes (blinding is preserved during the search — finders never see `D#`).

**v1 is the first pass**, so `F#` ↔ `D#` is 1:1. **v3 (delta-discovery, 2026-07-14)** added D20–D34
and re-found three prior items (D19, D13, D5b). Those three re-finds are *not* independent-draw
overlap for a capture–recapture estimate: v1 and v3 ran against **different commits** (v3 is
post-fix), and all three were known carry-forward / deferred-to-this-lens items, so they were
*expected* to re-surface, not a saturation signal (per the README, overlap only estimates a
population across **parallel blinded passes on one frozen commit** — the still-owed certification pair).

Status column reflects the cumulative outcome through **v5 (delta-discovery)**: `verified` = fix held
(revert-and-rerun / construction / inspection); `wont-fix` / `deferred` = decision upheld;
`open` = live finding awaiting a fix round. v4 (independent verification) flipped D13/D5b/D19/D21–D34's
14 fixables to `verified` and upheld the 4 deferrals; **v5 (blinded delta over the v3 fix round)
added D36–D48 (13 new, all fix-generative) and corroborated D35** — so D35–D48 are the current open
population (0 High, 0 blockers; 3 Med + 9 Low + 1 Cleanup).

| D# | First seen | Sev (v1) | Status (through v2) | Title |
|----|-----------|----------|--------|-------|
| D1 | v1 · F1 | 🔴 High | **verified** *(was blocker)* | Admin ZIP fulfilment download reads promoted originals from local tier only |
| D2 | v1 · F2 | 🟠 Med | **verified** | UploadCleanupJob deletes Cloud uploads against local tier; never deletes LargePreviewPath |
| D3 | v1 · F3 | 🟠 Med | **verified** | Cloud missing-original throws AmazonS3Exception not FileNotFoundException → preview 500 |
| D4 | v1 · F4 | 🟠 Med | **verified** | Purge on Shipped one-shot; skips in-flight promotion → original never purged until reboot |
| D5 | v1 · F5 | 🟠 Med | **verified** (part a); **D5b → confirmed v3** | Presigned-URL TTL vs hardcoded Cache-Control max-age divergence → expired/broken images |
| D5b | v1 · F5(b) | 🟠 Med | **verified** (v4 · F7) | Lightbox `largeUrl` captured at list-fetch expires after 1h TTL; no refresh/`(error)` fallback → broken image |
| D6 | v1 · F6 | 🟠 Med | **verified** | Promotion worker disposes concurrency semaphore under in-flight tasks on shutdown |
| D7 | v1 · F7 | 🟠 Med | **verified** | Migration DDL (FilePath NOT-NULL drop) unverified by tests/CI |
| D8 | v1 · F8 | 🟡 Low | **verified** | Preview GET TOCTOU: promotion deletes local thumb between service read and stream-open → 500 |
| D9 | v1 · F9 | 🟡 Low | **deferred** → bolt-035 | Concurrent duplicate payment webhooks race Order.Status (no concurrency token) |
| D10 | v1 · F10 | 🟡 Low | **wont-fix** (403 convention) | 403-vs-404 order-existence oracle on /photos and /detail |
| D11 | v1 · F11 | 🟡 Low | **verified** | /photos returns presigned URLs without Cache-Control: private |
| D12 | v1 · F12 | 🟡 Low | **wont-fix** (owner: user-only) | Guest-placed orders unreachable from the new /photos endpoint |
| D13 | v1 · F13 | 🟡 Low → 🟠 | **verified** (v4 · F6) *(four-way empty signal → follow-up)* | Empty-state copy conflates transient error / 401-expired / not-yet-promoted / purged into one permanent "no longer available"; no retry; contradictory toast on 500 |
| D14 | v1 · F14 | 🟡 Low | **verified** | Cloud preview regen branch never exercised (fake presets ThumbnailPath) |
| D15 | v1 · F15 | 🟡 Low | **verified** | GET /orders/{id}/photos has no integration test (auth pipeline untested) |
| D16 | v1 · F16 | 🟡 Low | **verified** | Promoter row-update-failure and preview-generation-failure branches untested |
| D17 | v1 · F17 | 🟡 Low | **verified** (owner: purge-on-cancel) | Paid-then-cancelled original-purge lifecycle |
| D18 | v1 · F18 | 🟡 Low | **verified** | BackfillCommand and S3BucketVerifier have zero tests |
| D19 | v2 · carry-fwd | 🟡 Low → 🟠 | **verified** (v4 · F1) | PromotionRecoveryScanner still boot-only while F4 made its purge sibling periodic — terminal-Local paid orders never reach cloud until reboot (ADR-008 durability intent unmet; local copy still serves) |
| D20 | v3 · F15 | 🟡 Low | **deferred** → 3-env/Testcontainers *(upheld v4)* | FilePath NOT-NULL drop migration verified only on SQLite, not Postgres (recurring db-parity / DB-1; *plausible*, not a live defect today) |
| D21 | v3 · F3 | 🟠 Med | **verified** (v4 · F3) | OriginalPurgeRecoveryScanner sweep untested — F4's periodic conversion left tests calling RunSweepAsync by reflection, `ExecuteAsync` undriven (coverage **regression**) |
| D22 | v3 · F5 | 🟠 Med | **verified** (v4 · F5) | Purge-on-cancel try/catch (F17) untested — throwing purger never exercised |
| D23 | v3 · F4 | 🟠 Med | **verified** (v4 · F4) | Production-complete purge lacks the cancel path's try/catch (F17 **class-sweep miss**) → 500 after commit+email+SignalR |
| D24 | v3 · F2 | 🟠 Med | **verified** (v4 · F2) | UploadCleanupJob `For(Cloud)` throws (uncaught) when cloud disabled → deterministic batch **wedges all cleanup** (fix-generated by F2 routing) |
| D25 | v3 · F9 | 🟡 Low | **verified** (v4 · F9) | StreamZipAsync `For(Cloud)` throws mid-stream when cloud disabled → corrupt admin ZIP (**sibling class of D24**; F1 routing) |
| D26 | v3 · F10 | 🟡 Low | **deferred** (orphan-reclaim design) *(upheld v4)* | Cleanup routes by StorageLocation → a **failed** promotion's cross-tier cloud litter never reclaimed (F2 residual); F1's periodic sweep now self-heals the *transient*-failure case |
| D27 | v3 · F11 | 🟡 Low | **deferred** → bolt-035 *(upheld v4; rationale incomplete → [[D35]])* | Duplicate concurrent promotion re-creates a just-purged cloud original as an unreclaimable orphan. v4: the deferral rests on the dup-webhook (D9) precondition, but this round's **F1 periodic sweep** is a **second, D9-independent trigger** (D35) — the ledger carries both |
| D28 | v3 · F12 | 🟡 Low | **verified** (v4 · F12) | New `PurgeSweepIntervalHours<=0` validator (F4) untested → `=0` boots then `PeriodicTimer(0)` crashes host |
| D29 | v3 · F14 | 🟡 Low | **verified** (v4 · F14) *(coverage; no live bug)* | Preview TOCTOU re-resolve-to-Local **200** branch (F8) untested |
| D30 | v3 · F13 | 🟡 Low | **verified** (v4 · F13) | BackfillCommand filter-drift test (F18) never crosses the exclusion boundary (Cancelled/PaymentFailed unseeded) |
| D31 | v3 · F8 | 🟠 Med | **verified** (v4 · F8) *(pre-existing, upload flow)* | photo-thumbnail `localUrl()` mints an unrevoked blob URL every CD cycle (leak/flicker) — **not a 043 regression** |
| D32 | v3 · F16 | 🟡 Low | **verified** (v4 · F16) *(Medium strand refuted)* | getOrderDetail blanket catchError bounces on transient/5xx with no retry (the logged-out-**strand** Medium was refuted — authGuard covers it) |
| D33 | v3 · F17 | 🟡 Low | **verified** (v4 · F17) | Lightbox modal lacks focus trap / `role=dialog` / `aria-modal` / focus restore (a11y — first-time frontend coverage) |
| D34 | v3 · F18 | 🟡 Low | **deferred** (latent) *(upheld v4)* | order-detail loads only in `ngOnInit` despite route-bound `orderId` input → stale on a future detail→detail reuse |
| D35 | v4 · NF1 | 🟡 Low | **open** *(fix-generative; corroborated v5 · F4, conv 2)* | F1's periodic promotion sweep has **no in-flight/queued dedup** (worker uses a plain `List<Task>`, `MaxConcurrentOrders=4`; promoter never re-reads live state before its row-flip) → the sweep can spawn a second concurrent promotion of one order and hit the [[D27]] orphan race **without** duplicate webhooks. v5's blinded race lens independently re-found the dedup gap (added consequence: a spurious `promotion.upload.failed reason=local-original-missing` + wasted retry). Folds into the D27/D9 concurrency-token fix (bolt-035) |
| D36 | v5 · F1 | 🟠 Med | **open** *(regression — F7 fix, conv 4)* | Stale `lightboxPhotoId` (never cleared on close) makes `refreshPhotoUrls` **re-open a closed lightbox** when a grid/lightbox thumbnail's expired presigned URL fires `(error)`. 4 lenses converged |
| D37 | v5 · F3 | 🟠 Med | **open** *(F1 fix-generated)* | F1's periodic re-scan (its whole purpose) is **untested + untestable** — only the boot sweep is tested; delete the `PeriodicTimer` loop and the suite stays green; interval is whole-hours so no fast periodic test. Wants a `TimeSpan`/`TimeProvider` seam |
| D38 | v5 · F2 | 🟠 Med | **open** *(F2 fix edge)* | Unroutable-Cloud cleanup skip is **post-fetch**, so ≥500 aged Cloud rows re-fill the deterministic batch every sweep and **starve local-orphan cleanup indefinitely** — the "later sweep" comment is wrong. Needs a query-level `WHERE` filter |
| D39 | v5 · F13 | 🟡 Low | **open** *(F1 test quality)* | Renamed `ExecuteAsync_ArchiveDisabled/_CloudTierOff` guard tests seed an **empty DB** → guard removal enqueues nothing anyway → `VerifyNoOtherCalls()` passes for the wrong reason. Seed a stuck Paid+Local order |
| D40 | v5 · F11 | 🟡 Low | **open** *(F7 coverage)* | Anti-refresh-loop guard (`urlsRefreshed`) untested — no spec dispatches a *second* img `(error)` to assert no third `getOrderPhotos` fetch |
| D41 | v5 · F12 | 🟡 Low | **open** *(F17 coverage)* | Lightbox focus-trap (`trapFocus` Tab/Shift+Tab `preventDefault` + refocus) has no spec — drop `preventDefault` and Tab escapes the modal, no test reddens |
| D42 | v5 · F9 | 🟡 Low | **open** *(F7 UX)* | Auto-heal shows "Reîncarcă pagina" **while** silently re-fetching a fresh URL → the user is told to reload for an error the app then auto-recovers. Show a neutral reloading state first |
| D43 | v5 · F8 | 🟡 Low | **open** *(hinted)* | 401 on order fetch for a **non-authenticated** user (interceptor guest branch only clears the token, no navigate) → `loadOrder` sets neither error nor redirect → **blank order body**, no retry |
| D44 | v5 · F14 | ⚪ Cleanup | **open** | Order retries + `ngOnInit` subs have no in-flight dedup / `takeUntilDestroyed` / `switchMap` — last-arriving-wins on rapid retries; late response sets signals post-destroy |
| D45 | v5 · F6 | 🟡 Low | **open** *(F9 residual)* | F9's ZIP pre-flight throws `InvalidOperationException` — unmapped → generic **500 logged "Unhandled exception"**; ops can't tell config-error from a crash. Map to 409/422 + Warning log |
| D46 | v5 · F5 | 🟡 Low | **open** *(F1 fix-generated)* | Periodic sweep re-enqueues **permanently-terminal** promotions (lost local original) **forever** — re-burns `MaxAttempts` + re-logs terminal every interval (boot-only never repeated this). Needs a give-up marker |
| D47 | v5 · F7 | 🟡 Low | **open** *(F1/F2 claim)* | `CloudEnabled` fixed at boot (`StorageRouter._cloud` set once) → a runtime `Provider=local→S3` flip **needs a restart**; contradicts the "retried when set back to S3" claim. Document or `IOptionsMonitor` |
| D48 | v5 · F10 | 🟡 Low | **open** *(F7/F17 edge)* | Lightbox `failed()` reset keyed on `src !== lastSrc`; an identical refreshed presigned URL leaves `failed` stuck (+ `urlsRefreshed` blocks retry) → error until page reload |

## Refuted (recorded, no D# assigned)

| Was | Sev | Claim | Disposition |
|-----|-----|-------|-------------|
| v1 R1 | 🟡 Low | S3StorageService coverage hinges on the MinIO gate | **refuted** — CI runs MinIO on every PR/non-main push and sets the `STORAGE_TEST_*` vars, so the SkippableFacts execute. Do not re-raise; the residual `IsTransient`/multipart gap is tracked under D3/D16. |
| v3 RV3-1 | 🟡 Low | Worker drain (F6) defeated by a long in-flight S3 upload vs the host shutdown timeout | **refuted** — `OrderPhotoPromoter` is Confirmed-Write-Then-Delete (row flips Cloud only after all writes succeed), same-key PUT idempotent, recovery scan re-runs; a force-killed mid-write leaves the row Local = harmless/re-doable regardless of timeout. Do not re-raise. |

## Accepted decisions / deferrals (through v2)

- **D12 / F12 — guest order-history photos: wont-fix (owner ruling).** `/photos` stays `[Authorize]`
  user-only; guests out of scope for bolt-053. Forward path if revisited: DualAuth + guest branch.
- **D17 / F17 — paid-then-cancelled originals: fixed (owner ruling = purge on cancel).** Fast-path
  purge in `CancelOrderAsync` + `Cancelled` in the recovery-sweep status set.
- **D10 / F10 — 403-vs-404 oracle: wont-fix.** 403-for-non-owner is the codebase convention;
  negligible GUID-enumeration risk. Re-open only as a codebase-wide 404 standardisation.
- **D9 / F9 — duplicate-webhook Order.Status race: deferred → bolt-035** (needs an `Order`
  concurrency token / event-dedup — payment-idempotency remit; no data loss today).
- **D13 / F13 + D5b / F5(b) — frontend-ux debt PAID by v3.** The v3 delta pass ran the owed
  full-surface frontend-ux lens (deliberate exception to delta scope, per the prior note). Both
  deferred items were **confirmed** — D13 re-found *and expanded* (also swallows transient/401 errors
  with no retry → re-rated 🟠, now D13/F6) and D5b confirmed with a concrete trace (D5b/F7). They are
  no longer deferred: they are **open findings for the next fix round**, along with four new frontend
  items the full-surface pass surfaced (D31 blob-URL leak · D32 catchError no-retry · D33 lightbox
  a11y · D34 ngOnInit-only staleness).

The **wont-fix / deferred-elsewhere** decisions (D9→bolt-035, D10 403-convention, D12 guest-scope)
were passed to the v3 delta pass as `decidedFindings`; the dedup agent matched **none** of them
(0 re-raises) — none re-surfaced in the fix diff. They remain **upheld — last affirmed @ `1e7b9d3`**
(the code-tip v3 reviewed; HEAD `2be8ab8` adds only docs). D13/D5b are no longer deferred (paid by
v3, above). Per the README's verification-runbook deferral gate, the next verification pass re-judges
a decision by agent only if `git diff 1e7b9d3..HEAD -- <cited file(s)>` shows the cited code moved;
otherwise it records "unchanged, stands" with no agent.

## v3 delta-discovery provenance (2026-07-14)

Blinded delta pass over `5706580..HEAD` (the v1→v2 fix round). **Backend:** 6 lenses (correctness ·
race · security · requirements · tests-coverage · completeness-critic), `passType: delta`, 17 raw →
13 canonical, max convergence 2. **Frontend:** owed full-surface `frontend-ux` lens, 6 findings.
Verdict `approve-with-followups` (0 High, 0 blockers; a delta pass cannot certify). **Dominant
theme: fix-generativity** — nearly every backend finding traces to a v1→v2 fix (F4's periodic
conversion, F17's cancel try/catch, F1/F2's `IStorageRouter` routing). **Not quiet → fix → verify →
delta again.** See [review-v3.md](review-v3.md) + [findings-v3.md](findings-v3.md).

## v5 delta-discovery provenance (2026-07-20)

Blinded 5-lens delta (correctness · race · tests-coverage · frontend-ux · completeness-critic) over
the **v3 fix round** `151abef..972a8b4`, `passType: delta`, 9 terminal decisions passed as
`decidedFindings`. 19 raw → **14 canonical** (max convergence **4**), 0 refuted, 0 decided-re-raises.
Verdict `approve-with-followups` (0 High, 0 blockers). **Not quiet — the v3 fix round was itself
fix-generative**, in two clusters: (A) F1's periodic promotion sweep shipped under-built — D35
(corroborated), D37 (untested), D46 (perpetual terminal re-enqueue), D47 (boot-fixed config); (B) the
frontend F7 refresh introduced a conv-4 Medium regression D36 (reopens a closed lightbox) + Lows
D40/D42/D48. Plus D38 (F2 batch-starvation edge) and D45 (F9 unmapped 500). Recommend: fix D36 + D38
now; treat cluster A as one design item folded into bolt-035 (D9/D27); batch cluster B. See
[review-v5.md](review-v5.md) + [findings-v5.md](findings-v5.md). **Certification remains gated behind a
quiet delta — not yet reached.**

## v4 verification provenance (2026-07-20)

Independent verification of [resolution-v3.md](resolution-v3.md) @ `972a8b4` by a fresh reviewer
(not the fixer). **14 of 14 fixed findings verified** non-vacuously by revert-and-rerun (each
reverted, owning test reddened with clean attribution + 0 collateral, restored → green; tree clean
after every revert); F1's periodicity additionally by inspection (as F4 in v2). Both fresh-eyes
class-sweeps (`0fc577a` UploadService preview, `972a8b4` grid-thumbnail) revert-proven. **4 deferrals
upheld** (D20/D26/D34 sound; **D27 upheld but rationale incomplete** → **D35/NF1**) + the D13 four-way
empty-signal follow-up. **1 new Low: D35 (NF1)** — F1's dedup-less periodic sweep is a D9-independent
trigger of the D27 orphan race; the fixer's "wasteful not corrupting" note was under-weighted.
Verdict `approve-with-followups` (a verification pass cannot certify). Suites: .NET 701/701 (+10 CI
MinIO) · FE 438/438 (3 non-043 load-flakes, green isolated). **NF1 surfaced → loop not quiet; owed:
full-manifest discovery + certification pair.** See [review-v4.md](review-v4.md).
