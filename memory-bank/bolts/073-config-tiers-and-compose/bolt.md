---
id: 073-config-tiers-and-compose
unit: 001-config-tiers-and-compose
intent: 033-environment-triad
type: simple-construction-bolt
status: planned
stories:
  - 001-define-dev-env-tier
  - 002-dev-env-compose-file
  - 003-three-tier-config-map
  - 004-boot-validation-parity
created: 2026-06-05T12:45:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: []
enables_bolts: [074-secrets-and-seeding]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 2
  max_dependencies: 1
  testing_scope: 2
---

# Bolt: 073-config-tiers-and-compose

## Overview

Define the missing third environment — the deployable dev sandbox — as a cleanly separated config tier: a named `ASPNETCORE_ENVIRONMENT` + layered `appsettings.{tier}.json` (Postgres, test-mode keys), a standalone `docker-compose.dev-env.yml`, a per-tier config map, and `ValidateOnStart` parity with prod. Local + prod tiers left behaviourally unchanged. **Infrastructure readiness only — validated locally, not deployed.**

## Objective

Give the project a real, prod-shaped-but-seedable dev tier that boots and validates locally, so a future deployment (Phase 6) has a defined sandbox to target — without performing any deployment now.

## Stories Included

- **001-define-dev-env-tier**: Named tier + layered appsettings (Must)
- **002-dev-env-compose-file**: `docker-compose.dev-env.yml` (Must)
- **003-three-tier-config-map**: Per-setting config map across tiers (Should)
- **004-boot-validation-parity**: `ValidateOnStart` parity for dev-env (Must)

## Bolt Type

**Type**: simple-construction-bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Pending → implementation-plan.md (tier name + config diff)
- [ ] **2. implement**: Pending → `appsettings.{tier}.json`; `docker-compose.dev-env.yml`; `docs/environments/config-map.md`; validator env coverage
- [ ] **3. test**: Pending → dev-env boots locally; `docker compose config` valid; prod config unchanged; loud-fail on missing secret

## Dependencies

### Requires
- None (builds from existing appsettings/compose assets)

### Enables
- 074-secrets-and-seeding

## Success Criteria

- [ ] Third tier boots locally with layered config (Postgres, test keys)
- [ ] `docker-compose.dev-env.yml` validates + boots locally; prod compose unchanged
- [ ] Config map documents every differing setting across tiers
- [ ] `ValidateOnStart` for dev-env fails loudly on missing required secret (no Development fallback)

## Notes

Foundation of the intent. Readiness only — no host provisioning (Phase 6). Hardest point: prevent silent fallback to Development defaults.

**NAMING RESOLUTION (owner, 2026-06-05) — applies to every story/artifact in this intent:**
the third tier is named **`Staging`** (ASP.NET built-in). Wherever stories/briefs say
`dev-env` / `{tier}` / `DevEnv`, read: `ASPNETCORE_ENVIRONMENT=Staging`,
`appsettings.Staging.json`, `docker-compose.staging.yml`, `.env.staging.example`.
Email for the Staging tier: **MailHog** (resolved Q2). "Dev environment" remains the
colloquial/roadmap name only — never use it in code, config, or filenames.
