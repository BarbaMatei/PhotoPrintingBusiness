# Tech Stack

*(Rewritten 2026-07-14 from the code. Descriptive — states what IS, not what is planned.)*

## Frontend — `src/PhotoPrint.UI`

- **Angular 21.2** — fully standalone (no NgModules), effectively **zoneless** (no zone.js
  polyfill; OnPush + signals + `toSignal`), SPA-only (no SSR). TypeScript **5.9** strict,
  RxJS 7.8.
- **Testing: Vitest 4** via the Angular builder (`@angular/build:unit-test`, jsdom). Karma and
  Jasmine are not used.
- **Tooling: Prettier only.** There is no ESLint (no config, no lint script, no CI step) and no
  e2e framework — adding either is future work, not current reality.
- Runtime libraries actually imported:
  - `@stripe/stripe-js` — lazy `import()` in the checkout payment step (Stripe Elements).
  - `leaflet` — lazy `import()` in the Easybox locker map (OpenStreetMap tiles).
  - `chart.js` (direct, **not** ng2-charts) — admin dashboard charts.
  - `@microsoft/signalr` v10 — admin order notifications hub client.
  - **No HEIC library** — HEIC support was removed (042); accepted uploads are JPG/PNG.

## Backend — `src/PhotoPrint.API`

- **ASP.NET Core 8 Web API** (.NET 8 / C# 12), single deployable, can also serve the SPA.
- Data: **EF Core 8** with BOTH `Npgsql.EntityFrameworkCore.PostgreSQL` (prod) and
  `Microsoft.EntityFrameworkCore.Sqlite` (dev/tests) — the dual-provider setup is load-bearing;
  see [data-stack.md](data-stack.md).
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

- **Dev**: SQLite file DB (no Docker needed for the API); optional MinIO for real-S3 tests
  (`STORAGE_TEST_*` env vars); SMTP dev default `localhost:1025` (MailHog-style).
- **CI (GitHub Actions)**: `ci.yml` — .NET build + test (with a MinIO container un-skipping the
  S3 suite; a postgres:16 service is provisioned but currently unused by tests) and UI Vitest +
  production build; no lint step. `secret-scan.yml` — gitleaks on every push/PR.
  `deploy.yml` — on green main CI: build/push `ghcr.io/<owner>/fototipar/api` image, SSH
  `docker compose pull/up` (self-skips without a `DEPLOY_HOST` secret).
- **Prod shape**: Docker Compose (`docker-compose.prod.yml`) + Caddy (`Caddyfile`) for TLS;
  PostgreSQL 16; cloud storage target recommendation is **Cloudflare R2** (ADR-009 — $0
  egress; S3/MinIO equally supported by config).
- Package managers: npm (UI, node 22 in CI), NuGet (API).
