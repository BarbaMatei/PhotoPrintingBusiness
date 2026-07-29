---
id: 092-phase-4-learn-and-measure
unit: 004-phase-4-learn-and-measure
intent: 035-bug-hunter-agent-system
type: simple-construction-bolt
status: planned
stories:
  - 001-suppression-learning
  - 002-bug-lifecycle
  - 003-eval-corpus
  - 004-eval-metrics
  - 005-curator-agent
  - 006-orchestrator-learn-ext
created: 2026-06-10T10:40:14Z
started: null
completed: null
current_stage: null
stages_completed: []
time_box: 5h

requires_bolts: [091-phase-3-oracle-grounding]
enables_bolts: [093-phase-5-remediation, 094-optional-integration]
requires_units: [001-phase-1-skeleton, 002-phase-2-trust, 003-phase-3-breadth-and-scale]
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 1
---

# Bolt: 092-phase-4-learn-and-measure

## Overview

Tooling-only bolt. Phase 4 in full — guide Prompts 25–29b: suppression learning from
dismissal reasons, the bug lifecycle, the eval corpus + metrics, the Curator
(fills the Learn slot), and the orchestrator's Learn-slot extension.

## ⚠️ Construction Method (owner mandate + guide Part I — read before Stage 1)

**Each component MUST be created with the `skill-creator` skill** (`Skill` tool →
`skill-creator:skill-creator`): paste Prompt N from
`docs/agent-systems/bug-hunter-build-guide.md`, build, **run the brief's test prompts**, fix,
then next — in order. Prompt 29b **re-opens** `orchestrator` (one-line slot fill;
re-run prior orchestrator tests). If skill-creator is unavailable, **STOP and report**.

## Stories Included (build in this order)

1. **001-suppression-learning** (Prompt 25, Should) — proposed-never-auto-activated;
   validated vs Confirmed set
2. **002-bug-lifecycle** (Prompt 26, Should) — leave the "mark Fixed" seam open for P5
3. **003-eval-corpus** (Prompt 27, Should) — fixtures under `bug-hunting/eval/`
4. **004-eval-metrics** (Prompt 28, Should) — recall vs seeded; precision via dismissals
5. **005-curator-agent** (Prompt 29, Should) — Learn/Reconcile/Measure/Summarize
6. **006-orchestrator-learn-ext** (Prompt 29b, Should — EXTENSION)

## Bolt Type

**Type**: Simple Construction Bolt (tooling)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Read stories + briefs + unit brief
- [ ] **2. implement**: Build via skill-creator in order
- [ ] **3. test**: All test prompts green incl. orchestrator re-runs; a curation pass
      after a run with dismissals produces validated pattern proposals + metrics + the
      health summary

## Dependencies

### Requires
- 091-phase-3-oracle-grounding (master order; 29b re-opens the orchestrator after
  24d). **Replanning note:** if the owner descopes 091's oracle (its ⛔), this bolt
  may instead require [090 + the split non-oracle orchestrator extension] — owner
  decision at wave planning.

### Enables
- 093 (fix-verification extends bug-lifecycle); 094 (issue-sync follows lifecycle)

## Success Criteria

- [ ] 5 skills + 1 extension via skill-creator, all test prompts passing
- [ ] Safety properties hold: patterns proposed-only and validated against Confirmed;
      self-closes evidence-based and audited; regressions flagged; eval runs pinned
      (model/temp)

## Notes

**Time-box: 5h.** Spec of record: guide Part II Phase 4.
