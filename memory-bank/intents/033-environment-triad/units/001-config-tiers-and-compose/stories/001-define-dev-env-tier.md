---
id: 001-define-dev-env-tier
unit: 001-config-tiers-and-compose
intent: 033-environment-triad
status: draft
priority: must
created: 2026-06-05T12:30:00Z
assigned_bolt: 073-config-tiers-and-compose
implemented: false
---

# Story: 001-define-dev-env-tier

## User Story

**As a** developer preparing a sandbox to experiment freely
**I want** a named, deployable dev-environment configuration tier distinct from local and prod
**So that** the app can run prod-shaped (Postgres) but freely seedable, without touching production config

## Acceptance Criteria

- [ ] **Given** the three-tier model, **When** the dev-env tier is defined, **Then** it has a documented `ASPNETCORE_ENVIRONMENT` value (Q1: recommend `Staging` or `DevEnvironment`) and an `appsettings.{tier}.json` that layers over `appsettings.json`
- [ ] **Given** the dev-env tier, **When** it boots, **Then** its connection string targets the compose PostgreSQL service, and it uses **test-mode** payment keys, a dev-env CORS/rate-limit posture (between local-relaxed and prod-strict), and dev-env email (Q2)
- [ ] **Given** the dev-env tier config, **When** compared to Development and Production, **Then** every differing setting is intentional and captured in the config map (story 003)
- [ ] **Given** this story, **When** complete, **Then** the tier is **runnable locally** for validation only — no host is provisioned (Phase-6 boundary respected)

## Technical Notes

- The dev-env appsettings layer carries non-secret defaults only; secrets come via env vars (ADR-006).
- Pick the tier name once and use it consistently across appsettings, compose, and docs.

## Dependencies

### Requires
- None (builds from existing appsettings)

### Enables
- 002-dev-env-compose-file
- 003-three-tier-config-map
- 004-boot-validation-parity

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| `ASPNETCORE_ENVIRONMENT` unset | Falls back per ASP.NET default (Production) — dev-env is opt-in, never implicit |
| Dev-env name collides with built-in `Staging` semantics | If `Staging` chosen, document that it IS the dev sandbox tier |

## Out of Scope

- Compose file (story 002), config map (story 003), validation (story 004).
- Standing the tier up on a host (Phase 6).
