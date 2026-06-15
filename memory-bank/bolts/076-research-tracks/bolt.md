---
id: 076-research-tracks
unit: 001-research-tracks
intent: 034-eu-expansion-architecture-study
type: spike-bolt
status: planned
stories:
  - 001-t1-fulfillment-logistics
created: 2026-06-05T12:57:50Z
started: null
completed: null
current_stage: null
stages_completed: []
time_box: 8h

requires_bolts: []
enables_bolts: [083-synthesis-and-decision]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 3
  max_dependencies: 3
  testing_scope: 1
---

# Bolt: 076-research-tracks

## Overview

Spike for **T1 — Fulfillment & logistics**, the dominant research track. Validates the
RO-ship decision with real per-corridor parcel cost/time and finds where it breaks.

## Objective

Produce `docs/analysis/eu-expansion/track-1-fulfillment.md`: per-corridor (RO→DE/FR/IT/ES/PL/HU/BG)
cost+time numbers for both market tiers, Sameday coverage boundary, local-partner fallback
(costed + revisit threshold), and a competitive scan.

## Stories Included

- **001-t1-fulfillment-logistics**: T1 fulfillment & logistics (Must)

## Bolt Type

**Type**: Spike Bolt (research — knowledge out, no code)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/spike-bolt.md`

## Stages

- [ ] **1. explore**: Pending → research questions, sources, raw findings (⛔ human checkpoint)
- [ ] **2. document**: Pending → `track-1-fulfillment.md` (⛔ human checkpoint)

## Dependencies

### Requires
- None (wave-parallel with 077–082)

### Enables
- 083-synthesis-and-decision

## Success Criteria

- [ ] Actual numbers per corridor (both tiers), each cited + dated
- [ ] Sameday coverage boundary stated; partner fallback costed + threshold proposed
- [ ] Competitive scan included

## Notes

**Time-box: 8h.** Multi-agent fan-out (parallel researchers per corridor) + adversarial
verification of headline cost claims. The fulfillment *model* is decided (RO-ship) — this
bolt *validates*, it does not re-run a 3-way comparison.
