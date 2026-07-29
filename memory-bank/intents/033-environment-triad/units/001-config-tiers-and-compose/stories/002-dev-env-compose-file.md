---
id: 002-dev-env-compose-file
unit: 001-config-tiers-and-compose
intent: 033-environment-triad
status: draft
priority: must
created: 2026-06-05T12:30:00Z
assigned_bolt: 073-config-tiers-and-compose
implemented: false
---

# Story: 002-dev-env-compose-file

## User Story

**As a** developer
**I want** a `docker-compose.dev-env.yml` expressing the dev-env tier
**So that** the sandbox tier is described as infrastructure the same way local and prod already are

## Acceptance Criteria

- [ ] **Given** the existing compose files, **When** the dev-env compose is authored, **Then** it stands up Postgres + API (+ dev-env email per Q2), parameterised by a dev-env `.env`, with the dev-env `ASPNETCORE_ENVIRONMENT`
- [ ] **Given** the dev-env compose, **When** `docker compose -f docker-compose.dev-env.yml config` runs, **Then** it validates, and a local `up` boots the API healthily against the compose Postgres
- [ ] **Given** the three compose files, **When** compared, **Then** dev-env never references the prod DB, prod CORS origin, live payment keys, or the Caddy edge (dev-env is not customer-facing)
- [ ] **Given** the existing `docker-compose.yml` and `docker-compose.prod.yml`, **When** this story lands, **Then** their behaviour is unchanged (Q5: standalone file, not a prod overlay)
- [ ] **Given** this story, **When** complete, **Then** the compose is validated **locally only** — no deployment to a host

## Technical Notes

- Model on `docker-compose.yml` (dev tier) for the Postgres+API+MailHog shape, but pin the dev-env `ASPNETCORE_ENVIRONMENT` and provider, and drop host port exposure that doesn't suit a sandbox.
- Keep Caddy/TLS out of dev-env unless the owner wants edge parity (note as a Phase-6 option).

## Dependencies

### Requires
- 001-define-dev-env-tier

### Enables
- 001-promotion-path-runbook (unit 003) references it

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Port clash with local stack | Use distinct host ports / project name so both can coexist on a dev machine |

## Out of Scope

- Deploying the compose to a server (Phase 6).
- The secrets template (unit 002).
