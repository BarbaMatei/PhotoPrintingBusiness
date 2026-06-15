---
id: 079-research-tracks
unit: 001-research-tracks
intent: 034-eu-expansion-architecture-study
type: spike-bolt
status: planned
stories:
  - 004-t4-backend-localization
created: 2026-06-05T12:57:50Z
started: null
completed: null
current_stage: null
stages_completed: []
time_box: 4h

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

# Bolt: 079-research-tracks

## Overview

Spike for **T4 — Backend localization** (.NET resource-based).

## Objective

Produce `docs/analysis/eu-expansion/track-4-backend-localization.md`: localization for
ProblemDetails messages, Razor transactional emails, invoice PDFs, enum/display strings;
culture-resolution strategy; and the **deferred-culture trap** (culture stored on the
job/entity, not ambient at send time).

## Stories Included

- **004-t4-backend-localization**: T4 backend localization (.NET) (Must)

## Bolt Type

**Type**: Spike Bolt (research — knowledge out, no code)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/spike-bolt.md`

## Stages

- [ ] **1. explore**: Pending → .NET options + repo touchpoints (⛔ human checkpoint)
- [ ] **2. document**: Pending → `track-4-backend-localization.md` (⛔ human checkpoint)

## Dependencies

### Requires
- None (wave-parallel)

### Enables
- 083-synthesis-and-decision

## Success Criteria

- [ ] Covers messages, emails, invoices, enums
- [ ] Culture-resolution strategy recommended + justified
- [ ] Deferred-culture trap flagged unmissably, with codebase touchpoints

## Notes

**Time-box: 4h.** Light repo cross-check to ground recommendations (full sizing is T7).
