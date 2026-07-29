---
intent: 033-environment-triad
phase: inception
status: units-decomposed
updated: 2026-06-05T12:20:00Z
---

# Environment Triad - Unit Decomposition

## Units Overview

Decomposes into **3 units** — all configuration / infrastructure-readiness / documentation work (no domain model), so all use `simple-construction-bolt`. Order: define the tier + config separation first, then the secrets + seeding policy that layer onto it, then the promotion runbook that ties everything together. Bolts: 073 → 074 → 075. **All three are readiness-only — none performs a deployment.**

### Unit 1: 001-config-tiers-and-compose
**Description**: Introduce the third named environment (deployable dev tier) with layered `appsettings.{tier}.json`, a `docker-compose.dev-env.yml`, a per-tier config map, and boot-time validation parity with prod. Local + prod configs left behaviourally unchanged.
**Stories**: 001-define-dev-env-tier, 002-dev-env-compose-file, 003-three-tier-config-map, 004-boot-validation-parity
**Deliverables**: `appsettings.{DevEnv}.json`; `docker-compose.dev-env.yml`; `docs/environments/config-map.md`; `ValidateOnStart` covering the dev-env tier.
**Dependencies**: Depends on None (builds from existing assets) · Depended by Units 2 and 3.
**Estimated Complexity**: M
**Unit Type**: frontend
**Default Bolt Type**: simple-construction-bolt

### Unit 2: 002-secrets-and-seeding
**Description**: The per-environment secrets strategy (matrix + `.env.dev-env.example`) and the per-environment seeding policy + selection mechanism (reusing existing seed classes, with a Production guard on demo data).
**Stories**: 001-secrets-tier-matrix, 002-dev-env-secrets-template, 003-seeding-policy-and-selector, 004-prod-demo-data-guard
**Deliverables**: `docs/environments/secrets-matrix.md`; `.env.dev-env.example`; seeding-policy doc + per-`ASPNETCORE_ENVIRONMENT` seed selection; Production demo-data guard.
**Dependencies**: Depends on Unit 1 (the tier must exist) · Depended by Unit 3.
**Estimated Complexity**: M
**Unit Type**: frontend
**Default Bolt Type**: simple-construction-bolt

### Unit 3: 003-promotion-readiness
**Description**: The dev→prod promotion runbook as readiness documentation — tying together config (Unit 1), secrets (Unit 2), seeding (Unit 2), the existing `deploy.yml` image-tag flow, and the migration caveat. Explicitly defers execution to Phase 6.
**Stories**: 001-promotion-path-runbook, 002-deployment-deferral-note
**Deliverables**: `docs/environments/promotion-path.md`; an explicit Phase-6 deferral note cross-linked from DEPLOYMENT.md.
**Dependencies**: Depends on Units 1 + 2 (references their outputs) · Depended by None.
**Estimated Complexity**: S
**Unit Type**: frontend
**Default Bolt Type**: simple-construction-bolt

## Requirement-to-Unit Mapping

- **FR-1** (define dev-env tier — config) → `001-config-tiers-and-compose`
- **FR-2** (config separation + dev-env compose) → `001-config-tiers-and-compose`
- **FR-3** (per-environment secrets strategy) → `002-secrets-and-seeding`
- **FR-4** (per-environment seeding policy) → `002-secrets-and-seeding`
- **FR-5** (dev→prod promotion runbook) → `003-promotion-readiness`

## Unit Dependency Graph

```text
[existing compose / appsettings / deploy.yml assets]
            │
            ▼
[001-config-tiers-and-compose] ──> [002-secrets-and-seeding] ──> [003-promotion-readiness]
```

## Execution Order

1. Unit 1 — define the dev-env tier + config separation + compose (the foundation).
2. Unit 2 — secrets matrix + seeding policy layered onto the tier.
3. Unit 3 — promotion readiness runbook tying it together (readiness only; no deploy).
