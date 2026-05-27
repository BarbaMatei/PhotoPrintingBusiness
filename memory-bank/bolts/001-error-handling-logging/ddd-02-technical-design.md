---
unit: 001-error-handling-logging
bolt: 001-error-handling-logging
stage: design
status: complete
updated: 2026-05-05T15:45:00Z
---

# Technical Design - Error Handling & Logging

## Architecture Pattern

**Pattern**: Cross-cutting middleware pipeline (not a domain service layer)

This unit sits entirely in the **Infrastructure** and **Presentation** layers. There are no application-layer use cases or domain aggregates — the middleware components intercept requests/responses before they reach controllers. The pattern is ASP.NET Core's built-in middleware pipeline with custom `IMiddleware`, `IHealthCheck`, and `IActionFilter` implementations.

**Rationale**: Error handling, logging, and correlation tracking are orthogonal concerns that apply uniformly to all endpoints. Middleware is the natural ASP.NET Core mechanism for this. No additional architectural abstraction (CQRS, hexagonal) is needed for this bolt.

## Layer Structure

```text
┌─────────────────────────────────────────────────────────┐
│  Presentation (Middleware + Filters)                    │
│  ├── CorrelationIdMiddleware                            │
│  ├── ExceptionHandlerMiddleware                         │
│  └── ValidationFilter (IActionFilter)                   │
├─────────────────────────────────────────────────────────┤
│  Application (Health Checks)                            │
│  ├── DbHealthCheck : IHealthCheck                       │
│  └── DiskHealthCheck : IHealthCheck                     │
├─────────────────────────────────────────────────────────┤
│  Domain (Exceptions + Models)                           │
│  ├── Custom exception classes                           │
│  ├── ProblemDetails response model                      │
│  └── ValidationErrorResponse model                     │
├─────────────────────────────────────────────────────────┤
│  Infrastructure (Logging Configuration)                 │
│  └── Serilog setup (Program.cs / extension method)      │
└─────────────────────────────────────────────────────────┘
```

## File Structure

```text
src/PhotoPrint.API/
├── Middleware/
│   ├── CorrelationIdMiddleware.cs
│   └── ExceptionHandlerMiddleware.cs
├── Filters/
│   └── ValidationFilter.cs
├── Exceptions/
│   ├── NotFoundException.cs
│   ├── ConflictException.cs
│   ├── ForbiddenException.cs
│   └── UnauthorizedException.cs
├── HealthChecks/
│   ├── DbHealthCheck.cs
│   └── DiskHealthCheck.cs
├── Extensions/
│   ├── SerilogExtensions.cs
│   └── MiddlewareExtensions.cs
├── Configuration/
│   └── HealthCheckSettings.cs
├── appsettings.json          (Serilog + HealthCheck config sections)
└── Program.cs                (middleware registration order)
```

## API Design

### Health Check Endpoint

| Endpoint | Method | Auth | Request | Response |
|----------|--------|------|---------|----------|
| `GET /health` | GET | None (anonymous) | — | `HealthResponse` (200 always) |

**Response Schema — `GET /health`**:
```json
{
  "status": "Healthy",
  "checks": {
    "database": {
      "status": "OK",
      "duration": "00:00:00.042"
    },
    "disk": {
      "status": "OK",
      "freeGb": 52.3
    }
  }
}
```

**Note**: Uses ASP.NET Core built-in `HealthCheckOptions` with custom `ResponseWriter` that serializes to the above JSON shape. The endpoint always returns 200 — the `status` field indicates actual health. This allows monitoring tools to parse the body for degraded states.

### Error Response Contracts

**ProblemDetails (4xx/5xx):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Not Found",
  "status": 404,
  "detail": "Comanda nu a fost găsită.",
  "correlationId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

**Validation Errors (422):**
```json
{
  "errors": [
    { "field": "email", "message": "Adresa de email nu este validă." },
    { "field": "password", "message": "Parola trebuie să aibă minim 8 caractere." }
  ]
}
```

## Component Specifications

### 1. CorrelationIdMiddleware

**Type**: `IMiddleware` (registered as scoped)

**Behavior**:
1. Read `X-Correlation-Id` from request headers
2. If missing or not a valid GUID → generate `Guid.NewGuid()`
3. Store in `HttpContext.Items["CorrelationId"]`
4. Push to Serilog `LogContext` via `LogContext.PushProperty("CorrelationId", correlationId)`
5. Add `X-Correlation-Id` response header (using `context.Response.OnStarting` callback)
6. Call `await next(context)`

**Registration**: `app.UseMiddleware<CorrelationIdMiddleware>()` — first in pipeline

**DI**: Registered as `services.AddScoped<CorrelationIdMiddleware>()`

### 2. ExceptionHandlerMiddleware

**Type**: `IMiddleware` (registered as scoped)

**Behavior**:
1. Wrap `await next(context)` in try/catch
2. On exception, map to `(statusCode, title)` using dictionary:
   - `NotFoundException` → (404, "Not Found")
   - `ConflictException` → (409, "Conflict")
   - `ForbiddenException` → (403, "Forbidden")
   - `UnauthorizedException` → (401, "Unauthorized")
   - All others → (500, "Internal Server Error")
3. Log the exception:
   - 4xx: `LogWarning` with exception type, message, path
   - 5xx: `LogError` with full exception
4. Build ProblemDetails response:
   - `detail`: exception message for known types; generic Romanian message (`"A apărut o eroare neașteptată. Încearcă din nou."`) for 500 in production
   - `correlationId`: read from `HttpContext.Items["CorrelationId"]`
   - In Development: add `exception` property with type + message + stack trace
5. Set `Content-Type: application/problem+json` and write serialized JSON

**Registration**: `app.UseMiddleware<ExceptionHandlerMiddleware>()` — after CorrelationIdMiddleware

**JSON serialization**: `System.Text.Json` with `JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }`

### 3. ValidationFilter

**Type**: `IActionFilter` (registered globally)

**Behavior** (`OnActionExecuting`):
1. Check `context.ModelState.IsValid`
2. If invalid, extract errors from `ModelState`:
   - Key → `field` (convert PascalCase to camelCase using `JsonNamingPolicy.CamelCase.ConvertName`)
   - `ErrorMessage` → `message`
3. Build response: `{ "errors": [{ "field": "...", "message": "..." }] }`
4. Set `context.Result = new ObjectResult(response) { StatusCode = 422 }`
5. Short-circuit — controller action never executes

**Registration**: `services.AddControllers(options => options.Filters.Add<ValidationFilter>())`

**FluentValidation integration**: Requires `services.AddFluentValidationAutoValidation()` to populate ModelState automatically. Validators are registered via `services.AddValidatorsFromAssemblyContaining<Program>()`.

### 4. Custom Exception Classes

Each exception follows the same pattern:

```csharp
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
```

Four classes: `NotFoundException`, `ConflictException`, `ForbiddenException`, `UnauthorizedException`.

**Namespace**: `PhotoPrint.API.Exceptions`

### 5. Serilog Configuration

**Setup location**: `SerilogExtensions.AddSerilogLogging(builder)` extension method called in `Program.cs`

**Configuration source**: `appsettings.json` + `appsettings.{Environment}.json`

**Development** (`appsettings.Development.json`):
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Information"
      }
    },
    "WriteTo": [
      { "Name": "Console" }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

**Production** (`appsettings.json` defaults):
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.json",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

**NuGet packages**:
- `Serilog.AspNetCore`
- `Serilog.Formatting.Compact`
- `Serilog.Enrichers.Environment`
- `Serilog.Enrichers.Thread`

**Program.cs integration**:
```csharp
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));
// ...
app.UseSerilogRequestLogging();
```

### 6. Health Checks

**DbHealthCheck** (`IHealthCheck`):
- Inject `PhotoPrintDbContext`
- Call `Database.CanConnectAsync()` with `CancellationTokenSource(TimeSpan.FromSeconds(5))`
- Return `HealthCheckResult.Healthy("OK")` or `HealthCheckResult.Unhealthy("Error", exception)`

**DiskHealthCheck** (`IHealthCheck`):
- Inject `IOptions<HealthCheckSettings>` for `UploadsPath`
- Read `DriveInfo` for the drive containing `UploadsPath`
- Return `Healthy` with `freeGb` in data dictionary, or `Unhealthy` on error

**Registration**:
```csharp
builder.Services.AddHealthChecks()
    .AddCheck<DbHealthCheck>("database")
    .AddCheck<DiskHealthCheck>("disk");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});
```

**Custom ResponseWriter**: `HealthCheckResponseWriter.WriteAsync(HttpContext, HealthReport)` — formats the JSON response with `status`, `checks.{name}.status`, `checks.{name}.duration`, and `checks.disk.freeGb`.

## Data Persistence

No new database tables. This bolt only reads DB status via `Database.CanConnectAsync()`.

## Security Design

| Concern | Approach |
|---------|----------|
| Health endpoint auth | Anonymous — no auth required; returns only operational status, no sensitive data |
| Error detail leakage | Production: generic message for 500s; no stack traces. Development: full exception details |
| Correlation ID injection | Accept `X-Correlation-Id` from clients but validate it's a valid GUID; reject malformed values and generate new |
| Log injection | Serilog's structured logging with named properties prevents log injection (no string interpolation in message templates) |

## NFR Implementation

| Requirement | Design Approach |
|-------------|-----------------|
| Performance | Middleware adds <1ms overhead; health check DB timeout at 5s prevents hung checks |
| Reliability | Exception middleware catches ALL unhandled exceptions — no unhandled crashes; health endpoint always returns 200 |
| Observability | Every request gets a correlation ID; Serilog enriches all logs with CorrelationId, RequestPath, MachineName, ThreadId |
| Maintainability | Exception mapping is a static dictionary — easy to extend; FluentValidation validators auto-discovered from assembly |

## Error Handling Matrix

| Exception Type | HTTP Code | Title | Detail Source |
|----------------|-----------|-------|---------------|
| `NotFoundException` | 404 | Not Found | Exception message (Romanian) |
| `ConflictException` | 409 | Conflict | Exception message (Romanian) |
| `ForbiddenException` | 403 | Forbidden | Exception message (Romanian) |
| `UnauthorizedException` | 401 | Unauthorized | Exception message (Romanian) |
| `FluentValidation.ValidationException` | 422 | Validation Failed | Field-level errors array |
| Any other `Exception` | 500 | Internal Server Error | Dev: exception message; Prod: `"A apărut o eroare neașteptată. Încearcă din nou."` |

## External Dependencies

| Package | Purpose | Version Strategy |
|---------|---------|-----------------|
| `Serilog.AspNetCore` | Structured logging integration | Latest stable |
| `Serilog.Formatting.Compact` | JSON log formatter | Latest stable |
| `Serilog.Enrichers.Environment` | MachineName enrichment | Latest stable |
| `Serilog.Enrichers.Thread` | ThreadId enrichment | Latest stable |
| `FluentValidation.AspNetCore` | Auto-validation + DI registration | Latest stable |
| `AspNetCore.HealthChecks.NpgSql` | Not used — custom DbHealthCheck uses EF Core directly | N/A |

## Program.cs Registration Order

```csharp
// === Services ===
builder.Services.AddSerilogLogging(builder);  // Serilog config
builder.Services.AddScoped<CorrelationIdMiddleware>();
builder.Services.AddScoped<ExceptionHandlerMiddleware>();
builder.Services.AddControllers(options =>
    options.Filters.Add<ValidationFilter>());
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddHealthChecks()
    .AddCheck<DbHealthCheck>("database")
    .AddCheck<DiskHealthCheck>("disk");
builder.Services.Configure<HealthCheckSettings>(
    builder.Configuration.GetSection("HealthCheck"));

var app = builder.Build();

// === Middleware Pipeline (ORDER MATTERS) ===
app.UseMiddleware<CorrelationIdMiddleware>();   // 1st: correlation ID
app.UseMiddleware<ExceptionHandlerMiddleware>(); // 2nd: exception handler
app.UseSerilogRequestLogging();                  // 3rd: request logging
// [future: security headers, CORS, rate limiting — bolt 002]
// [future: authentication — epic 1]
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});
```

## Test Strategy Outline

| Component | Test Type | Key Scenarios |
|-----------|-----------|---------------|
| ExceptionHandlerMiddleware | Unit | Each exception type → correct status code + ProblemDetails; 500 hides detail in prod; includes correlationId |
| CorrelationIdMiddleware | Unit | Passes existing header; generates when missing; rejects invalid GUID; adds to response |
| ValidationFilter | Unit | Invalid ModelState → 422 with field errors; valid ModelState → no short-circuit |
| DbHealthCheck | Integration | DB up → Healthy; DB down → Unhealthy; timeout → Unhealthy |
| DiskHealthCheck | Unit | Valid path → freeGb; invalid path → Unhealthy |
| Health endpoint | Integration | GET /health returns 200 with expected JSON shape |
| Serilog | Integration | Verify CorrelationId appears in log output |
