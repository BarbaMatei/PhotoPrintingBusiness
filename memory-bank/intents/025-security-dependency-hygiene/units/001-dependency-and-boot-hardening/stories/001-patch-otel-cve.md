---
id: 001-patch-otel-cve
unit: 001-dependency-and-boot-hardening
intent: 025-security-dependency-hygiene
status: draft
priority: must
created: 2026-06-05T09:30:00Z
assigned_bolt: 054-dependency-and-boot-hardening
implemented: false
---

# Story: 001-patch-otel-cve

## User Story

**As a** maintainer preparing for a security audit
**I want** the OpenTelemetry packages bumped past the known CVE
**So that** the deployed observability pipeline has no unpatched moderate advisory

## Acceptance Criteria

- [ ] **Given** the six `OpenTelemetry.*` references at 1.11.x, **When** they are bumped in lockstep to 1.15.x, **Then** `dotnet restore && dotnet build && dotnet test` succeeds
- [ ] **Given** the bump, **When** `dotnet list package --vulnerable` runs, **Then** it reports zero vulnerable packages
- [ ] **Given** the OTel pipeline, **When** `MetricsEndpointIntegrationTests` runs, **Then** `/metrics` and EF span emission still work
- [ ] **Given** no stable peer exists for an instrumentation package, **When** a beta is pinned, **Then** the pin is documented with a comment

## Technical Notes

- Bump as a set; version skew across OTel sub-packages causes init failures (GHSA-4625-4j76-fww9 → 1.15.x).
- `Instrumentation.EntityFrameworkCore` / `Prometheus.AspNetCore` may remain on a `-beta` track.

## Dependencies

### Requires
- None (first story)

### Enables
- 002-central-package-management (CPM will pin the new OTel versions)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Prometheus exporter API moved | Smoke-test `/metrics`; adjust registration |
| EF instrumentation beta breaks spans | Pin known-good beta; document |

## Out of Scope

- Sentry/AWS major upgrades (defer to Renovate cadence).
