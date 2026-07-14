---
type: code-review
target: 042-thumbnail-cache
version: 5
supersedes: 4
branch: feat/bolt-042-thumbnail-cache
commit: 6c4f334
base: main
reviewed: 2026-07-14
reviewer: Claude (multi-lens parallel review system)
pass-type: verification
verdict: approve-with-followups
blockers: []
---

# Review — Bolt 042: Thumbnail Cache — v5 (verification of the resolution-v4 fixes)

A **verification** re-review (anchored, per-fix — the opposite posture of v4's discovery). It does
**not** re-audit the feature; it checks whether each fix recorded in
[resolution-v4.md](resolution-v4.md) actually holds at `fixed_commit` `6c4f334`. Five independent
agents each took one cluster of fixes, read the fix commit + its regression test cold, and answered
three questions per finding: **does the test go red if the fix is reverted (non-vacuous)? is the fix
correct for the finding? does the delta introduce a regression?** A sixth job adjudicated the
non-`fixed` dispositions and skimmed the whole production diff for fix-introduced regressions.

## TL;DR

**The fixes hold.** Every `fixed` finding's regression test is **non-vacuous** (revert → red, confirmed
by an independent reader) and correct, and the cross-diff skim found **no fix-introduced regression**.
Suites green at `6c4f334`: **.NET 531/531, frontend 409/409**.

- **24 findings VERIFIED** (clean): M2, M3, M4, M5, M6, M7, M8, M9, M10, M11, L1, L3, L4, L6, L8, L9,
  L12, L13, L14, C1, C2, C3, C5, C6.
- **M1 — VERIFIED with a documented residual.** The fix catches the finding's stated ordering, but a
  narrower non-transactional window remains (V5-1 below), backstopped by the already-deferred **D31**
  orphan sweep (bolt-043). Not reopened.
- **C4 — PARTIALLY resolved.** The three *named* contradictions are fixed, but the same walkthrough
  retains a **material** residual contradiction plus two minor ones (V5-2). The fixer completed these
  post-verification (see resolution-v5).
- **C7 — VERIFIED**, with one stale doc comment left behind (V5-3).
- **Deferrals L5, L10, L11 and the L7 dispute — all upheld** as sound on independent review.
- L2 remains **false-positive** (unchanged).

**Disposition: approve-with-followups** — a verification pass cannot emit `approved` (only a saturated
*discovery* pass can), and three small follow-ups (V5-1/2/3) were surfaced. None is a blocker; two are
doc-only and were closed in resolution-v5, the third (M1 residual) is an accepted, D31-backstopped
limitation.

---

## Verification method (read this)

- **Anchored, not blinded.** Each agent was given the finding (findings-v4), the fixer's note
  (resolution-v4), and the fix commit SHA — and told to read only that delta + directly-relevant code,
  not the whole feature. This is correct for verification (you *want* to re-check the exact claim).
- **Non-vacuity is the bar.** For every `fixed` finding an independent agent identified the exact test
  assertion and the exact production line it pins, and confirmed a revert would red it. The fixer had
  already produced revert→red / mutation→red evidence per finding during the fix pass; this pass
  re-confirms it independently. Two tests were noted as *partially* self-checking but pinned by other
  assertions in the same test (L4's `ThrowAsync` clause alone is vacuous, but its `DeleteAsync` +
  signal assertions pin it; M4/L12 dimension coverage — see notes).
- **5 cluster agents + 1 judgment/skim agent, ~530k subagent tokens.** No workflow script, no manifest,
  no codePack (verification runbook, per [../README.md](../README.md)).

---

## Per-finding verification

### Mediums

| ID | Verdict | Evidence |
|----|---------|----------|
| M1 | ✅ verified *(residual V5-1)* | `RowSoftDeletedDuringWrite` pins the liveness-re-read delete (revert → `DeleteAsync` Times.Never → red); InMemory faithfully preserves the concurrently-set `DeletedAt`. Residual window remains (below). |
| M2 | ✅ verified | `ConcurrentWritersSameKey` deterministically reds on revert (GatedStream holds writer-1's handle → old `File.Create`/`FileShare.None` → writer-2 `IOException`). Temp-file + `File.Move(overwrite)` is a same-volume atomic replace; both call sites unaffected. |
| M3 | ✅ verified | Gate precedes read+decode; process-wide singleton (Program.cs); `_DecodeSlotUnavailable_` test (exhausted 1-slot + canceled token, Strict mock) reds on revert (GetStreamAsync would be hit). Ctor change: no other construction site. |
| M4 | ✅ verified | Batch catch emits `uploads.decompression_bomb.rejected` w/ dimensions, template identical to the middleware, gated on the bomb type; test reds on revert. |
| M5 | ✅ verified | `MimeValidator` drops all ftyp/HEIF → null → 415; INPUT-1 still holds (stricter); validator + photo-upload specs flipped accept→reject. Removed `using System.Text` + 8-byte buffer are safe. |
| M6 | ✅ verified | `FileNotFoundException` (System.IO) is unrelated to the custom `NotFoundException`; reverting the catch lets it escape → red. Catch is narrow; definite-assignment holds. |
| M7 | ✅ verified | Catch logs `{StoragePath}` + inner ex and rethrows via new 2-arg ctor (single-arg intact); test pins the warning; inner cause never leaks into the 422 body. |
| M8 | ✅ verified | Drops on `404 || 403 || (isRetry && 401)`; keeps 5xx/network (NEW-2 intact). 403-after-retry + persistent-401 tests both red on revert; FE-1/FE-2/FE-4/NEW-2 still hold. |
| M9 | ✅ verified | Real `Migrate()` on SQLite `:memory:` then `pragma_table_info` asserts the column; a broken `Up()` reds it (mutation-confirmed). SQLite arm only; Npgsql type/length deferred (DB-1). |
| M10 | ✅ verified | Flow reaches the bomb-path delete (900 MP > cap); added `DeleteAsync` Times.Once reds if the line-90 delete is removed. |
| M11 | ✅ verified | Decode extracted to `internal LoadSingleFrameAsync` (MaxFrames=1); 3-frame GIF via reflection asserts 1 frame; dropping MaxFrames → 3 → red. Dropping DecoderOptions from `Identify` is behaviour-preserving (header-only). |

### Lows (fixed)

| ID | Verdict | Evidence |
|----|---------|----------|
| L1 | ✅ verified | Direct-read + `catch(FNF)` → regenerate; `CachedFileVanished_RegeneratesInsteadOf500` reds on revert to Exists-then-Get. Redundant round-trip dropped (QUAL-2). |
| L3 | ✅ verified | Signal emitted inside the L1 catch; `EmitsMissingFileSignal` reds if the log line is removed. |
| L4 | ✅ verified | Broad `catch` rethrows via `throw;` (type/stack preserved); best-effort delete + signal both pinned; M1 re-read correctly skipped on rethrow (no double-delete). |
| L6 | ✅ verified | `SanitizeFileNameForLog` caps to 128 then strips control chars; test (newline + 200×`z`) reds on revert to raw filename. |
| L8 | ✅ verified* | Asserts exactly 2 attempts + terminal error. *Guard regression is a synchronous stack-overflow crash (test fails, not silently passes) — a valid guard, per the finding.* |
| L9 | ✅ verified | `of()` completes → `finalize` resets `guestInit$` → 2nd init fires; neutralizing `finalize` → 1 init → red. Correctly distinct from FE-1 (non-completing Subject). |
| L12 | ✅ verified | Distinct 31000×32000 asserts both dimensions render; dropping either → red (mutation-confirmed). |
| L13 | ✅ verified | Exact-type map entry for `InvalidMemoryOperationException` → 422; reverting → 500 → red. Pins the map (correct unit scope). |
| L14 | ✅ verified | Corrupt-IDAT PNG (signature+IHDR intact) → `InvalidImageContentException`; narrowing the catch to `UnknownImageFormatException` → escapes → red (targets the untested branch). |

### Cleanups (fixed)

| ID | Verdict | Evidence |
|----|---------|----------|
| C1 | ✅ verified | Revoke at all four leak paths (remove/drop/add-to-cart-clear/destroy); URL only created on a 200 so error paths orphan nothing; remove→revoke test pins it. |
| C2 | ✅ verified | Single byte-identical `UPLOAD_ERROR` at all three sites; no message changed. |
| C3 | ✅ verified | Real `AuthService` (no clear-spy): a real 401 runs real `clearGuestToken` against real `getGuestToken` over the same `guestSession` key — a key/shape divergence would leave the token set → red. Genuinely closes the seam TEST-1 could not. |
| C5 | ✅ verified | Story 003 now "110 MP / 100 MP cap" + 800px — matches shipped. |
| C6 | ✅ verified | Story 001 now `varchar(512)` / `FilePath` — matches shipped. |
| C7 | ✅ verified *(residual V5-3)* | `ThumbnailMaxDimension` 300→800; test asserts a 2000×1500 source →`>300 && ≤800` (the `>300` clause distinguishes new from old) → reverting to 300 reds it. Stale doc comment left (below). |

### Deferrals / dispute — upheld

| ID | Verdict | Evidence |
|----|---------|----------|
| L5 | ✅ deferral sound | The persist genuinely makes GET /preview write; no read replica exists (SQLite dev / single Postgres prod), so it can't fire. Note is accurate; matches the finding's "at minimum document" bar. |
| L7 | ✅ dispute sound | `error.interceptor.ts` confirmed **unchanged** this pass (`git log aa6639c^..HEAD -- error.interceptor.ts` empty). FE-3 test exists and passes ("does not navigate an anonymous user … to login on 401"). L7's headline remedy would add navigation on an anon 401 → fail FE-3 → so declining to implement it is correct. Its *alternative* (scope `clearGuestToken` to upload/preview) is optional, harmless, and correctly surfaced for the owner rather than done. |
| L10 | ✅ deferral sound | Snapshot is uniformly SQLite-flavoured; the phantom `AlterColumn` is documented in the migration comment; per-provider-assembly fix deferred to 3-env (consistent with DB-1). Finding's own verdict was "plausible, accept as documented deferral". |
| L11 | ✅ deferral sound | `stream.Length` is unconditional but only the seekable `LocalStorageService` is registered; the cloud provider is bolt-043. Not triggerable today (CLOUD-1). |

---

## Follow-ups surfaced by this pass

### V5-1 — M1 residual: a narrower write-vs-cleanup race still exists (accepted, D31-backstopped)
`Services/UploadService.cs` + `BackgroundJobs/UploadCleanupJob.cs`
The M1 liveness re-read closes the finding's stated ordering (cleanup commits soft-delete, *then*
preview writes). A symmetric interleaving survives: preview reads live → generates → persists
`ThumbnailPath` → its liveness re-read runs **before** the cleanup job commits its `DeletedAt`
(`stillLive=true`, no delete) → cleanup then soft-deletes but skips the thumbnail because it keys on
its own stale `upload.ThumbnailPath` snapshot (null at load time). This is the fundamental
non-transactional file-vs-DB TOCTOU; it is the **same class as NEW-3/D31**, whose orphan sweep
(deferred to bolt-043) is the designed backstop. Cleanup-side unconditional deletion of the
deterministic key narrows it further but does not fully close it either (a write strictly after
cleanup's delete + before its commit still leaks). **Disposition: accept — M1's fix is a sound
mitigation; the residual is covered by the deferred D31 sweep.** Recorded so the link isn't lost.

### V5-2 — C4 residual: the walkthrough refresh was incomplete
`memory-bank/bolts/042-thumbnail-cache/implementation-walkthrough.md`
The three *named* C4 items were fixed, but the same doc still contradicts shipped code:
- **Material:** line 44 ("Deviations from Plan") still asserts the migration is *"SQLite-typed (TEXT),
  not Npgsql varchar… functionally correct"* — contradicting the shipped provider-aware migration
  (`character varying(512)` on Npgsql) **and** line 27 of the same doc. This is the same
  copy-a-wrong-claim risk C4 exists to remove.
- line 21 still names the cap `ImageProcessor.MaxDecodeDimension` (shipped: `MaxDecodePixels`, area
  cap) — line 30 was fixed, line 21 missed.
- minor: line 56 retains the stale ">25000 dims" per-axis language.
*(The sibling `implementation-plan.md` carries the same 25000/MaxDecodeDimension text, but it's a
pre-implementation doc outside C4's scope.)* **Fixed in resolution-v5.**

### V5-3 — C7 residual: stale doc comment
`Services/IImageProcessor.cs:14` — the XML doc still reads "max 300 px on longest dimension", now
contradicting the 800px constant. Doc-only. **Fixed in resolution-v5.**

---

## Minor, non-actionable notes (recorded, no change needed)
- **M4 fixture** uses equal 30000×30000 dims, so it catches "event dropped" or "both dims dropped" but
  not "one of two"; the *distinct-value* coverage lives in L12 on the middleware path. The
  resolution's "also covers L12 for the batch path" is therefore slightly overstated — harmless.
- **FE-4 "retry-succeeds" mock** (401→200) that v4-L-frontend flagged as unrealistic was left
  unchanged — a test-realism nit, no correctness impact.
- **M2 `File.Move(overwrite)`** can throw on Windows *dev* if a concurrent reader holds the destination
  without delete-share; on the Linux/Postgres prod target POSIX rename replaces atomically. Pre-existing
  in nature, not introduced as a regression.

## Recommendation
**Approve with follow-ups.** All 24 clean fixes are verified non-vacuous and correct; M1 holds with a
D31-backstopped residual; the deferrals and the L7 dispute are sound. V5-2/V5-3 (doc-only) are closed in
[resolution-v5.md](resolution-v5.md); V5-1 is an accepted limitation. **Feature-closure still wants a
*saturated discovery* pass** (a later blinded pass that comes back quiet) — this verification round, however
green, does not certify saturation. (HEAD remains unpushed / ahead of `origin`.)
