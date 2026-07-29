---
id: 002-shared-test-application-factory
unit: 001-test-infrastructure
intent: 028-test-architecture
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 062-test-infrastructure
implemented: false
---

# Story: 002-shared-test-application-factory

## User Story

**As a** developer
**I want** one shared `WebApplicationFactory` base instead of 11 duplicating config
**So that** a standard test-config change is one edit, not eleven

## Acceptance Criteria

- [ ] **Given** `MetricsEndpointIntegrationTests.ObservabilityFactoryBase`, **When** promoted to `tests/_Base/PhotoPrintTestApplicationFactory.cs` (public abstract), **Then** it holds the 25 standard config keys + InMemory swap + no-op email
- [ ] **Given** the 11 factories, **When** refactored, **Then** each inherits the base and keeps only feature-specific overrides (~30 LOC)
- [ ] **Given** the change, **When** the suite runs, **Then** it is green
- [ ] **Given** `IClassFixture` sharing, **When** verified, **Then** no test depends on per-test isolation the share would break

## Technical Notes

- Standard config: Cors, RateLimit, JwtSettings, Email provider, HealthCheck paths, App:BaseUrl.

## Dependencies

### Requires
- 001-timeprovider-adoption

### Enables
- 003-test-builders, 004-reclassify-misnamed-unit-tests

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Factory needs a unique override | Override only the differing config; base stays shared |

## Out of Scope

- Builders (next story).
