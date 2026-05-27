---
id: 002-lazy-loaded-routes
unit: 004-angular-app-shell
intent: 001-foundation-infrastructure
status: draft
priority: must
created: 2026-05-05T15:27:00Z
assigned_bolt: null
implemented: false
---

# Story: 002-lazy-loaded-routes

## User Story

**As a** developer
**I want** feature routes lazy-loaded using standalone component routing
**So that** the initial bundle only contains the shell and each feature loads on demand

## Acceptance Criteria

- [ ] **Given** the app starts, **When** the initial bundle is loaded, **Then** feature code for auth, upload, checkout, orders, account, admin is NOT included
- [ ] **Given** a user navigates to `/auth/login`, **When** the route is resolved, **Then** the auth feature is lazy-loaded
- [ ] **Given** route definitions, **When** examined, **Then** all feature routes use `loadComponent` or `loadChildren` with standalone component routing (no NgModules)
- [ ] **Given** legal page routes (`/politica-de-confidentialitate`, `/termeni-si-conditii`, `/politica-cookie`), **When** navigated, **Then** placeholder components are rendered
- [ ] **Given** an unknown route, **When** navigated, **Then** a 404 "Pagina nu a fost găsită" component is displayed

## Technical Notes

- Use `provideRouter()` with `Routes` in `app.config.ts`
- Feature routes: `/auth/*`, `/cos/*`, `/checkout/*`, `/comenzile-mele/*`, `/contul-meu/*`, `/admin/*`
- Each feature has a `routes.ts` file exporting route definitions
- For now, feature components are simple placeholder components ("Coming soon" text)
- Legal pages can be eagerly loaded (small components)
- Create `NotFoundComponent` for wildcard route

## Dependencies

### Requires
- 001-app-shell-layout (provides router-outlet)

### Enables
- 003-route-guards (guards applied to routes)
- All future feature components

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Slow network loading lazy chunk | Show loading indicator (optional at MVP) |
| Failed lazy chunk load | Show error message, offer retry |
| Deep link to lazy route | Load the chunk, then render |

## Out of Scope

- Actual feature implementations (just placeholder components)
- Route preloading strategies
