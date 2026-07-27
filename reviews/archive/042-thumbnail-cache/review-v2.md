---
type: code-review
target: 042-thumbnail-cache
version: 2
supersedes: review-v1.md
branch: feat/bolt-042-thumbnail-cache
commit: 095285c
base: cf78fb4
reviewed: 2026-07-14
reviewer: Claude (anchored verification — revert-check + parallel cluster verifiers)
pass-type: verification
verdict: approve-with-followups
blockers: []
answers_resolution: resolution-v1.md
verified: [SEC-1, BUG-1, TEST-1, BUG-2, BUG-3, REQ-1, OBS-1, FE-1, FE-2, TEST-2, TEST-3, BUG-4, QUAL-1, QUAL-2, OBS-2, OBS-3, FE-3, FE-4, REQ-2, REQ-3, REQ-4, DB-1, INPUT-1, TEST-4, QUAL-3, QUAL-4, QUAL-5]
reopened: []
deferred: [CLOUD-1]
new: [NEW-1, NEW-2, NEW-3, NEW-4]
---

# Review v2 — Bolt 042: Thumbnail Cache (verification of resolution-v1)

Anchored **verification** pass over the fix delta `cf78fb4..095285c` (26 findings fixed +
1 deferred in [resolution-v1.md](resolution-v1.md)). This pass answers one question per
finding — *did the claimed fix hold?* — it does **not** re-audit the whole feature, so it
cannot certify the feature clean (that needs a saturated discovery pass; see
[README](../README.md) *Two loops*). Verdict is therefore **approve-with-followups**, not
`approved`.

## TL;DR

**All 26 fixed findings VERIFIED. 0 reopened. 1 deferral (CLOUD-1) re-affirmed.** Both
suites green: **.NET 511/511**, **frontend 402/402**. The three v1 blockers (SEC-1, BUG-1,
TEST-1) are closed and proven non-vacuous.

Four **new, non-blocking** follow-ups surfaced (fix-generativity + one pre-existing rough
edge the fixes now make worth handling):

- 🟠 **NEW-1** — the 50 MP decode cap rejects some legitimately large uploads (owner tuning decision).
- 🟡 **NEW-2** — a restored upload is dropped on *any* non-401 preview error (transient blip loses work).
- 🟡 **NEW-3** — latent cleanup-vs-preview race can orphan one thumbnail (sub-second window).
- 🟡 **NEW-4** — stored paths use OS separators (`thumbs\…` on Windows) — a cross-platform/cloud-port hazard.

One v2 finding (an FE-2 test that didn't exercise re-init) was **found and fixed in this
pass** (`095285c`), so it is not carried as open.

## Method

- **Adversarial non-vacuity (revert → red), main agent.** For the highest-value findings,
  the fix was reverted and the regression test re-run to confirm it goes red (the README's
  cheap mutation test). Then restored. Results below.
- **Parallel isolated cluster verifiers (×4).** Four independent agents (image-decode,
  thumbnail-lifecycle, observability/migration/input, frontend), each anchored to its
  findings + the resolution notes + the fix commits, confirmed each fix is present and
  correct, that its test genuinely covers the defect, and hunted for regressions the fix
  introduced. None reopened a finding; their fix-generativity hits are the NEW-* items.
- **Full suites re-run:** `.NET 511/511`, `frontend 402/402`.

### Decisive revert → red evidence (fixes are non-vacuous)

| Finding | Reverted to | Test | Result |
|---------|-------------|------|--------|
| BUG-1 | per-axis `>25000` | `ImageProcessorTests.GenerateThumbnailAsync_OversizedImage_ThrowsDecompressionBomb` (9000×6000) | **RED** ✓ |
| BUG-2 | drop thumbnail delete | `UploadCleanupJobTests.Cleanup_deletes_cached_thumbnail_file_alongside_original` | **RED** ✓ |
| BUG-3 / TEST-3 | drop miss-branch `SaveChanges` | `UploadServiceTests.GetPreviewAsync_SecondRequestFreshContext_UsesPersistedThumbnail` | **RED** ✓ |
| SEC-1 | `public, …, immutable` | `UploadControllerIntegrationTests.GetPreview_CacheControl_IsPrivateNotPublic` | **RED** ✓ |
| INPUT-1 | no brand check | `MimeValidatorTests.DetectMimeType_NonHeifIsoBmffContainer_ReturnsNull` (×3 brands) | **RED** ✓ |
| TEST-1 / FE-3 | unconditional logout+navigate | `error.interceptor.spec` guest + anon 401 specs | **RED** ✓ (2) |

## Verified findings

All findings below were confirmed present, correct, and covered. Blockers first.

- **SEC-1 ✓** — preview `Cache-Control` is `private, max-age=2592000` (no `public`/`immutable`);
  `private` also keeps `ResponseCaching` (registered before auth) from storing it. Directive pinned by test.
- **BUG-1 ✓** — total-pixel area cap (`ExceedsDecodeLimits`, `long` multiply) at both decode sites +
  `MaxFrames=1`; `DecompressionBombException` correctly does **not** get swallowed by the
  `catch (ImageFormatException)` (distinct hierarchy). Real-processor test rejects a genuine 54 MP image. *(See NEW-1 on the cap value.)*
- **TEST-1 ✓** — guest + anon 401 interceptor branches covered and mutation-sensitive.
- **BUG-2 ✓** — cleanup deletes `ThumbnailPath` (own try/catch, counted); both branches tested.
- **BUG-3 / REQ-2 ✓** — deterministic `thumbs/{ownerId}/{uploadId:N}.jpg`; provably cannot collide with the original;
  racing/cancelled writes overwrite the same key.
- **REQ-1 ✓** — `MemoryAllocator` 512 MB cap set once at startup.
- **OBS-1 ✓** — batch rejections logged (`uploads.batch.item_rejected`); catches the new bomb 422 too.
- **FE-1 ✓** — `ensureGuestSession` dedup via `shareReplay(1)` + upstream `finalize`-reset is correct (no stale/sticky replay, no null-race).
- **FE-2 ✓** — retry bounded to exactly once (`isRetry` guard), only on 401, batch/per-item handled correctly; test strengthened this pass to exercise the actual re-init.
- **TEST-2 ✓** — real `ImageProcessor` exercised; oversized test non-vacuous.
- **TEST-3 ✓** — SUT driven through a context separate from seed/assert; a missing `SaveChanges` fails it.
- **BUG-4 ✓** — `ImageFormatException` → 422, propagating the bomb exception past the catch.
- **QUAL-1 ✓** — `AsNoTracking` on hit; miss `Attach`es + marks only `ThumbnailPath` (no cross-column wipe).
- **QUAL-2 ✓** — generated stream returned rewound; ETag length consistent across miss and later hit; no double-read/dispose.
- **OBS-2 ✓** — client-abort at Information (`request.client_aborted`), scope preserved.
- **OBS-3 ✓** — `DecompressionBombException` maps to 422 via its own exact-type key; reserved event carries dimensions.
- **FE-3 ✓** — anon/no-token 401 no longer dead-ends at login.
- **FE-4 ✓** — restore preview distinguishes 401 (retry once) from 404 (drop); concurrent previews share one init.
- **REQ-3 ✓** — story 002 AC amended to the implemented 404; spec/code no longer contradict.
- **REQ-4 ✓** — bundled change B/C scope documented with a retroactive AC, backed by the TEST-1/FE-* specs.
- **DB-1 ✓** — migration provider-aware (`varchar(512)` on Npgsql), matching the sibling + runtime model; in-place edit safe.
- **INPUT-1 ✓** — HEIF brand verified; MP4/MOV/M4A rejected up front; legitimate HEIC brands still pass; slice in-bounds.
- **TEST-4 ✓** — Cache-Control, 304/If-None-Match, deterministic key, and ensureGuestSession dedup all covered.
- **QUAL-3 / QUAL-4 / QUAL-5 ✓** — shared helper+const; named TTL; intentional-duplication note.
- **CLOUD-1 — deferral re-affirmed** — still latent until bolt-043; QUAL-2 already removes one per-miss round-trip ahead of it.

## New follow-ups (non-blocking)

### 🟠 NEW-1 — 50 MP decode cap rejects legitimate large-format / high-MP uploads
`src/PhotoPrint.API/Services/ImageProcessor.cs:20`
BUG-1's area cap (50 MP) is correct as DoS defence, but it is a **behavior regression** for a
photo-*printing* product: the old per-axis check accepted anything ≤ 25000 per side (up to
625 MP), so a 6000×9000 (54 MP) upload — an A1-ish poster at 300 DPI, or output from a
50–108 MP phone/camera — was accepted and is now **422-rejected at upload**. The 512 MB
`MemoryAllocator` cap already hard-bounds a decode at ~128 MP, so there is headroom to raise
the pixel cap toward that without weakening the bomb defence (625 MP / 2.5 GB stay rejected).
**This is an owner tuning/product decision** — confirm the cap covers the largest supported
print resolution, then raise it (or gate large-format products on a higher limit). Not
blocking; the current value is within the v1 review's endorsed "tens of MP" range.

### 🟡 NEW-2 — a restored upload is discarded on *any* non-401 preview error
`src/PhotoPrint.UI/.../format-selector-page.ts` (`fetchPreviewWithRetry`)
FE-4 correctly distinguishes 401 (retry) from 404 (drop), but every other error (transient
500, `status 0` network blip) still drops the entry **and** rewrites `sessionStorage`,
permanently erasing a completed upload from the grid. Pre-existing (the old code dropped on
any error), but now that retry infrastructure exists, a transient failure should not lose
work. **Fix:** only drop on a definitive 404; leave transient failures retryable/visible.

### 🟡 NEW-3 — latent cleanup-vs-preview race can orphan one thumbnail
`src/PhotoPrint.API/BackgroundJobs/UploadCleanupJob.cs` + `Services/UploadService.cs`
Cleanup loads candidates (with `ThumbnailPath == null`) then soft-deletes them. If a preview
generates+persists a thumbnail for one of those uploads **between** the query and the
per-item soft-delete, cleanup's stale in-memory copy skips the thumbnail delete (and its
`SaveChanges` writes only `DeletedAt`), leaving a soft-deleted row that still references an
on-disk thumbnail never revisited (both paths filter `DeletedAt == null`). Sub-second window,
one small file — inherent to the soft-delete + non-transactional-file model. **Fix (future):**
re-read/patch `ThumbnailPath` at delete time, or a periodic orphan sweep.

### 🟡 NEW-4 — stored paths use OS separators (cross-platform / cloud-port hazard)
`src/PhotoPrint.API/Services/LocalStorageService.cs`
`Path.Combine` produces `thumbs\{owner}\{id}.jpg` on Windows and `thumbs/…` on Linux. It's
self-consistent within one OS (write/read/delete all use `Path.Combine`), and pre-existing
(the original file path does the same), but a path persisted on a Windows dev box then read on
Linux — or fed as a cloud object key (bolt-043) — would break. **Fold into CLOUD-1 / bolt-043:**
normalise stored keys to forward slashes at the storage boundary.

## Recommendation

**Approve with follow-ups.** All v1 findings are verified and both blockers are closed with
non-vacuous tests; the branch is safe to merge on the v1 findings. Address **NEW-1** (owner
decides the cap) before or shortly after merge given it can reject real orders; **NEW-2/3/4**
are fast-follows. Per the two-loops rule this verification cannot mark the feature `approved`
— closing the feature still wants a saturated **discovery** pass.
