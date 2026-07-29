---
id: 074-secrets-and-seeding
unit: 002-secrets-and-seeding
intent: 033-environment-triad
type: simple-construction-bolt
status: planned
stories:
  - 001-secrets-tier-matrix
  - 002-dev-env-secrets-template
  - 003-seeding-policy-and-selector
  - 004-prod-demo-data-guard
created: 2026-06-05T12:45:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [073-config-tiers-and-compose]
enables_bolts: [075-promotion-readiness]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 2
---

# Bolt: 074-secrets-and-seeding

## Overview

Layer the per-environment secrets strategy (matrix + `.env.dev-env.example`, test-mode keys) and the per-environment seeding policy (selection by environment, reusing the existing seed classes, with a Production guard on demo data) onto the dev-env tier from bolt 073. **Readiness only — no real secrets provisioned, no host stood up.**

## Objective

Make it unambiguous which secrets and which seed data each tier needs, and make demo data structurally impossible in Production.

## Stories Included

- **001-secrets-tier-matrix**: Secrets × tier matrix, test vs live (Must)
- **002-dev-env-secrets-template**: `.env.dev-env.example` (Must)
- **003-seeding-policy-and-selector**: Per-environment seed policy + selector (Should)
- **004-prod-demo-data-guard**: Refuse demo data in Production (Should)

## Bolt Type

**Type**: simple-construction-bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Pending → implementation-plan.md (secret list + seed policy)
- [ ] **2. implement**: Pending → `docs/environments/secrets-matrix.md`; `.env.dev-env.example`; seeding-policy doc + env-aware selector; Production demo-data guard
- [ ] **3. test**: Pending → no real secrets (scanning green); seed selection idempotent; guard refuses demo in prod (test asserts it)

## Dependencies

### Requires
- **073-config-tiers-and-compose** (Required): the dev-env tier must exist to scope secrets + seeding

### Enables
- 075-promotion-readiness

## Success Criteria

- [ ] Secrets matrix covers every secret × tier (test vs live, storage location)
- [ ] `.env.dev-env.example` committed with placeholders; scanning stays green
- [ ] Seed selection applies the correct set per environment; idempotent; reuses existing seeders
- [ ] `DevDataSeed` refuses to run in Production (asserted by a test)

## Notes

Reuses `ProductCatalogSeed` / `DevDataSeed`; no parallel seeder. Honours ADR-006 + intent-018 secret scanning. The Production demo-data guard is the highest-value safety net here.

**NAMING RESOLUTION (owner, 2026-06-05):** the third tier is named **`Staging`**. Wherever
stories/briefs say `dev-env` / `{tier}`, read `ASPNETCORE_ENVIRONMENT=Staging`; the secrets
template file is **`.env.staging.example`** (not `.env.dev-env.example`). Staging email =
MailHog. See bolt 073 Notes for the full mapping.
