---
unit: 004-angular-app-shell
intent: 001-foundation-infrastructure
unit_type: frontend
default_bolt_type: simple-construction-bolt
phase: inception
status: draft
created: 2026-05-05T15:24:00Z
updated: 2026-05-05T15:24:00Z
---

# Unit Brief: Angular App Shell

## Purpose

Establish the Angular 17+ SPA foundation: standalone component app shell (header, footer, router-outlet), lazy-loaded feature routes, route guards (Auth, Admin, GuestOrAuth), HTTP interceptors (JWT, Guest, Error), and environment configuration.

## Scope

### In Scope
- App shell with header (logo, nav, cart badge, login/avatar) and footer (legal links)
- Responsive layout: hamburger menu on mobile, horizontal nav on desktop
- Lazy-loaded route groups using standalone component routing (no NgModules)
- AuthGuard, AdminGuard, GuestOrAuthGuard (functional guards, Angular 17+)
- JwtInterceptor, GuestInterceptor, ErrorInterceptor
- Environment files (dev + prod) with apiUrl, stripePublishableKey, googleClientId
- Core services: AuthService (stub), CartService (stub) for shell integration

### Out of Scope
- Feature module implementations (login forms, upload UI, etc.) — separate epics
- Actual JWT authentication logic — Epic 1
- Cart business logic — Epic 2
- Stripe/Google integration — Epic 3 / Epic 1

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-10 | Angular App Shell (standalone components) | Must |
| FR-11 | Lazy-Loaded Route Groups (standalone routing) | Must |
| FR-12 | Route Guards (Auth, Admin, GuestOrAuth) | Must |
| FR-13 | HTTP Interceptors (JWT, Guest, Error) | Must |
| FR-14 | Environment Configuration | Must |

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes |
|--------|-------------|------------|
| AppShell | Root layout component | header, routerOutlet, footer |
| RouteConfig | Lazy-loaded route definitions | path, loadComponent/loadChildren, canActivate |
| AuthState | Observable auth state | isAuthenticated$, currentUser$, role |

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| GuardRoute | Check auth state before navigation | route, AuthService state | allow or redirect |
| InterceptRequest | Attach auth headers to outgoing requests | HttpRequest | Modified HttpRequest with headers |
| HandleHttpError | Catch 401/403/5xx and take action | HttpErrorResponse | Refresh token / toast / redirect |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 5 |
| Must Have | 5 |
| Should Have | 0 |
| Could Have | 0 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-app-shell-layout | App shell with header, footer, router-outlet | Must | Planned |
| 002-lazy-loaded-routes | Lazy-loaded feature routes with standalone components | Must | Planned |
| 003-route-guards | Auth, Admin, and GuestOrAuth functional guards | Must | Planned |
| 004-http-interceptors | JWT, Guest, and Error interceptors | Must | Planned |
| 005-environment-config | Environment files for dev and prod | Must | Planned |

---

## Dependencies

### Depends On
None — frontend shell is independent of backend units.

### Depended By
All frontend feature modules in subsequent epics will render inside this shell and use these guards/interceptors.
