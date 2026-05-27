---
name: scan-architecture
description: >
  Scans a codebase's full structure: folder layout, tech stack, service boundaries,
  database schema, API contracts, deployment config, and security model.
  Use this skill first, before any other analysis skill.
allowed-tools:
  - read_file
  - list_files
  - search_files
  - run_command
---

# Architecture Scanner

## What to scan — in this order

### 1. Project structure
- Run `find . -type f | head -200` to get a full file listing
- Identify the top-level layout: monolith, monorepo, microservices
- Note each service or module and its apparent responsibility

### 2. Tech stack detection
Look for these files and extract the stack from them:
- `package.json` / `yarn.lock` / `pnpm-lock.yaml` → Node.js stack
- `requirements.txt` / `pyproject.toml` / `Pipfile` → Python stack
- `go.mod` → Go stack
- `pom.xml` / `build.gradle` → Java/Kotlin stack
- `Gemfile` → Ruby stack
- `*.csproj` / `*.sln` → .NET stack
- Framework clues: look for express, fastapi, django, spring, rails, nextjs, nuxt, etc.

### 3. Database schema
Read ALL of these if they exist:
- `**/migrations/**/*.sql` — SQL migration files
- `**/schema.prisma` — Prisma schema
- `**/models.py` — Django/SQLAlchemy models
- `**/entity/*.ts` or `**/entities/*.ts` — TypeORM entities
- `**/*.mongoose.ts` or `**/schemas/*.ts` — MongoDB schemas

For each table/model, note:
- Fields and types
- Indexes defined
- Missing obvious fields (soft delete, audit timestamps, foreign keys)
- N+1 risks (relations without eager loading options)

### 4. API contracts
Read ALL of these if they exist:
- `**/openapi.yaml` / `**/swagger.yaml` / `**/swagger.json`
- `**/schema.graphql` / `**/*.graphql`
- `**/*.proto` — gRPC definitions
- If none found, read the route files: `**/routes/**`, `**/controllers/**`, `**/resolvers/**`

For each endpoint group, note:
- Authentication required (yes/no)
- Pagination present (yes/no)
- Rate limiting headers (yes/no)
- API versioning (yes/no)
- Error response format (consistent/inconsistent)

### 5. Security model
Look for:
- Auth implementation: `**/auth/**`, `**/middleware/auth*`, `**/guards/**`
- JWT / OAuth2 / session config
- Secret management: `.env`, `*.env.example`, hardcoded secrets in source (search for `SECRET`, `PASSWORD`, `API_KEY` in non-.env files)
- CORS configuration
- Input validation / sanitisation libraries

### 6. Deployment & infrastructure
Read:
- `docker-compose.yml` / `docker-compose*.yml`
- `Dockerfile` / `**/Dockerfile`
- `.github/workflows/*.yml` — CI/CD pipelines
- `**/k8s/**` / `**/kubernetes/**`
- `**/terraform/**` / `**/infra/**`
- `*.tf` files

Note: environment count, replica strategy, health checks defined, resource limits set.

### 7. Observability
Look for:
- Logging library config (`winston`, `pino`, `loguru`, `log4j`, `zap`, etc.)
- Tracing setup (`opentelemetry`, `jaeger`, `datadog`)
- Metrics (`prometheus`, `statsd`, `datadog`)
- Error tracking (`sentry`, `bugsnag`, `rollbar`)
- APM config files

## Output format for this skill

Produce a structured Markdown section titled **Architecture Scan Results** with subsections matching the 7 areas above. Be factual — only report what you actually found in files. Flag anything you could not read.
