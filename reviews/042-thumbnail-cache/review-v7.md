---
type: code-review
target: 042-thumbnail-cache
version: 7
supersedes: 6
branch: feat/bolt-042-thumbnail-cache
commit: 79c2eda
base: main
reviewed: 2026-07-14
reviewer: Claude (multi-lens parallel review system)
pass-type: verification
verdict: approve-with-followups
blockers: []
verifies: resolution-v6.md
---

# Review — Bolt 042: Thumbnail Cache — v7 (verification of the resolution-v6 fixes)

A **verification** re-review (anchored, per-fix — the opposite posture of a discovery pass). It does
**not** re-audit the feature; it checks whether each fix recorded in
[resolution-v6.md](resolution-v6.md) actually holds at `fixed_commit` `79c2eda` (working tree identical
at branch tip `d096f11`, which only adds the resolution doc). Method: the main agent ran
**revert-and-rerun** on all four behavioral fixes (revert the source fix, keep the test, confirm the
test goes red, restore, confirm green), then three anchored read-only agents judged the doc fixes, the
deferral rationales, and scanned the fix diff for fix-introduced regressions.

## TL;DR

**All six recommended fixes hold; F18's reclassification is honest; nothing reopened.** The four
behavioral fixes (F1, F2, F3, F5) each have a **non-vacuous** regression test — proven by reverting the
fix and watching exactly that test go red, then green on restore. The two doc fixes (F6, F7) match the
shipped code. The deferrals (F4, F8, and the F9–F29 long tail) are all legitimate — every deferred code
condition is still verbatim present at HEAD, nothing was silently fixed or dropped. **No fix-introduced
regression** was found in the cross-diff skim.

Per *Two loops* in the [README](../README.md), a verification pass emits **at most**
`approve-with-followups`, never `approved` — "these fixes held" is not "the feature is clean." Closure of
the *feature* still requires a **saturated discovery pass** (the review-v6 recommendation asked for one
more blinded discovery pass; that is a separate activity from this verification).

- **Verified (7):** F1, F2, F3, F5, F6, F7, F18.
- **Deferrals reaffirmed (F4, F8 + long tail):** all legitimate.
- **Reopened:** none. **New findings:** none.

**Suites green at HEAD: .NET 535/535, frontend 413/413** (both were 531/409 at v6; +4/+4 from the new
regression tests).

---

## Verification detail

### Behavioral fixes — revert-and-rerun (the non-vacuity proof)

| ID | Mutation applied | Result | Verdict |
|----|------------------|--------|---------|
| **F1** | `RecommendedMaxConcurrentDecodes` body → `return processorCount` (pre-fix, ignore RAM) | `..._LowRamHighCore_BoundedByMemoryNotCores` and `..._TinyRam_NeverBelowOne` went **red**; restore → green. `_AmpleRam` stayed green (doesn't distinguish the bug — expected) | **verified** |
| **F2** | `clearGuestToken` → `localStorage.removeItem('guestSession')` (pre-fix wipe) | `clearGuestToken preserves checkout contact info…` went **red**; the C3 real-seam interceptor test stayed green (token-only seed); restore → green | **verified** |
| **F3** | removed the `wasGuest &&` guard from both 401 branches | both `does not mint a guest session for a logged-in user whose upload/preview 401s (F3)` went **red**; restore → green | **verified** |
| **F5** | removed the `InvalidMemoryOperationException` bomb-event branch | `..._ImageAllocationBackstopTripped_EmitsReservedBombEvent` went **red**; the L13/D52 422-mapping test stayed green (mapping is independent of the event); restore → green | **verified** |

Each mutation was applied to source only (tests untouched), attributed to exactly the expected test(s),
then reverted — working tree confirmed clean afterward (`git status` empty). This is the cheap mutation
test the README's *Testing the tests* section requires: a green suite that can't go red isn't proving
the fix.

### Doc fixes — judged against shipped code (anchored agent)

- **F6 — verified.** `bolt.md:76-82` documents "Change D — HEIC no longer accepted" + AC (`:89-91`),
  matching the real mechanism: `MimeValidator.cs` recognizes only JPEG/PNG and falls through to `null`
  for ISO-BMFF/`ftyp` content (→ 415) — the doc correctly does **not** claim a brand check the code
  lacks. UI surfaces (`photo-upload.component.ts` extensions/accept/hint; `home-page.ts:118` copy) are
  all HEIC-free as documented. Cited tests exist and assert accept→reject.
- **F7 — verified.** `test-walkthrough.md:33` now states shipped `Cache-Control: private, max-age=2592000`
  (was the opposite `public … immutable`); matches `UploadsController.cs:25-26`. The integration test
  `GetPreview_CacheControl_IsPrivateNotPublic` (`UploadControllerIntegrationTests.cs:185`) asserts
  `Private=true / Public=false / MaxAge=30d`. The 460→531→535 / 409→413 test-count note is internally
  coherent.

### F18 — reclassification honest (anchored agent)

F18 (D73, logged-in-401-during-upload coverage gap) was flipped from a Low finding to `fixed` on the
grounds that the F3 regression tests are exactly the coverage it named. Confirmed: both F3 tests assert
`initAnonymousSession` is **not called** and there is **no retry**, across the upload and preview paths —
which is the "no guest session minted" assertion F18 asked for. Marking it fixed against the F3 commit is
accurate. *(Minor: the tests model the logout by flipping the auth flag rather than driving the real
Router navigation; the substantive assertions match F18's intent.)*

### Deferrals — all legitimate (anchored agent)

- **F4 (D31 → bolt-043 orphan sweep).** `UploadService.cs:216-220` still holds the non-atomic `stillLive`
  re-read, **not** an `ExecuteUpdate`. The "InMemory provider can't run ExecuteUpdate" rationale is
  credible: **zero** `ExecuteUpdate`/`ExecuteDelete` occurrences anywhere in `src` (the durable fix was
  genuinely not slipped in), and the upload tests use `UseInMemoryDatabase` (non-relational, no
  `ExecuteUpdate`). Same accepted-deferral class as V5-1/D31. **F17** correctly folds into the same
  deferral (it only disappears with F4's atomic write). Impact is an orphaned-file leak, not data loss.
- **F8 (D28 → bolt-043).** `UploadsController.cs:155` still calls `stream.Length` with no `CanSeek` guard;
  only seekable impls exist today (`LocalStorageService` `FileStream` + in-memory fake). Latent, not live.
- **Long tail (F13, F14, F20, F26, F28 spot-checked).** Every named code condition is still verbatim
  present at HEAD — deferral, not silent fix. F13/F14 are Windows-dev-only (prod Linux `rename`/unlink are
  safe). **F26** (duplicated bomb-alert template) was *not* fixed by F5 — F5 added a third emit site, which
  the resolution correctly flags as raising F26's value. **F28** (client-abort branch reads
  `Items["CorrelationId"]` directly) sits in the file F5 edited but was correctly left untouched. **F20**
  (stale AC in `implementation-plan.md`) is a defensible scope call — review-v6's Recommendation scoped the
  doc fixes to F6/F7 only; it's a Low doc drift, batchable next pass.

### Fix-introduced-regression skim (the one look beyond the anchor)

No regressions found across the six changed source files.
- **F1** — new default is always `≤` the old `ProcessorCount` (it's a `Min` with cores), so it can only
  *reduce* concurrency, and only on genuinely low-RAM/high-core hosts; on normal dev/CI hosts `RAM/512MB`
  ≫ cores so the default is unchanged. Tiny-RAM edge clamps to 1 (no deadlock). `GC.GetGCMemoryInfo()
  .TotalAvailableMemoryBytes` is seeded at heap init (non-zero from process start) on .NET 8, so no
  startup throttle. 512 MB matches the allocator backstop.
- **F2** — malformed JSON removes the entry (consistent with `getGuestToken`); a `guestSession` entry
  without `guestToken` breaks no consumer (`guest.interceptor.ts` skips the header, `guest-checkout-form`
  takes the create-new branch and re-stores, guards read null as before); `hasContactInfo` with all-empty
  fields removes the entry (matches the token-only test).
- **F3** — `wasGuest` captured pre-request reflects state before the interceptor flips `isAuthenticated`;
  a real guest (`isAuthenticated()===false`) is **not** suppressed; retry recursion is additionally gated
  by `!isRetry`, so no stale-flag path.
- **F5** — the new branch and the `DecompressionBombException` branch are on mutually exclusive types (no
  double-emit); mapping/response untouched; the `source=pixel_guard` addition keeps the existing
  substring-match test (`31000`/`32000`) green.

---

## Pass notes (methodology / cost)

- **Anchored, cheap, per-fix** — no blinding, no manifest, no discovery workflow script (the opposite of
  a discovery pass). 4 revert-and-rerun mutations by the main agent + **3 anchored read-only agents**
  (doc-judgment, deferral-legitimacy, regression-skim).
- **Revert-and-rerun is the load-bearing evidence.** Each behavioral fix's regression test was proven
  non-vacuous by mutation; attribution was exact (only the expected tests went red).
- **Cost:** 3 subagents, ~144k subagent tokens, + the main agent's mutation runs. Far cheaper than the v6
  discovery pass (51 agents / 1.83M tokens), as intended for a verification loop.

## Why this is not `approved`

A verification pass certifies that *these fixes held* — it cannot certify feature saturation (README
*Two loops*). review-v6 found 24 new findings and judged the feature **not saturated** (new-finding count
not decaying, dominated by fix-generated residuals). This pass fixed the runtime/data-loss subset and
verified them; the long tail is deferred. Feature closure still wants **one more blinded discovery pass**
that comes back quiet (0 new mediums, only long-tail cleanups) before `approved` is warranted.

**Recommendation:** merge is unblocked (0 High, 0 blockers, all recommended fixes verified). Before
calling bolt 042 *closed*, run the outstanding discovery pass and pick up the deferred long tail
(F9–F29) + the bolt-043 items (F4/F8) in their target phases.

Cross-pass identity mapping: [ledger.md](ledger.md). Fixer's dispositions: [resolution-v6.md](resolution-v6.md).
