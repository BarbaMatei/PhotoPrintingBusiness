---
unit: 001-containers-and-pipelines
intent: 017-deployment-cicd
phase: inception
status: complete
created: 2026-05-25T10:20:00Z
updated: 2026-05-25T10:20:00Z
---

# Unit Brief: Containers & Pipelines

## Purpose

Ship all six deliverables in one coherent ops bolt: API Dockerfile, dev compose, prod compose, CI, CD, and the env-var matrix.

## Scope

### In Scope
- `Dockerfile` (repo root) — multi-stage API build
- `.dockerignore`
- `docker-compose.yml`, `docker-compose.prod.yml`
- `.env.example`
- `.github/workflows/ci.yml`, `.github/workflows/deploy.yml`
- `Caddyfile` for prod
- `README.md` updates with env matrix + deploy runbook

### Out of Scope
- Kubernetes / Nomad migration
- Per-environment terraform / IaC
- Secret rotation history rewrite (intent 018)

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-1 | API Dockerfile (multi-stage) | Must |
| FR-2 | docker-compose.yml (local dev) | Must |
| FR-3 | docker-compose.prod.yml + Caddy | Must |
| FR-4 | CI workflow | Must |
| FR-5 | CD workflow | Must |
| FR-6 | Secrets out of repo (transitional) | Must |

---

## Story Summary

| Story ID | Title | Priority |
|----------|-------|----------|
| 001-api-dockerfile | Multi-stage Dockerfile with non-root user and HEALTHCHECK | Must |
| 002-docker-compose-dev | Compose for API + Postgres + MailHog | Must |
| 003-docker-compose-prod-caddy | Production compose + Caddy reverse proxy with LE | Must |
| 004-github-actions-ci | CI workflow: restore, build, test, artefacts | Must |
| 005-github-actions-deploy | CD workflow: tag image, push GHCR, deploy | Must |
| 006-env-vars-matrix | `.env.example` + README env-matrix + ValidateOnStart wiring | Must |

---

## Dependencies

### Depends On
- None (foundational ops)

### Depended By
- intent 019 (cloud storage), intent 020 (observability)
