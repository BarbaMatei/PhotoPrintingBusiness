# FotoTipar — Deployment Runbook

> **Status: not yet deployed.** There is no production server at the time of writing.
> This document is the step-by-step playbook for when one exists. Everything here is
> verified by `docker build` / CI on the day you provision infrastructure — the
> repo ships the config; the live steps are operator actions.

Contents:
1. [Hosting proposals (pick one)](#1-hosting-proposals-pick-one)
2. [What's already in the repo](#2-whats-already-in-the-repo)
3. [Prerequisites](#3-prerequisites)
4. [First-time server provisioning](#4-first-time-server-provisioning-single-vm-path)
5. [GitHub secrets to configure](#5-github-secrets-to-configure)
6. [The deploy flow](#6-the-deploy-flow)
7. [Database migrations](#7-database-migrations--read-before-first-deploy)
8. [Post-deploy verification checklist](#8-post-deploy-verification-checklist)
9. [Rollback](#9-rollback)
10. [Troubleshooting](#10-troubleshooting)

---

## 1. Hosting proposals (pick one)

All three run the **same image** built by this repo. They differ in who runs the
container and the database. Ordered cheapest-effort first.

### Proposal A — Single VM + Docker Compose  ⭐ recommended to start
A €5–12/mo VPS (Hetzner CX22, DigitalOcean Droplet, Contabo) running
`docker-compose.prod.yml`: Caddy (auto-TLS) → API container, with **managed
Postgres** alongside (see DB note) or a Postgres container on the same box.

- **Pros**: cheapest; full control; the CD pipeline already targets it; one command to stand up.
- **Cons**: you patch the OS; single point of failure; manual vertical scaling.
- **Best when**: launching, low/medium traffic, cost-sensitive. **This is the wired default.**

### Proposal B — Managed container platform
Push the image to GHCR (CD already does this) and run it on **DigitalOcean App
Platform**, **Azure Container Apps**, **Google Cloud Run**, or **Render**. Point at
a managed Postgres (their offering or Neon/Supabase).

- **Pros**: no servers to patch; TLS, autoscaling, health checks, zero-downtime deploys built in; rollbacks in the dashboard.
- **Cons**: ~€15–40/mo+; provider lock-in for the deploy glue; cold starts (Cloud Run).
- **Switch cost**: config-only — replace the SSH step in `deploy.yml` with the provider's deploy action/webhook; drop Caddy (the platform terminates TLS).
- **Best when**: you want to stop thinking about servers and can spend a bit more.

### Proposal C — Kubernetes (k3s / managed k8s)
- **Pros**: horizontal scaling, rolling deploys, self-healing, multi-service future-proofing.
- **Cons**: large operational overhead — not justified until there are several services and real scale.
- **Best when**: explicitly out of scope now (unit-brief lists k8s as out of scope). Documented only so the choice is conscious.

### Database, regardless of proposal
Prefer a **managed PostgreSQL 16** (DigitalOcean Managed DB, Neon, Supabase, Azure
Database for PostgreSQL): automated backups, PITR, failover — things you do **not**
want to hand-roll for customer/order data. A Postgres container is fine for a hobby
deploy (uncomment the `db` service in `docker-compose.prod.yml`) but then **backups
are on you**.

**Recommendation:** **Proposal A + managed Postgres** to launch; revisit B if ops time becomes the bottleneck.

---

## 2. What's already in the repo

| File | Role |
|------|------|
| `Dockerfile` | Multi-stage; builds the API + Angular SPA into one non-root image serving on `:8080` with a `/health` HEALTHCHECK. |
| `docker-compose.yml` | Local dev stack: API + Postgres + MailHog. |
| `docker-compose.prod.yml` | Production stack: Caddy (auto-TLS) → API; managed Postgres by default. |
| `Caddyfile` | TLS termination, HSTS, gzip/zstd, access logs. |
| `.env.example` | Every environment variable, documented. Copy to `.env`. |
| `.github/workflows/ci.yml` | Build + test API and UI on every PR / non-main push. |
| `.github/workflows/deploy.yml` | On green CI on `main`: build + push image to GHCR, then deploy (SSH step self-skips until a host is set). |
| `.github/workflows/secret-scan.yml` | Independent gitleaks scan (from bolt 041). |

---

## 3. Prerequisites

- A **domain** with DNS you control (e.g. `fototipar.ro`).
- A **container registry** — GHCR is used automatically (`ghcr.io/<owner>/fototipar/api`). Make the package visible to the deploy host or use a PAT.
- A **managed PostgreSQL 16** instance (connection string), or accept the in-compose Postgres + your own backups.
- **Secrets ready**: JWT keypair, Stripe keys, EuPlatesc credentials, SendGrid API key, Google OAuth client ID. See `.env.example` and the [README env matrix](../README.md#environment--secret-matrix).
- For Proposal A: an SSH-reachable Linux VM with Docker Engine + Compose v2.

---

## 4. First-time server provisioning (single-VM path)

```sh
# On the VM (Ubuntu/Debian example), as a sudo user:
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker "$USER"          # re-login afterwards

sudo mkdir -p /opt/fototipar && sudo chown "$USER" /opt/fototipar
cd /opt/fototipar

# Copy deploy assets to the server (from your workstation):
#   scp docker-compose.prod.yml Caddyfile  user@HOST:/opt/fototipar/
# Create the runtime env file on the server (never commit it):
cp .env.example .env && nano .env         # fill SITE_ADDRESS, GHCR_OWNER, DB, secrets

# DNS: point an A/AAAA record for SITE_ADDRESS at this VM's IP BEFORE first boot,
# so Caddy can complete the Let's Encrypt HTTP-01 challenge.

# If GHCR package is private, log in once so `docker compose pull` can fetch it:
echo "$GHCR_PAT" | docker login ghcr.io -u <github-user> --password-stdin

docker compose -f docker-compose.prod.yml up -d
docker compose -f docker-compose.prod.yml logs -f caddy   # watch cert issuance
```

---

## 5. GitHub secrets to configure

Repo → Settings → Secrets and variables → Actions. The deploy workflow stays
image-build-only until `DEPLOY_HOST` is present.

| Secret | Needed for | Notes |
|--------|-----------|-------|
| `DEPLOY_HOST` | SSH deploy (Proposal A) | VM IP/hostname. Absent ⇒ deploy step self-skips. |
| `DEPLOY_USER` | SSH deploy | e.g. `deploy` or your sudo user. |
| `DEPLOY_SSH_KEY` | SSH deploy | **Private** key whose public half is in the VM's `~/.ssh/authorized_keys`. |
| `GITHUB_TOKEN` | GHCR push | Provided automatically by Actions — no action needed. |

`GHCR_OWNER`, `IMAGE_TAG`, `SITE_ADDRESS`, DB and app secrets live in the server's
`.env`, **not** in GitHub secrets.

---

## 6. The deploy flow

```text
push to main ──► ci.yml (build + test) ──► [green] ──► deploy.yml
                                                          │
                         build image ──► push GHCR (:sha-xxxx and :latest)
                                                          │
                         DEPLOY_HOST set?  ── no ─► stop (image is published; deploy by hand)
                                │ yes
                         SSH: docker compose pull api && up -d  ──► live
```

Manual deploy (no CD, or to redeploy a specific build) — on the server:
```sh
cd /opt/fototipar
export IMAGE_TAG=sha-abc1234          # or `latest`
docker compose -f docker-compose.prod.yml pull api
docker compose -f docker-compose.prod.yml up -d
```

---

## 7. Database migrations — READ BEFORE FIRST DEPLOY

The API applies **EF Core migrations automatically at boot** when it connects to
PostgreSQL (`Database.Migrate()`, guarded by `IsNpgsql()` in `Program.cs`). SQLite
dev uses `EnsureCreated()` instead. So a normal deploy needs no manual migration step.

> ⚠️ **Known gap to resolve before the first real Postgres deploy.**
> Most migrations are Npgsql-native (`uuid`, `character varying`, `timestamp with time zone`),
> but the newest one — `20260527075359_AddOrderIdempotencyKey` — was generated under the
> **SQLite** provider (`TEXT` columns, a plain — not partial — unique index). `TEXT` is a
> valid Postgres type so boot won't crash, but the migration history is provider-inconsistent.
> **Before deploying to Postgres**, verify migrations apply against a real PG instance and,
> ideally, regenerate that migration under Npgsql:
> ```sh
> # against a scratch Postgres, with the prod provider:
> DatabaseProvider=Postgres ConnectionStrings__Default="Host=...;..." \
>   dotnet ef database update --project src/PhotoPrint.API
> ```
> If it applies cleanly and the unique index behaves as intended (Postgres allows multiple
> NULLs in a unique index, which matches the nullable idempotency key), you're good. This is
> tracked as a follow-up; it is not a containers/pipelines deliverable.

Seeding the product catalog (first deploy only):
```sh
docker compose -f docker-compose.prod.yml run --rm api dotnet PhotoPrint.API.dll --seed
```

---

## 8. Post-deploy verification checklist

- [ ] `curl -fsS https://SITE_ADDRESS/health` returns `200` with `"status"` healthy.
- [ ] TLS valid (padlock; `https://www.ssllabs.com/ssltest/` A or better).
- [ ] HTTP → HTTPS redirect works; HSTS header present.
- [ ] SPA loads at `https://SITE_ADDRESS/` and client-side routes deep-link (refresh on a sub-route serves the app, not 404 — confirms the SPA fallback).
- [ ] API reachable (e.g. `GET /api/products` returns catalog JSON).
- [ ] A test login / registration succeeds (JWT signing key wired).
- [ ] Logs clean: `docker compose -f docker-compose.prod.yml logs --tail=100 api` — no `OptionsValidationException` (means a required secret is missing) and no migration errors.
- [ ] A Stripe test-mode payment completes end-to-end (and the EuPlatesc redirect builds).

---

## 9. Rollback

Images are tagged by commit SHA, so rollback = redeploy the previous tag:
```sh
cd /opt/fototipar
export IMAGE_TAG=sha-<previous>       # find prior tags in GHCR
docker compose -f docker-compose.prod.yml up -d --force-recreate api
```
Pin `IMAGE_TAG` in `.env` to freeze the running version (otherwise `:latest` floats
to the newest push). **Migrations are not auto-rolled-back** — a schema-breaking
release needs a forward-fix migration or a DB restore from backup; prefer additive,
backwards-compatible migrations.

---

## 10. Troubleshooting

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| Caddy can't get a cert | DNS not pointing at the VM yet, or port 80/443 blocked | Fix DNS/firewall; use the staging `acme_ca` line in `Caddyfile` while testing. |
| API exits on boot with `OptionsValidationException` | A required secret (Stripe/EuPlatesc/JWT) is empty in Production | Set it in the server `.env`; `docker compose up -d` again. |
| `docker compose pull` 403/denied | GHCR package private and host not logged in | `docker login ghcr.io` with a PAT that has `read:packages`. |
| Migration error on first PG connect | The SQLite-flavored migration (see §7) | Verify/regenerate per §7 against a scratch Postgres first. |
| Uploaded images vanish on redeploy | `Storage` not on a volume | Confirm the `apidata:/app/Storage` volume in `docker-compose.prod.yml`. |
| Site shows API 404 instead of the app | UI not built into `wwwroot` (image built without the UI stage) | Rebuild with the standard `Dockerfile`; the `ui-build` stage populates `wwwroot`. |
