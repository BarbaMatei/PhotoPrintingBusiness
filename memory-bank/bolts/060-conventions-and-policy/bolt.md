---
id: 060-conventions-and-policy
unit: 002-conventions-and-policy
intent: 027-architectural-layering
type: simple-construction-bolt
status: planned
stories:
  - 001-abstractions-subfolders
  - 002-no-repository-policy-and-analyzer
created: 2026-06-05T09:30:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [059-layering-foundation]
enables_bolts: [061-handler-pattern]
requires_units: [001-layering-foundation]
blocks: false

complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 1
---

# Bolt: 060-conventions-and-policy

## Overview

Introduce the `Abstractions/` subfolder convention (P23) and lock the no-repository posture with a policy doc + IQueryable analyzer (P24).

## Objective

Resolve the interface↔implementation interleaving and prevent future repository/over-abstraction drift.

## Stories Included

- **001-abstractions-subfolders**: Abstractions/ per feature (Should)
- **002-no-repository-policy-and-analyzer**: Policy doc + analyzer (Should)

## Bolt Type

**Type**: simple-construction-bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Pending → implementation-plan.md
- [ ] **2. implement**: Pending → Abstractions/ moves; data-access-conventions.md; analyzer config
- [ ] **3. test**: Pending → build/test green; analyzer passes (or fixes a real leak)

## Dependencies

### Requires
- 059-layering-foundation

### Enables
- 061-handler-pattern

## Success Criteria

- [ ] All I*.cs under Abstractions/; consumers reference the namespace
- [ ] data-access-conventions.md + analyzer; no IQueryable leaks
- [ ] No behaviour change

## Notes

After the layering foundation. Lockstep with bolt 062.
