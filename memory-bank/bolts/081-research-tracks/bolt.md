---
id: 081-research-tracks
unit: 001-research-tracks
intent: 034-eu-expansion-architecture-study
type: spike-bolt
status: planned
stories:
  - 006-t6-payments-checkout
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
  max_dependencies: 3
  testing_scope: 1
---

# Bolt: 081-research-tracks

## Overview

Spike for **T6 — Payments & checkout** with multi-currency.

## Objective

Produce `docs/analysis/eu-expansion/track-6-payments.md`: Stripe EU local methods (iDEAL,
Bancontact, Przelewy24…) vs the existing Stripe integration (bolt 016); EuPlatesc
keep-RO-only vs retire; presentment-vs-settlement currency model and its order/invoice impact.

## Stories Included

- **006-t6-payments-checkout**: T6 payments & checkout (Must)

## Bolt Type

**Type**: Spike Bolt (research — knowledge out, no code)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/spike-bolt.md`

## Stages

- [ ] **1. explore**: Pending → Stripe/EuPlatesc/multi-currency research (⛔ human checkpoint)
- [ ] **2. document**: Pending → `track-6-payments.md` (⛔ human checkpoint)

## Dependencies

### Requires
- None (wave-parallel; related to T5 on settlement currency)

### Enables
- 083-synthesis-and-decision

## Success Criteria

- [ ] Stripe EU local-method coverage per tier, cited to Stripe docs
- [ ] EuPlatesc disposition stated with rationale
- [ ] Presentment-vs-settlement recommendation + order/invoice impact

## Notes

**Time-box: 4h.** Resolves the residual open question on settlement currency & FX handling;
coordinate conceptually with T5 (080).
