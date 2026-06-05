---
id: 001-admin-system-info-tab
unit: 004-observability-boot-manifest-ui
intent: 026-observability-boot-manifest
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 058-observability-boot-manifest-ui
implemented: false
---

# Story: 001-admin-system-info-tab

## User Story

**As an** admin
**I want** a System tab that renders the system-info manifest
**So that** I can see hosted services, flags, routes, and CLI verbs at a glance

## Acceptance Criteria

- [ ] **Given** an admin user, **When** they open the System tab, **Then** it fetches `/api/admin/system-info` and lists hosted services + status, feature flags, admin/webhook routes, and CLI verbs
- [ ] **Given** the manifest, **When** the admin searches, **Then** the view filters across all sections
- [ ] **Given** a non-admin, **When** they navigate to the route, **Then** access is denied (route guard + 401 from API)
- [ ] **Given** the lazy-loaded route, **When** built, **Then** it stays within the bundle budget (intent 030 P18)

## Technical Notes

- Standalone Angular 21 component under `features/admin/pages/system/`; use `BaseApiService` if intent 030 P26 has landed, else hand-rolled HttpClient with `withCredentials`.

## Dependencies

### Requires
- 026/002/001-system-info-endpoint

### Enables
- None

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Endpoint returns a flagged route-gap | Surface the gap note in the UI |

## Out of Scope

- The backend manifest (unit 002).
