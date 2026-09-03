# syntax=docker/dockerfile:1
# ──────────────────────────────────────────────────────────────────────────────
#  FotoTipar combined image — ASP.NET Core API that also serves the Angular SPA.
#  (Bolt 040, decision D1: one image; split to a separate static host later if
#   traffic warrants — see docs/DEPLOYMENT.md.)
# ──────────────────────────────────────────────────────────────────────────────

# ── Stage 1: build + publish the .NET API ─────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS api-build
WORKDIR /src
# Restore against just the API project first for Docker layer caching.
COPY src/PhotoPrint.API/*.csproj ./PhotoPrint.API/
RUN dotnet restore ./PhotoPrint.API/PhotoPrint.API.csproj
COPY src/PhotoPrint.API/ ./PhotoPrint.API/
RUN dotnet publish ./PhotoPrint.API/PhotoPrint.API.csproj \
      -c Release -o /app/publish /p:UseAppHost=false

# ── Stage 2: build the Angular SPA ─────────────────────────────────────────────
FROM node:22-alpine AS ui-build
WORKDIR /ui
COPY src/PhotoPrint.UI/package*.json ./
RUN npm ci
COPY src/PhotoPrint.UI/ ./
# @angular/build:application emits to dist/PhotoPrint.UI/browser
RUN npm run build -- --configuration=production

# ── Stage 3: runtime ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
# curl for HEALTHCHECK; non-root runtime user. icu-libs + icu-data-full because this base
# image ships no ICU and the invoice PDF renders in ro-RO; icu-libs alone pulls the
# English-only data set, which carries no Romanian locale data.
# Recent aspnet:8.0-alpine tags already ship `app` at 1001, and creating it again fails the build.
RUN apk add --no-cache curl icu-libs icu-data-full \
 && (getent group app > /dev/null || addgroup -g 1001 app) \
 && (id -u app > /dev/null 2>&1 || adduser -D -u 1001 -G app app)
WORKDIR /app
COPY --from=api-build /app/publish ./
COPY --from=ui-build  /ui/dist/PhotoPrint.UI/browser ./wwwroot
# Writable uploads dir (mount a volume here in compose for persistence).
RUN mkdir -p /app/Storage && chown -R app:app /app
USER app
EXPOSE 8080
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD curl -fsS http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "PhotoPrint.API.dll"]
