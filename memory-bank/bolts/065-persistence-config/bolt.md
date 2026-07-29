---
id: 065-persistence-config
unit: 003-persistence-config
intent: 029-decomposition-and-hardening
type: simple-construction-bolt
status: planned
stories:
  - 001-per-entity-configurations
created: 2026-06-05T09:30:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [059-layering-foundation]
enables_bolts: []
requires_units: [001-layering-foundation]
blocks: false

complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 1
---

# Bolt: 065-persistence-config

## Overview

Per-entity `IEntityTypeConfiguration<T>` files; shrink `OnModelCreating` to < 100 LOC (P15).

## Objective

Make persistence config reviewable per-entity with zero schema drift.

## Stories Included

- **001-per-entity-configurations**: 17 config files + ApplyConfigurationsFromAssembly (Could)

## Bolt Type

**Type**: simple-construction-bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Pending → implementation-plan.md
- [ ] **2. implement**: Pending → Infrastructure/Data/Configurations/*Configuration.cs
- [ ] **3. test**: Pending → Add-Migration NoOpVerify empty; CI green

## Dependencies

### Requires
- 059-layering-foundation (Infrastructure/Data placement)

### Enables
- None

## Success Criteria

- [ ] One config file per entity; OnModelCreating ≤ 100 LOC
- [ ] Empty Add-Migration diff

## Notes

Touches only Data/ — parallelisable with bolt 063.
