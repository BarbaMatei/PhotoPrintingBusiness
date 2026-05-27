---
unit: 001-error-handling-logging
intent: 001-foundation-infrastructure
phase: inception
status: draft
created: 2026-05-05T15:24:00Z
updated: 2026-05-05T15:24:00Z
---

# Unit Brief: Error Handling & Logging

## Purpose

Provide the global exception handling middleware, correlation ID tracking, structured logging (Serilog), health check endpoint, and FluentValidation integration that every API endpoint depends on.

## Scope

### In Scope
- ExceptionHandlerMiddleware → ProblemDetails (RFC 7807)
- Custom exception types (NotFoundException, ConflictException, ForbiddenException, UnauthorizedException)
- CorrelationIdMiddleware (read/generate X-Correlation-Id)
- Serilog configuration (console dev, JSON file prod)
- Health check endpoint (GET /health)
- FluentValidation auto-validation + custom ValidationFilter (422)

### Out of Scope
- Security headers (002-security-baselines)
- CORS, rate limiting (002-security-baselines)
- Email-specific error handling (003-email-infrastructure)
- Frontend error interceptors (004-angular-app-shell)

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-1 | Exception Handler Middleware → ProblemDetails | Must |
| FR-2 | Correlation ID Middleware | Must |
| FR-3 | Structured Logging (Serilog) | Must |
| FR-4 | Health Check Endpoint | Must |
| FR-5 | FluentValidation Integration | Must |

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes |
|--------|-------------|------------|
| ProblemDetails | RFC 7807 error response | type, title, status, detail, correlationId |
| CorrelationContext | Per-request tracking ID | correlationId (Guid) |
| HealthStatus | System health snapshot | status, db, diskFreeGb |

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| HandleException | Catch unhandled exception, map to ProblemDetails | Exception | ProblemDetails JSON (4xx/5xx) |
| TrackCorrelation | Read or generate correlation ID per request | X-Correlation-Id header (optional) | correlationId in response + logs |
| CheckHealth | Verify DB + disk status | none | HealthStatus JSON |
| ValidateRequest | Auto-validate DTOs via FluentValidation | Request DTO | 422 errors or pass-through |

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
| 001-exception-handler-middleware | Exception handler returns ProblemDetails | Must | Planned |
| 002-correlation-id-middleware | Correlation ID tracking per request | Must | Planned |
| 003-serilog-configuration | Structured logging with Serilog | Must | Planned |
| 004-health-check-endpoint | Health check endpoint with DB and disk checks | Must | Planned |
| 005-fluentvalidation-integration | FluentValidation auto-validation with 422 responses | Must | Planned |

---

## Dependencies

### Depends On
None — this is the foundational unit.

### Depended By
| Unit | Reason |
|------|--------|
| 002-security-baselines | Uses middleware pipeline established here |
| 003-email-infrastructure | Uses Serilog logging and error handling |
