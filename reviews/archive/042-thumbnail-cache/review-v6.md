---
type: code-review
target: 042-thumbnail-cache
version: 6
supersedes: 5
branch: feat/bolt-042-thumbnail-cache
commit: 6c0ed93dfd669d1e7408c706f49da0f2e46500a3
base: main
reviewed: 2026-07-14
reviewer: Claude (multi-lens parallel review system)
pass-type: discovery
lenses: [correctness, security, requirements, quality, tests-coverage, db-parity, input-validation, observability, race, frontend-ux, completeness-critic]
verdict: approve-with-followups
blockers: []
---

# Review — Bolt 042: Thumbnail Cache — v6 (fresh discovery)

The second **fresh, blinded discovery pass** on this feature (v4 was the first; v2/v3/v5 were
verification). Every lens re-audited the code cold — barred from reading `reviews/` — over the
whole feature at HEAD `6c0ed93`, after all 26 v4 fixes landed and were verified by v5. 11 manifest
lenses → one dedup agent → convergence-weighted adversarial verify, via
[lib/discovery-review.wf.js](../lib/discovery-review.wf.js).

> **Commit note.** HEAD is 27 commits ahead of `origin/feat/bolt-042-thumbnail-cache` (the v4 fixes
> + all review docs are unpushed). This pass reviewed **local** HEAD `6c0ed93`; the branch should be
> pushed so this commit is reproducible.

## TL;DR

**The v4 fixes held.** Not one of the 26 verified v4 findings was re-found as an open defect by an
independent blinded lens — the same strong signal v4 gave for v1. Suites green: **.NET 531/531,
frontend 409/409**.

What this pass surfaces is **29 findings, none High** — and, exactly as in v4, the most interesting
ones are **residuals of the v4 fixes**: the decode limiter that fixed the OOM (M3) has a
CPU-count-based default that can still OOM (F1); the temp-then-move that fixed the concurrent-write
500 (M2) still races on the *move target* on Windows (F13); the 422 mapping for the allocator
backstop (L13) isn't tagged as a bomb event (F5). Fix-generativity again. The other cluster is the
**guest-auth self-heal**, whose broadened 401 handling now silently wipes a whole guest session —
including checkout contact info + cart — on any unauthenticated 401 (F2), and mis-attributes an
expired logged-in user to a throwaway guest (F3).

- 🔴 0 High
- 🟠 **8 Medium** — decode-concurrency OOM default (F1), two guest-auth self-heal misfires (F2, F3),
  the residual cache-fill/cleanup thumbnail leak (F4), a bomb-observability gap (F5), two
  documentation-contract drifts (F6, F7), and the still-unexercised cloud stream contract (F8).
- 🟡 17 Low · ⚪ 4 Cleanup

**19 confirmed · 6 plausible · 4 cleanup (unverified by design) · 1 refuted.**

**Disposition: approve-with-followups** — no blockers, 0 High. Recommended before merge: **F1, F2,
F3, F5** (runtime / data-loss impact) plus the two cheap doc fixes **F6, F7**. F4 and F8 are
accepted-deferral class (bolt-043).

**This does not certify saturation — and the curve says it isn't saturated.** v4 named ~32 new
problems; this independent pass named **24 more** (5 medium, 15 low, 4 cleanup) plus re-raised 5
known deferrals/disputes. The new-finding count is not decaying, and it is again dominated by
fix-generated residuals. Per the two-loops rule closure still wants **another** quiet discovery pass
(one that finds nothing new). See *Saturation* below.

**Cross-lens convergence is weaker than v4** (max 3, vs v4's 4). Only two findings drew 3 independent
lenses — the cache-fill/cleanup leak (F4: correctness + race + completeness-critic) and the dead
`ExistsAsync` (F9: requirements + tests + completeness-critic). Everything else is convergence 1–2.
Low convergence on a post-fix pass is itself a signal: we are sampling the long tail, not the trunk.

---

## Pass notes (methodology)

- **Efficiency machinery held.** 51 agents / 1.83M tokens: 11 lenses + 1 dedup + **39 skeptics
  (15 guard + 24 trace)** — convergence-weighting ran 39 instead of a flat 52, no session-limit
  stall. 36 raw findings deduped to 30 canonical.
- **Skeptics earned their keep on precision, as designed.** They **refuted 1** finding outright
  (F-refuted below), and — more valuably — the trace-constructor **corrected two migration
  findings** the lenses over-claimed: F24 (a lens said *neither* the SQLite nor Npgsql DDL arm runs;
  the trace found `UploadThumbnailPathMigrationTests` *does* run `Database.Migrate()` on real SQLite,
  so only the Npgsql arm is uncovered) and F25 (the snapshot "phantom AlterColumn" only arises if the
  design-time provider is switched to Npgsql, which the project never does). Both were downgraded to
  *plausible* and reconciled to the known **D23** deferral rather than shipped as fresh mediums.
- **`hinted` flag worked.** 6 findings had their topic planted by the shared project-context hints
  (dual-DB, cloud follow-up, guest-vs-logged-in auth) — F2, F3, F8, F18, F19-region, F24 — and were
  correctly denied the ≥3-convergence skeptic discount.
- **`codePack` (#4) skipped again** — impractical to author into `args`; lenses read fresh. #1–#3
  carried the win. (Backlog item in [index.md](../index.md) stands.)
- **Re-raises of accepted deferrals** (attached, not suppressed): **F24/F25** → v1 **DB-1** = ledger
  **D23** (migration DDL/snapshot parity, deferred to the 3-env phase); **F8** → v1 **CLOUD-1** =
  **D28** (cloud seekable-stream, deferred to bolt-043); **F4** → the v5 **V5-1** M1 residual =
  **D31/D34** (orphan sweep, deferred to bolt-043); **F2** → v4 **L7** = **D48** (self-heal broadened
  to swallow every unauth 401 — was *disputed*; this pass sharpens it into a concrete checkout
  data-loss scenario). All still stand; F2 is worth revisiting given the sharper scenario.

---

## A. Decode / bomb protection (fix-generated residuals)

### F1 · 🟠 Medium · `Program.cs:359` (limiter default) · confirmed · security/DoS · [D61, new — residual of D33/M3]
The v4 M3 fix added `ImageDecodeLimiter` to bound concurrent decodes — but its default slot count is
`Environment.ProcessorCount`. On a high-core / low-RAM host (e.g. an 8-core report in a 2 GB pod),
8 concurrent first-preview decodes of legal ~100 MP images (~400 MB RGBA each) sum to ~3.2 GB; the
per-allocation 512 MB backstop does not bound the *sum*, so the process is OOM-killed. The limiter
exists but its default sizing re-opens the very DoS M3 closed. **Fix:** derive the default from a
memory budget (`floor(availableRAM / perDecodeBudget)`), or require an explicit
`ImageProcessing:MaxConcurrentDecodes` in prod config. The code comment already admits the default
ignores RAM.

### F5 · 🟠 Medium · `ExceptionHandlerMiddleware.cs:106` · confirmed · observability · [D62, new]
An image that under-reports its dimensions at `Identify` (passing the pixel guard) but blows the
512 MB allocator throws `InvalidMemoryOperationException` → correctly mapped to 422, but the reserved
`uploads.decompression_bomb.rejected` event gates on `exception is DecompressionBombException` only.
So ops alerting on that event **miss exactly the bombs that evaded the primary pixel guard** — they
appear only as a generic "Handled exception" warning. **Fix:** emit the reserved event (or a
`backstop-tripped` variant) for `InvalidMemoryOperationException` too.

### F12 · 🟡 Low · `ImageProcessor.cs:23` · confirmed · input-validation · [D77, new]
The 100 MP area cap is bytes-per-pixel-blind. A legitimate ~90 MP **16-bit** RGB PNG (an A0 scan,
<50 MB compressed) passes the pixel cap, then decodes to `Rgb48` = 6 B/px = 540 MB, trips the 512 MB
backstop, and is rejected 422. A real large-format print image is refused. **Fix:** budget in bytes
(multiply area by decoded bytes/px from bit depth), or downcast to `Rgba32` on load.

### F23 · 🟡 Low · `ImageProcessor.cs:77` · plausible (dead today) · input-validation · [D78, new]
`GenerateThumbnailAsync` enforces the pixel guard only `when info is not null`; a null `Identify`
falls through to decode with only the allocator backstop. **Dead today** — ImageSharp 3.1.11 throws
rather than returning null — but a version bump that reintroduces a null return would silently
disable the primary bomb control. **Fix:** fail closed on null `Identify` (throw
`UnprocessableEntityException`), mirroring the upload-time path.

### F15 · 🟡 Low · `ImageDecodeLimiter.cs:27` · confirmed · observability · [D68, new]
When the limiter saturates (the exact burst it defends against), callers block in `WaitAsync` with no
log or metric on wait time / queue depth, so ops cannot attribute the latency to decode throttling.
**Fix:** log/metric on wait-entry or when `AvailableSlots` hits zero.

### F21 · 🟡 Low · `ImageProcessor.cs:67` · plausible · tests-coverage · [D69, new]
No test asserts the decode slot is **released when the decode throws** (bomb/format error). `using
var slot` releases today, but is unpinned — a refactor could leak permits until all previews block.
**Fix:** with a 1-slot limiter, a throwing decode must leave `AvailableSlots == 1`.

### F22 · 🟡 Low · `ExceptionHandlerMiddleware.cs:26` · plausible · tests-coverage · [D70, new]
The `InvalidMemoryOperationException → 422` mapping uses an **exact-type** `TryGetValue` and is proven
only by an injected instance of that exact type. If a future ImageSharp raised a subtype on the
backstop, the lookup would miss → raw 500, suite still green. (The trace confirmed 3.1.11 throws the
exact type, so it's a latent test-robustness gap, not a live bug.) **Fix:** match by assignable base
type, or add a decode-driven test that trips the real limit.

## B. Cache-fill / cleanup lifecycle (the recurring TOCTOU family)

### F4 · 🟠 Medium · `UploadService.cs:216` · confirmed · race · [D34 residual → D31 deferral, re-raise]
The v4 M1 fix added a `stillLive` (`DeletedAt == null`) re-check after the cache-fill write to catch
the cleanup race. But `UploadCleanupJob` commits `DeletedAt` only **after its whole `foreach`** — so
a first-preview that generates + persists a thumbnail *during* a cleanup run reads `stillLive == true`
(not yet committed) and skips deletion, while cleanup's in-memory snapshot saw `ThumbnailPath == null`
and already skipped the file. Row ends soft-deleted, thumbnail leaks forever. This is the **same class
as V5-1 / D31** (accepted, deferred to the bolt-043 orphan sweep). **Fix (the durable one):** make the
fill write conditional and atomic — `UPDATE … SET ThumbnailPath WHERE Id=@id AND DeletedAt IS NULL`
(`ExecuteUpdate`); if 0 rows affected, delete the just-written file. This also removes F17's extra
round-trip.

### F10 · 🟡 Low · `UploadCleanupJob.cs:114` · confirmed · tests-coverage · [D71, new]
The cleanup job now deletes `ThumbnailPath` (the v1 D4 fix), but if `storage.DeleteAsync(thumbnail)`
throws (locked file locally, cloud 503 in bolt-043) it is caught, `fileErrors++`, and `DeletedAt` is
**still committed** — so the row is soft-deleted and the orphaned thumbnail is never revisited. No
test injects a throwing thumbnail delete. **Fix:** test it, and ideally don't soft-delete the row when
its file delete failed so a later run retries.

### F13 · 🟡 Low · `LocalStorageService.cs:45` (`File.Move`) · confirmed · correctness · [D75, new — residual of D35/M2]
The v4 M2 fix (temp file + `File.Move(overwrite:true)`) removed the `File.Create` collision, but not
the **move-target** race: on Windows (dev), two concurrent movers — or a move against a target held by
a cache-hit reader opened `FileShare.Read` (no delete-share) — can throw `IOException` → 500. Linux
`rename` is atomic so prod is safe. **Fix:** catch `IOException` around `File.Move` and treat an
already-present target as success (last-writer-wins), or retry once.

### F14 · 🟡 Low · `LocalStorageService.cs` (`GetStreamAsync`/`DeleteAsync`) · confirmed · race · [D76, new]
Symmetric to F13 on the delete side: a cache-hit GET streams via `File.OpenRead` (`FileShare.Read`, no
delete-share); a concurrent cleanup `File.Delete` throws a Windows sharing violation, is caught +
counted, but `DeletedAt` is set anyway → orphan never revisited. Windows-dev-only (Linux unlinks).
**Fix:** open served files `FileShare.ReadWrite | Delete`, and/or re-queue paths whose delete failed.
*(Note: the lens cited line 668; the file is 85 lines — location is `GetStreamAsync`/`DeleteAsync`.)*

### F17 · 🟡 Low · `UploadService.cs:216` · confirmed · quality/efficiency · [D67, new]
The `stillLive` guard from F4 adds a third DB round-trip (`SELECT` + `UPDATE` + `AnyAsync`) to **every**
cache-miss preview, purely to catch the rare soft-delete race. Folding it into the conditional
`ExecuteUpdate` (F4's fix) removes it.

## C. Guest-auth self-heal (the bundled frontend change)

### F2 · 🟠 Medium · `error.interceptor.ts:33` · confirmed · frontend/UX · [D48 re-raise — was disputed, now sharpened]
`errorInterceptor` calls `clearGuestToken()` on **any** unauthenticated 401 app-wide, and that removes
the entire `guestSession` localStorage entry — which also holds checkout **contact info (name / email
/ phone)**, not just the token. A guest who fills the checkout form, idles until the guest token
expires, then submits, gets their contact info **and** server-side cart association silently wiped —
no toast, no redirect; only format-selector has re-init/retry. The old behavior (`logout()` +
navigate) preserved `guestSession`. This is v4's **L7/D48** (previously *disputed* against the FE-3
no-login-redirect decision) — but the concrete checkout data-loss scenario is new and materially
stronger. **Fix:** scope the self-heal (clear only on upload/preview endpoints, or clear only the
token field, preserving contact info) and let checkout surface a re-auth notice.

### F3 · 🟠 Medium · `format-selector-page.ts:232` · confirmed · frontend · [D63, new]
A **logged-in** user whose JWT expires mid-upload: the interceptor sees `isAuthenticated() == true`,
calls `logout()` (flips state false) and navigates to `/auth/login`; then the component's
`onUploadError` runs, `ensureGuestSession()` now sees not-authenticated + no guest token, **mints a
throwaway anonymous guest**, and retries — the upload succeeds under a guest orphaned from the user's
account. Same for `fetchPreviewWithRetry`. **Fix:** run the guest self-heal only when the caller was
*already* a guest (capture `!isAuthenticated()` before the request, or skip re-init once a
logout/navigation fired).

### F16 · 🟡 Low · `format-selector-page.ts:381` · confirmed · frontend · [D72, new]
On a refresh that restores N previews with an expired token, all N `getPreviewBlob` fire and 401 at
once. If init from response #1 completes (stores fresh token, `finalize` nulls `guestInit$`) before a
lagging response #k arrives, #k's interceptor `clearGuestToken()` **wipes the just-minted token** and
mints yet another session — churning sessions. Grid outcome unchanged (all dropped); wasteful. **Fix:**
guard re-init behind a per-restore-batch flag / shared retry stream.

### F18 · 🟡 Low · `error.interceptor.ts:24` · confirmed · tests-coverage · [D73, new]
The logged-in-401-during-upload interaction (F3's path — interceptor `logout()`+navigate racing the
component retry) has **zero test coverage**: every FE test sets `isAuthenticated = false`. **Fix:** add
a logged-in 401 test asserting no guest session is minted and the retry doesn't fight the navigation.

### F19 · 🟡 Low · `format-selector-page.ts:176` · confirmed · tests-coverage · [D74, new]
`onFilesAccepted`'s guest-init **error** path (initial `initAnonymousSession` fails → must mark every
file `error`) is untested; all specs mock init to succeed. A regression there leaves files stuck
`uploading` with a green suite. **Fix:** test `initAnonymousSession` throwing on `onFilesAccepted`.

## D. Documentation contract drift

### F6 · 🟠 Medium · `bolt.md:57` · confirmed · requirements · [D64, new]
`bolt.md`'s "Bundled scope" section exists precisely so a reviewer doesn't unknowingly ship a
behavior change — it lists Change B (guest-auth) and Change C (dev-warnings). **HEIC removal** (the M5
fix: `MimeValidator`, `UploadService`, `photo-upload.component`, home copy — accepted types now
JPEG/PNG only, HEIC now 415s) is a **third** user-facing contract change with no story/AC and is
absent from that list. A reviewer approving "bolt 042" ships it blind. **Fix:** add HEIC removal as a
documented bundled-scope item (Change D) with its retroactive AC.

### F7 · 🟠 Medium · `test-walkthrough.md:28` · confirmed · requirements · [D65, new]
The AC-validation doc certifies story 002 delivered with `Cache-Control: public, max-age=2592000,
immutable`. The shipped code emits `private, max-age=2592000` (no `immutable`) and the integration test
asserts `Private=true / Public=false` — the **opposite** contract (this is the security-critical SEC-1
/ D1 fix). The doc also claims "460/460, +3 tests" while the branch adds ~30 tests + new services. A
reviewer trusting the doc signs off on a shared-cacheable contract the code never emits. **Fix:**
update the walkthrough to the shipped `Cache-Control`, real test count, and service inventory. *(This
is a different file than v4's C4/D57 walkthrough fix — that drift persisted into the test-walkthrough.)*

### F20 · 🟡 Low · `implementation-plan.md:59` · confirmed · requirements · [D80, new]
The plan's AC checklist still lists `Cache-Control: public … immutable` and "reject > 25000×25000",
while the code ships `private` and a 100 MP **area** cap (a 30000×3000 = 90 MP image now passes though
it exceeds 25000 on one axis). The walkthrough recorded the substitutions; the plan's own AC list was
not reconciled. **Fix:** reconcile or annotate each AC with its documented deviation.

## E. Seams under-exercised in CI (completeness / db-parity)

### F8 · 🟠 Medium · `UploadsController.cs:155` · plausible · completeness · [D28 re-raise → bolt-043]
The cloud `IStorageService` contract this bolt is built *for* is unexercised: `stream.Length` at
`:155` assumes a seekable stream. A bolt-043 cloud provider returning a non-seekable stream (S3
`GetObject`) throws `NotSupportedException` → every preview 500s. No non-seekable stream exists today
(only `FileStream` + in-memory fake), so it's a **latent** risk, not a live bug — the standing **D28**
deferral. **Fix (bolt-043):** assert the seekable/cheap-`Length` contract, add a non-seekable fake
test.

### F9 · 🟡 Low · `IStorageService.cs:21` · confirmed (conv 3) · completeness/requirements · [D66, new]
`ExistsAsync` was added to the interface but has **no production caller** — `GetPreviewAsync` now
reads-and-catches `FileNotFoundException` instead of pre-checking, so `ExistsAsync` is referenced only
by tests and the future cloud impl. The docs describe it as the deletion-detection mechanism; a
diff-focused reviewer may assume it's load-bearing on the hot path. **Fix:** document it explicitly as
a bolt-043-only seam, or drop it until 043 needs it.

### F11 · 🟡 Low · `ImageProcessor.cs:56` · confirmed · observability · [D79, new]
`GetInfoAsync`'s broad `catch (Exception)` collapses transient storage/IO faults **and** cancellation
into "Failed to identify image" → null → 422. A storage outage is thus indistinguishable in logs and
to the client from a user uploading a junk file. **Fix:** let `FileNotFoundException` /
`OperationCanceledException` propagate (map to 404 / aborted); reserve the null path for genuine
`ImageFormatException`.

### F24 · 🟡 Low · `AddUploadThumbnailPath.cs:19` · plausible · db-parity/tests · [D23 re-raise → 3-env]
The provider-aware Npgsql DDL arm (`character varying(512)`) is exercised by no test — integration
tests use InMemory, and the migration smoke test only runs the **SQLite** arm. (The skeptic corrected
the lens's overclaim that *neither* arm runs.) A typo in the Npgsql type string surfaces only at prod
`ef database update`. Known **D23** deferral. **Fix:** apply the migration against real Postgres in CI
(Testcontainers / 3-env phase).

### F25 · 🟡 Low · `PhotoPrintDbContextModelSnapshot.cs:707` · plausible · db-parity · [D23 re-raise → 3-env]
The model snapshot records `ThumbnailPath` as `TEXT` (SQLite-flavored). The skeptic clarified this only
yields a phantom `AlterColumn` if the design-time provider is switched to Npgsql — which the project
never does (all 14 migrations are SQLite-scaffolded) — so it's the standing **D23** snapshot/parity
gap, not a fresh defect. **Fix:** the documented per-provider-migration deferral stands; note it when
scaffolding the next Npgsql migration.

## F. Cleanups (⚪ — not adversarially verified, by design)

- **F26 · `UploadsController.cs:130`** — [D81] the `uploads.decompression_bomb.rejected` log template is
  duplicated verbatim in the controller and the middleware; a rename to one diverges the alert. Hoist
  to a shared constant.
- **F27 · `format-selector-page.ts:420`** — [D82] `dropRestoredEntry` duplicates `onRemoveUpload`'s
  body verbatim (from the M8/L8 fix). Delegate to one private `removeByClientId` helper.
- **F28 · `ExceptionHandlerMiddleware.cs:64`** — [D83] the new `client_aborted` branch reads
  `Items["CorrelationId"]` directly instead of `context.GetCorrelationId()` (the documented
  convention used 10 lines below). Use the extension.
- **F29 · `LocalStorageService.cs:53`** — [D84] `Saved upload to {Key}` / `Deleted upload {StoragePath}`
  log at `Debug` under an `Information` floor, so they never emit — the only per-file storage-mutation
  trace is invisible (compounds F11/F14/F5). Raise the delete trace to `Information`.

## G. Recorded false positive (dropped, not carried)

- **`UploadService.cs:208` — "orphan-reclaim swallowed, ops never learn"** — **REFUTED.** The claim was
  that a swallowed best-effort `DeleteAsync` failure hides the leak. But the `orphaned_on_commit_failure`
  **warning is emitted unconditionally *before* the swallowed delete** — it signals the orphan itself,
  not a handled state, so ops already have the signal regardless of the delete's outcome. No failing
  outcome exists. (Recorded so it isn't re-raised next pass.)

---

## Saturation — why this is not `approved`

Per *Recall & convergence* in the [README](../README.md), a feature is certified only when **K
consecutive independent full-breadth discovery passes find nothing new** — and this pass found plenty.

| Pass | Type | New (H/M/L/C) | Re-raises of deferrals |
|------|------|---------------|------------------------|
| v4 | discovery | 0 / 11 / 14 / 7 | 4 |
| **v6** | **discovery** | **0 / 5 / 15 / 4** | **5** |

The new-finding count is **not decaying** into quiet, and it is again dominated by **fix-generated
residuals** (F1, F5, F13, F14, F17 all trace to v4 fixes) plus a new sweep of the long tail (Windows
storage races, bit-depth budget, doc drift). As with v4 and bolt-035, the two discovery passes ran
against **different commits** (v6 is post-v4-fix), so their overlap cannot feed a capture–recapture
population estimate — the honest signal is qualitative: **among defects still open at v6's commit
(the deferrals D23/D28/D31/D48) v6 re-found all of them, and added 24 new.** The feature is **not
saturated.**

**Recommendation:** fix F1/F2/F3/F5 + the cheap doc items F6/F7, defer F4/F8 to bolt-043, then run
**one more** blinded discovery pass. If *that* pass comes back quiet (0 new mediums, only long-tail
cleanups), the feature is a candidate for `approved`.

Full per-finding detail (scenario / fix / guard+trace evidence) for all 29 findings:
[findings-v6.md](findings-v6.md). Cross-pass identity mapping: [ledger.md](ledger.md).
