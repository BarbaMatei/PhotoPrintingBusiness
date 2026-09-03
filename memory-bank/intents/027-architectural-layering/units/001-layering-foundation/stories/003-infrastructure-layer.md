---
id: 003-infrastructure-layer
unit: 001-layering-foundation
intent: 027-architectural-layering
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 059-layering-foundation
implemented: false
---

# Story: 003-infrastructure-layer

## User Story

**As a** developer
**I want** EF Core, HttpClient, and SDK code grouped under `Infrastructure/`
**So that** infrastructure concerns are clearly separated from application logic

## Acceptance Criteria

- [ ] **Given** `Data/`, `BackgroundJobs/`, `Observability/`, email templates, and implementation-only halves of features, **When** moved to `Infrastructure/<area>/`, **Then** namespaces update and DI registrations still resolve
- [ ] **Given** the Sameday background jobs (currently flat in `BackgroundJobs/`), **When** moved, **Then** they land under `Infrastructure/Sameday/`
- [ ] **Given** the move, **When** built/tested, **Then** CI green and `Add-Migration` empty diff

## Technical Notes

- Implementation classes (e.g. `S3StorageService`) move to `Infrastructure/Storage/`; their interfaces stay in `Application/Storage/` (Abstractions added in unit 002).

## Dependencies

### Requires
- 002-domain-layer-extraction

### Enables
- 004-web-layer

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| A service mixes app + infra | Split or leave the coordinator in Application, infra impl in Infrastructure |

## Out of Scope

- Web/ presentation move (next story).
