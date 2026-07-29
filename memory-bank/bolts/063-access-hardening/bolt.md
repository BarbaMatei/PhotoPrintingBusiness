---
id: 063-access-hardening
unit: 001-access-hardening
intent: 029-decomposition-and-hardening
type: simple-construction-bolt
status: planned
stories:
  - 001-global-rate-limit
  - 002-admin-policy-constant
created: 2026-06-05T09:30:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [054-dependency-and-boot-hardening]
enables_bolts: []
requires_units: [001-dependency-and-boot-hardening]
blocks: false

complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 2
---

# Bolt: 063-access-hardening

## Overview

Global per-IP rate limit + centralised `Policies.Admin` constant (P08).

## Objective

Bound abuse of the non-auth API surface and remove the string-literal admin-role footgun.

## Stories Included

- **001-global-rate-limit**: Global per-IP sliding-window limiter (Should)
- **002-admin-policy-constant**: Policies.Admin constant + migrate 6 controllers to it (attribute swap — NO EF schema migration) (Should)

## Bolt Type

**Type**: simple-construction-bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Pending → implementation-plan.md
- [ ] **2. implement**: Pending → SecurityExtensions limiter; Policies class; 6 controllers migrated
- [ ] **3. test**: Pending → 401 (anon admin), 429 (over-limit) integration tests

## Dependencies

### Requires
- 054-dependency-and-boot-hardening (P05 real client IP)

### Enables
- None

## Success Criteria

- [ ] Global limiter active; auth policies still stricter
- [ ] No `Roles="Admin"` literal; anonymous admin → 401
- [ ] Limit tuned so legitimate bursts pass

## Notes

Independent of the decompositions; ship first within intent 029. Soft pre-launch must-have.
