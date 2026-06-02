---
id: 001-exception-handler-middleware
unit: 001-error-handling-logging
intent: 001-foundation-infrastructure
status: complete
priority: must
created: 2026-05-05T15:25:00Z
assigned_bolt: null
implemented: true
---

# Story: 001-exception-handler-middleware

## User Story

**As a** developer
**I want** all unhandled exceptions to return consistent ProblemDetails (RFC 7807) JSON responses
**So that** API consumers get predictable error formats regardless of which endpoint fails

## Acceptance Criteria

- [ ] **Given** a NotFoundException is thrown, **When** the middleware catches it, **Then** a 404 ProblemDetails response is returned with `type`, `title`, `status`, `detail`, `correlationId`
- [ ] **Given** a ConflictException is thrown, **When** the middleware catches it, **Then** a 409 ProblemDetails response is returned
- [ ] **Given** a ForbiddenException is thrown, **When** the middleware catches it, **Then** a 403 ProblemDetails response is returned
- [ ] **Given** an unknown exception is thrown in production, **When** the middleware catches it, **Then** a 500 ProblemDetails response is returned with NO internal details (no stack trace, no exception message)
- [ ] **Given** an unknown exception is thrown in development, **When** the middleware catches it, **Then** the response includes exception details for debugging

## Technical Notes

- Create custom exception classes in `src/PhotoPrint.API/Exceptions/`: `NotFoundException`, `ConflictException`, `ForbiddenException`, `UnauthorizedException`
- Create `ExceptionHandlerMiddleware` in `src/PhotoPrint.API/Middleware/`
- Map exception types: NotFoundException→404, ConflictException→409, ForbiddenException→403, UnauthorizedException→401, all others→500
- Use `IHostEnvironment` to determine whether to include exception details
- Register in `Program.cs` pipeline

## Dependencies

### Requires
- None

### Enables
- 002-correlation-id-middleware (correlation ID included in ProblemDetails)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Exception in middleware itself | Should not cause infinite loop; fall back to 500 plain response |
| Null exception message | Return generic detail text in Romanian |
| Aggregate exception | Log all inner exceptions, return first as detail |

## Out of Scope

- Validation errors (handled by 005-fluentvalidation-integration)
