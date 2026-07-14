---
type: code-review
target: 042-thumbnail-cache
version: 9
supersedes: 8
branch: feat/bolt-042-thumbnail-cache
commit: bd0d5fd
base: main
reviewed: 2026-07-14
reviewer: Claude (multi-lens parallel review system)
pass-type: verification
verdict: approve-with-followups
blockers: []
verifies: resolution-v8.md
---

# Review — Bolt 042: Thumbnail Cache — v9 (verification of the resolution-v8 fixes)

A **verification** re-review (anchored, per-fix — the opposite posture of a discovery pass). It does
**not** re-audit the feature; it checks whether each fix recorded in
[resolution-v8.md](resolution-v8.md) actually holds at `fixed_commit` `bd0d5fd` (branch tip adds only
the resolution/index docs + the F27 sweep-completion doc, no reviewed source beyond the fixes). Method:
**revert-and-rerun** on all six behavioral fixes (revert the source fix, keep the test, confirm the test
goes red with exact attribution, restore, confirm green), then anchored read-only judgment on the two
doc fixes and all twenty deferrals.

> **Note on independence.** This pass was orchestrated by the same agent that wrote the fixes. The
> load-bearing evidence is therefore the **mechanical** revert-and-rerun (a test either goes red on
> revert or it doesn't — not a judgment call) and a **fresh-context** anchored agent for the deferral
> legitimacy. A fully independent re-run remains the ideal; nothing here rests on the fixer's say-so.

## TL;DR

**All eight recommended fixes hold; all twenty deferrals are legitimate; nothing reopened; no new
defects.** The six behavioral fixes (F2, F3, F4, F5, F6, F7) each have a **non-vacuous** regression test
— proven by reverting the fix and watching exactly that test (and only it) go red, then green on
restore. The two doc fixes (F23, F27) match the shipped code. Every deferred code condition is still
**verbatim present at HEAD** — nothing was silently fixed or dropped — and the bolt-043 / 3-env / design
rationales are sound.

Per *Two loops* in the [README](../README.md), a verification pass emits **at most**
`approve-with-followups`, never `approved` — "these fixes held" is not "the feature is clean." Closure of
the *feature* still requires a **saturated discovery pass** (review-v8 judged the feature not-saturated
and asked for one more blinded discovery pass *after* a fixer round under the new rules; this round is
that fixer round, so the discovery pass is the remaining step).

- **Verified (8):** F2, F3, F4, F5, F6, F7, F23, F27.
- **Deferrals reaffirmed (20):** F1, F8, F9, F10, F11, F12, F13, F14, F15, F16, F17, F18, F19, F20, F21,
  F22, F24, F25, F26, F28 — all legitimate.
- **Reopened:** none. **New findings:** none (one pre-existing cosmetic cousin of F2 carried forward — see below).

**Suites green at HEAD: .NET 540/540, frontend 416/416** (were 535/413 at v8; +5 .NET from the F2/F5/F6
+ two F7 tests, F4 strengthened an existing test; +3 FE from the F3 tests).

---

## Verification detail

### Behavioral fixes — revert-and-rerun (the non-vacuity proof)

All five backend mutations were applied at once and the affected test classes run; **exactly five tests
failed, each attributed to its finding, with zero collateral failures** (49 other tests stayed green).
F3 (frontend) was mutated separately.

| ID | Mutation applied (source only) | Result | Verdict |
|----|-------------------------------|--------|---------|
| **F2** | `GetOrdersAsync` order-by → drop `.ThenBy(o => o.Id)` | `AdminOrderServiceTests.GetOrdersAsync_TiedCreatedAt_PagesDeterministicallyKeepingItemsPerOrder` went **red** (deterministic-order assertion, line 187); restore → green | **verified** |
| **F3** | `storeSession` → blind `setItem(JSON.stringify(data))` (pre-fix) | both merge-pin specs went **red** (`preserves existing contact info when re-initing with an empty profile`; `keeps contact info across the clear-token -> re-init self-heal sequence`); the reverse-guard "overwrites … non-empty values" stayed green (expected); restore → green | **verified** |
| **F4** | bomb throw → plain `UnprocessableEntityException` (base) | `UploadServiceTests.UploadAsync_ImageDimensionsExceedLimit_DeletesStoredFileAndThrows` went **red** (derived-type + dims assertion); restore → green | **verified** |
| **F5** | removed the `uploads.original.missing_file` warning | `GetPreviewAsync_CacheMissWithMissingOriginal_EmitsMissingOriginalSignal` went **red**; restore → green | **verified** |
| **F6** | removed the `uploads.thumbnail.deleted_row_race` warning | `GetPreviewAsync_RowSoftDeletedDuringWrite_EmitsDeletedRowRaceSignal` went **red**; restore → green | **verified** |
| **F7** | `LoadSingleFrameAsync` → non-generic `Image.LoadAsync` (auto pixel type) | `ImageProcessorTests.LoadSingleFrameAsync_DeepColourSource_DecodesAs32BppNot64` went **red** (found 64 bpp — the exact defect); restore → green | **verified** |

Each mutation touched source only (tests untouched); restoration was via `git checkout` to the committed
fix, and `git status` confirmed a clean tree afterward. This is the cheap mutation test the README's
*Testing the tests* section requires: a green suite that can't go red isn't proving the fix.

### Doc fixes — judged against shipped code (anchored agent)

- **F23 — verified.** `bolt.md` Change C now states the global split-query default **does** change
  production query execution (was mislabelled "no behavior change") and carries a retroactive AC
  requiring a unique ORDER BY tiebreaker on every `Skip/Take` + collection-`Include` query. The AC
  references a **real** test (`AdminOrderServiceTests.GetOrdersAsync_TiedCreatedAt_PagesDeterministically
  KeepingItemsPerOrder`) and correctly notes the split-query symptom itself is a Postgres/3-env concern.
- **F27 — verified (and swept wider than the finding cited).** The finding named
  `implementation-plan.md`; the fix-diff micro-review caught a **second** stale `varchar(500)` in
  `intents/019-.../requirements.md` and fixed it too. Grep confirms **no** ThumbnailPath `varchar(500)` /
  `character varying(500)` remains anywhere under `memory-bank`; the column is `512` everywhere.

### Deferrals — all twenty legitimate (fresh anchored agent)

Every deferred finding's named code condition is still **verbatim present at HEAD** — a genuine
deferral, not a silent fix:

- **F1 / F11 / F18 (→ bolt-043 orphan sweep).** `UploadService.cs:218-220` still holds the non-atomic
  `stillLive` `AnyAsync` re-read (not `ExecuteUpdate`); **zero** `ExecuteUpdate`/`ExecuteDelete` anywhere
  under `src/` (the durable fix was genuinely not slipped in); cleanup still gates on
  `if (upload.ThumbnailPath is not null)` (`UploadCleanupJob.cs:101`). F6's new `deleted_row_race` log
  makes the race observable in the interim but does not resolve the non-atomicity. Impact is an
  orphaned-file leak, not data loss.
- **F14 (→ 3-env).** `AddUploadThumbnailPath.cs:24` still branches `character varying(512)` : `TEXT` on
  `isNpgsql`; the migration test runs only the SQLite arm and its own comment concedes the Npgsql arm is
  deferred. F2's split-query symptom rides this same Postgres-CI phase.
- **F24 (→ bolt-043).** `UploadsController.cs:155` still reads `stream.Length` with no `CanSeek` guard;
  only seekable impls exist today. Latent, not live.
- **Long tail (F9, F10, F12, F13, F15, F16, F17, F19, F20, F21, F22, F25, F26, F28).** Each condition
  confirmed still present (Windows-dev-only races F10; FE blob leaks F20/F21 with no `takeUntilDestroyed`;
  observability gaps F15/F22; test gaps F12/F13/F16/F17; cleanups F25/F26/F28). **F16 also served as an
  F7 cross-check: `MaxFrames = 1` is still applied at the new `Image.LoadAsync<Rgba32>` call site — F7 did
  not drop the frame-bomb cap.**

### F8 deferral — reaffirmed, with a sharpened cost (anchored agent)

F8 (device-local recoverable preview cache) stays a legitimate **Low** deferral — the exposure needs a
shared browser profile + the GUID URL in history + the same 30-day window, and `private` already bars
CDN/proxy caches. **Correction to the resolution's framing for the owner's decision:** the resolution
says the fix (`private, no-cache`) "partially defeats the 30-day cache" — that slightly overstates the
cost. `no-cache` still lets the browser *store* the bytes; it only forces **revalidation**, and since the
endpoint already emits an ETag, a repeat view becomes a cheap conditional GET → 304 (which is exactly
what re-checks ownership). So the real cost is one small round-trip per view, not loss of caching. Framed
accurately, `private, no-cache` is a cheap standard secure default for an ownership-gated per-user
resource — closer to *should-fix-soon* than the resolution implied. Still not a blocker, correctly not
silently dropped; the owner now has the accurate trade-off.

### Fix-introduced-regression — none

The fixer's own pre-hand-back fresh-eyes micro-review (two independent anchored agents over the full fix
diff) and this pass's checks agree: no adjacent behavior broken. Specifically — F2's `ThenBy(o.Id)` only
breaks CreatedAt ties (no existing test/DTO consumer asserts a list *sequence*; all assert totals/counts)
and re-applies to the split `Items` query; F4 is test-only (production still throws the derived type);
F5/F6 add only Warning emissions on existing branches (no log-count test breaks — the SUT's class-level
logger is a loose mock); F7's `async Task<Image>` keeps the runtime task type so the reflection casts
hold, no caller reads the source pixel type, and ~400 MB peak stays under both the 512 MB backstop and
the limiter's 512 MB-per-slot budget; F3's merge is byte-identical to before on a first store and the
legitimate `guest-checkout-form` (validator-gated non-empty) overwrite still works.

### Carry-forward (not a v8 finding, not fix-introduced)

While fixing F2 the fixer spotted a **pre-existing** cousin: `AdminStatsService.GetProductStatsAsync`
(`AdminStatsService.cs:109-114`) does `OrderByDescending(TotalQuantity).Take(10)` on an in-memory GroupBy
— a tie at the #10/#11 boundary picks non-deterministically. It is a top-N stats display (not
pagination, not the split-query hazard), so it's cosmetic, and it was correctly **left untouched** (out
of the v8 finding set). Recorded here so a future discovery pass / the ledger can weigh a `ThenBy` there;
not a defect this pass reopens or blocks on.

---

## Pass notes (methodology / cost)

- **Anchored, cheap, per-fix** — no blinding, no manifest, no discovery workflow script. 6
  revert-and-rerun mutations by the main agent (5 backend batched + 1 frontend) + **1 fresh anchored
  read-only agent** for deferral legitimacy + doc accuracy, on top of the **2 anchored micro-review
  agents** the fixer ran over the fix diff before hand-back.
- **Revert-and-rerun is the load-bearing evidence.** Each behavioral fix's regression test was proven
  non-vacuous by mutation with exact attribution (only the expected test[s] went red; F3's reverse-guard
  test correctly stayed green).
- **Cost:** 3 anchored agents total across the fixer micro-review + this verification, + the main
  agent's mutation runs. Far cheaper than a discovery pass, as intended.

## Why this is not `approved`

A verification pass certifies that *these fixes held* — it cannot certify feature saturation (README
*Two loops*). review-v8 found 13 genuinely-new findings (curve decaying 32 → 24 → 13) and judged the
feature **not saturated**, explicitly because the fix-generativity loop was still live (F3 defeated a
v7-verified fix). This round fixed the runtime/data-loss subset, verified them, **and applied the new
fixer-contract rules** (class sweep, new-mechanism bar, an adversarial design-check on F7 that caught two
compile/return-type blockers, and a fresh-eyes fix-diff micro-review that caught a wider F27 token
sweep). Feature closure now wants **one more blinded discovery pass**: if it comes back quiet (0 new
mediums, only long-tail cleanups), the feature is a candidate for `approved`.

**Recommendation:** merge is unblocked (0 High, 0 blockers, all recommended fixes verified). Before
calling bolt 042 *closed*, run the outstanding discovery pass and pick up the deferred long tail in its
target phases: F1/F11/F18/F24 → bolt-043, F14 (+ F2's split-query verification) → 3-env/Postgres CI, F8
as an owner decision, and the FE/observability/cleanup Lows in a next fix round.

Cross-pass identity mapping: [ledger.md](ledger.md). Fixer's dispositions: [resolution-v8.md](resolution-v8.md).
