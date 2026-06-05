---
intent: 025-security-dependency-hygiene
phase: inception
status: context-defined
updated: 2026-06-05T09:30:00Z
---

# Security & Dependency Hygiene - System Context

## System Overview

This intent operates on the **build-time dependency surface** and the **HTTP boot pipeline** of `PhotoPrint.API` — not on customer-facing runtime behaviour. It removes a known CVE, unifies package versions through Central Package Management, automates upgrades via Renovate, and makes the `/metrics` IP allow-list correct behind the production reverse proxy. The "actors" are mostly tooling and operators.

## Context Diagram

```mermaid
C4Context
    title System Context - Security & Dependency Hygiene

    Person(dev, "Maintainer / Dev", "Reviews + merges dependency PRs")
    Person(scraper, "Metrics Scraper", "Prometheus, allow-listed by IP")
    System(api, "PhotoPrint.API", "ASP.NET Core 8 monolith")
    System_Ext(nuget, "NuGet.org", "Package source")
    System_Ext(gh, "GitHub + Renovate App", "CI + automated upgrade PRs")
    System_Ext(caddy, "Caddy reverse proxy", "TLS termination + forwards X-Forwarded-For")
    System_Ext(otel, "OTel collector / Stripe", "Downstream of patched OTel deps")

    Rel(dev, gh, "Reviews grouped upgrade PRs")
    Rel(gh, api, "CI: restore/build/test against pinned versions")
    Rel(api, nuget, "Restores from Directory.Packages.props")
    Rel(scraper, caddy, "GET /metrics")
    Rel(caddy, api, "Forwards with X-Forwarded-For")
    Rel(api, otel, "Exports traces/metrics via patched OTel 1.15.x")
```

## External Integrations

- **NuGet.org**: package source; Central Package Management pins one version per package at restore.
- **GitHub + Renovate App**: grouped, scheduled upgrade PRs; vulnerability alerts labelled `security`.
- **Caddy reverse proxy**: terminates TLS and sets `X-Forwarded-For`; the `/metrics` allow-list must trust only its CIDR.
- **OpenTelemetry collector / Stripe**: downstream consumers of the patched OTel pipeline and unified Stripe.net SDK.

## High-Level Constraints

- .NET 8 LTS; single Docker image (Caddyfile + Dockerfile + docker-compose.prod.yml at root).
- Sequential ship order P01 → P02 → P03 → P05 (same `*.csproj` / `Program.cs` files).
- No customer-facing behaviour change.

## Key NFR Goals

- `dotnet list package --vulnerable` returns clean.
- Exactly one resolved version per package solution-wide.
- `/metrics` allow-list evaluates the real client IP (no spoofing via untrusted proxies).
