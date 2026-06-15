---
id: 094-optional-integration
unit: 006-optional-integration
intent: 035-bug-hunter-agent-system
type: simple-construction-bolt
status: planned
stories:
  - 001-report-rendering-sarif-ext
  - 002-issue-sync
  - 003-ci-gate
created: 2026-06-10T10:40:14Z
started: null
completed: null
current_stage: null
stages_completed: []
time_box: 3h

requires_bolts: [092-phase-4-learn-and-measure]
enables_bolts: []
requires_units: [001-phase-1-skeleton, 002-phase-2-trust, 004-phase-4-learn-and-measure]
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 1
---

# Bolt: 094-optional-integration

## Overview

Tooling-only bolt, **parked until owner adoption** (guide: "build only if you adopt
CI or an issue tracker"). Guide Prompts A–C: the SARIF twin, idempotent issue-sync,
and the baseline-aware CI gate. All three stories are **Could** priority.

## ⏸ Adoption gate (owner decision)

Do not schedule this bolt in a wave until the owner decides to (a) upload findings to
CI/code-scanning (→ A + C; GitHub Actions already exists, bolt 040), and/or (b) track
bugs in an issue tracker (→ B; GitHub Issues via `gh` is the zero-cost default).
Partial builds are fine — the three stories are independent of each other.

## ⚠️ Construction Method (owner mandate + guide Part I — read before Stage 1)

**Each component MUST be created with the `skill-creator` skill** (`Skill` tool →
`skill-creator:skill-creator`): paste the brief from
`docs/agent-systems/bug-hunter-build-guide.md`, build, **run its test prompts**, fix, then next.
Brief A **re-opens** `report-rendering` (re-run Prompt 4's tests after). If
skill-creator is unavailable, **STOP and report**.

## Stories Included

1. **001-report-rendering-sarif-ext** (Optional A, Could — EXTENSION)
2. **002-issue-sync** (Optional B, Could)
3. **003-ci-gate** (Optional C, Could)

## Bolt Type

**Type**: Simple Construction Bolt (tooling)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Confirm the adoption decision + which subset builds; read stories
      + briefs + unit brief
- [ ] **2. implement**: Build the chosen subset via skill-creator
- [ ] **3. test**: Briefs' test prompts green (incl. Prompt 4 re-run if A built);
      SARIF/Markdown parity; tickets idempotent; gate fails only NEW Critical/High vs
      baseline

## Dependencies

### Requires
- 092-phase-4-learn-and-measure (bug-lifecycle for issue-sync); report-rendering
  (085) + severity-scoring (087) already in place

### Enables
- (terminal tier)

## Success Criteria

- [ ] Built subset created via skill-creator, all test prompts passing
- [ ] No behavior change for the non-adopted parts (Markdown reports unchanged if A
      skipped, etc.)

## Notes

**Time-box: 3h.** Spec of record: guide "Optional — Integration".
