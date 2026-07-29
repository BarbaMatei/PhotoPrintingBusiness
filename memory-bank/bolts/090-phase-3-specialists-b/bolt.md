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
enables_bolts: [091-phase-3-oracle-grounding]
requires_units: [001-phase-1-skeleton, 002-phase-2-trust]
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 1
---

# Bolt: 090-phase-3-specialists-b

## Overview

Tooling-only bolt. Specialist hunters, second half + triage — guide Prompts 20–23:
`dependency-audit-agent` (CVEs from live advisories), `config-auditor-agent`
(config/infra bug class), `concurrency-auditor-agent` (conditional — Should), and
`root-cause-clustering`.

**Wave note:** runs **in parallel with bolt 089** — all-new disjoint skill
directories, both depending only on 088. No extension briefs here.

## ⚠️ Construction Method (owner mandate + guide Part I — read before Stage 1)

**Each skill MUST be created with the `skill-creator` skill** (`Skill` tool →
`skill-creator:skill-creator`): paste Prompt N from
`docs/agent-systems/bug-hunter-build-guide.md`, build, **run the brief's three test prompts**,
fix, then next — in order 20 → 21 → 22 → 23. If skill-creator is unavailable,
**STOP and report**.

## Stories Included (build in this order)

1. **010-dependency-audit-agent** (Prompt 20, Must) — NuGet + npm vs OSV/GH Advisory,
   queried live at run time
2. **011-config-auditor-agent** (Prompt 21, Must) — compose/CI/appsettings/env;
   reuse the repo's gitleaks via tool-ingest
3. **012-concurrency-auditor-agent** (Prompt 22, **Should** — conditional per D5;
   this async-heavy stack qualifies, owner may still defer)
4. **013-root-cause-clustering** (Prompt 23, Must)

## Bolt Type

**Type**: Simple Construction Bolt (tooling)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Read stories + briefs + unit brief; confirm with the owner whether
      story 012 builds now or defers (Should)
- [ ] **2. implement**: Build via skill-creator in order
- [ ] **3. test**: All test prompts green; dependency hits carry CVE id + fixed
      version; clustering stays conservative

## Dependencies

### Requires
- 088-phase-3-map-and-reachability (code-index, flow-tracing)

### Enables
- 091-phase-3-oracle-grounding (24b re-opens these hunters too, if built)

## Success Criteria

- [ ] 3–4 skills via skill-creator (012 per owner call), all test prompts passing
- [ ] Two new bug classes live (Dependency, Configuration); N-symptoms→1-bug
      clustering with multi-location records

## Notes

**Time-box: 4h.** Wave-parallel with 089. Spec of record: guide Part II Phase 3.
