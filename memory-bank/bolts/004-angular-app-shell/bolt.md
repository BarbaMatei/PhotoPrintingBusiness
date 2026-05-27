---
id: 004-angular-app-shell
unit: 004-angular-app-shell
intent: 001-foundation-infrastructure
type: simple-construction-bolt
status: complete
stories:
  - 001-app-shell-layout
  - 002-lazy-loaded-routes
  - 003-route-guards
  - 004-http-interceptors
  - 005-environment-config
created: 2026-05-05T15:30:00Z
started: 2026-05-20T00:00:00Z
completed: 2026-05-20T12:26:00Z
current_stage: complete
stages_completed:
  - plan
  - implement
  - test

requires_bolts: []
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 2
---

# Bolt: 004-angular-app-shell

## Overview

Build the Angular 17+ SPA foundation: standalone component app shell, lazy-loaded feature routes, functional route guards, HTTP interceptors, and environment configuration.

## Objective

Create the AppComponent shell (header, router-outlet, footer), define lazy-loaded route groups with placeholder components, implement AuthGuard/AdminGuard/GuestOrAuthGuard as functional guards, build JWT/Guest/Error interceptors, and configure environment files — producing a frontend skeleton that all feature modules will render inside.

## Stories Included

- **001-app-shell-layout**: App shell with header, footer, router-outlet (Must)
- **002-lazy-loaded-routes**: Lazy-loaded feature routes with standalone components (Must)
- **003-route-guards**: Auth, Admin, and GuestOrAuth functional guards (Must)
- **004-http-interceptors**: JWT, Guest, and Error interceptors (Must)
- **005-environment-config**: Environment files for dev and prod (Must)

## Bolt Type

**Simple Construction Bolt** — 3 stages: Implementation Plan → Implementation → Testing

## Dependencies

### Bolt Dependencies (within intent)
- None — frontend shell is independent of backend units

### Unit Dependencies (cross-unit)
- None

### Enables (other bolts waiting on this)
- All frontend feature bolts in subsequent intents

## Expected Outputs

- `src/app/app.component.ts` (standalone shell)
- `src/app/app.config.ts` (providers, routing, interceptors)
- `src/app/shared/components/header/header.component.ts`
- `src/app/shared/components/footer/footer.component.ts`
- `src/app/core/auth/auth.service.ts` (stub)
- `src/app/core/auth/jwt.interceptor.ts`
- `src/app/core/auth/guest.interceptor.ts`
- `src/app/core/auth/error.interceptor.ts`
- `src/app/core/guards/auth.guard.ts`
- `src/app/core/guards/admin.guard.ts`
- `src/app/core/guards/guest-or-auth.guard.ts`
- `src/environments/environment.ts`
- `src/environments/environment.prod.ts`
- Feature placeholder components and route files
- Unit tests for guards, interceptors, and shell components
