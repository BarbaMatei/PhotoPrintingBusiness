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
requires_units: [002-phase-2-trust]
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 1
---

# Bolt: 089-phase-3-specialists-a

## Overview

Tooling-only bolt. **Re-scoped 2026-09:** of Prompts 16–19, only `taint-analysis` (16) is a
gap — a source-to-sink procedure the security lens does not have. `security-auditor-agent` (19)
is **satisfied** by the security lens, and `flow-tracer-agent` (17) / `file-sweeper-agent` (18)
are partial by design: the lenses do both, without a tools-first pass. Waits for the Map slot
(bolt 088), not for `tool-ingest` alone, because it reads the map.

**Wave note:** runs **in parallel with bolt 090** — all-new disjoint skill
directories, both depending only on 088. Neither contains extension briefs, so no
shared files are touched.

## ⚠️ Construction Method (owner mandate + guide Part I — read before Stage 1)

Each component **extends the review loop** (`reviews/lib`, `.claude/skills`) at the seam named
in its story; build it as a skill or script in that tree, with a test under
`reviews/lib/tests`, following `reviews/README.md`'s conventions. Here that means lens work —
the security lens's prompt plus, where a lens is added, its row in the manifest's one machine
home (`reviews/lib/records/schema.mjs`); no new skill directories. The guide's Prompt N stays
the specification of each piece's behaviour.

## Stories Included (build in this order)

1. **006-taint-analysis** (Prompt 16, Must) — sources→sinks with sanitizer awareness
2. ~~**007-flow-tracer-agent** (Prompt 17)~~ — partial by design (the lenses hunt top-down);
   no work in this bolt
3. ~~**008-file-sweeper-agent** (Prompt 18)~~ — partial by design (the lenses sweep, without a
   tools-first pass); no work in this bolt
4. ~~**009-security-auditor-agent** (Prompt 19)~~ — **satisfied** by the security lens
   (`reviews/lib/records/schema.mjs` + its prompt); no work in this bolt

## Bolt Type

**Type**: Simple Construction Bolt (tooling)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Read stories + briefs + unit brief (repo: EF parameterization as
      the SQL sanitizer baseline; webhooks/uploads as sources; ownership checks on
      load-by-ID endpoints)
- [ ] **2. implement**: Build `taint-analysis` at its seam
- [ ] **3. test**: A test under `reviews/lib/tests`; the security lens still emits
      candidates-only, dedup-first, read-only

## Dependencies

### Requires
- 088-phase-3-map-and-reachability (code-index, app-mapping, flow-tracing)

### Enables
- 091-phase-3-oracle-grounding (24b re-opens these hunters)

## Success Criteria

- [ ] `taint-analysis` live at its seam, with a test under `reviews/lib/tests`
- [ ] Convention compliance: candidate shape, surface-everything, dedup-before-emit,
      coverage updates, read-only

## Notes

**Time-box: 4h.** Wave-parallel with 090 (one branch/PR each per the owner's
worktree workflow). Spec of record: guide Part II Phase 3.
