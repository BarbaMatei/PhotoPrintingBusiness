# FotoTipar

Romanian photo-printing e-commerce platform — ASP.NET Core 8 API + Angular 21 SPA.

- `src/PhotoPrint.API/` — Web API (EF Core, Postgres prod / SQLite dev)
- `src/PhotoPrint.UI/` — Angular SPA
- `src/PhotoPrint.Tests/` — xUnit unit + integration tests
- `memory-bank/` — Specs.md / AI-DLC planning artifacts (intents, units, stories, bolts)
- `ops/dashboards/` — Grafana dashboard JSON (paired with the SLO doc below)

## Operations

- **Service Level Objectives**: [memory-bank/operations/slos.md](memory-bank/operations/slos.md) — what "healthy" looks like
- **Grafana overview dashboard**: [ops/dashboards/fototipar-overview.json](ops/dashboards/fototipar-overview.json)
- **Metrics reference**: [memory-bank/operations/metrics.md](memory-bank/operations/metrics.md) — every metric, label and emission site
- **Tracing + metrics rollout**: [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) §14 — read §14.3 before exposing `/metrics`
- **Sentry rollout + monitoring**: [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) §13
- **Sameday courier rollout**: [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) §12

---

## First-time setup

The repository contains **no secrets**. Provide them locally before running the API.

### 1. Generate a dev JWT signing key

```sh
# from the repo root
scripts/gen-dev-keys.sh          # macOS/Linux/Git-Bash
# or
pwsh scripts/gen-dev-keys.ps1    # Windows PowerShell
```

This writes `secrets/dev-jwt-private.pem` + `secrets/dev-jwt-public.pem` (both gitignored).

### 2. Register the key with the API

Pick one (both are gitignored, never committed):

```sh
# Option A — dotnet user-secrets (recommended)
dotnet user-secrets set "JwtSettings:PrivateKeyPem" "$(cat secrets/dev-jwt-private.pem)" \
  --project src/PhotoPrint.API

# Option B — appsettings.Development.Local.json (loaded last, overrides appsettings.Development.json)
# {
#   "JwtSettings": { "PrivateKeyPem": "-----BEGIN RSA PRIVATE KEY-----\n...\n-----END RSA PRIVATE KEY-----" }
# }
```

The API **fails fast at startup** if `JwtSettings:PrivateKeyPem` is empty — that's intentional.

### 3. Run

```sh
dotnet run --project src/PhotoPrint.API     # API (SQLite dev DB auto-created)
cd src/PhotoPrint.UI && npm install && npm start   # SPA
```

---

## Run with Docker

Brings up the full backend stack (API + PostgreSQL 16 + MailHog) in containers — no
local Postgres or SMTP install needed.

```sh
# 1. one-time: create a keypair and copy the env template
scripts/gen-dev-keys.sh                 # or: pwsh scripts/gen-dev-keys.ps1
cp .env.example .env
#    then set JwtSettings__PrivateKeyPem in .env to the contents of
#    secrets/dev-jwt-private.pem (single line, '\n' between lines)

# 2. up
docker compose up --build
```

- API: <http://localhost:8080>  ·  MailHog UI: <http://localhost:8025>
- The dev compose uses Postgres, so the API applies EF migrations at boot.
- Run the Angular dev server on the host (`npm start`, proxied to `:8080`), or rely on
  the SPA baked into the image for a prod-like check.

The production image (Caddy + API, auto-TLS) and the full deploy procedure live in
**[docs/DEPLOYMENT.md](docs/DEPLOYMENT.md)** — including hosting proposals, server
provisioning, CI/CD secrets, the migrations note, and rollback.

---

## Environment / secret matrix

Provided via env vars (prod / Docker), user-secrets, or `appsettings.{Environment}.Local.json`
(dev). Never committed. **`.env.example` is the authoritative, full list**; the config-key
convention is `Section:Key` → `Section__Key` (double underscore) as an env var. Required
Production secrets are validated at boot — a missing one fails startup with the field named.

| Key | Purpose | Required |
|-----|---------|----------|
| `ConnectionStrings:Default` | Database connection string | always |
| `DatabaseProvider` | `Postgres` (prod/Docker) or `Sqlite` (local dev) | always |
| `JwtSettings:PrivateKeyPem` | RSA private key for signing JWTs | always (boot-fails if empty) |
| `Stripe:SecretKey`, `Stripe:WebhookSecret` | Stripe payment + webhook verification | Production |
| `EuPlatesc:MerchantId`, `EuPlatesc:SecretKey` | EuPlatesc payment gateway | Production |
| `Cors:AllowedOrigins` | Comma-separated allowed SPA origins | always |
| `Email:Provider` (+ `Email:Smtp:*` / `Email:SendGrid:ApiKey`) | SMTP (dev) or SendGrid (prod) | always |
| `GoogleAuth:ClientId` | Google OAuth | if Google sign-in enabled |
| `App:BaseUrl` | Public base URL for links in emails | always |

---

## Secret scanning

Two layers guard against committing secrets:

1. **Pre-commit hook** (`hooks/pre-commit`). Install once per clone — no extra tooling required:
   ```sh
   git config core.hooksPath hooks
   ```
   It blocks staged additions matching private-key / live-API-key patterns.

2. **CI** (`.github/workflows/secret-scan.yml`) runs [gitleaks](https://github.com/gitleaks/gitleaks) on every push/PR, configured by `.gitleaks.toml`.

**Allowlist**: `.gitleaks.toml` and the pre-commit hook intentionally permit the RSA fixture in
`src/PhotoPrint.Tests/Helpers/TestKeys.cs` and the PEM marker quoted in `docs/` and `memory-bank/`.
Add new legitimate exceptions there with a comment explaining why.

---

## JWT key rotation runbook (ops)

Rotate when a key is suspected exposed, on a schedule, or when onboarding a new environment.

1. Generate a fresh keypair (`scripts/gen-dev-keys.sh` for the format; for prod use a secure host).
2. Set the new `JwtSettings:PrivateKeyPem` in the target environment's secret store / env var.
3. Rolling-restart the API. Existing access tokens (15 min TTL) signed with the old key are rejected;
   clients transparently obtain new ones via the refresh-token flow (refresh tokens are SHA-256 hashed
   in the DB and unaffected by signing-key rotation).
4. Decommission the old key from the secret store once all instances are restarted.

Impact window: up to one access-token lifetime (~15 min) where a client may need a single refresh.

---

## Historical secret leak (resolved by rotation)

> **Decision: accept + rotate** (see [ADR-006](memory-bank/bolts/041-secrets-management/adr-006-secret-history-accept-and-rotate.md)).

An earlier commit committed a real dev RSA JWT key to `appsettings.Development.json`. The key has since
been removed from the working tree and the file now ships empty; secrets load from gitignored local
config. The key value still exists in git history. Rather than rewrite history (force-push, full re-clone
for everyone), the active mitigation is **key rotation**: the leaked dev key is rotated out of every
environment per the runbook above, rendering the historical copy worthless. The pre-commit hook + CI
gitleaks scan prevent recurrence.
