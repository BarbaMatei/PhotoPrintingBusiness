---
id: 001-system-info-endpoint
unit: 002-system-manifest-and-liveness
intent: 026-observability-boot-manifest
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 056-system-manifest-and-liveness
implemented: false
---

# Story: 001-system-info-endpoint

## User Story

**As an** admin/maintainer
**I want** a `/api/admin/system-info` endpoint that reports what is wired
**So that** off-by-default regressions are caught at PR time, not in production

## Acceptance Criteria

- [ ] **Given** an admin caller, **When** `GET /api/admin/system-info` is hit, **Then** it returns 200 `SystemManifest` (hosted services + status + gating flag, flags, admin routes, webhook routes, CLI verbs)
- [ ] **Given** an anonymous caller, **When** the endpoint is hit, **Then** it returns 401
- [ ] **Given** the flag section, **When** built, **Then** it is derived entirely from `IFeatureGate.GetAll()` (no duplicated list)
- [ ] **Given** `Anaf:Enabled=true`, **When** an integration test inspects the manifest, **Then** `InvoiceUploadJob` shows `Running`; removing its registration fails the test
- [ ] **Given** the response, **When** inspected, **Then** no secrets are present and the result is cached ~30s

## Technical Notes

- Introspect `IEnumerable<IHostedService>`, `IFeatureGate`, and `IEndpointRouteBuilder` (reflection with a static fallback for routes).

## Dependencies

### Requires
- 026/001/002-typed-feature-gate

### Enables
- 026/004 admin System tab UI

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Route reflection incomplete | Fall back to a curated list; flag the gap in the manifest |

## Out of Scope

- The UI tab (unit 004).
