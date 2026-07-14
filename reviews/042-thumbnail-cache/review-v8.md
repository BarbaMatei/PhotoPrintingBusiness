---
type: code-review
target: 042-thumbnail-cache
version: 8
supersedes: 6
branch: feat/bolt-042-thumbnail-cache
commit: e2093bdd596107d2e67ff4a4135c47e4530f6eb4
base: main
reviewed: 2026-07-14
reviewer: Claude (multi-lens parallel review system)
pass-type: discovery
lenses: [correctness, security, requirements, quality, tests-coverage, db-parity, input-validation, observability, race, frontend-ux, completeness-critic]
verdict: approve-with-followups
blockers: []
---

# Review — Bolt 042: Thumbnail Cache — v8 (fresh discovery)

The **third fresh, blinded discovery pass** on this feature (v4 was the first, v6 the second;
v2/v3/v5/v7 were verification). Every lens re-audited the code cold — barred from reading `reviews/` —
over the whole feature at HEAD `e2093bd`, after all v6 fixes (F1/F2/F3/F5 + doc F6/F7) landed and were
verified by v7. 11 manifest lenses → one dedup agent → convergence-weighted adversarial verify, via
[lib/discovery-review.wf.js](../lib/discovery-review.wf.js).

> **Model note.** This pass ran on **Opus 4.8** (`claude-opus-4-8`). Three earlier launch attempts on
> Fable 5 died on the model's session limit before any lens completed and were discarded; the run
> recorded here is a clean single Opus run (53 agents, 0 errors, 2.32M tokens). Source under review is
> frozen at `e2093bd`; the one commit on top of it (`9e9afe1`) is docs-only (fixer-contract v2 in the
> README) and touches no reviewed source.

## TL;DR

**The feature core is solid and the v6 fixes held.** Third discovery pass in a row with **0 High**, and
not one verified v6 fix was re-found as an open defect by an independent blinded lens. Suites green:
**.NET 535/535, frontend 413/413**.

This pass surfaces **28 findings (0 High)** — and for the first time the new-finding curve is clearly
**decaying** (v4: 32 → v6: 24 → v8: **13 genuinely new**; the other 15 are re-raises of already-catalogued
open/deferred items). But it is **not quiet**: it names **5 new mediums**, two of them fresh
**fix-generated residuals** — and one of those, F3, **defeats the F2 contact-info fix that v7 just
verified**. The single most valuable catch is a lens finally auditing the long-under-reviewed bundled
Change C: the **global `SplitQuery` default can drop/misplace items in paginated admin-order queries**
that lack a unique tiebreaker (F2).

- 🔴 0 High
- 🟠 **7 Medium** — the recurring cleanup/cache-fill orphan race (F1, deferred class), the global
  split-query paging hazard (F2, new), the F2-fix contact-info wipe on self-heal re-init (F3, new),
  a bomb-alert test that lets alerting silently regress (F4, new), two observability gaps on
  storage-integrity incidents (F5/F6, new), and the bit-depth-blind pixel cap (F7, re-raise bumped from low).
- 🟡 17 Low · ⚪ 4 Cleanup

**19 confirmed · 5 plausible · 4 cleanup (unverified by design) · 3 refuted.**

**Disposition: approve-with-followups** — no blockers, 0 High. Recommended before merge: **F2, F3, F4**
(data correctness / data loss / a security-alert signal that can die green) plus the cheap observability
lines **F5, F6** and the doc fix **F23**; **F7** is worth a bounded fix. **F1** stays the accepted-deferral
class (bolt-043 orphan sweep, with F18 a hard-kill facet); **F14/F24** → 3-env / bolt-043.

**This does not certify saturation — but the curve is finally bending.** See *Saturation* below.

---

## Pass notes (methodology)

- **Efficiency machinery held.** 53 agents / 2.32M tokens: 11 lenses + 1 dedup + **41 skeptics
  (15 guard + 26 trace)** — convergence-weighting ran 41 instead of a flat 54, no stall. 38 raw findings
  deduped to 31 canonical.
- **Convergence is low (max 3), as expected on a third post-fix pass.** Only two findings drew 3
  independent lenses — the cleanup/cache-fill orphan race (F1: correctness + race + completeness-critic)
  and the Npgsql-DDL parity gap (F14: tests + db-parity + completeness-critic, correctly `hinted`).
  Everything else is convergence 1–2. We are sampling the long tail, not the trunk.
- **Skeptics earned their keep on precision (3 refutations).** They **refuted 3** candidates outright —
  a "benign near-limit image logged as a bomb attack" (the 512 MB backstop is per-allocation, not a
  cumulative peak, so a legal ≤100 MP 8-bit image can't trip it), the "fail-open on null `Identify`"
  (ImageSharp 3.1.11 throws rather than returning null; dead today — re-raise of D78), and an
  "orphan-reclaim swallowed silently" (the `orphaned_on_commit_failure` warning already fired before the
  swallowed delete — same refutation as v6's recorded FP). All three recorded in §G so they aren't
  re-litigated.
- **`hinted` flag worked.** 2 findings had their topic planted by the shared hints (Npgsql parity F14,
  cloud stream F24) and were correctly denied the ≥3-convergence skeptic discount.
- **`codePack` (#4) skipped again** — impractical to author into `args`; lenses read fresh.

---

## A. Decode / bomb protection

### F4 · 🟠 Medium · `UploadServiceTests.cs:480` · confirmed · tests-coverage · [D87, new]
The pixel-bomb rejection test asserts only the **base** `UnprocessableEntityException` + a
`"*dimensions exceed*"` message. But every bomb-alert emitter — `ExceptionHandlerMiddleware` **and** the
batch-controller catch — gates on `ex is DecompressionBombException` (the *derived* type). Regress
`UploadService` to throw the plain base with the same message and the test **stays green** (FluentAssertions
`ThrowAsync<T>` matches subclasses), while `uploads.decompression_bomb.rejected` **silently stops firing
for both single and batch vectors** — ops alerting on the security signal goes dark. **Fix:** assert
`ThrowAsync<DecompressionBombException>()` and verify `.WidthPx/.HeightPx`, pinning the derived type the
event keys on. Cheap; protects a security signal.

### F7 · 🟠 Medium · `ImageProcessor.cs:23` · confirmed · input-validation · [D77 re-raise — bumped 🟡→🟠]
The 100 MP area cap is **bytes-per-pixel-blind**. A legitimate ~72 MP **16-bit RGBA** PNG (an 8500×8500
ProPhoto print scan) passes the pixel cap and stores fine (upload only `Identify`s), then the first
`GET /preview` decodes to `Rgba64` (8 B/px ≈ 578 MB), trips the 512 MB allocator backstop → 422 **plus a
false `decompression_bomb.rejected` alert**, and the photo is **permanently un-previewable** (every retry
re-trips). Raised to medium this pass because the outcome is a permanent, silent loss of a legitimate
large-format upload — the exact customer this print business serves. This is v6's **D77/F12** (was low).
**Fix:** budget decoded *bytes* not pixels — force a fixed 4 B/px decode via `Image.LoadAsync<Rgba32>` in
`LoadSingleFrameAsync` (the thumbnail down-converts anyway), or lower `MaxDecodePixels` to 64 MP so a
worst-case 8 B/px source stays ≤ 512 MB.

### F12 · 🟡 Low · `ImageProcessor.cs:67` · plausible · tests-coverage · [D69 re-raise]
No test asserts the decode-limiter slot is **released when a decode throws** (bomb / corrupt file). The
`using var slot` releases correctly today, but is unpinned — a refactor to a manual acquire without
`finally` would leak permits until every preview blocks (self-inflicted DoS). This is v6's **D69/F21**,
still open. **Fix:** after a throwing `GenerateThumbnailAsync`, assert `limiter.AvailableSlots` returned to max.

### F16 · 🟡 Low · `ImageProcessor.cs:81` · plausible · tests-coverage · [D42 residual re-raise]
The frame-bomb defence (`MaxFrames=1`) is verified only on the internal `LoadSingleFrameAsync` helper via
reflection, never **through** `GenerateThumbnailAsync`. Drop `DecoderOptions{MaxFrames=1}` at the real call
site (line 81) and a thousand-frame GIF again materialises every frame on decode, yet the isolated helper
test still passes and the JPEG output is single-frame regardless — suite stays green. The v4 M11/**D42** fix
landed the cap but pinned it at the wrong seam. **Fix:** decode a real multi-frame GIF through
`GenerateThumbnailAsync`, or spy the decode call site.

## B. Cache-fill / cleanup lifecycle (the recurring TOCTOU family)

### F1 · 🟠 Medium · `UploadCleanupJob.cs:101` · confirmed (conv 3) · race · [D34/D31 re-raise → bolt-043]
`UploadCleanupJob` batch-loads expiring uploads, then in its `foreach` deletes files off the **in-memory
snapshot** (`if (upload.ThumbnailPath is not null)`) and commits `DeletedAt` only after the whole loop. A
first-preview that persists `ThumbnailPath` *after* the batch load but *before* the soft-delete commit is
invisible to the job: cleanup's stale `null` snapshot skips the thumbnail delete, the preview's post-write
`stillLive` re-check still sees the row live (not yet committed) so it keeps the file, and the row ends
soft-deleted with a thumbnail **no path ever revisits → orphaned forever**. `Upload` carries no concurrency
token. This is the **same class as V5-1 / D31 / D34** (accepted, deferred to the bolt-043 orphan sweep).
**Fix (the durable one):** make the fill write a conditional atomic `UPDATE … WHERE Id=@id AND DeletedAt
IS NULL` (`ExecuteUpdate`; 0 rows → delete the just-written file) **and** have cleanup also attempt the
derivable key `thumbs/{owner}/{id}.jpg` for every candidate. Also removes F11's extra round-trip.

### F6 · 🟠 Medium · `UploadService.cs:219` · confirmed · observability · [D89, new]
The other face of F1's race: when the post-write `stillLive` check *does* fire `false` (row soft-deleted
mid-fill), the code deletes the just-written thumbnail **with no log**, and the `SaveChanges` that persisted
`ThumbnailPath` was keyed on `Id` only (no `DeletedAt` guard), so the soft-deleted row is left with a
`ThumbnailPath` pointing at a now-deleted file — a **silent partial DB/file state**. Every sibling anomaly
path (`cache_miss_missing_file` at :163, `orphaned_on_commit_failure` at :205) logs a distinct event; this
one is invisible to ops. **Fix:** emit a distinct Warning (e.g. `uploads.thumbnail.deleted_row_race
upload_id=…, key=…`) around the delete, consistent with its neighbours.

### F5 · 🟠 Medium · `UploadService.cs:183` · confirmed · observability · [D88, new]
When an upload's **original** blob is gone while its DB row is still live (ops-side deletion, storage
fault), `GetPreviewAsync`'s `catch (FileNotFoundException)` throws a plain `NotFoundException("no longer
available")` **with no log**, and the middleware records only the generic `Handled … NotFoundException` —
**identical to a routine unknown-id 404**. So a genuine storage-integrity incident hides in ordinary 404
noise, while the far less severe *missing-cache* case (:163) gets a distinct reserved event. **Fix:** log a
distinct Warning (`uploads.original.missing_file upload_id=…`) at the catch site before throwing, mirroring
the cache-miss event.

### F11 · 🟡 Low · `UploadService.cs:216` · confirmed · quality/efficiency · [D67 re-raise]
The `stillLive` guard (from the M1/D34 fix) adds a **third** DB round-trip — `SELECT` + `UPDATE` +
`AnyAsync` — to **every** cache-miss preview, purely to catch the rare soft-delete race. Cold path, so low
impact, but it re-reads the row it just wrote. Folding it into the conditional `ExecuteUpdate` (F1's durable
fix) removes it. This is v6's **D67/F17**, still open. **Fix:** the conditional atomic update.

### F18 · 🟡 Low · `UploadService.cs:187` · confirmed · race · [D31 re-raise — hard-kill variant]
A cache-miss writes the thumbnail to the deterministic key, then the process is **SIGKILLed / OOM-killed**
before `SaveChangesAsync` commits `ThumbnailPath` (the catch-and-delete can't run on hard termination). The
row keeps `ThumbnailPath=null`; if the upload is never previewed again, cleanup sees `null` and deletes only
`FilePath` → the thumbnail leaks permanently. A distinct trigger from F1 but the **same D31 orphan-sweep
deferral**. **Fix (bolt-043):** the orphan sweep — cleanup attempts the derivable key for every candidate,
not just when `ThumbnailPath` is set.

## C. Guest-auth self-heal (the bundled frontend change)

### F3 · 🟠 Medium · `format-selector-page.ts:205` · confirmed · frontend/UX · [D86, new — residual of the F2/v6 fix]
**The v6 F2 fix is defeated by the very next self-heal re-init.** F2 (verified in v7) changed
`clearGuestToken()` to preserve checkout contact info — it `delete`s only the `guestToken` field and keeps
`name/email/phone`. But when a stale-token upload 401s, `performUpload`'s error path calls
`ensureGuestSession()`, which re-inits and runs `storeSession({ guestToken, firstName:'', lastName:'',
email:'', phone:'' })` — a **full `localStorage.setItem` overwrite** of the whole `guestSession` key with
empty strings (`guest-auth.service.ts:47-49`). So the contact info F2 just preserved is **wiped anyway** on
the next line of the self-heal, exactly the checkout data-loss F2 was written to stop. Only
`clearGuestToken` is tested in isolation; the clear → re-init sequence is not. **Fix:** `storeSession` should
merge (preserve existing contact fields when re-initing with an empty profile), or `ensureGuestSession`
should re-hydrate them; add a test driving clear → re-init and asserting contact info survives. *(Textbook
fix-generativity — the F2 fixer patched the instance it was handed and didn't sweep the adjacent
`storeSession` overwrite; README fixer-rule #1.)*

### F17 · 🟡 Low · `format-selector-page.ts:214` · confirmed · tests-coverage · [D50 residual re-raise]
`ensureGuestSession`'s recovery **after an init error** is untested. If `initAnonymousSession` errors,
`finalize` nulls `guestInit$` so the next call re-inits and self-heals — but every one of the 12 specs mocks
init as success (`of()` / `Subject.next`), so the error-then-retry reset (the path that lets a guest recover
for the tab's lifetime) is never driven. Regress the reset and the suite stays green. Residual of the
v4 L9/**D50** shareReplay-reset fix (which tested only the completion path). **Fix:** a spec where init
errors once then later succeeds, asserting the second `ensureGuestSession` fires a fresh init and resolves.

### F19 · 🟡 Low · `error.interceptor.ts:33` · confirmed · frontend/UX · [D94, new]
The guest-401 self-heal is **format-selector-only**. When a guest's token expires **off** the upload page
(checkout / cart), the interceptor calls `clearGuestToken()` with **no toast, no navigation, no re-init** —
subsequent requests carry no token and keep 401ing, leaving the guest stuck with **zero feedback** (e.g. an
EuPlatesc pay attempt just clears `euPlatescLoading` and shows nothing). Pre-branch, a 401 at least navigated
them somewhere. **Fix:** show a session-expired toast on the unauthenticated-401 branch, or re-init a guest
session app-wide, so non-upload guest flows aren't left silently broken.

## D. Bundled Change C — global split-query default (previously under-reviewed)

### F2 · 🟠 Medium · `AdminOrderService.cs:67` · confirmed · correctness/data · [D85, new]
`Program.cs:37/39` now sets `UseQuerySplittingBehavior(SplitQuery)` **globally** for both SQLite and Npgsql.
`GetOrdersAsync` does `OrderByDescending(o => o.CreatedAt).Skip().Take().Include(o => o.Items)` with **no
unique tiebreaker** (`CreatedAt` is not unique). This is EF Core's documented Skip/Take + non-unique-order +
split-query hazard: the parent-page query and the `Items` child query run as **separate round-trips** with no
wrapping transaction, so at a page boundary where two orders share `CreatedAt` the tie can resolve differently
between the two statements — an admin order on the page can come back with **missing items** (`Sum quantity`
wrong) on Postgres under concurrent inserts / plan shifts. (The skeptic corrected the lens's "items of other
orders" wording — the child joins by PK, so the real symptom is *missing* items, not cross-order items.) No
prior pass audited this correctness angle of Change C — it was only ever seen as the QUAL-5/D22 duplicated
*config*. **Fix:** add a unique `ThenBy(o => o.Id)` to every `Skip/Take` + collection-`Include` query under
the new global default (audit `ProductService`, `CartService`, `OrderService` too), or keep single-query as
the default and opt specific queries into split. This is arguably its own bolt.

### F23 · 🟡 Low · `bolt.md:73` · plausible · requirements · [D91, new — doc/scope]
Bundled Change C (the global split-query default) is a **real change to production query execution** but got
only a prose bullet in `bolt.md` claiming *"No behavior change in production"* — no retroactive AC (unlike
Changes B and D) and **no test** (the comment concedes InMemory doesn't exercise it). A reviewer approving
"bolt 042" ships a query-execution change with no coverage, and F2 shows the "no behavior change" label is
wrong. **Fix:** give Change C a retroactive AC like B/D, correct the wording, and add/reference a query test
(the F2 tiebreaker fix would carry one).

## E. Storage / Windows-dev races & cache directives

### F8 · 🟡 Low · `UploadsController.cs:26` · confirmed · security · [D90, new — residual of D1/SEC-1]
The SEC-1/D1 fix changed the preview `Cache-Control` from `public` to `private, max-age=2592000` — but the
30-day max-age with **no revalidation** means the bytes are recoverable **device-locally**. A guest on a
shared/public PC previews personal photos (200 + those headers → browser caches the image); the guest token
is later cleared/expires; within 30 days the **next user of that browser profile** reopens the `/preview` URL
from history and the browser serves the cached bytes **with no request** — the ETag/`If-None-Match`
revalidation never fires and the server never re-checks ownership. `private` bars shared/proxy caches, not
the local per-profile cache. **Fix:** `private, no-cache` (or a short max-age) so the browser revalidates;
the existing ETag keeps it cheap (304 for the owner, fresh 403/404 for a new caller).

### F10 · 🟡 Low · `LocalStorageService.cs:45` · confirmed · correctness · [D75 re-raise — refined]
`File.Move` over a file **open for reading**: a cache-hit request streams `thumbs/{owner}/{id}.jpg` via
`File.OpenRead` (`FileShare.Read`, no `Delete`), while a concurrent request that read a pre-fill snapshot
regenerates and `File.Move(overwrite:true)` over the same key. On Windows the replace needs
`FILE_SHARE_DELETE` → sharing violation `IOException` → unmapped 500. Linux `rename` over an open fd
succeeds, so **dev-only**. This is v6's **D75/F13** (concurrent *writers*), now seen from the reader-holds-
the-handle angle. **Fix:** open served files `FileShare.ReadWrite | Delete`, or catch/retry the `IOException`
on `Move`; no action strictly needed for Linux prod.

## F. Blob-URL leaks (residuals of the C1/D54 fix) & cloud/CI seams

### F20 · 🟡 Low · `photo-thumbnail.component.ts:86` · confirmed · frontend · [D95, new — residual of C1/D54]
For freshly-uploaded (in-session) photos `previewUrl` is unset, so the template calls `localUrl()` →
`URL.createObjectURL(state.file)` on **every change-detection cycle**. Upload progress events fire many
`updateUpload` → new object ref → OnPush re-render per upload, minting **dozens of blob URLs** that are never
tracked; `revokeAllPreviews` only frees `previewUrl`, so they leak for the tab's life. The C1/**D54** fix
handled restored previews but left this template-method path. **Fix:** create the object URL once (cache it
per `clientId` / on the state) and revoke it alongside `previewUrl`.

### F21 · 🟡 Low · `format-selector-page.ts:404` · confirmed · frontend · [D92, new — residual of C1/D54]
On refresh, `restoreFromSession` fires a `getPreviewBlob` per restored upload with **no teardown tied to the
component lifetime**. Navigate away before the responses arrive: `ngOnDestroy` → `revokeAllPreviews` finds
nothing to revoke, then the surviving subscription resolves, mints a `URL.createObjectURL`, and stores it via
`updateUpload` on the **destroyed** component — never revoked. One leaked blob URL per pending preview. **Fix:**
`takeUntilDestroyed` / `DestroyRef` on the `getPreviewBlob` subscriptions, or revoke any URL produced after destroy.

### F22 · 🟡 Low · `ImageDecodeLimiter.cs:30` · confirmed · completeness · [D96, new — residual of F1/D61]
The v6 F1/D61 fix sized `RecommendedMaxConcurrentDecodes` as `min(cores, RAM/512MB)` so `slots × 512MB ≤ RAM`
— but that budget ignores **concurrent upload buffering**: `UploadsController` buffers each upload into a
`MemoryStream` (~50 MB/file) with no concurrency bound (the limiter caps *rate*, not in-flight count). On a
2 GB / 8-core host the recommender reserves the whole 2 GB for 4 decode slots with **zero headroom**; a burst
of ~20 large uploads buffering ~1 GB *plus* the decodes overshoots RAM → the OOM the decode limiter was meant
to prevent. **Fix:** fold upload-buffer memory into the budget (or bound concurrent upload buffering), and
document the exclusion.

### F13 · 🟡 Low · `UploadFactory.cs:239` · confirmed · tests-coverage · [D93, new]
No integration test proves the bomb / oversize → 422 path **end-to-end**: the test factory's
`FakeImageProcessor.GetInfoAsync` always returns `800×600`, so an oversized upload can't be simulated through
the HTTP pipeline (`UploadsController` → `UploadService.ExceedsDecodeLimits` → middleware 422). A wiring break
across that chain is uncatchable; only isolated units cover it, and the real gate lives in the unmocked
`ImageProcessor`. **Fix:** an integration test with a factory override / second fake returning oversized
dimensions, asserting HTTP 422 + the reserved `decompression_bomb` event.

### F15 · 🟡 Low · `ImageDecodeLimiter.cs:30` (`AcquireAsync`) · confirmed · observability · [D68 re-raise]
When the limiter saturates (the exact burst it defends against), callers block in `WaitAsync` with **no log
or metric** on wait time / queue depth, so an operator debugging slow previews under load gets no signal that
the process-wide decode gate is the bottleneck. `AvailableSlots` is exposed for tests but never surfaced.
v6's **D68/F15**, still open. **Fix:** log at Information (Debug is below the Serilog floor) on wait-entry or
when slots hit zero, or expose a saturation gauge.

### F9 · 🟡 Low · `IStorageService.cs:21` · plausible · requirements/quality · [D66 re-raise]
`ExistsAsync` was added to the interface but has **no production caller** — `GetPreviewAsync` detects a
vanished cache via `catch (FileNotFoundException)` on `GetStreamAsync`, not `ExistsAsync` (its own comment at
:148-150 explicitly rejects exists-then-get). The implementation-walkthrough calls it "used to detect ops-side
deletions" and "needed here" — both false — and `UploadServiceTests` even mocks it on the cache-hit path where
the SUT never calls it (false confidence; see F26). v6's **D66/F9**, still open. **Fix:** document it as
bolt-043-only scaffolding (currently unused), or drop it until 043 needs it.

### F14 · 🟡 Low · `AddUploadThumbnailPath.cs:19` · confirmed (conv 3, hinted) · db-parity/tests · [D23 re-raise → 3-env]
The provider-aware **Npgsql** DDL arm and the SQLite-flavored snapshot are exercised by no test — integration
tests use InMemory (migrations ignored), and `UploadThumbnailPathMigrationTests` runs only the **SQLite/TEXT**
arm via `Database.Migrate()`. A typo in the Npgsql type string ships green and surfaces first at the initial
Postgres `ef database update`. The standing **D23** deferral (3-env / Testcontainers). **Fix:** apply the
migration against real Postgres in CI; interim, assert the emitted DDL per `ActiveProvider`.

### F24 · 🟡 Low · `UploadsController.cs:155` · plausible (hinted) · completeness · [D28 re-raise → bolt-043]
`GetPreviewAsync` computes the ETag from `stream.Length` and returns `File(stream)`, assuming a **seekable
stream with a known Length**. `LocalStorageService` returns a seekable `FileStream`, so it works today; a
bolt-043 cloud provider returning a non-seekable network stream (S3 `GetObject`) throws
`NotSupportedException` → every preview 500s. The interface XML-doc states no seekability/Length contract and
no test enforces it. Latent, not live — the standing **D28** deferral. **Fix (bolt-043):** document the
contract and add a non-seekable-fake contract test, or don't rely on `Length`.

## G. Cleanups (⚪ — not adversarially verified, by design)

- **F25 · `UploadsController.cs:122`** — [D81 re-raise, worsened] the `uploads.decompression_bomb.rejected`
  emission is now copy-pasted in **three** places (controller batch catch + twice in the middleware) — v6's
  D81 noted two; the v6 F5 fix added the third. The controller copy also **omits the `source=` dimension**
  the middleware copies carry, so ops parsing on `source` can't distinguish batch bombs. Extract one helper
  (`LogBombRejected(logger, correlationId, source, w?, h?)`) and give the batch path a `source=batch` dimension.
- **F26 · `UploadServiceTests.cs:296`** — [D66 re-raise, test side] the `ExistsAsync ⇒ true` Moq stubs on the
  cache-hit tests are **inert** (the SUT never calls it) and actively harmful: if an exists-then-get TOCTOU is
  reintroduced, these stubs pre-answer `true` so the tests won't catch the resulting 500. Delete the dead stubs
  (couples with F9).
- **F27 · `implementation-plan.md:17`** — [D59 re-raise, another file] the plan still specifies
  `varchar(500)` for `ThumbnailPath` while the story, migration, `UploadConfiguration` (`HasMaxLength(512)`)
  and snapshot all use **512**. The same stale-token doc-drift class as v4 C6/D59 (which fixed the story) —
  the unit of fix is the token repo-wide, not one file. Update to 512.
- **F28 · `UploadsController.cs:158`** — [D97, new] conditional-GET is tested only with an exact strong-tag
  echo; a weak validator (`W/"…"`), a comma-separated `If-None-Match` list, or `*` never matches the
  `StringValues == etag` compare, so 304 silently degrades to a full 200 (extra bandwidth). Parse
  `If-None-Match` per RFC (or use built-in conditional support) + test weak/multi-value cases.

## H. Recorded false positives (dropped, not carried)

- **`ImageProcessor.cs:77` — "fail-open on null `Identify` before full decode"** — **REFUTED** (re-raise of
  **D78**). The pixel guard is skipped `when info is null`, but ImageSharp 3.1.11's `IdentifyAsync` returns
  non-null or throws (caught → 422), so `info` is never null at that line and the branch is dead today. The
  512 MB backstop + `MaxFrames=1` also fire during decode regardless. Latent-on-library-upgrade only, as v6
  already recorded. (Kept in the ledger as D78, `refuted-this-pass`.)
- **`ExceptionHandlerMiddleware.cs:116` — "benign near-limit image logged as a bomb attack"** — **REFUTED.**
  `AllocationLimitMegabytes=512` is a **per-single-allocation** cap in ImageSharp 3.1.11, not a cumulative /
  peak budget. A legal ≤100 MP 8-bit image is one ~400 MB buffer (< 512), and the downscale-to-800px resize
  allocates only small separate buffers — no single allocation nears 512 MB, so the backstop can't trip on a
  benign image. `source=allocator_backstop` fires only on a dimension-lying bomb that evaded the pixel guard,
  which is exactly what it's meant to flag.
- **`UploadService.cs:208` — "orphan-reclaim delete swallowed, ops never learn"** — **REFUTED** (same as v6's
  recorded FP). The `orphaned_on_commit_failure` warning (with the storage key) is emitted **before** the
  best-effort delete, so the orphan is signalled regardless of whether the compensating `DeleteAsync` throws;
  the empty catch only drops a redundant second log.

---

## Saturation — why this is not `approved`

Per *Recall & convergence* in the [README](../README.md), a feature is certified only when **K consecutive
independent full-breadth discovery passes find nothing new.** This pass found new mediums, so it isn't K yet —
but the trend is, for the first time, encouraging.

| Pass | Type | New (H/M/L/C) | New total | Re-raises of open/deferred |
|------|------|---------------|-----------|----------------------------|
| v4 | discovery | 0 / 11 / 14 / 7 | 32 | 4 |
| v6 | discovery | 0 / 5 / 15 / 4 | 24 | 5 |
| **v8** | **discovery** | **0 / 5 / 7 / 1** | **13** | **15** |

Two signals point toward approaching saturation: (1) the **new-finding count is decaying** for the first time
(32 → 24 → **13**), and (2) **15 of 28 findings are re-raises** of already-catalogued open/deferred items
(D23, D28, D31/D34, D42, D50, D66, D67, D68, D69, D75, D77, D78, D81, D59) — the search is increasingly
re-covering known ground rather than opening new. Three of these passes ran against **different commits**
(each post-fix of the last), so their overlap cannot feed a capture–recapture population estimate; the honest
read is qualitative.

But it is **not** saturated: **5 new mediums**, and — the load-bearing signal — **the fix-generativity loop is
still live.** F3 **defeats the F2 fix that v7 verified one pass ago**; F22 is a fresh residual of the v6 F1
decode-limiter; F20/F21 are residuals of the C1 blob-URL fix. This is exactly the failure the README's new
fixer-contract rules (§*Bounding fix-generativity*) target — each round's fixes re-seed the population the next
discovery pass then mines. Until a fix round applies those rules (class sweep, new-mechanism bar, fresh-eyes
micro-review) and the *next* discovery pass comes back quiet, closure stays open.

**Recommendation:** fix **F2, F3, F4** (data correctness / the F2-fix data-loss regression / the bomb-alert
test) + the cheap observability lines **F5, F6** and doc **F23**, apply a bounded fix to **F7**; defer **F1/F18**
(orphan sweep) and **F24** to bolt-043 and **F14** to the 3-env phase — then run **one more** blinded discovery
pass **after** a fix round that follows the new fixer rules. If that pass is quiet (0 new mediums, only
long-tail cleanups), the feature is a candidate for `approved`.

Full per-finding detail (scenario / fix / guard+trace evidence) for all 31 findings:
[findings-v8.md](findings-v8.md). Cross-pass identity mapping: [ledger.md](ledger.md).
