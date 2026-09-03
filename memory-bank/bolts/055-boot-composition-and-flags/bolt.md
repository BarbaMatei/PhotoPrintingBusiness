---
id: 055-boot-composition-and-flags
unit: 001-boot-composition-and-flags
intent: 026-observability-boot-manifest
type: simple-construction-bolt
status: planned
stories:
  - 001-program-subsystem-extensions
  - 002-typed-feature-gate
created: 2026-06-05T09:30:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: []
enables_bolts: [056-system-manifest-and-liveness, 058-observability-boot-manifest-ui]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 2
---

# Bolt: 055-boot-composition-and-flags

## Overview

Make the boot script reviewable (Program.cs subsystem extensions) and introduce a typed `IFeatureGate` registry that becomes the single source of truth for feature flags.

## Objective

Land P07 + P10 — the foundation that the system-info manifest (056) and admin UI (058) build on.

## Stories Included

- **001-program-subsystem-extensions**: Extract 5 Add* extensions (Should)
- **002-typed-feature-gate**: Typed IFeatureGate over a flag enum/registry (Should)

## Bolt Type

**Type**: simple-construction-bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Pending → implementation-plan.md
- [ ] **2. implement**: Pending → Extensions/*, FeatureFlags/*
- [ ] **3. test**: Pending → test-report (Enabled=false registers nothing; gate default-on-typo)

## Dependencies

### Requires
- None

### Enables
- 056-system-manifest-and-liveness (manifest reads IFeatureGate.GetAll())
- 058-observability-boot-manifest-ui

## Success Criteria

- [ ] Program.cs ≈ 120 LOC; ordering test green
- [ ] All flag reads migrated to IFeatureGate
- [ ] No behaviour change

## Notes

Internal order P07 → P10.
