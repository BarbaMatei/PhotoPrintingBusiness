---
type: resolution
target: 042-thumbnail-cache
answers_review: review-v8.md
version: 8
branch: feat/bolt-042-thumbnail-cache
status: resolved
fixed_commit: bd0d5fd
opened: 2026-07-14
closed: 2026-07-14
findings:
  F1:  { status: deferred, commit: null, note: "D34/D31 cleanup/cache-fill orphan race — the accepted bolt-043 orphan-sweep class. Durable fix is the conditional atomic UPDATE … WHERE DeletedAt IS NULL (ExecuteUpdate) + cleanup deriving thumbs/{owner}/{id}.jpg; InMemory can't run ExecuteUpdate (why M1 used a re-read). F6's new log makes the race face visible meanwhile. See decisions." }
  F2:  { status: fixed, commit: ac0485b, note: "Added unique ThenBy(o.Id) to the two Skip/Take sites (AdminOrderService.GetOrdersAsync — the real Include(Items) split-query hazard; OrderService.GetOrdersAsync — projection, added for stable paging). Class sweep: no other Skip/Take exists; ProductService/CartService/AdminProductService Include collections but don't paginate. Regression test: tied-CreatedAt pagination complete+deterministic, red on revert. NEW SURFACE: none (ordering refinement). Parity: split-query missing-items symptom needs Postgres (InMemory can't split) — rides 3-env (D23)." }
  F3:  { status: fixed, commit: 62a33cd, note: "storeSession now MERGES — an empty incoming contact field keeps the existing non-empty value; fresh guestToken always wins. Class-level fix covering every re-init caller (the two guestSession-key writers are storeSession + clearGuestToken). Tests: preserve-on-empty / overwrite-on-nonempty + full clear→re-init sequence, red on revert. NEW SURFACE: empty incoming contact no longer clears — no caller intends that today (checkout form always sends non-empty)." }
  F4:  { status: fixed, commit: 521fa15, note: "Upload-time bomb test now asserts the derived DecompressionBombException + WidthPx/HeightPx (was base UnprocessableEntityException only), pinning the type the alert emitters gate on. Revert-verified red (regress throw to base → red). Class sweep: preview-time test (ImageProcessorTests) already pins the derived type; only the upload-time test had the gap." }
  F5:  { status: fixed, commit: 521fa15, note: "Emit distinct Warning uploads.original.missing_file at the lost-original catch, so a storage-integrity incident is distinguishable from a routine unknown-id 404. Log-assertion test, red on revert. NEW SURFACE: one additive Warning on an existing branch; no sizing/limits." }
  F6:  { status: fixed, commit: 521fa15, note: "Emit distinct Warning uploads.thumbnail.deleted_row_race around the soft-delete-race delete, matching sibling anomaly paths. Log-assertion test, red on revert. NEW SURFACE: one additive Warning. The stale ThumbnailPath left on the dead row is F1/D31's deferred durable fix, not this log — noted in code + decisions." }
  F7:  { status: fixed, commit: bd0d5fd, note: "Pin preview decode to Image.LoadAsync<Rgba32> (4 B/px) so a legit ≤100 MP 16-bit source can't decode to 8 B/px and trip the 512 MB backstop → permanent un-previewability. Bounds any ≤100 MP decode ≤400 MB; keeps the 100 MP cap + large-format use case. Adversarial approach-check (rule #3) caught two blockers in the review's one-liner (won't compile; return type must stay Task<Image>) — folded in. Tests: reflection bit-depth guard (16-bit→32 bpp, red on revert) + e2e deep-colour→JPEG. NEW SURFACE: common 8-bit path 3→4 B/px (~300→~400 MB at 100 MP, still <512 MB & <limiter/slot budget); grayscale now 3-ch JPEG. See decisions for a finding-writeup correction." }
  F8:  { status: deferred, commit: null, note: "D90 — preview Cache-Control private,max-age=2592000 is recoverable device-locally on a shared browser. Real privacy nit but a DESIGN call: private,no-cache defeats the 30-day cache the D1/SEC-1 fix deliberately added (revalidation cost/bandwidth vs shared-device privacy). Not in the merge-recommendation set; flagged for owner decision, not silently dropped. See decisions." }
  F9:  { status: deferred, commit: null, note: "D66 — ExistsAsync has no production caller; bolt-043 cloud seam. Documenting/dropping it belongs with the 043 provider work. Couples with F26." }
  F10: { status: deferred, commit: null, note: "D75 — File.Move over an open reader; Windows-dev-only (prod Linux rename-over-open-fd succeeds). Next pass." }
  F11: { status: deferred, commit: null, note: "D67 — extra AnyAsync round-trip on cache-miss preview; removed only by F1's conditional ExecuteUpdate. Paired with F1 → bolt-043." }
  F12: { status: deferred, commit: null, note: "D69 — slot-release-on-throw untested; plausible/latent (using var releases today). Next pass." }
  F13: { status: deferred, commit: null, note: "D93 — no end-to-end bomb/oversize→422 integration test (FakeImageProcessor pins 800×600). Coverage gap; next pass / integration-fake work." }
  F14: { status: deferred, commit: null, note: "D23 — Npgsql migration DDL arm + snapshot exercised by no test (InMemory ignores migrations; only SQLite arm runs). Standing 3-env / Testcontainers deferral. F2's parity gap rides the same phase." }
  F15: { status: deferred, commit: null, note: "D68 — decode-limiter saturation/queue-depth unobservable. Observability follow-up; next pass." }
  F16: { status: deferred, commit: null, note: "D42 — frame-cap (MaxFrames=1) tested only on the internal helper, not through GenerateThumbnailAsync; plausible (cap holds today). Next pass." }
  F17: { status: deferred, commit: null, note: "D50 — ensureGuestSession recovery-after-init-error untested (all 12 specs mock init as success). Coverage gap; next pass." }
  F18: { status: deferred, commit: null, note: "D31 hard-kill variant — SIGKILL/OOM between SaveAsync and commit orphans the thumbnail. Same bolt-043 orphan-sweep deferral as F1 (the sweep attempts the derivable key for every candidate). See decisions." }
  F19: { status: deferred, commit: null, note: "D94 — guest 401 off the upload page is a silent dead-end (self-heal is format-selector-only). FE UX follow-up; not in the merge-recommendation set. Next pass." }
  F20: { status: deferred, commit: null, note: "D95 — localUrl() mints untracked blob URLs per change-detection for in-session photos (C1/D54 residual). FE leak, tab-lifetime; next pass." }
  F21: { status: deferred, commit: null, note: "D92 — restore-preview subscription resolving after ngOnDestroy leaks one object URL (C1/D54 residual). FE leak; next pass." }
  F22: { status: deferred, commit: null, note: "D96 — decode memory budget ignores concurrent upload buffering (F1/D61 residual). Memory-bound-config only; fold upload-buffer memory into the budget — next pass / limiter follow-up." }
  F23: { status: fixed, commit: 76d0b6a, note: "bolt.md Change C rewritten: states the split-query default IS a production query-execution change (was 'no behavior change'), + retroactive AC (every Skip/Take + collection-Include query carries a unique ORDER BY tiebreaker) referencing the F2 test and the Postgres/3-env verification (D23). Doc-only." }
  F24: { status: deferred, commit: null, note: "D28 — cloud stream seekable/Length contract untested; latent until the bolt-043 non-seekable provider (only seekable FileStream exists today). Standing bolt-043 deferral." }
  F25: { status: deferred, commit: null, note: "D81 — uploads.decompression_bomb.rejected literal copy-pasted in 3 places (controller batch omits source=). Extract a helper + add source=batch. Cleanup; next pass." }
  F26: { status: deferred, commit: null, note: "D66 test-side — inert ExistsAsync⇒true Moq stubs on cache-hit tests would mask a reintroduced exists-then-get TOCTOU. Delete; couples with F9. Cleanup; next pass." }
  F27: { status: fixed, commit: 76d0b6a, note: "implementation-plan.md ThumbnailPath 'character varying(500)' → 512 (76d0b6a); the fix-diff micro-review then caught a SECOND stale occurrence in intents/019 requirements.md the finding didn't cite → also fixed (00b0d39). Repo-wide grep now confirms no other ThumbnailPath 500 remains. Doc-only (D59 class)." }
  F28: { status: deferred, commit: null, note: "D97 — conditional-GET If-None-Match only matches an exact strong tag; weak/list/* degrade to full 200. Parse per RFC + test. Cleanup (bandwidth-only); next pass." }
---

# Resolution — Bolt 042: Thumbnail Cache (answers review-v8)

Fixer-owned; one row per finding ID from [review-v8.md](review-v8.md). No blockers
(`blockers: []`), verdict `approve-with-followups`. IDs are pass-local to v8 and map to
canonical `D#` in [ledger.md](ledger.md).

This round deliberately **applies the new fixer-contract rules** (README §*Bounding
fix-generativity*) the v8 review asked for, because v8 showed the fix-generativity loop is still
live (F3 defeated a v7-verified fix). Per fix: **#1 class sweep**, **#2 new-mechanism bar**,
**#3 design-check for the resource-budget change (F7)**, **#4 fresh-eyes micro-review of the fix
diff** (see the closing section).

## Scope of this resolution

Driven by review-v8's **Recommendation**: fix the mediums **F2, F3, F4** + cheap observability
**F5, F6** + doc **F23**, and a bounded **F7**; defer **F1/F18** (orphan sweep → bolt-043),
**F14** (Npgsql DDL → 3-env), **F24** (cloud stream → bolt-043); leave the Low/Cleanup long tail
for the next blinded discovery pass the review asks for. **F27** (a cheap doc-token fix in the
same D59 stale-token class as F23) is batched with F23. Everything deferred is recorded here with
rationale, not silently dropped.

Suites after the fixes: **.NET 540/540, frontend 416/416** (were 535/413 at v8; +5 .NET from the
F2/F5/F6 + two F7 tests, F4 strengthened an existing test; +3 FE from the F3 tests).

## Findings

| ID | Sev | Status | Commit | How |
|----|-----|--------|--------|-----|
| F1 | 🟠 | deferred | — | bolt-043 orphan sweep (D31/D34); atomic ExecuteUpdate blocked by InMemory; F6 log covers the race meanwhile |
| F2 | 🟠 | fixed | ac0485b | ThenBy(o.Id) on both Skip/Take queries; tied-CreatedAt pagination test (red on revert) |
| F3 | 🟠 | fixed | 62a33cd | storeSession merges (empty incoming keeps existing); preserve/overwrite + clear→re-init tests |
| F4 | 🟠 | fixed | 521fa15 | Bomb test asserts derived DecompressionBombException + dims; revert-verified |
| F5 | 🟠 | fixed | 521fa15 | Emit uploads.original.missing_file on lost-original; log-assertion test |
| F6 | 🟠 | fixed | 521fa15 | Emit uploads.thumbnail.deleted_row_race on soft-delete race; log-assertion test |
| F7 | 🟠 | fixed | bd0d5fd | Pin decode to Rgba32 (4 B/px); adversarial-checked; bit-depth guard + e2e tests |
| F8 | 🟡 | deferred | — | D90 — Cache-Control device-local recoverable; a design call vs the deliberate 30-day cache (decisions) |
| F9 | 🟡 | deferred | — | D66 — ExistsAsync no caller; bolt-043 cloud seam (couples F26) |
| F10 | 🟡 | deferred | — | D75 — File.Move over open reader; Windows-dev-only |
| F11 | 🟡 | deferred | — | D67 — extra round-trip; removed by F1's ExecuteUpdate (paired) |
| F12 | 🟡 | deferred | — | D69 — slot-release-on-throw untested; plausible/latent |
| F13 | 🟡 | deferred | — | D93 — no e2e bomb→422 integration test (fake pins 800×600) |
| F14 | 🟡 | deferred | — | D23 — Npgsql DDL/snapshot untested; 3-env/Testcontainers |
| F15 | 🟡 | deferred | — | D68 — limiter saturation unobservable |
| F16 | 🟡 | deferred | — | D42 — frame-cap tested only on helper; plausible |
| F17 | 🟡 | deferred | — | D50 — ensureGuestSession recovery-after-error untested |
| F18 | 🟡 | deferred | — | D31 hard-kill variant; bolt-043 orphan sweep (with F1) |
| F19 | 🟡 | deferred | — | D94 — guest 401 off upload page silent dead-end; FE UX follow-up |
| F20 | 🟡 | deferred | — | D95 — localUrl() blob leak (C1 residual) |
| F21 | 🟡 | deferred | — | D92 — restore-preview-after-destroy blob leak (C1 residual) |
| F22 | 🟡 | deferred | — | D96 — decode budget ignores upload buffering (D61 residual) |
| F23 | 🟡 | fixed | 76d0b6a | Change C label corrected + retroactive AC referencing F2 test/D23 |
| F24 | 🟡 | deferred | — | D28 — cloud stream contract; bolt-043 |
| F25 | ⚪ | deferred | — | D81 — bomb event copy-pasted ×3; extract helper + source=batch |
| F26 | ⚪ | deferred | — | D66 test-side — inert ExistsAsync stubs; delete (couples F9) |
| F27 | ⚪ | fixed | 76d0b6a, 00b0d39 | implementation-plan.md + intents/019 varchar(500)→512 (D59 token, repo-wide) |
| F28 | ⚪ | deferred | — | D97 — conditional-GET weak/list/* ETag not matched |

**8 fixed · 20 deferred · 0 wont-fix · 0 disputed.** All 7 mediums except F1 (accepted
bolt-043 deferral) are fixed. No blockers.

## Decisions / deferrals (attached, not suppressed)

- **F1 + F18 + F11 → bolt-043 orphan sweep (D31/D34).** The durable fix is the conditional
  atomic write — `UPDATE … SET ThumbnailPath WHERE Id=@id AND DeletedAt IS NULL` via
  `ExecuteUpdate`, deleting the just-written file on 0 rows — plus cleanup deriving
  `thumbs/{owner}/{id}.jpg` for every candidate (which also kills the F18 hard-kill leak and the
  F11 extra round-trip). The InMemory integration provider **cannot run `ExecuteUpdate`** (exactly
  why the v4 M1 fix used a liveness re-read), so landing it now ships untestable in this suite. Same
  accepted-deferral class as v5 V5-1. **F6's new `deleted_row_race` log makes the race observable in
  the interim** — it does not resolve the underlying non-atomicity.
- **F7 finding-writeup correction (for the re-reviewer).** The adversarial approach-check confirmed,
  against the real ImageSharp 3.1.11 package, that the current PNG failure path wraps the allocation
  error as `InvalidImageContentException` (derives from `ImageFormatException`) → caught locally in
  `GenerateThumbnailAsync` → re-thrown as a plain `UnprocessableEntityException`. So the middleware
  emits **no** `decompression_bomb.rejected` event on this path — the review's "plus a false
  decompression_bomb alert" is inaccurate for a PNG. The **real** defect (permanent
  un-previewability, every retry re-trips) is exactly as described and is what the fix removes; the
  correction does not change the fix.
- **F7 new-surface (rule #2).** Forcing `Rgba32` raises the **common 8-bit** decode from Rgb24
  (3 B/px, ~300 MB at 100 MP) to 4 B/px (~400 MB) — still under the 512 MB allocator backstop and
  under the decode limiter's 512 MB-per-slot budget, so no sizing change is needed. The existing
  `Program.cs` note "if the pixel cap is raised materially, raise [the 512 MB backstop] in step"
  stays correct; the per-pixel multiplier is now a fixed 4. Grayscale sources now encode as a
  3-channel JPEG (marginally larger, visually identical). This is the surface the re-review's
  input-validation/observability lens should re-audit.
- **F2 parity limitation (green ≠ proven).** The InMemory regression test proves the
  deterministic-ordering + per-order-item contract and goes red without the tiebreaker, but InMemory
  **does not split queries**, so the actual split-query *missing-items* symptom can only reproduce on
  Postgres. Full verification rides with the 3-env / Postgres-CI phase — the same class as F14/D23.
  The bolt.md Change-C AC (F23) records this.
- **F8 → deferred as a design call (D90).** `private, max-age=2592000` is recoverable from the
  browser's per-profile cache on a shared device. The review's fix (`private, no-cache` / short
  max-age) is real, but it **partially defeats the 30-day cache the D1/SEC-1 fix deliberately
  added** — it trades shared-device privacy for revalidation cost/bandwidth on every preview. It is
  a Low, not in the merge-recommendation set, and changes a deliberate caching design, so it wants an
  owner decision rather than a reflexive patch. Flagged, not dropped.
- **Long tail (F9, F10, F12, F13, F15, F16, F17, F19, F20, F21, F22, F25, F26, F28) → deferred.**
  Per the review's own disposition, the feature is **not saturated** and wants another blinded
  discovery pass; these Lows/Cleanups (Windows-dev-only races, latent/plausible test gaps, FE blob
  leaks, observability polish, cleanups) are the long tail that pass will re-weigh. None is a prod
  data-loss or runtime-break. Several (F20/F21 blob leaks, F13 integration coverage, F17 init-error
  coverage) are cheap and good candidates for the next fix round if the discovery pass re-raises them.

## Fix-diff micro-review (rule #4) — before hand-back

Two fresh-eyes anchored Explore agents (independent context) reviewed the full fix diff
(`bcf1ecc..bd0d5fd`), split backend vs frontend/docs, each asked the three required questions
(class-or-instance / new-surface-at-the-bar / regression).

- **Backend cluster (F2, F4, F5, F6, F7): clean.** Confirmed the F2 class sweep is complete (only
  two `Skip/Take` sites exist; `UploadCleanupJob`/`EmailRetryJob`/`AdminStatsService` are non-hazards
  — no Skip + no split collection Include); F4 pins the derived type at both production throw sites
  and the remaining base assertions cover genuine base scenarios; F5/F6 make all four `GetPreviewAsync`
  anomaly branches signal, with non-vacuous log-assertion tests; F7's ~400 MB peak sits under **both**
  the 512 MB allocator backstop **and** the decode-limiter's 512 MB-per-slot budget, no caller depends
  on the source pixel type, and the `async Task<Image>` signature is safe for all (production +
  reflection) callers. No regressions.
- **Frontend/docs cluster (F3, F23, F27): clean but for one class-sweep miss.** Confirmed the two
  `guestSession` writers both preserve contact, the legitimate `guest-checkout-form` overwrite still
  works (validator-gated non-empty), and the Change-C AC cross-references are accurate. **Caught: F27
  had a second stale `varchar(500)` in `intents/019-.../requirements.md:66` the finding didn't cite —
  fixed (00b0d39), completing the repo-wide token sweep.** Minor note (not a defect): the F3
  "overwrites non-empty" test is a reverse-direction guard, not a merge-pin; the other two F3 tests are
  the merge-pins (red on revert).

**Spotted while fixing — NEW, out of the finding set, flagged for the re-reviewer (not fixed):**
`AdminStatsService.GetProductStatsAsync` (`AdminStatsService.cs:109-114`) does
`OrderByDescending(TotalQuantity).Take(10)` on an **in-memory** GroupBy; a tie at the #10/#11 boundary
picks non-deterministically. It's a top-N stats display (not pagination, not the split-query hazard),
so it's cosmetic — but it's the nearest cousin to F2 and a re-review may want to weigh a `ThenBy` there.
Left untouched to keep this round to the finding set (README fixer rule).

## Hand back — next step is a re-review

The eight recommended/bounded findings (F2, F3, F4, F5, F6, F7, F23, F27) are `fixed` with
revert-verified regression tests (or doc-only for F23/F27). F1/F18/F11 (orphan sweep), F14
(Npgsql DDL), F24 (cloud stream) and the Low/Cleanup long tail are `deferred` with rationale above.
Resolution is **`resolved`** at `fixed_commit: bd0d5fd`. Suites **.NET 540/540 · FE 416/416**.

Per the loop contract I do **not** self-verify. The next step is a **verification re-review**
against `bd0d5fd` — revert-and-rerun each `fixed` finding's regression test (I confirmed each goes
red on revert; the re-review re-confirms independently), judge the doc fixes + deferral rationales —
producing `review-v9.md`, which flips the held findings to `verified` (or reopens them). The review
also asks for a **separate blinded discovery pass** to test saturation: with the fix-generativity
rules now applied, a quiet discovery pass (0 new mediums, only long-tail cleanups) would make the
feature a candidate for `approved`.
