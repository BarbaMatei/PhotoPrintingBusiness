---
intent: 033-environment-triad
phase: inception
status: units-defined
created: 2026-06-05T12:00:00Z
updated: 2026-06-05T12:00:00Z
source: docs/analysis/ai-workflow-review-2026-06-05.md §6 (Phase 4 — The environment triad)
priority_score: roadmap-phase-4
---

# Requirements: Environment Triad — Infrastructure Readiness

## Intent Overview

The application today understands exactly **two** environment shapes: `Development` (Postgres via docker-compose, MailHog, relaxed CORS/rate-limits) and `Production` (Postgres, SendGrid, strict everything). The owner's roadmap Phase 4 (`docs/analysis/ai-workflow-review-2026-06-05.md` §6) calls for the infrastructure to be **ready to run in three distinct states**:

1. **Local testing** — a developer's machine (the existing `docker-compose.yml` + `appsettings.Development.json` story).
2. **Deployable dev environment** — *the first thing that will ever be deployed*: a sandbox to test and experiment freely, with its **own** config, secrets handling, and seed data. **This tier does not exist yet** — that is the central gap this intent fills.
3. **Production** — the existing `docker-compose.prod.yml` + Caddy + managed Postgres story, kept untouched in behaviour.

### Framing (CRITICAL — read before any story)

This intent prepares **infrastructure readiness only**. It is **NOT deployment**. Deployment is deliberately the **final** phase of the owner's roadmap, after stabilization (Phase 3) and EU-readiness (Phase 5). Therefore:

- **No** "deploy to production" work, no "ship the dev environment" work, no provisioning of a real server, no cutover, no go-live checklist execution.
- The deliverables are **configuration, separation, documentation, and tooling** that *make a future deployment possible and safe* — not the act of deploying.
- Where this intent produces a `docker-compose.dev-env.yml` or a `Development.DevEnv`/`DevEnvironment` config tier, those are **defined and validated locally**; standing them up on a real host is out of scope and explicitly deferred to Phase 6.
- Artifacts must avoid deployment-pressure language. The goal is "the dev tier is *definable and runnable in principle*", not "the dev tier is *live*".

### The gap, concretely

- **Config**: `appsettings.json` + `appsettings.Development.json` only. No third tier for the deployable dev sandbox (which is Postgres-backed like prod, but freely seedable and non-customer-facing like dev).
- **Secrets**: a single `.env` convention + `dotnet user-secrets` for local. No documented **per-environment** secrets strategy (which secrets each tier needs, where they live, how they differ — e.g. dev uses Stripe/EuPlatesc **test** keys, prod uses live).
- **Seeding**: `--seed` (`ProductCatalogSeed`) and `--seed-dev` (`DevDataSeed`) exist, but there is no per-environment policy for **what** gets seeded **where** (prod: catalog + reference data only; dev-env: catalog + rich demo data; local: same as dev-env or lighter).
- **Promotion**: no documented, repeatable **dev→prod promotion path** (config diffs, secret swaps, the image-tag flow that already exists in `deploy.yml`, and the migration/seed steps that differ between tiers).

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| A formal deployable-dev tier is defined | A third environment (config + compose + secrets template + seed policy) exists and boots locally with `ASPNETCORE_ENVIRONMENT` set to the dev-env value | Must |
| Config is cleanly separated across the three tiers | Each tier's config is layered, documented, and validated-at-boot; no tier leaks another's defaults | Must |
| Per-environment secrets strategy is documented | A matrix lists every secret × tier (test vs live keys, where stored, who provisions) with a per-tier `.env` template | Must |
| Per-environment seeding policy exists | A documented policy + a mechanism selecting the seed set per tier (prod = reference-only; dev-env = demo; local = dev) | Should |
| A dev→prod promotion path is documented | A repeatable runbook (config/secret diffs + the existing image-tag + migration/seed steps) — readiness only, not executed | Should |
| Production behaviour is unchanged | The existing prod compose/Caddy/deploy story still validates identically; no regressions | Must |

---

## Functional Requirements

### FR-1: Define the deployable-dev environment tier (config)

- **Description**: Introduce a third, named environment for the deployable dev sandbox, with its own layered configuration. It is Postgres-backed (production-shaped DB) but non-customer-facing and freely seedable. Define the `ASPNETCORE_ENVIRONMENT` value, the `appsettings.{DevEnv}.json` layer, and which settings differ from both Development (local) and Production.
- **Acceptance Criteria**:
  - The tier is named **`Staging`** (`ASPNETCORE_ENVIRONMENT=Staging` — resolved Q1, owner 2026-06-05) with an `appsettings.Staging.json` that layers over `appsettings.json`.
  - The Staging tier's connection string targets its own PostgreSQL service, and the tier uses **test-mode** payment keys, a relaxed-but-not-local CORS/rate-limit posture, and **MailHog** for email (resolved Q2 — captured inbox, nothing reaches a real address).
  - Boot-time options validation (`ValidateOnStart`) passes for the dev-env tier exactly as it does for prod (required secrets enforced; no silent fallback to Development defaults).
  - The tier is documented as **runnable locally** for validation; standing it up on a host is explicitly out of scope.
- **Priority**: Must
- **Related Stories**: 033 unit 001

### FR-2: Per-environment configuration separation & a dev-env compose definition

- **Description**: Make the three tiers' configuration cleanly separated and self-documenting, and add a `docker-compose.dev-env.yml` (or documented overlay) that expresses the dev-env tier the same way `docker-compose.yml` (local) and `docker-compose.prod.yml` (prod) express theirs.
- **Acceptance Criteria**:
  - A documented config map shows, per setting that varies, its value in each of the three tiers (local / dev-env / prod) — the single reference for "what differs".
  - A `docker-compose.dev-env.yml` exists: Postgres + API (+ MailHog or sandbox email), parameterised by a dev-env `.env`, with the dev-env `ASPNETCORE_ENVIRONMENT`. It is validated by `docker compose config` and a local boot, not by deploying it.
  - No tier inherits another tier's secrets or hostnames by accident (e.g. dev-env does not point at the prod DB or prod CORS origin).
  - The existing `docker-compose.yml` and `docker-compose.prod.yml` remain behaviourally unchanged.
- **Priority**: Must
- **Related Stories**: 033 unit 001

### FR-3: Per-environment secrets strategy

- **Description**: Document and template the secrets each tier requires and how they differ, building on the existing `.env.example` + `dotnet user-secrets` conventions and the ADR-006 "secrets via env vars, never appsettings" rule.
- **Acceptance Criteria**:
  - A **secrets × tier matrix** lists every secret (JWT keypair, Stripe, EuPlatesc, SendGrid, Google OAuth, ANAF, Sameday, storage credentials, Sentry DSN) against local / dev-env / prod, stating for each: required?, **test vs live** value, and where it lives (user-secrets / `.env` / platform secret store).
  - A `.env.dev-env.example` template exists alongside `.env.example`, pre-set to test-mode keys and dev-env hostnames, with no real secrets committed.
  - The matrix makes explicit that dev-env uses **test/sandbox** credentials (Stripe test, EuPlatesc test, ANAF test base URL, etc.) and that **live** credentials are prod-only.
  - The strategy references, and does not contradict, the existing secret-scanning pre-commit hook + Gitleaks CI (intent 018).
- **Priority**: Must
- **Related Stories**: 033 unit 002

### FR-4: Per-environment seeding policy & selection mechanism

- **Description**: Define what data is seeded in each tier and provide a mechanism to select the seed set by environment, building on the existing `--seed` / `--seed-dev` modes (`ProductCatalogSeed`, `DevDataSeed`).
- **Acceptance Criteria**:
  - A documented seeding policy states: **prod** = reference/catalog data only (`ProductCatalogSeed`, lockers), never demo users/orders; **dev-env** = catalog + rich demo data (`DevDataSeed`: demo users, sample orders) for free experimentation; **local** = same as dev-env or a lighter subset.
  - A selection mechanism applies the correct seed set per `ASPNETCORE_ENVIRONMENT` (e.g. a guard so `DevDataSeed` cannot run in Production, even if invoked).
  - The mechanism is idempotent and re-runnable, and reuses the existing seed classes (no new parallel seeder).
  - Documentation shows the exact command per tier (e.g. `dotnet PhotoPrint.API.dll --seed` for prod-shaped, `--seed-dev` for dev-env/local).
- **Priority**: Should
- **Related Stories**: 033 unit 002

### FR-5: Documented dev→prod promotion path (readiness runbook)

- **Description**: Write a repeatable promotion runbook describing how a change moves from the dev-env tier toward production — as **readiness documentation**, not an executed deployment. It ties together the config/secret differences (FR-1–3), the seeding differences (FR-4), the existing image-tag flow in `deploy.yml`, and the migration steps (DEPLOYMENT.md §7).
- **Acceptance Criteria**:
  - A `docs/environments/promotion-path.md` (or §-in-DEPLOYMENT.md) describes the dev-env → prod promotion as ordered, repeatable steps: config swap, secret swap (test→live), image tag/promote, migration apply, seed policy, smoke verification — clearly labelled **readiness, not a deploy instruction to run now**.
  - The runbook cross-references the three-tier config map (FR-2), the secrets matrix (FR-3), and the seeding policy (FR-4) rather than restating them.
  - The runbook explicitly notes deployment is **deferred to roadmap Phase 6** and that executing it is out of scope for this intent.
  - It records the migration-provider caveat (verify migrations against real Postgres before a first prod apply) as a precondition, linking DEPLOYMENT.md §7.
- **Priority**: Should
- **Related Stories**: 033 unit 003

---

## Non-Functional Requirements

### Reliability

| Requirement | Metric | Target |
|-------------|--------|--------|
| Boot-time validation per tier | `ValidateOnStart` for dev-env | Passes; fails loudly on missing required secret (no silent Development fallback) |
| Production regression | prod compose/Caddy behaviour | Unchanged; `docker compose config` for prod identical to before |

### Security

| Requirement | Standard | Notes |
|-------------|----------|-------|
| No secrets in config files | ADR-006 (env vars only) | dev-env templates carry placeholders, never real values |
| Tier isolation | secrets/hostnames per tier | dev-env never references prod DB, prod CORS origin, or live payment keys |
| Test-vs-live key separation | per-tier matrix | dev-env = test/sandbox keys; live keys are prod-only |

### Maintainability

| Requirement | Metric | Target |
|-------------|--------|--------|
| Seeder reuse | parallel seeders introduced | 0 (reuse `ProductCatalogSeed` / `DevDataSeed`) |
| Config duplication | hand-copied settings across tiers | minimised via layered `appsettings.{tier}.json` |
| Single source of truth | "what differs per tier" | one config map + one secrets matrix |

---

## Constraints

### Technical Constraints

**Project-wide standards**: tech-stack (Infrastructure & Deployment §), coding-standards, system-architecture, ADR-006 (secrets) loaded by Construction Agent.

**Intent-specific constraints**:
- **Infrastructure readiness only — NOT deployment.** No server provisioning, no cutover, no go-live. The dev-env tier is defined and validated *locally*; standing it up is Phase 6.
- Build from the existing assets: `docker-compose.yml`, `docker-compose.prod.yml`, `Caddyfile`, `.github/workflows/deploy.yml`, `.env.example`, `appsettings*.json`, `docs/DEPLOYMENT.md`. Add a third tier alongside them; do not rewrite the two existing ones.
- Reuse existing seed classes and the `--seed`/`--seed-dev` entrypoints; no parallel seeder.
- Honour ADR-006: secrets via env vars / user-secrets / platform store, never committed.
- Production behaviour and config must remain unchanged (regression-safe).

### Business Constraints
- Phase 4 of the roadmap: comes **after** Phase 3 (stabilization, intent 032) and **before** Phase 5 (EU-readiness) and Phase 6 (deployment). It unblocks a future deployment without performing one.
- No customer-facing behaviour change.

---

## Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| A third `ASPNETCORE_ENVIRONMENT` value is acceptable to introduce | Naming churn / config confusion | Pick a conventional name (`DevEnvironment` or `Staging`) and document it once (Q1) |
| The dev-env tier is Postgres-backed (prod-shaped DB) | Dev-env diverges from prod and hides bugs | Mandate Postgres for dev-env in FR-1; local runs Postgres via docker-compose too |
| Existing seed classes cover dev-env demo needs | Demo data insufficient for free experimentation | Extend `DevDataSeed` content if needed (still one seeder, not a new one) |
| Defining the tier locally is enough for "readiness" | Owner expects a running dev host | Framing is explicit: standing up the host is Phase 6; this intent stops at definable + locally validated |
| The migration chain applies cleanly on a first real Postgres apply | A first prod apply breaks | Promotion runbook (FR-5) records verifying it as a precondition (DEPLOYMENT.md §7) |

---

## Open Questions

| Question | Owner | Due Date | Resolution |
|----------|-------|----------|------------|
| Q1: Name for the third tier — `DevEnvironment`, `Staging`, or `Sandbox`? | Maintainer | 2026-06-05 | **RESOLVED (owner, 2026-06-05): `Staging`** — ASP.NET built-in name (`appsettings.Staging.json`, `IsStaging()`); colloquially still called "the dev environment" |
| Q2: Does dev-env use MailHog, a sandbox SendGrid sender, or a catch-all inbox? | Maintainer | 2026-06-05 | **RESOLVED (owner, 2026-06-05): MailHog** — captured inbox, nothing ever reaches a real address |
| Q3: Where do dev-env secrets ultimately live when the tier is eventually stood up (Phase 6) — platform store, env file, vault? | Maintainer | Phase 6 | **DEFERRED to Phase 6** (owner, 2026-06-05): document the secrets matrix now; the store choice is made when the tier is stood up |
| Q4: Promotion path home — a new `docs/environments/promotion-path.md` or a new §16 in DEPLOYMENT.md? | Maintainer | 2026-06-05 | **RESOLVED (recommendation adopted, 2026-06-05): dedicated `docs/environments/` dir**, cross-linked from DEPLOYMENT.md |
| Q5: Should the dev-env tier reuse `docker-compose.prod.yml` with an overlay, or a standalone `docker-compose.dev-env.yml`? | Maintainer | 2026-06-05 | **RESOLVED (recommendation adopted, 2026-06-05): standalone `docker-compose.staging.yml`** (prod file stays untouched; name follows the Staging tier decision) |
