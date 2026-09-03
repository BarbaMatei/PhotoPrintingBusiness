---
id: 078-research-tracks
unit: 001-research-tracks
intent: 034-eu-expansion-architecture-study
type: spike-bolt
status: planned
stories:
  - 003-t3-frontend-i18n
created: 2026-06-05T12:57:50Z
started: null
completed: null
current_stage: null
stages_completed: []
time_box: 6h

requires_bolts: []
enables_bolts: [083-synthesis-and-decision]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 2
  max_dependencies: 2
  testing_scope: 1
---

# Bolt: 078-research-tracks

## Overview

Spike for **T3 — Frontend internationalization** for Angular 21 specifically.

## Objective

Produce `docs/analysis/eu-expansion/track-3-frontend-i18n.md`: Angular 21 built-in i18n
(compile-time) vs runtime libraries (Transloco & peers) — maturity, bundle impact, workflow;
interaction with each T2 option; multi-currency/number/date formatting; RTL not required.

## Stories Included

- **003-t3-frontend-i18n**: T3 frontend i18n (Angular 21) (Must)

## Bolt Type

**Type**: Spike Bolt (research — knowledge out, no code)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/spike-bolt.md`

## Stages

- [ ] **1. explore**: Pending → options + optional throwaway build experiment (⛔ human checkpoint)
- [ ] **2. document**: Pending → `track-3-frontend-i18n.md` (⛔ human checkpoint)

## Dependencies

### Requires
- None (wave-parallel; references T2 conceptually)

### Enables
- 083-synthesis-and-decision

## Success Criteria

- [ ] Angular 21-specific comparison (reject claims about old versions)
- [ ] Interaction with each T2 option stated
- [ ] Any throwaway experiment archived/deleted, never merged

## Notes

**Time-box: 6h** (includes a ~20-line throwaway Angular 21 i18n build experiment if needed,
then archived/deleted).
