---
stage: implement
bolt: 040-containers-and-pipelines
created: 2026-05-27T10:10:00Z
---

## Implementation Walkthrough: 001-containers-and-pipelines

### Summary

Containerised the app as one image (API serves the built Angular SPA), added dev + prod
Compose stacks with a Caddy auto-TLS edge, CI (build/test) and CD (build/push GHCR + deploy)
GitHub Actions workflows, a full `.env.example`, and a dedicated deployment runbook with
hosting proposals. The only compiled change is in `Program.cs`: serve the SPA, apply Postgres
migrations at boot, and fail fast on missing payment secrets in Production.

### Structure Overview

- **One image, three stages** (`Dockerfile`): publish the API (SDK), build the SPA (Node), assemble a non-root Alpine runtime that serves the SPA from `wwwroot` and the API on `:8080`.
- **Two Compose files**: `docker-compose.yml` (dev: API + Postgres + MailHog) and `docker-compose.prod.yml` (prod: Caddy → API, managed Postgres by default).
- **Two pipelines**: `ci.yml` gates PRs/branches; `deploy.yml` chains off green CI on `main`, publishes to GHCR, and deploys over SSH (self-skips until a host is configured). `secret-scan.yml` (bolt 041) is left standalone (decision D4).
- **Config as env**: `.env.example` is the authoritative variable list; required Production secrets are validated at boot.

### Completed Work

- [x] `Dockerfile` — multi-stage (api-build / ui-build / runtime); non-root `app` user; curl `/health` HEALTHCHECK; SPA copied to `wwwroot` (D1).
- [x] `.dockerignore` — trims build context; excludes secrets, `.env*` (keeps `.env.example`), build output, docs/planning.
- [x] `docker-compose.yml` — dev stack; Postgres healthcheck-gated; MailHog; named volumes for DB + uploads.
- [x] `docker-compose.prod.yml` — Caddy + API; API not host-exposed; managed-DB default with a commented all-in-one `db`; image tag parameterised via `${IMAGE_TAG}`.
- [x] `Caddyfile` — TLS termination + HSTS/security headers + gzip/zstd + access logs; LE-staging hint for pre-cutover.
- [x] `.env.example` — every var with real section names (`JwtSettings:*`, `Stripe:*`, `EuPlatesc:*`, …) and the `:`→`__` convention.
- [x] `.gitignore` — added `!.env.example` so the template is committed despite the `.env.*` ignore.
- [x] `.github/workflows/ci.yml` — `dotnet` job (restore/build/test, NuGet cache, Postgres service, trx artefact) + `web` job (npm ci/lint/test/build) against the **root** `PhotoPrint.sln`.
- [x] `.github/workflows/deploy.yml` — `workflow_run` after CI on `main`; build+push `:sha-xxxx` and `:latest` to GHCR with buildx GHA cache; SSH deploy gated on `DEPLOY_HOST` (self-skips with a notice otherwise).
- [x] `docs/DEPLOYMENT.md` — **new runbook**: hosting proposals (single-VM / managed / k8s) + recommendation, provisioning, GitHub secrets, deploy flow, migrations note, verification checklist, rollback, troubleshooting.
- [x] `README.md` — "Run with Docker" section + expanded env matrix + link to the runbook.
- [x] `src/PhotoPrint.API/Program.cs` — three changes (below).

### Program.cs changes (the only compiled change)

- [x] **D1 — serve the SPA**: `UseDefaultFiles()` + `UseStaticFiles()`, and `MapFallbackToFile("index.html")` **only when `wwwroot/index.html` exists**. API-only dev/test keep their 404 behaviour (fallback not registered).
- [x] **D2 — Postgres boot-migrate**: the DB-init block runs `Database.Migrate()` **guarded by `IsNpgsql()`**, so the InMemory test host is a no-op.
- [x] **Story 006 — fail fast**: Stripe/EuPlatesc moved to `AddOptions<T>().Bind(...).Validate(...).ValidateOnStart()`, **Production-gated** so the Testing host and local dev (no live keys) start normally.

### Key Decisions

- **D1 combined image, but fallback is file-gated** — avoids changing test/dev 404 semantics and avoids serving `index.html` for unknown `/api` routes when no SPA is bundled.
- **`IsNpgsql()`/`IsPostgres()` provider detection** rather than the `DatabaseProvider` string for the migrate guard — robust against the Testing host (which swaps to InMemory while the string still defaults to `Postgres`).
- **Production-gated payment validation** — delivers story 006's "missing required var fails boot" without breaking the 457-test suite (tests run under `Testing` and don't configure payment keys).
- **Standalone `secret-scan.yml`** kept (D4); CI references it rather than duplicating gitleaks.
- **Single combined image + managed Postgres** recommended to launch (D3 / runbook), parameterised so a managed-platform swap is config-only.

### Deviations from Plan

- The story templates assumed `src/PhotoPrint.sln`, `dist/photo-print-ui`, and `Jwt__*` keys. Corrected to the real `PhotoPrint.sln` (root), `dist/PhotoPrint.UI/browser`, and `JwtSettings:*`.
- Story 006 says "**each** `IOptions<T>` … `ValidateDataAnnotations()`". Applied selectively + Production-gated instead of a blanket sweep, because annotating every settings class with `[Required]` would crash the Testing host (which doesn't populate those sections). Recorded here intentionally.

### Dependencies Added

- No code/package dependencies. Infra pulls public images (`dotnet/{sdk,aspnet}:8.0-alpine`, `node:22-alpine`, `postgres:16-alpine`, `mailhog/mailhog`, `caddy:2-alpine`) and marketplace actions (`docker/*`, `appleboy/ssh-action`).

### Developer Notes

- **Migration hygiene gap (flagged, not fixed here):** `20260527075359_AddOrderIdempotencyKey` is Npgsql-typed (`TEXT`, plain unique index) amid otherwise-Npgsql migrations. `TEXT` is valid on Postgres so boot-migrate won't crash, but it should be verified/regenerated against a real Postgres before the first deploy — see `docs/DEPLOYMENT.md` §7. Out of scope for this bolt.
- API builds clean (0 errors; pre-existing Stripe NU1603 + EF1002 warnings only).
- **Stage 3 reality (D5):** Docker is not installed on this box, and GitHub Actions / a live site can't run locally. Stage 3 verification here = full `dotnet test` green (confirming the `Program.cs` changes don't regress the suite) + workflow/compose YAML sanity. Container build, CI/CD green runs, and the HTTPS check are operator steps in the runbook.
