---
intent: 017-deployment-cicd
phase: inception
status: complete
created: 2026-05-25T10:20:00Z
updated: 2026-05-25T10:20:00Z
source: docs/analysis/architect-review-2026-05-25.md#5
priority_score: 20
---

# Requirements: Deployment & CI/CD

## Intent Overview

`memory-bank/standards/tech-stack.md` documents a Docker + GitHub Actions topology, but no Dockerfile, no docker-compose, and no workflow file actually exists on disk. Production is deployed by hand. This intent fills the gap: containerise the API + UI, provide reproducible local + production compose files, and wire CI (build/test) and CD (build/push image, deploy) on GitHub Actions.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Reproducible production deploys | One-button or `git push main` deploy with no manual VM SSH | Must |
| Disaster recovery in < 30 minutes | Compose + image tag can stand a replacement VM up in 30 min | Must |
| Pull request feedback | CI checks run on every PR with build + unit + integration tests | Must |
| Local dev parity | `docker compose up` brings API + Postgres + MailHog + UI dev server | Should |

---

## Functional Requirements

### FR-1: API Dockerfile (multi-stage)
- **Description**: Multi-stage Dockerfile builds the API into a runtime image. Non-root user. `HEALTHCHECK` against `/health`.
- **Acceptance Criteria**:
  - `docker build -t fototipar/api .` produces an image < 250 MB.
  - Container runs as user `app` (UID 1000+); not root.
  - `HEALTHCHECK` reports unhealthy after 3 failed `/health` calls.
  - `ENV ASPNETCORE_ENVIRONMENT=Production` set by default.
- **Priority**: Must
- **Related Stories**: US-017-1

### FR-2: docker-compose.yml (local dev)
- **Description**: Brings up API + Postgres 16 + MailHog. Optional Angular dev server target documented (host-run usually preferred for HMR).
- **Acceptance Criteria**:
  - `docker compose up` from repo root brings the stack up.
  - `.env.example` documents the required variables; `.env` is gitignored.
  - Volume mounts persist Postgres data across restarts.
- **Priority**: Must
- **Related Stories**: US-017-2

### FR-3: docker-compose.prod.yml + Caddy reverse proxy
- **Description**: Production compose deploys API + (optionally) Postgres + Caddy with Let's Encrypt automatic TLS.
- **Acceptance Criteria**:
  - Caddy is the only ingress (ports 80/443).
  - API not exposed on host network.
  - HTTPS works automatically on first boot given correct DNS.
- **Priority**: Must
- **Related Stories**: US-017-3

### FR-4: CI workflow (ci.yml)
- **Description**: GitHub Actions workflow on `pull_request` and `push` to `main`: restore → build → test (.NET + Vitest) → upload artefacts.
- **Acceptance Criteria**:
  - Workflow fails on any test failure.
  - Caches NuGet + npm to keep wall-clock < 8 min on a warm cache.
  - Integration tests run against ephemeral Postgres service container.
- **Priority**: Must
- **Related Stories**: US-017-4

### FR-5: CD workflow (deploy.yml)
- **Description**: On `push` to `main`, build + tag image, push to GHCR, then deploy via SSH (production VM) or trigger DigitalOcean / Azure webhook.
- **Acceptance Criteria**:
  - Successful CI run on `main` triggers CD automatically.
  - Image tag follows `ghcr.io/<org>/fototipar/api:sha-<short>` + `:latest`.
  - Failure of CD halts rollout; previous container stays up.
- **Priority**: Must
- **Related Stories**: US-017-5

### FR-6: Secrets out of repo (transitional)
- **Description**: `appsettings.Development.json` and `appsettings.Production.json` contain placeholder values only; real secrets resolved from env vars in deployed environments.
- **Acceptance Criteria**:
  - Build fails fast in production if any required secret is missing (`Options` `ValidateOnStart`).
  - `README.md` documents the full env-var matrix.
- **Priority**: Must
- **Related Stories**: US-017-6 (overlaps with intent 018; see Q1)

---

## Non-Functional Requirements

### Reliability
| Requirement | Metric | Target |
|-------------|--------|--------|
| Mean time to recover | From green deploy to traffic restored after failure | < 15 min (revert tag) |
| Image size | API runtime image | < 250 MB |

### Security
| Requirement | Standard | Notes |
|-------------|----------|-------|
| Non-root containers | OCI best practice | UID/GID 1001 |
| Minimal base image | `mcr.microsoft.com/dotnet/aspnet:8.0-alpine` | Smaller surface |
| TLS | Caddy auto-cert | LE staging tested first |

### Performance
| Requirement | Metric | Target |
|-------------|--------|--------|
| CI wall clock | Warm cache full suite | < 8 min |

---

## Constraints

### Technical Constraints
- Must keep `dotnet user-secrets` workflow usable for dev.
- Postgres in `docker-compose.yml` is for dev only; prod points at managed DB.

### Business Constraints
- Ship before intent 019 (cloud storage) which depends on the deploy pipeline existing.

---

## Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| Single-VM deploy is acceptable until intent 021 lands Redis backplane | Multi-instance needed sooner | Compose is replaced with a small k8s/Nomad manifest in a follow-up |
| GHCR is acceptable image registry | Org policy mandates another | Workflow parameterised on registry host |
| Caddy is acceptable as ingress | Team prefers nginx | Caddy chosen for auto-cert; nginx alt documented |

---

## Open Questions

| Question | Owner | Due Date | Resolution |
|----------|-------|----------|------------|
| Q1: Bundle FR-6 here or rely on intent 018 for secret hygiene? | Backend | 2026-06-01 | Recommend keep `.env.example` + env-var matrix here; rotation and historical purge in 018 |
| Q2: Single image (API + Angular static) or separate? | DevOps | 2026-06-01 | Recommend single image — API serves static files behind Caddy |
