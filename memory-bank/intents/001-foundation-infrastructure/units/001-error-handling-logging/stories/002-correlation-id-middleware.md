---
id: 002-correlation-id-middleware
unit: 001-error-handling-logging
intent: 001-foundation-infrastructure
status: complete
priority: must
created: 2026-05-05T15:25:00Z
assigned_bolt: null
implemented: true
---

# Story: 002-correlation-id-middleware

## User Story

**As a** developer
**I want** every request to carry a correlation ID through the entire request lifecycle
**So that** I can trace errors and logs across middleware, services, and responses

## Acceptance Criteria

- [ ] **Given** a request with `X-Correlation-Id` header, **When** processed, **Then** the same value is used throughout and returned in the response header
- [ ] **Given** a request without `X-Correlation-Id` header, **When** processed, **Then** a new UUID is generated and returned in the response header
- [ ] **Given** a correlation ID is set, **When** Serilog logs a message, **Then** the CorrelationId property is included automatically
- [ ] **Given** a correlation ID is set, **When** an exception occurs, **Then** the ProblemDetails response includes the correlationId field

## Technical Notes

- Create `CorrelationIdMiddleware` in `src/PhotoPrint.API/Middleware/`
- Store correlation ID in `HttpContext.Items["CorrelationId"]` for service-layer access
- Push to Serilog `LogContext` using `LogContext.PushProperty("CorrelationId", correlationId)`
- Add `X-Correlation-Id` to response headers
- Register before ExceptionHandlerMiddleware so errors include correlation ID

## Dependencies

### Requires
- None (but should be registered before exception handler)

### Enables
- 001-exception-handler-middleware (provides correlationId for ProblemDetails)
- 003-serilog-configuration (provides correlationId enrichment)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Malformed X-Correlation-Id (not UUID) | Accept as-is (don't validate format, treat as opaque string) |
| Empty X-Correlation-Id header | Generate new UUID |

## Out of Scope

- Distributed tracing (OpenTelemetry) — future enhancement
