---
id: 080-research-tracks
unit: 001-research-tracks
intent: 034-eu-expansion-architecture-study
type: spike-bolt
status: planned
stories:
  - 005-t5-tax-invoicing-compliance
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
  avg_complexity: 3
  avg_uncertainty: 3
  max_dependencies: 3
  testing_scope: 1
---

# Bolt: 080-research-tracks

## Overview

Spike for **T5 — Tax, invoicing & compliance**. Highest verification-rigor track.

## Objective

Produce `docs/analysis/eu-expansion/track-5-tax-compliance.md`: EU OSS VAT (registration,
threshold, per-country rates, reporting) current to 2026; concrete `VatCalculator` (bolt 038)
impact for both tiers; per-country B2C e-invoicing mandates (vs e-Factura/bolt 039);
multi-currency invoicing; consumer-law checkout-copy deltas.

## Stories Included

- **005-t5-tax-invoicing-compliance**: T5 tax/invoicing/compliance (Must)

## Bolt Type

**Type**: Spike Bolt (research — knowledge out, no code)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/spike-bolt.md`

## Stages

- [ ] **1. explore**: Pending → OSS/e-invoicing/currency research + adversarial verification (⛔ human checkpoint)
- [ ] **2. document**: Pending → `track-5-tax-compliance.md` (⛔ human checkpoint)

## Dependencies

### Requires
- None (wave-parallel; related to T6 on settlement currency)

### Enables
- 083-synthesis-and-decision

## Success Criteria

- [ ] OSS current to 2026 (pre-OSS sources rejected)
- [ ] Every VAT/OSS/e-invoicing claim: official source + date + **adversarial-verification verdict**
- [ ] Concrete `VatCalculator` impact for both tiers; multi-currency invoicing covered

## Notes

**Time-box: 8h.** A **mandatory independent adversarial-verification agent** must
confirm/refute every high-stakes regulatory claim before it can enter the options paper.
