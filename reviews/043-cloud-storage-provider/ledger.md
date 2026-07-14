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

Status column reflects the cumulative outcome through **v3**: `verified` = fix held
(revert-and-rerun / construction / inspection, by v2); `wont-fix` / `deferred` = decision upheld;
`open` / `confirmed` = live finding from v3 awaiting a fix round.

| D# | First seen | Sev (v1) | Status (through v2) | Title |
|----|-----------|----------|--------|-------|
| D1 | v1 · F1 | 🔴 High | **verified** *(was blocker)* | Admin ZIP fulfilment download reads promoted originals from local tier only |
| D2 | v1 · F2 | 🟠 Med | **verified** | UploadCleanupJob deletes Cloud uploads against local tier; never deletes LargePreviewPath |
| D3 | v1 · F3 | 🟠 Med | **verified** | Cloud missing-original throws AmazonS3Exception not FileNotFoundException → preview 500 |
| D4 | v1 · F4 | 🟠 Med | **verified** | Purge on Shipped one-shot; skips in-flight promotion → original never purged until reboot |
| D5 | v1 · F5 | 🟠 Med | **verified** (part a); **D5b → confirmed v3** | Presigned-URL TTL vs hardcoded Cache-Control max-age divergence → expired/broken images |
| D5b | v1 · F5(b) | 🟠 Med | **confirmed** (v3 · F7) *(was deferred → frontend-ux)* | Lightbox `largeUrl` captured at list-fetch expires after 1h TTL; no refresh/`(error)` fallback → broken image |
| D6 | v1 · F6 | 🟠 Med | **verified** | Promotion worker disposes concurrency semaphore under in-flight tasks on shutdown |
| D7 | v1 · F7 | 🟠 Med | **verified** | Migration DDL (FilePath NOT-NULL drop) unverified by tests/CI |
| D8 | v1 · F8 | 🟡 Low | **verified** | Preview GET TOCTOU: promotion deletes local thumb between service read and stream-open → 500 |
| D9 | v1 · F9 | 🟡 Low | **deferred** → bolt-035 | Concurrent duplicate payment webhooks race Order.Status (no concurrency token) |
| D10 | v1 · F10 | 🟡 Low | **wont-fix** (403 convention) | 403-vs-404 order-existence oracle on /photos and /detail |
| D11 | v1 · F11 | 🟡 Low | **verified** | /photos returns presigned URLs without Cache-Control: private |
| D12 | v1 · F12 | 🟡 Low | **wont-fix** (owner: user-only) | Guest-placed orders unreachable from the new /photos endpoint |
| D13 | v1 · F13 | 🟡 Low → 🟠 | **confirmed + expanded** (v3 · F6) *(was deferred → frontend-ux)* | Empty-state copy conflates transient error / 401-expired / not-yet-promoted / purged into one permanent "no longer available"; no retry; contradictory toast on 500 |
| D14 | v1 · F14 | 🟡 Low | **verified** | Cloud preview regen branch never exercised (fake presets ThumbnailPath) |
| D15 | v1 · F15 | 🟡 Low | **verified** | GET /orders/{id}/photos has no integration test (auth pipeline untested) |
| D16 | v1 · F16 | 🟡 Low | **verified** | Promoter row-update-failure and preview-generation-failure branches untested |
| D17 | v1 · F17 | 🟡 Low | **verified** (owner: purge-on-cancel) | Paid-then-cancelled original-purge lifecycle |
| D18 | v1 · F18 | 🟡 Low | **verified** | BackfillCommand and S3BucketVerifier have zero tests |
| D19 | v2 · carry-fwd | 🟡 Low → 🟠 | **confirmed** (v3 · F1, conv 2) | PromotionRecoveryScanner still boot-only while F4 made its purge sibling periodic — terminal-Local paid orders never reach cloud until reboot (ADR-008 durability intent unmet; local copy still serves) |
| D20 | v3 · F15 | 🟡 Low | **deferred** → 3-env/Testcontainers | FilePath NOT-NULL drop migration verified only on SQLite, not Postgres (recurring db-parity / DB-1; *plausible*, not a live defect today) |
| D21 | v3 · F3 | 🟠 Med | **open** | OriginalPurgeRecoveryScanner sweep untested — F4's periodic conversion left tests calling RunSweepAsync by reflection, `ExecuteAsync` undriven (coverage **regression**) |
| D22 | v3 · F5 | 🟠 Med | **open** | Purge-on-cancel try/catch (F17) untested — throwing purger never exercised |
| D23 | v3 · F4 | 🟠 Med | **open** | Production-complete purge lacks the cancel path's try/catch (F17 **class-sweep miss**) → 500 after commit+email+SignalR |
| D24 | v3 · F2 | 🟠 Med | **open** | UploadCleanupJob `For(Cloud)` throws (uncaught) when cloud disabled → deterministic batch **wedges all cleanup** (fix-generated by F2 routing) |
| D25 | v3 · F9 | 🟡 Low | **open** | StreamZipAsync `For(Cloud)` throws mid-stream when cloud disabled → corrupt admin ZIP (**sibling class of D24**; F1 routing) |
| D26 | v3 · F10 | 🟡 Low | **open** | Cleanup routes by StorageLocation → a **failed** promotion's cross-tier cloud litter never reclaimed (F2 residual) |
| D27 | v3 · F11 | 🟡 Low | **open** | Duplicate concurrent promotion re-creates a just-purged cloud original as an unreclaimable orphan (F4/F17 **widened**; dup-webhook precond overlaps D9) |
| D28 | v3 · F12 | 🟡 Low | **open** | New `PurgeSweepIntervalHours<=0` validator (F4) untested → `=0` boots then `PeriodicTimer(0)` crashes host |
| D29 | v3 · F14 | 🟡 Low | **open** *(plausible / coverage)* | Preview TOCTOU re-resolve-to-Local **200** branch (F8) untested — no live bug |
| D30 | v3 · F13 | 🟡 Low | **open** | BackfillCommand filter-drift test (F18) never crosses the exclusion boundary (Cancelled/PaymentFailed unseeded) |
| D31 | v3 · F8 | 🟠 Med | **open** *(pre-existing, upload flow)* | photo-thumbnail `localUrl()` mints an unrevoked blob URL every CD cycle (leak/flicker) — **not a 043 regression** |
| D32 | v3 · F16 | 🟡 Low | **open** *(Medium strand refuted)* | getOrderDetail blanket catchError bounces on transient/5xx with no retry (the logged-out-**strand** Medium was refuted — authGuard covers it) |
| D33 | v3 · F17 | 🟡 Low | **open** | Lightbox modal lacks focus trap / `role=dialog` / `aria-modal` / focus restore (a11y — first-time frontend coverage) |
| D34 | v3 · F18 | 🟡 Low | **open** *(latent — not triggerable today)* | order-detail loads only in `ngOnInit` despite route-bound `orderId` input → stale on a future detail→detail reuse |

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
