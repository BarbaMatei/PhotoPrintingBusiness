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
