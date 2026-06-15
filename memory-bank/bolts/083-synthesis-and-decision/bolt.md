---
id: 083-synthesis-and-decision
unit: 002-synthesis-and-decision
intent: 034-eu-expansion-architecture-study
type: spike-bolt
status: planned
stories:
  - 001-synthesis-options-paper
  - 002-owner-decision-adr
created: 2026-06-05T12:57:50Z
started: null
completed: null
current_stage: null
stages_completed: []
time_box: 6h

requires_bolts: [076-research-tracks, 077-research-tracks, 078-research-tracks, 079-research-tracks, 080-research-tracks, 081-research-tracks, 082-research-tracks]
enables_bolts: [084-implementation-briefs]
requires_units: [001-research-tracks]
blocks: false

complexity:
  avg_complexity: 3
  avg_uncertainty: 2
  max_dependencies: 2
  testing_scope: 1
---

# Bolt: 083-synthesis-and-decision

## Overview

Spike that converts the seven findings into a decision: options paper (D2) → ⛔ owner
decision → ADR (D3).

## Objective

Stage 1 (explore): synthesize track findings into 2–3 coherent, costed bundles and draft the
options paper. Stage 2 (document): finalize `docs/analysis/eu-expansion-architecture-study.md`,
run the ⛔ owner-decision checkpoint, and record the ADR in `memory-bank/standards/decision-index.md`.

## Stories Included

- **001-synthesis-options-paper**: Synthesize findings → options paper D2 (Must)
- **002-owner-decision-adr**: ⛔ Owner decision → ADR D3 (Must)

## Bolt Type

**Type**: Spike Bolt (knowledge out, no code)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/spike-bolt.md`

## Stages

- [ ] **1. explore**: Pending → synthesize bundles + draft options paper (⛔ human checkpoint: owner reviews)
- [ ] **2. document**: Pending → finalize D2 → ⛔ owner decision → ADR D3 (⛔ human checkpoint)

## Dependencies

### Requires
- All 7 research-track bolts (076–082) complete

### Enables
- 084-implementation-briefs

## Success Criteria

- [ ] 2–3 coherent costed bundles; recommendation separated from "owner must decide" list
- [ ] One bundle stress-tests the partner-fallback sensitivity
- [ ] ADR recorded **only** after explicit owner decision, with rejected options + reasons

## Notes

**Time-box: 6h** for the synthesis/authoring work; the ⛔ owner decision is a separate human
step (budget for a round of owner Q&A). Reference intent 033's env triad when costing
deployment topology. Bundles vary on the site-architecture + i18n axes (fulfillment is fixed
to RO-ship).
