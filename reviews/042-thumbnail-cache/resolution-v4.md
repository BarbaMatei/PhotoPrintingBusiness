---
type: resolution
target: 042-thumbnail-cache
answers_review: review-v4.md
version: 4
branch: feat/bolt-042-thumbnail-cache
status: resolved
fixed_commit: 6c4f334
opened: 2026-07-14
closed: 2026-07-14
findings:
  M1:  { status: fixed, commit: 4d4d998, note: "After the preview ThumbnailPath persist, re-read liveness (DeletedAt null); if the row was soft-deleted under us, delete the just-written thumb. Used liveness re-read not guarded ExecuteUpdate (InMemory test provider can't run it). Race test injects concurrent soft-delete, revert-verified red." }
  M2:  { status: fixed, commit: aad083d, note: "SaveAsync writes to unique temp file then File.Move(overwrite) into place; concurrent writers of the same key no longer collide. Deterministic gated-stream concurrency test, revert-verified red." }
  M3:  { status: fixed, commit: aa6639c, note: "Added process-wide ImageDecodeLimiter (SemaphoreSlim) gating GenerateThumbnailAsync; config ImageProcessing:MaxConcurrentDecodes, default ProcessorCount. Limiter unit tests + gate-ordering test (revert-verified red)." }
  M4:  { status: fixed, commit: f1c4ade, note: "Batch catch now emits uploads.decompression_bomb.rejected with width/height when ex is DecompressionBombException, matching the middleware. Test asserts event name + dimensions (also covers L12 for the batch path). Revert-verified red." }
  M5:  { status: fixed, commit: "80379f6+63b815a", note: "Stop accepting HEIC (no HEIF decoder). Backend @80379f6: MimeValidator no longer classifies ISO-BMFF as image/heic (-> null/415), message drops HEIC. UI @63b815a: removed .heic from ACCEPTED_EXTENSIONS/accept attr/hint + home copy. Validator + photo-upload specs flipped accept->reject, revert-verified red. Real HEIC decode deferred to a future bolt." }
  M6:  { status: fixed, commit: fea0d45, note: "Catch FileNotFoundException around the cache-miss GenerateThumbnailAsync; throw NotFoundException (404) since the original is unrecoverable (also lets the FE drop the dead entry). Regression test, revert-verified red." }
  M7:  { status: fixed, commit: 2b22e25, note: "GenerateThumbnailAsync catch now logs warning with {StoragePath} + caught exception (mirrors GetInfoAsync) and passes it as inner via new UnprocessableEntityException(msg, inner) ctor. Test asserts log carries storagePath + non-null exception, revert-verified red." }
  M8:  { status: fixed, commit: 1bdb21b, note: "fetchPreviewWithRetry now drops the restored entry on 403 and on a still-401 after re-init (like 404); only 5xx/network stay kept (NEW-2). Two tests (403-after-retry drop; persistent-401 drop), revert-verified red." }
  M9:  { status: fixed, commit: 2945bda, note: "Added SQLite Migrate() smoke test applying the real migration chain, asserting ThumbnailPath column lands (nullable); mutation-verified (broken column name -> red). Npgsql varchar(512) arm stays deferred to 3-env/Testcontainers (DB-1/D23) per standing decision." }
  M10: { status: fixed, commit: 7a7170e, note: "Added _storageMock.Verify(DeleteAsync, Times.Once) to the dimensions-exceed test; mutation-verified (remove bomb-path delete -> red)." }
  M11: { status: fixed, commit: 1108d47, note: "Extracted decode into internal LoadSingleFrameAsync (MaxFrames=1) used by production; reflection test asserts a 3-frame GIF decodes to 1 frame. Mutation-verified (drop MaxFrames -> red)." }
  L1:  { status: fixed, commit: dfb8f56, note: "Cache-hit path reads directly (dropped ExistsAsync pre-check) and catches FileNotFoundException -> regenerate; no TOCTOU 500, one round-trip. Test regenerate-not-500, revert-verified red." }
  L2:  { status: false-positive, commit: null, note: "Refuted in review-v4 §H — MIME change IS traced to f850f69 (INPUT-1). Residual folded into C4/docs." }
  L3:  { status: fixed, commit: dfb8f56, note: "Emit uploads.thumbnail.cache_miss_missing_file when a recorded thumbnail is absent (folded into the L1 catch). Signal test, revert-verified red." }
  L4:  { status: fixed, commit: 9b0bc81, note: "Wrap cache-fill SaveChangesAsync: emit uploads.thumbnail.orphaned_on_commit_failure + best-effort delete the just-written thumb, then rethrow. Test via throwing DbContext, revert-verified red." }
  L5:  { status: deferred, commit: 8466658, note: "Documented the primary-DB constraint at the write site (finding's 'at minimum document' bar). Re-architecture (cache-fill off the GET path) deferred until read-replica routing exists — none today, not planned pre-deployment." }
  L6:  { status: fixed, commit: 158b733, note: "SanitizeFileNameForLog strips control chars + caps to 128 before the batch-reject log. Test (newline + 200-char name), revert-verified red." }
  L7:  { status: disputed, commit: null, note: "Conflicts with FE-3/D13 (verified): this guest-first app DELIBERATELY does not bounce unauthenticated 401s to login, with a passing test asserting exactly that. L7 wants the opposite. See decisions." }
  L8:  { status: fixed, commit: 1bdb21b, note: "Added persistent-401 upload test: exactly 2 attempts then error (the !isRetry guard prevents a loop). Coverage test (regression would be an infinite loop, not a clean red)." }
  L9:  { status: fixed, commit: 1bdb21b, note: "Added re-init-after-settle test with a completing init + null token -> initAnonymousSession called twice. Mutation-verified: neutralising finalize() -> red." }
  L10: { status: deferred, commit: null, note: "Same DB-1/D23 theme as M9. Per the finding, accept as a documented deferral — the migration comment already notes the phantom AlterColumn, and no in-place snapshot edit is wanted; per-provider migration assemblies deferred to the 3-env phase." }
  L11: { status: deferred, commit: null, note: "Re-raises v1 CLOUD-1 — seekable-stream/ETag assumption; not triggerable until bolt-043 cloud provider. Deferral stands." }
  L12: { status: fixed, commit: c0c07c7, note: "Bomb-log test now uses distinct 31000x32000 and asserts both dimensions render; mutation-verified (drop width/height from the log -> red)." }
  L13: { status: fixed, commit: e1c56c4, note: "Mapped SixLabors.ImageSharp.Memory.InvalidMemoryOperationException -> 422 in the middleware map; test asserts 422, mutation-verified red." }
  L14: { status: fixed, commit: ec8a894, note: "Added a corrupt-IDAT (recognized-but-broken) PNG test hitting the InvalidImageContentException branch; mutation-verified (narrow catch to UnknownImageFormatException -> red)." }
  C1:  { status: fixed, commit: af5cf74, note: "Revoke preview blob URLs on remove/drop/add-to-cart-clear/destroy (ngOnDestroy + revokeAllPreviews/revokePreview). Test: removing an upload revokes its URL." }
  C2:  { status: fixed, commit: af5cf74, note: "Extracted the thrice-duplicated upload-error string into a single UPLOAD_ERROR field." }
  C3:  { status: fixed, commit: f444a81, note: "Added a real-seam test (real AuthService, no clear spy): a guest 401 clears the same localStorage key getGuestToken reads. Covers the divergence concern without full component+HTTP wiring (component reads via the same AuthService)." }
  C4:  { status: fixed, commit: 6c4f334, note: "Refreshed walkthrough to shipped: private cache directive (SEC-1), AsNoTracking+Attach, migration 20260527102718. Also corrected adjacent drift in the same doc (deterministic key, 100 MP area cap, 800px) — see decisions." }
  C5:  { status: fixed, commit: 6c4f334, note: "Story 003 AC: '54 MP' -> '110 MP over the 100 MP cap'; '<=300 px' -> '<=800 px'." }
  C6:  { status: fixed, commit: 6c4f334, note: "Story 001 AC: varchar(500)->varchar(512); 'same shape as StoragePath' -> 'same shape as FilePath (varchar(512))'." }
  C7:  { status: fixed, commit: 28aff33, note: "Owner chose code->800px: ThumbnailMaxDimension 300->800; story ACs (already 800) now correct. Test asserts a 2000x1500 source downscales to >300 and <=800, revert-verified red." }
---

# Resolution — Bolt 042: Thumbnail Cache (answers review-v4)

Fixer-owned; one row per finding ID from [review-v4.md](review-v4.md). No blockers, so this is a
follow-up list, not a gate. IDs are pass-local to v4 (they do **not** map to v1's IDs).

## Recommended order (from review §I)

1. **M3** — decode concurrency gate (`SemaphoreSlim`); the only process-kill vector.
2. **M1 + M2 + M6** — make the deterministic-key write safe (temp-file+atomic-move, `DeletedAt`-guarded
   update, catch `FileNotFoundException`). One change closes most of cluster A.
3. **M4** — emit the bomb event on the batch path.
4. **M5** — HEIC: add a decoder or stop advertising it.
5. **M7, M9–M11** and §G lows/cleanup — fast-follows. **C4** (walkthrough's stale insecure
   `Cache-Control: public…immutable`) fix regardless — copying it reintroduces SEC-1.

## Decisions / deferrals (attached, not suppressed)

- **L11 → deferred** (re-raises v1 **CLOUD-1**): seekable-stream / ETag `stream.Length` assumption
  only breaks once the bolt-043 cloud `IStorageService` lands. Deferral stands; design constraint for 043.
- **C4 scope**: the finding named three walkthrough contradictions (cache directive, tracking,
  migration). Fixing them, the same one doc also mis-described the thumbnail path scheme (fresh UUID
  vs the shipped deterministic key), the decode cap (`MaxDecodeDimension=25000` vs `MaxDecodePixels`
  100 MP area), and the thumbnail size (300 vs the now-800 px from C7). A half-corrected walkthrough
  misleads identically, so all were refreshed in the C4 commit — recorded here per the "don't fix
  outside the finding set without saying why" rule.
- **M9 / L10 re-raise v1 DB-1** (migration DDL / snapshot never exercised): the Migrate()-based DDL
  test is deferred to the 3-env phase per the roadmap. The fixer may still add a cheap SQLite-file
  `Migrate()` smoke test now; the Postgres/Testcontainers arm stays deferred.
- **L2 → false-positive** (refuted in review-v4 §H).
- **L7 → disputed** (self-heal "broadened to swallow every unauthenticated 401"): this is a
  *direct conflict with FE-3 (D13), which was fixed AND verified*. FE-3 deliberately removed the
  login redirect for unauthenticated 401s because this is a guest-first app where a guest has no
  account to log into — and there's a **passing test** asserting an anonymous 401 does NOT navigate
  to login (`error.interceptor.spec.ts`: "does not navigate an anonymous user (no guest token) to
  login on 401"). L7 asks to restore that redirect / surface login, i.e. revert FE-3. A fixer must
  not revert a verified decision, so this is surfaced for the re-reviewer/owner rather than
  implemented. The residual over-broadening L7 notes is harmless: `clearGuestToken()` on an absent
  token is a no-op, and a guest token cleared by a spurious non-preview 401 self-heals on the next
  request. **If** the owner wants `clearGuestToken` scoped to upload/preview requests specifically,
  that's a small follow-up — but it does not change the no-login-redirect behavior FE-3 fixed.
- **L5 → deferred** (read-replica hazard): GET /preview writes on a cache miss. There is no read
  replica today (dual DB is SQLite dev / single Postgres prod), so the hazard can't fire. Took the
  finding's "at minimum document" option (a constraint note at the write site @8466658); the
  re-architecture to move cache-fill off the GET path is deferred until read-replica routing is
  actually introduced — premature to build now per the pre-deployment roadmap.

## Notes for the fixer

- **Fix-generativity is the theme here** — M1/M2/M6 exist *because* of the v1 BUG-3 deterministic-key
  fix. Self-review the concurrency of whatever you change (README *Bounding fix-generativity*).
- Keep comments minimal and don't narrate the fixes in-code (rationale goes here + the commit).
- A finding isn't `fixed` without the regression test the review named (esp. M9–M11, L12–L14 are
  themselves coverage gaps — the "fix" is the test).
