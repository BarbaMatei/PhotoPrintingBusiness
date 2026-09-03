---
unit: 004-observability-boot-manifest-ui
intent: 026-observability-boot-manifest
phase: inception
status: draft
unit_type: frontend
default_bolt_type: simple-construction-bolt
created: 2026-06-05T09:30:00Z
updated: 2026-06-05T09:30:00Z
---

# Unit Brief: System Manifest UI (Admin)

## Purpose

Render the `/api/admin/system-info` manifest as a searchable, clickable Admin "System" tab so the maintainer can see hosted services, flags, routes, and CLI verbs at a glance.

## Scope

### In Scope
- An Angular admin page under `features/admin/pages/system/` that fetches and displays the manifest with search/filter.

### Out of Scope
- The backend endpoint (unit 002) and the flag registry (unit 001).

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-3 (P04, UI) | Admin System tab rendering the manifest | Should |

---

## Domain Concepts

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| Load manifest | Fetch + render | GET /api/admin/system-info | searchable table view |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 1 |
| Must Have | 0 |
| Should Have | 1 |
| Could Have | 0 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-admin-system-info-tab | Admin System tab | Should | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| 002-system-manifest-and-liveness | Consumes the manifest endpoint |

### Depended By
| Unit | Reason |
|------|--------|
| None | — |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| PhotoPrint.API | Manifest data | Low |

---

## Technical Context

### Suggested Technology
Angular 21 standalone component, lazy-loaded admin route, existing admin shell patterns; should use `BaseApiService` once intent 030 P26 lands (otherwise hand-rolled HttpClient).

### Integration Points
| Integration | Type | Protocol |
|-------------|------|----------|
| Admin API | API | GET /api/admin/system-info |

---

## Constraints

- Admin-only route; no secrets displayed.

---

## Success Criteria

### Functional
- [ ] System tab lists hosted services + status, flags, routes, CLI verbs.
- [ ] Search/filter works across the manifest.

### Non-Functional
- [ ] Lazy-loaded; respects bundle budget (intent 030 P18).

### Quality
- [ ] Vitest spec for the page.

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 058-observability-boot-manifest-ui | simple | 001 | Admin System tab |

---

## Notes

Small UI; depends on unit 002 backend. Can ship after the manifest endpoint.
