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
enables_bolts: [092-phase-4-learn-and-measure]
requires_units: [001-phase-1-skeleton, 002-phase-2-trust]
blocks: true

complexity:
  avg_complexity: 2
  avg_uncertainty: 2
  max_dependencies: 3
  testing_scope: 2
---

# Bolt: 091-phase-3-oracle-grounding

## Overview

Tooling-only bolt. The v3 oracle tier — guide Prompts 24–24d: `intent-lookup` (read
the knowledge ledger's contracts) plus three cross-cutting extensions: hunters
surface contract contradictions (24b), Verifier/scoring weight contract corroboration
(24c), and the orchestrator gains specialists-dispatch + cost control + oracle wiring
(24d). Completes Phase 3.

## ⛔ GATED — cross-system prerequisite (requirements D6 — now a schedule)

`intent-lookup` consumes the **knowledge builder's `ledger-query` interface**, now
fully specified in `docs/agent-systems/integration-contract.md` (§2 envelope, §3 flow identity)
and built per `docs/agent-systems/knowledge-builder-build-guide.md`. Sequencing is normative in
the contract's §7: this bolt runs **after the knowledge builder's Phases 1–2** (which
themselves require bolts 085–088 of this intent first). If the owner instead descopes
the oracle for now, story 017's non-oracle parts (specialist dispatch, cost control,
incremental scanning) may be split out at replanning. **Do not stub the oracle
silently.**

## ⚠️ Construction Method (owner mandate + guide Part I — read before Stage 1)

**Each component MUST be created with the `skill-creator` skill** (`Skill` tool →
`skill-creator:skill-creator`): paste Prompt N from
`docs/agent-systems/bug-hunter-build-guide.md`, build, **run the brief's test prompts**, fix,
then next — in order. 24b/24c/24d **re-open existing skills** (hunters, bug-verifier,
orchestrator) — re-run each re-opened skill's original tests after. **This bolt must
never run in parallel with anything** (it touches skills from bolts 086–090). If
skill-creator is unavailable, **STOP and report**.

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
- [ ] **2. implement**: Build/extend via skill-creator in order
- [ ] **3. test**: New tests green + ALL re-opened skills' original test prompts
      re-run green (NFR-2); a diff-only budgeted run demonstrates cost control

## Dependencies

### Requires
- 089 + 090 (all hunters exist before 24b extends them)
- ⛔ External: knowledge ledger `ledger-query` interface (owner decision)

### Enables
- 092-phase-4-learn-and-measure (29b re-opens the orchestrator after 24d)

## Success Criteria

- [ ] Contract-contradiction findings cite the contradicted contract; model-prior-only
      findings tagged "intent-unconfirmed"; superseded contracts never treated as live
- [ ] Orchestrator dispatches specialists by risk class under a per-run budget with
      incremental + cheap-first ordering
- [ ] All prior phases' test prompts still pass (additive rule)

## Notes

**Time-box: 4h** (excluding the external-dependency wait). Spec of record: guide
Part II Phase 3 (Prompts 24–24d).
