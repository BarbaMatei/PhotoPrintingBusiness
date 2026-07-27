---
type: code-review
target: 042-thumbnail-cache
version: 1
supersedes: null
branch: feat/bolt-042-thumbnail-cache
commit: cf78fb471a73eb36a0a8c16844a8b287155ffe7e
base: main
reviewed: 2026-07-13
reviewer: Claude (multi-lens parallel review system)
pass-type: discovery
lenses: [correctness-cache, correctness-image, security, pr-requirements, quality-altitude, db-migration-parity, input-validation, observability, race-concurrency, frontend-ux, tests-coverage, completeness-critic]
verdict: request-changes
blockers: [SEC-1, BUG-1, TEST-1]
---

# Review — Bolt 042: Thumbnail Cache (whole branch)

Persists generated thumbnails (cache-on-first-request), adds `Uploads.ThumbnailPath`,
and hardens ImageSharp against decompression bombs. The branch also **bundles two
undocumented change-sets**: a guest-auth self-heal (clear stale guest token on 401 instead
of logging out) and dev-warning silencing (HTTPS-redirect non-dev only, static files only
when `wwwroot` exists, EF split-query default). Reviewed as one unit because all three merge
together. 14 backend files (+175/−11) + 3 frontend files (+~90/−30).

Run via the [multi-lens review system](../README.md): **12 isolated blinded lenses** over the
whole feature + build/test verify, then **2 adversarial skeptics per finding** (guard-hunter +
trace-constructor), synthesized here. 110 agents, 53 raw findings → **28 after dedup** + 2
recorded false-positives.

## TL;DR

The happy path works and is tidy, and the suite is green (**490/490**) — but *green proves very
little here*: the real `ImageProcessor` is mocked in every test, the whole guest-auth change has
zero coverage, and no test applies the migration. Underneath, the feature ships **three blockers**:
the decompression-bomb guard **does not actually stop decompression bombs**, the preview response
is marked **`public` on an ownership-checked resource** (cross-user cache disclosure), and the
branch's new auth behavior is **entirely untested**. Below that sit real storage-leak and
concurrency defects on the cache-fill path.

- 🔴 **3 High** — SEC-1 (public cache leak), BUG-1 (bomb guard bypass), TEST-1 (guest-auth untested)
- 🟠 **8 Medium** — BUG-2/BUG-3 (thumbnail leaks), OBS-1, FE-1/FE-2, TEST-2/TEST-3, REQ-1
- 🟡 **14 Low** — QUAL-1/2, OBS-2/3, FE-3/4, REQ-2/3/4, DB-1, INPUT-1, CLOUD-1, TEST-4, BUG-4
- ⚪ **3 Cleanup** — QUAL-3/4/5

**Disposition: request changes** on SEC-1, BUG-1, TEST-1. **BUG-2/BUG-3** (unbounded storage
leak) are strongly recommended before merge.

**Cross-lens convergence (unbiased signal):** the bomb-guard weakness (BUG-1) was hit
**independently by 5 lenses** (correctness-image, input-validation, security, completeness,
requirements) — every trace-constructor built the exploit. The `ThumbnailPath` storage leak
(BUG-2) and the cache-fill TOCTOU (BUG-3) were each hit by **5 lenses**. `Cache-Control: public`
(SEC-1) by **3**.

---

## A. Security & DoS

### 🔴 SEC-1 — `Cache-Control: public` on an ownership-checked preview → cross-user disclosure (blocker)
`src/PhotoPrint.API/Controllers/UploadsController.cs:126`
**Confirmed — security 8/10.** `GetPreviewAsync` returns the thumbnail only after an ownership
check (`ForbiddenException` otherwise), yet sets `Cache-Control: public, max-age=2592000, immutable`.
But `app.UseResponseCaching()` (Program.cs:242) runs **before** `UseAuthentication`/`UseAuthorization`,
and guest requests carry their token in a custom **`X-Guest-Token`** header, *not* `Authorization`.
ASP.NET Core's `ResponseCaching` therefore treats the guest preview as cacheable and, because the
response is `public`, stores it keyed only on the URL `/api/uploads/{id}/preview` (no `Vary`). Concrete
flow: guest A fetches their preview → cached; a *different* guest B (or an anonymous client with no
token at all) requesting the same URL is served A's JPEG directly by the caching middleware, **before
auth ever runs**. Upload GUIDs leak through cart/order payloads, `sessionStorage`, and history. (Logged-in
users send `Authorization`, so they neither populate nor read this cache — exposure is guest→guest and
guest→anonymous, a first-class flow for this feature.) A future CDN/reverse-proxy makes it worse.
**Fix:** never mark a per-user resource `public`. Use `Cache-Control: private, max-age=…` (browser-only;
`private` also stops ASP.NET Core's `ResponseCaching` from storing it) or `no-store`. Drop `immutable`
(see BUG-3 note / completeness: it also contradicts the regenerate-on-delete path). Add an integration
test pinning the exact directive (see TEST-4).

### 🔴 BUG-1 — Decompression-bomb guard doesn't stop bombs: per-axis cap misses total pixels & frame count (blocker)
`src/PhotoPrint.API/Services/ImageProcessor.cs:47,51`, `Services/UploadService.cs:83-84`
**Confirmed — 5-lens convergence, every trace-constructor built the exploit.** The guard rejects only
`Width > 25000 || Height > 25000` (per-axis, strict `>`). Two crafted-but-valid inputs sail through and
reach the full `Image.LoadAsync` decode:
- **Total-pixel bomb:** a solid-colour **25000×25000** PNG compresses to a few hundred KB (well under
  the 50 MB cap), passes both axis checks (`25000` is not `> 25000`), and decodes to **625 MP ≈ 2.5 GB**
  in one request. Even 24000×24000 (576 MP) passes. The decode dies *before* `ThumbnailPath` is set, so
  **every** subsequent preview re-triggers the multi-GB allocation → cheap, repeatable OOM DoS. A guest
  can pre-stage up to 100 such files.
- **Animated-PNG bomb:** the guard never checks frame count and no `DecoderOptions.MaxFrames` is set.
  A small-canvas APNG (shares the PNG magic bytes → accepted) with thousands of near-identical frames
  stays tiny on disk but materialises `frames × canvas × 4 bytes` = tens of GB on decode.

**Fix:** replace the per-axis check at **both** sites with a total-pixel (area) cap using a `long`
multiply — e.g. reject `(long)Width * Height > MaxDecodePixels` sized for a print workload (tens of MP,
not 625). Set `DecoderOptions.MaxFrames = 1` (this app needs one still frame). **Additionally** set a
global `Configuration.Default.MemoryAllocator` allocation limit (this API *does* exist in ImageSharp
3.1.11 — see REQ-1) as belt-and-suspenders. Centralise the check (QUAL-3) and add the regression test
(TEST-2).

---

## B. Correctness & data lifecycle

### 🟠 BUG-2 — Cleanup job never deletes `ThumbnailPath` → unbounded disk leak
`src/PhotoPrint.API/BackgroundJobs/UploadCleanupJob.cs:90`
**Confirmed — 3-lens convergence (correctness-cache, requirements, tests-coverage).** Bolt 042 adds a
*second* persistent file per upload (the thumbnail, saved under a fresh random GUID), but
`UploadCleanupJob.CleanupAsync` only deletes `upload.FilePath`. When a previewed-then-abandoned upload is
soft-deleted, its thumbnail is left on disk **forever**: `GetPreviewAsync` filters `DeletedAt == null` so
it's never re-touched, and the cleanup candidate query also filters `DeletedAt == null` so the row is
never revisited. One orphaned thumbnail per previewed-then-expired upload, growing without bound.
**Fix:** in the cleanup loop, after deleting `FilePath` also `if (upload.ThumbnailPath is not null)
await storage.DeleteAsync(upload.ThumbnailPath, ct);` (same try/catch). Add the regression test (TEST).

### 🟠 BUG-3 — Cache-fill write is non-idempotent & non-atomic → orphaned blobs on concurrency/cancel
`src/PhotoPrint.API/Services/UploadService.cs:145-146`, `Models/Upload.cs:15`
**Confirmed — 5-lens convergence (race, correctness-cache, completeness, observability, tests).** On a
cache miss, `SaveAsync` is called **without a `fileId`**, so every miss mints a *new random* filename,
and the file write (`:145`) is separate from the DB write (`:146`). Three failure modes, all leaking
files the cleanup job can't reach (see BUG-2):
1. **Concurrency (TOCTOU):** two concurrent first-previews for the same upload each get their own scoped
   `DbContext`, both read `ThumbnailPath == null`, both generate (duplicate CPU), both write *different*
   files, and — with **no `RowVersion`/concurrency token** on `Upload` — the second `SaveChangesAsync`
   silently last-writer-wins. The losing file is orphaned. (`N` concurrent hits → `N−1` leaks.)
2. **Cancellation/crash** between `SaveAsync` and `SaveChangesAsync` (image GETs are cancelled constantly
   as users scroll away; the repo already special-cases client cancellation) → file on disk, path never
   persisted, next request regenerates a fresh orphan.
3. **Write-on-GET:** removing `AsNoTracking` (QUAL-1) means the GET now issues an `UPDATE`; if prod ever
   routes GET traffic to a Postgres **read-replica**, the write fails outright (latent, topology-dependent).
**Fix:** give the thumbnail a **deterministic** key derived from the upload id in a distinct namespace
(e.g. `thumbs/{ownerId}/{uploadId:N}.jpg` — *not* `fileId: uploadId`, which collides with the original's
`{uploadId:N}.jpg`), so a racing/cancelled write is simply overwritten and cleanup can target it. This
also satisfies REQ-2. Optionally add a `RowVersion` (but only with a reload/retry handler — see resolution
note). Add the concurrency test (TEST-4).

### 🟡 BUG-4 — `UnknownImageFormatException` at preview is unmapped → 500 instead of 422
`src/PhotoPrint.API/Services/ImageProcessor.cs:46`, `Middleware/ExceptionHandlerMiddleware.cs:19-24`
**Confirmed (residual of a refuted finding — see §J).** ImageSharp 3.1.11's `IdentifyAsync` *throws*
`UnknownImageFormatException` for unreadable input (it never returns null — a verifier ran the real API).
For a file that passed the upload-time check but was later corrupted/replaced ops-side, the cache-miss
regeneration path throws this type, which isn't in the middleware's exception map → raw **500** rather
than a clean 422. Minor, but a real rough edge on the "regenerate on ops-side deletion" path.
**Fix:** map `UnknownImageFormatException` → 422, or catch-and-rethrow as `UnprocessableEntityException`
in `GenerateThumbnailAsync`.

---

## C. Requirements / contract (PR lens)

**Scope: the thumbnail cache core (schema + cache-on-first-request) is delivered.** Gaps:

### 🟠 REQ-1 — Story 003's `MemoryAllocator` cap was silently dropped (AC unmet)
`src/PhotoPrint.API/Program.cs`, `Services/ImageProcessor.cs`
**Confirmed.** Story 003 AC#1 requires **both** a `Configuration.Default.MemoryAllocator` allocation cap
**and** `MaxImageWidth/Height`. The walkthrough documents substituting the per-call `Identify` dimension
guard for `MaxImageWidth/Height` (that API genuinely doesn't exist in ImageSharp 3.1.11) — a reasonable,
documented trade. But the **memory-allocator cap was dropped with no justification and no equivalent**,
even though `MemoryAllocator.Create(new MemoryAllocatorOptions { AllocationLimitMegabytes = … })` *is*
present in 3.1.11. That omission is exactly what leaves BUG-1's within-dimension bomb able to allocate GBs.
**Fix:** add the allocation cap in `Program.cs` (closes BUG-1 defense-in-depth), or formally descope the
AC in story 003 rather than leaving it silently unmet.

### 🟡 REQ-2 — Thumbnail saved at a random owner-dir path, not the spec's deterministic `thumbs/{id}.jpg`
`src/PhotoPrint.API/Services/UploadService.cs:145` — story 002 specifies a deterministic id-keyed path.
The random path feeds BUG-2/BUG-3 and complicates the bolt-043 cloud port. **Fix:** same as BUG-3, or
update story 002 to record the chosen scheme.

### 🟡 REQ-3 — Story 002 soft-delete edge case contradicts implemented (and tested) behavior
`src/PhotoPrint.API/Services/UploadService.cs:128` — story 002 says "source soft-deleted but thumbnail
persisted → return thumbnail, don't regenerate," but the code filters `DeletedAt == null` → **404**, and
`GetPreviewAsync_SoftDeletedUpload_ThrowsNotFoundException` locks that in. Spec and code disagree with no
reconciling note. **Fix:** amend the AC (404 is defensible) or serve the cached thumbnail; don't leave them
contradicting.

### 🟡 REQ-4 — Branch silently bundles undocumented guest-auth + dev-warning scope under a bolt-042 label
`src/PhotoPrint.API/Program.cs`, `Extensions/SecurityExtensions.cs`, frontend interceptor/auth changes.
**Confirmed.** The guest-auth self-heal (change B) and middleware-ordering/dev-warning changes (change C)
have no story, AC, or walkthrough — they exist only as commit messages. A reviewer approving "bolt 042"
unknowingly ships an auth-behavior change with no requirements anchor. **Fix:** split B and C into their
own bolts/stories, or at minimum document them with ACs and tests.

---

## D. Frontend / guest-auth self-heal (change B)

The direction is right (a stale guest 401 clears the token instead of logging out), but:

### 🟠 FE-1 — No in-flight dedup: concurrent `ensureGuestSession()` creates duplicate guest sessions & can orphan uploads
`src/PhotoPrint.UI/.../format-selector/format-selector-page.ts:184` (also `:123`, `:168`)
**Confirmed.** `getGuestToken()` is a synchronous `localStorage` read, so it stays `null` during an
in-flight `/auth/guest/init`. `ngOnInit` fires init A; an eager user dropping files before A resolves fires
init B. Both mint sessions; `storeSession` is last-write-wins. In the B-before-A completion order, uploads
are sent under session B but `localStorage` ends on token A → the cart references uploads token A can't
claim, and one session is orphaned server-side. **Fix:** cache the in-flight observable (`shareReplay(1)`
cleared on complete/error) so both callers share one init.

### 🟠 FE-2 — "Self-heal" is not seamless: a stale token makes the first upload fail with a generic error
`src/PhotoPrint.UI/.../format-selector/format-selector-page.ts:168`
**Confirmed.** `getGuestToken()` never checks expiry (unlike `tryRestoreSession`), so `ensureGuestSession`
returns `of(void 0)` for an expired-but-present token; the upload goes out stale → 401 → the interceptor
clears the token but there's **no auto-retry** — the user sees "Eroare la încărcarea fișierului." and must
manually re-drop. The "expired session self-heals" comment overstates it. **Fix:** auto-retry once after
the 401 clears the token (re-run `ensureGuestSession` then re-issue), or validate the token's `exp`
proactively. At minimum correct the comment.

### 🟡 FE-3 — Anonymous user with no/corrupt guest token is logged out to `/auth/login` on any 401 (dead-end)
`src/PhotoPrint.UI/.../error.interceptor.ts:27` — the guard is `!isAuthenticated() && getGuestToken()`;
when the token is null/corrupt this is falsy → `logout()` + redirect to a login page the guest has no
account for. **Fix:** for unauthenticated users, treat *any* 401 as a stale/absent guest session (clear +
re-init, no navigation); only redirect when actually logged in.

### 🟡 FE-4 — `restoreFromSession` wipes the restored grid & `sessionStorage` on an expired-token 401
`src/PhotoPrint.UI/.../format-selector/format-selector-page.ts:347` — a refresh with an expired token
fires parallel preview fetches; each 401 drops the entry and re-saves, clearing the whole in-progress
selection with no re-init/retry (and the multi-upload case can additionally bounce to `/auth/login`).
**Fix:** distinguish a 401 (re-init + retry once) from a genuine 404 before discarding.

---

## E. Observability

### 🟠 OBS-1 — Batch-upload rejections (incl. the new pixel-bomb 422) are swallowed with no logging
`src/PhotoPrint.API/Controllers/UploadsController.cs:98-106`
**Confirmed.** `UploadPhotoBatchAsync` catches the rejection exceptions and turns each into a per-item
result with **no logging** (the controller has no `ILogger`), so the exception never reaches
`ExceptionHandlerMiddleware`. The same file sent to the *single* endpoint logs a Warning. So bulk abuse —
the most likely bomb vector — is invisible to ops (the batch returns 200). **Fix:** log each swallowed
rejection (Warning for bomb/oversize/too-many) with filename, type, and correlation id.

### 🟡 OBS-2 — Client-cancellation log is at `Debug`, below the `Information` floor → never emitted
`src/PhotoPrint.API/Middleware/ExceptionHandlerMiddleware.cs:54`
**Confirmed.** Serilog `MinimumLevel.Default = Information` in every environment (no `appsettings.Production.json`),
and the source context isn't overridden, so this `LogDebug` is filtered out in dev, test, **and** prod. The
comment's "log quietly" is in practice "log never." **Fix:** emit at Information (or a dedicated low-cardinality
`request.client_aborted` event), or add a per-source Debug override.

### 🟡 OBS-3 — Pixel-bomb 422 is indistinguishable in logs from an ordinary "unreadable image" 422
`src/PhotoPrint.API/Services/ImageProcessor.cs:48`, `Services/UploadService.cs:87`
**Confirmed.** Both throw the same `UnprocessableEntityException` logged through the same generic warning;
the only differentiator is the free-text message — unlike the idempotency exceptions in the same middleware,
which emit dedicated structured events for exactly this reason. Ops can't cleanly alert on "pixel-bomb spike."
**Fix:** emit a distinct structured event (`uploads.decompression_bomb.rejected`) with dimensions + correlation id.

---

## F. Quality / altitude (report-only)

### 🟡 QUAL-1 — `AsNoTracking()` dropped on the preview read → every cache HIT change-tracks for nothing
`src/PhotoPrint.API/Services/UploadService.cs:127`
**Confirmed (git-verified).** `.AsNoTracking()` was removed *solely* to make the miss-branch write compile,
but it applies to the whole query — so on the steady-state cache **hit** path (a gallery firing N previews)
EF now allocates an identity-map entry + original-values snapshot per request for an entity it never saves.
**Fix:** keep `AsNoTracking()`; in the miss branch only, `Attach` + mark `ThumbnailPath` modified before
`SaveChanges` (or `ExecuteUpdate`).

### 🟡 QUAL-2 — Generate branch disposes the in-memory thumbnail, then re-reads it from storage
`src/PhotoPrint.API/Services/UploadService.cs:143` — on a miss the just-generated `MemoryStream` is disposed
and the file is re-opened via `GetStreamAsync`. An avoidable open+read today; a **billed network round-trip**
per first view once cloud storage lands. **Fix:** rewind and return the generated stream directly on the miss path.

### ⚪ QUAL-3 — Dimension check + `"Image dimensions exceed limits."` duplicated across two layers
`Services/UploadService.cs:83-88` and `Services/ImageProcessor.cs:46-48` — hardening one (e.g. BUG-1's
megapixel cap) risks leaving the other exploitable. **Fix:** one `ImageProcessor.ExceedsDecodeLimits(w,h)`
helper + shared message const.

### ⚪ QUAL-4 — 30-day TTL hardcoded as the magic number `2592000` in an inline header string
`Controllers/UploadsController.cs:126` — **Fix:** a named `TimeSpan.FromDays(30)`-derived constant.

### ⚪ QUAL-5 — Identical split-query behavior configured separately in both DB-provider branches
`Program.cs:33` — low value; a brief "intentional duplication" comment is acceptable if extraction isn't worth it.

---

## G. DB / migration parity

### 🟡 DB-1 — New migration drops the provider-aware pattern; hardcoded `TEXT` diverges on Postgres; DDL untested
`src/PhotoPrint.API/Migrations/20260527102718_AddUploadThumbnailPath.cs:13-18`
**Plausible — 3-lens convergence (db-parity ×2, completeness).** Migrations live in **one shared folder**
(no per-provider assembly) and are applied on Postgres via `Database.Migrate()`. This migration hardcodes
`type:"TEXT", maxLength:512`, so on Postgres the column is created as **unbounded `text`**, diverging from
the runtime Npgsql model (`character varying(512)`) — the 512 cap isn't enforced at the DB level and the
next `ef migrations add` under Npgsql scaffolds a phantom `AlterColumn` diff. Notably the *immediately-preceding*
migration (`AddOrderIdempotencyKey`) was **deliberately made provider-aware** after prior reviews — this one
regressed that pattern. And **no test applies the migration** (all fixtures use InMemory or `EnsureCreated`;
nothing calls `Migrate()`), so neither the SQLite nor Postgres DDL is exercised.
**Honest scope:** no runtime failure today (paths are ~73-char UUIDs; `text` accepts them) — this is a
parity/snapshot-drift + coverage gap. **Fix:** mirror the sibling migration —
`type: ActiveProvider == "Npgsql…" ? "character varying(512)" : "TEXT"` (safe to edit in place; no Postgres
has applied it yet). A real migration smoke test (`Migrate()` on throwaway SQLite/Testcontainers-Postgres)
belongs to the 3-env phase per the roadmap — flag, don't necessarily build now.

---

## H. Input validation

### 🟡 INPUT-1 — HEIC magic-byte check accepts any ISO-BMFF/MP4 container; and HEIC never actually decodes
`src/PhotoPrint.API/Services/MimeValidator.cs:33`
**Confirmed.** Detection checks only bytes 4–7 == `ftyp` and never the brand at 8–11, so any MP4/MOV/M4A is
classified `image/heic`, **buffered and written to disk**, then rejected only later when `IdentifyAsync`
fails → 422. The "reject non-images early by magic bytes" promise isn't met (the decode does the real
validation). Separately, ImageSharp 3.1.11 has **no HEIF decoder**, so *legitimate* HEIC also always fails —
an advertised format that never works. **Fix:** verify the HEIF brand (heic/heix/mif1/…) before returning
`image/heic`; decide whether to advertise HEIC at all.

---

## I. Tests & verification — "green ≠ proven"

**Verification run (2026-07-13):** `dotnet build` → 0 errors (pre-existing NU1603/CS1998 warnings only).
`dotnet test` → **490 passed / 0 failed (~5s)**. But the green masks large untested surfaces:

### 🔴 TEST-1 — The guest-401 self-heal branch (core of change B) has zero coverage (blocker)
`src/PhotoPrint.UI/.../error.interceptor.ts:27`
**Confirmed.** Both existing interceptor tests run with `isAuthenticated()=false && getGuestToken()=null`,
so they only exercise the *else* (logout) branch. Invert the condition, forget `clearGuestToken()`, or let
the guest path also `logout()` and every test stays green — while guest uploads break entirely. A new auth
behavior shipping with no test is a blocker. **Fix:** add interceptor specs for (a) guest token present +
not authed → `clearGuestToken()` called, logout/navigate **not** called; (b) authed + 401 → logout still fires.

### 🟠 TEST-2 — The real `ImageProcessor` (and thus the bomb guard) is never exercised — mocked everywhere
No `ImageProcessorTests` exists; `UploadService`/integration tests replace `IImageProcessor` with a fake, and
the one "dimensions exceed" test mocks `GetInfoAsync`. So the actual `Identify` guard, `stream.Position=0`
reset, and decode all run in **no** test — BUG-1 could regress silently. Story 003 AC#3 (a real oversized-image
rejection test) is **not delivered**. **Fix:** add `ImageProcessorTests` against the real class + an in-memory
storage fake (oversized header → 422; small valid image → ≤300px JPEG; non-image → null/GetInfoAsync).

### 🟠 TEST-3 — Thumbnail cache persistence across requests is unproven (shared `DbContext` masks a missing `SaveChanges`)
`UploadServiceTests` shares one `DbContext` across both calls, so the entity stays tracked with `ThumbnailPath`
set even if `SaveChangesAsync` were deleted — in prod each request gets a fresh context and would regenerate
every time (the whole feature defeated), yet the test passes. **Fix:** use two contexts on the same InMemory DB
name; assert generation happens once. Add an integration test that GETs `/preview` twice.

### 🟡 TEST-4 — Other untested surfaces (each would ship a regression green)
`Cache-Control` directive value (SEC-1) · `304`/`If-None-Match` path · migration DDL (DB-1) ·
`ensureGuestSession` short-circuit vs init · concurrent cache-miss TOCTOU (BUG-3). Add pinning/regression
tests as each corresponding finding is fixed.

---

## J. Cleared / dropped false-positives (recorded so they aren't re-raised)

- **Null `IdentifyAsync` "fail-open"** (raised by correctness-image *and* input-validation) — **refuted.**
  A verifier exercised the real ImageSharp 3.1.11 API: `IdentifyAsync` **throws** `UnknownImageFormatException`
  for unreadable input; it never returns `null`, so the `info is not null` short-circuit-then-full-decode
  path is unreachable. The dead-defensive null check is harmless. *Residual kept as **BUG-4*** (the thrown
  exception is unmapped → 500).
- **`ownerId = upload.UserId ?? upload.GuestSessionId!.Value` NRE** — **refuted.** The `isOwner` gate throws
  `ForbiddenException` before line 144 whenever both owner columns are null (a null column can't equal a
  non-null caller id), so the both-null state is unreachable — independent of the InMemory provider skipping
  `CK_Uploads_OneOwner`. (Also it would be `InvalidOperationException`, not NRE.) Defensive-clarity only.
- **CLOUD-1 — seekability / `stream.Length` / per-hit `ExistsAsync` assumptions** (correctness-image,
  correctness-cache, completeness) — **plausible but not triggerable today.** `stream.Position=0` (ImageProcessor),
  the ETag's `stream.Length` (UploadsController:128), and the per-hit `ExistsAsync` all assume a seekable/cheap
  local stream. Every current `IStorageService` returns one; the trace-constructors could not build a failure
  because no cloud provider exists yet. **Recorded as a design constraint for bolt-043**: specify seekability/Length
  on the `IStorageService` contract (or compute the ETag from a stored size), and decide whether a cache hit
  should skip `ExistsAsync`. Low; deferred, not a v1 blocker.

---

## K. Recommendation

**Request changes**, blocking on:
1. **SEC-1** — change the preview `Cache-Control` to `private` (or `no-store`); pin it with a test.
2. **BUG-1** — total-pixel cap + `MaxFrames=1` at both decode sites (+ global `MemoryAllocator` cap, REQ-1);
   add the real-`ImageProcessor` test (TEST-2).
3. **TEST-1** — cover the guest-401 interceptor branch.

Strongly recommended with the same PR (cheap, real user/ops impact): **BUG-2 + BUG-3** — deterministic
thumbnail key + cleanup-deletes-thumbnail closes the unbounded storage leak in one stroke and also satisfies
REQ-2. Everything in §D/§E/§F and DB-1/INPUT-1 can be fast-follows. **REQ-4** (document/split the bundled
guest-auth + dev-warning scope) should be resolved so change B has a requirements anchor before it merges
under a thumbnail-cache label.

> **Note (per the review system's two-loops rule):** this is a single **discovery** pass, so it cannot
> certify the feature clean — even after these fixes, closing the feature wants a saturated discovery pass.
> Verdict is `request-changes`; a fix + verification re-review produces `review-v2`.
