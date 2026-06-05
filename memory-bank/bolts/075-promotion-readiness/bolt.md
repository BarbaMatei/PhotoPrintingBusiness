---
id: 075-promotion-readiness
unit: 003-promotion-readiness
intent: 033-environment-triad
type: simple-construction-bolt
status: planned
stories:
  - 001-promotion-path-runbook
  - 002-deployment-deferral-note
created: 2026-06-05T12:45:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [073-config-tiers-and-compose, 074-secrets-and-seeding]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 1
---

# Bolt: 075-promotion-readiness

## Overview

Tie the triad together with a dev→prod promotion runbook written as **readiness documentation** (config swap, secret swap test→live, the existing `deploy.yml` image-tag flow, migration apply, seed policy, smoke verification) and an explicit Phase-6 deployment-deferral note cross-linked from DEPLOYMENT.md. This bolt documents *how a future promotion would go*; it performs none of it.

## Objective

Make a future deployment safe and repeatable while keeping deployment firmly out of the present scope — explicitly countering the "deploy next" default that ai-workflow-review §6 warns against.

## Stories Included

- **001-promotion-path-runbook**: dev→prod promotion runbook (Should)
- **002-deployment-deferral-note**: Explicit Phase-6 deferral note (Should)

## Bolt Type

**Type**: simple-construction-bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Pending → implementation-plan.md (step sequence + cross-links)
- [ ] **2. implement**: Pending → `docs/environments/promotion-path.md`; deferral note cross-linked from DEPLOYMENT.md
- [ ] **3. test**: Pending → runbook cross-references units 073/074 + deploy.yml + DEPLOYMENT.md §7; no "deploy now" language; deferral note present

## Dependencies

### Requires
- **073-config-tiers-and-compose** (Required): references the config map
- **074-secrets-and-seeding** (Required): references the secrets matrix + seeding policy

### Enables
- None (terminal bolt of the intent / Phase-4 readiness complete)

## Success Criteria

- [ ] Promotion runbook sequences repeatable readiness steps, cross-referencing 073/074 + deploy.yml
- [ ] Migration-provider caveat recorded as a precondition (links DEPLOYMENT.md §7)
- [ ] Phase-6 deferral note present, cross-linked from DEPLOYMENT.md, counters the "deploy next" default
- [ ] No deployment-pressure language anywhere

## Notes

Smallest bolt; documentation only. Must not invoke `deploy.yml`, provision a host, or modify any pipeline.

**RESOLVED DECISIONS (owner, 2026-06-05):** runbook home is **`docs/environments/`**
(Q4 — cross-link from DEPLOYMENT.md). The third tier is named **`Staging`** — the runbook
documents the **Staging → Production** promotion path; use "Staging" in all docs, with
"dev environment" only as a parenthetical colloquial alias. See bolt 073 Notes for the
full naming mapping.
