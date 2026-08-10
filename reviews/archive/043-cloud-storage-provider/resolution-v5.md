---
type: resolution
target: 043-cloud-storage-provider
version: 5
answers: review-v5.md
status: resolved
fixed_commit: 2d02b13
opened: 2026-07-22
closed: 2026-07-22
tests: { dotnet: "702/702 (+10 skipped MinIO, run in CI)", frontend: "439/439" }
findings:
  F1:  { status: fixed, commit: 2d02b13, note: "closeLightbox() now clears lightboxPhotoId as well as lightboxSrc, and refreshPhotoUrls re-reads lightboxPhotoId inside the subscribe (resolve time) instead of capturing it before the fetch — a close before OR during the async refresh can no longer re-point/re-open the modal. Patch, not a new mechanism (fixes a state-reset hole in the existing F7 refresh flow), so no rule-3 approach-check. Regression test open->close->grid(error) asserts lightboxSrc stays null; revert-verified (reddens when the id-clear is removed)." }
  F2:  { status: fixed, commit: 036ba05, note: "Moved the unroutable-Cloud exclusion from the post-fetch loop-skip into the candidate query WHERE (cloudEnabled || StorageLocation != Cloud) so the OrderBy/Take window advances to routable rows; preserved the F2 ops signal with a cloud-off-only CountAsync. New surface: a shared Expression<Func<Upload,bool>> retentionExpired applied to both queries (translatable; count gated to cloud-disabled so zero hot-path cost when cloud is on). Patch (query filter + diagnostic count, no new mechanism), no rule-3 check. Regression test seeds BatchSize unroutable Cloud rows ahead of one aged local orphan, asserts the orphan is still soft-deleted; revert-verified (deleted=0 under the pre-fix skip)." }
  F3:  { status: deferred, commit: null, note: "Cluster A (periodic promotion-recovery model) → bolt-035. Untested/untestable periodicity needs a TimeSpan/TimeProvider seam; folded into the one design item, not patched piecemeal — see decisions." }
  F4:  { status: deferred, commit: null, note: "Cluster A → bolt-035. Dedup-less periodic sweep = D9-independent trigger of the D27 orphan race; belongs with the Order/Upload concurrency-token work (D9/D27) — see decisions. (Was D35/NF1 from v4.)" }
  F5:  { status: deferred, commit: null, note: "Cluster A → bolt-035. Perpetual re-enqueue of permanently-terminal promotions needs a give-up marker (attempt count / PromotionAbandonedAt) excluded from the sweep query; part of the periodic-model redesign — see decisions." }
  F6:  { status: backlog, commit: null, note: "Low, standalone. F9's ZIP pre-flight throws an unmapped InvalidOperationException → generic 500. Map to a domain exception (409/422) + Warning log naming the cloud-off cause. Batched to backlog per the severity-based stop rule — see decisions." }
  F7:  { status: deferred, commit: null, note: "Cluster A → bolt-035. CloudEnabled fixed at boot (StorageRouter._cloud set once) → a runtime Provider flip needs a restart. Document the restart requirement or use IOptionsMonitor + re-evaluate per sweep; folded into the periodic-model design item — see decisions." }
  F8:  { status: backlog, commit: null, note: "Low (hinted). 401 on order fetch for a non-authenticated user leaves a blank body (interceptor guest branch clears token, no navigate; loadOrder 401 path sets neither error nor redirect). Surface a retryable error or redirect. Backlog." }
  F9:  { status: backlog, commit: null, note: "Low. Auto-heal shows 'Reîncarcă pagina' while silently re-fetching a fresh URL. Show a neutral reloading state first; reserve the reload copy for after the single refresh fails. Backlog (frontend-refresh polish cluster)." }
  F10: { status: backlog, commit: null, note: "Low (narrow). Lightbox failed() reset keyed on src !== lastSrc; an identical refreshed presigned URL leaves failed stuck. Reset failed() on every open/refresh assignment (or cache-bust). Backlog (frontend-refresh polish cluster)." }
  F11: { status: backlog, commit: null, note: "Low (coverage). Anti-refresh-loop guard (urlsRefreshed) untested — no spec dispatches a second (error) to assert no third getOrderPhotos. Add the two-errors-one-refetch spec. Backlog." }
  F12: { status: backlog, commit: null, note: "Low (coverage). Lightbox focus-trap (trapFocus Tab/Shift+Tab preventDefault + refocus) has no spec. Add a keydown.tab spec asserting defaultPrevented + focus stays trapped. Backlog." }
  F13: { status: backlog, commit: null, note: "Low (test quality). Renamed ExecuteAsync_ArchiveDisabled/_CloudTierOff guard tests seed an empty DB → VerifyNoOtherCalls passes for the wrong reason. Seed a stuck Paid+Local order so guard removal reddens. Backlog." }
  F14: { status: backlog, commit: null, note: "Cleanup. Order retries + ngOnInit subs have no in-flight dedup / takeUntilDestroyed / switchMap. Disable retry while loading; switchMap + takeUntilDestroyed. Backlog." }
---

# Resolution v5 — 043-cloud-storage-provider

Fixer responses to [review-v5.md](review-v5.md) (second delta-discovery pass, 14 findings: 3 Medium,
10 Low, 1 Cleanup; 0 High, 0 blockers). Per-finding detail in [findings-v5.md](findings-v5.md);
canonical `D#` in [ledger.md](ledger.md). The review file is immutable — status lives here;
`verified` is set only by the v6 re-review.

**Scope of this fix round (per the 2026-07-22 severity-based stop rule).** Both Mediums the review
recommended fixing now (D36 regression, D38 batch-starvation) are **fixed** with revert-verified
regression tests. The rest are triaged, not patched here:

- **Cluster A — the F1 periodic-promotion-sweep model (D35/D37/D46/D47) → deferred to bolt-035** as
  **one design item**, not four spot patches. This is fixer rule #3 applied at the review's
  recommendation: the periodic recovery model needs dedup (D35), a testable interval seam (D37), a
  terminal give-up marker (D46), and runtime-config awareness (D47), and its concurrency half
  converges on the `Order`/`Upload` concurrency-token work already carrying D9/D27 in bolt-035.
  Patching them piecemeal here would re-seed the exact fix-generativity the stop rule exists to end.
- **The remaining Lows + Cleanup (D39–D45, D48) → backlog.** Under the severity-based stop rule, new
  non-regression 🟡/⚪ findings do not re-arm the loop; they are drained by the backlog groomer or the
  next bolt touching that area, or re-judged by the certification pair. None is a regression or a High.

This makes the fix round **patch-grade** (no High fixed, no mechanism added/converted, no design
changed): per the router, closure is verification + this hand-back — it does **not** re-arm a delta.

## Status table

| F# | D# | Sev | Status | Commit | How / why |
|----|----|-----|--------|--------|-----------|
| F1 | D36 | 🟠 | **fixed** | 2d02b13 | Clear `lightboxPhotoId` on close + re-read at resolve time → a closed lightbox can't re-open on a thumbnail error (revert-verified) |
| F2 | D38 | 🟠 | **fixed** | 036ba05 | Exclude unroutable Cloud rows in the candidate-query WHERE so they can't starve the batch; cloud-off-only diagnostic count (revert-verified) |
| F3 | D37 | 🟠 | **deferred** | | Cluster A → bolt-035: periodic re-scan untested/untestable (needs a TimeSpan/TimeProvider seam) |
| F4 | D35 | 🟡 | **deferred** | | Cluster A → bolt-035: dedup-less sweep = D9-independent D27 orphan trigger (with D9/D27) |
| F5 | D46 | 🟡 | **deferred** | | Cluster A → bolt-035: perpetual re-enqueue of terminal promotions (needs a give-up marker) |
| F6 | D45 | 🟡 | **backlog** | | F9 ZIP pre-flight throws unmapped `InvalidOperationException` → 500; map to 409/422 + Warning |
| F7 | D47 | 🟡 | **deferred** | | Cluster A → bolt-035: `CloudEnabled` boot-fixed → runtime flip needs restart (doc or `IOptionsMonitor`) |
| F8 | D43 | 🟡 | **backlog** | | 401 for a non-authenticated user → blank order body (hinted) |
| F9 | D42 | 🟡 | **backlog** | | Auto-heal shows "reload" while silently recovering — show a neutral reloading state first |
| F10 | D48 | 🟡 | **backlog** | | Lightbox `failed()` reset keyed on src inequality; identical refreshed URL stays stuck |
| F11 | D40 | 🟡 | **backlog** | | `urlsRefreshed` anti-loop guard has no test |
| F12 | D41 | 🟡 | **backlog** | | Lightbox focus-trap has no spec |
| F13 | D39 | 🟡 | **backlog** | | Renamed F1 guard tests seed empty DB → pass for the wrong reason |
| F14 | D44 | ⚪ | **backlog** | | Order retries/inits have no in-flight dedup or `takeUntilDestroyed` |

## Decisions / rationale

### D36 (F1) — fixed
The v3 F7 presigned-URL refresh introduced the regression: `close()` cleared `lightboxSrc` but not
`lightboxPhotoId`, and `refreshPhotoUrls` re-pointed the lightbox from that stale id, re-opening a
closed modal on any later thumbnail `(error)`. Fixed the **class**, not just the reported instance:
`closeLightbox()` resets both fields, and `refreshPhotoUrls` re-reads `lightboxPhotoId` at resolve
time so a close *during* the async refresh is also covered (the reported scenario was only the
close-before-error case). Regression test reddens when the id-clear is reverted.

### D38 (F2) — fixed, and a correction to the v3 F2 note
During the v3 F2 fix I judged this edge out-of-scope ("requires >500 aged Cloud rows"). The blinded
lens showed the wedge is real once that population exists: the post-fetch skip left the same oldest
`BatchSize` Cloud rows re-filling the `OrderBy/Take` window every sweep, so local orphans sorted
after them were never reached. The correct fix is the query-level exclusion (what the review
recommended, and what I should have done in v3). The per-sweep "unroutable" ops warning is preserved
via a `CountAsync` gated to the cloud-disabled case, so the query filter doesn't silently drop the
signal (definition-of-done class 6).

### Cluster A (D35/D37/D46/D47) — deferred to bolt-035 as one design item
The review's own recommendation, and the right call under fixer rule #3: these four are the
symptoms of one under-built mechanism (the F1 boot→periodic promotion-sweep conversion), and the
concurrency half (D35) is the same root cause as the already-deferred D9/D27. Bolt-035 owns the
`Order`/`Upload` concurrency-token work; the periodic-recovery model (dedup + interval seam +
give-up marker + runtime-config) should get one adversarial design pass there, before code. Fixing
them here as four patches is precisely the fix-generativity the new stop rule is designed to stop.

### D39–D45, D48 — backlog
All are non-regression Lows (+ one Cleanup): a standalone unmapped-exception Low (D45), the
frontend-refresh UX/coverage polish cluster (D40/D41/D42/D48/D39/D44), and a hinted guest-401 Low
(D43). Under the severity-based stop rule none re-arms the loop; they are recorded in the ledger as
`backlog` for the groomer / next frontend bolt / the certification pair to pick up. None is a
regression or a High.

## Hand-back

`status: resolved` at `fixed_commit: 2d02b13`. 0 blockers, 0 open. The two recommended Mediums are
fixed with revert-verified regression tests; cluster A is deferred to bolt-035; the Low/Cleanup tail
is backlog. Suites: **.NET 702/702** (+10 skipped MinIO, run in CI) · **FE 439/439**.

Next step is a **verification** re-review against `2d02b13` → `review-v6.md`, which flips F1/D36 and
F2/D38 to `verified` (or reopens). This was a **patch-grade** fix round (no High, no new/converted
mechanism, no design change), so per the router it does **not** re-arm a delta pass — verification +
this hand-back is the exit, and with D36/D38 verified the loop is **quiet**. Feature closure then
needs the risk-tiered **certification pair** (storage is full-loop tier), which folds in the still-owed
full-manifest lenses (db-parity / observability / input-validation / whole-feature requirements) and
waits for explicit owner go-ahead.
