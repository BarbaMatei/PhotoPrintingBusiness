---
id: 072-regression-methodology
unit: 003-regression-methodology
intent: 032-regression-and-e2e-stabilization
type: simple-construction-bolt
status: planned
stories:
  - 001-regression-checklist
  - 002-execute-regression-baseline
  - 003-triage-findings-to-backlog
created: 2026-06-05T11:45:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [071-e2e-journey-coverage]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 1
  avg_uncertainty: 2
  max_dependencies: 2
  testing_scope: 3
---

# Bolt: 072-regression-methodology

## Overview

The repeatable regression methodology: a checklist mapped to every shipped intent (tagged automated-by-e2e / automated-by-integration / manual), one executed and dated baseline pass, and triage of every finding into the backlog (new bolt / existing bolt / KNOWN_FAILURES.md). Consumes bolt 071's e2e coverage so the automated tags are accurate.

## Objective

Make Phase-3 stabilization provable and repeatable: a dated go/known-issues verdict plus durable scaffolding a future wave can re-run cheaply.

## Stories Included

- **001-regression-checklist**: Checklist mapped to shipped intents (Should)
- **002-execute-regression-baseline**: One executed + recorded baseline (Must)
- **003-triage-findings-to-backlog**: Route findings to backlog/KNOWN_FAILURES (Should)

## Bolt Type

**Type**: simple-construction-bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Pending → implementation-plan.md (intent→check mapping)
- [ ] **2. implement**: Pending → `docs/testing/regression-checklist.md`; dated baseline; triage entries
- [ ] **3. test**: Pending → checklist covers every shipped intent; pass recorded; all findings routed

## Dependencies

### Requires
- **071-e2e-journey-coverage** (Required): so checks are tagged automated-by-e2e accurately
- **057-architecture-and-standards-docs** (Soft): KNOWN_FAILURES.md home for accepted known-issues

### Enables
- None (terminal bolt of the intent / Phase-3 gate)

## Success Criteria

- [ ] Checklist covers every shipped intent with a tagged check each
- [ ] One full pass executed + recorded (date, SHA, per-check result)
- [ ] Every failure/known-issue routed to a backlog item or KNOWN_FAILURES entry
- [ ] Cross-references e2e/integration automation so the manual surface is explicit

## Notes

Terminal Phase-3 bolt. Best run after 071 so "automated-by-e2e" tags are real. Soft-depends on bolt 057 for KNOWN_FAILURES.md; if 057 has not shipped, stage the entries and note the dependency.
