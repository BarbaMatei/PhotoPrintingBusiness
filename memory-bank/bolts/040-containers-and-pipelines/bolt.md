---
id: 040-containers-and-pipelines
unit: 001-containers-and-pipelines
intent: 017-deployment-cicd
type: simple-construction-bolt
status: in-progress
stories:
  - 001-api-dockerfile
  - 002-docker-compose-dev
  - 003-docker-compose-prod-caddy
  - 004-github-actions-ci
  - 005-github-actions-deploy
  - 006-env-vars-matrix
created: 2026-05-25T10:20:00Z
started: 2026-05-27T09:00:00Z
completed: null
current_stage: implement
stages_completed:
  - name: plan
    completed: 2026-05-27T09:20:00Z
    artifact: implementation-plan.md

requires_bolts: []
enables_bolts: [041-secrets-management, 042-thumbnail-cache, 043-cloud-storage-provider, 044-tracing-and-metrics, 045-error-tracking, 046-distributed-state-redis]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 2
  max_dependencies: 0
  testing_scope: 2
---

# Bolt: 040-containers-and-pipelines

## Overview

One ops bolt containing six artefacts: Dockerfile, two compose files, CI, CD, env matrix.

## Objective

After this bolt, `git push main` results in a verified container image being deployed to the production VM with zero hand-rolled steps, and any contributor can stand the stack up with `docker compose up`.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Plan | `implementation-plan.md` — file layout, secret matrix, deploy target choice |
| 2 | Implement | Six new/updated files |
| 3 | Test | End-to-end: open a PR (CI green), merge (CD green), verify HTTPS site responds |

## Dependencies

- **Requires**: none.
- **Enables**: every later infra intent (018–021).

## Key Technical Notes

- `Caddyfile`, `Dockerfile`, `docker-compose*.yml`, `.dockerignore`, `.env.example`, `.github/workflows/{ci,deploy}.yml` all created in this bolt.
- The deploy target is the existing single VM; the workflow is parameterised so a managed-platform swap is config-only.
