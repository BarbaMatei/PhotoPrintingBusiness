---
id: 087-phase-2-trust
unit: 002-phase-2-trust
intent: 035-bug-hunter-agent-system
type: simple-construction-bolt
status: planned
stories:
  - 001-severity-scoring
  - 002-tool-ingest
  - 003-bug-verifier
  - 004-git-revision-tracking
  - 005-orchestrator-verify-wiring
  - 002-bug-documentation
  - 008-id-reservation-parallel-worktrees
  - 009-reconciler-trust-gate-rescore
  - 010-confidence-axis-reporting-floor
  - 011-owner-queue-age-escalation
  - 012-atomic-record-publish
created: 2026-06-10T10:40:14Z
started: null
completed: null
current_stage: null
stages_completed: []
time_box: 6h

requires_bolts: []
enables_bolts: [088-phase-3-map-and-reachability]
requires_units: []
blocks: false

complexity:
  avg_complexity: 3
  avg_uncertainty: 2
  max_dependencies: 3
  testing_scope: 2
---

# Bolt: 087-phase-2-trust

## Overview

Tooling-only bolt. **Re-scoped 2026-09:** the engine is the review loop under `reviews/`, so
this bolt builds only Phase 2's four gaps — a real risk score plus the reachability weight
(8, 14b), deterministic-tool ingestion (9), **execution proof** in the Verify slot (10), and
moved/fixed detection across runs (11). The orchestrator wiring (11b) is already satisfied by
the pass router. First bolt in the ruled order (integration contract §7) — the cheapest gaps,
and every later finding leans on them. After it, a high-severity finding carries a failing test
written by someone who did not fix it, and a risk score instead of a bare severity.

## ⚠️ Construction Method (owner mandate + guide Part I — read before Stage 1)

Each component **extends the review loop** (`reviews/lib`, `.claude/skills`) at the seam named
in its story; build it as a skill or script in that tree, with a test under
`reviews/lib/tests`, following `reviews/README.md`'s conventions. Three of the four gaps are
edits to code that already exists (the records schema, the verification pass, `verify/git.mjs`)
— re-run the tests that cover those files before hand-back; `tool-ingest` (9) is the one new
piece, a script under `reviews/lib`. The guide's Prompt N stays the specification of each
piece's behaviour.

## Stories Included (build in this order)

1. **001-severity-scoring** (Prompt 8, Must)
2. **002-tool-ingest** (Prompt 9, Must)
3. **003-bug-verifier** (Prompt 10, Must) — sandbox-vs-commit check; flaky double-run
4. **004-git-revision-tracking** (Prompt 11, Must)
5. ~~**005-orchestrator-verify-wiring** (Prompt 11b)~~ — **satisfied** by the pass router's
   rows (`reviews/lib/drive/rows.mjs`); no work in this bolt

### Added 2026-09-04 by the Phase 1 verification bolts (085 / 086)

Bolts 085 and 086 checked the claim that the review loop already satisfies unit 001 and confirmed
it story by story, with named gaps. Those gaps land here. They belong to **unit
001-phase-1-skeleton**, not unit 002 — the cross-unit assignment is intended, because each one
extends a component this bolt is already opening:

6. **002-bug-documentation** (unit 001, Prompt 2) — re-assigned, not a new story: the loop has no
   three-audience record. ⚠️ Two stories in this list now begin `002-`; they are different files
   in different units (`002-tool-ingest` is unit 002's). Any tooling that resolves a story by its
   numeric prefix will pick the wrong one — resolve by the full id.
7. **008-id-reservation-parallel-worktrees** (unit 001) — two worktrees mint the same `PPW-<n>`.
   Sequence this alone, not beside a wave of parallel worktrees: it edits `reviews/state/id-counter`.
8. **009-reconciler-trust-gate-rescore** (unit 001) — the reconciler's ground-truth score is two
   material rule-changes old.
9. **010-confidence-axis-reporting-floor** (unit 001) — confidence never reaches a record; also
   carries the injection flag, the redaction rule and the Observations section.
10. **011-owner-queue-age-escalation** (unit 001) — the parked-decision queue never ages, records
    no actor, and checks no status transition.
11. **012-atomic-record-publish** (unit 001) — records are published with a plain overwrite.

Evidence for all six: `memory-bank/bolts/085-phase-1-skeleton-core/test-walkthrough.md` and
`memory-bank/bolts/086-phase-1-skeleton-agents/test-walkthrough.md`.

## Bolt Type

**Type**: Simple Construction Bolt (tooling)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Read the four stories + their briefs + the unit brief, and the seam files
      they name. **Where the proof runs**: agree with the owner whether the failing test runs
      on the host (the repo's own `dotnet test` / `npm test` commands) or in a throwaway
      container; NFR-3 rules apply either way (caps, no production data)
- [ ] **2. implement**: Close the four gaps in order at their seams
- [ ] **3. test**: Each gap has a test under `reviews/lib/tests`; the tests already covering
      the touched files still pass; a run carries a risk score, and a high-severity finding
      carries a failing test naming the commit it was taken on

## Dependencies

### Requires
- The review loop (`reviews/`) — built; it is what these four gaps extend
- External: none. The proof runs the repo's own test commands, so the Phase 2 sandbox recipe
  (D4) is no longer a gate on this bolt

### Enables
- 088-phase-3-map-and-reachability (14b extends scoring); 093 (proving tests reused)

## Success Criteria

- [ ] The four gaps closed at their named seams, each with a test under `reviews/lib/tests`
- [ ] Verify slot proves rather than argues: a failing test by a non-fixer, commit-matched,
      flaky-test double-run; Low findings still reported (appendix)
- [ ] Blanket "unverified" label replaced by per-finding confidence; SHA at open,
      reconciliation proposals at close

## Notes

**Time-box: 6h** (the execution proof brings real environment work). Spec of record: guide
Part II Phase 2 + its "Implementation status (2026-09)" table; order per integration
contract §7.
