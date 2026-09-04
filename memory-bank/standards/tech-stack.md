# Tech Stack

*(Verified 2026-09-04 against `package.json`, `angular.json`, both `.csproj` files and the
workflow files in this repo. Descriptive — states what IS, not what is planned. Anything here
that cannot be traced to one of those files is a bug in this document.)*

## Frontend — `src/PhotoPrint.UI`

- **Angular 21.2** — fully standalone (no NgModules), effectively **zoneless** (no zone.js
  polyfill; OnPush + signals + `toSignal`), SPA-only (no SSR). TypeScript **5.9** strict,
  RxJS 7.8.
- **Testing: Vitest 4** via the Angular builder (`@angular/build:unit-test`, jsdom 28). Karma
  and Jasmine are not used, and neither is installed.
- **Tooling: Prettier only.** There is no ESLint — no config, no `lint` script, no CI step — and
  no e2e framework. Adding either is future work, not current reality.
- Runtime libraries actually imported:
  - `@stripe/stripe-js` — lazy `import()` in the checkout payment step (Stripe Elements).
  - `leaflet` — lazy `import()` in the Easybox locker map (OpenStreetMap tiles).
  - `chart.js` (direct, **not** ng2-charts) — admin dashboard charts.
  - `@microsoft/signalr` v10 — admin order notifications hub client.
  - **No HEIC library** — accepted uploads are JPG/PNG.

## Backend — `src/PhotoPrint.API`

- **ASP.NET Core 8 Web API** (.NET 8 / C# 12), single deployable, can also serve the SPA.
- Data: **EF Core 8** with `Npgsql.EntityFrameworkCore.PostgreSQL` — the only provider, in
  every environment; see [data-stack.md](data-stack.md).
- Request handling: `FluentValidation.AspNetCore` (all request validation — data annotations
  prohibited, ADR-002) · `Polly` and `Polly.RateLimiting` (S3 transient retry; outbound courier
  rate limiting) · `Microsoft.AspNetCore.Authentication.JwtBearer` ·
  `Microsoft.AspNetCore.Identity` (**only** `PasswordHasher<User>` — PBKDF2; not the Identity
  stack).
- Domain libraries: `Stripe.net` · `AWSSDK.S3` (R2/S3/MinIO through one client) ·
  `SixLabors.ImageSharp` (image pipeline + bomb guards) · `QuestPDF` (invoice PDF rendering —
  PuppeteerSharp is forbidden without a superseding ADR, ADR-021) · `RazorLight` (email
  templates) · `MailKit` and `SendGrid` (see Email below).
- Logging and observability: `Serilog.AspNetCore` + `Serilog.Enrichers.Environment` /
  `Serilog.Enrichers.Thread` / `Serilog.Formatting.Compact` (compact JSON) ·
  **OpenTelemetry** — `Extensions.Hosting`, ASP.NET Core / HTTP / EF Core / Runtime
  instrumentation, OTLP and Console exporters, and the Prometheus ASP.NET Core exporter ·
  **`Sentry.AspNetCore`**. The Prometheus exporter and the EF Core instrumentation are pinned to
  `-beta.1` builds because no stable line has ever been published for either.
- Design-time: `Microsoft.EntityFrameworkCore.Design` (migration scaffolding; private assets).
- Tests: xUnit + `xunit.runner.visualstudio` + `Microsoft.NET.Test.Sdk` + Moq +
  FluentAssertions + `Xunit.SkippableFact` (the MinIO-gated S3 suite) +
  `Microsoft.AspNetCore.Mvc.Testing` + `Microsoft.EntityFrameworkCore.InMemory` +
  `coverlet.collector`, plus three `Microsoft.Extensions.Configuration*` packages pinned at
  `10.0.8` — two majors ahead of this `net8.0` project, which resolves but is worth knowing
  before debugging a binding oddity.
- **Romanian invoice rendering needs three things to agree**, and dropping any one makes every
  invoice PDF render throw: `<InvariantGlobalization>false</InvariantGlobalization>` in the API
  csproj, `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false` in the `Dockerfile`, and the
  `icu-libs icu-data-full` packages the `Dockerfile` installs — the `aspnet:8.0-alpine` base
  image ships no ICU at all, and `icu-libs` alone carries the English-only data set with no
  `ro-RO` locale.
- Package versions are pinned inline in each `.csproj`; as of 2026-09-04 there is no
  `Directory.Packages.props` and no `Directory.Build.props`. Central package management arrives
  with bolt 054.

## Authentication (summary — details in system-architecture.md)

JWT RS256 (15-min access, claims-based roles) + rotated 30-day refresh token in an HttpOnly
SameSite=Strict cookie + Google OAuth (server-side id_token verification) + guest sessions via
`X-Guest-Token` (`DualAuth` policy). No refresh/silent-renew flow exists in the SPA.

## Email

The provider is **required configuration**, not an environment inference. `Email:Provider` must
be `Smtp` (MailKit) or `SendGrid`; boot throws on a missing or unrecognised value, and on
`SendGrid` without `Email:SendGrid:ApiKey`. Exactly one sender is registered, and both code
paths ship in every build — neither is a development-only artefact. The values that ship:
`appsettings.Development.json` sets `Smtp` (dev default `localhost:1025`, MailHog-style) and
`appsettings.json` sets `SendGrid`; either provider works in either environment if configured.

## Infrastructure & delivery

- **Dev**: a PostgreSQL 16 server the test helper can reach — either at `localhost:5432` as
  `postgres`/`postgres`, or wherever `POSTGRES_TEST_CONNECTION` points, and in both cases as a
  role allowed to `CREATE DATABASE`. Optional MinIO for real-S3 tests (`STORAGE_TEST_*` env
  vars). Which tests need which service, and what happens when they are absent:
  [../../docs/KNOWN_FAILURES.md](../../docs/KNOWN_FAILURES.md).
- **CI (GitHub Actions)**: `ci.yml` — .NET build + test (a MinIO container un-skips the S3
  suite; a `postgres:16-alpine` service backs the relational tests via
  `POSTGRES_TEST_CONNECTION`) and UI Vitest + production build on node 22; no lint step. It
  runs on pull requests and on pushes to every branch **except** `main`.
  `secret-scan.yml` — gitleaks on every push/PR. `deploy.yml` — builds and pushes the
  `ghcr.io/<owner>/fototipar/api` image, then SSH `docker compose pull/up` (self-skips without a
  `DEPLOY_HOST` secret). Its `workflow_run` trigger is filtered to `branches: [main]` while
  `ci.yml` ignores pushes to `main`, so in practice only `workflow_dispatch` reaches it — a
  manual deploy, not the automatic one the file's own comment describes.
- **Prod shape**: Docker Compose (`docker-compose.prod.yml`) + Caddy (`Caddyfile`) for TLS;
  PostgreSQL 16; cloud storage target recommendation is **Cloudflare R2** (ADR-009 — $0
  egress; S3/MinIO equally supported by config).
- Package managers: npm (UI, node 22 in CI), NuGet (API).
- Scaling posture: single instance. What breaks on a second replica, and what each ADR decided:
  [../../docs/architecture/multi-replica-readiness.md](../../docs/architecture/multi-replica-readiness.md).

## Keeping this document true

This file, `system-architecture.md` and `data-stack.md` are descriptive: a change that alters
reality updates the standard that states it, in the same change. The quarterly sweep that
catches what slipped through is
[../../docs/ARCHITECTURE_AUDIT_CHECKLIST.md](../../docs/ARCHITECTURE_AUDIT_CHECKLIST.md).
