---
type: findings-detail
target: 042-thumbnail-cache
version: 8
pass-type: discovery
commit: e2093bdd596107d2e67ff4a4135c47e4530f6eb4
reviewed: 2026-07-14
model: claude-opus-4-8
---

# Findings detail — Bolt 042 v8 (discovery)

Durable per-finding record for the v8 blinded discovery pass, so the full scenario + adversarial
guard/trace evidence survives outside session-temp (README convention). Ranked prose + fixes are in
[review-v8.md](review-v8.md); this file carries the raw skeptic evidence per canonical finding.

**Tally:** 31 canonical (from 38 raw across 11 lenses). 0 High / 7 Medium / 17 Low / 4 Cleanup =
**28 kept** + **3 refuted**. Verdicts: 19 confirmed · 5 plausible · 4 unverified-cleanup · 3 refuted.
Skeptic runs: 15 guard + 26 trace (flat 2-per would be 54). Max convergence 3.

## Mediums

### F1 — Cleanup/cache-fill race orphans the thumbnail · `UploadCleanupJob.cs:101` · confirmed · conv 3 [D34/D31 → bolt-043]
Lenses: correctness + race + completeness-critic. **Guard evidence:** No guard prevents the ordering.
`stillLive` (`UploadService.cs:216-219`) only deletes when the row is dead; in this interleaving the
preview commits `ThumbnailPath` before cleanup sets `DeletedAt`, so `stillLive=true`. `Upload` has no
concurrency token (comment `UploadService.cs:213`; absent in `Upload.cs`/`UploadConfiguration.cs`), so
cleanup's stale `null` snapshot (`UploadCleanupJob.cs:101`) skips the thumbnail delete; soft-deleted rows
are never rescanned. Real leak. (≥3-convergence spot-check; guard-hunt found nothing.)

### F2 — Global SplitQuery mis-pages collection-Include · `AdminOrderService.cs:67` · confirmed · conv 1 [D85 new]
Lens: completeness-critic. **Guard:** none — `AdminOrderService.cs:67` orders only by
`OrderByDescending(CreatedAt)` with no unique tiebreaker; `Program.cs:37/39` enable `SplitQuery` globally.
Exactly EF Core's documented Skip/Take + non-unique-order + split-query hazard. **Trace:** two orders share
`CreatedAt` at a page boundary; parent and child split queries run as separate round-trips (no wrapping txn),
so on Postgres a concurrent insert / plan shift breaks the tie differently → an order on the page loads empty
Items (`Sum quantity=0`). "Items of other orders" is inaccurate (child joins by PK); the real symptom is
**missing items**. Verified against code this synthesis pass (Program.cs:37/39 + AdminOrderService.cs:67-74).

### F3 — F2-fix contact-info wiped by self-heal re-init · `format-selector-page.ts:205` · confirmed · conv 2 [D86 new, F2-residual]
Lenses: frontend-ux + completeness-critic. **Guard:** none — `storeSession` (`guest-auth.service.ts:47-49`)
blindly `setItem`-overwrites the whole `guestSession` key with no merge; `clearGuestToken`
(`auth.service.ts:107-113`) preserves contact info, but the preserved `name/email/phone` is destroyed when
`ensureGuestSession` re-init calls `storeSession` with empty strings (`format-selector-page.ts:205-211`).
**Trace:** guest has `{guestToken, firstName:'John', email:'j@x.com', phone:'123'}`. Stale-token upload 401s
→ interceptor `clearGuestToken()` keeps contact info (F2) → `performUpload.onUploadError` (wasGuest, 401,
!isRetry) calls `ensureGuestSession()` → token null → re-inits, `tap` runs `storeSession({…, firstName:'',
email:'', phone:''})` → full `localStorage.setItem` overwrite wipes the preserved fields. Verified against
code this synthesis pass.

### F4 — Bomb test asserts base type, alert can regress green · `UploadServiceTests.cs:480` · confirmed · conv 1 [D87 new]
Lens: tests-coverage. **Guard:** none — `UploadService.cs:91` throws `DecompressionBombException`; the test
asserts base `UnprocessableEntityException`. FluentAssertions `ThrowAsync<T>` matches subclasses, so
regressing line 91 to the plain base + same message stays green. No other `UploadServiceTests` case pins the
concrete type. **Trace:** regress to `throw new UnprocessableEntityException(DimensionsExceededMessage)` —
message matches `"*dimensions exceed*"`, subclass check passes, test green; but middleware:106 and
controller:128 gate on `ex is DecompressionBombException` (now false), so `uploads.decompression_bomb.rejected`
never fires. Alerting silently dies.

### F5 — Lost original blob indistinguishable from benign 404 · `UploadService.cs:183` · confirmed · conv 1 [D88 new]
Lens: observability. **Guard:** `UploadService.cs:178-184` catch throws `NotFoundException` with NO log; the
cache-miss case at :157-165 emits a distinct `cache_miss_missing_file` warning — the more severe
original-blob-loss path has no equivalent signal. **Trace:** live row, owner, `ThumbnailPath` null → GET
/preview → `GenerateThumbnailAsync` throws `FileNotFoundException` (original gone) → catch throws
`NotFoundException("no longer available")` with no service log → middleware :78 emits only generic
`Handled … NotFoundException`, indistinguishable from routine unknown-id 404s. Severe storage-integrity
incident hidden; benign case flagged.

### F6 — Soft-delete-race deletion leaves silent partial state · `UploadService.cs:219` · confirmed · conv 1 [D89 new]
Lens: observability. **Guard:** none — `UploadService.cs:219-220` deletes the thumbnail with no log, unlike
siblings at :163 (`cache_miss_missing_file`) and :205 (`orphaned_on_commit_failure`). `SaveChanges` (:198)
persisted `ThumbnailPath` keyed only on `Id` (no `DeletedAt` guard, :213), so the soft-deleted row keeps
`ThumbnailPath` pointing at the deleted file. **Trace:** (1) GetPreviewAsync reads row live, ThumbnailPath
null; (2) cleanup (separate context) soft-deletes the row; (3) thumbnail generated + saved, ThumbnailPath set;
(4) commit UPDATE keyed on Id only writes path onto the dead row; (5) `stillLive=false` → :220 deletes file
with NO log. Stale ThumbnailPath on a dead row; the sibling anomaly paths log, this branch doesn't.

### F7 — Bit-depth-blind pixel cap · `ImageProcessor.cs:23` · confirmed · conv 1 [D77 re-raise, bumped 🟡→🟠]
Lens: input-validation. **Guard:** no bit-depth guard — `ExceedsDecodeLimits` (`ImageProcessor.cs:33-34`) caps
only `width*height` vs 100 MP, ignoring bytes/pixel; sole byte backstop is 512 MB `AllocationLimitMegabytes`
(`Program.cs:96`). **Trace:** upload 8500×8500 16-bit RGBA PNG (72.25 MP) — `UploadService` only Identifies +
pixel-caps (72.25 < 100 MP) → stored. First GET /preview: `GenerateThumbnailAsync` passes the same 100 MP
guard, then `LoadSingleFrameAsync` (non-generic load → `Rgba64`, 8 B/px) allocates 72.25M×8 = 578 MB > 512 MB
→ `InvalidMemoryOperationException` → middleware :116 emits a false `allocator_backstop` bomb alert + 422.
Repeats every preview → permanently un-previewable.

## Lows

### F8 — 30-day preview cache recoverable on a shared device · `UploadsController.cs:26` · confirmed · conv 1 [D90 new, D1-residual]
Lens: security. **Trace:** line 26 sets `private, max-age=2592000`; `private` bars shared/proxy caches, not the
browser's local per-profile cache. Guest on a shared PC previews → 200 + those headers → browser caches bytes.
Token cleared/expires. Next user of the same browser profile reopens the /preview URL from history; within 30d
it's fresh, browser serves cached bytes with no request — ETag/`If-None-Match` revalidation never fires, server
never re-checks ownership.

### F9 — ExistsAsync has no production caller · `IStorageService.cs:21` · plausible · conv 2 [D66 re-raise]
Lenses: requirements + quality. **Guard:** none — `ExistsAsync` has zero production callers (grep: impl + test
fake + mocks only). `GetPreviewAsync` (`UploadService.cs:153-165`) uses `GetStreamAsync` in
`catch(FileNotFoundException)`; its comment (:148-150) explicitly rejects exists-then-get. **Trace:** factually
real (dead method + false doc/test-mock confidence), but no input produces a wrong runtime result → no failing
execution constructible → plausible, not confirmed.

### F10 — File.Move over an open reader (Windows dev-only 500) · `LocalStorageService.cs:45` · confirmed · conv 1 [D75 re-raise]
Lens: correctness. **Trace:** Req A cache-hits → `GetStreamAsync` → `File.OpenRead` (`FileShare.Read`, no
Delete); controller returns `File(stream)`, holding the handle while streaming to a slow client. Req B read an
`AsNoTracking` snapshot with `ThumbnailPath==null`, regenerates → `SaveAsync` → `File.Move(overwrite:true)`
over the same key. Windows `MoveFileEx(REPLACE_EXISTING)` needs `FILE_SHARE_DELETE` → sharing violation
`IOException`, uncaught (only `FileNotFoundException` handled) → 500. Linux rename-over-open-fd succeeds:
dev-only.

### F11 — Cache-miss re-reads the just-written row · `UploadService.cs:216` · confirmed · conv 1 [D67 re-raise]
Lens: quality. **Trace:** any cache miss — row already loaded (round-trip 1); `SaveChangesAsync` persists
`ThumbnailPath` (round-trip 2, :198); then `AnyAsync(u.Id==uploadId && DeletedAt==null)` re-reads the same
just-written row (round-trip 3, :216-218). Confirmed 3rd round-trip, once per cold path. Low impact; a
conditional `ExecuteUpdate` folds write+check into one round-trip (blocked by InMemory tests lacking
`ExecuteUpdate`).

### F12 — Slot-release-on-throw untested · `ImageProcessor.cs:67` · plausible · conv 1 [D69 re-raise]
Lens: tests-coverage. **Guard:** no test guards it — the failed-decode tests assert only the throw, never
`AvailableSlots` after. `using var slot` prevents the runtime leak but is not a test. **Trace:** `using var
slot = await _decodeLimiter.AcquireAsync(ct)` disposes/releases on every exit (bomb + rethrown
`UnprocessableEntityException`), so the real code can't leak a slot → no failing execution today; hypothetical
future-refactor test gap → plausible.

### F13 — No end-to-end bomb→422 (integration fake pins 800×600) · `UploadFactory.cs:239` · confirmed · conv 1 [D93 new]
Lens: tests-coverage. **Trace:** integration test POSTs an "oversized" image; DI resolves `FakeImageProcessor`
(`UploadFactory:81`), whose `GetInfoAsync` always returns 800×600 → `ExceedsDecodeLimits(800,600)` (480k ≪
100M) never throws → upload succeeds instead of 422. The real dimension gate (`Image.IdentifyAsync`) lives in
`ImageProcessor`, unreachable in integration; only unit tests cover bomb→422.

### F14 — Npgsql migration DDL + snapshot parity untested · `AddUploadThumbnailPath.cs:19` · confirmed · conv 3, hinted [D23 re-raise → 3-env]
Lenses: tests-coverage + db-parity + completeness-critic. **Trace:** line 24 branches on `isNpgsql`; only the
else/"TEXT" arm is exercised. Introduce a typo in the Npgsql literal → suite still green (SQLite arm green,
everything else InMemory ignores migrations) → first Postgres `ef database update` throws. The migration test's
own comment concedes the Npgsql arm is deferred. Correctly `hinted` (dual-DB is a planted hint) → no
convergence discount.

### F15 — Decode-limiter saturation unobservable · `ImageDecodeLimiter.cs:30` · confirmed · conv 1 [D68 re-raise]
Lens: observability. **Trace:** `AcquireAsync` (:52-56) only `_gate.WaitAsync(ct)` + return Slot — no
log/metric; `AvailableSlots` is a bare getter, never surfaced. Requests > slot count → extras block silently in
`WaitAsync`; operator sees latency, zero signal. Purely an observability gap (correct behaviour) → low.

### F16 — Frame-cap tested only on the helper · `ImageProcessor.cs:81` · plausible · conv 1 [D42 residual]
Lens: tests-coverage. **Guard:** the only frame-cap test (`ImageProcessorTests.cs:154`) calls the internal
`LoadSingleFrameAsync` via reflection; nothing asserts frame count through `GenerateThumbnailAsync`. Dropping
`MaxFrames=1` at :81 keeps the suite green. **Trace:** line 81 today applies `DecoderOptions{MaxFrames=1}`, so
`GenerateThumbnailAsync` IS capped — the "regresses to Image.LoadAsync" scenario is a hypothetical future edit,
not a defect any input triggers now → test-brittleness, plausible.

### F17 — ensureGuestSession recovery-after-error untested · `format-selector-page.ts:214` · confirmed · conv 1 [D50 residual]
Lens: tests-coverage. **Trace:** all 12 specs mock `initAnonymousSession` as success (`of()`/Subject-next). L9
exercises the finalize-reset on completion, but no spec makes init emit an **error**, so the error-branch reset
(`guestInit$=null` on error → next `ensureGuestSession` re-inits and self-heals) is never driven. Genuine
coverage gap.

### F18 — Hard-kill between SaveAsync and commit orphans the file · `UploadService.cs:187` · confirmed · conv 1 [D31 re-raise, variant]
Lens: race. **Trace:** cache-miss `SaveAsync` writes the thumb to the deterministic key (:185-186); OOM/SIGKILL
before `SaveChangesAsync` (:198). The catch-delete (:200-210) fires only on exceptions, not hard kill, so the
row keeps `ThumbnailPath=null`. If never previewed again (no deterministic overwrite), `UploadCleanupJob:101`
(`if ThumbnailPath is not null`) skips the thumb → orphaned permanently. Low; only hard termination hits this.

### F19 — Guest 401 off the upload page is a silent dead-end · `error.interceptor.ts:33` · confirmed · conv 1 [D94 new]
Lens: frontend-ux. **Trace:** guest token expires on /checkout/plata; EuPlatesc pay → `initiateEuPlatesc` 401 →
interceptor guest branch calls `clearGuestToken()` only (no toast/nav); payment-step handler just clears the
loading flag. Re-click: guest interceptor skips the header (token null) → tokenless 401 again. Self-heal is
format-selector-only. Silent dead-end.

### F20 — In-session thumbnails leak via localUrl() · `photo-thumbnail.component.ts:86` · confirmed · conv 1 [D95 new, C1-residual]
Lens: frontend-ux. **Trace:** drop a file → `onFilesAccepted` builds `UploadState` with `file` but no
`previewUrl` (only restore sets it). During upload each progress event → `updateUpload` → new object ref →
OnPush re-render → template `[src]="localUrl()"` runs `URL.createObjectURL(file)` since `previewUrl` is unset.
Many progress events mint many untracked blobs; `revokeAllPreviews` only frees `previewUrl` → they leak for the
tab's life.

### F21 — Restore preview resolving after destroy leaks a URL · `format-selector-page.ts:404` · confirmed · conv 1 [D92 new, C1-residual]
Lens: correctness. **Trace:** refresh → `restoreFromSession` fires `fetchPreviewWithRetry`, HTTP in flight →
navigate away → `ngOnDestroy` → `revokeAllPreviews` finds no `previewUrl`, revokes nothing → subscription (no
`takeUntilDestroyed`) survives, response arrives, `URL.createObjectURL`, stores url via `updateUpload` on the
dead component → never revoked. One leaked blob URL per pending preview.

### F22 — Decode budget ignores concurrent upload buffering · `ImageDecodeLimiter.cs:30` · confirmed · conv 1 [D96 new, F1/D61-residual]
Lens: completeness-critic. **Trace:** host 2 GB/8 cores → `RecommendedMaxConcurrentDecodes=min(8, 2GB/512MB)=4`
→ 4×512MB=2GB reserved for decodes, zero headroom. Burst of ~20 concurrent uploads (fixed-window limiter caps
rate not concurrency) each buffering a ~50MB MemoryStream held while awaiting/running its decode slot: ~1GB
buffers + ~2GB decodes > 2GB → OOM. Real but low: only bites the memory-bound config; buffers are ~50MB each
(batch disposes per-file, not 500MB as first stated).

### F23 — Change C has no AC/test, mislabeled no-behavior-change · `bolt.md:73` · plausible · conv 1 [D91 new, doc]
Lens: requirements. **Guard:** `Program.cs:37/39` set SplitQuery for SQLite AND Npgsql (prod); the comment says
"No effect on the InMemory provider used in tests" so no test exercises it; `bolt.md:74` gives only a prose
bullet with no AC/test, unlike B/D. **Trace:** can't construct a wrong-result run from the *label* alone (EF
stitches split collections by PK, so output is identical — only SQL shape changes); the labeling/coverage nit is
real (and F2 shows the "no behavior change" claim is wrong for un-tiebroken paging) → plausible.

### F24 — Cloud stream contract (seekable + Length) untested · `UploadsController.cs:155` · plausible · conv 1, hinted [D28 re-raise → bolt-043]
Lens: completeness-critic. **Guard:** none — `:155` reads `stream.Length` with no `CanSeek` check/try-catch;
`IStorageService` XML-doc states no seekability/Length contract; `LocalStorageService` returns seekable
`File.OpenRead` which enforces nothing on future providers. **Trace:** the only impl is `LocalStorageService`
(seekable), so `stream.Length` always succeeds today; the claimed 500 needs the bolt-043 cloud provider, which
doesn't exist yet → speculative about unwritten code → plausible. Correctly `hinted`.

## Cleanups (⚪ — unverified by design)

- **F25 — Bomb event emitted in 3 diverging places · `UploadsController.cs:122` · conv 1 [D81 re-raise, worsened].**
  The `uploads.decompression_bomb.rejected` literal + emission is copy-pasted in the controller batch catch and
  twice in the middleware; the controller copy omits the `source=` dimension both middleware copies carry. A
  rename must touch three sites. Extract one helper + add `source=batch`.
- **F26 — Stale ExistsAsync Moq stubs · `UploadServiceTests.cs:296` · conv 2 [D66 re-raise, test side].**
  `GetPreviewAsync` never calls `ExistsAsync`; the `ExistsAsync⇒true` stubs in `_SecondCall`/`_SecondRequestFreshContext`
  are inert and would mask a reintroduced exists-then-get TOCTOU (they pre-answer true). Delete them.
- **F27 — Plan says varchar(500), everything else 512 · `implementation-plan.md:17` · conv 1 [D59 re-raise, another file].**
  Plan Deliverable 1 + AC specify `character varying(500)`; story 001, the migration (`varchar(512)`),
  `UploadConfiguration.HasMaxLength(512)` and the snapshot all use 512. Same stale-doc-token class as v4 C6/D59.
- **F28 — Conditional-GET ETag only tested with an exact strong tag · `UploadsController.cs:158` · conv 1 [D97 new].**
  `Request.Headers.IfNoneMatch == etag` compares `StringValues` to a string; a weak validator (`W/"…"`), a
  comma-separated list, or `*` never matches → 304 silently degrades to a full 200. Parse per RFC + test.

## Refuted (dropped, recorded so they aren't re-raised)

- **`ImageProcessor.cs:77` — fail-open on null Identify** — REFUTED (re-raise of **D78**). ImageSharp 3.1.11
  `IdentifyAsync` returns non-null or throws (caught → 422), so `info` is never null at :77; the check-skipping
  branch is dead today. Backstop + `MaxFrames=1` fire regardless. Latent-on-library-upgrade only.
- **`ExceptionHandlerMiddleware.cs:116` — benign near-limit image logged as a bomb** — REFUTED.
  `AllocationLimitMegabytes=512` is a per-single-allocation cap (ImageSharp 3.1.11), not a cumulative/peak
  budget. A legal ≤100 MP 8-bit image is one ~400 MB buffer < 512; resize allocates only small separate buffers.
  The backstop can't trip on a benign image; `source=allocator_backstop` flags only dimension-lying bombs.
- **`UploadService.cs:208` — orphan-reclaim delete swallowed, ops never learn** — REFUTED (same as v6's FP).
  `orphaned_on_commit_failure` warning (with the key) is emitted BEFORE the best-effort delete, so the orphan is
  signalled regardless of the delete's outcome; the empty catch only drops a redundant second log.
