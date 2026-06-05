---
id: 004-prod-demo-data-guard
unit: 002-secrets-and-seeding
intent: 033-environment-triad
status: draft
priority: should
created: 2026-06-05T12:35:00Z
assigned_bolt: 074-secrets-and-seeding
implemented: false
---

# Story: 004-prod-demo-data-guard

## User Story

**As a** maintainer
**I want** demo data seeding to be impossible in Production
**So that** demo users/orders can never contaminate a real customer database, even by mistake

## Acceptance Criteria

- [ ] **Given** the dev demo seeder (`DevDataSeed`), **When** it is invoked while `ASPNETCORE_ENVIRONMENT=Production`, **Then** it refuses to run and logs a clear explanation (no demo rows written)
- [ ] **Given** `--seed` (reference/catalog) in Production, **When** invoked, **Then** it runs normally (reference data is allowed in prod)
- [ ] **Given** the guard, **When** dev-env or local invokes `--seed-dev`, **Then** demo data is applied (guard only blocks Production)
- [ ] **Given** the guard, **When** tested, **Then** a unit/integration test asserts the Production refusal and the dev-env/local allowance

## Technical Notes

- Smallest possible enforcement: an environment check at the top of `DevDataSeed.ApplyAsync` (or the seed-mode dispatcher) that throws/returns on Production.
- Pair with the policy doc (story 003) so the guard and the documentation agree.

## Dependencies

### Requires
- 003-seeding-policy-and-selector

### Enables
- 001-promotion-path-runbook (unit 003) can state "demo data structurally cannot reach prod"

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Demo seed forced via a flag in prod | Still refused — the guard is not flag-overridable |
| Environment misdetected | Default to the safe side (refuse demo) when uncertain |

## Out of Scope

- The seeding policy doc (story 003 — this story enforces it).
