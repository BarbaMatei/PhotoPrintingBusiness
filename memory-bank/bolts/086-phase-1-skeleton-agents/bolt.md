---
id: 086-phase-1-skeleton-agents
unit: 001-phase-1-skeleton
intent: 035-bug-hunter-agent-system
type: simple-construction-bolt
status: planned
stories:
  - 006-general-hunter
  - 007-orchestrator-skeleton
created: 2026-06-10T10:40:14Z
started: null
completed: null
current_stage: null
stages_completed: []
time_box: 3h

requires_bolts: [085-phase-1-skeleton-core]
enables_bolts: [087-phase-2-trust]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 2
---

# Bolt: 086-phase-1-skeleton-agents

## Overview

Tooling-only bolt. Completes Phase 1 with the two agents-as-skills — guide Prompts
6–7: `general-hunter` and the `orchestrator` skeleton that defines all six permanent
pipeline slots. Ends with the system's **first end-to-end run on this repo**.

## ⚠️ Construction Method (owner mandate + guide Part I — read before Stage 1)

**Each skill MUST be created with the `skill-creator` skill** (`Skill` tool →
`skill-creator:skill-creator`): paste Prompt 6 then Prompt 7 from
`docs/agent-systems/bug-hunter-build-guide-v3.6.md`, build, **run each brief's three test prompts**,
fix before proceeding. If skill-creator is unavailable, **STOP and report**.

## Stories Included (build in this order)

1. **006-general-hunter** (Prompt 6, Must) — the skeleton's one hunting capability
2. **007-orchestrator-skeleton** (Prompt 7, Must) — defines all six slots; Verify
   pass-through; Phase 1 report labeled **"unverified candidates — high
   false-positive rate until Phase 2"**

## Bolt Type

**Type**: Simple Construction Bolt (tooling)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Read both stories + briefs + unit brief; confirm bolt 085's five
      skills pass their tests
- [ ] **2. implement**: Build both skills **via skill-creator**, hunter first
- [ ] **3. test**: The Phase 1 milestone — run the system end-to-end on this repo:
      ledger updated, NEW labeled report in `bug-hunting/reports/`, second run
      surfaces only new findings

## Dependencies

### Requires
- 085-phase-1-skeleton-core (all five foundation skills)

### Enables
- 087-phase-2-trust (11b re-opens the orchestrator built here)

## Success Criteria

- [ ] `general-hunter` + `orchestrator` skills exist, built via skill-creator, briefs'
      test prompts passing
- [ ] All six slots explicitly defined in the orchestrator (additive seams for 11b/24d/29b)
- [ ] First full run completed on this repo with the "unverified" label and the
      reporting floor; read-only on source verified

## Notes

**Time-box: 3h.** This bolt closes Unit 1 — after it, the system works (just doesn't
verify yet). Spec of record: guide Part II Phase 1.
