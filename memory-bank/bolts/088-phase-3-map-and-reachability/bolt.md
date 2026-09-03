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

Tooling-only bolt. **Re-scoped 2026-09:** the review loop has **no Map slot at all**, which is
the biggest hole in the engine — this bolt fills it. The application map (12), the searchable
code index (13) and reachability (14, with the **framework-aware unknown weight** that matters
in this DI/attribute-routing .NET stack) are all missing; the scoring extension (14b) then adds
reachability as the third risk factor, and the budget-and-incremental-scanning half of Prompt
24d belongs here too (the loop caps delta passes and picks lenses by touched area, but has no
budget unit). `flow-tracing` (15) is left as it is — the lenses trace flows by prompt.
`code-index` is a **shared deterministic tool** with the knowledge builder (contract §7): keep
judgment out of it.

## ⚠️ Construction Method (owner mandate + guide Part I — read before Stage 1)

Each component **extends the review loop** (`reviews/lib`, `.claude/skills`) at the seam named
in its story; build it as a skill or script in that tree, with a test under
`reviews/lib/tests`, following `reviews/README.md`'s conventions. The map, the index and
reachability are new deterministic tools under `reviews/lib`, wired into
`reviews/lib/discovery-review.wf.js`; 14b edits the same scoring code Prompt 8 touched, so
re-run its tests. The guide's Prompt N stays the specification of each piece's behaviour.

## Stories Included (build in this order)

1. **001-app-mapping** (Prompt 12, Must)
2. **002-code-index** (Prompt 13, Must)
3. **003-reachability** (Prompt 14, Must)
4. **004-severity-scoring-reachability-ext** (Prompt 14b, Must — EXTENSION)
5. ~~**005-flow-tracing** (Prompt 15)~~ — partial by design: the lenses trace flows by prompt;
   no work in this bolt
6. The **budget unit + incremental scanning** half of `017-orchestrator-scale-ext` (Prompt 24d)
   — the story file stays with bolt 091, whose oracle wiring is its other half

## Bolt Type

**Type**: Simple Construction Bolt (tooling)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Read stories + briefs + unit brief (repo risk classes: auth,
      checkout/payment, order state machine, uploads, invoicing, shipping)
- [ ] **2. implement**: Build in order at the named seams (index before reachability)
- [ ] **3. test**: A test per piece under `reviews/lib/tests` + the scoring tests re-run; risk =
      severity × confidence × reachability with the unknown weight calibrated for this stack

## Dependencies

### Requires
- 087-phase-2-trust (the risk score exists to extend)

### Enables
- 089 + 090 (specialists consume index/map/tracing) — those two run **wave-parallel**

## Success Criteria

- [ ] Map, index, reachability and the scoring extension live at their seams, each with a test
      under `reviews/lib/tests`
- [ ] Map with risk-classed flows in the records tree; incremental index; reachable High
      outranks unreachable Critical; DI-heavy "unknown" doesn't flatten the signal
- [ ] A run stops at its budget and says so

## Notes

**Time-box: 5h.** Index/map persistence lives under `bug-hunting/` (D3). Spec of
record: guide Part II Phase 3.
