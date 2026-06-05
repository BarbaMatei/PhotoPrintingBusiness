---
id: 001-secrets-tier-matrix
unit: 002-secrets-and-seeding
intent: 033-environment-triad
status: draft
priority: must
created: 2026-06-05T12:35:00Z
assigned_bolt: 074-secrets-and-seeding
implemented: false
---

# Story: 001-secrets-tier-matrix

## User Story

**As a** maintainer
**I want** a matrix of every secret against each tier
**So that** it is unambiguous which secrets each environment needs, test vs live, and where they live

## Acceptance Criteria

- [ ] **Given** the app's secrets, **When** the matrix is written, **Then** it rows every secret (JWT keypair, Stripe, EuPlatesc, SendGrid, Google OAuth, ANAF, Sameday, Storage credentials, Sentry DSN) against columns local / dev-env / prod
- [ ] **Given** each cell, **When** filled, **Then** it states: required?, **test-vs-live** value class, and where stored (user-secrets / `.env` / platform secret store)
- [ ] **Given** the dev-env column, **When** reviewed, **Then** it uses **test/sandbox** credentials throughout (Stripe test, EuPlatesc test, ANAF test base URL); live credentials appear only in the prod column
- [ ] **Given** the matrix, **When** stored, **Then** it lives at `docs/environments/secrets-matrix.md` and references ADR-006 + the secret-scanning controls (intent 018); it contains **no real secret values**

## Technical Notes

- Source the secret list from `.env.example` + the `appsettings.json` `_comment` annotations (ANAF, Sameday, Sentry, Storage all document their secret fields).
- The matrix is documentation; it does not store secrets.

## Dependencies

### Requires
- unit 001 (the dev-env tier exists)

### Enables
- 002-dev-env-secrets-template
- 001-promotion-path-runbook (unit 003)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| A secret is optional in a tier (feature flag off) | Marked optional with the gating flag noted (e.g. ANAF only when `Anaf:Enabled`) |

## Out of Scope

- The `.env` template (story 002); seeding (stories 003/004).
