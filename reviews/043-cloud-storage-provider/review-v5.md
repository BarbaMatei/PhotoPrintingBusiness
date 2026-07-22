---
type: review
target: 043-cloud-storage-provider
version: 5
supersedes: 4
commit: 972a8b4
delta-base: 151abef
branch: feat/bolt-043-cloud-storage-provider
pass-type: delta-discovery
date: 2026-07-20
reviewer: delta-discovery (5 blinded lenses [delta] → dedup → convergence-weighted verify)
verifies: null
verdict: approve-with-followups
blockers: []
findings: { high: 0, medium: 3, low: 10, cleanup: 1, refuted: 0 }
tests: { dotnet: "701/701 (+10 skipped MinIO)", frontend: "438/438" }
---

# Review v5 — 043-cloud-storage-provider (delta-discovery pass)

The **second delta-discovery pass**, per the README's loop (fix → verify → **delta**, repeated until
a delta is quiet). Blinded (lenses barred from `reviews/`), scoped to the **v3 fix round**
(`151abef..972a8b4` — the 11 commits that fixed review-v3's findings), with the 9 terminal-status
ledger decisions passed as `decidedFindings`. Lenses: correctness · race · tests-coverage ·
frontend-ux · completeness-critic (5).

**Verdict: `approve-with-followups`. 0 High, 0 blockers.** A delta pass is capped there — it cannot
certify. This pass is **NOT quiet**: 19 raw → **14 canonical findings (3 Medium, 10 Low, 1 Cleanup)**,
0 refuted, max convergence **4**. The v4 verification confirmed the fixes *work*; this blinded pass
asked the different question — *what did the fix round break or introduce* — and the answer is a
real fix-generated population.

## Headline: the v3 fix round was fix-generative (again), in two clusters

Every one of the 14 findings traces to a v3 fix — the exact pattern the delta tier exists to catch
cheaply (README, from the 042 data). Two mechanisms dominate:

**Cluster A — the F1 periodic-promotion-sweep conversion** (a mechanism that shipped as a mini-feature
but under-built, README fixer rule #2). The blinded race lens independently re-found the NF1/D35 dedup
gap, and the pass added three more: the periodicity is untested, terminal orders are re-swept forever,
and a runtime provider-flip silently needs a restart. → **D35 (corroborated), D37, D46, D47.**

**Cluster B — my frontend F7 presigned-URL refresh** (`refreshPhotoUrls`). It introduced a
**convergence-4 Medium regression**: a *closed* lightbox spontaneously re-opens. → **D36, D40, D42, D48.**

Plus two standalone Mediums/Lows in the guard fixes: **D38** (the F2 cleanup-skip starves the batch —
the edge I dismissed as out-of-scope during fixing) and **D45** (F9's ZIP guard throws an unmapped
`InvalidOperationException` → 500).

## Findings (F# ranked; `D#` = canonical ledger identity)

### 🟠 Medium (3)

| F# | D# | Conv | Verdict | File | Finding |
|----|----|------|---------|------|---------|
| F1 | D36 | **4** | confirmed | `order-detail-page.ts` | **Regression (my F7 fix):** `close()` sets `lightboxSrc=null` but never clears `lightboxPhotoId`; a later grid/lightbox thumbnail `(error)` runs `refreshPhotoUrls`, which reads the stale `lightboxPhotoId`, finds the fresh photo, and calls `lightboxSrc.set(...)` → **the modal re-opens with no user action.** 4 lenses agreed. Fix: clear `lightboxPhotoId` on close (or gate the re-point on the lightbox being open). |
| F2 | D38 | 2 | confirmed | `UploadCleanupJob.cs` | **F2-fix edge:** the unroutable-Cloud skip is *post-fetch* — with ≥500 aged Cloud rows and cloud disabled, each hourly batch re-selects the same oldest 500 (skipped, never soft-deleted), so local orphans sorted after them are **never reached → cleanup wedges indefinitely**, not "left for a later sweep" (my comment was wrong). Fix: exclude unroutable rows in the query's `WHERE`/`OrderBy/Take` window, not after. |
| F3 | D37 | 1 | confirmed | `PromotionRecoveryScanner.cs` | **F1's marquee behavior (periodic re-scan) is untested + untestable:** the only `ExecuteAsync` test awaits the *boot* sweep; delete the `while (WaitForNextTickAsync)` loop and the suite stays green (exactly the F1 bug returns). The interval is whole-hours, so no fast periodic test is possible. Fix: an internal `TimeSpan` (or `Func`) seam so a ~20 ms test asserts a second (periodic) enqueue. |

### 🟡 Low (10)

| F# | D# | Conv | Verdict | File | Finding |
|----|----|------|---------|------|---------|
| F4 | **D35** ⟳ | 2 | confirmed | `PromotionRecoveryScanner.cs` | **Corroborates NF1 (v4):** the periodic sweep re-enqueues in-flight/queued orders (no dedup) → duplicate concurrent promotion. New consequence surfaced here: job B hits `GetStreamAsync(localKey)` after job A deleted the local original → a **spurious `promotion.upload.failed reason=local-original-missing` + wasted retry** for an order that promoted fine. Independently re-found blind → strengthens D35. |
| F5 | D46 | 1 | confirmed | `PromotionRecoveryScanner.cs` | **F1 fix-generated:** a permanently-terminal promotion (local original lost) is re-selected every interval → re-burns `MaxAttempts` + re-logs `promotion.failed.terminal` **forever, per stuck order** (boot-only never repeated this). Fix: a give-up marker (attempt count / `PromotionAbandonedAt`) excluded from the sweep query. |
| F6 | D45 | 1 | confirmed | `AdminOrderService.cs` | **F9 residual:** the ZIP pre-flight throws `InvalidOperationException`, which the exception middleware does not map → a generic **500 logged as "Unhandled exception"**; ops can't tell config-error from a crash. Fix: a mapped domain exception (409/422) + a Warning log naming the cloud-off cause. |
| F7 | D47 | 1 | confirmed | `PromotionRecoveryScanner.cs` | `Enabled`/`CloudEnabled` are read once at boot (`StorageRouter._cloud` fixed at construction); a runtime `Provider=local→S3` flip **never triggers a sweep without a full restart** — contradicting the "retried when set back to S3" claim in the F2 cleanup comment. Fix: document the restart requirement, or `IOptionsMonitor` + re-evaluate per sweep. |
| F8 | D43 | 1 (hinted) | confirmed | `order-detail-page.ts` | A non-authenticated (guest/expired-guest-token) user opening an order URL 401s; the interceptor's guest branch only clears the token (no navigate), and `loadOrder`'s 401 path sets neither `orderError` nor a redirect → **blank order body, no retry, no redirect.** Fix: surface a retryable error (or redirect) for a 401 on a non-authenticated user. |
| F9 | D42 | 1 | confirmed | `photo-lightbox.component.ts` | **F7 UX:** on a stale-URL `(error)`, `failed=true` renders "Imaginea nu a putut fi încărcată… Reîncarcă pagina" **while** the parent silently re-fetches a fresh URL; on success the image appears — the user was told to reload for an error the app auto-recovered. Fix: show a neutral "reloading" state first; reserve the reload-copy for after the single refresh fails. |
| F10 | D48 | 1 | confirmed | `photo-lightbox.component.ts` | **F7/F17 edge:** `failed()` is reset on `src !== lastSrc`; if the refreshed presigned URL equals the failed one (deterministic presign / stable dev path), `src===lastSrc` so `failed` stays true and the `urlsRefreshed` guard blocks a retry → **error persists until a full page reload.** Fix: reset `failed()` on every open/refresh assignment (or cache-bust). |
| F11 | D40 | 1 | confirmed | `order-detail-page.ts` | **F7 coverage:** the anti-refresh-loop guard (`urlsRefreshed`) has no test — no spec dispatches a *second* `(error)` to assert `getOrderPhotos` isn't re-called; a regression resetting the guard per-error would loop unbounded and ship green. Fix: the two-errors-one-refetch spec. |
| F12 | D41 | 1 | confirmed | `photo-lightbox.component.ts` | **F17 coverage:** the focus-trap (`trapFocus`: Tab/Shift+Tab `preventDefault` + refocus) has no spec — the a11y test only checks open/close focus move. Drop `preventDefault` and Tab escapes the modal, no test reddens. Fix: a keydown.tab spec asserting `defaultPrevented` + focus stays trapped. |
| F13 | D39 | 1 | confirmed | `PromotionRecoveryScannerTests.cs` | **F1 test quality:** the renamed `ExecuteAsync_ArchiveDisabled/_CloudTierOff` guard tests seed an **empty DB**, so a removed guard enqueues nothing anyway → `VerifyNoOtherCalls()` passes for the wrong reason. Fix: seed one stuck Paid+Local order so guard removal reddens. |

### ⚪ Cleanup (1)

| F# | D# | Conv | Verdict | File | Finding |
|----|----|------|---------|------|---------|
| F14 | D44 | 1 | unverified-cleanup | `order-detail-page.ts` | Retry button + `ngOnInit` subscriptions have no in-flight dedup / `takeUntilDestroyed` / `switchMap` — rapid retries can last-arriving-wins, and a late response can set signals post-destroy. |

## Convergence & skeptics

19 raw across 5 lenses → **14 canonical** (max convergence **4** — F1/D36, found by correctness + race
+ tests-coverage + frontend-ux). 15 skeptic runs (3 guard + 12 trace); **0 refuted**, 0 disputed. 0
decided-re-raises (the dedup matched none of the 9 `decidedFindings` — the accepted deferrals did not
re-surface, and D35 was deliberately left out of `decidedFindings` so its re-find is *independent*
convergence, not a suppressed match). `hinted`: F8/D43 (guest-auth topic seeded by the shared hints).

## Build & tests

- **.NET 701/701** (+10 skipped MinIO, run in CI) · **FE 438/438** — unchanged since v4 (this pass
  reviewed the frozen `972a8b4` tree; no code changed).
- *Green ≠ proven, confirmed again:* F3/D37 (periodic sweep), F11/D40 (refresh-loop guard), F12/D41
  (focus-trap), F13/D39 (vacuous guard tests) are all "the suite is green but this fix-round behavior
  has no red-able test" — the highest-value delta class, and the recurring cost of the fix round.

## Assessment — the loop is not converging yet

This is the **second** consecutive fix-generative round (v3 delta found the v1→v2 round generative;
this pass finds the v3 round generative). Two fixes account for most of it:

- **F1 (periodic promotion sweep)** was a mechanism-adding fix (rule #2) that shipped under-built:
  no dedup (D35), untested periodicity (D37), no terminal give-up (D46), boot-fixed config (D47). This
  is a **design-check-escalation** candidate (README fixer rule #3) — the periodic recovery model
  wants one adversarial design pass before the next patch, not four more spot fixes. Much of it
  converges on the **same concurrency-token / live-re-read work already deferred to bolt-035** (D9/D27).
- **My frontend F7 refresh** introduced a Medium regression (D36) + three Low residuals (D40/D42/D48).
  A focused frontend fix cluster.

## Next

**Not quiet → the loop continues.** Recommended: (1) fix the Medium regression **D36** and the
**D38** batch-starvation now (both are self-contained and real); (2) treat the **F1 periodic-sweep
cluster (D35/D37/D46/D47)** as one design item, folded into the bolt-035 concurrency-token work rather
than patched piecemeal here; (3) batch the frontend-refresh Lows (D40/D42/D48) + coverage gaps
(D37/D39/D40/D41) into the fix round; then **verify → another delta.** Feature closure still needs the
owed full-manifest lenses (db-parity/observability/input-validation/whole-feature-requirements) + the
certification pair — but that is gated behind a *quiet* delta, which this is not.

Full per-finding detail in [findings-v5.md](findings-v5.md); canonical identities in [ledger.md](ledger.md).
