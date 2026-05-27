---
name: code-review
description: Code review checklist and quality standards for FotoTipar. Use this skill when reviewing pull requests, evaluating code quality, or checking for common issues in both Angular frontend and ASP.NET Core backend code.
---

## Review Checklist

### Security (OWASP Top 10)

- [ ] No SQL injection — all queries parameterized or via EF Core LINQ
- [ ] No XSS — Angular auto-escapes by default; verify no `bypassSecurityTrust*` misuse
- [ ] Authentication enforced — `[Authorize]` on all protected endpoints
- [ ] Authorization checked — users can only access their own data (IDOR check)
- [ ] File uploads: MIME validated by magic bytes, UUID filenames, no path traversal
- [ ] Sensitive data not logged (passwords, tokens, card numbers)
- [ ] CORS whitelist uses exact origins, not wildcards
- [ ] Rate limiting on auth endpoints (login, register, password reset)
- [ ] CSRF: API uses JWT Bearer (immune) or guest token header (immune)
- [ ] Secrets not hardcoded — use environment variables or Secret Manager

### Backend (C# / ASP.NET Core)

- [ ] Controllers are thin — logic in services
- [ ] Proper async/await usage (no `.Result` or `.Wait()`)
- [ ] DTOs used for input/output — entities never exposed to API
- [ ] FluentValidation rules cover all input constraints
- [ ] Exceptions are meaningful (`NotFoundException`, not generic `Exception`)
- [ ] `using` or DI-managed disposable resources
- [ ] EF Core queries use `AsNoTracking()` for reads
- [ ] No N+1 query problems (check `.Include()` usage)
- [ ] Database migrations are additive (no destructive changes in production)
- [ ] Logging includes `correlationId` and structured data

### Frontend (Angular / TypeScript)

- [ ] Components use `OnPush` change detection where appropriate
- [ ] Subscriptions managed — `async` pipe or explicit unsubscribe
- [ ] Forms validate on blur and submit with Romanian error messages
- [ ] Loading states shown during API calls (spinner or disabled button)
- [ ] HTTP errors handled — toast for 5xx, field errors for 4xx
- [ ] No direct DOM manipulation — use Angular bindings
- [ ] Lazy loading maintained — no eager imports of feature modules
- [ ] `trackBy` used in all `*ngFor` loops
- [ ] Template expressions are pure (no method calls in templates, use pipes)
- [ ] SCSS follows component scoping (no global style leaks)

### General Quality

- [ ] Code follows project naming conventions (Romanian UI, English code)
- [ ] No commented-out code or `TODO` left without a linked issue
- [ ] No `console.log` or `Debug.WriteLine` in production code
- [ ] Unit tests cover happy path AND key error cases
- [ ] Test names follow convention: `MethodName_State_Expected`
- [ ] No magic numbers — use named constants
- [ ] Functions do one thing and are under ~40 lines
- [ ] No circular dependencies between modules
- [ ] API response shapes match frontend DTOs
- [ ] Currency amounts are `decimal` (C#) / `number` with 2-decimal formatting (TS)

### Performance

- [ ] Database queries have appropriate indexes
- [ ] No unbounded result sets — pagination required for list endpoints
- [ ] Images served as thumbnails for previews, not full resolution
- [ ] Bundle size checked — no unnecessary imports
- [ ] API calls deduplicated — no duplicate requests on component init

### Pull Request Standards

- PR title: `[US-XXX] Short description`
- PR description: what changed, why, how to test
- Max 400 lines changed per PR (split larger work)
- All CI checks must pass before merge
- At least one approval required
- Squash merge to main branch
