---
id: 005-fluentvalidation-integration
unit: 001-error-handling-logging
intent: 001-foundation-infrastructure
status: complete
priority: must
created: 2026-05-05T15:25:00Z
assigned_bolt: null
implemented: true
---

# Story: 005-fluentvalidation-integration

## User Story

**As a** developer
**I want** request DTOs automatically validated by FluentValidation with consistent 422 error responses
**So that** every endpoint gets input validation without manual checks in controllers

## Acceptance Criteria

- [ ] **Given** a request DTO with a registered FluentValidation validator, **When** the request is invalid, **Then** a 422 response is returned with `{ "errors": [{ "field": "email", "message": "Adresa de email nu este validă" }] }`
- [ ] **Given** multiple validation errors, **When** the request is invalid, **Then** all errors are returned in a single response (not just the first)
- [ ] **Given** a request DTO without a validator, **When** the request is sent, **Then** it passes through without validation errors
- [ ] **Given** validation messages, **When** returned to the client, **Then** all messages are in Romanian

## Technical Notes

- NuGet: `FluentValidation.AspNetCore`
- Configure in `Program.cs`: `AddFluentValidationAutoValidation()`
- Create custom `ValidationFilter` as an action filter that intercepts `ModelState` errors from FluentValidation
- Transform ModelState errors into `{ "errors": [{ "field", "message" }] }` format
- Register all validators from assembly via `AddValidatorsFromAssemblyContaining<Program>()`

## Dependencies

### Requires
- 001-exception-handler-middleware (validation filter works alongside exception middleware)

### Enables
- All future request DTOs with validators (Epic 1 registration, login, etc.)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Nested object validation | Nested field names include parent (e.g., "address.city") |
| Null request body | Return 400 Bad Request (before validation runs) |
| Empty array field | Validate according to validator rules |

## Out of Scope

- Specific validators for business DTOs — created with each feature story
