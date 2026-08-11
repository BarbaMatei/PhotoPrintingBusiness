---
type: review-ledger
target: 042-thumbnail-cache
updated: 2026-08-11
closed: 2026-08-11 — retroactive owner sign-off (loop quiet since v9 @bd0d5fd 2026-07-14; no certification pass ran)
---

# Ledger — 042-thumbnail-cache

## Findings

| D# | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| D1 | 🔴 | v1 (SEC-1) | Preview `Cache-Control: public` on an ownership-checked response leaks it between users | `Controllers/UploadsController.cs:126` | verified | `095285c` |
| D2 | 🔴 | v1 (BUG-1) | The decode-bomb guard checks each axis, so it misses total pixels and frame count | `Services/ImageProcessor.cs:47` | verified | `095285c` |
| D3 | 🔴 | v1 (TEST-1) | The guest-401 self-heal branch of the interceptor has no test | `UI/…/error.interceptor.ts:27` | verified | `095285c` |
| D4 | 🟠 | v1 (BUG-2) | The cleanup job never deletes `ThumbnailPath`, so thumbnails pile up forever | `BackgroundJobs/UploadCleanupJob.cs:90` | verified | `095285c` |
| D5 | 🟠 | v1 (BUG-3) | The cache-fill write is neither repeatable nor atomic, so it orphans thumbnails | `Services/UploadService.cs:145` | verified | `095285c` |
| D6 | 🟡 | v1 (BUG-4) | An unreadable image at preview time is unmapped and returns 500 instead of 422 | `Services/ImageProcessor.cs:46` | verified | `095285c` |
| D7 | 🟠 | v1 (REQ-1) | Story 003's memory-allocator cap was dropped with no equivalent | `Program.cs` | verified | `095285c` |
| D8 | 🟡 | v1 (REQ-2) | The thumbnail is saved at a random path, not the spec's id-keyed path | `Services/UploadService.cs:145` | verified | `095285c` |
| D9 | 🟡 | v1 (REQ-3) | Story 002's soft-delete case contradicts the implemented and tested 404 | `Services/UploadService.cs:128` | verified | `095285c` |
| D10 | 🟡 | v1 (REQ-4) | The bundled guest-auth and dev-warning changes have no story, criterion or test | `Program.cs` | verified | `095285c` |
| D11 | 🟠 | v1 (FE-1) | Two overlapping `ensureGuestSession` calls mint two guest sessions | `UI/…/format-selector-page.ts:184` | verified | `095285c` |
| D12 | 🟠 | v1 (FE-2) | The self-heal is not seamless: a stale token fails the first upload with no retry | `UI/…/format-selector-page.ts:168` | verified | `095285c` |
| D13 | 🟡 | v1 (FE-3) | A visitor with no guest token is logged out to a login page they cannot use | `UI/…/error.interceptor.ts:27` | verified | `095285c` |
| D14 | 🟡 | v1 (FE-4) | `restoreFromSession` wipes the restored grid on an expired-token 401 | `UI/…/format-selector-page.ts:347` | verified | `095285c` |
| D15 | 🟠 | v1 (OBS-1) | Batch-upload rejections are swallowed with no logging | `Controllers/UploadsController.cs:98` | verified | `095285c` |
| D16 | 🟡 | v1 (OBS-2) | The client-cancellation log sits at Debug, under the Information floor, so it never emits | `Middleware/ExceptionHandlerMiddleware.cs:54` | verified | `095285c` |
| D17 | 🟡 | v1 (OBS-3) | A pixel-bomb 422 reads in the logs exactly like an ordinary unreadable image | `Services/ImageProcessor.cs:48` | verified | `095285c` |
| D18 | 🟡 | v1 (QUAL-1) | `AsNoTracking` was dropped, so every cache hit change-tracks for nothing | `Services/UploadService.cs:127` | verified | `095285c` |
| D19 | 🟡 | v1 (QUAL-2) | The miss branch throws away the generated thumbnail and re-reads it from storage | `Services/UploadService.cs:143` | verified | `095285c` |
| D20 | ⚪ | v1 (QUAL-3) | The dimension check and its message are duplicated across two layers | `Services/UploadService.cs:83` | verified | `095285c` |
| D21 | ⚪ | v1 (QUAL-4) | The 30-day cache lifetime is the inline number `2592000` | `Controllers/UploadsController.cs:126` | verified | `095285c` |
| D22 | ⚪ | v1 (QUAL-5) | Split-query configuration is written out separately in both database branches | `Program.cs:33` | verified | `095285c` |
| D23 | 🟡 | v1 (DB-1) | The migration's Postgres arm and the model snapshot are exercised by no test | `Migrations/20260527102718_AddUploadThumbnailPath.cs:19` | backlog | `bd0d5fd` |
| D24 | 🟡 | v1 (INPUT-1) | The HEIC magic-byte check accepts any ISO-BMFF container, including video | `Services/MimeValidator.cs:33` | verified | `095285c` |
| D25 | 🟠 | v1 (TEST-2) | The real image processor, and so the bomb guard, is mocked in every test | `Services/ImageProcessor.cs` | verified | `095285c` |
| D26 | 🟠 | v1 (TEST-3) | Cache persistence is unproven because one database context is shared across both calls | `Tests/…/UploadServiceTests.cs` | verified | `095285c` |
| D27 | 🟡 | v1 (TEST-4) | Cache-Control, 304, the migration and the cache-miss race have no tests | `Tests/…` | verified | `095285c` |
| D28 | 🟡 | v1 (CLOUD-1) | The storage contract assumes a rewindable stream with a readable length | `Controllers/UploadsController.cs:155` | backlog | `bd0d5fd` |
| D29 | 🟠 | v2 (NEW-1) | The 50 MP decode cap refuses legitimate large-format print uploads | `Services/ImageProcessor.cs` | verified | `f8b1325` |
| D30 | 🟡 | v2 (NEW-2) | A restored upload is discarded on any non-401 preview error, including a passing blip | `UI/…/format-selector-page.ts` | verified | `f8b1325` |
| D31 | 🟡 | v2 (NEW-3) | Nothing reclaims a thumbnail written between the cleanup job's read and its commit | `BackgroundJobs/UploadCleanupJob.cs:101` | backlog | `bd0d5fd` |
| D32 | 🟡 | v2 (NEW-4) | Stored keys use the operating system's separator instead of a forward slash | `Services/LocalStorageService.cs` | verified | `f8b1325` |
| D33 | 🟠 | v4 (M3) | Image decode has no total or concurrent memory bound, so many large images exhaust memory | `Services/UploadService.cs:158` | verified | `6c4f334` |
| D34 | 🟠 | v4 (M1) | The cache-fill write races the cleanup job and strands a thumbnail on the dead row | `Services/UploadService.cs:216` | backlog | `bd0d5fd` |
| D35 | 🟠 | v4 (M2) | Two first previews at once collide on an exclusive file create and return 500 | `Services/LocalStorageService.cs:32` | verified | `6c4f334` |
| D36 | 🟠 | v4 (M4) | A bomb sent to the batch endpoint never emits the reserved alert event | `Controllers/UploadsController.cs:119` | verified | `6c4f334` |
| D37 | 🟠 | v4 (M5) | HEIC is accepted but no decoder exists, so every HEIC upload fails | `Services/MimeValidator.cs:52` | verified | `6c4f334` |
| D38 | 🟠 | v4 (M6) | A cache miss whose original is gone returns 500 instead of a clean 4xx | `Services/ImageProcessor.cs:63` | verified | `6c4f334` |
| D39 | 🟠 | v4 (M7) | An unreadable stored image is logged without its storage path or its cause | `Services/ImageProcessor.cs:88` | verified | `6c4f334` |
| D40 | 🟠 | v4 (M8) | A preview kept on 403 leaves an upload the guest can never put in a cart | `UI/…/format-selector-page.ts:400` | verified | `6c4f334` |
| D41 | 🟠 | v4 (M10) | No test proves the bomb rejection deletes the file it already stored | `Tests/…/UploadServiceTests.cs:381` | verified | `6c4f334` |
| D42 | 🟠 | v4 (M11) | The one-frame decode cap is proven only through the internal helper, not the public call | `Services/ImageProcessor.cs:81` | backlog | `bd0d5fd` |
| D43 | 🟡 | v4 (L1) | The cache-hit path checks then reads, so a vanished file gives 500 and costs a round-trip | `Services/UploadService.cs:150` | verified | `6c4f334` |
| D44 | 🟡 | v4 (L3) | A vanished cache file quietly regenerates with no signal | `Services/UploadService.cs:150` | verified | `6c4f334` |
| D45 | 🟡 | v4 (L4) | A thumbnail orphaned by a failed commit emits no signal | `Services/UploadService.cs:159` | verified | `6c4f334` |
| D46 | 🟡 | v4 (L5) | The preview GET writes to the database, which fails against a read replica | `Services/UploadService.cs:166` | backlog | `6c4f334` |
| D47 | 🟡 | v4 (L6) | The batch-rejection log prints the raw client filename with no length limit | `Controllers/UploadsController.cs:120` | verified | `6c4f334` |
| D48 | 🟡 | v4 (L7) | Any unauthenticated 401 wipes the whole guest session, checkout contact details included | `UI/…/error.interceptor.ts:30` | verified | `79c2eda` |
| D49 | 🟡 | v4 (L8) | The one-shot retry guard is untested for a retry that still fails | `UI/…/format-selector-page.ts:216` | verified | `6c4f334` |
| D50 | 🟡 | v4 (L9) | Guest-session recovery after a failed init is untested | `UI/…/format-selector-page.ts:214` | backlog | `bd0d5fd` |
| D51 | 🟡 | v4 (L12) | The bomb log test asserts the event name but not the dimensions the event exists to carry | `Tests/…/ExceptionHandlerMiddlewareTests.cs:254` | verified | `6c4f334` |
| D52 | 🟡 | v4 (L13) | The 512 MB allocator backstop throws an unmapped exception, giving 500, and is untested | `Program.cs:95` | verified | `6c4f334` |
| D53 | 🟡 | v4 (L14) | The recognised-but-broken image path to 422 is untested | `Services/ImageProcessor.cs:81` | verified | `6c4f334` |
| D54 | ⚪ | v4 (C1) | Preview object URLs are never released, so every restore and retry leaks one | `UI/…/format-selector-page.ts:388` | verified | `6c4f334` |
| D55 | ⚪ | v4 (C2) | The upload error message is written out at three sites | `UI/…/format-selector-page.ts:220` | verified | `6c4f334` |
| D56 | ⚪ | v4 (C3) | The self-heal seam is tested only with each half mocked | `UI/…/format-selector-page.ts:227` | verified | `6c4f334` |
| D57 | ⚪ | v4 (C4) | The implementation walkthrough contradicts the shipped code | `memory-bank/…/implementation-walkthrough.md:32` | fixed | `838c9b6` |
| D58 | ⚪ | v4 (C5) | Story 003 says 54 MP is refused while the shipped cap is 100 MP | `memory-bank/…/003-imagesharp-max-pixels.md:27` | verified | `6c4f334` |
| D59 | ⚪ | v4 (C6) | Story 001 names `varchar(500)` and `StoragePath`; the column shipped as `varchar(512)` and `FilePath` | `memory-bank/…/001-thumbnail-path-schema.md:22` | verified | `bd0d5fd` |
| D60 | ⚪ | v4 (C7) | The thumbnail shipped at 300 px while the stories and the brief say 800 px | `memory-bank/…/002-persist-thumbnail-on-first-request.md:39` | verified | `6c4f334` |
| D61 | 🟠 | v6 (F1) | The decode limiter defaults to the core count and ignores memory, so it still exhausts memory | `Program.cs:359` | verified | `79c2eda` |
| D62 | 🟠 | v6 (F5) | A bomb caught by the allocator backstop never emits the reserved bomb event | `Middleware/ExceptionHandlerMiddleware.cs:106` | verified | `79c2eda` |
| D63 | 🟠 | v6 (F3) | A logged-in user whose token expired is re-attributed to a throwaway guest | `UI/…/format-selector-page.ts:232` | verified | `79c2eda` |
| D64 | 🟠 | v6 (F6) | The HEIC removal is missing from the bolt's bundled-scope document | `memory-bank/…/bolt.md:57` | verified | `79c2eda` |
| D65 | 🟠 | v6 (F7) | The test walkthrough certifies a `Cache-Control` value the code never emits | `memory-bank/…/test-walkthrough.md:28` | verified | `79c2eda` |
| D66 | 🟡 | v6 (F9) | `ExistsAsync` was added to the storage interface but nothing in production calls it | `IStorageService.cs:21` | backlog | `bd0d5fd` |
| D67 | 🟡 | v6 (F17) | Every cache-miss preview pays an extra database round-trip to spot the soft-delete race | `Services/UploadService.cs:216` | backlog | `bd0d5fd` |
| D68 | 🟡 | v6 (F15) | Nothing reports how saturated or how queued the decode limiter is | `ImageDecodeLimiter.cs:30` | backlog | `bd0d5fd` |
| D69 | 🟡 | v6 (F21) | No test proves the decode slot is released when the decode throws | `Services/ImageProcessor.cs:67` | backlog | `bd0d5fd` |
| D70 | 🟡 | v6 (F22) | The allocator-exception-to-422 mapping is proven only by an injected instance | `Middleware/ExceptionHandlerMiddleware.cs:26` | backlog | `79c2eda` |
| D71 | 🟡 | v6 (F10) | A failed thumbnail delete in the cleanup job is untested and silently leaks the file again | `BackgroundJobs/UploadCleanupJob.cs:114` | backlog | `79c2eda` |
| D72 | 🟡 | v6 (F16) | Parallel preview 401s defeat the init sharing, and a late 401 wipes a fresh token | `UI/…/format-selector-page.ts:381` | backlog | `79c2eda` |
| D73 | 🟡 | v6 (F18) | The logged-in 401-during-upload path has no test | `UI/…/error.interceptor.ts:24` | verified | `79c2eda` |
| D74 | 🟡 | v6 (F19) | The guest-init error path in `onFilesAccepted` is untested, so files hang as uploading | `UI/…/format-selector-page.ts:176` | backlog | `79c2eda` |
| D75 | 🟡 | v6 (F13) | Moving a file onto a shared key races other writers on Windows and returns 500 | `Services/LocalStorageService.cs:45` | backlog | `bd0d5fd` |
| D76 | 🟡 | v6 (F14) | A cleanup delete fails against an open read handle on Windows and leaves an orphan | `Services/LocalStorageService.cs` | backlog | `79c2eda` |
| D77 | 🟠 | v6 (F12) | The pixel-area cap ignores bytes per pixel, so a legitimate 16-bit PNG is refused | `Services/ImageProcessor.cs:23` | verified | `bd0d5fd` |
| D78 | 🟡 | v6 (F23) | The pixel guard is skipped when the identify call returns null | `Services/ImageProcessor.cs:77` | false-positive | `e2093bd` |
| D79 | 🟡 | v6 (F11) | Storage faults and cancellation are reported as an unreadable image | `Services/ImageProcessor.cs:56` | backlog | `79c2eda` |
| D80 | 🟡 | v6 (F20) | The implementation plan's acceptance criteria are stale after documented substitutions | `memory-bank/…/implementation-plan.md:59` | backlog | `79c2eda` |
| D81 | ⚪ | v6 (F26) | The bomb-alert log template is copied across the controller and the middleware | `Controllers/UploadsController.cs:122` | backlog | `bd0d5fd` |
| D82 | ⚪ | v6 (F27) | `dropRestoredEntry` repeats the body of `onRemoveUpload` word for word | `UI/…/format-selector-page.ts:420` | backlog | `79c2eda` |
| D83 | ⚪ | v6 (F28) | The client-abort log reads the raw correlation-id item instead of the accessor | `Middleware/ExceptionHandlerMiddleware.cs:64` | backlog | `79c2eda` |
| D84 | ⚪ | v6 (F29) | Storage save and delete traces sit at Debug under an Information floor, so they never emit | `Services/LocalStorageService.cs:53` | backlog | `79c2eda` |
| D85 | 🟠 | v8 (F2) | The global split-query default mis-pages a collection include that has no tiebreaker | `Services/AdminOrderService.cs:67` | verified | `bd0d5fd` |
| D86 | 🟠 | v8 (F3) | `storeSession` overwrites the contact details the earlier fix preserved | `UI/…/format-selector-page.ts:205` | verified | `bd0d5fd` |
| D87 | 🟠 | v8 (F4) | The bomb test asserts the base exception, so the alert can regress while tests stay green | `Tests/…/UploadServiceTests.cs:480` | verified | `bd0d5fd` |
| D88 | 🟠 | v8 (F5) | A lost original blob is logged as a plain 404, with no distinct signal | `Services/UploadService.cs:183` | verified | `bd0d5fd` |
| D89 | 🟠 | v8 (F6) | The soft-delete-race deletion leaves database and file state silently out of step | `Services/UploadService.cs:219` | verified | `bd0d5fd` |
| D90 | 🟡 | v8 (F8) | The 30-day private preview cache is recoverable on a shared device | `Controllers/UploadsController.cs:26` | backlog | `bd0d5fd` |
| D91 | 🟡 | v8 (F23) | Bundled change C has no criterion or test and is labelled as changing no behaviour | `memory-bank/…/bolt.md:73` | verified | `bd0d5fd` |
| D92 | 🟡 | v8 (F21) | A restore preview that resolves after the page is destroyed leaks an object URL | `UI/…/format-selector-page.ts:404` | backlog | `bd0d5fd` |
| D93 | 🟡 | v8 (F13) | No end-to-end test reaches the bomb-to-422 path because the integration fake pins 800×600 | `Tests/…/UploadFactory.cs:239` | backlog | `bd0d5fd` |
| D94 | 🟡 | v8 (F19) | A guest 401 away from the upload page is a silent dead end | `UI/…/error.interceptor.ts:33` | backlog | `bd0d5fd` |
| D95 | 🟡 | v8 (F20) | `localUrl()` mints an untracked object URL on every change-detection cycle | `UI/…/photo-thumbnail.component.ts:86` | backlog | `bd0d5fd` |
| D96 | 🟡 | v8 (F22) | The decode memory budget ignores the upload buffering that shares the same memory | `ImageDecodeLimiter.cs:30` | backlog | `bd0d5fd` |
| D97 | ⚪ | v8 (F28) | The conditional GET matches only an exact strong tag, so weak, list and `*` fall back to 200 | `Controllers/UploadsController.cs:158` | backlog | `bd0d5fd` |

## Details

### D1 — Preview `Cache-Control: public` on an ownership-checked response leaks it between users

- **What:** The preview endpoint checked ownership and then marked the response `public`. Response
  caching runs before authentication and guests carry a custom header, so one guest's photo was served
  to another guest, or to nobody in particular, from the cache.
- **History:**
  - v1: found (SEC-1) — one of the pass's three fixes required before merge
  - round 1: fixed @`9af3b87` — the directive became `private` with a named 30-day constant
  - v2: verified @`095285c` — reverting the directive reddens the pinning test

### D2 — The decode-bomb guard checks each axis, so it misses total pixels and frame count

- **What:** The guard refused only images wider or taller than 25000 pixels. A 25000×25000 image, and an
  animated PNG with thousands of frames, both passed and decoded to gigabytes.
- **History:**
  - v1: found (BUG-1) — five lenses reached it independently and each built the exploit
  - round 1: fixed @`533996c` — a total-pixel cap at both decode sites plus a one-frame limit
  - v2: verified @`095285c` — the per-axis check passes a 9000×6000 image, so the revert reddens

### D3 — The guest-401 self-heal branch of the interceptor has no test

- **What:** Both interceptor tests ran with no guest token, so only the logout branch executed. The new
  guest branch could be inverted or deleted and every test stayed green.
- **History:**
  - v1: found (TEST-1) — one of the pass's three fixes required before merge
  - round 1: fixed @`978620c` — specs for token-present and token-absent
  - v2: verified @`095285c`

### D4 — The cleanup job never deletes `ThumbnailPath`, so thumbnails pile up forever

- **What:** The feature added a second file per upload but cleanup deleted only the original. Both the
  preview read and the cleanup candidate query skip soft-deleted rows, so the thumbnail was never
  reachable again.
- **History:**
  - v1: found (BUG-2)
  - round 1: fixed @`c245a1e` — cleanup deletes the thumbnail alongside the original
  - v2: verified @`095285c`

### D5 — The cache-fill write is neither repeatable nor atomic, so it orphans thumbnails

- **What:** Each cache miss minted a fresh random filename and wrote the file separately from the row.
  Two concurrent misses, or a cancelled request, left files nothing could reach.
- **History:**
  - v1: found (BUG-3)
  - round 1: fixed @`c245a1e` — a deterministic owner-scoped key under a `thumbs/` prefix
  - v2: verified @`095285c`
  - v4: recorded as the cause of D34, D35 and D38, which the deterministic key created

### D6 — An unreadable image at preview time is unmapped and returns 500 instead of 422

- **What:** The image library throws its own exception for unreadable input. That type was absent from
  the exception map, so a file corrupted after upload produced a raw 500 on the regeneration path.
- **History:**
  - v1: found (BUG-4) — the residual of a suspicion the pass refuted
  - round 1: fixed @`533996c` — the generic image-format exception is caught and mapped to 422
  - v2: verified @`095285c`

### D7 — Story 003's memory-allocator cap was dropped with no equivalent

- **What:** The story required both an allocator cap and per-image dimension limits. The dimension limits
  were substituted for a documented reason; the allocator cap was dropped silently, which is what let
  D2's bomb allocate gigabytes.
- **History:**
  - v1: found (REQ-1)
  - round 1: fixed @`533996c` — a 512 MB allocator cap, with the story amended @`eb8a6f8`
  - v2: verified @`095285c`

### D8 — The thumbnail is saved at a random path, not the spec's id-keyed path

- **What:** Story 002 specifies a path derived from the upload id. The code minted a random name, which
  fed D4 and D5 and would complicate the later move to cloud storage.
- **History:**
  - v1: found (REQ-2)
  - round 1: fixed @`c245a1e` — owner-scoped `thumbs/{ownerId}/{uploadId}.jpg`, story amended @`eb8a6f8`
  - v2: verified @`095285c`

### D9 — Story 002's soft-delete case contradicts the implemented and tested 404

- **What:** The story said a soft-deleted source with a surviving thumbnail should serve the thumbnail.
  The code filters soft-deleted rows and returns 404, with a test pinning it. Nothing reconciled them.
- **History:**
  - v1: found (REQ-3)
  - round 1: fixed @`eb8a6f8` — the story was amended to 404, since cleanup is about to remove both files
  - v2: verified @`095285c`

### D10 — The bundled guest-auth and dev-warning changes have no story, criterion or test

- **What:** The branch shipped a guest-auth self-heal and a set of startup changes under a
  thumbnail-cache label. Neither had a story, an acceptance criterion or a walkthrough entry, so
  approving the bolt approved an unannounced change to authentication behaviour.
- **History:**
  - v1: found (REQ-4)
  - round 1: fixed @`eb8a6f8` — both recorded in the bolt document with retroactive criteria
  - v2: verified @`095285c`

### D11 — Two overlapping `ensureGuestSession` calls mint two guest sessions

- **What:** The token read is synchronous, so it stays empty while an init is in flight. Page load and an
  eager file drop each started one. Uploads went out under one session while storage kept the other.
- **History:**
  - v1: found (FE-1)
  - round 1: fixed @`f55daae` — one shared in-flight init, cleared when it settles
  - v2: verified @`095285c`

### D12 — The self-heal is not seamless: a stale token fails the first upload with no retry

- **What:** An expired but present token short-circuited the init, so the upload went out stale, got a
  401, and the user saw a generic error and had to drop the files again.
- **History:**
  - v1: found (FE-2)
  - round 1: fixed @`f55daae` — one automatic retry after the token is cleared
  - v2: verified @`095285c` — the pass also found and fixed a retry test that skipped the re-init

### D13 — A visitor with no guest token is logged out to a login page they cannot use

- **What:** The interceptor required a guest token to take the guest branch, so a missing or corrupt
  token fell through to logout and a redirect to a login page a guest has no account for.
- **History:**
  - v1: found (FE-3)
  - round 1: fixed @`978620c` — an unauthenticated 401 clears the token and does not navigate
  - v2: verified @`095285c`

### D14 — `restoreFromSession` wipes the restored grid on an expired-token 401

- **What:** A refresh with an expired token fired parallel preview fetches. Each 401 dropped its entry
  and rewrote session storage, clearing the whole in-progress selection with no retry.
- **History:**
  - v1: found (FE-4)
  - round 1: fixed @`f55daae` — a 401 re-inits and retries once; only a 404 drops the entry
  - v2: verified @`095285c`

### D15 — Batch-upload rejections are swallowed with no logging

- **What:** The batch endpoint turned each rejection into a per-item result and never logged it, so bulk
  abuse — the most likely bomb route — was invisible while the endpoint returned 200.
- **History:**
  - v1: found (OBS-1)
  - round 1: fixed @`21e66c8` — each swallowed rejection logs a warning with the correlation id
  - v2: verified @`095285c`

### D16 — The client-cancellation log sits at Debug, under the Information floor, so it never emits

- **What:** Logging is set to Information in every environment with no override for this source, so the
  Debug line was filtered out everywhere. The comment said "log quietly"; in practice it logged never.
- **History:**
  - v1: found (OBS-2)
  - round 1: fixed @`26165a3` — raised to Information as a distinct client-abort event
  - v2: verified @`095285c`

### D17 — A pixel-bomb 422 reads in the logs exactly like an ordinary unreadable image

- **What:** Both paths threw the same exception through the same generic warning, so the only difference
  was free text. Nothing could alert on a spike in bomb attempts.
- **History:**
  - v1: found (OBS-3)
  - round 1: fixed @`533996c` — a distinct bomb exception and a reserved event carrying the dimensions
  - v2: verified @`095285c`

### D18 — `AsNoTracking` was dropped, so every cache hit change-tracks for nothing

- **What:** The no-tracking hint was removed only so the miss branch would compile, but it applies to the
  whole query. On the steady-state hit path the framework snapshotted an entity it never saves.
- **History:**
  - v1: found (QUAL-1)
  - round 1: fixed @`c245a1e` — no-tracking restored; the miss branch attaches and marks one column
  - v2: verified @`095285c`

### D19 — The miss branch throws away the generated thumbnail and re-reads it from storage

- **What:** On a miss the in-memory thumbnail was disposed and the file re-opened. That is an avoidable
  read on local disk and a billed round-trip once storage moves to the cloud.
- **History:**
  - v1: found (QUAL-2)
  - round 1: fixed @`c245a1e` — the generated stream is rewound and returned
  - v2: verified @`095285c`

### D20 — The dimension check and its message are duplicated across two layers

- **What:** The upload service and the image processor each carried their own dimension check and their
  own message, so hardening one could leave the other exploitable.
- **History:**
  - v1: found (QUAL-3)
  - round 1: fixed @`533996c` — one shared helper and one message constant
  - v2: verified @`095285c`

### D21 — The 30-day cache lifetime is the inline number `2592000`

- **What:** The cache lifetime was a magic number inside a header string.
- **History:**
  - v1: found (QUAL-4)
  - round 1: fixed @`9af3b87` — a named constant derived from a 30-day span
  - v2: verified @`095285c`

### D22 — Split-query configuration is written out separately in both database branches

- **What:** The same split-query setting was configured twice, once per database provider.
- **History:**
  - v1: found (QUAL-5)
  - round 1: fixed @`eb8a6f8` — a short note that the duplication is deliberate; extraction was not worth it
  - v2: verified @`095285c`

### D23 — The migration's Postgres arm and the model snapshot are exercised by no test

- **What:** The migration branches on the provider, but no test runs the Postgres arm and the model
  snapshot records the SQLite type. The next scaffolded migration under Postgres would produce a phantom
  column change. Four passes re-raised it and every one reached the same answer.
- **Evidence:** `Migrations/20260527102718_AddUploadThumbnailPath.cs:19` branches on the provider;
  `Migrations/PhotoPrintDbContextModelSnapshot.cs:707` records the SQLite type. The migration test runs
  the SQLite arm only, and its own comment concedes the Postgres arm is deferred (v9 verification detail).
- **Suggested fix:** Run the migration chain against Postgres in the three-environment phase and assert
  the column type, then regenerate the snapshot under that provider.
- **History:**
  - v1: found (DB-1) — the provider-aware column type was fixed @`bca68fa`; the test was deferred
  - v4: re-raised (M9, L10); a SQLite smoke test landed @`2945bda`, verified at v5
  - v6: re-raised (F24, F25) · v8: re-raised (F14) — both deferred to the three-environment phase
  - v9: deferral upheld; the provider branch is still present at `bd0d5fd`
  - 2026-08-11: row carried to `reviews/backlog.md`

### D24 — The HEIC magic-byte check accepts any ISO-BMFF container, including video

- **What:** Detection read only the four bytes spelling the container type and never the brand, so any
  MP4, MOV or M4A was classified as an image, buffered and written to disk before being refused.
- **History:**
  - v1: found (INPUT-1)
  - round 1: fixed @`f850f69` — the brand is checked, so generic containers are refused up front
  - v2: verified @`095285c` — a legitimate HEIC still fails to decode, tracked separately as D37

### D25 — The real image processor, and so the bomb guard, is mocked in every test

- **What:** No test exercised the real processor. The identify guard, the stream rewind and the decode
  itself ran in no test, so D2 could return silently.
- **History:**
  - v1: found (TEST-2)
  - round 1: fixed @`533996c` — tests against the real processor for bomb, valid and unreadable inputs
  - v2: verified @`095285c`

### D26 — Cache persistence is unproven because one database context is shared across both calls

- **What:** The test shared one context, so the entity stayed tracked with the path set even if the save
  were deleted. In production each request gets a fresh context and would regenerate every time.
- **History:**
  - v1: found (TEST-3)
  - round 1: fixed @`c245a1e` — the test drives the service through a separate context
  - v2: verified @`095285c` — deleting the save reddens it

### D27 — Cache-Control, 304, the migration and the cache-miss race have no tests

- **What:** Four surfaces would each have shipped a regression green: the exact cache directive, the
  conditional-GET path, the migration, and the concurrent cache miss.
- **History:**
  - v1: found (TEST-4)
  - round 1: fixed @`fad7693` — tests added alongside each matching fix; the migration test stayed with D23
  - v2: verified @`095285c`

### D28 — The storage contract assumes a rewindable stream with a readable length

- **What:** The stream rewind, the entity tag taken from the stream's length, and the per-hit existence
  check all assume a cheap local stream. A cloud provider need not give one. Nothing can trigger it while
  the only implementations are local, so it stayed a design constraint for the cloud-storage bolt.
- **Evidence:** `Controllers/UploadsController.cs:155` reads the stream length with no check that the
  stream can rewind; the v9 verification confirms the line is unchanged at `bd0d5fd`.
- **Suggested fix:** State rewindability and length on the storage interface, or take the entity tag from
  a stored size, and decide whether a cache hit should skip the existence check.
- **History:**
  - v1: found (CLOUD-1) — deferred; no cloud provider existed to trigger it
  - v4: re-raised (L11) · v6: re-raised (F8) · v8: re-raised (F24) — deferral upheld each time
  - v9: deferral upheld; only rewindable implementations exist at `bd0d5fd`
  - 2026-08-11: the cloud-storage target 043 closed on 2026-07-22 without taking this row
  - 2026-08-11: row carried to `reviews/backlog.md`

### D29 — The 50 MP decode cap refuses legitimate large-format print uploads

- **What:** D2's cap was correct defence but a behaviour change for a printing product: an A1 poster at
  300 dots per inch, and ordinary high-resolution phone photos, were refused at upload.
- **History:**
  - v2: found (NEW-1) — raised by the verification pass as a consequence of the D2 fix
  - round 2: fixed @`656c2fd` — owner raised the cap to 100 MP, which stays under the allocator cap
  - v3: verified @`f8b1325`

### D30 — A restored upload is discarded on any non-401 preview error, including a passing blip

- **What:** A transient server error or a dropped connection permanently erased a completed upload and
  rewrote session storage.
- **History:**
  - v2: found (NEW-2)
  - round 2: fixed @`5712aad` — only a definitive 404 drops the entry
  - v3: verified @`f8b1325`

### D31 — Nothing reclaims a thumbnail written between the cleanup job's read and its commit

- **What:** The preview writes a thumbnail file and its row separately from the cleanup job's read,
  delete and soft-delete. A thumbnail written inside that window is skipped by both sides and stays on
  disk with no row referencing it. Every pass agreed the honest fix is a periodic sweep over storage.
- **Evidence:** `BackgroundJobs/UploadCleanupJob.cs:101` still gates the delete on the row's recorded
  thumbnail path; the v9 verification confirms it at `bd0d5fd`. Two shapes were recorded: the ordinary interleaving
  (v6 F4) and a hard kill between the file write and the commit (v8 F18).
- **Suggested fix:** A periodic sweep that lists stored keys with no live row, or a conditional atomic
  update plus a cleanup that always deletes the derivable key.
- **History:**
  - v2: found (NEW-3) — deferred to the cloud-storage bolt, where the storage lifecycle is redesigned
  - v5: the M1 residual (V5-1) was ruled a wont-fix backed by this row
  - v6: re-raised (F4) · v8: re-raised (F1, F18) — deferral upheld, no atomic update exists anywhere
  - v9: deferral upheld; the non-atomic re-read is verbatim present at `bd0d5fd`
  - 2026-08-11: the cloud-storage target 043 closed on 2026-07-22 without taking this row
  - 2026-08-11: row carried to `reviews/backlog.md`

### D32 — Stored keys use the operating system's separator instead of a forward slash

- **What:** Path joining produced back-slashed keys on Windows. Self-consistent per machine, but a key
  written on Windows would not read on Linux and would not map to a cloud object name.
- **History:**
  - v2: found (NEW-4)
  - round 2: fixed @`e3a77d9` — keys are forward-slashed, with the first tests for the real storage service
  - v3: verified @`f8b1325`

### D33 — Image decode has no total or concurrent memory bound, so many large images exhaust memory

- **What:** Each cache miss decoded up to about 400 MB. The rate limiter counts requests, not cost, and
  its per-address partition is easy to spread, so many concurrent first previews could exhaust the host.
- **History:**
  - v4: found (M3)
  - round 4: fixed @`aa6639c` — a process-wide decode limiter with a configurable slot count
  - v5: verified @`6c4f334`
  - v6: the limiter's default was found still unsafe and recorded separately as D61

### D34 — The cache-fill write races the cleanup job and strands a thumbnail on the dead row

- **What:** The preview writes the thumbnail path onto a row the cleanup job is soft-deleting. Cleanup
  revisits only live rows, so the file stays on disk forever. The fix closed the stated ordering; a
  narrower symmetric window remains because the file and the row are not written together.
- **Evidence:** `Services/UploadService.cs:216` holds a non-atomic liveness re-read rather than a
  conditional update; the v9 verification records zero conditional updates anywhere under `src/` at `bd0d5fd`.
- **Suggested fix:** A conditional update that sets the path only while the row is live, deleting the
  just-written file when it matches nothing. It needs a database provider the tests do not use today.
- **History:**
  - v4: found (M1) — the residual of the D5 deterministic-key fix
  - round 4: fixed @`4d4d998` — a liveness re-read after the write, deleting the file if the row died
  - v5: verified @`6c4f334` with a documented residual (V5-1), backed by D31 rather than reopened
  - v6: re-raised (F4) · v8: re-raised (F1) — deferred both times to the cloud-storage orphan sweep
  - v9: deferral upheld. 2026-08-11: 043 closed without taking it; row carried to `reviews/backlog.md`

### D35 — Two first previews at once collide on an exclusive file create and return 500

- **What:** The deterministic key from the D5 fix meant two simultaneous first previews wrote the same
  path. The file create opens exclusively, so the second threw an unmapped exception.
- **History:**
  - v4: found (M2) — the residual of the D5 deterministic-key fix
  - round 4: fixed @`aad083d` — write to a unique temporary file, then move it into place
  - v5: verified @`6c4f334`
  - v6: the move itself was found to race on Windows and recorded separately as D75

### D36 — A bomb sent to the batch endpoint never emits the reserved alert event

- **What:** The batch endpoint caught the rejection itself, so it never reached the middleware that emits
  the reserved bomb event. Alerting covered the single-file route only.
- **History:**
  - v4: found (M4)
  - round 4: fixed @`f1c4ade` — the batch catch emits the same event with the dimensions
  - v5: verified @`6c4f334`

### D37 — HEIC is accepted but no decoder exists, so every HEIC upload fails

- **What:** A phone photo in the default Apple format was accepted, buffered and written to disk, then
  refused at decode because the image library ships no decoder for it.
- **History:**
  - v4: found (M5) — the residual left open when D24 fixed the container check
  - round 4: fixed @`80379f6` and @`63b815a` — the format is no longer advertised, backend and interface
  - v5: verified @`6c4f334`
  - v6: the missing scope document for this removal was recorded separately as D64

### D38 — A cache miss whose original is gone returns 500 instead of a clean 4xx

- **What:** When the original blob was removed but the row survived, the read threw a missing-file
  exception outside the catch and absent from the exception map, so the response was a raw 500.
- **History:**
  - v4: found (M6) — the residual of the D5 deterministic-key fix
  - round 4: fixed @`fea0d45` — the missing file becomes a 404, which also lets the interface drop the entry
  - v5: verified @`6c4f334`

### D39 — An unreadable stored image is logged without its storage path or its cause

- **What:** The preview path threw a bare exception with no log, dropping the storage path and the
  original error, so a corrupted stored file looked exactly like a bad upload.
- **History:**
  - v4: found (M7)
  - round 4: fixed @`2b22e25` — a warning carrying the storage path, and the original error passed inward
  - v5: verified @`6c4f334`

### D40 — A preview kept on 403 leaves an upload the guest can never put in a cart

- **What:** After a guest session expired, a restored upload that answered 403 was kept on screen. The
  guest could not add it to a cart and had no way to clear it.
- **History:**
  - v4: found (M8)
  - round 4: fixed @`1bdb21b` — a 403, and a still-failing 401 after re-init, drop the entry
  - v5: verified @`6c4f334`

### D41 — No test proves the bomb rejection deletes the file it already stored

- **What:** The upload path stores the file before checking dimensions and deletes it on rejection.
  Nothing asserted the delete, so a leak could ship green.
- **History:**
  - v4: found (M10)
  - round 4: fixed @`7a7170e` — the dimensions test asserts the delete once
  - v5: verified @`6c4f334`

### D42 — The one-frame decode cap is proven only through the internal helper, not the public call

- **What:** The frame cap is the defence against an animated-image bomb. The only test calls the internal
  helper by reflection, so dropping the cap from the public path would not redden anything.
- **Evidence:** `Services/ImageProcessor.cs:81`; the frame-cap test at `Tests/…/ImageProcessorTests.cs:154`
  calls the internal helper. the v9 verification confirms the cap is still applied at the new call site at `bd0d5fd`.
- **Suggested fix:** Drive a multi-frame image through the public thumbnail call and assert one frame.
- **History:**
  - v4: found (M11) — the cap had no coverage at all
  - round 4: fixed @`1108d47` — a reflection test proving a three-frame image decodes to one
  - v5: verified @`6c4f334`
  - v8: re-raised (F16) — the coverage is on the helper, not the public path; deferred to the next pass
  - v9: deferral upheld; the pass it waited for never ran
  - 2026-08-11: row carried to `reviews/backlog.md`

### D43 — The cache-hit path checks then reads, so a vanished file gives 500 and costs a round-trip

- **What:** The hot read asked storage whether the file existed and then read it. A file removed between
  the two answers gave a 500, and every hit paid a second storage call.
- **History:**
  - v4: found (L1)
  - round 4: fixed @`dfb8f56` — read directly and regenerate if the file is missing
  - v5: verified @`6c4f334`

### D44 — A vanished cache file quietly regenerates with no signal

- **What:** If a stored thumbnail disappeared, the service regenerated it and said nothing, so storage
  faults and operator deletions were invisible.
- **History:**
  - v4: found (L3)
  - round 4: fixed @`dfb8f56` — a distinct event on the regeneration path
  - v5: verified @`6c4f334`

### D45 — A thumbnail orphaned by a failed commit emits no signal

- **What:** If the row save failed after the file was written, the file was orphaned silently.
- **History:**
  - v4: found (L4)
  - round 4: fixed @`9b0bc81` — an event plus a best-effort delete before the error is re-thrown
  - v5: verified @`6c4f334`

### D46 — The preview GET writes to the database, which fails against a read replica

- **What:** A cache miss on a read endpoint issues a write. There is no read replica today, so nothing
  can fail, but the endpoint is no longer safe to route to one. The round took the finding's minimum
  option and documented the constraint at the write site instead of moving the work off the read path.
- **Evidence:** `Services/UploadService.cs:166` performs the save inside the preview read; the constraint
  note landed at `8466658`.
- **Suggested fix:** Move the cache fill off the read path, as a queued job or a write-endpoint step,
  when read-replica routing is introduced.
- **History:**
  - v4: found (L5) — deferred with the constraint documented at `8466658`
  - v5: the deferral was upheld
  - v6, v8: not re-raised; no later pass revisited it
  - 2026-08-11: row carried to `reviews/backlog.md`

### D47 — The batch-rejection log prints the raw client filename with no length limit

- **What:** The warning added by D15 wrote the client's filename straight into the log, with no bound on
  length and no stripping of control characters.
- **History:**
  - v4: found (L6) — the residual of the D15 fix
  - round 4: fixed @`158b733` — control characters stripped and the name capped at 128 characters
  - v5: verified @`6c4f334`

### D48 — Any unauthenticated 401 wipes the whole guest session, checkout contact details included

- **What:** The D13 fix cleared the guest session on every unauthenticated 401, anywhere in the
  application. That removed the whole stored entry, including the contact details the visitor typed at
  checkout, not only the stale token.
- **History:**
  - v4: found (L7) — disputed, because it read as a request to revert the verified D13 decision
  - v6: re-raised (F2), sharpened to the contact-details loss rather than the login redirect
  - round 6: fixed @`069f5ea` — only the token field is dropped; contact details survive
  - v7: verified @`79c2eda` — reverting to the whole-entry removal reddens the preserve test
  - v8: a second writer was found to undo the same preservation and is recorded separately as D86

### D49 — The one-shot retry guard is untested for a retry that still fails

- **What:** The retry after a 401 is limited to one attempt by a flag. No test drove a retry that failed
  again, so a regression would appear as an endless loop rather than a red test.
- **History:**
  - v4: found (L8)
  - round 4: fixed @`1bdb21b` — a test pinning exactly two attempts then an error
  - v5: verified @`6c4f334`

### D50 — Guest-session recovery after a failed init is untested

- **What:** The shared in-flight init from D11 resets when it settles, so a caller after a failure should
  start a new one. Every specification mocks the init as succeeding, so the recovery path never runs.
- **Evidence:** `UI/…/format-selector-page.ts:214`; all twelve specifications supply a successful init
  (v8 F17). the v9 verification confirms the gap at `bd0d5fd`.
- **Suggested fix:** A specification where the first init fails and the next call starts a fresh one.
- **History:**
  - v4: found (L9) — the re-init-after-settle path had no test
  - round 4: fixed @`1bdb21b` — a test for re-init after a completed init
  - v5: verified @`6c4f334`
  - v8: re-raised (F17) — the failure branch is still uncovered; deferred to the next pass
  - v9: deferral upheld; the pass it waited for never ran
  - 2026-08-11: row carried to `reviews/backlog.md`

### D51 — The bomb log test asserts the event name but not the dimensions the event exists to carry

- **What:** The event exists so operators can see how big the refused image claimed to be. The test
  checked only the name, so the dimensions could be dropped and stay green.
- **History:**
  - v4: found (L12)
  - round 4: fixed @`c0c07c7` — distinct width and height are asserted
  - v5: verified @`6c4f334`

### D52 — The 512 MB allocator backstop throws an unmapped exception, giving 500, and is untested

- **What:** When the allocator cap trips, the image library throws its own memory exception. It was
  absent from the exception map, so the response was a 500, and nothing tested it.
- **History:**
  - v4: found (L13)
  - round 4: fixed @`e1c56c4` — the type maps to 422, with a test
  - v5: verified @`6c4f334`
  - v6: the missing bomb event on this branch was recorded separately as D62

### D53 — The recognised-but-broken image path to 422 is untested

- **What:** A file recognised as an image but broken inside takes a different branch than an unknown
  format. That branch had no test.
- **History:**
  - v4: found (L14)
  - round 4: fixed @`ec8a894` — a corrupt image test, proven by narrowing the catch until it reddens
  - v5: verified @`6c4f334`

### D54 — Preview object URLs are never released, so every restore and retry leaks one

- **What:** Object URLs minted for previews were never released, so the browser held the bytes for the
  life of the tab across restores and retries.
- **History:**
  - v4: found (C1)
  - round 4: fixed @`af5cf74` — URLs are released on remove, drop, cart clear and page destroy
  - v5: verified @`6c4f334`
  - v8: two remaining leak paths were recorded separately as D92 and D95

### D55 — The upload error message is written out at three sites

- **What:** The same user-facing error string existed in three places.
- **History:**
  - v4: found (C2)
  - round 4: fixed @`af5cf74` — one shared constant
  - v5: verified @`6c4f334`

### D56 — The self-heal seam is tested only with each half mocked

- **What:** The interceptor clears the token and the page re-inits. Each half was tested with the other
  mocked, so a divergence between them would pass.
- **History:**
  - v4: found (C3)
  - round 4: fixed @`f444a81` — a test with the real authentication service on both sides of the seam
  - v5: verified @`6c4f334`

### D57 — The implementation walkthrough contradicts the shipped code

- **What:** The walkthrough described the insecure cache directive D1 replaced, the tracking behaviour
  D18 restored, and the pre-fix migration. Copying from it would have reintroduced D1.
- **Evidence:** `memory-bank/…/implementation-walkthrough.md:32`, with three further contradictions found
  during verification: the provider-aware migration line, the decode-limit name, and the dimension text.
- **Suggested fix:** None outstanding. What is missing is a pass confirming the completion held.
- **History:**
  - v4: found (C4)
  - round 4: fixed @`6c4f334` — the three named contradictions plus adjacent drift in the same document
  - v5: verified in part; three residual contradictions were named (V5-2) and were outside the fix
  - round 5: completed @`838c9b6`. No pass verified the completion; a document fix needs no re-review
  - v6: a blinded pass at `6c0ed93`, after this commit, did not re-find this document
  - 2026-08-11: left at `fixed` — the work landed, the confirmation never did

### D58 — Story 003 says 54 MP is refused while the shipped cap is 100 MP

- **What:** The acceptance criterion still quoted the figure from before D29 raised the cap.
- **History:**
  - v4: found (C5)
  - round 4: fixed @`6c4f334` — the criterion quotes 110 MP over the 100 MP cap
  - v5: verified @`6c4f334`

### D59 — Story 001 names `varchar(500)` and `StoragePath`; the column shipped as `varchar(512)` and `FilePath`

- **What:** The story's column width and sibling column name did not match what shipped.
- **History:**
  - v4: found (C6)
  - round 4: fixed @`6c4f334` — the story matches the shipped column
  - v5: verified @`6c4f334`
  - v8: re-raised (F27) — the same stale width survived in two more documents
  - round 8: fixed @`76d0b6a` and @`00b0d39` — the second document was caught by the round's micro-review
  - v9: verified @`bd0d5fd` — a repository-wide search finds no remaining 500

### D60 — The thumbnail shipped at 300 px while the stories and the brief say 800 px

- **What:** The code and the requirements disagreed on the thumbnail's longest side.
- **History:**
  - v4: found (C7)
  - round 4: fixed @`28aff33` — the owner chose the code's side: 300 px raised to 800 px
  - v5: verified @`6c4f334`, with one stale interface comment left behind (V5-3)
  - round 5: the interface comment corrected @`838c9b6`

### D61 — The decode limiter defaults to the core count and ignores memory, so it still exhausts memory

- **What:** The D33 limiter defaulted its slot count to the processor count. On a host with many cores
  and little memory the default still allowed enough concurrent decodes to exhaust it.
- **History:**
  - v6: found (F1) — the residual of the D33 fix
  - round 6: fixed @`548663f` — the default is the smaller of the core count and memory divided per slot
  - v7: verified @`79c2eda` — returning the body to the core count reddens two of the three new tests
  - v8: the budget's blind spot for upload buffering was recorded separately as D96

### D62 — A bomb caught by the allocator backstop never emits the reserved bomb event

- **What:** D52 mapped the allocator's memory exception to 422 but did not emit the reserved bomb event,
  so a bomb caught by the backstop rather than the pixel guard was invisible to alerting.
- **History:**
  - v6: found (F5) — the residual of the D52 fix
  - round 6: fixed @`6b7ce09` — the middleware emits the event, tagged with which guard caught it
  - v7: verified @`79c2eda`
  - v8: the third copy of the event template was recorded against D81

### D63 — A logged-in user whose token expired is re-attributed to a throwaway guest

- **What:** The self-heal ran on any 401, including one from a signed-in user whose token had expired.
  That minted an anonymous guest session and attributed the user's work to it.
- **History:**
  - v6: found (F3)
  - round 6: fixed @`39b0098` — guest status is captured before the request and the self-heal is gated on it
  - v7: verified @`79c2eda` — removing the gate reddens both new specifications

### D64 — The HEIC removal is missing from the bolt's bundled-scope document

- **What:** D37 stopped accepting a previously advertised upload format. That is a contract change and it
  was the third unlisted change in a bolt already flagged for bundling.
- **History:**
  - v6: found (F6)
  - round 6: fixed @`6e577fd` — recorded as a bundled change with a retroactive criterion
  - v7: verified @`79c2eda` — the document matches the shipped behaviour, claiming no check the code lacks

### D65 — The test walkthrough certifies a `Cache-Control` value the code never emits

- **What:** A second document, separate from D57, still stated the public cache directive that D1
  replaced, and presented it as verified.
- **History:**
  - v6: found (F7)
  - round 6: fixed @`79c2eda` — the document states the shipped private directive and the pinning assertion
  - v7: verified @`79c2eda` — checked against the controller and the integration test

### D66 — `ExistsAsync` was added to the storage interface but nothing in production calls it

- **What:** The D43 fix removed the only production caller of the existence check, leaving an interface
  member every implementation must provide and nothing uses. Test stubs still pre-answer it, which would
  hide a reintroduced check-then-read.
- **Evidence:** `IStorageService.cs:21` has no production caller (v6 F9, v8 F9); the inert stubs sit at
  `Tests/…/UploadServiceTests.cs:296` (v8 F26).
- **Suggested fix:** Drop the member, or record why the cloud provider will need it, and delete the
  stubs either way.
- **History:**
  - v6: found (F9) — deferred to the cloud-storage bolt, where the interface changes
  - v8: re-raised twice (F9 for the member, F26 for the stubs); deferral upheld
  - v9: deferral upheld at `bd0d5fd`
  - 2026-08-11: the cloud-storage target 043 closed on 2026-07-22 without taking this row
  - 2026-08-11: row carried to `reviews/backlog.md`

### D67 — Every cache-miss preview pays an extra database round-trip to spot the soft-delete race

- **What:** The D34 fix re-reads the row after the write to see whether it is still live. That is an extra
  query on every miss, and it disappears only when D34's conditional update lands.
- **Evidence:** `Services/UploadService.cs:216`; the row is already loaded before the write (v8 F11).
- **Suggested fix:** Fold it into D34's conditional update, which removes the second read.
- **History:**
  - v6: found (F17) — the residual of the D34 fix; deferred, paired with D34
  - v8: re-raised (F11); deferral upheld and paired again
  - v9: deferral upheld at `bd0d5fd`
  - 2026-08-11: the cloud-storage target 043 closed on 2026-07-22 without taking this row
  - 2026-08-11: row carried to `reviews/backlog.md`

### D68 — Nothing reports how saturated or how queued the decode limiter is

- **What:** The limiter added by D33 is a hard cap on throughput. It emits nothing, so operators cannot
  see queueing or saturation, and a wrongly sized slot count looks like ordinary slowness.
- **Evidence:** `ImageDecodeLimiter.cs:30` — acquiring a slot only waits and returns it (v8 F15).
- **Suggested fix:** Count waits and record wait time, or expose the free slot count.
- **History:**
  - v6: found (F15) — deferred to the next pass
  - v8: re-raised (F15); deferral upheld
  - v9: deferral upheld at `bd0d5fd`; the next pass it waited for never ran
  - 2026-08-11: row carried to `reviews/backlog.md`

### D69 — No test proves the decode slot is released when the decode throws

- **What:** If a slot leaks on a failing decode, the limiter drains to zero and every later preview
  blocks. Today's code releases it, but nothing pins that.
- **Evidence:** `Services/ImageProcessor.cs:67`; the failed-decode tests assert only the exception (v8 F12).
- **Suggested fix:** A test that fails a decode repeatedly and asserts the slot count returns to full.
- **History:**
  - v6: found (F21) — recorded as plausible, since the current code releases the slot; deferred
  - v8: re-raised (F12); deferral upheld
  - v9: deferral upheld at `bd0d5fd`; the next pass it waited for never ran
  - 2026-08-11: row carried to `reviews/backlog.md`

### D70 — The allocator-exception-to-422 mapping is proven only by an injected instance

- **What:** The map in D52 keys on the exact exception type. The test injects an instance of that type,
  so it would still pass if a library upgrade started throwing a different one.
- **Evidence:** `Middleware/ExceptionHandlerMiddleware.cs:26` maps by exact type (v6 F22).
- **Suggested fix:** Reach the mapping through a real oversized decode, or pin the library version with
  a test that fails when the thrown type changes.
- **History:**
  - v6: found (F22) — recorded as plausible; deferred to the next pass
  - v7: deferral upheld; the condition is verbatim present at `79c2eda`
  - v8: not re-raised. 2026-08-11: row carried to `reviews/backlog.md`

### D71 — A failed thumbnail delete in the cleanup job is untested and silently leaks the file again

- **What:** The D4 delete is inside a catch that counts file errors and soft-deletes the row anyway. When
  the delete fails, the file leaks exactly as before D4, and no test covers that branch.
- **Evidence:** `BackgroundJobs/UploadCleanupJob.cs:114` (v6 F10).
- **Suggested fix:** A test with a throwing storage service, asserting the failure is counted, plus the
  D31 sweep as the durable backstop.
- **History:**
  - v6: found (F10) — deferred, same orphan family as D31
  - v7: deferral upheld; the condition is verbatim present at `79c2eda`
  - v8: not re-raised. 2026-08-11: row carried to `reviews/backlog.md`

### D72 — Parallel preview 401s defeat the init sharing, and a late 401 wipes a fresh token

- **What:** A page restoring several previews at once fires several 401s. They arrive after the shared
  init has settled, so each starts another one, and a late arrival clears the token just minted.
- **Evidence:** `UI/…/format-selector-page.ts:381` (v6 F16). The grid still ends up correct; the cost is
  wasted sessions and churn.
- **Suggested fix:** Share one re-init across a burst of 401s, or ignore a 401 raised before the current
  token was issued.
- **History:**
  - v6: found (F16) — deferred; the visible outcome is unchanged
  - v7: deferral upheld at `79c2eda`
  - v8: not re-raised. 2026-08-11: row carried to `reviews/backlog.md`

### D73 — The logged-in 401-during-upload path has no test

- **What:** D63's path — a signed-in user whose token expires mid-upload — had no coverage, so the
  interceptor's logout could race the page's retry unnoticed.
- **History:**
  - v6: found (F18)
  - round 6: fixed @`39b0098` — the D63 regression tests assert no guest session is minted and no retry runs
  - v7: verified @`79c2eda` — the reclassification was checked and matches what the finding asked for

### D74 — The guest-init error path in `onFilesAccepted` is untested, so files hang as uploading

- **What:** If the guest init fails when files are dropped, the files stay marked uploading with no error
  shown. Nothing tests that branch.
- **Evidence:** `UI/…/format-selector-page.ts:176` (v6 F19).
- **Suggested fix:** A specification with a failing init that asserts the files show an error rather than
  staying in progress.
- **History:**
  - v6: found (F19) — deferred; outside that round's recommended set
  - v7: deferral upheld at `79c2eda`
  - v8: not re-raised. 2026-08-11: row carried to `reviews/backlog.md`

### D75 — Moving a file onto a shared key races other writers on Windows and returns 500

- **What:** The D35 fix moves a temporary file onto the shared key. On Windows that move can fail while
  another writer holds the target, giving an unmapped 500. On Linux the rename is atomic, so production
  is unaffected and only development machines see it.
- **Evidence:** `Services/LocalStorageService.cs:45` (v6 F13, v8 F10).
- **Suggested fix:** Retry the move briefly, or treat a failed move whose target already exists as success.
- **History:**
  - v6: found (F13) — the residual of the D35 fix; deferred as development-only
  - v8: re-raised (F10); deferral upheld
  - v9: deferral upheld at `bd0d5fd`
  - 2026-08-11: row carried to `reviews/backlog.md`

### D76 — A cleanup delete fails against an open read handle on Windows and leaves an orphan

- **What:** A cache-hit read holds the file open. On Windows the cleanup delete of that file fails, the
  row is soft-deleted anyway, and the file is orphaned. Linux unlinks regardless, so production is safe.
- **Evidence:** `Services/LocalStorageService.cs`, the read and delete pair (v6 F14).
- **Suggested fix:** The D31 sweep covers it; otherwise open reads with sharing that permits deletion.
- **History:**
  - v6: found (F14) — deferred as development-only
  - v7: deferral upheld at `79c2eda`
  - v8: not re-raised. 2026-08-11: row carried to `reviews/backlog.md`

### D77 — The pixel-area cap ignores bytes per pixel, so a legitimate 16-bit PNG is refused

- **What:** The cap counts pixels, not bytes. A legitimate deep-colour image under the pixel cap decoded
  at twice the expected bytes per pixel, tripped the allocator backstop, and could never be previewed —
  every retry failed the same way.
- **History:**
  - v6: found (F12) as a 🟡; deferred to the next pass
  - v8: re-raised (F7) and raised to 🟠 once the permanent failure was traced
  - round 8: fixed @`bd0d5fd` — the decode is pinned to four bytes per pixel, bounding any allowed image
  - v9: verified @`bd0d5fd` — restoring the automatic pixel type reddens the bit-depth test at 64 bits
  - v8: the round's own check corrected the finding's claim of a false bomb alert on this path

### D78 — The pixel guard is skipped when the identify call returns null

- **What:** Suspected fail-open: if the identify call returned nothing, the size guard was skipped and
  the full decode ran. It cannot happen with the shipped library version, which throws instead.
- **History:**
  - v6: found (F23) — recorded as plausible but dead today
  - v8: refuted @`e2093bd` — the library returns a value or throws, so the branch is unreachable
  - 2026-08-11: kept as a refuted candidate; it becomes live only if the library changes that behaviour

### D79 — Storage faults and cancellation are reported as an unreadable image

- **What:** A broad catch turns storage errors, input-output errors and cancelled requests into the same
  "cannot read this image" answer, so a storage outage looks like a wave of bad uploads.
- **Evidence:** `Services/ImageProcessor.cs:56` (v6 F11).
- **Suggested fix:** Let cancellation through, and log or map storage errors separately.
- **History:**
  - v6: found (F11) — deferred to the next pass
  - v7: deferral upheld at `79c2eda`
  - v8: not re-raised. 2026-08-11: row carried to `reviews/backlog.md`

### D80 — The implementation plan's acceptance criteria are stale after documented substitutions

- **What:** The plan still lists the public cache directive and the per-axis dimension cap, both replaced
  during the first fix round. Same drift family as D57 and D65, in a third document.
- **Evidence:** `memory-bank/…/implementation-plan.md:59` (v6 F20).
- **Suggested fix:** Restate the criteria to match the shipped area cap and private directive.
- **History:**
  - v6: found (F20) — deferred; that round's document fixes were scoped to D64 and D65
  - v7: deferral upheld and judged a defensible scope call
  - v8: not re-raised. A different stale value in the same file was fixed under D59
  - 2026-08-11: row carried to `reviews/backlog.md`

### D81 — The bomb-alert log template is copied across the controller and the middleware

- **What:** The reserved bomb event name and its fields are written out at three sites after the D62 fix,
  and the controller copy omits the field naming which guard caught it. A rename must touch all three.
- **Evidence:** `Controllers/UploadsController.cs:122` and two sites in the middleware (v8 F25).
- **Suggested fix:** One helper emitting the event, and add the missing field to the batch route.
- **History:**
  - v6: found (F26) — deferred; the same round's D62 fix added the third site
  - v8: re-raised (F25) with the missing field noted; deferral upheld
  - v9: deferral upheld at `bd0d5fd`
  - 2026-08-11: row carried to `reviews/backlog.md`

### D82 — `dropRestoredEntry` repeats the body of `onRemoveUpload` word for word

- **What:** The D40 fix added a second function whose body duplicates an existing one.
- **Evidence:** `UI/…/format-selector-page.ts:420` (v6 F27).
- **Suggested fix:** Call the existing function.
- **History:**
  - v6: found (F27) — the residual of the D40 fix; deferred
  - v7: deferral upheld at `79c2eda`
  - v8: not re-raised. 2026-08-11: row carried to `reviews/backlog.md`

### D83 — The client-abort log reads the raw correlation-id item instead of the accessor

- **What:** One branch reads the correlation id out of the request's item bag directly while every
  sibling uses the accessor, so the two can drift.
- **Evidence:** `Middleware/ExceptionHandlerMiddleware.cs:64` (v6 F28).
- **Suggested fix:** Use the accessor.
- **History:**
  - v6: found (F28) — deferred even though the D62 fix touched the same file, to keep that round's scope
  - v7: deferral upheld at `79c2eda`
  - v8: not re-raised. 2026-08-11: row carried to `reviews/backlog.md`

### D84 — Storage save and delete traces sit at Debug under an Information floor, so they never emit

- **What:** The same class of defect as D16, one file over: storage traces are written at a level the
  configured floor filters out everywhere.
- **Evidence:** `Services/LocalStorageService.cs:53` (v6 F29).
- **Suggested fix:** Raise them to Information, or add a per-source override.
- **History:**
  - v6: found (F29) — deferred
  - v7: deferral upheld at `79c2eda`
  - v8: not re-raised. 2026-08-11: row carried to `reviews/backlog.md`

### D85 — The global split-query default mis-pages a collection include that has no tiebreaker

- **What:** The bundled change C turned on split queries for every query. A paged query that includes a
  collection and orders by a non-unique column can then return a page whose child rows come from a
  different ordering, so an administrator's order list loses items.
- **History:**
  - v8: found (F2) — the pass's headline new finding, on a surface no earlier pass audited
  - round 8: fixed @`ac0485b` — a unique tiebreaker on both paged queries; a sweep found no other site
  - v9: verified @`bd0d5fd` — dropping the tiebreaker reddens the tied-timestamp paging test
  - v8: recorded limitation — the in-memory test provider does not split queries, so the missing-items
    symptom itself can only be reproduced on Postgres. That verification rides with D23

### D86 — `storeSession` overwrites the contact details the earlier fix preserved

- **What:** The D48 fix stopped the interceptor wiping contact details, but the re-init path writes the
  session again with empty contact fields, overwriting what was preserved. The fix was defeated one
  function away, on a path verified clean a round earlier.
- **History:**
  - v8: found (F3) — the residual that showed fixes were still making new defects
  - round 8: fixed @`62a33cd` — the write merges: an empty incoming field keeps the stored value
  - v9: verified @`bd0d5fd` — the pre-fix blind write reddens both merge tests

### D87 — The bomb test asserts the base exception, so the alert can regress while tests stay green

- **What:** The upload-time bomb test asserted the general rejection type, not the specific bomb type the
  alert emitters key on. Narrowing the thrown type would have silently disabled the alert.
- **History:**
  - v8: found (F4)
  - round 8: fixed @`521fa15` — the test asserts the specific type and the dimensions
  - v9: verified @`bd0d5fd` — throwing the base type reddens it

### D88 — A lost original blob is logged as a plain 404, with no distinct signal

- **What:** The D38 fix turned a missing original into a 404. A missing blob under a live row is a
  storage-integrity incident, and it now looked exactly like a request for an unknown id.
- **History:**
  - v8: found (F5)
  - round 8: fixed @`521fa15` — a distinct warning on the lost-original branch
  - v9: verified @`bd0d5fd` — removing the warning reddens the log assertion

### D89 — The soft-delete-race deletion leaves database and file state silently out of step

- **What:** When D34's liveness re-read finds the row dead it deletes the thumbnail, but the dead row
  keeps the path it just wrote and nothing says so.
- **History:**
  - v8: found (F6)
  - round 8: fixed @`521fa15` — a distinct warning on that branch
  - v9: verified @`bd0d5fd`. The stale path on the dead row is D34's deferred work, not this signal

### D90 — The 30-day private preview cache is recoverable on a shared device

- **What:** The D1 fix made the preview cache private, which stops shared caches but not the browser's
  own store. On a shared computer profile the photo stays recoverable for the whole 30 days.
- **Evidence:** `Controllers/UploadsController.cs:26` sets a 30-day private lifetime (v8 F8).
- **Suggested fix:** An owner decision, not a reflexive patch. Requiring revalidation keeps the stored
  bytes and turns a repeat view into one cheap conditional request, since the endpoint already sends a
  validator, so the cost is a round-trip per view rather than losing the cache.
- **History:**
  - v8: found (F8) — deferred as a design call and flagged for the owner rather than dropped
  - v9: deferral upheld, with the cost corrected: the fix does not defeat the cache, it revalidates it
  - 2026-08-11: the owner decision was never taken; row carried to `reviews/backlog.md`

### D91 — Bundled change C has no criterion or test and is labelled as changing no behaviour

- **What:** The bundled split-query default was documented as changing no behaviour. D85 shows it changes
  how production queries execute, and it had neither an acceptance criterion nor a test.
- **History:**
  - v8: found (F23)
  - round 8: fixed @`76d0b6a` — the document states it is a query-execution change and carries a
    retroactive criterion requiring a unique ordering tiebreaker, naming the D85 test
  - v9: verified @`bd0d5fd` — the cited test exists and the Postgres limitation is stated correctly

### D92 — A restore preview that resolves after the page is destroyed leaks an object URL

- **What:** A preview request still in flight when the page is destroyed resolves afterwards and mints an
  object URL nothing will ever release. One leak per navigation away mid-restore.
- **Evidence:** `UI/…/format-selector-page.ts:404`; no teardown on the subscription (v8 F21).
- **Suggested fix:** Tie the subscription to the component's lifetime.
- **History:**
  - v8: found (F21) — the residual of the D54 fix; deferred to the next pass
  - v9: deferral upheld at `bd0d5fd`; the next pass it waited for never ran
  - 2026-08-11: row carried to `reviews/backlog.md`

### D93 — No end-to-end test reaches the bomb-to-422 path because the integration fake pins 800×600

- **What:** The integration test posts an oversized image, but dependency injection resolves a fake image
  processor that always reports 800×600, so the guard never runs and the test proves nothing about it.
- **Evidence:** `Tests/…/UploadFactory.cs:239` supplies the fake (v8 F13).
- **Suggested fix:** Let the integration test reach the real processor, or make the fake report the
  dimensions the test posts.
- **History:**
  - v8: found (F13) — deferred to the next pass
  - v9: deferral upheld at `bd0d5fd`; the next pass it waited for never ran
  - 2026-08-11: row carried to `reviews/backlog.md`

### D94 — A guest 401 away from the upload page is a silent dead end

- **What:** The self-heal lives on the upload page. A guest whose token expires elsewhere — at the payment
  step, for example — gets a 401 that clears the token and nothing else: no re-init, no message, no
  navigation.
- **Evidence:** `UI/…/error.interceptor.ts:33`; the re-init is on the upload page only (v8 F19).
- **Suggested fix:** Re-init centrally in the interceptor, or show a message telling the visitor what
  happened.
- **History:**
  - v8: found (F19) — deferred; outside that round's recommended set
  - v9: deferral upheld at `bd0d5fd`
  - 2026-08-11: row carried to `reviews/backlog.md`

### D95 — `localUrl()` mints an untracked object URL on every change-detection cycle

- **What:** For a photo still held in the session, the thumbnail component creates a fresh object URL each
  time the view is checked, and none of them are tracked or released.
- **Evidence:** `UI/…/photo-thumbnail.component.ts:86` (v8 F20).
- **Suggested fix:** Create the URL once per file and release it when the component is destroyed.
- **History:**
  - v8: found (F20) — the residual of the D54 fix; deferred to the next pass
  - v9: deferral upheld at `bd0d5fd`; the next pass it waited for never ran
  - 2026-08-11: row carried to `reviews/backlog.md`

### D96 — The decode memory budget ignores the upload buffering that shares the same memory

- **What:** The D61 budget divides host memory by a per-decode allowance, but uploads buffered in memory
  draw on the same pool, so the computed slot count can still exhaust the host.
- **Evidence:** `ImageDecodeLimiter.cs:30` — the budget counts decode slots only (v8 F22).
- **Suggested fix:** Subtract the upload buffering allowance from the memory the budget divides.
- **History:**
  - v8: found (F22) — the residual of the D61 fix; deferred as a configuration matter
  - v9: deferral upheld at `bd0d5fd`
  - 2026-08-11: row carried to `reviews/backlog.md`

### D97 — The conditional GET matches only an exact strong tag, so weak, list and `*` fall back to 200

- **What:** The endpoint compares the incoming validator to its own as plain text. A weak validator, a
  comma-separated list, or a wildcard never matches, so the 304 quietly becomes a full response and the
  cache saving is lost.
- **Evidence:** `Controllers/UploadsController.cs:158` (v8 F28).
- **Suggested fix:** Parse the header per the specification and add a test for each of the three forms.
- **History:**
  - v8: found (F28) — deferred; bandwidth only
  - v9: deferral upheld at `bd0d5fd`
  - 2026-08-11: row carried to `reviews/backlog.md`
