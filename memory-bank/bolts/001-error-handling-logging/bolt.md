---
id: 001-error-handling-logging
unit: 001-error-handling-logging
intent: 001-foundation-infrastructure
type: ddd-construction-bolt
status: completed
stories:
  - 001-exception-handler-middleware
  - 002-correlation-id-middleware
  - 003-serilog-configuration
  - 004-health-check-endpoint
  - 005-fluentvalidation-integration
created: 2026-05-05T15:30:00Z
started: 2026-05-05T15:40:00Z
completed: 2026-05-05T16:30:00Z
current_stage: test
stages_completed:
  - name: domain-model
    completed: 2026-05-05T15:42:00Z
    artifact: ddd-01-domain-model.md
  - name: technical-design
    completed: 2026-05-05T15:45:00Z
    artifact: ddd-02-technical-design.md
  - name: adr-analysis
    completed: 2026-05-05T15:53:00Z
    artifacts:
      - adr-001-health-endpoint-200.md
      - adr-002-validation-filter-422.md
      - adr-003-correlation-id-trust.md
  - name: implement
    completed: 2026-05-05T16:10:00Z
    artifacts:
      - src/PhotoPrint.API/Exceptions/NotFoundException.cs
      - src/PhotoPrint.API/Exceptions/ConflictException.cs
      - src/PhotoPrint.API/Exceptions/ForbiddenException.cs
      - src/PhotoPrint.API/Exceptions/UnauthorizedException.cs
      - src/PhotoPrint.API/Middleware/CorrelationIdMiddleware.cs
      - src/PhotoPrint.API/Middleware/ExceptionHandlerMiddleware.cs
      - src/PhotoPrint.API/Filters/ValidationFilter.cs
      - src/PhotoPrint.API/HealthChecks/DbHealthCheck.cs
      - src/PhotoPrint.API/HealthChecks/DiskHealthCheck.cs
      - src/PhotoPrint.API/HealthChecks/HealthCheckResponseWriter.cs
      - src/PhotoPrint.API/Configuration/HealthCheckSettings.cs
      - src/PhotoPrint.API/Extensions/SerilogExtensions.cs
      - src/PhotoPrint.API/Extensions/MiddlewareExtensions.cs
      - src/PhotoPrint.API/Data/PhotoPrintDbContext.cs
      - src/PhotoPrint.API/Program.cs
      - src/PhotoPrint.API/appsettings.json
      - src/PhotoPrint.API/appsettings.Development.json
  - name: test
    completed: 2026-05-05T16:30:00Z
    artifact: ddd-03-test-report.md
    results:
      total: 23
      passed: 23
      failed: 0

requires_bolts: []
enables_bolts: [002-security-baselines, 003-email-infrastructure]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 2
---

# Bolt: 001-error-handling-logging

## Overview

Establish the global error handling, correlation tracking, structured logging, health check, and validation infrastructure for the ASP.NET Core backend. This is the first bolt to execute — all other backend bolts depend on the middleware pipeline established here.

## Objective

Create the middleware pipeline (ExceptionHandler, CorrelationId), configure Serilog, implement the health check endpoint, and integrate FluentValidation — producing a backend that returns consistent ProblemDetails errors, tracks correlation IDs, and logs structured JSON.

## Stories Included

- **001-exception-handler-middleware**: Exception handler returns ProblemDetails (Must)
- **002-correlation-id-middleware**: Correlation ID tracking per request (Must)
- **003-serilog-configuration**: Structured logging with Serilog (Must)
- **004-health-check-endpoint**: Health check endpoint with DB and disk checks (Must)
- **005-fluentvalidation-integration**: FluentValidation auto-validation with 422 responses (Must)

## Bolt Type

**DDD Construction Bolt** — 5 stages: Domain Model → Technical Design → Implementation → Testing → Review

## Dependencies

### Bolt Dependencies (within intent)
- None — this is the first bolt

### Unit Dependencies (cross-unit)
- None — foundational unit

### Enables (other bolts waiting on this)
- 002-security-baselines
- 003-email-infrastructure

## Expected Outputs

- `src/PhotoPrint.API/Middleware/ExceptionHandlerMiddleware.cs`
- `src/PhotoPrint.API/Middleware/CorrelationIdMiddleware.cs`
- `src/PhotoPrint.API/Exceptions/NotFoundException.cs` (and other custom exceptions)
- `src/PhotoPrint.API/Filters/ValidationFilter.cs`
- `src/PhotoPrint.API/HealthChecks/DbHealthCheck.cs`
- Serilog configuration in `Program.cs` and `appsettings.json`
- Unit tests for all middleware and filters
- Integration tests for health check and error responses
