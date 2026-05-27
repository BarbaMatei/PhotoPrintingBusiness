# US-801 — Global Error Handling & Logging (Backend)

## Story
**As a** system  
**I want to** ensure all errors return consistent responses and are logged for debugging

## Type
BACKEND — ASP.NET Core

## Epic
EPIC-8 | Platformă & Non-Funcționale

## Dependencies
- None (foundational — should be implemented first)

## Acceptance Criteria

1. **ExceptionHandlerMiddleware**: catches all unhandled exceptions; returns ProblemDetails (RFC 7807) with `type`, `title`, `status`, `detail`, `correlationId`
2. **FluentValidation errors**: `422` with `[{field, message}]` array
3. **Serilog**: structured JSON logging; console sink (dev) + file sink with daily rolling (prod)
4. **`GET /health`** — checks DB connectivity; returns `{status, db, diskFreeGb}`; public endpoint
5. **CorrelationId middleware**: reads `X-Correlation-Id` header or generates UUID; included in all responses and logs

## Technical Notes

### Implementation Details

#### Exception Handler Middleware
```csharp
// Returns RFC 7807 ProblemDetails for all unhandled exceptions
// Maps known exception types:
//   NotFoundException → 404
//   ValidationException → 422
//   UnauthorizedException → 401
//   ForbiddenException → 403
//   ConflictException → 409
//   All others → 500 (no internal details exposed in production)
```

#### Correlation ID Middleware
- Read `X-Correlation-Id` from request headers; if absent, generate `Guid.NewGuid()`
- Add to `HttpContext.Items` for service-layer access
- Add to response header `X-Correlation-Id`
- Push to Serilog `LogContext` for automatic inclusion in all log entries

#### Serilog Configuration
- NuGet: `Serilog.AspNetCore`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`
- Development: Console sink with human-readable output
- Production: File sink with JSON format, daily rolling, 30-day retention
- Enrich with: CorrelationId, MachineName, ThreadId, RequestPath

#### Health Check
- `GET /health` — public, no auth
- Check: EF Core DB connectivity (`Database.CanConnectAsync()`)
- Check: disk free space on uploads volume
- Response: `{ "status": "Healthy", "db": "OK", "diskFreeGb": 45.2 }`

#### FluentValidation Integration
- Configure in `Program.cs`: `AddFluentValidationAutoValidation()`
- Custom `ValidationFilter`: intercepts model state errors from FluentValidation, returns 422 with field-level error array

## Files to Create/Modify
- `src/PhotoPrint.API/Middleware/ExceptionHandlerMiddleware.cs`
- `src/PhotoPrint.API/Middleware/CorrelationIdMiddleware.cs`
- `src/PhotoPrint.API/Exceptions/` (NotFoundException, ConflictException, etc.)
- `src/PhotoPrint.API/Filters/ValidationFilter.cs`
- `src/PhotoPrint.API/HealthChecks/DbHealthCheck.cs`
- `Program.cs` (Serilog + middleware + health check registration)
- `appsettings.json` (Serilog config)

## Testing
- Unit test: exception middleware returns correct status codes
- Unit test: correlation ID generated when missing
- Unit test: correlation ID forwarded when provided
- Unit test: FluentValidation errors return 422
- Integration test: health check with DB connection
- Integration test: unhandled exception returns ProblemDetails
