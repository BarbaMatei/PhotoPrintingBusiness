---
type: findings-detail
target: 042-thumbnail-cache
answers_review: review-v4.md
version: 4
note: Full scenario/fix/adversarial-evidence for ALL 32 v4 findings. review-v4.md details the 11 Mediums in prose and lists the Lows/Cleanups as one-liners; this file is the durable full record for every finding (esp. the Lows/Cleanups). IDs match review-v4 / resolution-v4 (M#=Medium, L#=Low, C#=Cleanup) and map to canonical defect IDs in ledger.md.
---

# Findings detail — Bolt 042 v4 (all 32)

Generated from the discovery-pass workflow output (deduped + convergence-weighted + adversarially
verified). Each entry: file:line · verdict · convergence (lenses) · confidence · scenario · fix ·
guard/trace evidence where kept.

### M1 [MEDIUM] Lazy GET-preview thumbnail write is non-atomic with the DB (no DeletedAt guard) and races the cleanup job into permanent orphaned thumbnails
src/PhotoPrint.API/Services/UploadService.cs:159 | confirmed | conv 3 | correctness, race, completeness-critic | c6
SCENARIO: Upload is old enough for cleanup. Preview reads it (DeletedAt null, ThumbnailPath null) and writes thumbs/o/u.jpg. Cleanup (candidates loaded earlier) deletes FilePath, skips the still-null ThumbnailPath, sets DeletedAt, saves. Preview's UPDATE (no DeletedAt guard) sets ThumbnailPath on the dead row. Cleanup only revisits DeletedAt==null rows, so the thumbnail leaks forever.
FIX: Guard the update (UPDATE ... WHERE Id=@id AND DeletedAt IS NULL; if 0 rows affected, delete the just-written thumb), or make cleanup always DeleteAsync the deterministic thumbs/{owner}/{id}.jpg key.
GUARD: No guard. UploadService.cs:164-166 marks only ThumbnailPath modified → UPDATE ... WHERE Id=@id, no DeletedAt clause; Upload has no concurrency token (Upload.cs, UploadConfiguration.cs). Read-time DeletedAt==null (line 138) precedes the race, doesn't constrain the write. Cleanup revisits only DeletedAt==null (UploadCleanupJob.cs:74), so the thumbnail on the dead row leaks. Real.

### M2 [MEDIUM] Concurrent first preview of the same upload collides on exclusive File.Create (FileShare.None) -> 500
src/PhotoPrint.API/Services/LocalStorageService.cs:32 | confirmed | conv 2 | correctness, race | c6
SCENARIO: Two simultaneous first-preview requests for one upload (double-click, gallery re-render, browser prefetch). Both miss the cache and SaveAsync the same deterministic key thumbs/o/u.jpg. File.Create opens exclusively (FileShare.None); the second throws IOException, which is unmapped -> 500. Original uploads never hit this because they used unique GUID keys.
FIX: Write to a unique temp file then atomically move/overwrite into the final key so concurrent writers don't collide; optionally map storage IOException to 503.
GUARD: No guard. UploadService.cs:159 calls SaveAsync with deterministic key thumbs/{owner}/{id}.jpg on cache miss; no lock/semaphore/dedup around it. LocalStorageService.cs:32 File.Create uses FileShare.None (.NET default), so a concurrent second first-preview write throws IOException. No IOException-to-status mapping exists on this path -> 500. Finding is real.
TRACE: Two concurrent GET previews for one upload, ThumbnailPath still null. Both pass AsNoTracking read + ExistsAsync=false (UploadService.cs:150), both generate and call SaveAsync with fileId=uploadId, prefix="thumbs" -> same path. LocalStorageService.cs:32 File.Create uses FileShare.None (exclusive on Windows and .NET-on-Linux flock); the second overlapping open throws IOException. It is not in ExceptionHandlerMiddleware's map -> 500.

### M3 [MEDIUM] Image decode has per-allocation caps (100 MP / 512 MB) but no aggregate/concurrency memory bound -> OOM DoS under concurrent large images
src/PhotoPrint.API/Services/UploadService.cs:158 | confirmed | conv 2 | security, input-validation | c7
SCENARIO: Attacker uploads many highly-compressible ~100MP PNGs (few KB on disk, pass the <=100MP cap), then fires many first-hit GET /{id}/preview requests. Each cache miss decodes ~400MB (Rgba32) via GenerateThumbnailAsync. The rate limiter counts requests, not cost, and extra IPs bypass its per-IP partition; concurrent decodes exhaust RAM and OOM the process.
FIX: Gate GenerateThumbnailAsync behind a bounded SemaphoreSlim (or a dedicated tight concurrency/rate-limit policy on upload+preview) so total in-flight decode memory is capped regardless of request count or source IP.
GUARD: No aggregate/concurrency guard exists. ImageSharp AllocationLimitMegabytes=512 (Program.cs:96) is per single allocation; ExceedsDecodeLimits 100MP (ImageProcessor.cs:34) is per-image. The only throttle is a fixed-window per-IP request-count limiter (SecurityExtensions.cs:59-77) — no cost/concurrency bound, no SemaphoreSlim/Channel around GenerateThumbnailAsync (UploadService.cs:158), no Kestrel MaxConcurrentConnections. Finding is REAL.
TRACE: Guest uploads ~100 solid-color 10000x10000 PNGs (=100MP, few KB each) — pass the 50MB and <=100MP upload caps. Then fires 100 concurrent first-hit GET /{id}/preview from one IP (within the 100/min per-IP window; attacker owns them so isOwner passes). Each cache miss calls GenerateThumbnailAsync -> Image.LoadAsync ~400MB Rgba32. The 512MB cap is per-allocation only; no concurrency/aggregate bound -> ~40GB concurrent -> OOM. REAL.

### M4 [MEDIUM] Decompression bomb via the batch upload endpoint never emits the reserved bomb-alert event
src/PhotoPrint.API/Controllers/UploadsController.cs:119 | confirmed | conv 3 | requirements, observability, completeness-critic | c8
SCENARIO: Attacker POSTs pixel bombs to /api/uploads/batch (the code's own "most likely bomb vector"). Each DecompressionBombException is caught here (it subclasses UnprocessableEntityException) and logged only as uploads.batch.item_rejected, bypassing the middleware. The uploads.decompression_bomb.rejected event (with width/height) that ops alert on never fires, so a batch bomb spike is invisible to that alert.
FIX: In the batch catch, when ex is DecompressionBombException also emit the uploads.decompression_bomb.rejected event with WidthPx/HeightPx, matching the middleware, so alerting covers both vectors.
GUARD: No genuine guard. UploadsController.cs:108-123 catches UnprocessableEntityException (DecompressionBombException's base), logging only uploads.batch.item_rejected; the exception never reaches ExceptionHandlerMiddleware.cs:102-105 where uploads.decompression_bomb.rejected (width/height) is emitted. The batch line's reason={Reason} is a different event lacking dimensions — a partial guard missing this case. Real.

### M5 [MEDIUM] MimeValidator accepts HEIC/HEIF but no decoder exists in the stack (over-accept)
src/PhotoPrint.API/Services/MimeValidator.cs:52 | confirmed | conv 1 | input-validation | c9
SCENARIO: User uploads an iPhone .heic photo (default camera format). DetectMimeType returns image/heic, the file is buffered and written to disk, then GetInfoAsync's Image.IdentifyAsync throws UnknownImageFormatException (verified: ImageSharp 3.1.11 registers no HEIF decoder) -> null -> file deleted -> 422 'could not be read as an image'. 100% of HEIC uploads fail confusingly.
FIX: Add a HEIF decoder (libheif/Magick.NET or an ImageSharp HEIF plugin), or stop accepting HEIC in MimeValidator and drop it from the 'JPEG, PNG, HEIC accepted' message until decode is supported.
GUARD: No guard. MimeValidator.cs:52 returns image/heic for legit HEIF brands; UploadService.cs:52-55,75,82-87 accepts, saves .heic, then GetInfoAsync (ImageProcessor.cs:50) runs Image.IdentifyAsync. csproj line 26 has only SixLabors.ImageSharp 3.1.11 — no HEIF decoder — so it throws→caught→null→delete→422. The brand check only blocks non-HEIF ISO-BMFF, not HEIC itself.
TRACE: 1. .heic upload; DetectMimeType matches ftyp + brand "heic" -> returns "image/heic" (MimeValidator:52). 2. UploadService maps ext "heic", SaveAsync writes to disk (UploadService:80). 3. GetInfoAsync -> Image.IdentifyAsync throws (no HEIF decoder in ImageSharp 3.1.11), caught -> null (ImageProcessor:57). 4. null -> DeleteAsync + UnprocessableEntityException "The uploaded file could not be read as an image" -> 422. Every HEIC fails.

### M6 [MEDIUM] Preview cache-miss with a missing original returns 500 instead of a clean 4xx
src/PhotoPrint.API/Services/ImageProcessor.cs:63 | confirmed | conv 1 | completeness-critic | c8
SCENARIO: Original blob is deleted ops-side (or by the cleanup race) but the row/DeletedAt survives. A preview misses the cache, GenerateThumbnailAsync calls GetStreamAsync, which throws FileNotFoundException — outside the ImageFormatException catch and absent from the exact-type exception map → 500. BUG-4's fix only covers corrupt images, not missing files.
FIX: Catch FileNotFoundException in the miss path (or map it in ExceptionHandlerMiddleware) and surface 404/422 instead of 500.
GUARD: Real. GetStreamAsync throws FileNotFoundException (LocalStorageService.cs:55). In GenerateThumbnailAsync the call is at line 63, outside the try (opens line 71), so the ImageFormatException catch can't reach it. No FileNotFoundException/IOException entry in the map (ExceptionHandlerMiddleware.cs:10-26) → 500. ExistsAsync (UploadService.cs:150) guards only ThumbnailPath, not upload.FilePath on the miss path. No guard.
TRACE: Row survives (DeletedAt null), original blob deleted. GET preview: owner check passes; cache miss (ThumbnailPath null or cached file gone). GetPreviewAsync calls GenerateThumbnailAsync(FilePath). ImageProcessor.cs:63 GetStreamAsync — OUTSIDE the try (try starts line 71) — LocalStorageService.cs:55 throws FileNotFoundException. Middleware maps by exact GetType(); FileNotFoundException absent from _exceptionMappings → else → 500. BUG-4 catch only handles ImageFormatException. Confirmed.

### M7 [MEDIUM] Unreadable stored image at preview time is logged without storage path or root cause
src/PhotoPrint.API/Services/ImageProcessor.cs:88 | confirmed | conv 1 | observability | c7
SCENARIO: A stored file that passed upload magic-byte validation is later corrupted/replaced ops-side. GenerateThumbnailAsync catches ImageFormatException and throws a bare UnprocessableEntityException — no log, storagePath dropped, inner exception discarded. Middleware logs a generic 422 warning identical to a user's bad upload. Ops can't identify which file corrupted or why. GetInfoAsync (upload path) logs path+exception; preview path does not.
FIX: Log a warning here with storagePath and the caught exception before rethrowing, and pass the original as inner exception, mirroring GetInfoAsync.
GUARD: ImageProcessor.cs:81-89 — catch(ImageFormatException) binds no variable, throws bare UnprocessableEntityException with no _logger call, dropping storagePath and inner exception. Contrast GetInfoAsync line 56 which logs both. No middleware or other guard restores this context, so preview-time corruption is indistinguishable from a user bad-upload 422. Finding is real; no preventing guard.
TRACE: Stored file corrupted/replaced ops-side after upload. GenerateThumbnailAsync (line 74/79) hits ImageFormatException; catch (lines 81-89) throws UnprocessableEntityException("The file could not be read as an image.") with no _logger call, storagePath dropped, inner ex discarded. Middleware emits a generic 422 indistinguishable from a user's bad upload. GetInfoAsync (lines 54-58) logs ex+StoragePath; preview path does not. Asymmetry confirmed; observability finding real.

### M8 [MEDIUM] Restored preview kept on 403 leaves orphaned, un-cartable uploads after guest-session expiry
src/PhotoPrint.UI/src/app/features/upload/pages/format-selector/format-selector-page.ts:400 | confirmed | conv 1 | frontend-ux | c7
SCENARIO: Long-lived tab, guest token expires. Refresh: preview 401s → interceptor clears token → re-init mints a NEW session → retry preview 403s (new session doesn't own the old upload). fetchPreviewWithRetry only drops on 404, so the entry is KEPT preview-less; user adds it to cart → checkout 403.
FIX: Treat 403 (and a persistent 401 after the re-init retry) the same as 404: dropRestoredEntry. Only 5xx/network are transient/keepable. Also fix the FE-4 spec, whose retry-succeeds mock is impossible in reality.
GUARD: No guard. fetchPreviewWithRetry (format-selector-page.ts:386-409) drops only on 404 (line 400). A retried 403 (isRetry=true) skips the 401 branch (391) and 404 branch (400), falling through to the "keep entry preview-less" path (405-407). The orphaned upload stays cartable — finding is REAL.
TRACE: 1) Long-lived tab, done uploads in sessionStorage, guest token expired but still present. 2) Refresh: ensureGuestSession sees stale token, mints nothing; restoreFromSession→fetchPreviewWithRetry(false). 3) getPreviewBlob→401; interceptor clears token; re-init mints NEW session; retry(true). 4) Old upload row still exists (cleanup not run) but owned by old session→server throws ForbiddenException→403. 5) isRetry so 401 skipped; 403≠404 so entry KEPT preview-less. 6) onAddToCart→CartService ownership check→403. Real.

### M9 [MEDIUM] Provider-aware migration DDL and the ThumbnailPath column (incl. the Npgsql/Postgres arm) are exercised by no test
src/PhotoPrint.API/Migrations/20260527102718_AddUploadThumbnailPath.cs:34 | confirmed | conv 3 HINTED | db-parity, tests-coverage, completeness-critic | c7
SCENARIO: Every upload/preview test uses UseInMemoryDatabase (ignores migrations); the few SQLite tests use EnsureCreated (model, not migrations) and never touch Uploads. A typo in the Npgsql 'character varying(512)' string or a broken Up() ships green — the column diverges from the Npgsql model in prod only.
FIX: Add a migration smoke test that applies migrations to a real SQLite (and ideally Postgres via Testcontainers) DB and asserts the ThumbnailPath column exists with the right type/length.
GUARD: Real. Zero Migrate()/GetPendingMigrations in src/PhotoPrint.Tests; every fixture uses UseInMemoryDatabase or EnsureCreated (model, not migrations). The only Migrate() is Program.cs:210, prod Npgsql-only. The lone "character varying" test hit (OrderIdempotencyColumnTests.cs:13) is a comment on a different column and still runs InMemory. Nothing exercises this migration's Up()/Down() or the Npgsql "character varying(512)" arm — no guard.
TRACE: Edit line 24 to a typo, e.g. "charcter varying(512)". Run full test suite: all green — no test calls Migrate()/MigrateAsync (grep: 0 in Tests); upload fixtures use InMemory, SQLite tests use EnsureCreated (model, not migrations) and never touch Uploads; no Testcontainers/Npgsql in tests. The only Migrate() is Program.cs:210, Npgsql-guarded, unreached. Broken DDL ships; fails at Postgres deploy boot.

### M10 [MEDIUM] Upload-time bomb rejection deletes the stored file but no test verifies it
src/PhotoPrint.Tests/Unit/Services/UploadServiceTests.cs:381 | confirmed | conv 1 | tests-coverage | c8
SCENARIO: UploadAsync deletes storagePath before throwing DecompressionBombException (UploadService.cs:91), but UploadAsync_ImageDimensionsExceedLimit_... only asserts the exception. Remove the DeleteAsync and a rejected bomb file leaks on disk forever, suite still green — unlike the null-image test which does verify DeleteAsync.
FIX: Add _storageMock.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once) to the dimensions-exceed test.
GUARD: UploadServiceTests.cs:380-393 only asserts ThrowAsync<UnprocessableEntityException> with message "*dimensions exceed*"; no _storageMock.Verify(DeleteAsync). The delete at UploadService.cs:91 is unpinned — removing it keeps the suite green. No guard. Finding is real.
TRACE: Mutation trace: delete UploadService.cs:91 (`_storage.DeleteAsync` on the bomb path). Run suite. Test at UploadServiceTests.cs:381 only asserts ThrowAsync<UnprocessableEntityException> + message, no DeleteAsync Verify. Null-image test (line 152) uses its own per-test mock, untouched. No other test hits ExceedsDecodeLimits. Suite stays green while the rejected bomb file leaks. Real coverage gap.

### M11 [MEDIUM] MaxFrames=1 multi-frame (APNG) bomb defence has zero test coverage
src/PhotoPrint.API/Services/ImageProcessor.cs:68 | confirmed | conv 1 | tests-coverage | c8
SCENARIO: GenerateThumbnailAsync sets DecoderOptions{MaxFrames=1} as the frame-bomb control, but no test feeds a multi-frame image. Delete MaxFrames=1 and every test stays green while an animated file with thousands of frames again materialises frames x canvas x 4 bytes on decode.
FIX: Add a unit test using a genuine multi-frame image (animated PNG/GIF/WebP) and assert the decode/output reflects a single frame (or that MaxFrames is honoured).
GUARD: No guard catches this. ImageProcessorTests.cs GenerateThumbnailAsync tests (lines 53,68,89) cover only oversized-pixel-area, small-valid, and unreadable files — none feeds a multi-frame/APNG image. MaxFrames=1 (ImageProcessor.cs:68) is the only frame cap; deleting it leaves all tests green. Finding is REAL.
TRACE: REAL. ImageProcessorTests.cs (lines 39-123) only feeds single-frame PNGs and non-image bytes — never a multi-frame APNG/GIF. Delete MaxFrames=1 at ImageProcessor.cs:68 (DecoderOptions defaults to decoding all frames): single-frame decodes identically, so all five thumbnail/info tests plus ExceedsDecodeLimits stay green. The frame-bomb guard is untested; a thousands-of-frames file again materialises frames×canvas×4 bytes undetected.

### L1 [LOW] Cache-hit preview path uses check-then-get (ExistsAsync -> GetStreamAsync): TOCTOU 500 if the file vanishes plus a redundant second storage round-trip on the hottest read
src/PhotoPrint.API/Services/UploadService.cs:150 | confirmed | conv 4 | correctness, quality, race, completeness-critic | c4
SCENARIO: Preview cache-hit: ExistsAsync(ThumbnailPath) returns true, then ops or the cleanup job deletes the file, then GetStreamAsync opens it -> FileNotFoundException -> unmapped -> 500, instead of transparently regenerating the thumbnail.
FIX: Catch FileNotFoundException on the hit path and fall through to regeneration, or drop the ExistsAsync pre-check and try GetStreamAsync directly, regenerating on the not-found catch.
GUARD: Real. UploadService.cs:150-151 does ExistsAsync then GetStreamAsync with no surrounding try/catch or regenerate fallback. LocalStorageService.cs:54-55 throws FileNotFoundException if the file vanished; ExceptionHandlerMiddleware.cs:10-26 maps only custom types (not FileNotFoundException), so it hits the generic catch -> 500. No guard prevents the TOCTOU.

### L2 [LOW] MIME-acceptance behavior change shipped with no story and omitted from the scope doc
src/PhotoPrint.API/Services/MimeValidator.cs:46 | refuted | conv 1 | requirements | c5
SCENARIO: Approving 'bolt 042' also ships an upload-acceptance change: ISO-BMFF containers (MP4/MOV/M4A) previously classified image/heic are now rejected at validation. No story/AC covers it, and bolt.md's bundled-scope section enumerates only Change B/C, so this (and OBS-1/OBS-2 logging) ships untraced to any requirement.
FIX: List the MimeValidator HEIF-brand change and OBS-1/OBS-2 in bolt.md's bundled-scope section (or give them a story/AC) so a reviewer approves them knowingly.
GUARD: Commit f850f69 ("fix(uploads)... INPUT-1, review 042-v1") is a dedicated, traced change adding brand-accept/container-reject tests; the code comment at MimeValidator.cs:18-22 cites INPUT-1. The MP4/MOV acceptance it removed was a bug, not an intended behavior. So the change traces to a documented requirement, refuting "ships untraced to any requirement."
TRACE: No runtime failure is constructible: the code is correct. The HEIC-tightening (MP4/MOV brands isom/mp42/qt now return null instead of image/heic) is real but is a security fix, not a defect. And it IS traced — commit f850f69 cites INPUT-1/review 042-v1 — contradicting "untraced to any requirement." Only genuine gap: bolt.md's Bundled-scope lists Change B/C, omitting this. That's a doc/process observation, not a code failure.

### L3 [LOW] Cache file vanishing (ops deletion/storage fault) silently regenerates with no signal
src/PhotoPrint.API/Services/UploadService.cs:150 | confirmed | conv 1 | observability | c5
SCENARIO: ThumbnailPath is set but ExistsAsync returns false (files wiped, storage misconfig, or an Exists bug). Every preview silently falls through to full ImageSharp regeneration, defeating the cache and spiking CPU/cost, with no log distinguishing this from a normal first-time miss. A cache that has silently stopped working is undetectable.
FIX: Emit a low-cardinality warning (e.g. uploads.thumbnail.cache_miss_missing_file with uploadId) when ThumbnailPath is non-null but the file is absent.
TRACE: State: upload.ThumbnailPath="thumbs/x.jpg" but ops delete the file, so ExistsAsync returns false. Line 150 short-circuits false; control enters the miss branch (153-171), which calls GenerateThumbnailAsync (ImageSharp) on every request. That branch has no _logger calls, so a wiped cache is indistinguishable from a first-time miss. Matches code exactly; real but low-severity observability gap.

### L4 [LOW] Orphaned thumbnail on failed DB commit emits no distinct signal
src/PhotoPrint.API/Services/UploadService.cs:159 | confirmed | conv 1 | observability | c5
SCENARIO: Cache miss: SaveAsync writes thumbs/{owner}/{id}.jpg, then SaveChangesAsync throws (transient DB fault). ThumbnailPath is never persisted, so the cleanup job (which keys on ThumbnailPath) can never delete the file once the upload expires. The failure only surfaces as a generic unhandled-500 log with no indication a thumbnail file was orphaned.
FIX: Wrap the SaveChanges in a try/catch that logs the just-written key (and ideally deletes it) on failure, so the orphan is observable and self-cleans.
TRACE: Cache-miss branch: line 159 SaveAsync writes thumbs/{owner}/{id}.jpg and returns the path; line 166 SaveChangesAsync throws (transient fault) before the assignment is persisted. No try/catch wraps lines 159-166, so ThumbnailPath stays null in the DB while the file exists on disk, and the only output is the caller's generic unhandled-500 log — no distinct orphan signal.

### L5 [LOW] GET /preview now performs a DB write, breaking safe/idempotent GET semantics (read-replica hazard)
src/PhotoPrint.API/Services/UploadService.cs:166 | confirmed | conv 1 | race | c5
SCENARIO: If prod later routes GET traffic to a Postgres read replica (a common scaling step), the cache-miss SaveChangesAsync fails on the read-only connection, so every first-preview 500s until a thumbnail is warmed elsewhere. GET also stops being safe for proxy/retry semantics.
FIX: Route the ThumbnailPath persistence through the primary explicitly, or defer cache-fill to a write endpoint/background step; at minimum document that /preview requires the primary DB and cannot be read-replica routed.
TRACE: Confirmed. UploadsController.cs:130 [HttpGet("{id}/preview")] -> GetPreviewAsync. Owner GETs a never-previewed upload (ThumbnailPath==null): cache-miss branch generates the thumbnail, then Attach + IsModified + SaveChangesAsync (line 166) performs a real DB write. So GET is non-idempotent; on a read-only replica connection SaveChangesAsync throws -> 500 on first preview. Real, low severity.

### L6 [LOW] Batch-rejection warning logs the raw client filename with no length/encoding bound
src/PhotoPrint.API/Controllers/UploadsController.cs:120 | confirmed | conv 1 | input-validation | c4
SCENARIO: Attacker sends a batch of many invalid files whose FileName contains control characters / newlines or is very long. The new 'uploads.batch.item_rejected file={FileName}' line logs it verbatim per item -> log-volume amplification, and in any plain-text sink a newline in the name can forge log lines (Serilog structured capture mitigates but does not bound length).
FIX: Sanitize and truncate file.FileName before logging (strip control chars, cap to e.g. 128 chars).
TRACE: Code logs file.FileName verbatim, unbounded, once per rejected item. Trace: POST batch of N tiny invalid files (within MaxBatchSizeBytes), each FileName = "x\nFAKE uploads.done" and unsupported type -> each hits the catch -> LogWarning fires N times. In any plain-text/console sink using the default output template, the {FileName} value renders inline, so the embedded newline forges a log line; length is never truncated. Real, low severity.

### L7 [LOW] Guest-token self-heal broadens to every unauthenticated 401 app-wide
src/PhotoPrint.UI/src/app/core/interceptors/error.interceptor.ts:30 | confirmed | conv 1 HINTED | requirements | c5
SCENARIO: An anonymous (non-guest) user hits any account-only endpoint and gets 401. The interceptor now clears the guest token and does nothing on every endpoint, replacing the prior redirect to /auth/login with a silent dead-end. The bolt's stated scope was the upload/preview flow, not all requests.
FIX: Scope the no-navigate self-heal to upload/preview requests, or surface a login prompt for non-guest 401s, instead of silently swallowing every unauthenticated 401.
TRACE: State: user not authenticated, no guest token. Any request returns 401. Current code takes the else branch (!isAuthenticated) → clearGuestToken() is a no-op, no navigation, error just rethrown. Prior code (initial + bolt cf78fb4) took else → logout() + navigateByUrl('/auth/login'). So the app-wide redirect for anonymous non-guest 401s is now gone — a silent dead-end. Real behavior change (severity low; impact depends on whether such a flow exists).

### L8 [LOW] One-shot retry guard (isRetry) untested for a still-failing retry
src/PhotoPrint.UI/src/app/features/upload/pages/format-selector/format-selector-page.ts:216 | plausible | conv 1 | tests-coverage | c7
SCENARIO: Tests cover retry-succeeds, non-401, and 404, but never a persistent 401 (401 -> re-init -> retry -> 401 again). If the !isRetry guard regressed in performUpload/fetchPreviewWithRetry, the retry loops, hammering initAnonymousSession and the endpoint; no test goes red.
FIX: Add tests where the retried upload and preview also return 401; assert exactly two attempts then terminal error (upload) / kept entry (preview).
GUARD: No test covers persistent 401. Retry test (spec line 232) makes attempt 2 succeed; non-401 test uses 500; 404/5xx are preview-only. No test drives 401->re-init->retry->401-again, so a regressed !isRetry guard (page line 228) would loop undetected. Gap is real.
TRACE: No failing execution exists in current code. performUpload (line 228) and fetchPreviewWithRetry (line 391) both gate re-init on !isRetry; a second 401 goes to failAll()/keep-entry, so no loop or hammering. The finding's scenario is conditional on a hypothetical future regression of the guard, not present now. It's a real (minor) test-coverage gap, but no concrete failing trace is constructible against the real code.

### L9 [LOW] shareReplay/finalize re-init after a settled init is never exercised
src/PhotoPrint.UI/src/app/features/upload/pages/format-selector/format-selector-page.ts:210 | confirmed | conv 1 | tests-coverage | c7
SCENARIO: FE-1's test uses a Subject that never completes, so finalize(() => guestInit$=null) never runs. No test proves that after one init settles a later expiry triggers a SECOND init. Remove the finalize-reset and the self-heal 're-init on later expiry' silently breaks while FE-1/FE-2 stay green.
FIX: Complete the first init, then with no token call ensureGuestSession again and assert initAnonymousSession was called twice.
TRACE: Mutation: delete finalize (line 210). Guest, no token: ensureGuestSession fires init#1, settles; guestInit$ now holds the completed shareReplay. Token later expires (interceptor clears on 401); getGuestToken→null. Retry's ensureGuestSession hits `guestInit$ ??=`, sees non-null, replays stale result — init#2 never fires, self-heal broken. FE-1 (non-completing Subject, asserts once), FE-2/FE-4 (single populate) all stay green. Coverage gap real.

### L10 [LOW] Model snapshot records ThumbnailPath as SQLite TEXT while the Npgsql runtime model is varchar(512), producing a phantom AlterColumn
src/PhotoPrint.API/Migrations/PhotoPrintDbContextModelSnapshot.cs:707 | plausible | conv 1 HINTED | db-parity | c8
SCENARIO: The single shared snapshot is SQLite-flavored (TEXT, no character varying). The runtime Npgsql model emits character varying(512). Next `dotnet ef migrations add` under the Npgsql provider diffs model vs snapshot and scaffolds a spurious AlterColumn(TEXT->varchar(512)) for ThumbnailPath, which a reviewer must recognize and discard.
FIX: Accept as documented deferral (the migration comment already notes it), or move to per-provider migration assemblies to eliminate drift. No in-place snapshot edit needed.
GUARD: No enforced guard. No IDesignTimeDbContextFactory exists; design-time provider defaults to "Postgres"/Npgsql (Program.cs:26). The provider-aware migration (AddUploadThumbnailPath.cs:19-26) only fixes the runtime column type, not the snapshot. Snapshot line 707-709 stays hardcoded HasColumnType("TEXT"), so an Npgsql migrations-add diffs varchar(512) vs TEXT and scaffolds the phantom AlterColumn. Nothing prevents this case.
TRACE: Cannot isolate a ThumbnailPath/line-707 failure. The entire snapshot is uniformly SQLite (FilePath 690-691 TEXT+512, all Guids/longs too) because migrations are generated under SQLite; there snapshot TEXT matches the SQLite runtime model, no phantom. Switching to Npgsql regeneration phantoms every column, not ThumbnailPath specifically. Line 707 is correct and consistent, not a defect; the migration .cs already emits varchar(512) on Postgres.

### L11 [LOW] ETag/streaming assumes a seekable stream, untested against the planned cloud storage provider
src/PhotoPrint.API/Controllers/UploadsController.cs:144 | plausible | conv 1 HINTED | completeness-critic | c6
SCENARIO: Bolt 043's cloud IStorageService returns a non-seekable stream (e.g. S3 GetObject). The controller's `etag = id-{stream.Length}` throws NotSupportedException → every preview 500s. The changed interface (ExistsAsync, prefix, '/'-keys) is explicitly cloud-directed, yet only LocalStorageService (seekable FileStream) is exercised.
FIX: Document GetStreamAsync's seekability/Length contract, or derive the ETag from persisted metadata (id + FileSizeBytes) instead of stream.Length.
GUARD: No guard. UploadsController.cs:144 calls stream.Length unconditionally; IStorageService.cs:18 returns Task<Stream> with no CanSeek/seekability contract; UploadService.GetPreviewAsync cache-hit (UploadService.cs:151) returns the raw storage stream unbuffered. Only LocalStorageService (seekable FileStream, line 57) exists today, so nothing prevents a non-seekable cloud stream throwing NotSupportedException.
TRACE: Only registered IStorageService is LocalStorageService; its GetStreamAsync returns File.OpenRead(...), a seekable FileStream where .Length works. GetPreviewAsync returns that stream, so line 144 stream.Length never throws today. The non-seekable cloud provider is "planned" (Bolt 043) and does not exist in the repo. No failing execution constructible from real code — hypothetical only.

### L12 [LOW] Bomb log test asserts the event name but not the dimensions the event exists to carry
src/PhotoPrint.Tests/Unit/Middleware/ExceptionHandlerMiddlewareTests.cs:254 | confirmed | conv 1 | tests-coverage | c8
SCENARIO: InvokeAsync_DecompressionBomb_... checks the message contains 'uploads.decompression_bomb.rejected' but never that width/height (30000) appear. Drop the dimensions from the log (the entire purpose of WidthPx/HeightPx) and the test passes. Same presence-not-value gap in the batch-reject and client-abort tests.
FIX: Assert the formatted log state contains the width/height values (and, for the other tests, the file/reason/path fields).
TRACE: Confirmed. Line 274-275 asserts only v.ToString().Contains("uploads.decompression_bomb.rejected"). No check for "30000"/WidthPx/HeightPx. Mutation: drop both dimensions from the middleware's log call, keep the event string -> test still passes green (Times.Once matches). Client-abort test (247) likewise only matches the event name. Real presence-not-value gap.

### L13 [LOW] 512 MB allocator backstop and InvalidMemoryOperationException are untested and map to a raw 500
src/PhotoPrint.API/Program.cs:95 | confirmed | conv 1 | tests-coverage | c6
SCENARIO: A bomb whose header understates true decode size passes the pixel-area Identify check, then LoadAsync trips the AllocationLimitMegabytes cap throwing InvalidMemoryOperationException. That type isn't in the exception map and isn't caught by catch(ImageFormatException), so it surfaces as a generic 500. No test exercises the cap or asserts the status.
FIX: Map InvalidMemoryOperationException to 422/413 in ExceptionHandlerMiddleware and add a test; or at minimum add a test pinning the current backstop behaviour.
TRACE: Upload a ~100MP 16-bit PNG. IdentifyAsync reports 10000x10000 = 100_000_000 = MaxDecodePixels, so ExceedsDecodeLimits (strict >) is false and it passes. LoadAsync allocates Rgba64 (~800MB) > 512MB cap, throwing ImageSharp's InvalidMemoryOperationException. It derives from Exception, not ImageFormatException, so the catch (line 81) misses it and it's absent from _exceptionMappings, yielding a raw 500. No test covers the cap.

### L14 [LOW] Recognised-but-broken image 422 path (InvalidImageContentException) is untested
src/PhotoPrint.API/Services/ImageProcessor.cs:81 | plausible | conv 1 | tests-coverage | c6
SCENARIO: GenerateThumbnailAsync_UnreadableFile_ feeds random bytes -> UnknownImageFormatException. The catch also claims to handle InvalidImageContentException (valid magic bytes, truncated body), but no test feeds a truncated-but-recognised image. Narrow the catch to UnknownImageFormatException only and a truncated JPEG 500s with no test catching it.
FIX: Add a test with a valid JPEG/PNG header followed by garbage/truncation and assert UnprocessableEntityException (422).
GUARD: No guard. ImageProcessorTests.cs:89 (only unreadable-file test) feeds random bytes 0xDEADBEEF -> UnknownImageFormatException path. No test feeds a truncated-but-recognised image, so InvalidImageContentException branch of the catch at ImageProcessor.cs:81 is uncovered. Catch uses base ImageFormatException so production is fine, but the test-coverage gap is real.
TRACE: Line 81 catches ImageFormatException, the base of both UnknownImageFormatException and InvalidImageContentException. A truncated-but-recognised JPEG throws InvalidImageContentException, which IS caught and re-thrown as UnprocessableEntityException (422). No 500 is reachable in the real code. The finding only describes a test-coverage gap and a hypothetical narrowing that the code does not do; no concrete failing execution exists.

### C1 [CLEANUP] Preview object URLs never revoked -> memory leak on every restore/retry
src/PhotoPrint.UI/src/app/features/upload/pages/format-selector/format-selector-page.ts:388 | unverified-cleanup | conv 1 | frontend-ux | c6
SCENARIO: getPreviewBlob returns URL.createObjectURL(blob); restoreFromSession creates one per restored upload on every load, and onRemoveUpload/cart-clear drop the state without URL.revokeObjectURL. Blobs accumulate in memory across restores/navigation for the tab's lifetime.
FIX: Track and URL.revokeObjectURL(previewUrl) when replacing a preview, removing an upload, clearing the grid on add-to-cart, and on component destroy.

### C2 [CLEANUP] User-facing upload error string duplicated across three sites
src/PhotoPrint.UI/src/app/features/upload/pages/format-selector/format-selector-page.ts:220 | unverified-cleanup | conv 1 | quality | c6
SCENARIO: 'Eroare la încărcarea fișierului.' is hard-coded at lines 175, 220, and 263; a future wording/i18n change must touch all three and can drift out of sync.
FIX: Extract a single private readonly constant (e.g. UPLOAD_ERROR) and reference it at all three call sites.

### C3 [CLEANUP] Self-heal seam (interceptor clears token <-> component re-inits) only tested with each half mocked
src/PhotoPrint.UI/src/app/features/upload/pages/format-selector/format-selector-page.ts:227 | unverified-cleanup | conv 1 HINTED | completeness-critic | c6
SCENARIO: error.interceptor clears 'guestSession' on 401; the component's retry re-inits via ensureGuestSession. Each half is unit-tested with the other simulated (FE-2 manually nulls the token). If the interceptor and getGuestToken ever diverged on storage key/shape, both isolated tests still pass while the real clear→re-init→retry loop silently breaks.
FIX: Add one integration-style test wiring the real errorInterceptor + component so the clear→re-init→retry seam is exercised end-to-end.

### C4 [CLEANUP] Implementation walkthrough contradicts the shipped code (cache directive, tracking, migration)
memory-bank/bolts/042-thumbnail-cache/implementation-walkthrough.md:32 | unverified-cleanup | conv 1 | requirements | c9
SCENARIO: Walkthrough states preview sets 'Cache-Control: public, max-age=2592000, immutable' (shipped: private, no immutable), that AsNoTracking was dropped for a tracked load (shipped keeps AsNoTracking + Attach/IsModified), and cites migration 20260527102445 as SQLite-only TEXT (shipped: 20260527102718, provider-aware varchar(512)). A maintainer trusts the wrong, security-relevant cache description.
FIX: Refresh the walkthrough post-review to match shipped code: private cache directive, AsNoTracking+Attach persistence, provider-aware migration and correct filename.

### C5 [CLEANUP] Story AC cites '54 MP rejected' but the shipped cap is 100 MP
memory-bank/intents/019-thumbnail-cache-and-cloud-storage/units/001-thumbnail-cache/stories/003-imagesharp-max-pixels.md:27 | unverified-cleanup | conv 1 | requirements | c8
SCENARIO: Story 003 AC says the test rejects 'an oversized image (54 MP)', but MaxDecodePixels is 100 MP, so a 54 MP image is accepted. The number predates NEW-1 raising the cap; the actual test uses 110 MP. A verifier trusting the doc expects the wrong threshold behavior.
FIX: Update the AC example to 110 MP (or '>100 MP') to match the shipped 100 MP cap and the ImageProcessorTests fixture.

### C6 [CLEANUP] Story AC specifies varchar(500)/StoragePath but the column shipped as varchar(512)/FilePath
memory-bank/intents/019-thumbnail-cache-and-cloud-storage/units/001-thumbnail-cache/stories/001-thumbnail-path-schema.md:22 | unverified-cleanup | conv 1 | requirements | c8
SCENARIO: AC says 'Uploads.ThumbnailPath varchar(500) NULL' and 'same shape as StoragePath'; the migration and EF config ship maxLength 512 (matching FilePath), and no StoragePath property exists. A reviewer verifying the AC literally finds a length mismatch and a nonexistent sibling column.
FIX: Update story 001 AC to varchar(512) and reference FilePath instead of the nonexistent StoragePath.

### C7 [CLEANUP] Thumbnail shipped at 300px while stories and unit-brief specify 800px
memory-bank/intents/019-thumbnail-cache-and-cloud-storage/units/001-thumbnail-cache/stories/002-persist-thumbnail-on-first-request.md:39 | unverified-cleanup | conv 1 | requirements | c6
SCENARIO: Story 002 technical notes (maxWidth: 800), story 001 out-of-scope ('single 800px for now'), and the unit brief all specify 800px; the code keeps the pre-existing 300px (ThumbnailMaxDimension). Disclosed in the walkthrough, but the story ACs and unit-brief still say 800, so the specced size was effectively reduced without updating them.
FIX: Reconcile the docs and code: either update the stories/unit-brief to 300px or raise ThumbnailMaxDimension to 800 to honor the spec.
