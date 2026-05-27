---
unit: 001-error-handling-logging
bolt: 001-error-handling-logging
stage: model
status: complete
updated: 2026-05-05T15:42:00Z
---

# Static Model - Error Handling & Logging

## Bounded Context

This unit defines the **cross-cutting infrastructure** bounded context — middleware, error standardization, correlation tracking, health monitoring, and input validation. It does not own business entities but establishes patterns consumed by all other bounded contexts. The "domain" here is the HTTP request pipeline itself.

## Domain Entities

| Entity | Properties | Business Rules |
|--------|------------|----------------|
| **ProblemDetails** | `Type` (string, RFC 7807 URI), `Title` (string), `Status` (int, HTTP status code), `Detail` (string, Romanian message), `CorrelationId` (string, UUID) | Must conform to RFC 7807; `Detail` must be in Romanian; `Status` must match the mapped exception type; no internal details (stack trace, exception message) in production |
| **CorrelationContext** | `CorrelationId` (Guid) | If `X-Correlation-Id` request header present, reuse it; otherwise generate new UUID; must be included in all log entries and all response headers |
| **HealthStatus** | `Status` (string: "Healthy"/"Unhealthy"), `Db` (string: "OK"/"Error"), `DiskFreeGb` (decimal) | DB check must timeout after 5 seconds; disk check reads configured upload path; response is always returned (even if checks fail) |
| **ValidationError** | `Field` (string, camelCase property name), `Message` (string, Romanian error text) | All validation messages in Romanian; nested fields use dot notation (e.g., "address.city"); all errors returned in single response (not just first) |

## Value Objects

| Value Object | Properties | Constraints |
|--------------|------------|-------------|
| **ExceptionMapping** | `ExceptionType` (Type), `StatusCode` (int), `Title` (string) | Immutable mapping table: NotFoundException→404, ConflictException→409, ForbiddenException→403, UnauthorizedException→401, ValidationException→422, default→500 |
| **LogConfiguration** | `Environment` (string), `SinkType` (Console/File), `Format` (HumanReadable/JSON), `RetentionDays` (int) | Dev: Console+HumanReadable; Prod: File+JSON+30-day retention; enriched with CorrelationId, MachineName, ThreadId, RequestPath |

## Aggregates

This unit does not have traditional aggregates — it operates at the middleware/infrastructure layer. The "aggregate" is the **HTTP Request Pipeline**, which orchestrates the middleware components in a specific order.

| Aggregate Root | Members | Invariants |
|----------------|---------|------------|
| **MiddlewarePipeline** | CorrelationIdMiddleware, ExceptionHandlerMiddleware, ValidationFilter | Correlation ID must be set before exception handler runs; exception handler must catch all unhandled exceptions; validation filter must intercept FluentValidation model state errors |

## Domain Events

| Event | Trigger | Payload |
|-------|---------|---------|
| **UnhandledExceptionCaught** | Any unhandled exception reaches ExceptionHandlerMiddleware | ExceptionType, Message, CorrelationId, RequestPath, Timestamp |
| **ValidationFailed** | FluentValidation produces model state errors | Field[], Message[], CorrelationId |
| **HealthCheckCompleted** | GET /health endpoint called | Status, DbStatus, DiskFreeGb, Duration |

*Note: These are logical events logged via Serilog, not event-bus domain events.*

## Domain Services

| Service | Operations | Dependencies |
|---------|------------|--------------|
| **ExceptionHandlerMiddleware** | `InvokeAsync(HttpContext)` — wraps pipeline in try/catch, maps exception to ProblemDetails, writes JSON response | IHostEnvironment (to determine dev/prod), ILogger |
| **CorrelationIdMiddleware** | `InvokeAsync(HttpContext)` — reads/generates correlation ID, stores in HttpContext.Items, pushes to Serilog LogContext, adds to response header | ILogger |
| **ValidationFilter** | `OnActionExecuting(context)` — intercepts ModelState errors from FluentValidation, transforms to `{ errors: [{field, message}] }`, returns 422 | None |
| **DbHealthCheck** | `CheckHealthAsync(context)` — tests DB connectivity via `Database.CanConnectAsync()` with 5s timeout | PhotoPrintDbContext |
| **DiskHealthCheck** | `CheckHealthAsync(context)` — reads free space on configured uploads volume | IConfiguration (uploads path) |

## Repository Interfaces

This unit does not define repository interfaces — it operates at the middleware layer. The only data access is the health check's `Database.CanConnectAsync()` call, which uses the existing `PhotoPrintDbContext` directly.

## Ubiquitous Language

| Term | Definition |
|------|------------|
| **ProblemDetails** | RFC 7807 standard JSON format for HTTP API error responses. Contains `type`, `title`, `status`, `detail`, and custom extensions like `correlationId`. |
| **Correlation ID** | A UUID that uniquely identifies a single HTTP request across all middleware, services, and log entries. Propagated via `X-Correlation-Id` header. |
| **Middleware** | ASP.NET Core pipeline component that processes HTTP requests/responses. Executed in registration order for requests, reverse order for responses. |
| **ExceptionMapping** | The deterministic mapping from a custom exception type (e.g., NotFoundException) to an HTTP status code (e.g., 404) and ProblemDetails title. |
| **ValidationFilter** | An ASP.NET Core action filter that intercepts FluentValidation model state errors before the controller action executes, returning a 422 response with field-level error messages. |
| **Health Check** | A public endpoint (`GET /health`) that reports the system's operational status — database connectivity and available disk space — for monitoring tools. |
| **Structured Logging** | Logging where each entry is a structured object (JSON in production) with named properties (CorrelationId, RequestPath, etc.) rather than free-form text strings. |

## Custom Exception Hierarchy

```text
Exception
├── NotFoundException          → 404 Not Found
├── ConflictException          → 409 Conflict
├── ForbiddenException         → 403 Forbidden
├── UnauthorizedException      → 401 Unauthorized
└── (all others)               → 500 Internal Server Error
```

All custom exceptions extend `Exception` with a `string message` constructor. The `Detail` field in ProblemDetails uses the exception message (in Romanian). In production, unrecognized exceptions use a generic message: `"A apărut o eroare neașteptată. Încearcă din nou."`.

## Middleware Pipeline Order

```text
Request ──► CorrelationIdMiddleware
              ──► ExceptionHandlerMiddleware
                    ──► [Rate Limiting]    (future: bolt 002)
                          ──► [Auth]       (future: epic 1)
                                ──► Routing
                                      ──► ValidationFilter
                                            ──► Controller
```

*Brackets indicate future middleware not in this bolt's scope.*
