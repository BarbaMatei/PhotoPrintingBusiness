# Tech Stack

*(Rewritten 2026-07-14 from the code. Descriptive — states what IS, not what is planned.)*

## Frontend — `src/PhotoPrint.UI`

- **Angular 21.2** — fully standalone (no NgModules), effectively **zoneless** (no zone.js
  polyfill; OnPush + signals + `toSignal`), SPA-only (no SSR). TypeScript **5.9** strict,
  RxJS 7.8.
- **Testing: Vitest 4** via the Angular builder (`@angular/build:unit-test`, jsdom). Karma and
  Jasmine are not used.
- **Tooling: Prettier only.** There is no ESLint (no config, no lint script, no CI step) —
  adding it is future work, not current reality.
- **E2e: Playwright 1.62** (`@playwright/test`, Chromium only), specs in `src/PhotoPrint.UI/e2e/`
  — outside `src/`, so the Vitest builder never collects them; `npm run e2e` runs them,
  `npm run e2e:check` type-checks them through `tsconfig.e2e.json`. Three smoke specs cover the
  real-money paths (guest checkout to review, admin login, admin real-time order update). They
  need a booted stack: `docker compose -f docker-compose.yml -f docker-compose.e2e.yml -p
  fototipar-e2e up -d --build`, then the same invocation with `run --rm api --seed-dev`.
- **Bundle budgets** (`angular.json`, production configuration): `initial` warns at 400 kB and
  fails at 500 kB against a measured 331.99 kB (2026-09-04); `anyComponentStyle` warns at 4 kB
  and fails at 16 kB. The 4 kB warning is a reduction target, not a gate — six built stylesheets
  exceed it today. Reduction target for `initial`: under 300 kB.
- Runtime libraries actually imported:
  - `@stripe/stripe-js` — lazy `import()` in the checkout payment step (Stripe Elements).
  - `leaflet` — lazy `import()` in the Easybox locker map (OpenStreetMap tiles).
  - `chart.js` (direct, **not** ng2-charts) — admin dashboard charts.
  - `@microsoft/signalr` v10 — admin order notifications hub client.
  - **No HEIC library** — HEIC support was removed (042); accepted uploads are JPG/PNG.

## Backend — `src/PhotoPrint.API`

- **ASP.NET Core 8 Web API** (.NET 8 / C# 12), single deployable, can also serve the SPA.
- Data: **EF Core 8** with `Npgsql.EntityFrameworkCore.PostgreSQL` — the only provider, in
  every environment; see [data-stack.md](data-stack.md).
- Key libraries: `FluentValidation` (all request validation — data annotations prohibited,
  ADR-002) · `Serilog.AspNetCore` + enrichers, compact JSON · `Stripe.net` ·
  `AWSSDK.S3` (R2/S3/MinIO via one client) · `Polly` (S3 transient retry) ·
  `SixLabors.ImageSharp` (image pipeline + bomb guards) · `RazorLight` (email templates) ·
  `MailKit` (SMTP dev) + `SendGrid` (prod) · `Microsoft.AspNetCore.Authentication.JwtBearer` ·
  `Microsoft.AspNetCore.Identity` (**only** `PasswordHasher<User>` — PBKDF2; not the Identity
  stack).
- Tests: xUnit + Moq + FluentAssertions + `Xunit.SkippableFact` (MinIO-gated S3 suite).

## Authentication (summary — details in system-architecture.md)

JWT RS256 (15-min access, claims-based roles) + rotated 30-day refresh token in an HttpOnly
SameSite=Strict cookie + Google OAuth (server-side id_token verification) + guest sessions via
`X-Guest-Token` (`DualAuth` policy). No refresh/silent-renew flow exists in the SPA.

## Infrastructure & delivery

- **Dev**: a local PostgreSQL 16 server (`localhost:5432`); optional MinIO for real-S3 tests
  (`STORAGE_TEST_*` env vars); SMTP dev default `localhost:1025` (MailHog-style).
- **CI (GitHub Actions)**: `ci.yml` — .NET build + test (a MinIO container un-skips the S3
  suite; a postgres:16 service backs the relational tests via `POSTGRES_TEST_CONNECTION`) and UI
  Vitest + production build; no lint step. `secret-scan.yml` — gitleaks on every push/PR.
  `deploy.yml` — on green main CI: build/push `ghcr.io/<owner>/fototipar/api` image, SSH
  `docker compose pull/up` (self-skips without a `DEPLOY_HOST` secret).
  `playwright-e2e.yml` — on PRs and non-main pushes, skipping documentation-only changes: boots
  API + PostgreSQL through `docker-compose.e2e.yml`, seeds, runs the three Playwright specs.
  It is **advisory**, not a required check, and `deploy.yml` chains off `ci.yml` alone, so a
  merge to main can deploy without the smoke suite having run on the merge commit.
  Because the image build runs the SPA's production build, a bundle over budget fails this
  workflow and `deploy.yml` too, not only `ci.yml`'s `web` job.
- **Prod shape**: Docker Compose (`docker-compose.prod.yml`) + Caddy (`Caddyfile`) for TLS;
  PostgreSQL 16; cloud storage target recommendation is **Cloudflare R2** (ADR-009 — $0
  egress; S3/MinIO equally supported by config).
- Package managers: npm (UI, node 22 in CI), NuGet (API).
