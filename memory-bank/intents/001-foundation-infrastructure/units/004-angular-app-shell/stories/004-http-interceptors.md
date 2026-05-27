---
id: 004-http-interceptors
unit: 004-angular-app-shell
intent: 001-foundation-infrastructure
status: draft
priority: must
created: 2026-05-05T15:27:00Z
assigned_bolt: null
implemented: false
---

# Story: 004-http-interceptors

## User Story

**As a** system
**I want** HTTP interceptors that automatically attach auth headers and handle errors
**So that** every API call includes the correct authentication and errors are handled consistently

## Acceptance Criteria

- [ ] **Given** a user with a JWT access token, **When** an API call is made, **Then** `Authorization: Bearer {token}` header is attached
- [ ] **Given** a guest user with a guest token (no JWT), **When** an API call is made, **Then** `X-Guest-Token: {token}` header is attached
- [ ] **Given** both JWT and guest token exist, **When** an API call is made, **Then** only `Authorization: Bearer` is attached (JWT takes precedence)
- [ ] **Given** a 401 response, **When** the interceptor catches it, **Then** a token refresh is attempted; if refresh fails, the user is logged out
- [ ] **Given** a 403 response, **When** the interceptor catches it, **Then** a toast message "Acces interzis" is shown
- [ ] **Given** a 5xx response, **When** the interceptor catches it, **Then** a toast message "Eroare de server. Încearcă din nou." is shown

## Technical Notes

- Use `HttpInterceptorFn` functional interceptors (Angular 17+)
- `jwtInterceptor`: clone request with Bearer header if token exists; on 401, queue request, attempt refresh, replay
- `guestInterceptor`: if no JWT, check localStorage for guest token, attach X-Guest-Token
- `errorInterceptor`: catch errors, show toast notifications, handle 401 refresh flow
- Register in `app.config.ts` via `provideHttpClient(withInterceptors([...]))`
- Toast notification: simple service with `BehaviorSubject<Toast[]>` and a toast container component

## Dependencies

### Requires
- 001-app-shell-layout (provides AuthService stub for token access)

### Enables
- All API calls in feature modules automatically get auth headers
- Consistent error handling across the app

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Multiple simultaneous 401s | Queue requests, refresh once, replay all |
| Refresh token also returns 401 | Logout user, redirect to login |
| Network error (no response) | Show "Eroare de rețea. Verifică conexiunea." toast |
| Request to external URL (Stripe, Google) | Do NOT attach auth headers (only attach for apiUrl) |

## Out of Scope

- Actual token refresh API call (stubbed until Epic 1)
- Retry logic for failed requests (beyond 401 refresh)
