---
id: 003-seeding-policy-and-selector
unit: 002-secrets-and-seeding
intent: 033-environment-triad
status: draft
priority: should
created: 2026-06-05T12:35:00Z
assigned_bolt: 074-secrets-and-seeding
implemented: false
---

# Story: 003-seeding-policy-and-selector

## User Story

**As a** developer
**I want** a documented per-environment seeding policy and a mechanism that applies the right seed set per tier
**So that** each environment gets appropriate data — reference-only in prod, rich demo in dev-env — without manual guesswork

## Acceptance Criteria

- [ ] **Given** the existing `--seed` (`ProductCatalogSeed`) and `--seed-dev` (`DevDataSeed`) modes, **When** the policy is written, **Then** it states: **prod** = reference/catalog + lockers only; **dev-env** = catalog + demo users/orders; **local** = dev-env set or a lighter subset
- [ ] **Given** the policy, **When** the selection mechanism is implemented, **Then** the correct seed set is chosen by `ASPNETCORE_ENVIRONMENT` (and/or the explicit `--seed`/`--seed-dev` flag), reusing the existing seed classes — no new parallel seeder
- [ ] **Given** the mechanism, **When** run twice, **Then** it is idempotent (re-running does not duplicate seeded rows)
- [ ] **Given** the policy doc, **When** stored, **Then** it shows the exact command per tier and lives under `docs/environments/`

## Technical Notes

- `ProductCatalogSeed` + `DevDataSeed` already exist and are invoked from `Program.cs` seed-only mode; this story formalises *which runs where* and wires environment-aware selection.
- The Production demo-data guard (story 004) is the safety partner to this policy.

## Dependencies

### Requires
- unit 001 (the dev-env tier exists)

### Enables
- 004-prod-demo-data-guard
- 001-promotion-path-runbook (unit 003)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| `--seed-dev` invoked in dev-env | Applies demo data (allowed) |
| Re-seed on an already-seeded DB | Idempotent; no duplicates |

## Out of Scope

- The Production guard enforcement (story 004 — implements the refusal).
