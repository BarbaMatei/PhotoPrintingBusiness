# Coding Standards

*(Rewritten 2026-07-14 from the code. Descriptive — states what IS, not what is planned.)*

## Formatting & linting — current reality

### Backend (C#)
- 4-space indent, Allman braces, ~120-char lines, `System` usings first.
- Nullable reference types enabled.
- No analyzer/lint step in CI beyond the compiler.

### Frontend (TypeScript/SCSS)
- **Prettier** (`.prettierrc`) is the only formatter. 2-space indent, single quotes.
- **There is no ESLint** — no config, no `lint` script, no CI lint step (ci.yml says so
  explicitly). Do not "fix lint" that doesn't exist; adding ESLint is planned work, not current
  reality.
- `tsconfig` is strict: `strict`, `strictTemplates`, `strictInjectionParameters`,
  `noPropertyAccessFromIndexSignature`.

## Naming conventions

### Backend (C#) — unchanged and accurate

| Element | Convention | Example |
|---------|------------|---------|
| Classes / methods / properties | PascalCase | `AuthService`, `GetUserByIdAsync` |
| Interfaces | `I` + PascalCase | `IStorageService` |
| Private fields | `_camelCase` | `_logger` |
| Async methods | `Async` suffix | `RegisterAsync` |
| Constants | PascalCase | `MaxUploadSize` |

### Frontend (TypeScript) — as actually practiced (Angular 21)

| Element | Convention | Example |
|---------|------------|---------|
| Components | standalone class, kebab selector, short file names | `Header` in `header.ts` (`app-header`) |
| DI | **`inject()` only** — no constructor-parameter injection | `private auth = inject(AuthService)` |
| Service state | RxJS `BehaviorSubject`, `$$` private / `$` public | `isAuthenticated$$` / `isAuthenticated$` |
| Component state | **signals** (`signal`, `computed`); bridge services via `toSignal()` | `cartCount = toSignal(this.cart.itemCount$)` |
| Templates | `@if/@for/@switch` for new code (legacy `*ngIf/*ngFor` still exists in older templates — migrate opportunistically, don't mix within one template) | |
| Files | kebab-case, sibling `.spec.ts` | `auth.service.ts` / `auth.service.spec.ts` |

Component defaults: standalone, `OnPush` change detection (the app runs **zoneless** — no
zone.js polyfill; forgetting OnPush/signals means missed change detection, not just slowness),
external `templateUrl`/`styleUrl`, SCSS. UI strings are **Romanian**.

## File organization

### Backend — `src/PhotoPrint.API/`

```text
Authentication/    → guest-token auth handler
BackgroundJobs/    → hosted services (cleanup, promotion, recovery, email…)
Cli/               → CLI commands (e.g. backfill-archive) run via Program args
Configuration/     → settings POCOs
Controllers/       → thin controllers, delegate to Services/
Data/              → PhotoPrintDbContext, DbProviders
DTOs/ Exceptions/ Extensions/ Filters/ HealthChecks/ Hubs/ Middleware/
Migrations/        → single shared set — see data-stack.md before touching
Models/            → EF entities
Services/          → business logic (IService + Service pairs)
Validators/        → FluentValidation (data annotations are prohibited — ADR-002)
```

### Frontend — `src/PhotoPrint.UI/src/app/`

```text
core/       → guards, interceptors, models, pipes, services (singletons)
shared/     → reusable components, utils, validators, toast
features/   → lazy areas: account, admin, auth, cart, checkout, home, legal,
              orders, pricing, upload — each with pages/ (+ components/, *.routes.ts)
layout/     → header, footer
src/styles/ → _variables (tokens), _mixins, _buttons, _auth-forms partials
```

Routing: Romanian slugs, everything lazy (`loadComponent`/`loadChildren`); heavy libs
(leaflet, stripe-js) are additionally deferred via dynamic `import()` inside the component.

## Testing

### Backend (xUnit + Moq + FluentAssertions + Xunit.SkippableFact)

- Naming: `Method_Scenario_ExpectedOutcome`. Arrange-Act-Assert.
- Integration tests use the `WebApplicationFactory<Program>` family (`AuthFactory` base →
  feature factories). **Default DB is EF InMemory** — it cannot enforce unique indexes or
  check constraints; for relational behavior use the `PostgresPaymentFactory` pattern, or
  `PostgresTestDatabase` for a unit-level throwaway database. See data-stack.md
  *"what the test matrix proves"*.
- Real-S3 tests are `[SkippableFact]` gated on `STORAGE_TEST_*` env vars (set in CI's MinIO
  step; skipped locally unless you run MinIO).
- **The mocking rule (definition-of-done class 5): mock only at system boundaries** — network,
  external APIs, SMTP. The component under test's own collaborators (image processing, storage
  routing, DB behavior) must run REAL in at least one test of their guards. History: a fully
  mocked `IImageProcessor` made 490 green tests prove nothing about image handling (042-D25).
- Every failure mode named in the bolt's ddd-02 table has a test that goes red when the bug is
  injected. Fix-regression tests must fail on revert (the review loop checks this).
- There is **no clock abstraction** — code calls `DateTimeOffset.UtcNow` directly; time-window
  tests assert against real time. Factor this in before writing time-sensitive assertions.
- Each suite/report states **what it cannot prove** (Postgres semantics, real image decoding,
  live payment APIs) and where that gap is covered.

### Frontend (Vitest 4)

- Runner is Vitest via the Angular builder (`@angular/build:unit-test`); **there is no
  vitest.config file** — configuration lives in angular.json/tsconfig.spec.json. jsdom
  environment. `npm test` → `ng test`; CI adds `--watch=false`.
- Pattern: `TestBed.configureTestingModule` with standalone providers;
  `provideHttpClient(withInterceptors([...]))` + `provideHttpClientTesting()` +
  `HttpTestingController`; `provideRouter([])`; mocking via `vi.spyOn`/`vi.fn`.
- Sibling `*.spec.ts` files, co-located.

## Error handling

- Backend: custom exceptions (`NotFoundException` 404, `ConflictException` 409 per ADR-004,
  `ForbiddenException` 403, validation 422 per ADR-002) → `ExceptionHandlerMiddleware` →
  ProblemDetails with `correlationId`. Every exception type a dependency can throw is mapped or
  deliberately propagated, with a test (definition-of-done class 10).
- Frontend: `errorInterceptor` — 401: authenticated → logout + redirect to login; guest/anon →
  `clearGuestToken()` only (never redirect a guest to login). 403/5xx/network-0 → Romanian
  toasts. **There is no refresh-token / silent-renew flow** — deliberately deferred; don't
  assume one exists.
- Any change to interceptor/guard/session behavior must walk the user-type × token-state
  matrix (definition-of-done class 11) — this cluster produced more re-found defects than any
  other.

## Logging

- Serilog, structured JSON (compact formatter), enrichers for environment/thread; always
  `correlationId`.
- **Level floor is Information** — a `Debug` log line in production code paths effectively
  does not exist (042-D16/D84). New error/side-effect paths log at `Information`+ and must be
  distinguishable per incident type (definition-of-done class 6).
- Never log secrets, tokens, or PII; bound any user-controlled string you log.

## Comments

A last resort, kept to one short line. Never add a comment to narrate a change, a bug fix, or a
feature ("now handles X"). Only two reasons justify one:

- **Why non-obvious code exists** — state the constraint or gotcha itself, with **no reference**
  to the bolt, review, finding or decision id (`PPW-12`, `F3`, `D50`, `BUG-2`…), ADR, ticket, PR,
  or past discussion where it was decided. That history lives in the commit and, for review
  fixes, the resolution file.
- **A short behaviour description on an interface member** (`///`, JSDoc) — never on a concrete
  class, and never a restatement of the signature.

When you edit a file, delete the non-essential comments you pass through.

Enforced at commit time: `.githooks/pre-commit` lists every `//` or `///` line the commit adds to
a `.cs` or `.ts` file and refuses the commit. Delete the narration and recommit; only if every
listed line is genuinely allowed, re-run the exact same commit prefixed with `COMMENTS_OK=1` —
which the hook records in `reviews/state/overrides.jsonl`, where an unattended review run reads
it. Never `--no-verify`.

## Commit messages

Conventional style, **exactly one sentence, subject line only** — no body and no trailers (no
`Co-Authored-By`). Sole exception: a breaking change may carry a body. Name the bolt or finding
ids in the subject where they apply, e.g.
`fix(orders): guard duplicate AWB creation (PPW-284, review 015-v3)`.

Both rules above hold at every entry point, so `CLAUDE.md` states them for each session too;
this file is their home as a standard, and the two are edited together.
