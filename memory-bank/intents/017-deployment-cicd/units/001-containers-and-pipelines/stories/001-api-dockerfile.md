---
id: 001-api-dockerfile
unit: 001-containers-and-pipelines
intent: 017-deployment-cicd
status: draft
priority: must
created: 2026-05-25T10:20:00Z
assigned_bolt: 040-containers-and-pipelines
implemented: false
---

# Story: 001-api-dockerfile

## User Story

**As** a release engineer
**I want** a multi-stage Dockerfile that produces a minimal, non-root API runtime image
**So that** deploys are reproducible and the attack surface is small

## Acceptance Criteria

- [ ] `docker build -t fototipar/api .` succeeds from a clean clone.
- [ ] Final image size < 250 MB.
- [ ] Runtime user is `app` (UID 1001, non-root).
- [ ] `HEALTHCHECK CMD wget -q --spider http://localhost:8080/health || exit 1` (or `curl` equivalent) configured with sensible interval/retries.
- [ ] `ENV ASPNETCORE_ENVIRONMENT=Production` and `ENV ASPNETCORE_URLS=http://+:8080`.
- [ ] Static Angular assets included in the image; served by the API.
- [ ] `.dockerignore` excludes `bin/`, `obj/`, `node_modules/`, `.git/`, `*.user`, `appsettings.*.local.json`.

## Technical Notes

Multi-stage:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/PhotoPrint.API/*.csproj ./PhotoPrint.API/
COPY src/PhotoPrint.Tests/*.csproj ./PhotoPrint.Tests/
RUN dotnet restore ./PhotoPrint.API/PhotoPrint.API.csproj
COPY src/ ./
RUN dotnet publish ./PhotoPrint.API/PhotoPrint.API.csproj -c Release -o /app/publish

FROM node:22-alpine AS ui-build
WORKDIR /ui
COPY src/PhotoPrint.UI/package*.json ./
RUN npm ci
COPY src/PhotoPrint.UI/ ./
RUN npm run build -- --configuration=production

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
RUN addgroup -g 1001 app && adduser -D -u 1001 -G app app
WORKDIR /app
COPY --from=build      /app/publish ./
COPY --from=ui-build   /ui/dist/photo-print-ui ./wwwroot
USER app
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD wget -q --spider http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "PhotoPrint.API.dll"]
```

## Dependencies

### Requires
- None

### Enables
- 002-docker-compose-dev, 003-docker-compose-prod-caddy

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| ImageSharp native libs missing on Alpine | Add `apk add --no-cache libgdiplus` or switch to bookworm-slim base if needed |
| `Storage/` volume mount path | Documented in compose files |

## Out of Scope

- Separate UI container.
