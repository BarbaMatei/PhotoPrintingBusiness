---
type: review
target: 043-cloud-storage-provider
version: 3
supersedes: 2
commit: 2be8ab8
code-tip: 1e7b9d3
delta-base: 5706580
branch: feat/bolt-043-cloud-storage-provider
pass-type: delta-discovery
date: 2026-07-14
reviewer: delta-discovery (6 backend lenses [delta] + frontend-ux [full-surface skipped-lens debt] → dedup → convergence-weighted verify)
verdict: approve-with-followups
blockers: []
findings: { high: 0, medium: 8, low: 10, cleanup: 0, refuted: 1 }
tests: { dotnet: "686/686 (+10 skipped MinIO, run in CI)", frontend: "423/423" }
---

# Review v3 — 043-cloud-storage-provider (delta-discovery pass)

The **first delta-discovery pass** for this target, per the README's middle tier. It is **blinded**
(lenses barred from `reviews/`) like a full discovery pass, but scoped to the **diff since the last
full pass** rather than the whole feature:

- **Backend delta** — `passType: delta`, 6 lenses (correctness · race · security · requirements ·
  tests-coverage · completeness-critic) over the cumulative diff `5706580..HEAD` (the entire v1→v2
  fix round: 11 source + 12 test files, ~1,900-line diff) + collaborators.
- **Frontend-ux** — `passType: full`, 1 lens over the **full** preview/lightbox/order-photos surface.
  The frontend delta is empty (no frontend file changed in the fix round), so per the ledger's
  explicit exception this lens runs as the **owed skipped-lens debt** the lean v1 pass left — a
  full-surface review, not a delta.

**Verdict: `approve-with-followups`. No blockers, 0 High.** A delta pass is capped at
`approve-with-followups` by design — it audits only the fix diff, so a quiet delta *gates to*
certification and never certifies (README *Two loops* / *Severity scale*). This pass was **not
quiet**: it surfaced **18 findings (8 Medium, 10 Low)** + 1 refuted, so the fix→verify→delta loop
continues.

## Headline: this fix round was fix-generative

The delta tier exists because "the population of new defects lives almost entirely in the fix diff"
(README, from the 042 data). This pass is a textbook confirmation — **almost every backend finding
traces to a v1→v2 fix**, not to the original feature:

| Fix-generativity class (README fixer rules #1/#2) | This pass |
|---|---|
| **Class-sweep miss (#1)** — fixed the instance, not the class | F1/D19 (F4 made the *purge* recovery scanner periodic but left its *promotion* sibling boot-only) · F4/D23 (F17 wrapped the *cancel* purge in try/catch but not the *production-complete* sibling) |
| **New-mechanism, new bug (#2)** — the fix introduced a fault | F2/D24 + F9/D25 (routing reads through `IStorageRouter.For()` now throw `InvalidOperationException` when the cloud tier is disabled but Cloud-located rows exist) |
| **New-mechanism, no test (#2)** — mechanism shipped untested | F3/D21 (periodic-sweep conversion left the tests calling `RunSweepAsync` by reflection — `ExecuteAsync` is now a coverage *regression*) · F5/D22 (cancel try/catch untested) · F12/D28 (new `PurgeSweepIntervalHours` validator untested) · F14/D29 (F8's new re-resolve 200 branch untested) · F13/D30 (F18's BackfillCommand test never crosses the exclusion boundary) |
| **Fix widened a race** | F11/D27 (F4+F17's new/more-frequent purge triggers widen a duplicate-promotion-vs-purge window into an unreclaimable cloud orphan) |
| **Routing residual** | F10/D26 (F2 routes cleanup by `StorageLocation`, so a *failed* promotion's cross-tier litter is never reclaimed) |

The **frontend-ux** findings are the opposite: they are **first-time discovery** of a surface the
lean v1 pass never reviewed (the owed debt), not fix-generated — D13 (confirmed + expanded) and D5b
(confirmed) were the two items v1 deferred *to* this lens, plus four genuinely new ones.

## Findings (F# ranked; `D#` = canonical ledger identity)

Convergence `Conv` = independent lenses that raised it. Verdicts from the workflow's
convergence-weighted skeptics; I dropped 1 refuted with a reason and downgraded F16 (its Medium
"strand" scenario was refuted; the Low residual stands).

### 🟠 Medium (8)

| F# | D# | Conv | Verdict | File | Finding |
|----|----|------|---------|------|---------|
| F1 | **D19** ⟳ | 2 | confirmed | `PromotionRecoveryScanner.cs` | Promotion recovery left **boot-only** (`IHostedService`) while F4 made its purge sibling **periodic** — a paid order whose promotion goes terminal-Local is never re-enqueued until reboot, so its original never reaches the durable cloud tier (ADR-008 intent silently unmet; local copy still serves, so not user-visible loss). *The v2 carry-forward, now confirmed with a trace; class-sweep miss of F4.* |
| F2 | D24 | 1 | confirmed | `UploadCleanupJob.cs:92` | `router.For(Cloud)` (added by F2) is **outside** the per-upload try/catch and throws `InvalidOperationException` when cloud is disabled but a Cloud row is in the batch → the deterministic hourly batch **wedges all cleanup** (incl. local orphans) indefinitely. |
| F3 | D21 | 2 | confirmed | `OriginalPurgeRecoveryScanner.cs:60` | F4 converted the scanner to `BackgroundService` and the tests were rewritten to call `RunSweepAsync` **by reflection** — no test now enters `ExecuteAsync` (both `ExecuteAsync` tests short-circuit on `enabled=false`). Delete the boot sweep or break the `PeriodicTimer` loop and the suite stays green. **A coverage regression the fix introduced.** |
| F4 | D23 | 1 | confirmed | `AdminOrderService.cs:135` | The **production-complete** purge (`UpdateStatusAsync`) has **no try/catch**, while F17 added one to the **cancel** purge sibling. An uncaught purge throw (transient DB load / client-disconnect `ct`) returns a 500 to the admin PATCH *after* Shipped already committed + emailed + SignalR'd. Class-sweep miss of F17. |
| F5 | D22 | 1 | confirmed | `AdminOrderService.cs:235` | F17's cancel-purge try/catch is **untested** — the only cancel test stubs the purger to return `Empty`. Remove the guard and a throwing purger 500s after refund+email+SignalR, with no red test. |
| F6 | **D13** ⟳ | 1 | confirmed | `order-detail-page.ts` | Order-photos empty-state **conflates** transient-error / 500 / 401-expired / not-yet-promoted / purged into one permanent-sounding "nu mai sunt disponibile" with **no retry** — and on a 500 it shows that *alongside* the interceptor's "server error" toast (contradictory). *v1 D13 (Low, "not-yet-promoted only") re-found and expanded by the owed lens.* |
| F7 | **D5b** ⟳ | 1 (hinted) | confirmed | `order-detail-page.ts` | The lightbox `largeUrl` is a presigned 1h-TTL URL **captured at list-fetch** and reused verbatim on open; after the TTL it 403s and the `<img>` has **no `(error)` handler / refresh / fallback** → broken image. *v1 D5 part-b, deferred to this lens, now confirmed.* |
| F8 | D31 | 1 | confirmed | `photo-thumbnail.component.ts:86` | `localUrl()` calls `URL.createObjectURL(file)` **inside a template-evaluated method** with no memoization → on OnPush the upload thumbnail mints a fresh **unrevoked blob URL every change-detection cycle** (each progress event), leaking + flickering. *Pre-existing upload-flow issue, caught by the full-surface pass; not a 043 regression.* |

### 🟡 Low (10)

| F# | D# | Conv | Verdict | File | Finding |
|----|----|------|---------|------|---------|
| F9 | D25 | 1 | confirmed | `AdminOrderService.cs:171` | Sibling of F2: `StreamZipAsync`'s `For(Cloud)` throws **mid-stream** (after ZIP headers sent) when cloud is disabled → truncated admin ZIP, no clean 500. |
| F10 | D26 | 2 | confirmed | `UploadCleanupJob.cs:92` | F2 routes cleanup solely by the row's `StorageLocation`, so a **failed** promotion (cloud blobs written, row-flip `SaveChanges` failed → row stays Local, preview paths null) leaves 3 cloud objects that neither purge nor cleanup can ever reclaim. |
| F11 | D27 | 1 | confirmed | `OrderPhotoPromoter.cs:168` | F4+F17's new/more-frequent purge triggers widen a race: duplicate concurrent promotions (P1 flips→Cloud, purge deletes+nulls `FilePath`, P2 re-writes the object but never re-sets `FilePath`) → an **unreclaimable cloud orphan** (PII past retention). Precondition (dup webhooks) overlaps D9. |
| F12 | D28 | 1 | confirmed | `ArchiveSettings.cs:86` | The new `PurgeSweepIntervalHours <= 0` validator branch has **no test**; drop it and `PurgeSweepIntervalHours=0` boots, then `new PeriodicTimer(TimeSpan.Zero)` throws at runtime (host crash, not fail-fast). |
| F13 | D30 | 1 | confirmed | `BackfillCommandTests.cs:40` | F18's BackfillCommand test *claims* to guard filter-drift but only seeds included statuses (Paid/Printing) — never Cancelled/PaymentFailed (exclusion) or Shipped/Delivered (inclusion), so drift that re-promotes cancelled/refunded photos ships green. |
| F14 | D29 | 1 | plausible | `UploadsController.cs:200` | F8's re-resolve-to-Local **success (200)** branch is untested (both TOCTOU tests make the local read always throw). Coverage gap, not a live bug — no failing trace constructible today. |
| F15 | D20 | 1 | plausible (hinted) | `UploadThumbnailPathMigrationTests.cs:48` | F7's migration test asserts nullable `FilePath` on **SQLite** only; the **Postgres** NOT-NULL drop is unverified (InMemory purger tests allow null regardless). Trace confirms the migration *is* correct on Postgres today → coverage gap. **The recurring db-parity/DB-1 deferral (→ Testcontainers/3-env).** |
| F16 | D32 | 1 | plausible (Medium refuted) | `order-detail-page.ts:357` | `getOrderDetail`'s blanket `catchError` redirect. The skeptic **refuted** the "strand a logged-out user" Medium (the authGuard on `/comenzile-mele` sends them to login anyway); residual Low = a transient network blip bounces a still-authenticated user off the page with **no retry**. |
| F17 | D33 | 1 | confirmed | `photo-lightbox.component.ts` | Lightbox modal has **no `role=dialog`/`aria-modal`, no focus trap, no focus restore** — keyboard/SR users Tab through the page behind the backdrop; focus isn't returned to the trigger on close. *(The accessibility/UX lens the manifest calls for on frontend changes.)* |
| F18 | D34 | 1 | plausible (latent) | `order-detail-page.ts:357` | Order + photos load only in `ngOnInit`, but `orderId` is a route-bound input with `withComponentInputBinding`; if a future detail→detail link is added, the reused component would show stale data. **Not triggerable today** (no such nav exists) — a latent trap. |

### Refuted (recorded, no `D#`)

- **Worker drain vs long in-flight S3 upload** (completeness-critic, Low) — claimed F6's
  `WhenAll(inFlight)` drain could be defeated if a multipart upload exceeds the host shutdown
  timeout. **Refuted:** `OrderPhotoPromoter` is Confirmed-Write-Then-Delete (row flips to Cloud only
  *after* all cloud writes succeed), the same-key PUT is idempotent, and the recovery scan re-runs
  it — so a force-terminated mid-write leaves the row Local and is harmless/re-doable regardless of
  the timeout. Do not re-raise.

## Convergence & what the skeptics bought

- **Backend:** 17 raw findings across 6 lenses → **13 canonical** (max convergence 2 — F1/D19 by
  requirements+completeness, F3/D21 by tests+completeness, F10/D26 by requirements+completeness).
  21 skeptic runs (8 guard + 13 trace) vs a flat 26. **1 refuted** (the drain claim), **1 Medium
  downgraded** on the frontend (F16). 0 decided-re-raises (the dedup matched none of D9/D10/D12/R1).
- **Frontend:** 6 raw → 6 canonical (single lens, all conv 1). 11 skeptic runs. The trace skeptics
  earned their keep here: they **refuted** F16's Medium strand and **confirmed** F6/F7/F8/F17 with
  concrete traces, and correctly marked F18 latent (not triggerable).

## Build & tests (run by the reviewer)

- **.NET:** `686/686` passed, **10 skipped** (MinIO `[SkippableFact]`s, run in CI). +1 over v2 (the
  in-pass cancel-purge test at `1e7b9d3`).
- **Frontend:** `423/423` passed (46 files, vitest+jsdom). Unchanged — no frontend file was touched.
- *Green ≠ proven:* five of this pass's findings (F3, F5, F12, F13, F14) are exactly "the suite is
  green but this fix-round behavior has no red-able test" — the highest-value delta class.

## What this pass structurally could NOT see

A delta pass audits only the fix diff + the frontend surface; it does **not** re-audit the original
feature, and v1 was **lean**. Still owed before feature closure (README *Two loops*):

- The **four other lenses v1 skipped** — **db-parity, observability, input-validation,
  requirements-of-the-whole-feature** — beyond the slices that touched the fix diff. (frontend-ux is
  now paid via this pass.) F15/D20 is a symptom of the missing db-parity lens.
- The **saturating certification pair** — two parallel blinded full-manifest passes on one frozen
  commit — which is the only instrument that can emit `approved` and the only one that catches
  original-population defects outside the fix surface.

## Next

**Not quiet → the loop continues.** Recommended order: fix the Mediums (F1–F8) under the four fixer
rules — F1/D19 and F4/D23 especially need the **class-sweep** (#1) they were the victims of, and the
`For(Cloud)`-throws class (F2/D24 + F9/D25) wants a single guarded-resolve fix — then **verify**,
then **another delta**. When a delta finally comes back quiet, freeze and run the **certification
pair**. F15/D20 (Postgres parity) folds into the 3-env/Testcontainers track; F11/D27's dup-webhook
precondition folds into D9 → bolt-035.

Full per-finding detail (scenario · evidence · fix · verdict) in
[findings-v3.md](findings-v3.md); canonical identities and cross-pass mapping in [ledger.md](ledger.md).
