---
type: resolution
target: 042-thumbnail-cache
answers_review: review-v5.md
version: 5
branch: feat/bolt-042-thumbnail-cache
status: resolved
fixed_commit: 838c9b6
opened: 2026-07-14
closed: 2026-07-14
findings:
  V5-1: { status: wont-fix, commit: null, note: "M1 residual write-vs-cleanup window under a symmetric interleaving. Accepted, not code-fixed: it's the fundamental non-transactional file-vs-DB TOCTOU, same class as NEW-3/D31 whose orphan sweep (deferred to bolt-043) is the designed backstop; a cleanup-side unconditional delete narrows but can't fully close it either. M1's liveness re-read stands as a sound mitigation." }
  V5-2: { status: fixed, commit: 838c9b6, note: "Completed the C4 walkthrough refresh: line 44 provider-aware migration (was 'SQLite-typed not Npgsql, functionally correct' — a material contradiction), line 21 MaxDecodeDimension->MaxDecodePixels (100 MP area), line 56 >25000 dims->over-100 MP." }
  V5-3: { status: fixed, commit: 838c9b6, note: "IImageProcessor doc comment 'max 300 px'->'max 800 px' (C7)." }
---

# Resolution — Bolt 042: Thumbnail Cache (answers review-v5)

Fixer response to the three follow-ups the v5 **verification** surfaced. review-v5 verified the 26
resolution-v4 fixes as non-vacuous + correct and upheld the deferrals/dispute; this closes the
residuals it found.

| ID | Status | How |
|----|--------|-----|
| V5-1 (M1 residual race) | wont-fix (accepted) | Backstopped by the deferred **D31** orphan sweep (bolt-043); see decisions. |
| V5-2 (C4 walkthrough incomplete) | fixed `838c9b6` | Rewrote the 3 residual contradictions to match shipped code. |
| V5-3 (C7 stale doc comment) | fixed `838c9b6` | `IImageProcessor` doc 300→800 px. |

## Decisions

- **V5-1 → wont-fix (accepted limitation), NOT reopened.** The M1 fix (post-write liveness re-read)
  closes the finding's stated ordering. The residual — preview persists `ThumbnailPath`, its re-read
  sees the row still live *before* the cleanup job commits its `DeletedAt`, so neither side deletes the
  thumb — is the fundamental non-transactional file-vs-DB TOCTOU. It is the **same defect class as
  NEW-3 (D31)**, whose orphaned-thumbnail sweep is already **deferred to bolt-043** as the designed
  backstop. Adding a cleanup-side unconditional delete of the deterministic key would narrow the window
  but cannot fully close it either (a write strictly after cleanup's delete + before its commit still
  leaks). Fully closing it needs either a transaction/lock spanning the file+DB write (disproportionate
  for a cached, regenerable artifact) or the D31 sweep. So: keep M1's mitigation, rely on D31. Recorded
  in [ledger.md](ledger.md) against D34/D31.
- **V5-2 / V5-3 → fixed** (doc/comment only; no test per the doc-only rule). These complete work the
  v4 C4/C7 fixes left incomplete — a re-review is not required to accept a doc correction, but they ride
  along for the next pass's record.

## Hand-back

All v5 follow-ups are terminal. No code behaviour changed since `fixed_commit` `6c4f334` (V5-2/V5-3 are
doc/comment only), so the v5 verification of the code fixes stands. **Feature-closure still wants a
saturated *discovery* pass** — a later blinded pass that comes back quiet — which neither v4 (found 32)
nor this verification can certify.
