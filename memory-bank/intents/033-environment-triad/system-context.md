---
intent: 033-environment-triad
phase: inception
status: context-defined
updated: 2026-06-05T12:10:00Z
---

# Environment Triad - System Context

## System Overview

A configuration / infrastructure-readiness layer that teaches the application to run in **three** distinct, cleanly-separated tiers — **local testing**, a new **deployable dev environment** (the sandbox tier that does not yet exist), and **production** — without performing any deployment. The deliverables are: a third named `ASPNETCORE_ENVIRONMENT` + layered `appsettings`, a `docker-compose.dev-env.yml`, a per-tier config map, a secrets × tier matrix + `.env.dev-env.example`, a per-environment seeding policy reusing the existing seed classes, and a dev→prod promotion runbook written as readiness documentation. Everything is defined and validated **locally**; standing the dev tier up on a host is deferred to roadmap Phase 6. Production behaviour is unchanged.

## Context Diagram

```mermaid
C4Context
    title System Context - Environment Triad (infrastructure readiness, NOT deployment)

    Person(dev, "Developer", "Runs local tier; validates dev-env tier locally")
    Person(maint, "Maintainer", "Owns the secrets matrix + promotion runbook")

    System_Boundary(triad, "Three configuration tiers") {
        System(local, "Local testing", "ASPNETCORE_ENVIRONMENT=Development · Postgres · MailHog · docker-compose.yml")
        System(devenv, "Deployable dev env (NEW)", "Postgres · test-mode keys · seedable demo data · docker-compose.dev-env.yml")
        System(prod, "Production", "Postgres (managed) · live keys · Caddy · docker-compose.prod.yml — UNCHANGED")
    }

    System(api, "PhotoPrint.API", "Reads ASPNETCORE_ENVIRONMENT → layered appsettings + env vars")
    System_Ext(seeds, "Seed modes", "--seed (ProductCatalogSeed) / --seed-dev (DevDataSeed)")
    System_Ext(gha, "deploy.yml (existing)", "Image-tag flow referenced by the promotion runbook")
    System_Ext(docs, "Docs", "Config map · secrets matrix · seeding policy · promotion-path.md")

    Rel(dev, local, "Runs + tests")
    Rel(dev, devenv, "Validates locally")
    Rel(maint, docs, "Authors + reviews")
    Rel(local, api, "Development config")
    Rel(devenv, api, "Dev-env config (test keys)")
    Rel(prod, api, "Production config (live keys)")
    Rel(api, seeds, "Applies per-tier seed set")
    Rel(docs, gha, "References image-tag flow (readiness only)")
```

## External Integrations

- **Existing seed entrypoints** (`--seed` / `--seed-dev`): reused, with a per-tier policy and a Production guard. No new seeder.
- **Existing `deploy.yml` image-tag flow**: *referenced* by the promotion runbook as the mechanism a future deployment would use — not invoked or modified here.
- **Existing secret-scanning (intent 018)**: the per-tier secrets strategy must remain compatible with the pre-commit hook + Gitleaks CI.
- **Payment/email/ANAF/Sameday sandboxes**: dev-env uses their **test** modes; the matrix records test-vs-live per tier.

## Builds-From Assets (extend, do not rewrite)

`docker-compose.yml` (local) · `docker-compose.prod.yml` (prod) · `Caddyfile` · `.github/workflows/deploy.yml` · `.env.example` · `appsettings.json` + `appsettings.Development.json` · `docs/DEPLOYMENT.md`.

## High-Level Constraints

- **Infrastructure readiness only — NOT deployment.** No provisioning, no cutover, no go-live. Dev-env is defined + validated locally; standing it up is Phase 6.
- Add a third tier *alongside* the two existing compose files; leave both behaviourally unchanged.
- Reuse existing seed classes + `--seed`/`--seed-dev`; honour ADR-006 (secrets via env vars only).
- Dev-env is Postgres-backed (prod-shaped); local runs Postgres via docker-compose too.
- No deployment-pressure language in any artifact.

## Key NFR Goals

- A third tier that boots locally with `ValidateOnStart` passing exactly like prod (no silent Development fallback).
- One config map + one secrets matrix as the single sources of truth for "what differs per tier".
- Per-tier seed policy with a Production guard on demo data; idempotent; one seeder.
- A promotion runbook that is repeatable readiness documentation, explicitly deferring execution to Phase 6.
- Production config/behaviour unchanged (regression-safe).
