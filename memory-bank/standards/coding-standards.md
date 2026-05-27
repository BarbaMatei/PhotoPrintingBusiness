# Coding Standards

## Overview
Consistent code style and quality standards for both the Angular frontend (TypeScript/SCSS) and ASP.NET Core backend (C#), optimized for AI code generation and team consistency.

## Code Formatting

### Backend (C#)
**Tool**: Built-in .NET formatting / EditorConfig
**Key Settings**:
- Indentation: 4 spaces
- Braces: Allman style (new line)
- Max line length: 120 characters
- Usings: sorted, `System` first

### Frontend (TypeScript/SCSS)
**Tool**: Angular CLI defaults (TSLint/ESLint + Prettier)
**Key Settings**:
- Indentation: 2 spaces
- Semicolons: always
- Quotes: single quotes
- Trailing commas: multi-line only
- Max line length: 120 characters

**Enforcement**: Angular CLI lint on save and in CI pipeline

## Linting

### Backend (C#)
**Tool**: .NET analyzers + nullable reference types enabled
**Strictness**: Strict
- Nullable reference types: enabled (`<Nullable>enable</Nullable>`)
- Treat warnings as errors in CI
- No unused variables or imports

### Frontend (TypeScript)
**Tool**: ESLint with Angular plugin
**Strictness**: Strict
- `strict: true` in tsconfig
- No `any` type — use `unknown` where type is uncertain
- No unused variables (error)
- No console.log in production code

## Naming Conventions

### Backend (C#)

| Element | Convention | Example |
|---------|------------|---------|
| Classes | PascalCase | `AuthService`, `OrderController` |
| Interfaces | `I` prefix + PascalCase | `IUserRepository`, `IEmailService` |
| Methods | PascalCase | `GetUserByIdAsync`, `RegisterAsync` |
| Properties | PascalCase | `Email`, `CreatedAt` |
| Variables | camelCase | `userName`, `isActive` |
| Constants | PascalCase | `MaxUploadSize`, `DefaultPageSize` |
| Private fields | `_camelCase` | `_userRepository`, `_logger` |
| Async methods | `Async` suffix | `RegisterAsync`, `GetOrdersAsync` |

**File Naming**: PascalCase matching class name — `AuthService.cs`, `OrderController.cs`

### Frontend (TypeScript)

| Element | Convention | Example |
|---------|------------|---------|
| Components | PascalCase class, kebab-case selector | `RegisterComponent`, `app-register` |
| Services | PascalCase | `AuthService`, `CartService` |
| Interfaces/Types | PascalCase (no `I` prefix) | `User`, `OrderSummary` |
| Variables | camelCase | `userName`, `isLoading` |
| Constants | UPPER_SNAKE_CASE | `MAX_FILE_SIZE`, `API_URL` |
| Observables | `$` suffix | `user$`, `orders$` |
| Booleans | `is`/`has`/`can` prefix | `isActive`, `hasPermission` |

**File Naming**: kebab-case — `register.component.ts`, `auth.service.ts`, `order-summary.model.ts`

## File Organization

### Backend Pattern: Type-based with domain context

```text
src/PhotoPrint.API/
  Controllers/       → API controllers (thin, delegate to services)
  Services/          → Business logic (IService + Service pairs)
  Models/            → EF Core entities (POCO classes)
  DTOs/              → Request + Response DTOs per feature
  Validators/        → FluentValidation validators
  Hubs/              → SignalR hubs
  Middleware/         → Exception handling, correlation ID, security headers
  BackgroundJobs/    → IHostedService implementations
  EmailTemplates/    → Razor .cshtml templates
  Migrations/        → EF Core auto-generated
  Exceptions/        → Custom exception types
  Filters/           → Action filters
  Configuration/     → Settings POCO classes
```

### Frontend Pattern: Feature-based with shared core

```text
photo-print-fe/src/app/
  core/              → Singletons: services, guards, interceptors, models
  shared/            → Reusable components, pipes, directives
  features/          → Lazy-loaded feature modules
    auth/
    upload/
    checkout/
    orders/
    account/
    admin/
    legal/
  environments/      → Environment config files
```

**Conventions**:
- Tests: co-located `.spec.ts` (frontend), separate `Tests/` project (backend)
- Types: co-located in `core/models/` (frontend), `DTOs/` and `Models/` (backend)
- One component per file; co-locate `.ts`, `.html`, `.scss`, `.spec.ts`

## Testing Strategy

### Backend (xUnit)

**Framework**: xUnit + Moq + FluentAssertions
**Coverage Target**: 80%+ line coverage (services), 100% rule coverage (validators)

| Type | Tool | Location |
|------|------|----------|
| Unit | xUnit + Moq | `src/PhotoPrint.Tests/Unit/` |
| Integration | xUnit + WebApplicationFactory + Testcontainers | `src/PhotoPrint.Tests/Integration/` |

**Conventions**:
- Test naming: `MethodName_StateUnderTest_ExpectedBehavior`
- Structure: Arrange-Act-Assert
- Mock all dependencies via interfaces
- Use `InMemoryDatabase` for simple EF queries; Testcontainers (real PostgreSQL) for complex queries

### Frontend (Jasmine/Karma)

**Framework**: Jasmine + Karma (Angular default)
**Coverage Target**: 70%+ branch coverage (components), 80%+ (services)

| Type | Tool | Location |
|------|------|----------|
| Unit | Jasmine + Karma | Co-located `.spec.ts` files |
| E2E | Cypress or Playwright | `e2e/` folder |

**Conventions**:
- Use `TestBed` for component tests
- Mock services with `jasmine.createSpyObj()`
- Use `HttpClientTestingModule` for HTTP service tests
- Use `data-testid` attributes for stable E2E selectors

### Test Data
- Use factories/builders for test data creation
- Seed realistic Romanian test data (names, addresses, phone numbers)
- Never use production data in tests

### CI Integration
- Unit tests: run on every PR
- Integration tests: run on merge to main
- E2E tests: nightly or before release

## Error Handling

### Backend Pattern: Custom exceptions + middleware

**Custom Exceptions**: `NotFoundException` (404), `ConflictException` (409), `ForbiddenException` (403)
**Global Handler**: `ExceptionHandlerMiddleware` catches all unhandled exceptions → ProblemDetails (RFC 7807)
**API Errors**: include `correlationId`, never expose stack traces in production

### Frontend Pattern: Interceptors + toast notifications

**HTTP Errors**: `ErrorInterceptor` handles 403/5xx with toast notifications
**Form Validation**: inline field errors shown on blur and submit
**Loading States**: spinner/disabled button during API calls

## Logging

### Backend
**Tool**: Serilog (structured JSON logging)
**Format**: JSON (structured)

| Level | Usage |
|-------|-------|
| Information | Business events (user registered, order placed) |
| Warning | Expected errors (validation failures, not found) |
| Error | Unexpected failures (unhandled exceptions, integration errors) |
| Debug | Detailed technical info (development only) |

**Rules**:
- Always log: API requests (method, path, status, duration), auth events, business events, errors with context
- Never log: passwords, tokens, API keys, PII without consent
- Always include `correlationId` in log entries

### Frontend
- No `console.log` in production — use `environment.production` guard
- Error interceptor logs to browser console in dev mode only
