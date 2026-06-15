---
id: 088-phase-3-map-and-reachability
unit: 003-phase-3-breadth-and-scale
intent: 035-bug-hunter-agent-system
type: simple-construction-bolt
status: planned
stories:
  - 001-app-mapping
  - 002-code-index
  - 003-reachability
  - 004-severity-scoring-reachability-ext
  - 005-flow-tracing
created: 2026-06-10T10:40:14Z
started: null
completed: null
current_stage: null
stages_completed: []
time_box: 5h

requires_bolts: [087-phase-2-trust]
enables_bolts: [089-phase-3-specialists-a, 090-phase-3-specialists-b]
requires_units: [002-phase-2-trust]
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 2
  max_dependencies: 2
  testing_scope: 1
---

# Bolt: 088-phase-3-map-and-reachability

## Overview

Tooling-only bolt. Phase 3's structural layer — guide Prompts 12–15: the application
map, the searchable code index, reachability (with the v3 **framework-aware unknown
weight** — directly relevant to this DI/attribute-routing .NET stack), the scoring
extension adding reachability as the third factor, and the shared flow-tracing
procedure.

## ⚠️ Construction Method (owner mandate + guide Part I — read before Stage 1)

**Each component MUST be created with the `skill-creator` skill** (`Skill` tool →
`skill-creator:skill-creator`): paste Prompt N from
`docs/agent-systems/bug-hunter-build-guide-v3.6.md`, build, **run the brief's test prompts**, fix,
then next — in order. Prompt 14b **re-opens** `severity-scoring` (re-run Prompt 8's
tests after). If skill-creator is unavailable, **STOP and report**.

## Stories Included (build in this order)

1. **001-app-mapping** (Prompt 12, Must)
2. **002-code-index** (Prompt 13, Must)
3. **003-reachability** (Prompt 14, Must)
4. **004-severity-scoring-reachability-ext** (Prompt 14b, Must — EXTENSION)
5. **005-flow-tracing** (Prompt 15, Must)

## Bolt Type

**Type**: Simple Construction Bolt (tooling)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Read stories + briefs + unit brief (repo risk classes: auth,
      checkout/payment, order state machine, uploads, invoicing, shipping)
- [ ] **2. implement**: Build via skill-creator in order (index before reachability)
- [ ] **3. test**: All test prompts green incl. Prompt 8 re-run; risk = severity ×
      confidence × reachability with the unknown weight calibrated for this stack

## Dependencies

### Requires
- 087-phase-2-trust (severity-scoring exists to extend)

### Enables
- 089 + 090 (specialists consume index/map/tracing) — those two run **wave-parallel**

## Success Criteria

- [ ] 4 new skills + 1 extension via skill-creator, all test prompts passing
- [ ] Map with risk-classed flows in the ledger; incremental index; reachable High
      outranks unreachable Critical; DI-heavy "unknown" doesn't flatten the signal

## Notes

**Time-box: 5h.** Index/map persistence lives under `bug-hunting/` (D3). Spec of
record: guide Part II Phase 3.
