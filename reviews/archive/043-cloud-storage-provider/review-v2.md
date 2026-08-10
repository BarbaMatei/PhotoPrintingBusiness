---
type: review
target: 043-cloud-storage-provider
version: 2
supersedes: 1
commit: 1e7b9d3
branch: feat/bolt-043-cloud-storage-provider
pass-type: verification
date: 2026-07-14
reviewer: verification (revert-and-rerun + 2 anchored agents)
verifies: resolution-v1.md
verdict: approve-with-followups
blockers: []
verified: [F1, F2, F3, F4, F5, F6, F7, F8, F11, F14, F15, F16, F17, F18]
upheld: [F9, F10, F12, F13]
reopened: []
tests: { dotnet: "685/685 (+10 skipped MinIO)", frontend: "423/423 (unchanged)" }
---

# Review v2 — 043-cloud-storage-provider (verification pass)

Anchored verification of [resolution-v1.md](resolution-v1.md) against `fixed_commit` `319d7b3`
(tip `1e7b9d3` after this pass's in-pass test tweak). This is a **verification** pass, not
discovery: it checks that the specific fixes hold and that the accepted deferrals/wont-fixes still
stand — it is *not* a fresh audit and cannot certify saturation (see the two-loops rule in
[README.md](README.md)).

**Verdict: `approve-with-followups`.** All **14** fixed findings verified; the **4** non-fixed
decisions (F9 deferred, F10/F12 wont-fix, F13 deferred, + F5 part-b deferred) upheld; **0 reopened**;
no fix-introduced regression. A verification pass cannot emit `approved` — feature closure still
requires the owed **full-manifest discovery pass** (the v1 pass was lean; 5 lenses skipped).

## How this was verified

1. **Revert-and-rerun (the non-vacuity proof).** Nine production reverts were applied as one batched
   mutation set and the full suite run once: **exactly the 10 owning fix-tests reddened, with clean
   attribution and zero collateral** (one unrelated `ReliableEmailService` test flickered — flaky,
   green on both clean runs). Restored → suite green again. This proves each behavioral fix is
   load-bearing:

   | Finding | Revert applied | Test(s) that reddened |
   |---|---|---|
   | F1 | route `.For(location)` → `.Local` | `StreamZipAsync_PromotedCloudOrder_ReadsOriginalsFromCloudTier` |
   | F2 | `router.For(location)` → `router.Local` | `Cleanup_agedCloudUpload_deletesAllThreeKeysFromCloudTier` |
   | F3 | disable the `AmazonS3Exception NotFound` catch | `GetStreamAsync_MissingObject_TranslatesS3NotFoundToFileNotFound` |
   | F5(a) | `max-age={ttl}` → hardcoded `3600` | `GetPreviewAsync_CloudUpload_MaxAgeTracksPresignTtl` |
   | F6 | `WhenAll(inFlight)` → `Task.CompletedTask` | `Shutdown_WithInFlightPromotion_DrainsBeforeDisposingSemaphore` |
   | F8 | disable the local-thumb `FileNotFoundException` catch | `…ReResolvesToCloud302` + `…LocalThumbGoneOnBothResolves_Returns404` |
   | F11 | remove the `Cache-Control` header | `GetOrderPhotos_OwnOrder_Returns200WithPrivateNoStore` |
   | F17 (fast-path) | gate `if (false && …)` | `CancelOrderAsync_TriggersOriginalPurge` |
   | F17 (sweep) | drop `Cancelled` from `OriginalPurgeSweepStatuses` | `RunSweep_CancelledStuckOrder_FiresPurger` |

2. **F4 — verified by inspection.** F4 changed *when* the purge sweep runs (boot-only → periodic),
   not the sweep logic, so it has no red-able unit test; the sweep logic that fires the purger for a
   stuck Cloud+FilePath order is guarded by `RunSweep_StuckOrderAtOrPastShipped_FiresPurger`. The
   periodic `BackgroundService` structure (boot sweep before the timer's first tick, `PeriodicTimer`
   every `PurgeSweepIntervalHours`, `Take(BatchSize)`, `Enabled`/`CloudEnabled` guards, per-sweep
   logs) was confirmed by inspection.

3. **Coverage findings (F7, F14, F15, F16, F18) — verified by construction.** Each new test asserts a
   discriminating post-condition (migration `FilePath.notNull==false`; cloud regen persisted +
   stored; `/photos` 401/403/404/guest-401; promoter `Failed` count + row-stays-`Local` via a
   throwing context; BackfillCommand exit codes; verifier throws for a missing bucket). The
   verifier + S3 round-trip `[SkippableFact]`s are the 3 of the 10 skips that execute in CI.

4. **Two anchored agents (fresh context):**
   - **Deferral/wont-fix rationale check** — confirmed all 5 code conditions still hold at HEAD and
     **upheld all 5** (F9, F10, F12, F13, F5b): no `Order` concurrency token / event-dedup exists
     (F9 genuinely needs a bolt-035-scope schema change, no data loss); 403-for-non-owner is the
     codebase convention (F10); `/photos` is `[Authorize]` user-only, guest→401 (F12); the empty
     state collapses four causes with no API signal (F13); the lightbox URL is minted at page load
     (F5b).
   - **Three-question pass on the fixer's own fresh-eyes fixes** (`957f61a`/`319d7b3`, which no agent
     had reviewed) — **no blocking issues**: the cancel-vs-Shipped guard asymmetry is intentional and
     correct, the try/catch scope is adequate (a real bug stays Error-visible; OCE-swallow is benign
     for an HTTP handler), `PurgeSweepIntervalHours` is consistently defined across class/validator/
     appsettings/doc, and gating on `CloudEnabled` regresses nothing.

## Findings — verification status

| ID | Sev | v1 status | v2 verdict | Method |
|----|-----|-----------|------------|--------|
| F1 | 🔴 High | fixed | **verified** | revert-and-rerun |
| F2 | 🟠 Med | fixed | **verified** | revert-and-rerun |
| F3 | 🟠 Med | fixed | **verified** | revert-and-rerun (+ CI MinIO fact) |
| F4 | 🟠 Med | fixed | **verified** | inspection (periodic structure) + sweep test |
| F5 | 🟠 Med | fixed | **verified** (part a); part b deferred | revert-and-rerun (a) |
| F6 | 🟠 Med | fixed | **verified** | revert-and-rerun |
| F7 | 🟠 Med | fixed | **verified** | construction (real migration chain) |
| F8 | 🟡 Low | fixed | **verified** | revert-and-rerun (×2) |
| F9 | 🟡 Low | deferred | **upheld** (→ bolt-035 remit) | anchored agent |
| F10 | 🟡 Low | wont-fix | **upheld** (403 convention) | anchored agent |
| F11 | 🟡 Low | fixed | **verified** | revert-and-rerun |
| F12 | 🟡 Low | wont-fix | **upheld** (owner: user-only) | anchored agent |
| F13 | 🟡 Low | deferred | **upheld** (→ frontend-ux) | anchored agent |
| F14 | 🟡 Low | fixed | **verified** | construction |
| F15 | 🟡 Low | fixed | **verified** | construction |
| F16 | 🟡 Low | fixed | **verified** | construction (throwing ctx) |
| F17 | 🟡 Low | fixed | **verified** | revert-and-rerun (×2) + hardened |
| F18 | 🟡 Low | fixed | **verified** | construction + CI MinIO facts |

## Carry-forward (for the owed discovery pass — NOT acted on here)

- **New (out of v1 set): `PromotionRecoveryScanner` is still boot-only** — same *class* as F4 in a
  different subsystem. A promotion that exhausts `MaxAttempts` at runtime stays `Local` until the
  next reboot / `backfill-archive` run. Lower severity than F4 (previews still serve from local).
  Candidate for the same periodic-sweep treatment; left for the discovery pass to weigh.
- **F5b / F13 → frontend-ux:** the lightbox re-fetch-on-open and the four-way empty-state signal are
  a frontend feature + a small API-contract change, owed to the frontend-ux lens the lean pass
  skipped. (Incidental: the `order-detail-page.ts:145-146` comment claiming lazy per-open URL loading
  is misleading — only the `<img>` byte fetch is lazy — fold into that work.)
- **F9 → bolt-035:** duplicate-webhook `Order.Status` race; fold the `Order` concurrency token /
  event-dedup into the payment-idempotency remit.
- **Owed before feature closure:** a **saturated full-manifest discovery pass** (db-parity,
  observability, input-validation, requirements, frontend-ux — the five the v1 lean pass skipped).

## Build & tests (run by the re-reviewer)

- **.NET:** `685/685` passed, **10 skipped** (MinIO `[SkippableFact]`s — 7 original + F3 missing-key +
  2 F18 bucket-verifier; all execute in CI). Up from the v1 baseline of 661.
- **Frontend:** `423/423` — **unchanged** (no frontend file was touched; F13/F5b deferred).
