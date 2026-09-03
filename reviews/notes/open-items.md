---
type: open-items-note
status: live
created: 2026-09-02
owner: Matei Barba
---

# Open items

Live tracker for owner decisions the machinery restructure left pending. Kept short by
design — depth lives in the archived notes each entry links into.

1. **Merge PR #12.** The redesign branch (`chore/loop-speed-redesign`) has been ready since
   the chapter-close wave; whether and when to merge it into
   `feat/bolt-038-vat-calculation` is still the owner's call. Context:
   [loop-speed-handoff.md](archive/loop-speed-handoff.md).

2. **One real target run through the rebuilt loop.** Recommended before any of the
   restructure's delete decisions are treated as final — the run supplies the evidence
   nothing has provided yet. Acceptance targets, measured by `speed-report.mjs`: ≤15
   min/fix all-in (baseline median 29), ≥90% doc-gate first-pass approval, ≤0.15 record
   sittings per fixed finding. Context:
   [machinery-restructure-plan.md](archive/machinery-restructure-plan.md),
   [machinery-architecture-review.md](archive/machinery-architecture-review.md).

3. **The skill evals' first shakedown.** `loop-driver/evals` and `fix-review/evals` were
   rebuilt in-tree against today's machinery but never run through `claude plugin eval` —
   status is REBUILT/UNRUN. Context:
   [machinery-architecture-review.md](archive/machinery-architecture-review.md#8-migration-order-for-7b)
   (step 12).

4. **The prose-only "Per-provider symmetry" lens row.** Carries no manifest key by design
   (`key: null` in `records/schema.mjs`); whether it should ever be promoted to a real,
   keyed lens is still an open call. Context:
   [machinery-architecture-review.md](archive/machinery-architecture-review.md#4-rules-ledger).

5. **Cold/loaded-machine suite time, if it ever matters.** "Full run under one minute"
   holds warm on an idle machine (30–47 s) but not under load (82–103 s); nobody has asked
   for a loaded-machine budget. Context:
   [machinery-restructure-plan.md](archive/machinery-restructure-plan.md).

6. **Post-merge follow-ups from the reconciliation.** Seeded run 2, the planted-bug recall
   experiment (D7: post-merge, owner-scheduled); a decision record for the standing-sweep
   mode — the scheduled pass over the whole codebase on `main` (D1); the id-reservation
   mechanism that stops two passes minting the same `PPW-n` id (D2); the proof rule, under
   which a 🔴 top-severity finding counts only with a failing test written by someone other
   than the fixer (D3). Context:
   [reconciliation-plan-2026-09.md](../../docs/agent-systems/reconciliation-plan-2026-09.md).
