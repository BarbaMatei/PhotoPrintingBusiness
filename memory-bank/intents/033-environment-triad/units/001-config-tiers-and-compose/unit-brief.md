---
unit: 001-config-tiers-and-compose
intent: 033-environment-triad
phase: inception
status: stories-defined
unit_type: frontend
default_bolt_type: simple-construction-bolt
created: 2026-06-05T12:20:00Z
updated: 2026-06-05T12:30:00Z
---

# Unit Brief: Config Tiers & Compose

## Purpose

Introduce the missing third environment — the deployable dev sandbox — as a cleanly separated configuration tier with its own layered `appsettings`, a `docker-compose.dev-env.yml`, a per-tier config map, and boot-validation parity with production. Leave the local and production tiers behaviourally unchanged. **Readiness only — the tier is defined and validated locally; standing it up on a host is Phase 6.**

## Scope

### In Scope
- A third `ASPNETCORE_ENVIRONMENT` value + `appsettings.{tier}.json` (Postgres-backed, test-mode keys, dev-env CORS/rate posture).
- `docker-compose.dev-env.yml` (Postgres + API + dev-env email), parameterised by a dev-env `.env`.
- `docs/environments/config-map.md`: per-setting value across local / dev-env / prod.
- `ValidateOnStart` coverage so the dev-env tier fails loudly on missing required secrets (no silent Development fallback).

### Out of Scope
- The secrets matrix + seeding policy (unit 002).
- The promotion runbook (unit 003).
- Standing the tier up on a real host (Phase 6 — explicitly deferred).
- Any change to `docker-compose.yml` / `docker-compose.prod.yml` behaviour.

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-1 | Define the deployable-dev environment tier (config) | Must |
| FR-2 | Per-environment config separation + dev-env compose | Must |

---

## Domain Concepts

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| Select tier | Resolve config by `ASPNETCORE_ENVIRONMENT` | env var | layered config |
| Boot-validate | Enforce required settings per tier | config + secrets | pass / loud fail |
| Compose dev-env | Express the dev-env tier as compose | dev-env `.env` | validated compose |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 4 |
| Must Have | 3 |
| Should Have | 1 |
| Could Have | 0 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-define-dev-env-tier | Define the dev-env `ASPNETCORE_ENVIRONMENT` + appsettings | Must | Planned |
| 002-dev-env-compose-file | `docker-compose.dev-env.yml` | Must | Planned |
| 003-three-tier-config-map | Per-setting config map across tiers | Should | Planned |
| 004-boot-validation-parity | `ValidateOnStart` parity for dev-env | Must | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| None | Builds directly from existing compose/appsettings assets |

### Depended By
| Unit | Reason |
|------|--------|
| 002-secrets-and-seeding | Secrets + seeding layer onto the defined tier |
| 003-promotion-readiness | Runbook references the config map |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| Existing appsettings/compose | Extended, not rewritten | Low (regression risk on prod — guarded) |

---

## Technical Context

### Suggested Technology
ASP.NET Core layered configuration (`appsettings.{Environment}.json`), Options + `ValidateOnStart`, docker-compose.

### Integration Points
| Integration | Type | Protocol |
|-------------|------|----------|
| `appsettings.{tier}.json` | config layer | ASP.NET Core |
| `docker-compose.dev-env.yml` | infra | compose |

---

## Constraints

- Dev-env is Postgres-backed (prod-shaped); only local stays SQLite.
- Prod compose/Caddy behaviour unchanged.
- Readiness only — no host provisioning.
- Secrets never in config files (ADR-006); the appsettings layer carries non-secret defaults only.

---

## Success Criteria

### Functional
- [ ] A third tier boots locally with its own layered config.
- [ ] `docker-compose.dev-env.yml` validates via `docker compose config` + a local boot.
- [ ] Config map documents every setting that differs across the three tiers.

### Non-Functional
- [ ] `ValidateOnStart` for dev-env behaves like prod (loud fail on missing secret).
- [ ] Prod `docker compose config` output unchanged.

### Quality
- [ ] No tier inherits another's secrets/hostnames.

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 073-config-tiers-and-compose | simple | 001–004 | Define dev-env tier + compose + config map |

---

## Notes

Foundation of the intent. The hardest correctness point is ensuring the dev-env tier does NOT silently fall back to Development defaults — hence the dedicated boot-validation-parity story.
