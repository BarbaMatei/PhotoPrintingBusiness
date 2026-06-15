---
id: 089-phase-3-specialists-a
unit: 003-phase-3-breadth-and-scale
intent: 035-bug-hunter-agent-system
type: simple-construction-bolt
status: planned
stories:
  - 006-taint-analysis
  - 007-flow-tracer-agent
  - 008-file-sweeper-agent
  - 009-security-auditor-agent
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

# Bolt: 089-phase-3-specialists-a

## Overview

Tooling-only bolt. Specialist hunters, first half — guide Prompts 16–19:
`taint-analysis` (the security procedure) and three agents-as-skills:
`flow-tracer-agent`, `file-sweeper-agent`, `security-auditor-agent`.

**Wave note:** runs **in parallel with bolt 090** — all-new disjoint skill
directories, both depending only on 088. Neither contains extension briefs, so no
shared files are touched.

## ⚠️ Construction Method (owner mandate + guide Part I — read before Stage 1)

**Each skill MUST be created with the `skill-creator` skill** (`Skill` tool →
`skill-creator:skill-creator`): paste Prompt N from
`docs/agent-systems/bug-hunter-build-guide.md`, build, **run the brief's three test prompts**,
fix, then next — in order 16 → 17 → 18 → 19. If skill-creator is unavailable,
**STOP and report**.

## Stories Included (build in this order)

1. **006-taint-analysis** (Prompt 16, Must) — sources→sinks with sanitizer awareness
2. **007-flow-tracer-agent** (Prompt 17, Must) — top-down hunt, riskiest flows first
3. **008-file-sweeper-agent** (Prompt 18, Must) — bottom-up sweep, tools first
4. **009-security-auditor-agent** (Prompt 19, Must) — taint + authz + secrets + vuln classes

## Bolt Type

**Type**: Simple Construction Bolt (tooling)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Read stories + briefs + unit brief (repo: EF parameterization as
      the SQL sanitizer baseline; webhooks/uploads as sources; ownership checks on
      load-by-ID endpoints)
- [ ] **2. implement**: Build via skill-creator in order
- [ ] **3. test**: All 12 test prompts green; hunters emit candidates-only,
      dedup-first, read-only

## Dependencies

### Requires
- 088-phase-3-map-and-reachability (code-index, app-mapping, flow-tracing)

### Enables
- 091-phase-3-oracle-grounding (24b re-opens these hunters)

## Success Criteria

- [ ] 4 skills via skill-creator, all test prompts passing
- [ ] Convention compliance: candidate shape, surface-everything, dedup-before-emit,
      coverage updates, read-only

## Notes

**Time-box: 4h.** Wave-parallel with 090 (one branch/PR each per the owner's
worktree workflow). Spec of record: guide Part II Phase 3.
