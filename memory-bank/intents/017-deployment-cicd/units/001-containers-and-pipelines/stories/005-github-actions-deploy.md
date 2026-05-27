---
id: 005-github-actions-deploy
unit: 001-containers-and-pipelines
intent: 017-deployment-cicd
status: draft
priority: must
created: 2026-05-25T10:20:00Z
assigned_bolt: 040-containers-and-pipelines
implemented: false
---

# Story: 005-github-actions-deploy

## User Story

**As** the team
**I want** `git push main` to deploy a verified container to production
**So that** releases are routine and reversible

## Acceptance Criteria

- [ ] `.github/workflows/deploy.yml` triggers on `push` to `main` after CI succeeds (`workflow_run`).
- [ ] Builds and pushes image `ghcr.io/<org>/fototipar/api:sha-<short>` + `:latest`.
- [ ] Deploy step:
  - Default: SSH to a single VM running `docker-compose.prod.yml`, run `docker compose pull && docker compose up -d`.
  - Alt: trigger a managed deploy webhook (DigitalOcean App Platform / Azure Container Apps) — config-only swap.
- [ ] Rollback documented: `docker compose up -d --force-recreate` with the prior `:sha-...` tag pinned in `.env`.
- [ ] All secrets (registry token, SSH key, DB connection string) read from GitHub Actions secrets, never echoed.

## Technical Notes

```yaml
name: deploy
on:
  workflow_run:
    workflows: [ci]
    types: [completed]
    branches: [main]
jobs:
  publish-and-deploy:
    if: ${{ github.event.workflow_run.conclusion == 'success' }}
    runs-on: ubuntu-latest
    permissions: { packages: write, contents: read }
    steps:
      - uses: actions/checkout@v4
      - uses: docker/setup-buildx-action@v3
      - uses: docker/login-action@v3
        with: { registry: ghcr.io, username: ${{ github.actor }}, password: ${{ secrets.GITHUB_TOKEN }} }
      - uses: docker/build-push-action@v6
        with:
          push: true
          tags: |
            ghcr.io/${{ github.repository_owner }}/fototipar/api:sha-${{ github.sha }}
            ghcr.io/${{ github.repository_owner }}/fototipar/api:latest
      - name: SSH deploy
        uses: appleboy/ssh-action@v1
        with:
          host: ${{ secrets.DEPLOY_HOST }}
          username: ${{ secrets.DEPLOY_USER }}
          key: ${{ secrets.DEPLOY_SSH_KEY }}
          script: |
            cd /opt/fototipar
            docker compose -f docker-compose.prod.yml pull api
            docker compose -f docker-compose.prod.yml up -d api
            docker image prune -af --filter "until=72h"
```

## Dependencies

### Requires
- 003-docker-compose-prod-caddy, 004-github-actions-ci

### Enables
- Future: blue/green or canary

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Deploy fails after image push | Container stays on prior tag; `:latest` resolves to previous sha until success — document the override `IMAGE_TAG=sha-<prev>` |
| Migrations on container start | Run via `dotnet ef database update` at API boot OR a dedicated pre-deploy job (recommend boot-time + ValidateOnStart) |

## Out of Scope

- Multi-region failover.
