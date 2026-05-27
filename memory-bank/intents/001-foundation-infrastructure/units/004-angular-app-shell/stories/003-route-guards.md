---
id: 003-route-guards
unit: 004-angular-app-shell
intent: 001-foundation-infrastructure
status: draft
priority: must
created: 2026-05-05T15:27:00Z
assigned_bolt: null
implemented: false
---

# Story: 003-route-guards

## User Story

**As a** system
**I want** route guards that protect pages based on authentication state and user role
**So that** unauthorized users cannot access protected pages and are redirected appropriately

## Acceptance Criteria

- [ ] **Given** an unauthenticated user navigating to a protected route, **When** AuthGuard evaluates, **Then** the user is redirected to `/auth/login` and the original URL is stored as `returnUrl`
- [ ] **Given** a non-admin user navigating to `/admin/*`, **When** AdminGuard evaluates, **Then** the user is redirected to `/`
- [ ] **Given** a user with JWT token OR guest token navigating to checkout, **When** GuestOrAuthGuard evaluates, **Then** access is allowed
- [ ] **Given** a user with neither JWT nor guest token navigating to checkout, **When** GuestOrAuthGuard evaluates, **Then** the user is redirected to `/auth/login`
- [ ] **Given** all guards, **When** examined, **Then** they are functional guards (Angular 15+ `CanActivateFn` pattern)

## Technical Notes

- Use `CanActivateFn` functional guards (not class-based)
- `authGuard`: inject `AuthService`, check `isAuthenticated()`, store `returnUrl` via `AuthService.setReturnUrl()`
- `adminGuard`: inject `AuthService`, decode JWT, check `role` claim === `Admin`
- `guestOrAuthGuard`: check either `AuthService.isAuthenticated()` OR presence of guest token in storage
- Register guards in route definitions via `canActivate: [authGuard]`

## Dependencies

### Requires
- 001-app-shell-layout (provides AuthService stub)
- 002-lazy-loaded-routes (provides route definitions to attach guards to)

### Enables
- Protected routes for account, orders, admin
- Checkout flow accessible by both auth modes

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Expired JWT (not yet refreshed) | Guard checks observable; interceptor handles refresh |
| Guard applied to child route | Guard fires on child navigation too |
| returnUrl is external URL | Ignore external URLs to prevent open redirect |

## Out of Scope

- JWT token refresh logic (handled by interceptor in 004-http-interceptors)
- Actual authentication implementation (Epic 1)
