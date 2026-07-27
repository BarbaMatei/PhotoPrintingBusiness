---
type: code-review
target: 042-thumbnail-cache
version: 3
supersedes: review-v2.md
branch: feat/bolt-042-thumbnail-cache
commit: f8b1325
base: 095285c
reviewed: 2026-07-14
reviewer: Claude (anchored verification — revert-check + independent verifier)
pass-type: verification
verdict: approve-with-followups
blockers: []
answers_resolution: resolution-v2.md
verified: [NEW-1, NEW-2, NEW-4]
reopened: []
deferred: [NEW-3, CLOUD-1]
new: []
---

# Review v3 — Bolt 042: Thumbnail Cache (verification of resolution-v2)

Anchored **verification** of the four new follow-ups review-v2 raised (fix delta
`095285c..f8b1325`). Scope: the NEW-1/2/4 fixes and the NEW-3 deferral only — not a
re-audit of the feature. Verdict **approve-with-followups** (a verification pass can't
certify the feature clean).

## TL;DR

**NEW-1, NEW-2, NEW-4 VERIFIED. NEW-3 deferral accepted (sound). 0 reopened.** Both suites
green: **.NET 515/515**, **frontend 403/403**. Two comment-drift regressions the follow-up
fixes introduced were **caught and fixed in-pass** (`f8b1325`), so nothing new is carried
open. One deployment-time capacity note recorded (below).

## Method

- **Revert → red (main agent):** each fixed finding's regression test goes red when its fix
  is reverted, then restored.
- **Independent verifier (1 isolated agent):** anchored to review-v2's new findings +
  resolution-v2; confirmed each fix present/correct/tested, ruled on the NEW-3 deferral, and
  hunted for regressions (it found the two comment drifts, now fixed).

### Revert → red evidence

| Finding | Reverted to | Test | Result |
|---------|-------------|------|--------|
| NEW-1 | 50 MP cap | `ImageProcessorTests.ExceedsDecodeLimits_AtCapAllowed_…` (100 MP boundary) | **RED** ✓ |
| NEW-4 | `Path.Combine` key | `LocalStorageServiceTests.SaveAsync_WithPrefix_ReturnsForwardSlashKey` | **RED** ✓ |
| NEW-2 | drop-on-any-error | `format-selector-page.spec … keeps a restored entry on a transient error` | **RED** ✓ (@e3a77d9) |

## Verified

- **NEW-1 ✓** — `MaxDecodePixels = 100_000_000`; `long`-multiply overflow guard intact;
  allocator unchanged at 512 MB (a 100 MP RGBA decode ≈ 400 MB fits). Boundary test moved to
  100 MP; oversized real-image test moved to 110 MP (still > cap); upload-site 900 MP guard
  test still rejects — none vacuous.
- **NEW-2 ✓** — `fetchPreviewWithRetry` drops only on 404; 401 re-inits + retries once (FE-4
  intact, still bounded); 5xx/network/still-401-after-retry keep the entry visible for a later
  refresh. New transient-error spec is non-vacuous; FE-4 401/404 specs still pass.
- **NEW-4 ✓** — all four filesystem ops route through `ToFullPath`, so a `/`-key round-trips
  on Windows and Linux; no other code emits/consumes an OS-separator key; real-service
  `LocalStorageServiceTests` + the 13 upload/preview integration tests pass.

## Deferral ruling

- **NEW-3 — deferral ACCEPTED (sound).** Independently confirmed a genuine non-transactional
  file+DB TOCTOU: a concurrent preview (separate `DbContext`) sets `ThumbnailPath` after
  cleanup's candidate read; cleanup's property-level `DeletedAt` update does **not** clobber
  the DB `ThumbnailPath` (so the outcome is exactly one orphaned file, not a wiped reference —
  and there's no concurrency token, so no `DbUpdateConcurrencyException`). A read-then-delete
  narrowing can't close it and isn't cleanly unit-testable; a periodic **orphan sweep** is the
  right home and also covers the bolt-043 cloud case. *Nuance (verifier):* a full close does
  exist — guard the preview's `ThumbnailPath` UPDATE on `DeletedAt IS NULL` and self-delete the
  just-written file on a 0-row result — but that's a hot-path concurrency protocol with its own
  test burden, so deferring to the sweep remains defensible. Re-affirm when bolt-043 lands.
- **CLOUD-1 — deferral re-affirmed** (unchanged; bolt-043).

## Fixed in-pass (fix-generativity caught by v3)

Both comment-only, corrected at `f8b1325`:
- `Program.cs` allocator comment cited the old 50 MP / 200 MB figure — updated to 100 MP /
  ~400 MB with a "raise the allocator if the cap goes higher" note.
- `fetchPreviewWithRetry` JSDoc said "a failed retry drops the entry" — after NEW-2 only a 404
  drops; corrected.

## Note (deployment-time, non-blocking)

NEW-1 doubles a single large preview's decode footprint (~200 MB → ~400 MB). `AllocationLimit`
is **per-allocation**, not global, so concurrent large cache-miss previews add real process
memory pressure rather than failing cleanly. When sizing the deployment, consider a **decode
concurrency limit** (and mapping `InvalidMemoryOperationException` → 413/422 so an allocator
trip isn't a raw 500 — that path is pre-existing and only fires above the 100 MP guard). This
belongs with the deploy/ops phase, not a bolt-042 fix.

## Recommendation

**Approve with follow-ups.** All v2 follow-ups are resolved (NEW-1/2/4 verified; NEW-3 deferral
sound). Combined with review-v2 (26 v1 findings verified), the branch's review findings are all
resolved or deferred with sound rationale. Per the two-loops rule, closing the **feature** still
wants a saturated **discovery** pass; the decode-concurrency item is a deploy-time consideration.
