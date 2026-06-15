---
id: 085-phase-1-skeleton-core
unit: 001-phase-1-skeleton
intent: 035-bug-hunter-agent-system
type: simple-construction-bolt
status: planned
stories:
  - 001-ledger-io
  - 002-bug-documentation
  - 003-deduplication
  - 004-report-rendering
  - 005-triage-intake
created: 2026-06-10T10:40:14Z
started: null
completed: null
current_stage: null
stages_completed: []
time_box: 5h

requires_bolts: []
enables_bolts: [086-phase-1-skeleton-agents]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 1
---

# Bolt: 085-phase-1-skeleton-core

## Overview

Tooling-only bolt. Builds the bug-hunting system's **foundation skills** — guide
Prompts 1–5 in master-build-order: `ledger-io`, `bug-documentation`, `deduplication`,
`report-rendering`, `triage-intake`. No application code is touched (the system is
read-only on source by design).

## ⚠️ Construction Method (owner mandate + guide Part I — read before Stage 1)

**Each skill MUST be created with the `skill-creator` skill** (`Skill` tool →
`skill-creator:skill-creator`): paste the story's brief (Prompt N) from
`docs/agent-systems/bug-hunter-build-guide-v3.6.md`, build, **run the brief's three test prompts**,
fix, and only then move to the next story — strictly in order 001 → 005. If
skill-creator is unavailable, **STOP this bolt and report** (intent FR-1).

## Stories Included (build in this order)

1. **001-ledger-io** (Prompt 1, Must) — concurrency-safe shared memory; everything
   else depends on it
2. **002-bug-documentation** (Prompt 2, Must) — the canonical record
3. **003-deduplication** (Prompt 3, Must) — needs ledger-io
4. **004-report-rendering** (Prompt 4, Must) — needs bug-documentation
5. **005-triage-intake** (Prompt 5, Must) — needs ledger-io

## Bolt Type

**Type**: Simple Construction Bolt (tooling — output is Claude Code skills)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Read the 5 stories + their briefs in the guide + the unit brief
      (pinned paths: `bug-hunting/` outputs — requirements D3)
- [ ] **2. implement**: Build the five skills **via skill-creator**, in order, running
      each brief's three test prompts before moving on
- [ ] **3. test**: Re-verify all 15 test prompts green; confirm writes stay inside
      `bug-hunting/` + `.claude/skills/`

## Dependencies

### Requires
- (none — first bolt of the system)

### Enables
- 086-phase-1-skeleton-agents (general-hunter + orchestrator consume all five)

## Success Criteria

- [ ] 5 skills under `.claude/skills/{ledger-io,bug-documentation,deduplication,report-rendering,triage-intake}/`
- [ ] Construction log records a skill-creator invocation + 3 passing test prompts per skill
- [ ] Ledger at `bug-hunting/bug-ledger.json` (+ `.md` mirror); reports targeting `bug-hunting/reports/`
- [ ] v3 specifics verified: single-writer merge + atomic IDs + `correlation_id`; contract-sourced `expected_behavior` (or "intent-unconfirmed" tag); reporting floor; reason-required dismissals

## Notes

**Time-box: 5h.** Spec of record: `docs/agent-systems/bug-hunter-build-guide-v3.6.md` Part II Phase 1.
Unit brief: `memory-bank/intents/035-bug-hunter-agent-system/units/001-phase-1-skeleton/unit-brief.md`.
