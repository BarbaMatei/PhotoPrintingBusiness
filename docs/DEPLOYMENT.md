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
11. [Cloud Storage](#11-cloud-storage-bolt-043--intent-024)
12. [Sameday courier integration](#12-sameday-courier-integration-intent-015--bolts-036--037)
13. [Error tracking with Sentry](#13-error-tracking-with-sentry-intent-020--bolt-045)
14. [Tracing and metrics](#14-tracing-and-metrics-intent-020--bolt-044)

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
| `Caddyfile` | TLS termination, HSTS, gzip/zstd, access logs; refuses `/metrics*` so the scrape path has no route from the internet (§14.3). |
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
PostgreSQL (`Database.Migrate()`, guarded by `IsNpgsql()` in `Program.cs`). Every
environment — local dev included — uses that same path, so a normal deploy needs no
manual migration step.

There is a single migration, `20260820133204_InitialPostgres`, scaffolded under the Npgsql
design-time provider: `uuid`, `timestamp with time zone`, `jsonb`, `numeric`, a partial
unique index on `Orders.IdempotencyKey`, both one-owner check constraints, the 42 Easybox
locker seed rows, the `uq_invoices_series_year_number` expression index, and the
`invoice_seq_ft_2026` sequence. It has been applied against a real PostgreSQL 16 instance
from an empty database.

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
| Migration error on first PG connect | Role lacks DDL rights, or a partially-created schema | Confirm the role owns the database, then re-run against a scratch Postgres per §7. |
| Uploaded images vanish on redeploy | `Storage` not on a volume | Confirm the `apidata:/app/Storage` volume in `docker-compose.prod.yml`. |
| Site shows API 404 instead of the app | UI not built into `wwwroot` (image built without the UI stage) | Rebuild with the standard `Dockerfile`; the `ui-build` stage populates `wwwroot`. |

---

## 11. Cloud Storage (bolt 043 + intent 024)

Operational guide for enabling the cloud tier in a real deployment. Skip this whole
section for dev — `Storage:Provider=Local` is the right default there.

### 11.1 Overview

The API runs **two storage tiers in parallel**, gated by `Upload.StorageLocation` per
upload:

- **Local tier** (always on) — serves pre-payment bytes from the deployment VM's disk.
  Guest/abandoned uploads never leave the host.
- **Cloud tier** (`Storage:Provider=S3`) — receives bytes only when an order is *paid*,
  via the intent-024 promote-on-payment lifecycle. After fulfilment (Shipped /
  Delivered) the original is purged from cloud; large preview + thumbnail remain for
  the retention window (default 12 months) as the customer-facing order archive.

This split is deliberate: it minimises the cloud footprint to *only paid orders*, which
is the GDPR posture for an EU photo business. See
[ADR-008](../memory-bank/bolts/043-cloud-storage-provider/adr-008-two-tier-storage-with-storage-location.md)
for the architectural rationale; this section is purely operational.

### 11.2 Choosing a provider

The code path is the same — only configuration differs.

| Provider | Use case | One-liner |
|----------|----------|-----------|
| **Cloudflare R2** ⭐ recommended | Production | **Zero egress** is decisive for image-serving; CF-edge proximity to RO. See [ADR-009](../memory-bank/bolts/043-cloud-storage-provider/adr-009-cloudflare-r2-recommended-cloud-target.md). |
| AWS S3 | Supported, fall-back | Mature, widest tooling; egress dominates the bill. |
| MinIO | CI + self-hosted staging mirrors | Same S3 API; useful where prod is R2 and you want a no-cost facsimile. |

### 11.3 Cloudflare R2 setup walkthrough

1. **Create the bucket.** Cloudflare dashboard → R2 → *Create bucket* →
   `photoprint-prod`. Pick the EU jurisdiction.
2. **Grab the account ID.** Top-right of the R2 page; you need it for `EndpointUrl`.
3. **Generate an API token.** R2 → *Manage R2 API Tokens* → *Create API token* →
   permission **Object Read & Write** → scope to the `photoprint-prod` bucket only.
   Copy the `Access Key ID` and `Secret Access Key` somewhere safe — they're shown
   once. Treat them as production secrets (see §11.7).
4. **CORS.** The SPA fetches large previews and thumbnails via presigned URLs
   directly from R2 in browser, so the bucket needs CORS to allow the SPA origin.
   R2 → bucket → *Settings* → *CORS Policy*:

   ```json
   [
     {
       "AllowedOrigins": ["https://fototipar.ro"],
       "AllowedMethods": ["GET"],
       "AllowedHeaders": ["*"],
       "ExposeHeaders": [],
       "MaxAgeSeconds": 3600
     }
   ]
   ```

5. **Set the env vars** on the deploy host (`/opt/fototipar/.env`):

   ```sh
   Storage__Provider=S3
   Storage__Bucket=photoprint-prod
   Storage__Region=auto
   Storage__EndpointUrl=https://<R2_ACCOUNT_ID>.r2.cloudflarestorage.com
   Storage__ForcePathStyle=true
   Storage__AccessKey=<your-access-key>
   Storage__SecretKey=<your-secret-key>
   Storage__PresignTtlMinutes=60
   ```

   `Region=auto` and `ForcePathStyle=true` are R2-specific quirks — both are required.

### 11.4 AWS S3 setup walkthrough

1. **Create the bucket** in the AWS console in your chosen region (e.g.
   `eu-central-1`). Block all public access — the API issues presigned URLs.
2. **Create an IAM user** with programmatic access. Attach an inline policy scoped
   to the single bucket:

   ```json
   {
     "Version": "2012-10-17",
     "Statement": [{
       "Effect": "Allow",
       "Action": ["s3:GetObject", "s3:PutObject", "s3:DeleteObject"],
       "Resource": "arn:aws:s3:::photoprint-prod/*"
     }]
   }
   ```

3. **Env vars** — no `EndpointUrl`, no `ForcePathStyle`, real region name:

   ```sh
   Storage__Provider=S3
   Storage__Bucket=photoprint-prod
   Storage__Region=eu-central-1
   Storage__ForcePathStyle=false
   Storage__AccessKey=<your-access-key>
   Storage__SecretKey=<your-secret-key>
   ```

4. **CORS** — same shape as the R2 JSON above (set on the bucket via the AWS console).

### 11.5 MinIO setup (staging / self-hosted)

Primarily for CI and staging mirrors of prod. The
[CI workflow](../.github/workflows/ci.yml) starts MinIO this way — copy the pattern:

```sh
docker run -d --name minio \
  -p 9000:9000 \
  -e MINIO_ROOT_USER=minioadmin \
  -e MINIO_ROOT_PASSWORD=minioadmin \
  minio/minio:RELEASE.2024-12-18T13-15-44Z \
  server /data
```

Then create the bucket once via `mc` or the MinIO console, and point the API at it:

```sh
Storage__Provider=S3
Storage__Bucket=photoprint-staging
Storage__Region=auto
Storage__EndpointUrl=http://minio:9000
Storage__ForcePathStyle=true
Storage__AccessKey=minioadmin
Storage__SecretKey=minioadmin
```

### 11.6 Full env-var reference

All keys use the ASP.NET Core `Section__Key` env-var form. Defaults come from
[`appsettings.json`](../src/PhotoPrint.API/appsettings.json) and the
[`StorageSettings`](../src/PhotoPrint.API/Configuration/StorageSettings.cs) /
[`OrderPhotoArchiveSettings`](../src/PhotoPrint.API/Configuration/OrderPhotoArchiveSettings.cs) /
[`ArchiveSettings`](../src/PhotoPrint.API/Configuration/ArchiveSettings.cs) types.

#### Storage (cloud tier)

| Key | Required when `Provider=S3`? | Default | Notes |
|-----|------------------------------|---------|-------|
| `Storage__Provider` | — | `Local` | `Local` = cloud tier off (dev default). `S3` = cloud tier on. |
| `Storage__BasePath` | — | `/var/app/uploads` | Local-tier root. Must be writable by the API user; mount on a persistent volume in prod. |
| `Storage__Bucket` | **yes** | *(empty)* | Cloud bucket name. Validator rejects boot if empty when `Provider=S3`. |
| `Storage__Region` | **yes** | `auto` | `auto` for R2; a real region name (e.g. `eu-central-1`) for AWS. |
| `Storage__EndpointUrl` | required for R2 / MinIO; empty for AWS | *(empty)* | Custom endpoint URL. |
| `Storage__ForcePathStyle` | `true` for R2 / MinIO; `false` for AWS | `false` | Path-style vs. virtual-host addressing. |
| `Storage__AccessKey` | **yes** | *(empty)* | Secret. ADR-006 — env or secret store, never committed. |
| `Storage__SecretKey` | **yes** | *(empty)* | Secret. ADR-006. |
| `Storage__PresignTtlMinutes` | — | `60` | Presigned-URL lifetime for preview/thumb fetches. Must be > 0. |

#### OrderPhotoArchive (promote-on-paid, bolt 051)

| Key | Default | Notes |
|-----|---------|-------|
| `OrderPhotoArchive__Enabled` | `true` | Master switch. `false` behaves identically to cloud tier off. |
| `OrderPhotoArchive__MaxConcurrentOrders` | `4` | Worker semaphore. |
| `OrderPhotoArchive__MaxAttempts` | `5` | Retry ceiling before logging `UploadPromotionFailed`. |
| `OrderPhotoArchive__BackoffSeconds__0..4` | `30,120,300,900,3600` | Per-attempt delay, clamps at last value. Set with indexed env vars: `OrderPhotoArchive__BackoffSeconds__0=30` etc. |

#### Archive (retention + original purge, bolt 052)

| Key | Default | Notes |
|-----|---------|-------|
| `Archive__Enabled` | `true` | Master switch for both purger and retention job. |
| `Archive__PurgeOriginalAtStatus` | `Shipped` | Must parse to `Shipped` or `Delivered` — validator rejects anything else. |
| `Archive__RetentionMonths` | `12` | Window from `Order.PaidAt` (anchor — see [ADR-012](../memory-bank/bolts/052-archive-retention/adr-012-retention-anchor-paid-at.md)). |
| `Archive__JobIntervalHours` | `6` | Periodic sweep cadence. |
| `Archive__BatchSize` | `500` | Per-tick limit on retention deletes. |

### 11.7 Secret management

Per
[ADR-006](../memory-bank/bolts/041-secrets-management/adr-006-secret-history-accept-and-rotate.md):
credentials live in environment variables (or a platform secret store), **never** in
`appsettings.json` or git. The repo's [`.env.example`](../.env.example) is the
template; the runtime `.env` is gitignored.

Where to put the values:

- **docker-compose** (single-VM, Proposal A) — append to `/opt/fototipar/.env`
  alongside the other secrets. `docker-compose.prod.yml` already forwards `.env` via
  `env_file:`.
- **systemd unit** (bare metal) — `Environment=Storage__Provider=S3` and friends in
  the `[Service]` section, or `EnvironmentFile=/etc/fototipar.env` with `chmod 600`.
- **Managed platform** (Proposal B) — use the provider's secret/env-var UI (DO App
  Platform "App-Level Environment Variables" as `SECRET`, Azure Container Apps
  "Secrets", Cloud Run "Secret Manager" reference). Mark `AccessKey` / `SecretKey`
  as secrets so they don't appear in logs.

Rotation: generate a new R2 token, set the new env vars, restart the API,
*then* revoke the old token. Order matters — boot validation will fail if the live
token doesn't match the bucket.

### 11.8 First-deploy checklist

1. Confirm [`appsettings.json`](../src/PhotoPrint.API/appsettings.json) still ships
   `Storage:Provider=Local` (the safe default — cloud only turns on via env
   override).
2. Set the production env vars from §11.3 in `/opt/fototipar/.env` (or your
   platform's secret store).
3. Boot the API and tail the logs:
   ```sh
   docker compose -f docker-compose.prod.yml up -d
   docker compose -f docker-compose.prod.yml logs -f api | grep -i bucket
   ```
   You should see a single line from
   [`S3BucketVerifier`](../src/PhotoPrint.API/Services/S3BucketVerifier.cs):
   `S3 bucket 'photoprint-prod' verified at boot.` If the host crashes here, see
   §11.10.
4. Run a test order through the **Stripe test endpoint**: place an order, complete
   payment in test mode, watch the webhook arrive.
5. Confirm cloud objects appear:
   - R2 dashboard → bucket → *Objects* — you should see `originals/...`,
     `previews/...`, and `thumbs/...` keys.
   - Or `aws s3 ls s3://photoprint-prod/ --endpoint-url=https://<R2_ACCOUNT_ID>.r2.cloudflarestorage.com --recursive`.
6. Confirm the **local-tier files for that upload are gone** under
   `Storage__BasePath` — promotion runs Confirmed-Write-Then-Delete (ADR-011).
7. In the admin UI, mark the test order **Shipped**. Re-check the bucket: the
   `originals/...` key for that upload should be deleted; `previews/...` and
   `thumbs/...` remain.
8. Log in as the test customer → *Order history* → confirm the photo grid renders
   and the lightbox opens (this proves presigned-URL CORS works end-to-end).
9. Identify pre-existing paid orders that need backfilling:
   ```sh
   docker compose -f docker-compose.prod.yml run --rm api \
     dotnet PhotoPrint.API.dll backfill-archive --dry-run
   ```
   Then run it for real (no `--dry-run`) once the dry-run output looks sane.

### 11.9 Backfill for pre-existing paid orders

If you enable the cloud tier on a deployment that already has paid orders sitting in
the local tier, those orders won't be promoted by anything — the in-process queue
only fires on new payment webhooks. The backfill CLI (bolt 051 story 004) catches up:

```sh
# Inspect what would be promoted, no side effects:
dotnet run --project src/PhotoPrint.API -- backfill-archive --dry-run

# Run it for real:
dotnet run --project src/PhotoPrint.API -- backfill-archive
```

The CLI walks the same `Status >= Paid && uploads have StorageLocation=Local` query
the recovery scanner uses, calls the same `OrderPhotoPromoter`, so:

- **Idempotent** — re-running is free; already-promoted uploads short-circuit on
  `StorageLocation == Cloud`.
- **Resumable** — Ctrl+C cancels at the next `await`; the next invocation picks up
  where it left off.
- **Same code path as live promotion** — there's no shadow path to maintain.

Exit code is `0` on success, `1` on any per-order failure.

### 11.10 Verification & troubleshooting

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| Boot crashes with `OptionsValidationException: Storage:Bucket is required when Storage:Provider=S3` | One of `Bucket` / `AccessKey` / `SecretKey` / `Region` is empty | Set the missing `Storage__*` env var. Full list in §11.6. |
| Boot crashes inside `S3BucketVerifier` with `InvalidOperationException: S3 bucket '...' could not be verified at boot` | Wrong credentials, wrong bucket name, wrong endpoint, or wrong region | Re-check the four against the R2 / AWS console. Watch for accidental whitespace in env values. |
| `S3BucketVerifier` reports bucket exists but uploads 403 in production | IAM/token scoped to the wrong operations | Confirm the token has `s3:GetObject`/`PutObject`/`DeleteObject` (AWS) or *Object Read & Write* (R2). |
| `LocalStorageService` warning at boot: "could not pre-create BasePath ..." | The deployment user can't write `Storage__BasePath` | Harmless in dev (test factories override the path). In prod, ensure the volume mount has the right owner — pre-payment uploads will otherwise fail at first upload. |
| Error log `promotion.refused order_id=X reason=cloud-tier-off` on a Paid order in prod | `Storage:Provider=Local` in a prod deployment | Set `Storage__Provider=S3` and restart. The order will be picked up by the next startup's recovery scan. |
| Error log `purge.refused order_id=X reason=cloud-tier-off` on a Shipped order | Same as above | Same fix. |
| `OrderPhotoPromoter` failing repeatedly with cloud-side errors | Network reachability, throttling, or a quota | Promoter retries 5x with exponential backoff (30s → 1h). After exhausting, the next API restart's recovery scan re-enqueues. |

### 11.11 Recommended bucket policies (defense in depth)

The in-app `ArchiveRetentionJob` is the **primary** mechanism for bounded retention
(default 12 months from `Order.PaidAt`). Add a cloud-side lifecycle rule as a
belt-and-suspenders backstop:

- **R2:** *Object lifecycle rules* → delete objects older than **400 days** under
  prefix `previews/` and `thumbs/`. Don't touch `originals/` (purged by app code on
  Shipped — a lifecycle rule there would just shorten the window).
- **AWS S3:** equivalent *Lifecycle rule* → *Expire current versions* after 400
  days, scoped to those two prefixes.

400 days is intentionally longer than the in-app 12-month window: the app is the
primary; the bucket rule only catches objects the app missed (e.g. a long-running
outage of the retention job). If you ever change `Archive__RetentionMonths`, bump
the bucket rule to match + ~30 days.

### 11.12 Future considerations

[ADR-008](../memory-bank/bolts/043-cloud-storage-provider/adr-008-two-tier-storage-with-storage-location.md)'s
*Future* section notes one optimisation worth keeping in mind but **not implemented**:
serve `thumbs/` from a public R2 custom-domain so the hot path drops presigned-URL
churn (each presigned URL is unique → poor CDN cache-hit ratio). The trade-off is
that public-domain thumbs would be enumerable if keys were guessable, so it requires
either keeping the per-upload UUID in the key (current shape — fine) or moving
behind Cloudflare Access. Defer until presigned-URL latency or R2 request counts
show up as a real cost.

---

## 12. Sameday courier integration (intent 015 / bolts 036 + 037)

The Sameday integration is **off by default**. With both flags unset the
app behaves exactly as it did pre-bolt: `StaticShippingService` serves
the locker list + shipping cost endpoints, and AWB generation is the
manual workflow (`AwbResultDto.Manual = true`). Flipping it on is a
**deliberate two-stage rollout** — see [§ 12.5](#125-recommended-rollout-sequence).

### 12.1 What the integration does

- **AWB creation**: when an order transitions to `Paid` (payment-webhook
  hot path), the system enqueues an in-process job. A `BackgroundService`
  drains the channel, calls Sameday's `POST /api/awb`, and persists
  `Order.AwbNumber` + `Order.AwbLabelUrl`.
- **AWB retry safety net**: a 60-min job re-discovers orders that are
  `Paid AND AwbNumber IS NULL AND PaidAt > now - 24h` and re-enqueues
  them. After 24 h, gives up with one Error log per order (admin manual
  fallback). Recovers from process crashes / replica restarts that lost
  pending channel items.
- **Tracking poll**: a 15-min job polls `Shipped` orders against
  Sameday's tracking endpoint. On observed `delivered`, transitions the
  order to `Delivered`, sets `DeliveredAt`, fires the existing
  customer "your order has arrived" email.

### 12.2 Two flags, two stages

| Flag | Path | Purpose |
|---|---|---|
| `Sameday:Enabled` | `Sameday__Enabled` | Master gate. Off → static fallback registered; on → typed `HttpClient` + token cache + auth handler active. Boot fails fast if any required credential is missing. |
| `Sameday:Jobs:Enabled` | `Sameday__Jobs__Enabled` | Lifecycle gate. Off → no AWB workflow, no tracking poll, webhook notifier is a no-op. On → three `BackgroundService`s start at boot. |

They are **orthogonal on purpose** (ADRs 013/014). A deployment can
flip `Sameday:Enabled` first to validate credentials, watch the
`sameday.token.refreshed` log fire on first auth call, then flip
`Sameday:Jobs:Enabled` once you're confident the wire is right. There
is no operational downside to running in stage-1 mode for a while.

### 12.3 Provisioning prerequisites

Before flipping anything on you need:

1. **A Sameday merchant account.**
   - **Sandbox**: contact Sameday sales to enable sandbox access on
     your account. Sandbox base URL is `https://sameday-api.demo.sameday.ro`.
   - **Production**: same credentials, different base URL —
     `https://api.sameday.ro`.
2. **A pickup-point ID.** Sameday's "Pickup Point" is the physical
   address they collect parcels from. The ID is configured per-environment
   (so dev / sandbox / prod can each have a distinct one — typical for
   a single warehouse, but the seam exists).
   - Get it from the Sameday merchant portal → Settings → Pickup
     Points → copy the numeric ID.
   - Required even in stage-1 (the validator demands it when
     `Sameday:Enabled = true`).
3. **API username + password.** Same merchant portal → API access.
   Use a **dedicated service user** if Sameday supports it — never
   share credentials with the human admin login.

### 12.4 Secret management

Aligned with [ADR-006](../memory-bank/bolts/041-secrets-management/adr-006-secret-history-accept-and-rotate.md):
**credentials never live in source.** `appsettings.json` ships with
`Username = ""`, `Password = ""`, `PickupPointId = ""` — the validator
will refuse to boot the app with `Sameday:Enabled = true` if any of
these are blank. Use one of the two paths below.

**Local dev** — `dotnet user-secrets`:

```powershell
cd src/PhotoPrint.API
dotnet user-secrets set "Sameday:Enabled"       "true"
dotnet user-secrets set "Sameday:Username"      "your-sandbox-username"
dotnet user-secrets set "Sameday:Password"      "your-sandbox-password"
dotnet user-secrets set "Sameday:PickupPointId" "12345"
# Optional — only if you also want to test the jobs locally:
dotnet user-secrets set "Sameday:Jobs:Enabled"  "true"
```

User-secrets are gitignored by design (stored in
`%APPDATA%\Microsoft\UserSecrets\<id>\` on Windows). The
`UserSecretsId` is already declared in
[`PhotoPrint.API.csproj`](../src/PhotoPrint.API/PhotoPrint.API.csproj).

**Staging / production** — environment variables. ASP.NET Core binds
`Sameday__Foo__Bar` (double underscore) to `Sameday:Foo:Bar`:

```bash
# Stage 1 — credentials only, jobs off
Sameday__Enabled=true
Sameday__BaseUrl=https://api.sameday.ro
Sameday__Username=fototipar-prod
Sameday__Password=<from-secret-store>
Sameday__PickupPointId=12345
Sameday__RequestTimeoutSeconds=10
Sameday__Jobs__Enabled=false

# Stage 2 — add these when you're ready to flip the jobs on
Sameday__Jobs__Enabled=true
Sameday__Jobs__AwbRetryIntervalMinutes=60
Sameday__Jobs__AwbGiveUpHours=24
Sameday__Jobs__TrackingIntervalMinutes=15
Sameday__Jobs__TrackingMaxAgeDays=30
Sameday__Jobs__MaxConcurrentSamedayCalls=5
# DispatchBackoffSeconds is an array — bind via indexed env vars:
Sameday__Jobs__DispatchBackoffSeconds__0=30
Sameday__Jobs__DispatchBackoffSeconds__1=120
Sameday__Jobs__DispatchBackoffSeconds__2=300
Sameday__Jobs__DispatchBackoffSeconds__3=900
Sameday__Jobs__DispatchBackoffSeconds__4=3600
```

For systemd-managed deploys, the existing `EnvironmentFile=` pattern in
the service unit handles all of these — append to your existing
`/etc/photoprint/photoprint.env`. For Docker, `--env` or an
`env_file:` in docker-compose. For Azure App Service, App Settings.

### 12.5 Recommended rollout sequence

1. **Run the EF migration.** Bolts 036 + 037 added three columns to
   `Orders`: `AwbLabelUrl`, `LastTrackingSyncAt`, `ShippedAt`,
   `DeliveredAt`. The two migrations
   (`20260602141429_AddSamedayOrderFields`,
   `20260602190046_AddOrderShippedAtAndDeliveredAt`) are
   additive-nullable — safe to apply to a live database before the
   feature flags flip on. The standard deploy flow
   ([§ 7](#7-database-migrations--read-before-first-deploy)) runs them
   automatically on Postgres.
2. **Stage 1 — credentials only.** Set the env vars above with
   `Sameday__Enabled=true` and `Sameday__Jobs__Enabled=false`.
   Redeploy. Watch the logs:
   - `Sameday token refreshed. ExpiresAt=…` should fire the **first
     time** any code path resolves `ISamedayTokenProvider`. With jobs
     off, that's "never" by itself, but you can prove auth works by
     triggering a manual call from a debugging endpoint or just by
     trusting `ValidateOnStart` succeeded.
   - If the host fails to boot with a `SamedaySettingsValidator`
     error, fix the missing/malformed setting before continuing.
3. **Stage 2 — flip jobs on.** Set `Sameday__Jobs__Enabled=true`.
   Redeploy. Within ~60 s of boot you should see:
   - `AwbDispatcher started (maxConcurrent=5)`
   - `AwbRetryJob started (intervalMinutes=60 giveUpHours=24)`
   - `ShipmentTrackingJob started (intervalMinutes=15 maxAgeDays=30)`
   - The `AwbRetryJob` runs once on startup; if any orders are
     currently in the `Paid` cohort with no AWB, you'll see
     `sameday.awb.retry-sweep enqueued=N` and then per-order
     `sameday.awb.created order_id=… awb=…` logs as the dispatcher
     drains.
4. **Watch for the first new order.** Make one real (or sandbox)
   order through the checkout flow. Expected log sequence within
   ~10–60 s of payment confirmation:
   ```
   sameday.awb.enqueued order_id=<id>
   sameday.awb.created  order_id=<id> awb=RO… attempt=1
   ```
   The order's row should now have `AwbNumber` and `AwbLabelUrl`
   populated. Admin order-detail surfaces this via `Order.AwbNumber`
   already.

### 12.6 Multi-replica notes (read before scaling out)

The current design is correct under multi-replica but with two
deliberate trade-offs ([ADR-015](../memory-bank/bolts/037-awb-and-tracking-jobs/adr-015-accept-duplicate-awb-create-on-multi-replica.md),
[ADR-016](../memory-bank/bolts/037-awb-and-tracking-jobs/adr-016-cas-execute-update-for-multi-replica-status-transitions.md)):

- Each replica runs its own `AwbRetryJob` and `ShipmentTrackingJob`
  ticks. **Duplicate `CreateAwb` calls are expected** and absorbed by
  Sameday's `awbPayment` external-reference idempotency + our
  `Status == Paid AND AwbNumber IS NULL` re-check.
- `Shipped → Delivered` transitions use a compare-and-swap
  (`ExecuteUpdateAsync WHERE Status = 'Shipped'`). Two replicas
  observing `Delivered` from Sameday race; the losing replica logs
  `sameday.tracking.race-lost` at Info and moves on. **No duplicate
  emails.**
- Each replica's `SamedayTokenProvider` cache is independent (ADR-013).
  N replicas → up to N parallel `/api/authenticate` calls per token
  cycle. Well within Sameday's documented rate budget for any
  realistic N; the 5 req/s rate-limit policy applies per-replica, not
  globally. Plan accordingly if you scale out to ~20+ replicas
  before bolt 046 lands Redis.
- The above is fine for single-digit replica counts. If the business
  decides to scale meaningfully wider, bolt 046 (Redis introduction)
  is the natural next step; ADR-015 and ADR-016 will be augmented
  rather than superseded.

### 12.7 Operations playbook

**Pause the integration immediately** (e.g., Sameday outage,
suspected misconfiguration after a deploy):

```bash
# Quickest path — flip the lifecycle flag, redeploy.
Sameday__Jobs__Enabled=false
```

The hot path (`webhook → notifier`) becomes a `Task.CompletedTask`
no-op. Existing AWBs are NOT touched. Orders that were in flight at
flip-time stay in `Paid AND AwbNumber IS NULL`; when you flip the
flag back on, `AwbRetryJob`'s startup sweep picks them up.

**Pause everything including credential validation** (e.g.,
credentials rotated and you don't want failing auth calls in the
log noise):

```bash
Sameday__Enabled=false
# (Sameday__Jobs__Enabled is ignored when the master flag is off.)
```

Boot reverts to `StaticShippingService`. Existing AWBs are preserved;
the manual fallback workflow takes over for new orders.

**Force-retry a specific order's AWB creation**: today, there's no
admin endpoint. Two options:
1. Wait for the next `AwbRetryJob` tick (≤ 60 min).
2. Set the order's `PaidAt` to "now" via a DB update — the retry job
   uses `PaidAt > now - 24h` as the inclusion predicate. Use as a
   last resort; the natural retry cadence is usually fine.

A proper admin "force-retry" button is flagged as a follow-up in
[story 002](../memory-bank/intents/015-sameday-shipping-integration/units/002-awb-and-tracking-jobs/stories/002-awb-retry-job.md)'s
"Out of Scope" section.

**Force-stop tracking for a specific order** (e.g., Sameday lost the
parcel and we've reissued manually):
- The order's `ShippedAt < now - 30 days` excludes it from polls
  automatically.
- Inside the 30-day window, the only escape is to manually transition
  the order to `Delivered` (or `Cancelled`) via the admin UI. The
  tracking job filters on `Status == Shipped` exclusively.

**Credentials rotation**:
- Update the env vars / user-secrets in your deployment target.
- Restart the API host(s). Each replica re-authenticates on its next
  outbound Sameday call.
- The cached in-process token is bound to the process; there is **no
  hot-reload path** for credentials. Until bolt 046 lands Redis +
  potentially an admin "invalidate token" button, credential
  rotation requires a deploy.

### 12.8 What to monitor

Filter Serilog by these structured-log keys:

| Log message | Level | Means |
|---|---|---|
| `Sameday token refreshed. ExpiresAt=…` | Info | Healthy. Fires per replica per token cycle (Sameday's tokens are ~24 h). |
| `sameday.awb.created order_id=… awb=… attempt=N` | Info | Happy path. `attempt=1` is real-time; `attempt>1` means the dispatcher's in-process backoff fired. |
| `sameday.awb.skipped order_id=…` | Info | No label was needed: order missing, not `Paid`, already has an AWB, another worker holds a fresh claim, or Sameday deduped onto the number already stored. Healthy, and excluded from the AWB SLO on both sides. |
| `sameday.awb.orphaned order_id=… created_awb=… persisted_awb=…` | Error | A billable label was created but the order was no longer writable, so nothing references it and Sameday has no void endpoint here. **Void it manually at the courier**, then reconcile the order. Counted as an AWB SLO failure, not as a skip. |
| `sameday.awb.retry-scheduled order_id=… attempt=N delay=Ds` | Info | Transient Sameday failure; in-process retry queued. |
| `sameday.awb.non-transient-retry-later order_id=… reason=…` | Warning | Sameday is up but returned auth-fail or protocol-fail. Investigate; the retry job will keep trying. |
| `sameday.awb.permanent-fail order_id=… reason=…` | Error | Our request is malformed (bad postal code, weight over the courier ceiling, …). Admin manual fallback. |
| `sameday.awb.give-up order_id=… status=… paid_at=…` | Error | 24 h elapsed without success. Admin manual fallback required. Fires for `Paid` **and `Printing`** — an order advanced before its label existed is the case nothing recovers, so `status` tells you which. Wire admin notifications in a future intent. |
| `sameday.shipment.delivered order_id=… awb=…` | Info | Tracking transition succeeded; customer email queued. |
| `sameday.tracking.race-lost order_id=…` | Info | Multi-replica race; another instance won. Expected, not a problem. |
| `sameday.tracking.polling-stopped order_id=… shipped_at=…` | Warning | Order has been `Shipped` for > 30 days. Admin manual closure required. |

The PR description should call out which alerts you want set up.
At minimum, page on **`sameday.awb.give-up`** (24-h failure means a
real order is stuck) and **count(`sameday.awb.permanent-fail`) > 0**
in any 1-hour window (means our code is producing bad requests).

### 12.9 Cost ceiling

Sameday's rate budget is ~10 req/s; our policy throttles at 5 req/s
per replica ([SamedayPolicies.cs](../src/PhotoPrint.API/Services/Sameday/SamedayPolicies.cs)).
For a deployment doing ≤ 200 orders/day with all jobs enabled,
expected outbound Sameday call volume is roughly:

- AWB creation: 1 call/order × 200/day ≈ 200/day.
- Tracking poll: 1 call per `Shipped` order per 15 min for up to 30 d
  = at most ~2880 polls per order. With 200/day shipping and ~3-day
  delivery, sustained active cohort is ~600 orders × 4 polls/h ≈
  2400 polls/h ≈ 0.67 req/s. Comfortable.

The rate-limit ceiling is for *bursts* (deploy + startup sweep), not
sustained traffic.

### 12.10 Future considerations

Two non-implemented items worth tracking:

1. **Live tracking webhooks (Sameday → us push notifications)** —
   would replace the 15-min poll. Not in this intent; would be a
   future intent's work. Today's polling is sufficient for current
   volume.
2. **Admin notifications on `AwbCreationGivenUp` + `ShipmentPollingStopped`** —
   today the structured-log Error / Warning is the only signal. A
   future intent will route these to whatever admin notification
   surface exists at that point (email, in-app banner, SignalR
   broadcast).

---

## 13. Error tracking with Sentry (intent 020 / bolt 045)

### 13.1 What the integration does

When an unhandled exception escapes any controller or middleware, the API:

1. Routes it through `ExceptionHandlerMiddleware` (existing behaviour: log + ProblemDetails 500).
2. **Captures it to Sentry** with full context — stack trace, correlation id, user id (when authenticated), environment, release SHA, scrubbed request metadata.
3. Sentry pages / Slacks / emails per your project's alert rules (configured in the Sentry UI, not here).

What reaches Sentry is decided by **status code, not by whether the exception is mapped**:

- Unhandled exceptions (the `else` branch in `ExceptionHandlerMiddleware`) → 500 → captured.
- Mapped exceptions whose status is **≥ 500** (today only `BadGatewayException` → 502) → captured, and logged at `Error` rather than `Warning`. A mapped 5xx is a dependency failure that burns SLO 1, so it is not an expected business outcome. A caller who disconnects mid-request is normally **not** one of these: the cancellation is rethrown at the call site rather than translated into a 502, so it reaches the client-abort guard and never becomes a Sentry issue. One deliberate exception since 2026-08-05: if the dependency's own deadline had **already** elapsed when the caller disconnected, that is a real outage the caller merely stopped waiting for, so it is reported as a 502 and captured. The trade is one-sided on purpose — the alternative silently returned 200 for genuine outages under exactly the conditions that produce them.
- Mapped exceptions below 500 (`NotFoundException`, `ConflictException`, `UnprocessableEntityException`, …) → **NOT** captured. They are expected business outcomes, and capturing them would exhaust the quota the alert rules in §13.8 depend on.

Standalone `LogError` calls that throw no exception (`sameday.awb.orphaned`, for example) do **not** reach Sentry — they land in the Serilog file sink only. `UseSerilog` is wired with `writeToProviders` left at its default `false` (`SerilogExtensions.cs`), which disables the SDK's MEL-provider auto-capture. Do not flip that flag to "fix" this: it would double-capture every exception the middleware already reports explicitly, and auto-ship every `LogError` in the repo. Cross-check `Error`-level logs against Sentry per §13.8 instead.

### 13.2 Master flag — `Sentry:Enabled`

The integration follows the same two-stage rollout posture as the Sameday integration (§12.2):

- `Sentry:Enabled` (master). Default: `false`. When false, the SDK is never constructed, no middleware is registered, no events leave the host. Boot is byte-identical to baseline.
- `Sentry:Dsn` provisioned via secret store (see 13.4). With the master flag off, an empty DSN is irrelevant.

When `Sentry:Enabled=true`, the SDK runs; when `Sentry:Dsn` is also valid, events flow.

### 13.3 Provisioning prerequisites

1. **Create a Sentry project** (sentry.io or self-hosted).
   - Platform: **.NET / ASP.NET Core**.
   - Take note of the project DSN (looks like `https://abc123@o0.ingest.sentry.io/0`).
2. **Configure alert rules in Sentry** (the events arrive; Sentry decides who gets notified).
   - Minimum recommended: alert on every new issue + alert on issue regression.
   - For payment-webhook errors (see §13.8): tag-based filter `correlation_id` exists + path matches `/api/webhooks/*` → page immediately.
3. **Set release tagging via the deploy workflow.** The `GIT_COMMIT_SHA` env var is read at boot and tagged on every event. This is already set by the deploy workflow shipped in bolt 040 — no extra work, just verify `echo $GIT_COMMIT_SHA` in the container prints the deployed commit.

### 13.4 Secret management

The DSN is a secret (it identifies your Sentry project and is rate-limit-tied). Set it the same way as the JWT private key (§5):

| Environment | Mechanism |
|---|---|
| Local dev | `dotnet user-secrets set "Sentry:Enabled" "true" --project src/PhotoPrint.API` then `dotnet user-secrets set "Sentry:Dsn" "https://abc@o0.ingest.sentry.io/0" --project src/PhotoPrint.API` |
| Staging / Production | Env vars `Sentry__Enabled=true` and `Sentry__Dsn=https://...` in your `.env` / orchestrator secret store |

Full env-var reference for Sentry:

| Env var | Type | Default | Purpose |
|---|---|---|---|
| `Sentry__Enabled` | bool | `false` | Master flag — flip to true to wire the SDK |
| `Sentry__Dsn` | string | `""` | Project DSN from Sentry UI |
| `Sentry__Release` | string | `$GIT_COMMIT_SHA` | Override the release tag (rare; default is the deploy SHA) |
| `Sentry__Environment` | string | ASP.NET env name | Override the environment tag |
| `Sentry__SampleRate` | double | `1.0` | Fraction of error events to send (1.0 = all) |
| `Sentry__TracesSampleRate` | double | `0.1` | Fraction of transactions to send for performance tracing |
| `Sentry__Debug` | bool | `false` | **Verbosity**, not on/off. SDK internal logging is always on (see below); `true` drops it from `Warning` to `Debug` level (noisy; only for diagnosing wiring) |

**SDK-internal failures are always logged.** Sentry's `SentryOptions.DiagnosticLogger`
property returns `null` whenever `Debug` is `false` — so with `Debug=false` the SDK is not
merely quiet, it is deaf: an exhausted monthly quota returns 429 and every event is dropped
inside the SDK with nothing in the application log. `Program.cs` therefore sets `Debug=true`
unconditionally and uses `Sentry__Debug` to pick `DiagnosticLevel` (`Warning` normally,
`Debug` when the flag is on). Transport failures, rate limits and rejected envelopes reach
the Serilog file sink through the SDK's own MEL logger.

**What this still does not give you:** no metric counts dropped events and no health check
covers Sentry reachability, so "no new Sentry issues" is only trustworthy as far as the log
grep in §13.8 goes. Grep the log for `Sentry` at `Warning` or above when validating a rollout.

### 13.5 Recommended rollout sequence

Same two-stage posture as the Sameday rollout (§12.5):

1. **Pre-flight (config only).** Provision the DSN in staging secrets. Leave `Sentry__Enabled=false`. Deploy. Verify the API still boots with no Sentry-related log lines — proves the disabled path is byte-identical.

2. **Stage 1 — Sentry on, low blast radius.** Flip `Sentry__Enabled=true` in **staging only**. Trigger a known synthetic error (hit a debug endpoint or briefly add a `throw` in a low-stakes controller). Verify the event lands in your Sentry project with the right tags (`correlation_id`, `release`, `environment=staging`). Watch the dashboard for a week.

3. **Stage 2 — Production.** Flip `Sentry__Enabled=true` in production. Monitor the Sentry project for the first hour — any flood of new issues represents a pre-existing pile of un-logged errors finally surfacing. That's a feature, not a bug.

### 13.6 PII scrubbing

The scrubber is **deny-by-default**: a value leaves the process only if its key is on an allow-list. Adding a field to Sentry's payload therefore cannot leak by omission — a new header, query parameter, extra or span tag arrives redacted until someone allow-lists it deliberately.

It runs on all three SDK egress hooks — `BeforeSend` (error events), `BeforeSendTransaction` (performance transactions, which `BeforeSend` does **not** cover) and `BeforeBreadcrumb`.

**Scrubbed:**

- **Request and response bodies** — always replaced with `<scrubbed:request-body>`. The ProblemDetails response in our error path already contains the useful sanitized info; the raw body is never needed.
- **Query string** — every value replaced with `<scrubbed>`; parameter names are kept (`?search=<scrubbed>&page=<scrubbed>`). A segment that is not a plain `name=value` pair is dropped whole. Admin order search and email confirmation both carry PII/credentials here.
- **URLs** — query string, fragment and any embedded credentials stripped, in `Request.Url`, span descriptions and breadcrumb data.
- **Headers** (request and response) — everything except `Accept`, `Accept-Encoding`, `Accept-Language`, `Content-Length`, `Content-Type`, `Host`, `User-Agent`, `X-Correlation-Id`. Matching is case-insensitive, so HTTP/2's lowercase field names are covered.
- **Cookies**, **`Request.Env`** except `SERVER_NAME`/`SERVER_PORT`, and **`Request.Other`**.
- **Every `Extra` value**, and every span tag/extra and breadcrumb data value outside the diagnostic allow-list (HTTP method, status code, `db.system`).
- **User** — only `Id` survives; email, username, IP address and custom fields are dropped.
- **Log-message parameters** and the rendered message text (the template is kept).
- **Exception `Mechanism.Data`** — this is where a CLR exception's `Data` dictionary lands.

**Kept:** stack traces, exception type and message, tags (`correlation_id`, `user_id`, `release`, `environment`), transaction/route names, HTTP status codes, and the SDK's own device/OS/runtime contexts.

**If the scrubber throws, the payload is dropped, not sent.** This is deliberate: Sentry 4.13 sends the *original, unscrubbed* payload when a `BeforeSend`-family callback throws, so the scrubber catches everything, logs at `Error`, and returns null.

The allow-lists live in [`Configuration/SentryDataScrubbers.cs`](../src/PhotoPrint.API/Configuration/SentryDataScrubbers.cs). Widening one is a 1-line change plus a test — and a deliberate privacy decision.

### 13.7 Operations playbook

**Pause Sentry without a deploy** — flip `Sentry__Enabled=false` and restart the container. The SDK never constructs. Events stop immediately.

**Sentry quota approaching** — drop `Sentry__SampleRate` (e.g., `0.5` keeps half) or `Sentry__TracesSampleRate` (default already low at `0.1`). Errors at sample rate 0.5 is usually fine; you'll still notice spikes via the dashboard.

**Sentry's own outage** — Sentry SDK queues with a bounded in-memory buffer and drops oldest on overflow. The API never blocks on Sentry; an outage on Sentry's side is invisible to customers.

**Replay a missed event** — Sentry doesn't replay. The event was either captured (in Sentry, find it via the correlation_id from your Serilog logs) or it wasn't (the SDK queue overflowed during a flood). The Serilog logs always have the full picture; Sentry is the alerting layer on top.

**Rotate the DSN** — provision the new DSN, set the env var, rolling-restart the API. Old in-flight events go to the old DSN; new events go to the new one. No coordination needed.

### 13.8 What Sentry alerts on (suggested)

Configure in the Sentry UI, not in code. Suggested baseline:

| Signal | Severity | Notification |
|---|---|---|
| Any new issue (first occurrence of an exception type/location) | **Page** | Slack + email |
| Issue regression (resolved issue happens again) | **Page** | Slack |
| Issue volume > 10× baseline in 5 min | **Page** | Slack + email — likely an incident |
| Issue tagged `correlation_id` (any) on `/api/webhooks/*` path | **Page** | Slack — payment webhooks are SLA-critical, see [`slos.md`](../memory-bank/operations/slos.md) §3 |
| `Error`-level log with no matching Sentry event | — | Every 5xx exception is captured (§13.1), so a mismatch means a standalone `LogError` — cross-check the Serilog file sink against Sentry monthly |

### 13.9 Cost envelope

Free tier: 5k errors + 10k transactions / month. With the defaults shipped (errors at 100%, transactions at 10%) plus the SLO targets in [`slos.md`](../memory-bank/operations/slos.md):

- Availability target ≥ 99.5% → ≤ 1/200 requests is a 5xx → ≤ 0.5% of a few hundred req/day daily = a handful of error events per day, well under the free tier.
- Transactions at 10% × ~500 req/day = ~50/day = ~1500/month, under the free tier.

If volumes grow 10×, transactions become the constraint; drop `Sentry__TracesSampleRate` to `0.01` or upgrade.

### 13.10 Service Level Objectives

This bolt also ships a written SLO record + a starter Grafana dashboard:

- **[`memory-bank/operations/slos.md`](../memory-bank/operations/slos.md)** — 5 SLOs: availability, checkout latency, payment-webhook success, AWB auto-creation success, ANAF submission success.
- **[`ops/dashboards/fototipar-overview.json`](../ops/dashboards/fototipar-overview.json)** — Grafana dashboard JSON, 8 panels mapping to the SLOs.

**Caveat**: the dashboard panels reference metrics that ship with bolt 044 (OTel + Prometheus business counters), so they need the scrape stack of §14 running. A panel still reading "No Data" once Prometheus shows the target UP is a metric-name mismatch, not a missing feature — check [`metrics.md`](../memory-bank/operations/metrics.md). The SLO doc has the long-form explanation.

### 13.11 Not implemented (future considerations)

1. **Frontend Sentry SDK (Angular)** — UI errors today surface only through the existing toast service. A separate intent would wire `@sentry/angular`.
2. **Burn-rate alerts on SLOs** — Sentry alerts on individual events; SLO burn-rate alerts (e.g. "we're consuming error budget too fast") need a Prometheus + AlertManager stack that bolt 044 starts.
3. **PagerDuty / OpsGenie integration** — Sentry's built-in Slack + email is enough until on-call rotations exist.

---

## 14. Tracing and metrics (intent 020 / bolt 044)

OpenTelemetry traces pushed over OTLP, plus a Prometheus scrape endpoint carrying runtime and
business metrics. Same two-stage flag posture as Sentry (§13.2) and Sameday (§12.2): with the
master flag off, nothing is wired and `/metrics` does not exist.

**Read 14.3 before you enable anything.** The scrape endpoint has no login — the whole of its
access control is *where the request came from*, and getting that wrong publishes your order
volumes, error rates and deploy health to anyone who asks.

### 14.1 What the stack does

| Signal | Where it goes | Default |
|---|---|---|
| Traces (ASP.NET, HttpClient, EF Core spans) | OTLP push to a collector you provision | off |
| Metrics (runtime + business counters/histograms) | Prometheus **pull** from `GET /metrics` | off |

Metric names, labels and emission sites: [`memory-bank/operations/metrics.md`](../memory-bank/operations/metrics.md).
The SLOs these feed: [`memory-bank/operations/slos.md`](../memory-bank/operations/slos.md).
Starter dashboard: [`ops/dashboards/fototipar-overview.json`](../ops/dashboards/fototipar-overview.json).

### 14.2 Master flag — `Observability:Enabled`

`Observability__Enabled` (default `false`). When false the OTel SDK is never wired, no
middleware is registered, and `/metrics` returns the SPA fallback like any unknown path — boot
is byte-identical to baseline. Everything below only matters once it is `true`.

### 14.3 How `/metrics` is protected — read this first

Two independent gates, both enforced by `MetricsEndpointIpAllowListMiddleware`
(see [ADR-018](../memory-bank/bolts/044-tracing-and-metrics/adr-018-metrics-endpoint-ip-allow-list-not-jwt.md)):

1. **The scrape listener.** The API binds a second Kestrel port (`9090` in the shipped
   `docker-compose.prod.yml`) and `Observability__Metrics__ScrapePort` names it. A request for
   the scrape path arriving on any *other* listener gets `404`. Caddy proxies `:8080` only, so
   the endpoint has no route from the internet even if the allow-list is wrong.
2. **The IP allow-list.** On the scrape listener, the peer address must be in
   `Observability__Metrics__AllowedScrapeIps`, or the request gets `403` with an empty body.

The shipped `Caddyfile` also answers `/metrics*` itself with a `404`, so the path is refused at
the edge as well. Three things that follow, in order of how badly they bite:

> **Never put Caddy's address in `AllowedScrapeIps`.** Every request Caddy forwards arrives at
> the API from Caddy's own container IP — an internal scraper and an anonymous visitor look
> identical at that point. Allow-listing it is the one configuration that would make the metric
> store public. The scrape-port gate is what stops it from working, and that is deliberate.

> **`X-Forwarded-For` is not a fix.** It is a header any client can set. It is used nowhere in
> this decision, and it must not be introduced here.

> **If you change `Observability__Metrics__PrometheusEndpoint`, update the `Caddyfile` matcher
> to the new path** in the same change. The scrape-port gate still holds if you forget, but the
> edge refusal will be pointing at nothing.

### 14.4 Provisioning Prometheus

The scraper must be able to reach the API's scrape port, which is on the Compose network and is
not published to the host. That means the scraper runs **on that network**. Add to
`docker-compose.prod.yml`:

```yaml
  prometheus:
    image: prom/prometheus:latest
    restart: unless-stopped
    depends_on:
      - api
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - prom_data:/prometheus
    # No `ports:` — reach the UI over an SSH tunnel:
    #   ssh -L 9091:localhost:9091 user@HOST   (add `ports: ["127.0.0.1:9091:9090"]` first)
```

…and a `prometheus.yml` next to the compose file:

```yaml
global:
  scrape_interval: 15s
scrape_configs:
  - job_name: fototipar-api
    static_configs:
      - targets: ['api:9090']
```

Then add the `prom_data:` volume to the `volumes:` block, and put the Prometheus container's
address in the allow-list (14.5).

> **Do not add `ports:` to the `api` service for 9090.** The scrape listener is a *second full
> API pipeline*, not a metrics-only port — it serves every route, over plain HTTP, without the
> TLS, HSTS and security headers Caddy adds. Publishing it puts the unprotected API on the
> internet. Reach it from another container on the Compose network, never from the host.

**Hosted Prometheus (Grafana Cloud, AMP, …) is pull-based and lives off-box, so it cannot reach
an unpublished port — and the answer is *not* to publish one or to allow-list Caddy.** Run an
in-network agent that scrapes locally and pushes out: Grafana Alloy or the OTel Collector's
Prometheus receiver, with `remote_write` to the hosted endpoint. The agent is just another
container on the Compose network, so it is allow-listed exactly like Prometheus above.

### 14.5 The allow-list

`Observability__Metrics__AllowedScrapeIps` accepts plain IPv4/IPv6 addresses **and CIDR ranges**.
Every entry is parsed at boot; a single unparseable entry aborts startup with an
`OptionsValidationException` naming it, so a typo can never silently disable scraping.

| Entry | Meaning |
|---|---|
| `127.0.0.1`, `::1` | The shipped default. Local dev and same-host scrapers only. |
| `172.20.0.0/16` | A Compose network subnet — use this, because Compose assigns container IPs dynamically and a fixed address for the Prometheus container is not guaranteed. |
| `10.244.0.0/16` | A k8s pod CIDR for the monitoring namespace. |

Find your Compose network's subnet with:

```sh
docker network inspect fototipar_default -f '{{range .IPAM.Config}}{{.Subnet}}{{end}}'
```

Rules the validator enforces: at least one entry; every entry parses; CIDR base addresses have
all host bits zero (it will tell you to write `10.42.0.0/16`, not `10.42.0.5/16`); no
whitespace-only entries; no leading-zero forms (`010.0.0.1` is read as octal by .NET and is
rejected rather than silently becoming `8.0.0.1`); no IPv4-mapped IPv6 ranges like
`::ffff:10.42.0.0/112`, which would match nothing. Note that `0.0.0.0/0` **is** accepted — it
means "any IPv4 source", is almost never what you want, and is only safe if the scrape port is
firewalled elsewhere.

Env-var syntax for arrays is indexed:

```sh
Observability__Metrics__AllowedScrapeIps__0=172.20.0.0/16
Observability__Metrics__AllowedScrapeIps__1=127.0.0.1
```

> **Indexed env vars merge with the defaults, they do not replace them.** `appsettings.json`
> ships `["127.0.0.1", "::1"]`, so setting only index `0` leaves `::1` in place at index 1. To
> get exactly the list you want, set every index you intend to keep.

### 14.6 Provisioning the OTLP collector

`Observability__Otlp__Endpoint` must be an absolute `http(s)` URL; `Observability__Otlp__Protocol`
is `Grpc` (default, port 4317) or `HttpProtobuf` (port 4318). Any OTLP-compatible backend works —
a self-hosted OTel Collector, Jaeger, Tempo, or a vendor endpoint.

> **Always set `Otlp__Endpoint` outside Development.** The console span exporter — full
> spans, SQL text included, written to container stdout on the request thread — is a dev
> convenience, not a production mode, so it is reachable **only** when
> `ASPNETCORE_ENVIRONMENT=Development`. Anywhere else, a blank endpoint means the trace
> pipeline is not built at all: metrics still export, traces are absent, and the API writes
> `observability.tracing.disabled` once at boot to whatever sink Serilog is configured with
> (in production that is the rolling file, not stdout). Grep the API log for that line if
> spans never arrive.

Spans carry EF Core SQL command text. `EnableSensitiveDataLogging` is off, so parameter values
are not included — but treat the collector as a system that sees your query shapes, and keep it
inside your own network.

### 14.7 Sampling and cost

| Key | Default | Meaning |
|---|---|---|
| `Observability__Sampling__Default` | `1.0` | Fraction of traces exported. One rate for the whole service. `0.0` exports errored spans only. |

Sampling is deterministic on the trace id ([ADR-017](../memory-bank/bolts/044-tracing-and-metrics/adr-017-deterministic-trace-id-sampling.md)),
so a rate-sampled request's spans are all kept or all dropped — never a partial trace. The one
exception is an errored request the rate would have dropped: its server span is exported alone,
tagged `fototipar.sampling.error_override`, because its children were already gone by the time
the failure was known.

**There is no per-route rate.** The sampler decides while the server span is being
created, before routing has matched an endpoint, so no route is available to key on;
`Observability__Sampling__Routes__<key>` existed until 2026-08-03, silently matched
nothing, and has been removed. If you need one route cheaper than another, do it in the
collector with tail sampling — that is also the only way to get a *complete* errored
trace, since the in-app override can only rescue the span that failed, not the children
already dropped when it started. A rescued span is tagged
`fototipar.sampling.error_override`.

**A caller's `traceparent` does not change any of this.** The inbound trace id is kept so traces
join up across services, but the sampled flag on it is ignored: the rate above decides every span.
Until 2026-08-05 the flag won, which meant `curl -H 'traceparent: 00-…-00'` made a request
untraceable — including its 500s — at any rate. **The trace id itself still steers the decision**,
because sampling is a hash of it: a caller who picks an id that hashes below the rate is sampled at
any rate above zero, and one who picks an id above it is not. That is inherent to deterministic
sampling and is the documented way to force a trace on for debugging — send a chosen `traceparent`
rather than restarting with a higher rate. It also means the rate is not a hard cap against a
caller who wants to be traced.

**Sentry's own sampling was a separate door and is closed separately.** `TracesSampleRate` is only
consulted for a transaction nothing else has already decided, and an inbound `sentry-trace` header
counts as deciding — so until 2026-08-05 `curl -H 'sentry-trace: <id>-<span>-1'` made every request
a fully-sampled Sentry transaction regardless of the rate, and `-0` blinded performance monitoring.
A `TracesSampler` now answers with our configured rate on every call, and Caddy strips `sentry-trace`
and `baggage` from proxied requests (`traceparent` is deliberately **not** stripped — see above).
Two limits worth knowing: the edge strip only covers traffic that goes through Caddy, and an inbound
`sentry-trace` can still supply the trace and parent-span ids, which is trace continuity, not a
sampling or quota decision.

Metrics cost nothing per request to a scraper; cardinality is the budget, and every label value
is a constant (see `metrics.md`). Traces are the line item: at a few hundred requests a day,
`Default=1.0` is affordable. `0.1` is the next stop; below that, add the collector's tail
sampling rather than reaching for a smaller number.

**What lowering the rate actually saves.** Child spans and network egress — not the per-request
span work. An out-of-rate inbound request is *held* rather than dropped (that is what lets an
errored one still export), and a held span has `IsAllDataRequested` set, so the ASP.NET Core
instrumentation still writes its tags and resolves its status. Treat `Default` as an egress and
storage lever, not a CPU one; if per-request tracing overhead is the problem, the switch is
`Observability__Enabled=false`, which also turns off metrics.

### 14.8 Rollout sequence

1. **Pre-flight.** Deploy with `Observability__Enabled=false`. Confirm `/metrics` is not served
   and the API boots clean — proves the disabled path.
2. **Stage 1 — metrics only, staging.** Set `Enabled=true`, `ScrapePort=9090`, the allow-list,
   and leave `Otlp__Endpoint` blank. `ASPNETCORE_URLS` must already carry `http://+:9090`
   alongside the API port — a `ScrapePort` the process does not listen on refuses to boot (14.10),
   so a host that has only the API port will restart-loop rather than 404. Outside Development
   that means no trace pipeline is built — metrics only, nothing on stdout — and the boot log says
   so. Run the 14.9 checks. Watch for a week.
3. **Stage 2 — traces.** Point `Otlp__Endpoint` at the collector. Confirm spans arrive and the
   volume matches what `Sampling__Default` implies.
4. **Stage 3 — production.** Same flags, production allow-list. Re-run 14.9 against the live
   host, including the "must not be reachable from the internet" checks.

### 14.9 Post-deploy verification

From **outside** — every one of these must fail. Any that returns Prometheus text (lines
starting `# HELP`) is a live exposure, so treat it as an incident and turn `Enabled` back off:

```sh
for p in /metrics /METRICS /metrics/ '/metrics?x=1' /%6Detrics //metrics /x/../metrics; do
  echo "== $p"; curl -sS -o /dev/null -w '%{http_code}\n' "https://SITE_ADDRESS$p"
done
# Repeat with --http1.1 and --http2: a proxy and its backend can disagree about
# path normalization, and that disagreement is what a bypass looks like.
```

From **inside** the Compose network — this must succeed:

```sh
docker compose -f docker-compose.prod.yml exec prometheus \
  wget -qO- http://api:9090/metrics | head -5      # expect "# HELP …"
```

And confirm the gates actually deny:

```sh
docker compose -f docker-compose.prod.yml exec api \
  wget -S -qO- http://localhost:8080/metrics       # expect 404 — wrong listener
```

- [ ] Boot logs contain no `observability.metrics.scrape_port_unset` warning.
- [ ] Boot logs contain no `OptionsValidationException` mentioning `AllowedScrapeIps`.
- [ ] Prometheus's own Targets page shows `fototipar-api` as **UP**.
- [ ] The Grafana dashboard's panels resolve (a panel reading "No Data" means a metric name
      mismatch, not a scrape failure — check `metrics.md`).

### 14.10 Operations playbook

**Turn it all off without a deploy** — `Observability__Enabled=false`, restart the container.
The endpoint disappears and no spans are exported.

**Scraper suddenly gets 403** — its address changed (container reschedule, new replica). The
API logs one line per distinct denied source: `metrics.scrape.denied ip=…`. Grep for it, then
widen the allow-list to the subnet rather than chasing individual addresses. Only the first
512 distinct sources are logged; past that you get one
`metrics.scrape.denied.log_cap_reached` warning and silence until restart, which in practice
means you are being scanned, not misconfigured.

**Scraper gets 404** — it is talking to the wrong listener. The API logs one
`metrics.scrape.wrong_listener ip=… port=…` line per distinct source, naming the port it
arrived on and the port that would work. Check `ScrapePort` against `ASPNETCORE_URLS`; the port
must appear in both, and the scrape target must use it. A `ScrapePort` missing from
`ASPNETCORE_URLS` entirely now refuses to boot rather than 404ing forever, so if the API is
running you are looking at a scrape target pointed at the wrong port, not a config mismatch.

**API won't boot after a config change** — read the `OptionsValidationException`; it names the
offending key and, for allow-list entries, the exact entry and how to write it. A boot failure
carrying `observability.metrics.scrape_listener_invalid` at `Critical` is a different check and
is **not** an `OptionsValidationException`. It fires for one of three reasons: the scrape port is
not bound at all; it is the process's only listener, so the gate would protect nothing; or the
process listens on no TCP port at all (only a unix socket or a named pipe), so nothing could
scrape it. The message names the configured port, and either the ports actually bound or — in the
third case — the addresses. Under `restart: unless-stopped` this presents as a restart loop, so
read the Critical line rather than the resulting Caddy 502s.

**Dashboards go dark** — check Prometheus Targets first (scrape-side), then `/metrics` from
inside the network (API-side). A dark dashboard with a healthy target is a metric-name problem.

**Cardinality alarm** — every label value in this app is a compile-time constant. If a series
count grows without bound, a new call site introduced a free-form label; `metrics.md` lists the
legal values.

### 14.11 Environment variable reference

| Env var | Type | Default | Purpose |
|---|---|---|---|
| `Observability__Enabled` | bool | `false` | Master flag |
| `Observability__ServiceName` | string | `PhotoPrint.API` | `service.name` on every span |
| `Observability__Otlp__Endpoint` | string | `""` | OTLP target; blank ⇒ console span exporter in Development, no tracing anywhere else |
| `Observability__Otlp__Protocol` | string | `Grpc` | `Grpc` or `HttpProtobuf` |
| `Observability__Metrics__PrometheusEndpoint` | string | `/metrics` | Scrape path; keep the `Caddyfile` matcher in sync |
| `Observability__Metrics__ScrapePort` | int | `0` | Listener that may serve the scrape path; `0` = every listener (dev only) |
| `Observability__Metrics__AllowedScrapeIps__<n>` | string | `127.0.0.1`, `::1` | Allowed scrape sources: addresses or CIDR |
| `Observability__Sampling__Default` | double | `1.0` | Service-wide trace sample rate; `0.0` = errored spans only |

### 14.12 Not implemented (future considerations)

1. **AlertManager / burn-rate alerts.** Prometheus scrapes; nothing alerts on the SLO burn rate
   yet. That needs an AlertManager and rules derived from `slos.md`.
2. **mTLS on the scrape endpoint.** Right now the internal network is the trust boundary. A
   multi-tenant or cross-org scrape consumer would need client certificates — a new ADR
   superseding ADR-018 for that topology.
3. **Log export.** Serilog writes to stdout only; the OTel logs signal is not wired.

---

## 15. Invoicing — VAT, e-Factura XML, ANAF SPV submission (intent 016 / bolts 038 + 039)

### 15.1 What the integration does

For every paid order the API:

1. **Computes a VAT breakdown** ([`VatCalculator`](../src/PhotoPrint.API/Services/VatCalculator.cs)) at order creation (bolt 038). The 3 columns `Order.NetTotalRon`, `Order.VatRon`, `Order.VatRate` are snapshotted from `Vat:Rate` at the moment of creation and never re-derived. Legal trail = the rate that was in effect when the customer paid.
2. **Allocates an invoice number** inside the payment webhook's existing transaction (bolt 039), via `IInvoiceNumberingService.NextNumberAsync("FT", year)`, which uses `nextval()` on a per-`(series, year)` `SEQUENCE`. The number is `FT-YYYY-NNNNN`. See [ADR-020](../memory-bank/bolts/038-vat-calculation/adr-020-postgres-sequence-for-invoice-numbering-accept-gap-on-rollback.md) for the gap-on-rollback trade-off and the quarterly audit (§15.8).
3. **Inserts the `Invoice` row** in the same transaction as the Order → Paid transition. The Invoice is the frozen legal artefact; the Order can be modified later (admin notes, status corrections) but the Invoice's monetary snapshot is immutable.
4. **Builds a UBL 2.1 + CIUS-RO compliant XML payload** ([`InvoiceXmlBuilder`](../src/PhotoPrint.API/Services/Invoicing/InvoiceXmlBuilder.cs)) asynchronously via `InvoiceUploadJob`. Stored on `Invoice.XmlPayload`.
5. **Renders a customer-facing PDF** ([`InvoicePdfRenderer`](../src/PhotoPrint.API/Services/Invoicing/InvoicePdfRenderer.cs)) via QuestPDF; stores it via `IStorageService` at `invoices/yyyy/MM/{InvoiceNumber}.pdf`. See [ADR-021](../memory-bank/bolts/039-efactura-anaf/adr-021-pdf-library-questpdf-not-puppeteersharp.md) for why QuestPDF over PuppeteerSharp.
6. **Uploads the XML to ANAF SPV** via OAuth 2 client-credentials + PKCS#12 client cert, polls for status, and lands the invoice in `Accepted` / `Rejected` / `Failed`. `InvoiceUploadJob : BackgroundService` runs every 30 minutes by default.
7. **Exposes admin tooling** at `/api/admin/invoices` — list, retry, raw-XML download.

VAT computation runs **unconditionally**; ANAF submission is gated by a master flag (§15.2).

### 15.2 Three knobs

| Flag | Path | Purpose |
|---|---|---|
| `Vat:Rate` | `Vat__Rate` | The Romanian VAT rate (default `0.19`). Snapshotted at order creation; changing this does NOT mutate existing orders. **Always on**; no master flag. |
| `Anaf:Enabled` | `Anaf__Enabled` | Master gate for ANAF submission. Off → no OAuth, no HTTP client, no worker. Invoice rows are still created (you need the legal artefact), but no XML or PDF is built either — the worker is their only caller — so `GET /api/orders/{id}/invoice` always answers 404. Boot fails fast if any required ANAF credential is missing when this is on. |
| `Invoicing:CustomerEmailAttachments:Enabled` | `Invoicing__CustomerEmailAttachments__Enabled` | **Dual-write rollout flag** per [ADR-022](../memory-bank/bolts/039-efactura-anaf/adr-022-dual-write-rollout-via-feature-flag.md). **Currently changes nothing customer-visible: no email send path is implemented.** Flipping it on only swaps which log line the notifier writes. It becomes the real seam once an email integration lands (§15.7). |

**Why three flags, not one:** the rollout posture for regulated work is "exercise the pipeline against real production data, but don't expose the customer-visible side effect until you've inspected the output." The dual-write flag is the seam.

**One tuning knob worth knowing (multi-replica deploys):** `Anaf:ClaimTtlMinutes` / `Anaf__ClaimTtlMinutes`, default `10`. When a worker picks up a `Pending` invoice it stamps a claim; this is how long that claim holds before another replica may reclaim the row. Set it **above** the duration of one full pass (XML build + PDF render + storage write + a 30 s ANAF upload timeout) or two replicas can work the same invoice at once and submit it twice — nothing in this codebase can then tell the duplicate filing from the first one. Set it too high and a replica that crashes mid-pass strands its invoice for that long. The validator rejects anything outside `2`–`1440`; single-replica deploys can leave the default alone.

**The knob beside it:** `Anaf:MaxUnknownUploadOutcomes` / `Anaf__MaxUnknownUploadOutcomes`, default `3`, validator range `1`–`10`. An upload that hits the client timeout may already be filed at ANAF, so every later attempt on that invoice is a blind re-post of the same number. The worker counts them on the row and, once this many have accumulated, parks the invoice as `Failed` with a `LastError` telling the operator to reconcile the number in SPV; `POST /api/admin/invoices/{id}/retry` accepts `Failed`, and retrying resets the count. Three attempts spend about two hours of the five-business-day submission deadline — but only at the default `Anaf:ClaimTtlMinutes`: the wait before the next attempt is whichever is longer, that TTL or the poll interval, so a TTL raised towards its 1440-minute ceiling spaces the same three attempts across days and leaves the operator far less of the deadline. Raise it only if your ANAF link times out routinely and duplicate filings are cheaper to reconcile than late ones.

### 15.3 Seller fiscal identity

Every UBL XML and PDF embeds the seller's fiscal identity. The `Seller:` config section is read at boot and validated by [`SellerSettingsValidator`](../src/PhotoPrint.API/Validators/SellerSettingsValidator.cs). A typo here invalidates every emitted invoice — fail fast at startup.

Required values (validator rejects empties / wrong shapes):

| Setting | Format | Example |
|---|---|---|
| `Seller:Name` | non-empty, ≤ 200 chars | `"FotoTipar SRL"` |
| `Seller:Cui` | `^RO\d{2,10}$` | `"RO12345678"` |
| `Seller:RegistrationNumber` | non-empty, ≤ 50 chars | `"J40/1234/2026"` |
| `Seller:Address:Line1` | non-empty | `"Str. Exemplu 1, Sector 1"` |
| `Seller:Address:City` | non-empty | `"București"` |
| `Seller:Address:PostalCode` | non-empty | `"010101"` |
| `Seller:Address:CountryCode` | ISO 3166-1 alpha-2 | `"RO"` |
| `Seller:IbanRon` | optional (cash-on-delivery sellers leave blank) | `"RO49AAAA1B31007593840000"` |

The `appsettings.json` ships placeholder values (`"FotoTipar SRL"`, `"RO12345678"`, etc.) — **these are placeholders only**; replace with the real entity's data before the first production deploy. The shipped placeholders pass the validator (so the API boots in dev), but emit invalid invoices.

The CUI **must** match the entity registered with ANAF, otherwise SPV will reject every upload.

### 15.4 ANAF SPV provisioning (OAuth + PKCS#12 cert)

ANAF requires two credentials for SPV API access:

1. **OAuth 2 client-credentials** (`Anaf:ClientId` + `Anaf:ClientSecret`) — issued via ANAF's "Aplicații OAuth" portal at `anaf.ro` after the entity registers as a regulated submitter.
2. **A PKCS#12 (`.p12`) client certificate** issued by a Romanian Qualified Trust Service Provider (typically certSIGN, DigiSign, or Trans Sped). Cost: ~€50–150/year for an entity-level cert; the cert files are emailed after fiscal-identity verification (CUI + ID document).

**The cert is what ANAF authenticates against** — the OAuth token is an additional layer on top. Without the cert, OAuth refuses to issue a token.

Provisioning workflow (operator-facing):

1. **Register the entity in ANAF SPV.** Go to `anaf.ro` → SPV → "Înregistrare persoană juridică". You'll need the CUI, the registration number (`J40/...`), and an authorised representative's qualified electronic signature.
2. **Order a Qualified Electronic Signature certificate.** Provider's website (`certsign.ro`, `digisign.ro`, `transsped.ro`). Choose the **company-level** variant (not personal). Validity: 1 or 2 years; renew before expiry (§15.10).
3. **Receive the `.p12` file and its password by email.** Cert files arrive as encrypted email attachments. The password is delivered separately (SMS or signed letter, per provider). **Treat the file as a secret — it grants tax-authority write access.**
4. **Register the OAuth app in ANAF.** ANAF SPV portal → "Aplicații" → register a new OAuth client. You'll upload the `.p12` cert here. ANAF returns `client_id` + `client_secret`.
5. **Confirm sandbox vs production endpoints.**
   - **Sandbox**: `https://api.anaf.ro/test/FCTEL/rest/` (validates submissions but never reports them to the fiscal authority — safe for the dual-write inspection week).
   - **Production**: `https://api.anaf.ro/prod/FCTEL/rest/` (real submissions reported to ANAF — invoices count toward your fiscal obligations).

### 15.5 Secret management

Aligned with [ADR-006](../memory-bank/bolts/041-secrets-management/adr-006-secret-history-accept-and-rotate.md): **credentials never live in source.** `appsettings.json` ships with `ClientSecret`, `CertPath`, `CertPassword` empty; the validator refuses to boot the app with `Anaf:Enabled = true` if any of these are blank, and **checks `File.Exists` on `CertPath` at boot** so a missing cert file is a fail-fast.

**Local dev** — `dotnet user-secrets`:
```powershell
cd src/PhotoPrint.API
dotnet user-secrets set "Anaf:Enabled"      "true"
dotnet user-secrets set "Anaf:BaseUrl"      "https://api.anaf.ro/test/FCTEL/rest/"
dotnet user-secrets set "Anaf:ClientId"     "your-sandbox-client-id"
dotnet user-secrets set "Anaf:ClientSecret" "your-sandbox-client-secret"
dotnet user-secrets set "Anaf:CertPath"     "C:/secrets/anaf-sandbox.p12"
dotnet user-secrets set "Anaf:CertPassword" "your-cert-password"
```

**Staging / production** — environment variables (double-underscore for nesting):
```bash
Anaf__Enabled=true
Anaf__BaseUrl=https://api.anaf.ro/prod/FCTEL/rest/
Anaf__ClientId=<from-anaf-portal>
Anaf__ClientSecret=<from-anaf-portal>
Anaf__CertPath=/etc/photoprint/secrets/anaf.p12
Anaf__CertPassword=<from-provider-sms>
Anaf__PollIntervalMinutes=30
Anaf__MaxBatchSize=50
Anaf__ClaimTtlMinutes=10
Anaf__MaxUnknownUploadOutcomes=3
Anaf__BackoffHours__0=1
Anaf__BackoffHours__1=4
Anaf__BackoffHours__2=16
Anaf__BackoffHours__3=64

# Stays off during the inspection week (§15.7):
Invoicing__CustomerEmailAttachments__Enabled=false
```

**Cert file deployment**: copy the `.p12` to the production host at `/etc/photoprint/secrets/anaf.p12` (or wherever you point `Anaf__CertPath`). File mode `0400` (read-only by the API process owner). **Never commit the cert.** **Never log the cert subject, thumbprint, or password.**

For systemd-managed deploys, the existing `EnvironmentFile=` pattern + a separate path mount handles both the env vars and the cert file.

Sentry's scope scrubber (§13.6) doesn't yet recognise `cert` / `password` / `client_secret` as sensitive substrings by default — verify your `SensitiveFieldNames` list includes substrings that cover ANAF before flipping prod. The base list already covers `password`; consider adding `cert` and `client_secret` if you log option blocks at debug level.

### 15.6 QuestPDF Community License obligation

The PDF renderer uses QuestPDF (per [ADR-021](../memory-bank/bolts/039-efactura-anaf/adr-021-pdf-library-questpdf-not-puppeteersharp.md)). The Community License is **free for companies with revenue < $1M USD/year** — sufficient for FotoTipar today.

The license is declared at process startup in `Program.cs`:
```csharp
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
```

**Ops checklist**:
- **Annual review**: at end of fiscal year, check whether revenue exceeded $1M. If yes, purchase a Professional license (~$700/year as of writing) at [questpdf.com](https://www.questpdf.com/pricing) and update the license type in `Program.cs`.
- **The check is on the honour system** — QuestPDF doesn't phone home. Compliance is the operator's responsibility.

### 15.7 Recommended rollout sequence

Two-stage rollout per [ADR-022](../memory-bank/bolts/039-efactura-anaf/adr-022-dual-write-rollout-via-feature-flag.md).

1. **Pre-flight (config only).** All `Anaf__*` env vars set with `Anaf__Enabled=false`. `Invoicing__CustomerEmailAttachments__Enabled=false`. Cert file at `/etc/photoprint/secrets/anaf.p12`. Deploy. Verify boot succeeds — note this proves nothing about the ANAF values themselves: every rule in `AnafSettingsValidator` is guarded by `Enabled`, so they are first checked at the stage-1 flip. Verify no `Anaf` log lines appear at Information level.

2. **Schema sanity.** Bolt 038's migration is **already applied** if you ran the standard deploy flow (§7). Confirm via:
   ```sql
   \dt invoices;
   SELECT relname FROM pg_class WHERE relname LIKE 'invoice_seq_%';
   -- Should list invoice_seq_ft_2026 (or current year).
   ```

3. **Stage 1 — sandbox flip.** In staging, point at the ANAF **sandbox** (`BaseUrl=https://api.anaf.ro/test/FCTEL/rest/`) and set `Anaf__Enabled=true`. Redeploy. Trigger a test order. Watch for:
   ```
   invoice.creation.number-attempted invoice_number=FT-2026-00001
   anaf.token.refreshed
   anaf.upload-job.batch size=1
   anaf.upload-job.xml-built invoice_id=…
   anaf.upload-job.pdf-rendered invoice_id=… key=invoices/2026/06/FT-2026-00001.pdf
   anaf.spv.upload upload_id=…
   ```
   Then after the next poll (≤ 30 min):
   ```
   anaf.spv.status upload_id=… status=Validated
   invoice.lifecycle.transition invoice_id=… Submitted -> Accepted
   ```
   Verify the row's `AnafStatus=Accepted`, `XmlPayload` non-null, `PdfStoragePath` non-null. **Run for 1 day in sandbox to flush any wire-protocol surprises.**

   If the credentials are wrong (the common stage-1 failure), the shape is:
   ```
   anaf.upload-job.auth-failed invoice_id=…              ← Error + Sentry, ONCE per alert window per replica
   anaf.upload-job.auth-outage-continues invoice_id=… alert_window_minutes=120 interval_minutes=30
                                                        ← Warning, ~every 2nd tick (see below)
   anaf.upload-job.auth-failure-skipped count=N          ← Warning, when the batch had other rows
   ```
   The Error is deliberately rate-limited to one page per **alert window**: without it a multi-day
   outage pages every poll interval. That window is four poll intervals with a two-hour floor, so
   it is 2 h at the default 30-minute cadence and it widens with `Anaf__PollIntervalMinutes` —
   slowing the worker down cannot outrun the rate limit. Four, because a row that just recorded an
   error sits out one interval, so consecutive auth attempts on it are up to two intervals apart.
   At the validated maximum of 1440 minutes the window is 96 h, still inside the 5-business-day
   submission deadline, so an outage nobody acts on pages at least twice before an invoice is
   late. Both numbers are on every `auth-outage-continues` line, so the pair is checkable in the
   logs rather than inferred. **Silence is not recovery** — the `auth-outage-continues`
   Warning is what tells them apart, and each affected row also carries the reason in
   `Invoices.LastError` (visible in the admin invoice list). Expect that Warning about every
   **other** tick (~60 min at the default cadence), not every tick: a row that just recorded an
   error waits out one poll interval before it is picked up again. A tick whose batch comes back
   empty logs nothing at all. The window is per-process, so N replicas page up to N times per
   window and a restart re-pages immediately.

4. **Stage 2 — production flip with customer emails OFF (dual-write inspection week).** In production:
   ```bash
   Anaf__Enabled=true
   Anaf__BaseUrl=https://api.anaf.ro/prod/FCTEL/rest/
   Invoicing__CustomerEmailAttachments__Enabled=false   # ← stays off
   ```
   Redeploy. **Every paid order now lands in ANAF for real.** The PDF is generated, stored, and available at `GET /api/orders/{id}/invoice` for the customer who knows to look. No email surfaces it — that integration does not exist yet, so the flag below is not what is holding it back.

5. **Inspection week (7 days):**
   - Daily: open `GET /api/admin/invoices` and verify the day's orders show `AnafStatus=Accepted`. If any are `Rejected` or `Failed`, read the `LastError` and address (typo in `Seller`, wrong VAT category, etc.). Use `POST /api/admin/invoices/{id}/retry` to re-queue.
   - Spot-check the rendered PDFs: download a few via `GET /api/orders/{id}/invoice` and verify they look right (logo, totals, buyer address, fiscal note). PDF fidelity issues found here are cheap to fix — the customer hasn't seen them yet.
   - Check `invoice_anaf_status_total` metric — `accepted` should dominate; ratio of `accepted : rejected` should be > 95:5.

6. **Stage 3 — flip customer emails on.** Blocked: there is no email send path to turn on. Setting
   ```bash
   Invoicing__CustomerEmailAttachments__Enabled=true
   ```
   changes nothing a customer sees — the notifier logs `invoice.pdf-ready.no-email-integration` instead of `invoice.pdf-ready.suppressed` and returns. Do this step only once an email attachment integration ships; until then customers reach their invoice at `GET /api/orders/{id}/invoice` and nowhere else.

7. **Post-rollout cleanup (eventually):** When the dual-write flag stays `true` for ≥ 30 days, a follow-up PR removes the flag + the `if (settings.Enabled)` branches. Tracked in the bolt-039 construction log per ADR-022.

**Rollback paths:**
- *PDF renderer bug surfaces.* This flag is not a rollback path today — it gates no customer-visible send. Flip `Anaf__Enabled=false` to stop the pipeline, or fix forward and re-render via `POST /api/admin/invoices/{id}/retry`.
- *ANAF rejecting most submissions.* Flip `Anaf__Enabled=false`. Invoices still get created with numbers (legal artefact), but no XML, no PDF, no SPV upload — and the customer download endpoint 404s until you re-enable. Investigate the rejection cause, fix, then re-enable. Use `POST /api/admin/invoices/{id}/retry` after re-enabling to push the accumulated backlog.
- *Total integration failure.* Flip both flags off. The Order → Paid transition still creates the Invoice row (you need the numbering allocated for the legal sequence), but no XML, no PDF, no upload. You're back to the pre-bolt-039 behaviour with a forward-compatible schema.

### 15.8 Quarterly invoice-number-gap audit

[ADR-020](../memory-bank/bolts/038-vat-calculation/adr-020-postgres-sequence-for-invoice-numbering-accept-gap-on-rollback.md) accepts that Postgres `SEQUENCE` advances on transaction rollback — meaning if the Stripe webhook's transaction rolls back after `nextval()` was called but before commit, the sequence has advanced and that number is "burned" (no invoice claims it). Romanian Fiscal Code forbids gaps.

**Mitigation: run the audit query quarterly** (end of March, June, September, December). Surface any gaps to the accountant with a brief explanation.

```sql
-- Find gaps in the current fiscal year's invoice numbering.
WITH expected AS (
  SELECT generate_series(1, COALESCE(MAX("Number"), 0)) AS n
  FROM "Invoices"
  WHERE "Series" = 'FT'
    AND EXTRACT(YEAR FROM "IssuedAt") = EXTRACT(YEAR FROM CURRENT_DATE)
)
SELECT expected.n AS missing_number
FROM expected
LEFT JOIN "Invoices" i
  ON i."Series" = 'FT'
  AND i."Number" = expected.n
  AND EXTRACT(YEAR FROM i."IssuedAt") = EXTRACT(YEAR FROM CURRENT_DATE)
WHERE i."Id" IS NULL
ORDER BY expected.n;
```

If the result is empty: no gaps, nothing to explain. If the result lists numbers: each one represents either (a) a Stripe webhook that crashed mid-transaction (extraordinarily rare with bolt 035's idempotency), or (b) a manual DB intervention. Document each gap + cause in an accountant-facing memo for the fiscal year close.

**Expected gap rate**: zero or near-zero. A handful of gaps over a year is defensible; systematic gaps would be a finding under audit.

### 15.9 Admin retry runbook

`POST /api/admin/invoices/{id}/retry` flips a `Rejected` or `Failed` invoice back to `Pending` for the next worker tick. 409 if the status isn't retryable.

**When to retry vs investigate first:**

| `LastError` symptom | Action |
|---|---|
| `"Conexiunea cu serverul a fost întreruptă"` / 5xx | Retry — likely a transient ANAF outage. |
| `"CUI invalid"` | **Do not retry.** Fix the `Seller:Cui` config first. The cert may be tied to a different CUI. |
| `"Documentul este invalid"` (XML schema) | **Do not retry blindly.** Inspect the XML via `GET /api/admin/invoices/{id}/xml` and diff against a known-good invoice. The error usually reveals a schema deviation introduced by a recent change. |
| `"Această factură a fost deja primită"` | Already-accepted. Retry is safe; ANAF will return the existing `index_incarcare`. |
| Status `Failed` (budget exhausted) | Investigate the original `LastError`, fix the cause, then retry. The retry re-uploads from scratch. |

**Batch retry** (no built-in endpoint — use the admin UI loop or a one-shot SQL):
```sql
-- Flip all Failed invoices from a specific date back to Pending. The worker picks up next tick.
UPDATE "Invoices"
   SET "AnafStatus" = 'Pending',
       "AnafUploadId" = NULL,
       "LastError" = NULL,
       "UpdatedAt" = NOW()
 WHERE "AnafStatus" = 'Failed'
   AND "CreatedAt" >= '2026-06-01';
```

### 15.10 What to monitor

The Sentry section (§13.8) covers exception alerts. For invoicing-specific signals, use Prometheus metrics + the Grafana dashboard:

| Signal | Source | Action if it fires |
|---|---|---|
| `invoice_anaf_status_total{status="failed"}` rate > 0 in 1h | Prometheus | Page — every Failed invoice is a manual-remediation candidate. |
| `invoice_anaf_status_total{status="rejected"}` rate > 5/day | Prometheus | Investigate. A sustained Rejected rate means a misconfiguration (Seller, VAT rate) is rejecting everything. |
| Cert expiry < 30 days | Manual / OpenSSL cron | Renew at the provider. New cert → new env var → restart. |
| Days-since-`Anaf__Enabled` flip with `CustomerEmailAttachments=false` > 14 | Manual | Document why the inspection extended. Flipping the flag on is not yet an option — no email send path exists (§15.7 step 6). |
| Quarter end | Calendar | Run the gap audit query (§15.8). |

**Cert expiry monitoring** (manual cron, no metric for this):
```bash
openssl pkcs12 -in /etc/photoprint/secrets/anaf.p12 -nodes -passin pass:$ANAF_CERT_PASSWORD \
  | openssl x509 -noout -enddate
# notAfter=Jun 15 12:00:00 2027 GMT
```
Set a calendar reminder 60 days before that date.

### 15.11 Cost envelope

| Cost item | Annual estimate |
|---|---|
| Qualified Electronic Signature cert (certSIGN / DigiSign / Trans Sped) | €50–€150 |
| ANAF SPV submissions | Free (regulated; ANAF doesn't charge per submission) |
| QuestPDF Community License | $0 (free below $1M revenue; ~$700/yr above) |
| PDF storage (S3 / R2) | Negligible — invoices are tens of KB; 1k orders/year = ~10 MB. Within R2's free tier. |
| Total marginal cost of bolt 039 | **€50–€150/year** (cert) — everything else is in the noise. |

### 15.12 Not implemented (future considerations)

1. **Credit notes / storno invoices.** Out of scope for this intent. Will reuse `IInvoiceNumberingService` with `Series="FS"` — no code rework needed.
2. **B2B invoices with buyer CUI.** Today every buyer is treated as a `Persoană fizică` (guest) and `BT-48 BuyerVATIdentifier` is omitted. Adding B2B requires capturing the buyer's CUI at checkout and emitting the corresponding party block.
3. **Multi-currency invoices.** Romanian invoices are RON-only by convention (and our shop is RON-only by design). A foreign-currency intent would require new UBL fields and a daily exchange-rate source.
4. **e-Receipts (`eAMEF`).** Romania's electronic-receipt mandate covers physical-presence retail; e-commerce is exempt. If we ever open a physical kiosk, a separate intent is needed.
5. **Frontend invoice viewer.** Today the customer hits `GET /api/orders/{id}/invoice` and gets a raw PDF. A dedicated Angular view (with download button + zoom + print) is a UX-team follow-up.
6. **Cert rotation without restart.** The `AnafTokenProvider` loads the cert once at first use. To rotate, restart the API. A SIGHUP-driven reload pattern exists but adds complexity not justified by the ~annual rotation cadence.
7. **Per-row attempt counter.** Per [ADR-024](../memory-bank/bolts/039-efactura-anaf/adr-024-implicit-attempt-count-from-updatedat-no-persisted-counter.md), the retry budget is derived from `(now - CreatedAt)` rather than a persisted column. If incidents make per-row retry history a frequent ops query, add the column under a new ADR.
