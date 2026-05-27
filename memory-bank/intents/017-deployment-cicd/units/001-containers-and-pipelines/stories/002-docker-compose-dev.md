---
id: 002-docker-compose-dev
unit: 001-containers-and-pipelines
intent: 017-deployment-cicd
status: draft
priority: must
created: 2026-05-25T10:20:00Z
assigned_bolt: 040-containers-and-pipelines
implemented: false
---

# Story: 002-docker-compose-dev

## User Story

**As** a developer
**I want** `docker compose up` to bring the full backend stack online with one command
**So that** I can develop without installing Postgres + SMTP locally

## Acceptance Criteria

- [ ] `docker-compose.yml` at repo root brings up `api`, `db` (Postgres 16), `mailhog`.
- [ ] API depends-on db; healthcheck-aware (`condition: service_healthy`).
- [ ] Postgres data persisted to named volume `pgdata`.
- [ ] MailHog UI reachable at `http://localhost:8025`; SMTP at `1025`.
- [ ] `.env.example` documents all required env vars; `.env` is gitignored.
- [ ] README has a "Run with Docker" section with one-liner command + first-time setup notes.

## Technical Notes

```yaml
services:
  db:
    image: postgres:16-alpine
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U fototipar"]
      interval: 5s
    environment:
      POSTGRES_USER: fototipar
      POSTGRES_PASSWORD: fototipar
      POSTGRES_DB: fototipar
    volumes: [pgdata:/var/lib/postgresql/data]

  mailhog:
    image: mailhog/mailhog
    ports: ["1025:1025", "8025:8025"]

  api:
    build: .
    depends_on:
      db: { condition: service_healthy }
    environment:
      ConnectionStrings__Default: Host=db;Database=fototipar;Username=fototipar;Password=fototipar
      Email__Smtp__Host: mailhog
      Email__Smtp__Port: 1025
    ports: ["8080:8080"]
    volumes: ["./_storage:/app/Storage"]

volumes:
  pgdata:
```

## Dependencies

### Requires
- 001-api-dockerfile

### Enables
- 003-docker-compose-prod-caddy (shared pattern)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Angular dev server | Run on host (`npm start`) — proxy to `http://localhost:8080/api`; documented in README |
| Windows host file permissions on `_storage` | Use named volume `apidata:/app/Storage` if mount fails |

## Out of Scope

- Production parity (next story).
