---
type: code-review
target: 042-thumbnail-cache
version: 4
supersedes: 3
branch: feat/bolt-042-thumbnail-cache
commit: 9e44714b7e87f7d6cfef54c27e130f5a1133e313
base: main
reviewed: 2026-07-14
reviewer: Claude (multi-lens parallel review system)
pass-type: discovery
lenses: [correctness, security, requirements, quality, db-parity, input-validation, observability, race, frontend-ux, tests-coverage, completeness-critic]
verdict: approve-with-followups
blockers: []
---

# Review — Bolt 042: Thumbnail Cache — v4 (fresh discovery)

The first **fresh, blinded discovery pass** since v1 (v2/v3 were verification passes). Every lens
re-audited the *fixed* code cold (barred from reading `reviews/`), over the whole feature at HEAD
`9e44714`. This is the pass the index flagged as needed for feature-closure. 11 manifest lenses →
one dedup agent → convergence-weighted adversarial verify, via
[lib/discovery-review.wf.js](../lib/discovery-review.wf.js).

## TL;DR

**The v1 fixes held.** Not one of the three v1 blockers (public-cache leak, decode-bomb bypass,
guest-auth untested) was re-found by an independent blinded lens — the strongest signal a
verification pass can't give you. The suites are green: **.NET 515/515, frontend 403/403**.

What this pass *did* surface is **32 findings, none High** — and the most interesting ones are
**regressions the v1 fixes introduced**: making the thumbnail key deterministic (the BUG-3 fix)
closed the orphan-leak but opened a *concurrent-write* collision and a *write-vs-cleanup* race.
That's textbook fix-generativity, and it's exactly why a fresh discovery pass after fixing matters.

- 🔴 0 High
- 🟠 **11 Medium** — cache-fill concurrency/lifecycle (M1, M2, M6), a decode-concurrency DoS (M3),
  observability gaps (M4, M7), HEIC over-accept (M5), a guest-expiry UX dead-end (M8), and test
  gaps on the security-critical paths (M9, M10, M11)
- 🟡 14 Low · ⚪ 7 Cleanup

20 confirmed · 4 plausible · 1 refuted · 7 cleanup (unverified by design).

**Disposition: approve-with-followups** — no blockers. Recommended before merge: **M1–M6** (the
runtime-impacting ones). Per the two-loops rule this is one pass, so it cannot emit `approved`;
closing the feature still wants a *saturated* discovery pass (a later one that finds nothing new).

**Cross-lens convergence:** the cache-hit TOCTOU (L1) was hit by **4** lenses; the write-vs-cleanup
orphan (M1) and the batch bomb-event gap (M4) by **3** each.

---

## Pass notes (methodology — read this)

- **This is a re-run.** The first attempt was **void**: an `args`-passing bug (this harness delivers
  the workflow `args` as a JSON *string*, and the script read fields off the raw string) meant only
  the 6 default lenses ran, with no scope/diff orientation. Fixed the script to parse string `args`;
  this pass confirms all 11 lenses ran with full context. Cost of the mistake: one wasted 31-agent
  run (~1.2M tokens) + a 0-agent diagnostic.
- **Efficiency machinery worked.** 51 agents / 2.03M tokens: 11 lenses + 1 dedup + **39 skeptics
  (17 guard + 22 trace)** — convergence-weighting ran 39 instead of a flat 50, no session-limit stall
  (v1 hit one at 110 agents / 3.5M). The `hinted` flag correctly caught 5 findings whose topic my
  scope planted (M9, L7, L10, L11, C3) and withheld the convergence discount.
- **`codePack` (#4) was skipped** — authoring a ~25k-token pack into `args` isn't practical, and a
  path-based pack forces every agent to read everything. #4 needs a redesign; #1–#3 carried the win.
- **Re-raises of accepted deferrals** (attached, not suppressed — per the ledger caveat, prior
  re-raises were often right): **M9/L10** re-raise v1 **DB-1** (migration DDL untested → deferred to
  the 3-env phase); **L11** re-raises v1 **CLOUD-1** (seekable-stream/cloud provider → deferred to
  bolt-043); **M3** re-raises the v3 deploy-time note on decode concurrency. All three deferrals
  still stand; M3 is worth promoting from "note" to an explicit gate (below).

---

## A. Cache-fill concurrency & lifecycle (the fix-generated cluster)

Making the thumbnail key deterministic (BUG-3) and writing it lazily on `GET /preview` (removing
`AsNoTracking` only for the miss branch) is sound in the single-request happy path but has several
rough edges under concurrency and against the cleanup job.

### 🟠 M1 — Lazy preview-write races the cleanup job → permanently orphaned thumbnail
`Services/UploadService.cs:159` · confirmed · conv 3 (correctness, race, completeness-critic)
A preview reads an upload as live (`DeletedAt` null), generates and writes `thumbs/o/u.jpg`; the
cleanup job (candidates loaded earlier) deletes the original, skips the still-null `ThumbnailPath`,
and sets `DeletedAt`. The preview's `UPDATE` has **no `DeletedAt` guard**, so it writes
`ThumbnailPath` onto the now-dead row — which cleanup never revisits. The thumbnail leaks forever.
**Fix:** `UPDATE … WHERE Id=@id AND DeletedAt IS NULL` (delete the just-written thumb on 0 rows), or
have cleanup always `DeleteAsync` the deterministic key.

### 🟠 M2 — Concurrent first preview collides on exclusive `File.Create` → 500
`Services/LocalStorageService.cs:32` · confirmed · conv 2 (correctness, race)
Two simultaneous first previews of the same upload (double-click, gallery re-render, prefetch) both
miss and `SaveAsync` the **same** deterministic key. `File.Create` uses `FileShare.None`, so the
second throws `IOException` → unmapped → 500. The old random-GUID keys never collided; the BUG-3 fix
created this. **Fix:** write to a temp file then atomic move/overwrite; optionally map storage
`IOException` → 503.

### 🟠 M6 — Preview cache-miss with a missing original → 500 instead of 4xx
`Services/ImageProcessor.cs:63` · confirmed · conv 1 (completeness-critic)
If the original blob is gone (ops delete, or the M1 race) but the row survives, the miss path calls
`GetStreamAsync` **outside** the `ImageFormatException` try → `FileNotFoundException` → unmapped →
500. BUG-4's fix only covered *corrupt* images, not *missing* ones. **Fix:** catch
`FileNotFoundException` on the miss path (or map it) → 404/422.

> Related lows in this cluster: **L1** cache-hit TOCTOU 500 + redundant round-trip (`UploadService.cs:150`,
> conv 4); **L5** GET now does a DB write (read-replica hazard); **L3**/**L4** the cache-vanish
> regenerate and the orphan-on-failed-commit emit no distinct signal.

---

## B. Decode DoS — the per-image caps don't bound aggregate memory

### 🟠 M3 — No concurrency/aggregate memory bound on decode → OOM DoS
`Services/UploadService.cs:158` · confirmed · conv 2 (security, input-validation)
The caps are all *per-image*: `ExceedsDecodeLimits` (100 MP) and ImageSharp `AllocationLimitMegabytes`
(512 MB, per single allocation). A guest can stage ~100 compressible 100-MP PNGs (a few KB each, all
pass the caps) then fire ~100 concurrent first previews; each decodes ~400 MB → ~40 GB in flight →
OOM. The rate limiter counts *requests*, not cost, and is per-IP. This re-raises the v3 deploy-time
note on decode concurrency. **Fix:** gate `GenerateThumbnailAsync` behind a bounded `SemaphoreSlim`
(or a tight concurrency policy) so total in-flight decode memory is capped regardless of request rate.
**Recommend promoting this from a deploy-time note to an explicit gate before merge** — it's the one
finding with process-kill impact.

> Related low: **L13** the 512 MB backstop + `InvalidMemoryOperationException` map to a raw 500 and
> are untested (a 16-bit ~100 MP PNG decodes to Rgba64 ~800 MB > the cap).

---

## C. Observability residuals

### 🟠 M4 — Batch bomb uploads never emit the reserved alert event
`Controllers/UploadsController.cs:119` · confirmed · conv 3 (requirements, observability, completeness-critic)
`DecompressionBombException` subclasses `UnprocessableEntityException`, so the batch catch logs it as
the generic `uploads.batch.item_rejected` and it never reaches the middleware that emits
`uploads.decompression_bomb.rejected` (with dimensions). Ops alerts keyed on that event miss bombs
sent through `/batch` — the code's own "most likely bomb vector." **Fix:** in the batch catch, when
`ex is DecompressionBombException`, also emit the reserved event with `WidthPx/HeightPx`.

### 🟠 M7 — Unreadable stored image at preview time is logged without path or cause
`Services/ImageProcessor.cs:88` · confirmed · conv 1 (observability)
The preview-path `catch(ImageFormatException)` rethrows a bare 422 with no log — dropping
`storagePath` and the inner exception — so ops can't tell *which* stored file corrupted, and it's
indistinguishable from a user's bad upload. `GetInfoAsync` (upload path) logs both; the preview path
should mirror it. **Fix:** log a warning with `storagePath` + inner exception before rethrowing.

> Related lows: **L6** batch-rejection warning logs the raw client filename unbounded (control-char /
> newline log forging); **L12** the bomb log *test* asserts the event name but not the dimensions the
> event exists to carry.

---

## D. Format & requirements

### 🟠 M5 — MimeValidator accepts HEIC but nothing can decode it
`Services/MimeValidator.cs:52` · confirmed · conv 1, confidence 9 (input-validation)
The v1 INPUT-1 fix correctly rejects non-HEIF ISO-BMFF, but **HEIC itself is still accepted** and
ImageSharp 3.1.11 has no HEIF decoder — so every iPhone `.heic` (the default camera format) is
buffered, written, then fails `IdentifyAsync` → 422 "could not be read as an image." 100% of HEIC
uploads fail confusingly, yet the UI advertises "JPEG, PNG, HEIC accepted." **Fix:** add a HEIF
decoder (libheif / Magick.NET / an ImageSharp HEIF plugin), or drop HEIC from the validator and the
message until decode is supported.

---

## E. Frontend / guest-auth

### 🟠 M8 — After guest-session expiry, a restored preview kept on 403 becomes an un-cartable orphan
`format-selector-page.ts:400` · confirmed · conv 1 (frontend-ux)
Long-lived tab, token expires, refresh: preview 401 → interceptor clears token → re-init mints a
**new** session → retry preview 403 (new session doesn't own the old upload). `fetchPreviewWithRetry`
drops only on 404, so the entry is **kept preview-less**; the user carts it → checkout 403.
**Fix:** treat 403 (and a persistent post-retry 401) like 404 — drop the entry. (Also the FE-4 spec's
"retry succeeds" mock is impossible in reality — L-frontend.)

> Related lows: **L7** the self-heal broadened to swallow *every* unauthenticated 401 app-wide
> (silent dead-end where there used to be a login redirect); **L8/L9** the one-shot-retry guard and
> the `shareReplay` re-init-after-settle are untested.

---

## F. Tests & verification — "green ≠ proven"

`dotnet build` clean; **515/515 .NET**, **403/403 frontend**. Gaps that would ship a regression green:

| ID | Gap | File |
|----|-----|------|
| 🟠 M9 | Provider-aware migration DDL + `ThumbnailPath` column (incl. the Npgsql arm) run in **no** test (all InMemory / EnsureCreated) — re-raises DB-1 | `Migrations/…AddUploadThumbnailPath.cs:34` |
| 🟠 M10 | Upload-time bomb rejection deletes the stored file, but no test verifies the `DeleteAsync` (delete it → file leaks, suite green) | `UploadServiceTests.cs:381` |
| 🟠 M11 | `MaxFrames=1` (the APNG frame-bomb control) has **zero** coverage — remove it and every test stays green | `ImageProcessor.cs:68` |
| 🟡 L12 | Bomb log test asserts the event name but not the width/height it carries | `ExceptionHandlerMiddlewareTests.cs:254` |
| 🟡 L13 | 512 MB allocator backstop → `InvalidMemoryOperationException` → raw 500, untested | `Program.cs:95` |
| 🟡 L14 | Truncated-but-recognized image (`InvalidImageContentException`) 422 path untested | `ImageProcessor.cs:81` |

---

## G. Low & cleanup (fast-follows)

**Low (beyond those noted above):** L10 model-snapshot phantom `AlterColumn` (SQLite `TEXT` vs Npgsql
`varchar(512)` — same DB-1 theme, *plausible*); L11 ETag `stream.Length` assumes a seekable stream —
breaks the planned cloud provider (*plausible*, re-raises CLOUD-1).

**Cleanup (⚪):** C1 preview object-URLs never revoked (blob memory leak); C2 upload-error string
duplicated ×3; C3 self-heal seam only tested with each half mocked; **C4 the implementation
walkthrough still describes the *old insecure* `Cache-Control: public…immutable`** (SEC-1
reintroduction risk if copied — worth fixing despite being a doc); C5 story AC says "54 MP" vs the
shipped 100 MP; C6 story AC says `varchar(500)`/`StoragePath` vs shipped `varchar(512)`/`FilePath`;
C7 thumbnail ships at 300px while stories/unit-brief specify 800px.

---

## H. Cleared / refuted (recorded)

- **L-refuted — "MIME-acceptance change shipped untraced"** — **refuted.** The HEIF-brand tightening
  *is* traced to commit `f850f69` (INPUT-1, review 042-v1) with tests; the removed MP4/MOV acceptance
  was a bug, not intended behavior. The only residual is that `bolt.md`'s bundled-scope list omits it
  (a doc nit, folded into C-docs), not an untraced code change.
- The cloud-provider seekability items (L11, and the ETag `stream.Length`) are **plausible but not
  triggerable today** — no non-`LocalStorageService` implementation exists; deferred to bolt-043.

---

## I. Recommendation

**Approve with follow-ups.** No blockers; the feature's core promises hold and the v1 fixes survived
a blinded re-audit. Before merge, address the runtime-impacting mediums:

1. **M3** — add the decode concurrency gate (the only process-kill vector; already on the deploy radar).
2. **M1 + M2 + M6** — make the deterministic-key write safe under concurrency and against cleanup
   (temp-file+atomic-move, `DeletedAt`-guarded update, catch `FileNotFoundException`).
3. **M4** — emit the bomb event on the batch path.
4. **M5** — decide HEIC: decoder or stop advertising it.

M7–M11 and everything in §G are fast-follows. **C4** (the walkthrough's stale insecure cache
directive) should be corrected regardless, since copying it reintroduces SEC-1.

> **Feature-closure:** this pass found new (if minor) issues, so it is **not saturated** — after the
> follow-ups land and a verification pass confirms them, closure still wants one more discovery pass
> that comes back quiet. (Also: HEAD is 22 commits ahead of `origin` — the branch is unpushed.)
