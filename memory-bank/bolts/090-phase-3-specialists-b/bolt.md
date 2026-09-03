---
id: 090-phase-3-specialists-b
unit: 003-phase-3-breadth-and-scale
intent: 035-bug-hunter-agent-system
type: simple-construction-bolt
status: planned
stories:
  - 010-dependency-audit-agent
  - 011-config-auditor-agent
  - 012-concurrency-auditor-agent
  - 013-root-cause-clustering
created: 2026-06-10T10:40:14Z
started: null
completed: null
current_stage: null
stages_completed: []
time_box: 4h

requires_bolts: [088-phase-3-map-and-reachability]
enables_bolts: [091-phase-3-oracle-grounding, 092-phase-4-learn-and-measure]
requires_units: [002-phase-2-trust]
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 1
---

# Bolt: 090-phase-3-specialists-b

## Overview

Tooling-only bolt. **Re-scoped 2026-09:** three gaps of Prompts 20–23 —
`dependency-audit-agent` (20, CVEs from live advisories) and `config-auditor-agent` (21,
config/infra bug class) are two lenses the manifest lacks, both consumers of `tool-ingest`;
`root-cause-clustering` (23) is partial — the fixer clusters and the reconciler tracks lineage,
but one record covering many locations does not exist. `concurrency-auditor-agent` (22) is
**satisfied** by the race lens.

**Wave note:** runs **in parallel with bolt 089** — disjoint files, both waiting on the Map
slot (088). No extension briefs here.

## ⚠️ Construction Method (owner mandate + guide Part I — read before Stage 1)

Each component **extends the review loop** (`reviews/lib`, `.claude/skills`) at the seam named
in its story; build it as a skill or script in that tree, with a test under
`reviews/lib/tests`, following `reviews/README.md`'s conventions. The two new lenses are a row
each in the manifest's one machine home (`reviews/lib/records/schema.mjs`) plus their prompts —
the runbook tables regenerate from that file, so never hand-edit them; clustering extends the
records tree (`reviews/lib/records/ledger.mjs`). The guide's Prompt N stays the specification of
each piece's behaviour.

## Stories Included (build in this order)

1. **010-dependency-audit-agent** (Prompt 20, Must) — NuGet + npm vs OSV/GH Advisory,
   queried live at run time
2. **011-config-auditor-agent** (Prompt 21, Must) — compose/CI/appsettings/env;
   reuse the repo's gitleaks via tool-ingest
3. ~~**012-concurrency-auditor-agent** (Prompt 22)~~ — **satisfied** by the race lens
   (`reviews/lib/records/schema.mjs` + its prompt); no work in this bolt
4. **013-root-cause-clustering** (Prompt 23, Must)

## Bolt Type

**Type**: Simple Construction Bolt (tooling)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Read the three stories + their briefs + the unit brief, and the seam files
      they name
- [ ] **2. implement**: Add the two lens rows and their prompts, then clustering
- [ ] **3. test**: A test per piece under `reviews/lib/tests`; dependency hits carry CVE id +
      fixed version; clustering stays conservative

## Dependencies

### Requires
- 088-phase-3-map-and-reachability (code-index, flow-tracing)

### Enables
- 091-phase-3-oracle-grounding (24b re-opens the lenses this bolt adds) and
  092-phase-4-learn-and-measure (the corpus scores what these lenses produce)

## Success Criteria

- [ ] The three gaps closed at their seams, each with a test under `reviews/lib/tests`
- [ ] Two new bug classes live (Dependency, Configuration); N-symptoms→1-bug
      clustering with multi-location records

## Notes

**Time-box: 4h.** Wave-parallel with 089. Spec of record: guide Part II Phase 3.
