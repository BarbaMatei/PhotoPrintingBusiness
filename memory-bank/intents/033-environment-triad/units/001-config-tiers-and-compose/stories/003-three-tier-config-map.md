---
id: 003-three-tier-config-map
unit: 001-config-tiers-and-compose
intent: 033-environment-triad
status: draft
priority: should
created: 2026-06-05T12:30:00Z
assigned_bolt: 073-config-tiers-and-compose
implemented: false
---

# Story: 003-three-tier-config-map

## User Story

**As a** maintainer
**I want** a single config map showing every setting that varies across the three tiers
**So that** "what differs between local, dev-env, and prod" has one authoritative answer

## Acceptance Criteria

- [ ] **Given** the three tiers, **When** the map is written, **Then** it lists each setting that varies (DatabaseProvider, connection string shape, CORS origins, rate-limit posture, email provider, payment test-vs-live, Storage provider, observability/ANAF/Sameday enablement) with its value per tier
- [ ] **Given** the map, **When** stored, **Then** it lives at `docs/environments/config-map.md` (Q4) and is the reference the promotion runbook (unit 003) links to
- [ ] **Given** a setting that is identical across all tiers, **When** the map is written, **Then** it is omitted (the map shows only what differs, to stay maintainable)
- [ ] **Given** the map, **When** reviewed, **Then** secret values are referenced by name only (actual values live in the secrets matrix, unit 002) — no secrets in the map

## Technical Notes

- Derive the rows from `appsettings.json` vs `appsettings.Development.json` vs the new dev-env layer.
- Keep it a table per concern (DB, payments, email, CORS/limits, integrations) for scannability.

## Dependencies

### Requires
- 001-define-dev-env-tier

### Enables
- 001-promotion-path-runbook (unit 003)
- 001-secrets-tier-matrix (unit 002) complements it

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| A new setting added later | Map updated alongside; it is the single source of truth |

## Out of Scope

- Secret values (secrets matrix, unit 002).
