---
id: 059-layering-foundation
unit: 001-layering-foundation
intent: 027-architectural-layering
type: simple-construction-bolt
status: planned
stories:
  - 001-no-split-adr
  - 002-domain-layer-extraction
  - 003-infrastructure-layer
  - 004-web-layer
  - 005-application-feature-promotion
created: 2026-06-05T09:30:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: []
enables_bolts: [060-conventions-and-policy, 061-handler-pattern]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 2
  max_dependencies: 2
  testing_scope: 2
---

# Bolt: 059-layering-foundation

## Overview

The big structural move: ADR + four layering PRs that establish Domain / Infrastructure / Web / Application inside one assembly (folds first-pass P06 + P16). Zero behaviour change, zero migration drift.

## Objective

Resolve the maintainer's core layer-separation complaint by reshaping the folder/namespace structure — sequenced so each PR is independently mergeable.

## Stories Included

- **001-no-split-adr**: No-four-project ADR (Could)
- **002-domain-layer-extraction**: Domain/ (P16) (Could)
- **003-infrastructure-layer**: Infrastructure/ (Should)
- **004-web-layer**: Web/ (Should)
- **005-application-feature-promotion**: Application/<Feature>/ (P06) (Should)

## Bolt Type

**Type**: simple-construction-bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Pending → per-PR namespace find/replace scripts
- [ ] **2. implement**: Pending → PR1 ADR → PR2 Domain → PR3 Infrastructure → PR4 Web → PR5 Application
- [ ] **3. test**: Pending → build/test green + empty Add-Migration after EACH PR

## Dependencies

### Requires
- None

### Enables
- 060-conventions-and-policy
- 061-handler-pattern

## Success Criteria

- [ ] Four layers by folder+namespace; four controllers no longer inject DbContext
- [ ] Zero behaviour change; zero migration drift after every PR
- [ ] Layer rules codified

## Notes

Highest churn (~200 files). Lockstep with bolt 062 (test infrastructure). Schedule a quiet window.
