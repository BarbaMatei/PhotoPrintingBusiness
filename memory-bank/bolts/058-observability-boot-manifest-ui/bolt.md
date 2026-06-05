---
id: 058-observability-boot-manifest-ui
unit: 004-observability-boot-manifest-ui
intent: 026-observability-boot-manifest
type: simple-construction-bolt
status: planned
stories:
  - 001-admin-system-info-tab
created: 2026-06-05T09:30:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [056-system-manifest-and-liveness]
enables_bolts: []
requires_units: [002-system-manifest-and-liveness]
blocks: false

complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 2
---

# Bolt: 058-observability-boot-manifest-ui

## Overview

Admin "System" tab rendering the system-info manifest (P04 UI).

## Objective

Give the maintainer a searchable view of hosted services, flags, routes, and CLI verbs.

## Stories Included

- **001-admin-system-info-tab**: Admin System tab (Should)

## Bolt Type

**Type**: simple-construction-bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Pending → implementation-plan.md
- [ ] **2. implement**: Pending → features/admin/pages/system/
- [ ] **3. test**: Pending → Vitest spec + route guard

## Dependencies

### Requires
- 056-system-manifest-and-liveness (manifest endpoint)

### Enables
- None

## Success Criteria

- [ ] System tab renders + searches the manifest
- [ ] Admin-only; lazy-loaded within bundle budget

## Notes

Depends on the backend manifest. Use BaseApiService if intent 030 P26 has landed.
