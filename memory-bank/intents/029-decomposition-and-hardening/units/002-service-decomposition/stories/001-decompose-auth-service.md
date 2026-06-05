---
id: 001-decompose-auth-service
unit: 002-service-decomposition
intent: 029-decomposition-and-hardening
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 064-service-decomposition
implemented: false
---

# Story: 001-decompose-auth-service

## User Story

**As a** developer
**I want** the 424-LOC `AuthService` split into three focused services
**So that** register/reset/auth concerns are separable and individually testable

## Acceptance Criteria

- [ ] **Given** `AuthService` (6 concerns), **When** split, **Then** `IAccountRegistrationService` (Register, ConfirmEmail, ResendConfirmation), `IPasswordResetService` (Forgot, Reset), and a slim `IAuthService` (Login, Refresh, Revoke) exist
- [ ] **Given** the new services, **When** placed, **Then** they live under `Application/Auth/Services/` (post-027 shape)
- [ ] **Given** each service, **When** tested, **Then** it has its own test file (the 636-LOC `AuthServiceTests.cs` splits)
- [ ] **Given** the split, **When** the auth integration suite runs, **Then** it passes (no behaviour change)

## Technical Notes

- TimeProvider already injected (intent 028 P28) → deterministic expiry tests.

## Dependencies

### Requires
- 027 (layered shape)

### Enables
- None

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Token-service / email-token concerns | Stay separate (already their own services) |

## Out of Scope

- Webhook/order decomposition (next story).
