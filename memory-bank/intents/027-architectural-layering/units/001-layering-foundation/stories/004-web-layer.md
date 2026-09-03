---
id: 004-web-layer
unit: 001-layering-foundation
intent: 027-architectural-layering
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 059-layering-foundation
implemented: false
---

# Story: 004-web-layer

## User Story

**As a** developer
**I want** controllers, hubs, middleware, filters, auth, and request validators under `Web/`
**So that** the presentation layer is clearly delimited and stops reaching into data access

## Acceptance Criteria

- [ ] **Given** `Controllers/`, `Hubs/`, `Middleware/`, `Filters/`, `Authentication/`, and request-shape validators, **When** moved to `Web/`, **Then** namespaces update and routing/DI still resolve
- [ ] **Given** the four controllers that inject `PhotoPrintDbContext`, **When** this layering completes (with units 002/003), **Then** they no longer hold a direct `DbContext` dependency
- [ ] **Given** the move, **When** built/tested, **Then** CI green and `Add-Migration` empty diff

## Technical Notes

- Settings validators are NOT request validators — they move to `Configuration/Validators/` (P21 PR5 / unit sweep), not `Web/`.

## Dependencies

### Requires
- 003-infrastructure-layer

### Enables
- 005-application-feature-promotion

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Controller still needs a query | Route through an Application service/handler, not DbContext |

## Out of Scope

- Promoting Services/ to Application/ (next story).
