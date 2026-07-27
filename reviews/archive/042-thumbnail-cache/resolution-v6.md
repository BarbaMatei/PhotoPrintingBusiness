---
type: resolution
target: 042-thumbnail-cache
answers_review: review-v6.md
version: 6
branch: feat/bolt-042-thumbnail-cache
status: resolved
fixed_commit: 79c2eda
opened: 2026-07-14
closed: 2026-07-14
findings:
  F1:  { status: fixed, commit: 548663f, note: "Decode-limiter default now min(cores, availableRAM / 512 MB-per-decode) via ImageDecodeLimiter.RecommendedMaxConcurrentDecodes(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes, ProcessorCount); bounds summed in-flight decode memory to host RAM. 3 unit tests (low-RAM→memory-bound, ample-RAM→core-bound, tiny-RAM→never<1)." }
  F2:  { status: fixed, commit: 069f5ea, note: "clearGuestToken() now drops only the guestToken field and keeps checkout contact info under the same guestSession key; removes the whole entry only when nothing but the token remains. 2 auth.service.spec tests (preserve contact / remove-when-token-only)." }
  F3:  { status: fixed, commit: 39b0098, note: "Capture wasGuest = !isAuthenticated() BEFORE the request in performUpload + fetchPreviewWithRetry; self-heal (re-init guest + retry) runs only when wasGuest. Expired logged-in user no longer minted a throwaway guest. 2 format-selector.spec tests (upload + preview), revert-verified red." }
  F4:  { status: deferred, commit: null, note: "Same class as v5 V5-1 / ledger D31 — accepted, deferred to the bolt-043 orphan sweep. Durable fix is the atomic ExecuteUpdate the review names; not done now because the InMemory test provider can't run ExecuteUpdate (same reason M1 used a liveness re-read). See decisions." }
  F5:  { status: fixed, commit: 6b7ce09, note: "Middleware now emits uploads.decompression_bomb.rejected (source=allocator_backstop) for InvalidMemoryOperationException, alongside the pixel-guard branch (tagged source=pixel_guard); ops alerting on the reserved event catch backstop-tripped bombs too. Test asserts the event + source." }
  F6:  { status: fixed, commit: 6e577fd, note: "Added HEIC removal as bundled-scope Change D in bolt.md with retroactive AC (415 on ISO-BMFF ftyp content; UI drops .heic), mirroring B/C. Doc-only." }
  F7:  { status: fixed, commit: 79c2eda, note: "test-walkthrough now states shipped `private, max-age=2592000` (was the opposite `public…immutable`, contradicting SEC-1/D1) + Private=true assertion; reconciled the point-in-time 460/+3 count with the v1–v6→v6-resolution test growth (531/409 at v6, 535/413 after). Doc-only." }
  F8:  { status: deferred, commit: null, note: "D28 re-raise — non-seekable cloud stream at UploadsController.cs:155; latent until the bolt-043 cloud IStorageService lands. No non-seekable stream exists today. Deferral stands (bolt-043)." }
  F9:  { status: deferred, commit: null, note: "D66 — ExistsAsync has no production caller; it's a bolt-043 cloud seam. Documenting/dropping it belongs with the 043 provider work. Deferred to bolt-043." }
  F10: { status: deferred, commit: null, note: "D71 — cleanup-job thumbnail-delete-failure still soft-deletes the row; same orphan-sweep family as F4/D31. Deferred to bolt-043." }
  F11: { status: deferred, commit: null, note: "D79 — GetInfoAsync broad catch collapses storage/IO faults + cancellation into 422. New low; next discovery pass / observability follow-up. Deferred." }
  F12: { status: deferred, commit: null, note: "D77 — pixel-area cap is bytes-per-pixel-blind; legit 16-bit large PNGs 422. Input-validation refinement, no data loss. Deferred to next pass." }
  F13: { status: deferred, commit: null, note: "D75 — File.Move move-target race; Windows-dev-only, prod Linux rename is atomic. Deferred to next pass." }
  F14: { status: deferred, commit: null, note: "D76 — cache-hit GET read-share vs cleanup delete race; Windows-dev-only, prod Linux unlinks. Deferred to next pass." }
  F15: { status: deferred, commit: null, note: "D68 — limiter saturation/queue unobservable. Observability follow-up; deferred to next pass." }
  F16: { status: deferred, commit: null, note: "D72 — staggered parallel-preview 401s can churn sessions; grid outcome unchanged, wasteful only. Deferred to next pass." }
  F17: { status: deferred, commit: null, note: "D67 — extra AnyAsync round-trip on cache-miss preview; removed only by F4's atomic ExecuteUpdate, which is deferred with F4. Deferred (paired with F4)." }
  F18: { status: fixed, commit: 39b0098, note: "The logged-in-401-during-upload coverage gap (D73) is closed by the F3 regression tests: format-selector.spec now asserts a logged-in 401 (upload + preview) mints no guest session and fires no retry. Byproduct of the F3 fix." }
  F19: { status: deferred, commit: null, note: "D74 — onFilesAccepted initial guest-init error path untested. Separate coverage gap, not in the v6 recommendation set. Deferred to next pass." }
  F20: { status: deferred, commit: null, note: "D80 — implementation-plan.md AC still lists public/immutable + 25000×25000 axis cap. Same drift family as F7 but a different file; not in the v6 recommendation set. Deferred to next pass (cheap doc fix)." }
  F21: { status: deferred, commit: null, note: "D69 — no test pins slot release on a throwing decode; plausible/latent (using var releases today). Deferred to next pass." }
  F22: { status: deferred, commit: null, note: "D70 — exact-type 422 mapping proven only by an injected instance; plausible/latent (3.1.11 throws the concrete type). Deferred to next pass." }
  F23: { status: deferred, commit: null, note: "D78 — null-Identify fail-open; dead today (3.1.11 throws, never returns null). Deferred to next pass." }
  F24: { status: deferred, commit: null, note: "D23 re-raise — Npgsql migration DDL arm unexercised (InMemory tests). Standing 3-env/Testcontainers deferral." }
  F25: { status: deferred, commit: null, note: "D23 re-raise — SQLite-typed snapshot vs Npgsql varchar(512); phantom AlterColumn only under a design-time provider switch the project never does. Standing 3-env deferral." }
  F26: { status: deferred, commit: null, note: "D81 — bomb-alert log template duplicated across controller + middleware. Note: F5 added a third emit site of the same event name, so the hoist-to-constant is now marginally more valuable. Cleanup, deferred to next pass." }
  F27: { status: deferred, commit: null, note: "D82 — dropRestoredEntry duplicates onRemoveUpload. Cleanup, deferred to next pass." }
  F28: { status: deferred, commit: null, note: "D83 — client_aborted branch reads Items[\"CorrelationId\"] directly instead of GetCorrelationId(). Trivial cleanup in the same file F5 touched; left deferred to keep the v6 scope disciplined. Deferred to next pass." }
  F29: { status: deferred, commit: null, note: "D84 — storage save/delete traces at Debug never emit under the Information floor. Cleanup, deferred to next pass." }
---

# Resolution — Bolt 042: Thumbnail Cache (answers review-v6)

Fixer-owned; one row per finding ID from [review-v6.md](review-v6.md). No blockers, verdict
`approve-with-followups`. IDs are pass-local to v6 and map to canonical `D#` in [ledger.md](ledger.md).

## Scope of this resolution

Driven by review-v6's **Recommendation** section: fix the runtime/data-loss mediums **F1, F2, F3, F5**
plus the two cheap doc fixes **F6, F7**; defer **F4** and **F8** to bolt-043. The remaining long-tail
(F9–F29 Lows/Cleanups) is left for the next blinded discovery pass the review asks for — recorded as
`deferred` here with rationale, not silently dropped. **F18** flipped to `fixed` because the F3
regression tests are exactly the coverage it asked for (byproduct, same commit).

Suites after the fixes: **.NET 535/535, frontend 413/413** (both were 531/409 at v6; +4 .NET, +4 FE
from the F1/F5/F2/F3 regression tests).

## Findings

| ID | Sev | Status | Commit | How |
|----|-----|--------|--------|-----|
| F1 | 🟠 | fixed | 548663f | Default decode slots = min(cores, availableRAM / 512 MB); +3 unit tests |
| F2 | 🟠 | fixed | 069f5ea | clearGuestToken drops only the token, preserves contact info; +2 tests |
| F3 | 🟠 | fixed | 39b0098 | Capture guest-ness before the request; self-heal only for real guests; +2 tests |
| F4 | 🟠 | deferred | — | bolt-043 orphan sweep (D31); atomic ExecuteUpdate blocked by InMemory provider |
| F5 | 🟠 | fixed | 6b7ce09 | Emit bomb event (source=allocator_backstop) for InvalidMemoryOperationException; +1 test |
| F6 | 🟠 | fixed | 6e577fd | HEIC removal documented as bundled-scope Change D + AC |
| F7 | 🟠 | fixed | 79c2eda | Walkthrough Cache-Control corrected to shipped `private`; test counts reconciled |
| F8 | 🟠 | deferred | — | bolt-043 cloud provider (D28); non-seekable stream latent until 043 |
| F9 | 🟡 | deferred | — | D66 — bolt-043 cloud seam (ExistsAsync) |
| F10 | 🟡 | deferred | — | D71 — orphan-sweep family, bolt-043 |
| F11 | 🟡 | deferred | — | D79 — GetInfoAsync fault/cancel conflation; next pass |
| F12 | 🟡 | deferred | — | D77 — bytes-per-pixel budget; next pass |
| F13 | 🟡 | deferred | — | D75 — Windows-only move race; next pass |
| F14 | 🟡 | deferred | — | D76 — Windows-only delete race; next pass |
| F15 | 🟡 | deferred | — | D68 — limiter observability; next pass |
| F16 | 🟡 | deferred | — | D72 — parallel-preview session churn; next pass |
| F17 | 🟡 | deferred | — | D67 — extra round-trip, removed by F4's fix (paired) |
| F18 | 🟡 | fixed | 39b0098 | Logged-in-401 coverage — closed by the F3 tests |
| F19 | 🟡 | deferred | — | D74 — guest-init error path untested; next pass |
| F20 | 🟡 | deferred | — | D80 — plan AC drift (different file than F7); next pass |
| F21 | 🟡 | deferred | — | D69 — slot-release test; next pass |
| F22 | 🟡 | deferred | — | D70 — exact-type mapping test; next pass |
| F23 | 🟡 | deferred | — | D78 — null-Identify fail-open (dead today); next pass |
| F24 | 🟡 | deferred | — | D23 — Npgsql DDL; 3-env/Testcontainers |
| F25 | 🟡 | deferred | — | D23 — snapshot parity; 3-env |
| F26 | ⚪ | deferred | — | D81 — bomb-log template dup (now 3 sites after F5) |
| F27 | ⚪ | deferred | — | D82 — dropRestoredEntry dup |
| F28 | ⚪ | deferred | — | D83 — client_aborted correlation-id accessor |
| F29 | ⚪ | deferred | — | D84 — Debug-level storage traces |

## Decisions / deferrals (attached, not suppressed)

- **F4 → deferred (D31 / bolt-043 orphan sweep).** The review's durable fix is a conditional atomic
  write — `UPDATE … SET ThumbnailPath WHERE Id=@id AND DeletedAt IS NULL` via `ExecuteUpdate`, deleting
  the just-written file on 0 rows. The InMemory provider the integration tests run on cannot execute
  `ExecuteUpdate` (this is exactly why the v4 M1 fix used a liveness re-read, not `ExecuteUpdate`), so
  landing the durable fix now would ship untestable in this suite. This is the same accepted-deferral
  class as v5 V5-1 and belongs with the bolt-043 orphan sweep, where a real provider is in play. **F17**
  (the extra `AnyAsync` round-trip the M1 re-read costs) is folded into the same deferral — it only
  disappears with F4's `ExecuteUpdate`.
- **F8 → deferred (D28 / bolt-043).** `stream.Length` at `UploadsController.cs:155` assumes a seekable
  stream. No non-seekable stream exists today (only `FileStream` + the in-memory fake), so it's latent
  until the bolt-043 cloud provider. Design constraint for 043.
- **F18 → fixed via the F3 commit.** F18 asked for a logged-in-401-during-upload test asserting no guest
  session is minted. The F3 regression tests add exactly that (upload + restored-preview paths). Recorded
  as fixed against the F3 commit rather than left open, since the coverage it names now exists.
- **F2 residual (surfaced for the re-reviewer).** The fix stops the *interceptor* from wiping contact
  info. It does not change `format-selector`'s `ensureGuestSession`, which on a re-init still calls
  `storeSession({...empty contact})` — so an upload-page re-init would overwrite preserved contact info.
  This is out of F2's scenario (contact info is entered at checkout via `guest-checkout-form`, not on the
  upload page) and pre-existing, but noting it: fully preserving contact across an upload-page re-init,
  and the "checkout surfaces a re-auth notice" UX the review also mentions, are follow-ups (not the
  localStorage-wipe F2 fixes). Cart re-association after token expiry is a server-side concern beyond the
  FE fix.
- **F5 ↔ F26 interaction.** F5 adds a third emit site of `uploads.decompression_bomb.rejected` (now:
  batch path in the controller, pixel-guard + allocator-backstop in the middleware). This makes F26's
  "hoist the event name/template to a shared constant" marginally more valuable; recorded so the next
  pass weights it accordingly. Not fixed here (cleanup, out of the v6 recommendation set).
- **F28 → deferred despite being in a file I touched.** F5 edited `ExceptionHandlerMiddleware.cs`, and
  F28 (client-abort branch reading `Items["CorrelationId"]` instead of `GetCorrelationId()`) is a
  one-line cleanup in the same file. Left deferred to keep the v6 change set to the recommended scope;
  trivially batchable in the next cleanup sweep.
- **F20 → deferred (D80).** Same doc-drift family as F7 but in `implementation-plan.md`, which the v6
  recommendation did not list. Cheap; folded into the next pass with the other doc reconciliations.
- **Long tail (F9–F17, F19, F21–F29) → deferred.** Per the review's own disposition, the feature is
  **not saturated** and wants another blinded discovery pass; these Lows/Cleanups (Windows-dev-only
  races, latent version-bump/subtype risks, observability polish, cleanups) are the long tail that pass
  will re-weigh. None is a data-loss or runtime-break at prod; deferring them is deliberate, not a miss.

## Hand back — next step is a re-review

The six recommended findings (F1, F2, F3, F5, F6, F7) are `fixed` with regression tests (F3
revert-verified red; F1/F2/F5 assert the new behavior; F6/F7 doc-only). F18 is covered by F3's tests.
F4/F8 and the long tail are `deferred` with rationale above. Resolution is **`resolved`** at
`fixed_commit: 79c2eda`.

Per the loop contract I do **not** self-verify. The next step is a **verification re-review** against
`79c2eda` — revert-and-rerun each `fixed` finding's regression test, judge the doc fixes and deferral
rationales — producing `review-v7.md`, which is what flips the held findings to `verified` (or reopens
them). Note the review also asks for a separate **blinded discovery pass** to test saturation; that is a
distinct activity from verifying these fixes.
