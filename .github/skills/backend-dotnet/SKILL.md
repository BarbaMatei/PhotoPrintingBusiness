---
name: backend-dotnet
description: ASP.NET Core 8 backend development conventions and patterns for FotoTipar. Use this skill when building API controllers, services, middleware, EF Core entities, validators, or any C# backend code.
---

## Tech Stack

- **ASP.NET Core 8** Web API
- **Entity Framework Core 8** (Code-First with PostgreSQL)
- **FluentValidation** for request validation
- **Serilog** for structured logging
- **SignalR** for real-time admin notifications

## Project Structure

```
src/PhotoPrint.API/
  Controllers/       → API controllers (thin, delegate to services)
  Services/          → Business logic (interfaces + implementations)
  Models/            → EF Core entities
  DTOs/              → Request + Response DTOs (per endpoint group)
  Validators/        → FluentValidation validators
  Hubs/              → SignalR hubs
  Middleware/        → Exception handling, correlation ID, security headers
  BackgroundJobs/    → IHostedService implementations
  EmailTemplates/    → Razor .cshtml templates
  Migrations/        → EF Core auto-generated
  Exceptions/        → Custom exception types
  Filters/           → Action filters (validation, etc.)
  Configuration/     → Settings POCO classes
```

## Coding Conventions

### Controllers

- **Thin controllers**: validate input → call service → return result
- Use `[ApiController]` attribute for automatic model binding and ProblemDetails
- Route prefix: `[Route("api/[controller]")]`
- Return `ActionResult<T>` with specific status codes
- Group related endpoints in one controller (e.g., `AuthController` for all auth endpoints)
- Use `[Authorize]`, `[Authorize(Roles="Admin")]`, or `[AllowAnonymous]` attributes

### Services

- Define `IService` interface + `Service` implementation
- Register in DI as scoped (per-request)
- Services contain business logic; controllers are entry points only
- Throw custom exceptions (`NotFoundException`, `ConflictException`) — middleware converts to HTTP responses

### Entity Framework

- Code-First approach with migrations
- All IDs are **UUID (Guid)** — use `Guid.NewGuid()` for generation
- Entities in `Models/` folder — POCO classes with navigation properties
- DbContext: `PhotoPrintDbContext` with `DbSet<T>` for each entity
- Use `IQueryable` for composable queries; materialize with `ToListAsync()`
- **Never use raw SQL with user input** — always parameterized queries
- Indexes: add composite indexes on frequently filtered columns (Status, CreatedAt)
- Soft delete pattern: `DeletedAt` nullable DateTime column where applicable

### DTOs

- Separate Request and Response DTOs — never expose entities directly
- Use records for immutable DTOs: `public record RegisterRequest(string Email, ...)`
- Map with manual mapping or a lightweight mapper (no AutoMapper unless needed)

### Validation

- FluentValidation for all request DTOs
- Register validators with `AddFluentValidationAutoValidation()`
- Custom `ValidationFilter` returns 422 with `[{field, message}]` array
- Validation messages in **Romanian**

### Authentication & Authorization

- JWT RS256 with 15-min access token
- Refresh token: opaque UUID, SHA-256 hashed in DB, 30-day expiry, rotated on use
- Refresh token in HttpOnly Secure SameSite=Strict cookie
- Guest sessions: `X-Guest-Token` header, validated by custom `GuestAuthenticationHandler`
- Dual auth: endpoints accept EITHER Bearer JWT OR X-Guest-Token

### Error Handling

- `ExceptionHandlerMiddleware`: catches all unhandled exceptions → ProblemDetails (RFC 7807)
- Custom exceptions: `NotFoundException` (404), `ConflictException` (409), `ForbiddenException` (403)
- Include `correlationId` in all error responses
- Never expose stack traces or internal details in production

### Logging

- Serilog with structured JSON logging
- Log levels: Information for business events, Warning for expected errors, Error for unexpected
- Include `correlationId` in all log entries
- Sensitive data (passwords, tokens) NEVER logged

### Configuration

- Non-secret config in `appsettings.json`
- Secrets: .NET Secret Manager (dev) / environment variables (prod)
- Config POCO classes in `Configuration/` bound via `IOptions<T>`

## Security Requirements

- HTTPS enforced; HSTS in production
- CORS: exact origin whitelist only
- Rate limiting: 100 req/min public, 10/min auth endpoints
- File uploads: validate MIME by magic bytes, UUID filenames, no path traversal
- Security headers: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`
- Passwords: PBKDF2-SHA256, 10,000 iterations (ASP.NET Identity default)

## Database

- **PostgreSQL** (via docker-compose in development)
- EF Core provider: `Npgsql.EntityFrameworkCore.PostgreSQL`
- Connection string in environment variable `ConnectionStrings__DefaultConnection`

## API Response Patterns

```csharp
// Success
return Ok(dto);                    // 200
return Created(uri, dto);          // 201
return NoContent();                // 204

// Errors (via exceptions → middleware)
throw new NotFoundException("Order not found");       // 404
throw new ConflictException("Email already exists");  // 409
// Validation errors handled by FluentValidation → 422
```
