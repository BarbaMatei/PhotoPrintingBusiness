---
id: 004-boot-validation-parity
unit: 001-config-tiers-and-compose
intent: 033-environment-triad
status: draft
priority: must
created: 2026-06-05T12:30:00Z
assigned_bolt: 073-config-tiers-and-compose
implemented: false
---

# Story: 004-boot-validation-parity

## User Story

**As a** maintainer
**I want** the dev-env tier to enforce its required settings at boot exactly like production
**So that** a misconfigured dev-env fails loudly instead of silently behaving like local Development

## Acceptance Criteria

- [ ] **Given** the existing `ValidateOnStart` options validation, **When** the dev-env tier boots, **Then** required settings (JWT keypair, DB connection, payment **test** keys as the tier requires) are validated and a missing one throws at startup (no silent fallback)
- [ ] **Given** the dev-env tier, **When** a required secret is absent, **Then** the failure message names the missing setting and the tier — matching the prod failure behaviour
- [ ] **Given** Development (local), **When** it boots, **Then** its more relaxed validation is unchanged (local still tolerates placeholder/test values)
- [ ] **Given** the three tiers, **When** validation rules are reviewed, **Then** dev-env validation is closer to prod than to local (it is a deployable tier, not a dev convenience)

## Technical Notes

- Reuse the existing options validators (e.g. `SellerSettingsValidator`, JWT/Stripe boot checks); extend their environment applicability to include the dev-env tier.
- Do not weaken prod validation; only add dev-env coverage.

## Dependencies

### Requires
- 001-define-dev-env-tier

### Enables
- 001-promotion-path-runbook (unit 003) can rely on dev-env being a faithful pre-prod check

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Dev-env run with local placeholder keys | Boot fails (dev-env demands real test-mode keys), surfacing the misconfig early |

## Out of Scope

- The secrets matrix itself (unit 002 story 001).
