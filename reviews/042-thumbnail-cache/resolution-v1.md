---
type: resolution
target: 042-thumbnail-cache
answers_review: review-v1.md
version: 1
branch: feat/bolt-042-thumbnail-cache
status: resolved
fixed_commit: fad7693
opened: 2026-07-13
closed: 2026-07-13
findings:
  SEC-1:  { status: fixed, commit: 9af3b87, note: "preview Cache-Control now `private, max-age=30d` (drops public + immutable); ResponseCaching no longer stores it. Named TTL const (QUAL-4). Integration test pins Private=true/Public=false/MaxAge=30d." }   # blocker
  BUG-1:  { status: fixed, commit: 533996c, note: "per-axis 25000 check replaced by a total-pixel area cap (ExceedsDecodeLimits, 50 MP, long multiply) at BOTH decode sites + DecoderOptions.MaxFrames=1. Real-ImageProcessor test rejects a 54 MP image before decode." }   # blocker
  TEST-1: { status: fixed, commit: 978620c, note: "error.interceptor.spec now covers the guest branches: guest token present -> clearGuestToken, no logout/nav; anon no-token -> no logout/nav. Existing 401 tests re-pointed at an authenticated user." }   # blocker
  BUG-2:  { status: fixed, commit: c245a1e, note: "UploadCleanupJob now deletes ThumbnailPath alongside FilePath (same try/catch, counted as file error). Tests: deletes both; upload w/o thumbnail deletes only original." }
  BUG-3:  { status: fixed, commit: c245a1e, note: "deterministic id-keyed thumbnail path in a distinct 'thumbs/' namespace (SaveAsync prefix param) -> a racing/cancelled write overwrites the same key instead of orphaning. No RowVersion (deterministic key makes the race benign; avoids the 500-on-conflict caveat noted below)." }
  BUG-4:  { status: fixed, commit: 533996c, note: "GenerateThumbnailAsync catches ImageFormatException (covers UnknownImageFormat + InvalidImageContent) -> UnprocessableEntityException (422) instead of a raw 500. ImageProcessorTests: unreadable file -> 422." }
  REQ-1:  { status: fixed, commit: 533996c, note: "Configuration.Default.MemoryAllocator allocation cap (512 MB) added in Program.cs (code); story 003 AC amended (eb8a6f8). MaxImageWidth/Height don't exist in ImageSharp 3.1.11 -> replaced by the area cap." }
  REQ-2:  { status: fixed, commit: c245a1e, note: "deterministic path thumbs/{ownerId}/{uploadId:N}.jpg (owner-scoped namespace, not the spec's thumbs/{id}.jpg to avoid colliding with the original). Story 002 AC updated to the as-built path (eb8a6f8)." }
  REQ-3:  { status: fixed, commit: eb8a6f8, note: "amended story 002's soft-delete edge case to 404 (implemented + tested behavior); the 'serve the thumbnail' variant was never built and 404 is defensible (cleanup is about to remove both files). Spec/code no longer contradict." }
  REQ-4:  { status: fixed, commit: eb8a6f8, note: "documented the bundled guest-auth self-heal (change B) + dev-warning (change C) scope in bolt.md with a retroactive AC, now backed by the TEST-1/FE-* specs. Full split into separate bolts left to the owner (see decisions)." }
  FE-1:   { status: fixed, commit: f55daae, note: "ensureGuestSession shares one in-flight init (shareReplay + finalize-reset) so concurrent callers don't mint duplicate sessions. Spec: two concurrent calls -> initAnonymousSession called once." }
  FE-2:   { status: fixed, commit: f55daae, note: "upload auto-retries exactly once after a 401 (interceptor clears the stale token, ensureGuestSession re-inits). Spec: 401-then-success -> upload called twice, ends 'done'; 500 -> no retry." }
  FE-3:   { status: fixed, commit: 978620c, note: "interceptor 401 handler restructured: authenticated -> logout+redirect; else -> clearGuestToken (no navigation). An anonymous/no-token client is no longer bounced to a login page it has no account for." }
  FE-4:   { status: fixed, commit: f55daae, note: "restoreFromSession distinguishes 401 (re-init + retry once) from 404 (drop). Specs: 401 keeps the entry after retry; 404 drops it without re-init." }
  OBS-1:  { status: fixed, commit: 21e66c8, note: "UploadsController injects ILogger; each swallowed batch rejection logs a Warning (uploads.batch.item_rejected: file, reason type, correlation id). Controller unit test verifies the log + 200." }
  OBS-2:  { status: fixed, commit: 26165a3, note: "client-abort log raised Debug -> Information as a distinct request.client_aborted event (Serilog floor is Information everywhere). Middleware test verifies the Information emit." }
  OBS-3:  { status: fixed, commit: 533996c, note: "distinct DecompressionBombException(w,h) mapped to 422; middleware emits uploads.decompression_bomb.rejected with dimensions + correlation id (mirrors the idempotency-event pattern). Middleware test verifies." }
  QUAL-1: { status: fixed, commit: c245a1e, note: "AsNoTracking() restored on the preview read (hot cache-hit path); the miss branch Attaches + marks only ThumbnailPath modified." }
  QUAL-2: { status: fixed, commit: c245a1e, note: "miss branch returns the just-generated MemoryStream directly (rewound) instead of disposing + re-reading from storage." }
  QUAL-3: { status: fixed, commit: 533996c, note: "single ImageProcessor.ExceedsDecodeLimits(w,h) helper + DimensionsExceededMessage const used at both decode sites." }
  QUAL-4: { status: fixed, commit: 9af3b87, note: "PreviewCacheControl const derived from TimeSpan.FromDays(30); no more inline 2592000 magic string." }
  QUAL-5: { status: fixed, commit: eb8a6f8, note: "brief 'intentional duplication' comment on the split-query provider branches (extraction not worth it, per the review's own guidance)." }
  DB-1:   { status: fixed, commit: bca68fa, note: "migration now provider-aware (varchar(512) on Npgsql, TEXT on SQLite), mirroring the sibling AddOrderIdempotencyKey; safe in-place edit (no Postgres has applied it). The Migrate()-based DDL smoke test is deferred to the 3-env phase per the roadmap (see decisions)." }
  INPUT-1:{ status: fixed, commit: f850f69, note: "HEIF brand at bytes 8-11 now verified against a HEIF-brand set; generic ISO-BMFF containers (MP4/MOV/M4A) rejected up front. HEIC is still advertised (fails cleanly at decode until a HEIF decoder lands) — see decisions." }
  TEST-2: { status: fixed, commit: 533996c, note: "new ImageProcessorTests exercises the REAL processor: oversized -> DecompressionBomb, small valid -> <=300px JPEG, unreadable -> 422, GetInfo dimensions/null, ExceedsDecodeLimits boundary+overflow." }
  TEST-3: { status: fixed, commit: c245a1e, note: "UploadServiceTests drives the SUT through a context SEPARATE from the seed/assert context (same in-memory db name); a fresh-context persistence test proves SaveChanges ran, not a shared tracker." }
  TEST-4: { status: fixed, commit: fad7693, note: "added: Cache-Control directive (9af3b87), deterministic-key/TOCTOU proxy (c245a1e), ensureGuestSession dedup (f55daae), 304/If-None-Match (fad7693). Migration-DDL smoke test deferred with DB-1." }
  CLOUD-1:{ status: deferred, commit: null, note: "Latent until bolt-043 cloud storage provider; not triggerable today. Design constraint for 043, not a v1 fix. QUAL-2 (returning the in-memory stream on a miss) already removes one per-miss storage round-trip ahead of the cloud port." }
---

# Resolution — Bolt 042: Thumbnail Cache (answers review-v1)

Fixer's response to [review-v1.md](review-v1.md). One row per finding; the reviewer's
file stays immutable. All 27 open findings are now at a terminal status (**26 fixed, 1
deferred**), all 3 blockers addressed, and both suites are green:

- **.NET:** `dotnet test` → **510 passed / 0 failed** (was 490 at v1; +20 tests).
- **Frontend:** `ng test` (vitest/jsdom) → **402 passed / 0 failed** (46 files).

Fixed blocker-first, one focused commit per finding/group referencing its ID. Two fixes
each closed a cluster: the shared `ExceedsDecodeLimits` helper + `DecompressionBombException`
closed **BUG-1/QUAL-3/OBS-3** at both decode sites, and the deterministic `thumbs/` key +
cleanup-deletes-thumbnail closed **BUG-2/BUG-3/REQ-2** in one stroke.

Next step is a **verification re-review** against `fixed_commit` (`fad7693`) → `review-v2`,
which flips surviving findings to `verified` (or reopens). I have **not** self-verified.

| ID | Sev | Status | Summary | Fix commit |
|----|-----|--------|---------|-----------|
| SEC-1 | 🔴 | fixed | `Cache-Control: private` (not public/immutable) + pinning test | 9af3b87 |
| BUG-1 | 🔴 | fixed | Total-pixel area cap + `MaxFrames=1` at both decode sites | 533996c |
| TEST-1 | 🔴 | fixed | Guest-401 interceptor branches now covered | 978620c |
| BUG-2 | 🟠 | fixed | Cleanup deletes `ThumbnailPath` too | c245a1e |
| BUG-3 | 🟠 | fixed | Deterministic id-keyed thumbnail path (no orphans) | c245a1e |
| REQ-1 | 🟠 | fixed | `MemoryAllocator` allocation cap added (+ story AC) | 533996c / eb8a6f8 |
| OBS-1 | 🟠 | fixed | Batch rejections logged (Warning + correlation id) | 21e66c8 |
| FE-1 | 🟠 | fixed | In-flight `ensureGuestSession` dedup (shareReplay) | f55daae |
| FE-2 | 🟠 | fixed | Upload auto-retries once after a 401 | f55daae |
| TEST-2 | 🟠 | fixed | Real `ImageProcessor` exercised (bomb/valid/unreadable) | 533996c |
| TEST-3 | 🟠 | fixed | Separate seed/SUT contexts prove persistence | c245a1e |
| BUG-4 | 🟡 | fixed | `ImageFormatException` → 422, not 500 | 533996c |
| QUAL-1 | 🟡 | fixed | `AsNoTracking` restored on hit path; Attach on miss | c245a1e |
| QUAL-2 | 🟡 | fixed | Return generated stream on miss (no re-read) | c245a1e |
| OBS-2 | 🟡 | fixed | Client-abort log at Information (`request.client_aborted`) | 26165a3 |
| OBS-3 | 🟡 | fixed | Reserved `uploads.decompression_bomb.rejected` event | 533996c |
| FE-3 | 🟡 | fixed | Anon 401 clears token, no login dead-end | 978620c |
| FE-4 | 🟡 | fixed | `restoreFromSession` distinguishes 401 from 404 | f55daae |
| REQ-2 | 🟡 | fixed | Deterministic `thumbs/{owner}/{id}.jpg` path (+ story AC) | c245a1e / eb8a6f8 |
| REQ-3 | 🟡 | fixed | Story 002 soft-delete AC amended to 404 | eb8a6f8 |
| REQ-4 | 🟡 | fixed | Bundled guest-auth + dev-warning scope documented + AC'd | eb8a6f8 |
| DB-1 | 🟡 | fixed | Migration provider-aware (`varchar(512)` on Npgsql) | bca68fa |
| INPUT-1 | 🟡 | fixed | HEIF brand verified (rejects MP4/MOV/M4A early) | f850f69 |
| TEST-4 | 🟡 | fixed | Cache-Control + 304 + dedup + TOCTOU-proxy tests added | fad7693 |
| CLOUD-1 | 🟡 | deferred | Stream seekability/`Length`/`ExistsAsync` — bolt-043 design constraint | — |
| QUAL-3 | ⚪ | fixed | Shared `ExceedsDecodeLimits` helper + message const | 533996c |
| QUAL-4 | ⚪ | fixed | Named 30-day cache TTL constant | 9af3b87 |
| QUAL-5 | ⚪ | fixed | Intentional-duplication comment on split-query branches | eb8a6f8 |

## Decisions / rationale

- **REQ-3 — amended the AC to 404 (did not serve the thumbnail).** Story 002's edge case
  said "source soft-deleted but thumbnail persisted → return the thumbnail," but the code
  filters `DeletedAt == null` → 404, and a test locks that in. A soft-deleted upload is on
  its way out (cleanup deletes both files, now including the thumbnail — BUG-2), so serving
  it would be resurrecting a deleted resource. Amended the spec to 404. Push back if you'd
  rather the thumbnail be served.
- **BUG-3 — deterministic key, NOT `RowVersion`.** The resolution seed and the review both
  note a `RowVersion` without a reload/retry handler would turn today's benign silent leak
  into an uncaught `DbUpdateConcurrencyException` → 500. The deterministic key makes a
  concurrent/cancelled write overwrite the same path (benign), so no concurrency token is
  needed. The thumbnail path is `thumbs/{ownerId}/{uploadId:N}.jpg` (owner-scoped) rather
  than the spec's `thumbs/{id}.jpg` — a distinct namespace so it can't collide with the
  original (`{ownerId}/{uploadId:N}.jpg`).
- **REQ-4 — documented rather than split.** The bundled guest-auth self-heal and
  dev-warning changes now have a retroactive AC in `bolt.md` and (for change B) real tests.
  Retroactively rewriting history into separate bolts is a process decision for the owner;
  the review accepts "at minimum document with ACs and tests," which is done.
- **DB-1 — DDL fixed in place; migration smoke test deferred.** The provider-aware column
  type is corrected (safe: no Postgres has applied this migration). A `Migrate()`-based DDL
  smoke test (SQLite/Testcontainers-Postgres) belongs to the 3-env phase of the roadmap, per
  the sibling AddOrderIdempotencyKey deferral and the review's own "flag, don't necessarily
  build now."
- **INPUT-1 — brand check fixed; HEIC still advertised.** The over-broad `ftyp` acceptance
  is fixed (MP4/MOV/M4A rejected up front). ImageSharp 3.1.11 has no HEIF decoder, so a
  *legitimate* HEIC still fails — but now cleanly at decode (422 via BUG-4). Whether to stop
  advertising HEIC entirely is a product decision left to the owner; not silently dropped.
- **CLOUD-1 — deferred (reviewer-seeded).** Unchanged: the seekable-stream / `Length` /
  `ExistsAsync` assumptions hold for the only current (local) `IStorageService` and only
  break once bolt-043's cloud provider lands. Recorded as a 043 design constraint.

## Notes for the re-reviewer

- **Self-reviewed the diff** (fix-generativity): no new guard was dropped; the
  `AsNoTracking` + `Attach`-single-column path only ever writes `ThumbnailPath` (other
  columns stay `Unchanged`), so there's no wipe risk; the ETag stays consistent across the
  miss (in-memory length) and hit (stored file length) paths because the stored bytes equal
  the returned bytes. New tests are not duplicative (the shared-context and fresh-context
  cache tests assert different things).
- **Decisive non-vacuity to check in v2:** (a) revert the `ExceedsDecodeLimits` area cap →
  `ImageProcessorTests.GenerateThumbnailAsync_OversizedImage_ThrowsDecompressionBomb` should
  go red (a per-axis check passes 9000×6000); (b) delete the miss-branch `SaveChanges` →
  `GetPreviewAsync_SecondRequestFreshContext_UsesPersistedThumbnail` should regenerate
  (Times.Exactly(2)); (c) drop the thumbnail delete in cleanup →
  `Cleanup_deletes_cached_thumbnail_file_alongside_original` should fail.
- **Not exercised (named per the review's "green ≠ proven"):** a true multi-threaded
  cache-miss race (InMemory can't model it; the deterministic key makes it benign by design
  and a determinism unit test stands in); the Postgres migration DDL (deferred, DB-1).
