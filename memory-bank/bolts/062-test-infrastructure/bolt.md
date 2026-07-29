---
id: 062-test-infrastructure
unit: 001-test-infrastructure
intent: 028-test-architecture
type: simple-construction-bolt
status: planned
stories:
  - 001-timeprovider-adoption
  - 002-shared-test-application-factory
  - 003-test-builders
  - 004-reclassify-misnamed-unit-tests
created: 2026-06-05T09:30:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: []
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 2
---

# Bolt: 062-test-infrastructure

## Overview

Test-architecture refactor: adopt TimeProvider (P28), promote a shared factory base + Builders, and reclassify the misnamed unit tests (P27). Companion to the intent-027 structural refactor.

## Objective

Make the suite deterministic and honest, and make the intent-027 PRs reviewable by interleaving with this work.

## Stories Included

- **001-timeprovider-adoption**: TimeProvider across older services (Should)
- **002-shared-test-application-factory**: Shared WAF base (Should)
- **003-test-builders**: Fluent Builders (Should)
- **004-reclassify-misnamed-unit-tests**: Move DbContext tests to Integration (Should)

## Bolt Type

**Type**: simple-construction-bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Pending → implementation-plan.md
- [ ] **2. implement**: Pending → TimeProvider, _Base/factory, Builders/, Integration/ServiceLevel
- [ ] **3. test**: Pending → full suite green; CI filters updated

## Dependencies

### Requires
- None (but interleaved with bolts 059–061)

### Enables
- None

## Success Criteria

- [ ] Zero raw UtcNow in Application/Infrastructure; FakeTimeProvider scenarios added
- [ ] 11 factories inherit shared base; Builders cover 6 entities
- [ ] Misnamed tests reclassified; suite green

## Notes

Internal order P28 → P27. **Lockstep / interleaved with intent 027 (bolts 059–061).**
