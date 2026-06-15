---
id: 087-phase-2-trust
unit: 002-phase-2-trust
intent: 035-bug-hunter-agent-system
type: simple-construction-bolt
status: planned
stories:
  - 001-severity-scoring
  - 002-tool-ingest
  - 003-bug-verifier
  - 004-git-revision-tracking
  - 005-orchestrator-verify-wiring
created: 2026-06-10T10:40:14Z
started: null
completed: null
current_stage: null
stages_completed: []
time_box: 6h

requires_bolts: [086-phase-1-skeleton-agents]
enables_bolts: [088-phase-3-map-and-reachability]
requires_units: [001-phase-1-skeleton]
blocks: false

complexity:
  avg_complexity: 3
  avg_uncertainty: 2
  max_dependencies: 3
  testing_scope: 2
---

# Bolt: 087-phase-2-trust

## Overview

Tooling-only bolt. Phase 2 in full — guide Prompts 8–11b: real risk scoring,
deterministic-tool ingestion, the **hardened Verifier** (fills the Verify slot),
commit pinning, and the orchestrator wiring extension. After this bolt, findings are
trustworthy: confirmed by execution where possible, scored by real risk.

## ⚠️ Construction Method (owner mandate + guide Part I — read before Stage 1)

**Each component MUST be created with the `skill-creator` skill** (`Skill` tool →
`skill-creator:skill-creator`): paste Prompt N from
`docs/agent-systems/bug-hunter-build-guide.md`, build, **run the brief's three test prompts**,
fix, then next — in order. Prompt 11b **re-opens** `orchestrator` (seam extension —
re-run Prompt 7's tests after). If skill-creator is unavailable, **STOP and report**.

## Stories Included (build in this order)

1. **001-severity-scoring** (Prompt 8, Must)
2. **002-tool-ingest** (Prompt 9, Must)
3. **003-bug-verifier** (Prompt 10, Must) — sandbox-vs-commit check; flaky double-run
4. **004-git-revision-tracking** (Prompt 11, Must)
5. **005-orchestrator-verify-wiring** (Prompt 11b, Must — EXTENSION)

## Bolt Type

**Type**: Simple Construction Bolt (tooling)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Read stories + briefs + unit brief. **Sandbox prerequisite
      (requirements D4)**: confirm with the owner the sandbox recipe adapted from the
      repo's compose assets (API + Postgres + `dotnet test`); NFR-3 rules (network
      lockdown, caps, no production data)
- [ ] **2. implement**: Build via skill-creator in order; extension last
- [ ] **3. test**: All briefs' test prompts green incl. Prompt 7 re-run; a run now
      carries per-finding confidence + risk; stale-recipe path produces "could not
      verify in sandbox"

## Dependencies

### Requires
- 086-phase-1-skeleton-agents (orchestrator to extend; Phase 1 components)
- External: sandbox recipe provided/approved by owner (D4)

### Enables
- 088-phase-3-map-and-reachability (14b extends scoring); 093 (proving tests reused)

## Success Criteria

- [ ] 4 new skills + 1 extension, all via skill-creator, all test prompts passing
- [ ] Verify slot live: disprove-first; dynamic confirmation; commit-match guard;
      flaky-test double-run; Low findings still reported (appendix)
- [ ] Blanket "unverified" label replaced by per-finding confidence; SHA at open,
      reconciliation proposals at close

## Notes

**Time-box: 6h** (largest single bolt — the Verifier + sandbox bring real environment
work). Spec of record: guide Part II Phase 2.
