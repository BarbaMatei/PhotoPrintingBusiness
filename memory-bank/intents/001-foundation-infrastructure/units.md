# Unit Decomposition: 001-foundation-infrastructure

## Requirement-to-Unit Mapping

- **FR-1**: Exception Handler Middleware → `001-error-handling-logging`
- **FR-2**: Correlation ID Middleware → `001-error-handling-logging`
- **FR-3**: Structured Logging (Serilog) → `001-error-handling-logging`
- **FR-4**: Health Check Endpoint → `001-error-handling-logging`
- **FR-5**: FluentValidation Integration → `001-error-handling-logging`
- **FR-6**: HTTPS & HSTS → `002-security-baselines`
- **FR-7**: CORS Policy → `002-security-baselines`
- **FR-8**: Rate Limiting → `002-security-baselines`
- **FR-9**: Security Headers (incl. CSP) → `002-security-baselines`
- **FR-10**: Angular App Shell → `004-angular-app-shell`
- **FR-11**: Lazy-Loaded Route Groups → `004-angular-app-shell`
- **FR-12**: Route Guards → `004-angular-app-shell`
- **FR-13**: HTTP Interceptors → `004-angular-app-shell`
- **FR-14**: Environment Configuration → `004-angular-app-shell`
- **FR-15**: IEmailService Abstraction → `003-email-infrastructure`
- **FR-16**: Email Razor Templates → `003-email-infrastructure`
- **FR-17**: Email Retry Queue → `003-email-infrastructure`

## Units

| # | Unit | Type | Bolt Type | Stories | Dependencies |
|---|------|------|-----------|---------|-------------|
| 1 | 001-error-handling-logging | backend | ddd-construction-bolt | 5 | None |
| 2 | 002-security-baselines | backend | ddd-construction-bolt | 4 | 001-error-handling-logging |
| 3 | 003-email-infrastructure | backend | ddd-construction-bolt | 3 | 001-error-handling-logging |
| 4 | 004-angular-app-shell | frontend | simple-construction-bolt | 5 | None |

## Execution Order

```text
[001-error-handling-logging] ──► [002-security-baselines]
         │                       
         └──────────────────────► [003-email-infrastructure]

[004-angular-app-shell] (independent, can be built in parallel)
```
