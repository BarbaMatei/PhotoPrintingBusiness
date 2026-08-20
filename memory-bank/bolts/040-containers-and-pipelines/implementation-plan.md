---
stage: plan
bolt: 040-containers-and-pipelines
created: 2026-05-27T09:00:00Z
---

## Implementation Plan: 001-containers-and-pipelines

### Objective

After this bolt, a contributor can run the whole backend stack with `docker compose up`, every
PR runs build + tests in GitHub Actions, and a push to `main` produces a verified container image
deployed to production — with all secrets/config supplied as environment variables, never committed.

### Deliverables

1. **`Dockerfile`** (repo root) + **`.dockerignore`** — multi-stage, non-root `app` user, `HEALTHCHECK` on `/health`. (story 001)
2. **`docker-compose.yml`** (dev) — `api` + `db` (Postgres 16, healthcheck-gated) + `mailhog`; named `pgdata` volume. (story 002)
3. **`docker-compose.prod.yml`** + **`Caddyfile`** — Caddy edge (auto-TLS, HSTS, gzip) in front of `api`; API port not host-exposed. (story 003)
4. **`.github/workflows/ci.yml`** — `dotnet` job (restore/build/test against a Postgres service) + `web` job (npm ci/lint/test/build); artefacts uploaded. (story 004)
5. **`.github/workflows/deploy.yml`** — on CI success on `main`: build + push image to GHCR, deploy to the target. (story 005)
6. **`.env.example`** + **README "Environment matrix"** + **`ValidateOnStart()` wiring** on the settings classes; missing required var → clear boot failure. (story 006)

### Grounding corrections (verified against the real repo — stories were written from assumptions)

- Solution is **`PhotoPrint.sln` at the repo root**, not `src/PhotoPrint.sln`. CI/Docker paths use the root sln.
- Projects: `src/PhotoPrint.API`, `src/PhotoPrint.Tests`, `src/PhotoPrint.UI`.
- Angular uses the **`@angular/build:application`** builder → production output is **`dist/PhotoPrint.UI/browser`**, not `dist/photo-print-ui`.
- **The API does not serve the SPA today** — no `UseStaticFiles`/`MapFallbackToFile` in `Program.cs`. Baking the UI into `wwwroot` (story 001) is a *new* choice (see Decision D1).
- A **`DatabaseProvider`** config switch already exists (Postgres default / PostgreSQL). Compose + env matrix must set it explicitly.
- Bolt 041 already shipped `.gitignore` secret discipline, `secrets/`, `gen-dev-keys.{sh,ps1}`, and a standalone `secret-scan.yml`. This bolt builds on those (see Decision D4).

### Dependencies

- **Runtime/base images**: `mcr.microsoft.com/dotnet/{sdk,aspnet}:8.0`, `node:22-alpine`, `postgres:16-alpine`, `mailhog/mailhog`, `caddy`.
- **GitHub Actions**: `actions/checkout@v4`, `setup-dotnet@v4`, `setup-node@v4`, `cache@v4`, `docker/{setup-buildx,login,build-push}-action`, `appleboy/ssh-action` (VM path).
- **Existing in-repo**: `/health` endpoint (bolt from intent 001), `secrets/` + `gen-dev-keys` (bolt 041), `DatabaseProvider` switch.
- **ImageSharp on Alpine**: may need `libgdiplus`/`libc6-compat` or a `bookworm-slim` base — confirm at implement time.

### Technical Approach

- **Dockerfile**: SDK stage restores + publishes the API from the root sln; (conditionally) a `node:22-alpine` stage builds the UI; `aspnet:8.0` runtime stage runs as non-root `app`, `EXPOSE 8080`, `ASPNETCORE_URLS=http://+:8080`.
- **Dev compose**: `db` healthcheck-gated via `depends_on: condition: service_healthy`; API reads `ConnectionStrings__Default` + `Email__Smtp__*` from env; storage on a named volume to dodge Windows bind-mount permission issues.
- **Prod compose + Caddy**: Caddy terminates TLS (Let's Encrypt), sets HSTS/`X-Content-Type-Options`/`Referrer-Policy`, `reverse_proxy api:8080`; Postgres points at a managed instance by default (commented-out local `db` for emergencies).
- **CI**: two jobs; Postgres `services:` container for the dotnet tests; npm cache keyed on the UI lockfile. Keep CI green-gating independent of deploy.
- **Deploy**: `workflow_run` after CI success on `main` → build/push `ghcr.io/<owner>/fototipar/api:sha-<short>` + `:latest` → deploy step. Rollback documented via pinned `:sha-...`.
- **Env matrix**: `AddOptions<T>().Bind(...).ValidateDataAnnotations().ValidateOnStart()` on the settings POCOs so a missing required var fails fast at boot with the field named.

### Open decisions for the checkpoint

- **D1 — Image topology**: (A) **Combined image** — API serves the built SPA from `wwwroot` (adds a UI build stage + `UseStaticFiles`/`MapFallbackToFile` to `Program.cs`); matches stories 001–003. **(B) API-only image** + frontend deployed separately to a static host/CDN; matches tech-stack.md. This changes the Dockerfile and compose materially.
- **D2 — Migrations on deploy**: dev currently uses `EnsureCreated`. Prod Postgres needs real EF migrations — apply at boot (`Database.Migrate()` + `ValidateOnStart`) vs a dedicated pre-deploy job. Recommend boot-time migrate behind the `DatabaseProvider=Postgres` path.
- **D3 — Deploy target**: single VM over SSH (story 005 default; needs `DEPLOY_HOST/USER/SSH_KEY` GitHub secrets + a provisioned host) vs a managed platform webhook (config-only swap). Affects what `deploy.yml` actually does.
- **D4 — secret scanning**: keep the standalone `secret-scan.yml` (bolt 041) as-is, or fold a gitleaks step into `ci.yml`. Recommend: keep standalone, reference it from CI docs.
- **D5 — Local testability**: CI/CD runs and the live HTTPS check are **not** exercisable from this Windows dev box. Realistic Stage-3 verification here = `docker build` succeeds, `docker compose config` validates, and the workflow YAML is schema-valid; the GitHub-side green runs + prod HTTPS check are operator steps documented in the README. (Also: is Docker available on this machine? — confirm.)

### Acceptance Criteria

- [ ] `docker build -t fototipar/api .` succeeds from a clean clone; final image runs as non-root with a working `/health` HEALTHCHECK.
- [ ] `docker compose up` brings up api + Postgres 16 + MailHog; MailHog UI on `:8025`; API healthy after `db` is healthy.
- [ ] `docker-compose.prod.yml` + `Caddyfile` route TLS → `api:8080` with HSTS; API not host-exposed.
- [ ] `ci.yml` runs the dotnet + web jobs on PR/push and fails on test failure; `deploy.yml` builds/pushes a tagged image and deploys on `main` after CI success.
- [ ] `.env.example` lists every required var; a missing required var fails boot with an `OptionsValidationException` naming the field; no secret values committed.
- [ ] README has a "Run with Docker" section and an "Environment matrix" table; deploy + rollback runbook documented.
