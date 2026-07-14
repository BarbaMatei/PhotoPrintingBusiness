---
type: findings-detail
target: 042-thumbnail-cache
answers_review: review-v6.md
version: 6
note: Full scenario/fix/adversarial-evidence for all 29 v6 findings + 1 refuted FP. review-v6.md details the 8 Mediums in prose; this is the durable full record for every finding incl. Lows/Cleanups. IDs are pass-local F# (severity/category are columns, not in the ID) and map to canonical D# in ledger.md.
---

# Findings detail — Bolt 042 v6 (fresh discovery, 29 + 1 FP)

Generated from the discovery-pass workflow (deduped + convergence-weighted + adversarially verified),
commit `6c0ed93`. Each entry: `file:line · severity · verdict · convergence (lenses) · confidence ·
D#`, then scenario / fix / guard+trace evidence. Ranked most-severe first, matching review-v6.

---

### F1 [MEDIUM] Decode-concurrency ceiling keyed to ProcessorCount, not memory → OOM DoS
`Program.cs:359` (limiter default `:103`) · confirmed · conv 1 · security · c7 · **D61 (new, residual of D33/M3)**
SCENARIO: Container reports 8 host cores but has 2 GB RAM. `ImageDecodeLimiter` defaults to
`Environment.ProcessorCount`=8 slots. 8 concurrent first-preview requests each decode a legal ~100 MP
image (~400 MB RGBA) = ~3.2 GB in flight. The per-allocation 512 MB backstop doesn't bound the sum →
OOM-kill. The v4 M3 fix added the limiter but its default sizing re-opens the DoS.
FIX: Derive the default from a memory budget (`floor(availableRAM / perDecodeBudget)`), or require an
explicit `ImageProcessing:MaxConcurrentDecodes` in prod config.
GUARD: No guard bounds summed decode memory. `Program.cs:103` defaults slots to `ProcessorCount`; the
512 MB backstop (`:96`) and 100 MP cap (`ImageProcessor.cs:23`) are per-decode only. The config knob is
opt-in, not a default guard.
TRACE: 8-core / 2 GB pod, 8 concurrent first-previews each pass the 100 MP bomb check and hold a slot
for the full load (~400 MB), summing ~3.2 GB; 512 MB caps a single allocation, not the sum → OOM. The
code comment itself admits the default ignores RAM.

### F2 [MEDIUM] Any unauthenticated 401 silently wipes the whole guest session (contact info + cart)
`error.interceptor.ts:33` · confirmed · conv 1 · hinted · frontend-ux · c6 · **D48 re-raise (was disputed, sharpened)**
SCENARIO: Guest fills name/email/phone at checkout (stored in `guestSession`), idles until the guest
token expires, then submits. The request 401s; `errorInterceptor` calls `clearGuestToken()`, which
removes the whole `guestSession` entry. No toast, no redirect. Contact info is gone and the
server-side cart (keyed by `X-Guest-Token`) is inaccessible; only format-selector has re-init/retry.
The old `logout()`+navigate preserved `guestSession`.
FIX: Scope the self-heal — clear only on upload/preview endpoints (or clear only the token field,
preserving contact info) — and let checkout surface a re-auth notice instead of a silent wipe.
GUARD: No guard. `error.interceptor.ts:33` unconditionally calls `auth.clearGuestToken()` on any
non-auth 401; that removes the whole `guestSession` key (`auth.service.ts:95`), which stores contact
info + token. No presence check, toast, or retry.
TRACE: Guest checkout form → `storeSession` writes `{guestToken, firstName, lastName, email, phone}`
to `guestSession`. Token expires server-side. Any later request 401s → `isAuthenticated()` false →
`clearGuestToken()` → `removeItem('guestSession')`, wiping contact info AND token. Confirmed.
NOTE: This is v4 **L7 / ledger D48**, previously *disputed* against the verified FE-3 no-login-redirect
decision. The dispute stands on the redirect question, but the checkout **data-loss** scenario is new
and materially stronger — worth revisiting rather than leaving disputed.

### F3 [MEDIUM] Expired-JWT logged-in user silently re-attributed to a throwaway anonymous guest
`format-selector-page.ts:232` · confirmed · conv 1 · hinted · frontend-ux · c5 · **D63 (new)**
SCENARIO: A logged-in user's JWT expires mid-session on format-selector. Upload 401s: `errorInterceptor`
sees `isAuthenticated()==true`, calls `logout()` (flips state false) + navigates to `/auth/login`. The
component's `onUploadError` then runs; `ensureGuestSession()` now sees not-authenticated + no guest
token, mints a NEW anonymous guest, and retries — the upload succeeds under a throwaway guest orphaned
from the user's account. Same for `fetchPreviewWithRetry`.
FIX: In `onUploadError`/`fetchPreviewWithRetry`, only run the guest self-heal when the user was already
a guest (capture `!isAuthenticated()` before the request, or skip re-init if a logout/navigation fired).
GUARD: No guard. `error.interceptor.ts:43` re-throws the 401 after `logout()` synchronously sets
`isAuthenticated=false`. `onUploadError` calls `ensureGuestSession` (`:195`), which checks only
`isAuthenticated() || getGuestToken()` — both false — so it mints a fresh guest and retries. No check
distinguishes an expired-JWT user.
TRACE: Logged-in JWT expires; upload 401s. Interceptor runs first (upstream of the component
subscriber): `isAuthenticated()==true` → `logout()` sets state false + navigates. Re-thrown 401 reaches
`onUploadError` (isRetry=false) → `ensureGuestSession()` sees not-authenticated + `getGuestToken()==null`
(logout leaves `guestSession` untouched) → mints a fresh guest and retries. Upload succeeds under a
throwaway guest.

### F4 [MEDIUM] Cache-fill vs cleanup `stillLive` TOCTOU leaks the soft-deleted thumbnail
`UploadService.cs:216` · confirmed · conv 3 (correctness, race, completeness-critic) · c6 · **D34 residual → D31 deferral**
SCENARIO: `UploadCleanupJob.ToListAsync` snapshots row R (`ThumbnailPath=null`) but commits `DeletedAt`
only after the whole `foreach` (`:117-118`). During that window a first-preview of R generates + saves
`thumbs/owner/R.jpg`, `SaveChanges` `ThumbnailPath`, then `stillLive` reads true (`DeletedAt` not yet
committed) so it skips deletion. Cleanup then commits `DeletedAt` without deleting the thumbnail (its
snapshot saw null). Row soft-deleted, never revisited → thumbnail leaks forever.
FIX: Make the fill write conditional + atomic: `UPDATE … SET ThumbnailPath WHERE Id=@id AND DeletedAt
IS NULL` (`ExecuteUpdate`); if 0 rows affected, delete the just-written file. Closes the window the
second `AnyAsync` read cannot (and removes F17's round-trip).
GUARD: The `stillLive` guard (`:216-218`) checks `DeletedAt==null`, but `UploadCleanupJob` commits
`DeletedAt` only at `:118` after its foreach. During that window `stillLive` reads true and skips
deletion, while cleanup's in-memory snapshot (`:82`) had `ThumbnailPath=null` so it already skipped the
thumbnail. No transaction makes the two atomic.
NOTE: Same class as v5 **V5-1** (M1 residual) / ledger **D31** — accepted, deferred to the bolt-043
orphan sweep.

### F5 [MEDIUM] Allocator-backstop bomb (`InvalidMemoryOperationException`) not emitted as a bomb event
`ExceptionHandlerMiddleware.cs:106` · confirmed · conv 1 · observability · c6 · **D62 (new)**
SCENARIO: An image that under-reports dimensions at `Identify` (passing `ExceedsDecodeLimits`) but blows
the 512 MB allocator throws `InvalidMemoryOperationException` → mapped to 422 but logged only as the
generic "Handled exception" warning. Ops alerting on `uploads.decompression_bomb.rejected` miss exactly
the bombs that evaded the primary pixel guard.
FIX: Emit `uploads.decompression_bomb.rejected` (or a distinct `backstop-tripped` variant) when the
exception is `InvalidMemoryOperationException`, alongside the existing `DecompressionBombException` branch.
GUARD: `:106` gates the bomb event on `exception is DecompressionBombException` only.
`InvalidMemoryOperationException` (mapped 422 at `:26`) hits none of the distinct-event blocks → generic
warning only (`:78`).
TRACE: `:26` maps `InvalidMemoryOperationException` → 422 (its comment says it's a bomb that slipped the
pixel guard). But the emit at `:106` gates on `DecompressionBombException`, which it isn't → 422 with
only the generic warning; ops alerting on the reserved event miss it.

### F6 [MEDIUM] HEIC removal is an undocumented upload-contract change missing from bundled scope
`memory-bank/bolts/042-thumbnail-cache/bolt.md:57` · confirmed · conv 1 · requirements · c7 · **D64 (new)**
SCENARIO: `bolt.md`'s "Bundled scope" enumerates non-story changes (B guest-auth, C dev-warnings) so a
reviewer doesn't unknowingly ship a behavior change. Dropping HEIC — a user-facing contract change
across `MimeValidator`, `UploadService`, `photo-upload.component`, and home copy (accepted types
JPEG/PNG/HEIC → JPEG/PNG; upload now 415s) — is a third such change with no story/AC and is absent from
that list. A reviewer approving "bolt 042" ships it blind.
FIX: Add HEIC removal as a documented bundled-scope item (Change D) in `bolt.md` with its retroactive
AC, mirroring B and C.
GUARD: `bolt.md:57` lists ONLY Change B and Change C. HEIC removal (M5: `MimeValidator.cs:7`,
`photo-upload.component.ts:18`, backend, home copy) is a real committed contract change with no story/AC,
absent from the list and the bolt's docs (grep found nothing). Source comments explain the *why* but are
not the reviewer-facing scope enumeration.
TRACE: Commit `80379f6` removes HEIC support (MimeValidator drops `ftyp→image/heic`, UploadService
rejects it). `bolt.md:57-79` bundled-scope lists only B and C; HEIC is a real third contract change,
absent → reviewer ships it unknowingly.

### F7 [MEDIUM] Test-walkthrough certifies a Cache-Control value the code does not emit
`memory-bank/bolts/042-thumbnail-cache/test-walkthrough.md:28` · confirmed · conv 1 · requirements · c9 · **D65 (new)**
SCENARIO: The AC-validation doc marks story 002 delivered with `Cache-Control: public, max-age=2592000,
immutable`. Shipped code sets `private, max-age=2592000` (no `immutable`), and the integration test
asserts `Private=true/Public=false` (this is the security-critical SEC-1/D1 fix). It also claims
"460/460, +3 new" and lists 3 tests while the branch adds ~30 tests + new services (`ImageDecodeLimiter`,
`DecompressionBombException`). A reviewer trusting the doc signs off on the wrong (shared-cacheable)
contract.
FIX: Update `test-walkthrough` to the shipped `Cache-Control` (private, no immutable), the real test
count, and the review-added test/service inventory.
GUARD: `UploadsController.cs:25-26` emits `private, max-age=2592000`; test `:194-195` asserts
`Private=true/Public=false`. `test-walkthrough.md:28` certifies `public … immutable` — the opposite
contract. Nothing reconciles them.
NOTE: Different file than v4's C4/D57 walkthrough fix — that drift persisted into the *test*-walkthrough.

### F8 [MEDIUM] Cloud `IStorageService` contract unexercised — non-seekable stream breaks ETag/304
`UploadsController.cs:155` · plausible · conv 1 · hinted · completeness-critic · c6 · **D28 re-raise → bolt-043**
SCENARIO: A bolt-043 cloud provider returns a non-seekable stream (S3 `GetObject`). `stream.Length` at
`:155` throws `NotSupportedException` → every preview 500s. All the cloud-directed surface (prefix,
'/'-keys, `ExistsAsync`) is only ever run against `LocalStorageService`'s seekable `FileStream` and the
in-memory fake.
FIX (bolt-043): Assert the contract the cloud impl must satisfy (seekable / cheap-`Length` stream, or
compute ETag from a known length); add a non-seekable fake-storage test.
GUARD: `IStorageService.cs:18` promises only "a read stream" (no `CanSeek`). `UploadsController.cs:155`
calls `stream.Length` with no `CanSeek` check/buffering. Only seekable impls exist, so nothing enforces
seekability.
TRACE: No non-seekable stream exists in the real code — only `LocalStorageService` (`FileStream`) and
the test fake (in-memory), both making `.Length` safe. The S3/cloud provider is doc-only (bolt-043),
unimplemented → no execution reaches `:155` with a length-less stream. Latent future risk, standing
**D28** deferral, not a current defect.

### F9 [LOW] `ExistsAsync` added to `IStorageService` but has no production caller
`IStorageService.cs:21` · confirmed · conv 3 (requirements, tests-coverage, completeness-critic) · c6 · **D66 (new)**
SCENARIO: `GetPreviewAsync` now reads-directly-and-catches `FileNotFoundException` instead of
pre-checking, so `ExistsAsync` is referenced only by tests and the future bolt-043 cloud impl. Docs
describe it as the deletion-detection mechanism; a diff-focused reviewer may assume it's load-bearing on
the hot path, and the cloud provider inherits a method the app never calls.
FIX: Document `ExistsAsync` explicitly as a bolt-043-only seam, or drop it until 043 needs it.
GUARD: grep for `ExistsAsync` over `src` hits only `IStorageService.cs:21` (decl),
`LocalStorageService.cs:78` (impl), and tests. No production caller invokes it.

### F10 [LOW] Cleanup job's thumbnail-delete-failure path is untested and silently re-leaks the file
`UploadCleanupJob.cs:114` · confirmed · conv 1 · tests-coverage · c7 · **D71 (new)**
SCENARIO: `storage.DeleteAsync(thumbnailPath)` throws (locked file / cloud 503 in bolt-043): caught,
`fileErrors++`, but `upload.DeletedAt=now` still commits, so the row is soft-deleted and the orphaned
thumbnail is never revisited. No test injects a throwing thumbnail delete → the leak ships green.
FIX: Add a test: thumbnail `DeleteAsync` throws ⇒ `errors==1` and (ideally) the row is NOT soft-deleted
so a later run retries. Mirror the original-file expectation.
TRACE: Candidate has `ThumbnailPath` set. `:105` `DeleteAsync` throws; caught at `:107`, `fileErrors++`.
Execution continues to `:114` `DeletedAt=now`, committed `:118`. Query at `:74` filters `DeletedAt==null`,
so the soft-deleted row is never re-selected → orphan leaks permanently. The `:100` comment confirms
nothing revisits it.

### F11 [LOW] `GetInfoAsync` collapses storage/IO faults and cancellation into 'unreadable image'
`ImageProcessor.cs:56` · confirmed · conv 1 · observability · c5 · **D79 (new)**
SCENARIO: A transient storage read failure (or client cancellation) during upload is caught by the broad
`catch (Exception)`, logged identically to a corrupt file as "Failed to identify image", and returned
null → 422. A storage outage is indistinguishable in logs and to the client from junk uploads.
FIX: Let `FileNotFoundException`/`OperationCanceledException` propagate (map to 404/aborted); reserve
the warning+null path for genuine `ImageFormatException`.
TRACE: Valid file; storage `GetStreamAsync`/`IdentifyAsync` hits a transient `IOException` (or ct
cancels). `:56` broad catch swallows both, logs "Failed to identify image", returns null.
`UploadService.cs:82-85` turns null into 422 + deletes the file. Storage faults and cancellation surface
identically to junk uploads.

### F12 [LOW] Pixel-area cap ignores bytes-per-pixel; legit large 16-bit PNGs get a confusing 422
`ImageProcessor.cs:23` · confirmed · conv 1 · input-validation · c6 · **D77 (new)**
SCENARIO: A valid ~90 MP 16-bit RGB PNG (A0 scan), <50 MB compressed. Upload passes (canvas 90 MP ≤
100 MP). First preview decodes to `Rgb48` = 90M × 6 B = 540 MB, trips the 512 MB backstop →
`InvalidMemoryOperationException` → 422. A legitimate large-format print image is rejected.
FIX: Budget in bytes not pixels (multiply area by decoded bytes/px, or read bit depth from `ImageInfo`),
or downcast to `Rgba32` on load so the pixel cap matches the byte backstop.
TRACE: 90 MP passes `MaxDecodePixels=100MP` (`:23,77`). Non-generic `Image.LoadAsync` (`:116`) preserves
source depth → `Rgb48`=6 B/px: 90M×6=540 MB > `AllocationLimitMegabytes=512` (`Program.cs:94`). ImageSharp
throws `InvalidMemoryOperationException`, not caught by the `ImageFormatException`-only catch → mapped to
422 (`ExceptionHandlerMiddleware.cs:26`). The comment assumes 4 B/px RGBA.

### F13 [LOW] `File.Move(overwrite:true)` to a shared deterministic key can throw on concurrent writers (Windows)
`LocalStorageService.cs:45` · confirmed · conv 1 · correctness · c5 · **D75 (new, residual of D35/M2)**
SCENARIO: Two concurrent first-previews of the same upload each write a unique temp then `File.Move`
onto the same `thumbs/{owner}/{id}.jpg`. On Windows (dev), `MoveFileEx REPLACE_EXISTING` can fail with
`IOException` if the target is momentarily held (the other move, or an open reader without
`FILE_SHARE_DELETE`) → 500. Linux `rename` is atomic so prod is safe.
FIX: Catch `IOException` around `File.Move` and treat an already-present target as success
(last-writer-wins), or retry once. The temp-file swap (M2 fix) only removed the `File.Create` collision,
not the move-target race.
TRACE: Reader A `File.OpenRead(fullPath)` opens `FileShare.Read` only (no delete-share). Writer B
re-saves same key → `File.Move(overwrite:true)` at `:45`; `MoveFileEx REPLACE_EXISTING` must delete the
held target, reader's share mode denies → `IOException` → caught (`:47`) → rethrown → 500. Two racing
moves also collide.

### F14 [LOW] Cleanup `DeleteAsync` can fail while a cache-hit GET holds an open read handle → orphan (Windows)
`LocalStorageService.cs` `GetStreamAsync`/`DeleteAsync` · confirmed · conv 1 · race · c3 · **D76 (new)**
SCENARIO: A cache-hit GET streams via `File.OpenRead` (`FileShare.Read`, no delete-share). Concurrently
`UploadCleanupJob` deletes the same path; on Windows `File.Delete` throws a sharing violation, caught +
logged as a `fileError`, but `DeletedAt` is still set. Row soft-deleted, file remains → orphan cleanup
never revisits. Windows-dev-only; prod Linux unlinks.
FIX: Open served files `FileShare.ReadWrite|Delete` in `GetStreamAsync`, and/or have cleanup re-queue
paths whose delete failed rather than soft-deleting unconditionally.
TRACE: `GetStreamAsync` uses `File.OpenRead` (`FileShare.Read`). Concurrent `CleanupAsync` →
`DeleteAsync` → `File.Delete` throws sharing violation on Windows; `UploadCleanupJob` catches+counts
(`:107-111`) but still runs `DeletedAt=now` (`:114`). Candidate query filters `DeletedAt==null` (`:74`)
→ surviving file never revisited.
NOTE: The lens cited line 668; the file is 85 lines — actual location is `GetStreamAsync`/`DeleteAsync`.

### F15 [LOW] Decode-limiter saturation/queuing is unobservable
`ImageDecodeLimiter.cs:27` · confirmed · conv 1 · observability · c5 · **D68 (new)**
SCENARIO: A burst of concurrent first-previews (the exact vector the limiter defends) exhausts all
slots; subsequent requests silently block in `WaitAsync`. Latency spikes with no log or metric on wait
time / queue depth, so ops cannot attribute the slowdown to decode throttling.
FIX: Log (Information/Warning) or expose a metric when a caller waits for a slot, or when
`AvailableSlots` hits zero.
TRACE: N+1 concurrent `AcquireAsync`; N slots taken, request N+1 blocks at `:29` `_gate.WaitAsync(ct)`.
No log/metric on wait entry, duration, or queue depth.

### F16 [LOW] Parallel preview 401s defeat init dedup; a late 401 wipes the freshly minted token
`format-selector-page.ts:381` · confirmed · conv 1 · frontend-ux · c4 · **D72 (new)**
SCENARIO: Refresh restores N previews with an expired token; all N `getPreviewBlob` fire and 401 at
once. If init from response #1 completes (stores fresh token, `finalize` nulls `guestInit$`) before
response #k arrives, #k's interceptor `clearGuestToken()` wipes the just-minted token and its handler
mints yet another session — churning sessions. Grid outcome unchanged (all dropped); wasteful.
FIX: Guard re-init behind a per-restore-batch flag or shared retry stream so staggered 401s converge on
one re-init and don't clear a token another retry just established.
TRACE: Interceptor (`:33`) clears any guest token on 401 unconditionally. Two restored previews 401.
Response #1 mints session A; `finalize` nulls `guestInit$` + stores fresh token, retries. Delayed
response #2's interceptor wipes the fresh token; its handler sees `guestInit$===null` and mints session
B. Requires #2 to lag a full init round-trip; wasteful only.

### F17 [LOW] Extra `AnyAsync` round-trip on every cache-miss preview to detect the soft-delete race
`UploadService.cs:216` · confirmed · conv 1 · quality/efficiency · c4 · **D67 (new)**
SCENARIO: Each first-preview (cache miss) issues `SELECT` + `UPDATE` + a second `SELECT` (`stillLive
AnyAsync`) purely to catch the rare cleanup-soft-delete race (F4), adding a DB round-trip to
first-preview latency for every upload.
FIX: Fold the guard into the write — `ExecuteUpdateAsync` filtering `DeletedAt==null` and, if 0 rows
affected, delete the orphan thumbnail (F4's fix) — removing the extra `SELECT`.
TRACE: Any cache miss hits `:172+`: generate, `SaveChangesAsync` (UPDATE) at `:198`, then unconditional
`AnyAsync` at `:216-218` — a third round-trip after the initial live read + UPDATE, on every miss
regardless of whether the race occurred.

### F18 [LOW] Logged-in 401-during-upload interaction untested; interceptor logout/navigate races the retry
`error.interceptor.ts:24` · confirmed · conv 1 · hinted · tests-coverage · c5 · **D73 (new)**
SCENARIO: A logged-in user's token expires mid-upload: the interceptor calls `logout()`+navigate while
`onUploadError` also fires on the same 401, calling `ensureGuestSession()` (now unauthenticated) which
mints a guest session for a user being bounced to login, and retries. Every new component/interceptor
test sets `isAuthenticated=false`, so this branch (F3's path) has zero coverage.
FIX: Add a test for a logged-in 401 during upload; confirm no guest session is minted and the retry
doesn't fight the navigation.
TRACE: Logged-in, 401 mid-upload. Interceptor runs first: `isAuthenticated()` true → `logout()` flips
state false + navigates. Error propagates to `onUploadError` (!isRetry && 401) → `ensureGuestSession()`
mints a fresh guest + retries once. User bounced to login yet a guest session is spawned — incoherent,
untested. (Retry uses the new guest token, not a dead token.)

### F19 [LOW] `onFilesAccepted` guest-init error path untested; files can hang in 'uploading'
`format-selector-page.ts:176` · confirmed · conv 1 · hinted · tests-coverage · c6 · **D74 (new)**
SCENARIO: Guest with no token, `initAnonymousSession` errors (server/network down): the
`ensureGuestSession().subscribe` error handler must mark every dropped file 'error'. All FE tests
exercise upload-time 401/500 and re-init success; none make the INITIAL `ensureGuestSession` error, so a
regression leaves files stuck spinning with a green suite.
FIX: Add a test: `initAnonymousSession` returns `throwError` on `onFilesAccepted` ⇒ every new upload's
status becomes 'error' and `performUpload` is never called.
TRACE: Every test mocks `initAnonymousSession` to succeed (`of({guestToken:'fresh'})`): spec lines
240,260,288,311,323,343,360,382,397. 401/500 tests only hit upload-time errors with a successful init.
None makes the INITIAL init emit error, so the error handler at `:175-181` is never exercised.

### F20 [LOW] Plan acceptance criteria left stale after documented substitutions
`memory-bank/bolts/042-thumbnail-cache/implementation-plan.md:59` · confirmed · conv 1 · requirements · c7 · **D80 (new)**
SCENARIO: Plan AC still lists `Cache-Control: public, max-age=2592000, immutable` and rejection when
"dimensions exceed 25000×25000". Shipped code substitutes `private` (no immutable) and a 100 MP
total-area cap (a 30000×3000 = 90 MP image now passes, though it exceeds 25000 on one axis). The
walkthrough records these deviations, but the plan's own AC checklist was not reconciled.
FIX: Reconcile the plan AC list with shipped behavior (`private` cache-control; 100 MP area cap), or
annotate each with its documented deviation.
TRACE: Plan AC `:59` states public/immutable but `UploadsController.cs:26` ships `private` (no
immutable). AC `:60` says reject >25000×25000, but `ImageProcessor.cs:23-34` uses a 100 MP area cap; a
30000×3000 (90 MP) image passes the code yet violates the plan's per-axis rule.

### F21 [LOW] No test proves the decode slot is released when `GenerateThumbnailAsync` throws
`ImageProcessor.cs:67` · plausible · conv 1 · tests-coverage · c6 · **D69 (new)**
SCENARIO: Tests cover gate ordering and cancellation, but none assert the slot returns after a
bomb/format-error throw. If a refactor moved `using var slot` or caught around it, each rejected image
would leak a permit; after `MaxConcurrentDecodes` rejections all previews block forever — suite stays
green.
FIX: With a 1-slot limiter, a throwing decode (bomb or corrupt file) must leave `AvailableSlots==1`.
GUARD: No guarding test. `ImageProcessorTests` asserts throws for bomb (`:55`), unreadable (`:116/128`),
truncated (`:180`), and gate-precedes-read cancellation (`:94`), but none assert the permit returns
after a post-acquire throw.
TRACE: `:67` `using var slot` disposes the permit on every exit path today, incl. `DecompressionBomb`
and `ImageFormat` throws — no leak now. The failure only arises "if a refactor moved" the using — a
hypothetical future edit, not a constructible execution. Test-coverage gap, correctly low, not a defect.

### F22 [LOW] `InvalidMemoryOperationException→422` exact-type mapping proven only by an injected instance
`ExceptionHandlerMiddleware.cs:26` · plausible · conv 1 · tests-coverage · c6 · **D70 (new)**
SCENARIO: Mapping uses `TryGetValue(exception.GetType())` (exact type). The middleware test throws
exactly `InvalidMemoryOperationException`. If ImageSharp's 512 MB limit actually raised a subclass on a
header-lying bomb, the exact-type lookup misses → raw 500, suite green.
FIX: Match by assignable base type, or add a decode-driven test that trips the real allocator limit and
asserts 422.
GUARD: `:76` does `TryGetValue(exception.GetType())` — exact-type, no base/subclass walk. Any
subclass/sibling falls to the else → raw 500 (`:128`).
TRACE: ImageSharp throws `InvalidMemoryOperationException` directly from its allocator throw-helper — it
is the concrete type, not a base with a subclass. No real path raises a derived type, so the exact-type
lookup hits and 422 is returned. Speculative; no concrete failing execution.

### F23 [LOW] Pixel-area guard skipped when `Identify` returns null (fail-open)
`ImageProcessor.cs:77` · plausible (dead today) · conv 1 · input-validation · c3 · **D78 (new)**
SCENARIO: `GenerateThumbnailAsync` enforces `ExceedsDecodeLimits` only `when info is not null`; a null
`Identify` falls through to `LoadSingleFrameAsync` with only the 512 MB backstop. ImageSharp 3.1.11
throws rather than returning null, so dead today — but a version bump reintroducing null-return silently
disables the primary bomb control.
FIX: Fail closed — treat a null `Identify` as unreadable (throw `UnprocessableEntityException`) instead
of proceeding to decode, mirroring the upload-time `GetInfoAsync`-null handling.
GUARD: `:77` gates the guard on `info is not null`; a null `Identify` skips it. `LoadSingleFrameAsync`
(`:116`) uses `MaxFrames=1` with no pixel/allocation cap. Only the separate allocator backstop remains.
TRACE: No input today makes `info` null: 3.1.11 `IdentifyAsync` throws (caught as `ImageFormatException`)
on unreadable data and returns non-null otherwise, so the guard is never bypassed. Unreachable; failure
requires a hypothetical version bump the finding itself calls "dead today".

### F24 [LOW] Provider-aware Npgsql migration DDL is exercised by no test
`Migrations/20260527102718_AddUploadThumbnailPath.cs:19` · plausible · conv 2 · hinted · db-parity/tests-coverage · c6 · **D23 re-raise → 3-env**
SCENARIO: Integration tests use InMemory, so the Npgsql `character varying(512)` branch runs nowhere in
CI. A typo in the Npgsql type string, or drift from the runtime model, surfaces only at prod `ef
database update` / a phantom AlterColumn on the next scaffold.
FIX: Apply the migration against real Postgres AND SQLite in CI and diff the model; don't rely on
InMemory to validate DDL.
GUARD: The only migration test, `UploadThumbnailPathMigrationTests`, exercises just the SQLite `TEXT`
arm and explicitly defers the Npgsql arm to future Testcontainers. The Npgsql DDL branch runs nowhere.
TRACE (correction): `UploadThumbnailPathMigrationTests:42` runs `db.Database.Migrate()` on real SQLite,
executing the migration's `Up()` and its SQLite `TEXT` branch, then asserts the column lands nullable —
so the SQLite arm IS exercised, contradicting the lens's "neither branch runs". Only the Npgsql arm is
uncovered — the known, explicitly deferred **D23** gap, not a demonstrable defect.

### F25 [LOW] Model snapshot records `ThumbnailPath` as TEXT, diverging from Npgsql varchar(512) (phantom AlterColumn)
`Migrations/PhotoPrintDbContextModelSnapshot.cs:707` · plausible · conv 1 · hinted · db-parity · c8 · **D23 re-raise → 3-env**
SCENARIO: The model snapshot is SQLite-flavored (`ThumbnailPath HasColumnType("TEXT")`), but the runtime
Npgsql model resolves `maxLength 512` to `character varying(512)`. Running `ef migrations add` under
Npgsql scaffolds a spurious `AlterColumn(TEXT→varchar(512))`.
FIX: Accept as the documented per-provider-migration deferral, or note it when reviewing the next Npgsql
migration diff.
GUARD: `Program.cs:26` defaults the provider to Postgres at design time; no `IDesignTimeDbContextFactory`
pins SQLite. The committed snapshot is uniformly SQLite-typed.
TRACE (correction): The entire snapshot is SQLite-flavored (all props TEXT/INTEGER) → scaffolded under
the SQLite design-time provider; `ThumbnailPath` isn't special. A phantom `AlterColumn` only arises if
design-time is switched to Npgsql — which the project never does (all 14 migrations SQLite) — and then
*every* column phantoms, not this one. Under the real SQLite workflow, TEXT+maxLength(512) is stable.
Misdescribed as a fresh defect; it is the standing **D23** parity gap.

### F26 [CLEANUP] Bomb-alert log template duplicated verbatim in controller and middleware
`UploadsController.cs:130` · unverified-cleanup · conv 1 · quality · c8 · **D81 (new)**
The template `"uploads.decompression_bomb.rejected correlation_id=… width=… height=…"` is hardcoded in
both `UploadsController.cs:130` and `ExceptionHandlerMiddleware.cs:108`. An ops-driven rename to one
copy silently diverges the two emit sites, breaking the alert on one vector. FIX: hoist the event
name/template to one shared constant (alongside `DimensionsExceededMessage`).

### F27 [CLEANUP] `dropRestoredEntry` duplicates `onRemoveUpload` body verbatim
`format-selector-page.ts:420` · unverified-cleanup · conv 1 · quality · c8 · **D82 (new)**
`dropRestoredEntry` (`:420`) and `onRemoveUpload` (`:282`) have identical bodies (`revokePreview`,
`uploads.update` filter, `saveToSession`, `cdr.markForCheck`). A future change to removal semantics must
be made in both or they drift. FIX: have `onRemoveUpload` delegate to a shared private
`removeByClientId` helper. *(This code came from the M8/L8 fix.)*

### F28 [CLEANUP] `client_aborted` log reads raw `Items["CorrelationId"]` instead of `GetCorrelationId()`
`ExceptionHandlerMiddleware.cs:64` · unverified-cleanup · conv 1 · quality · c7 · **D83 (new)**
`HttpContextExtensions` documents "Prefer `GetCorrelationId` over this key", and `HandleExceptionAsync`
10 lines below uses `context.GetCorrelationId()`. The new client-abort branch bypasses the extension
with the magic-string key, so a future key/format change won't reach this call site. FIX: use
`context.GetCorrelationId()`.

### F29 [CLEANUP] Storage save/delete traces logged at Debug under an Information floor — never emit
`LocalStorageService.cs:53` · unverified-cleanup · conv 1 · observability · c6 · **D84 (new)**
`MinimumLevel.Default` is Information in every environment, so `Saved upload to {Key}` (`:53`) and
`Deleted upload {StoragePath}` (`:63`) never emit. Per-file storage writes/deletes (incl. cleanup-job
thumbnail deletes and the GetPreview race delete) leave no individual trace, compounding F5/F11/F14.
FIX: raise the delete trace to Information (or emit a structured event at the delete call sites), the
only per-file storage-mutation signal.

---

## Recorded false positive (dropped)

### FP · `UploadService.cs:208` — "Orphaned-thumbnail reclaim is silent/swallowed, ops never learn"
raised by observability (medium, c7) · **REFUTED**
CLAIM: A failed best-effort `DeleteAsync` in the orphan-reclaim path is swallowed by `catch { }`, so the
leak is invisible.
WHY REFUTED: `UploadService.cs:205-207` emits the `orphaned_on_commit_failure` **warning
unconditionally, before** the best-effort `DeleteAsync` at `:208`. The warning signals the orphan itself
(not a "handled" state), so ops learn of the potential leak regardless of whether the swallowed delete
succeeds. The claimed harm ("reads as handled, ops never learn") is false — no failing outcome exists.
Recorded so it isn't re-raised next pass.
