---
id: 091-phase-3-oracle-grounding
unit: 003-phase-3-breadth-and-scale
intent: 035-bug-hunter-agent-system
type: simple-construction-bolt
status: planned
stories:
  - 014-intent-lookup
  - 015-hunters-contract-ext
  - 016-verifier-scoring-contract-ext
  - 017-orchestrator-scale-ext
created: 2026-06-10T10:40:14Z
started: null
completed: null
current_stage: null
stages_completed: []
time_box: 4h

requires_bolts: [089-phase-3-specialists-a, 090-phase-3-specialists-b]
enables_bolts: []
requires_units: [002-phase-2-trust]
blocks: true
notes: gated on the knowledge-builder's ledger-query per the owner's 2026-09 build-order ruling; last in the order, so nothing waits on it

complexity:
  avg_complexity: 2
  avg_uncertainty: 2
  max_dependencies: 3
  testing_scope: 2
---

# Bolt: 091-phase-3-oracle-grounding

## Overview

Tooling-only bolt. The oracle tier — guide Prompts 24–24d, all missing: `intent-lookup` (24,
read the knowledge ledger's contracts) plus three cross-cutting extensions: lenses surface
contract contradictions (24b), verification/scoring weight contract corroboration (24c), and
the orchestrator's oracle wiring (24d — its budget-and-incremental half belongs to bolt 088).
**Re-scoped 2026-09: this bolt is now LAST in the order** (integration contract §7), because it
is the only piece that cannot start until the knowledge builder exists. Nothing else waits on
it — learn & measure (092) and the remediation hand-off (093) run before it.

## ⛔ GATED — cross-system prerequisite (requirements D6 — now a schedule)

`intent-lookup` consumes the **knowledge builder's `ledger-query` interface**, now
fully specified in `docs/agent-systems/integration-contract.md` (§2 envelope, §3 flow identity)
and built per `docs/agent-systems/knowledge-builder-build-guide.md`. Sequencing is normative in
the contract's §7: this bolt runs **last**, after the knowledge builder's Phases 1–2 (which
themselves want bolts 087–088 of this intent first). Story 017's non-oracle parts (cost
control, incremental scanning, the budget unit) are scheduled with bolt 088 and can be built
without the oracle. **Do not stub the oracle silently.**

## ⚠️ Construction Method (owner mandate + guide Part I — read before Stage 1)

Each component **extends the review loop** (`reviews/lib`, `.claude/skills`) at the seam named
in its story; build it as a skill or script in that tree, with a test under
`reviews/lib/tests`, following `reviews/README.md`'s conventions. `intent-lookup` is the one
genuinely **new standalone skill** left in this intent — build that one with the
`skill-creator` skill (`Skill` tool → `skill-creator:skill-creator`), paste Prompt 24, and run
the brief's three test prompts; if skill-creator is unavailable, **STOP and report**.
24b/24c/24d re-open pieces built in bolts 088–090 — re-run their tests after. **This bolt must
never run in parallel with anything.**

## Stories Included (build in this order)

1. **014-intent-lookup** (Prompt 24, Must) — the oracle read; authority/superseded tagging
2. **015-hunters-contract-ext** (Prompt 24b, Must — EXTENSION across all built hunters)
3. **016-verifier-scoring-contract-ext** (Prompt 24c, Must — EXTENSION)
4. **017-orchestrator-scale-ext** (Prompt 24d, Must — EXTENSION: map refresh,
   specialist dispatch, reachability+oracle into Verify, clustering into Triage,
   run budget + incremental + cheap-first)

## Bolt Type

**Type**: Simple Construction Bolt (tooling)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Resolve the ⛔ above with the owner; read stories + briefs;
      inventory which hunters exist (for 24b's scope)
- [ ] **2. implement**: Build `intent-lookup` with skill-creator, then the three extensions at
      their seams
- [ ] **3. test**: New tests under `reviews/lib/tests` green + every re-opened piece's own
      tests re-run green (NFR-2)

## Dependencies

### Requires
- 089 + 090 (all hunters exist before 24b extends them)
- ⛔ External: knowledge ledger `ledger-query` interface (owner decision)

### Enables
- (nothing — last in the order per the 2026-09 ruling; 092 and 093 no longer wait on it)

## Success Criteria

- [ ] Contract-contradiction findings cite the contradicted contract; model-prior-only
      findings tagged "intent-unconfirmed"; superseded contracts never treated as live
- [ ] Orchestrator dispatches specialists by risk class under a per-run budget with
      incremental + cheap-first ordering
- [ ] All prior phases' test prompts still pass (additive rule)

## Notes

**Time-box: 4h** (excluding the external-dependency wait). Spec of record: guide
Part II Phase 3 (Prompts 24–24d).
