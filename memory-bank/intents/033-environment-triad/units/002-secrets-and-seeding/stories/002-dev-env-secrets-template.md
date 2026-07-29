---
id: 002-dev-env-secrets-template
unit: 002-secrets-and-seeding
intent: 033-environment-triad
status: draft
priority: must
created: 2026-06-05T12:35:00Z
assigned_bolt: 074-secrets-and-seeding
implemented: false
---

# Story: 002-dev-env-secrets-template

## User Story

**As a** developer setting up the dev-env tier
**I want** a `.env.dev-env.example` template
**So that** I can copy it and fill in test-mode credentials without guessing what the tier needs

## Acceptance Criteria

- [ ] **Given** the secrets matrix (story 001), **When** the template is authored, **Then** `.env.dev-env.example` exists alongside `.env.example`, pre-set with the dev-env `ASPNETCORE_ENVIRONMENT`, `DatabaseProvider=Postgres`, dev-env hostnames, and **test-mode** payment/email placeholders
- [ ] **Given** the template, **When** inspected, **Then** it contains **no real secrets** (placeholders only) and passes the secret-scanning hook + Gitleaks (intent 018)
- [ ] **Given** the template, **When** compared to `.env.example` (prod-oriented), **Then** the differences (test vs live, dev-env vs prod hostnames) are exactly those in the secrets matrix
- [ ] **Given** the template, **When** used with `docker-compose.dev-env.yml`, **Then** the dev-env stack boots locally with `ValidateOnStart` passing once placeholders are filled with valid test values

## Technical Notes

- Mirror `.env.example`'s structure and comments; flip values to the dev-env/test column from the matrix.
- Keep the file committed (like `.env.example`); the real `.env.dev-env` is gitignored.

## Dependencies

### Requires
- 001-secrets-tier-matrix
- unit 001 story 002 (compose) for end-to-end validation

### Enables
- 001-promotion-path-runbook (unit 003)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Developer accidentally pastes a live key | Secret-scanning hook blocks the commit (intent 018) |

## Out of Scope

- The seeding policy (stories 003/004).
