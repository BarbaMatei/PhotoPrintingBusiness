---
id: 077-research-tracks
unit: 001-research-tracks
intent: 034-eu-expansion-architecture-study
type: spike-bolt
status: planned
stories:
  - 002-t2-site-url-architecture
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

# Bolt: 077-research-tracks

## Overview

Spike for **T2 — Site & URL architecture** under a single EU-wide brand.

## Objective

Produce `docs/analysis/eu-expansion/track-2-site-architecture.md`: multi-locale single site
vs subdomains vs path prefixes (ccTLD-per-country = rejected-by-default), SEO consequences,
content/legal management per jurisdiction, and each option's env-triad multiplier
(referencing intent 033).

## Stories Included

- **002-t2-site-url-architecture**: T2 site & URL architecture (Must)

## Bolt Type

**Type**: Spike Bolt (research — knowledge out, no code)
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/spike-bolt.md`

## Stages

- [ ] **1. explore**: Pending → options, SEO/topology findings (⛔ human checkpoint)
- [ ] **2. document**: Pending → `track-2-site-architecture.md` (⛔ human checkpoint)

## Dependencies

### Requires
- None (wave-parallel)

### Enables
- 083-synthesis-and-decision

## Success Criteria

- [ ] Each option states env-multiplier (vs intent 033) + SEO trade-offs
- [ ] ccTLD-per-country documented as rejected-by-default with reasons
- [ ] Interaction with T3 i18n build strategy noted

## Notes

**Time-box: 6h.** Brand is fixed (one EU-wide); evaluate options under that constraint.
Must reference `memory-bank/intents/033-environment-triad/`.
