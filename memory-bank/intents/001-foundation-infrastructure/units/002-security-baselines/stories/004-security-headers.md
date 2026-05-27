---
id: 004-security-headers
unit: 002-security-baselines
intent: 001-foundation-infrastructure
status: draft
priority: must
created: 2026-05-05T15:26:00Z
assigned_bolt: null
implemented: false
---

# Story: 004-security-headers

## User Story

**As a** system
**I want** security headers added to all API responses
**So that** common web vulnerabilities (clickjacking, MIME sniffing, content injection) are mitigated

## Acceptance Criteria

- [ ] **Given** any API response, **When** examined, **Then** `X-Content-Type-Options: nosniff` header is present
- [ ] **Given** any API response, **When** examined, **Then** `X-Frame-Options: DENY` header is present
- [ ] **Given** any API response, **When** examined, **Then** `Referrer-Policy: strict-origin-when-cross-origin` header is present
- [ ] **Given** any API response, **When** examined, **Then** `Content-Security-Policy` header is present with restrictive policy (default-src 'self', script-src 'self', frame-ancestors 'none', object-src 'none')

## Technical Notes

- Create `SecurityHeadersMiddleware` in `src/PhotoPrint.API/Middleware/`
- Add all 4 headers to every response
- CSP policy should be configurable via appsettings for fine-tuning
- Register early in middleware pipeline (after CORS, before routing)

## Dependencies

### Requires
- None

### Enables
- Production security compliance

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| CSP conflicts with Stripe Elements | CSP must allow Stripe domains (js.stripe.com) — configure in appsettings |
| CSP conflicts with Google OAuth | CSP must allow Google domains (accounts.google.com) |
| Swagger UI in development | CSP may need relaxation for Swagger — only in Development environment |

## Out of Scope

- Permissions-Policy header — future enhancement
- Report-URI for CSP violation reporting
