---
unit: 001-dependency-and-boot-hardening
intent: 025-security-dependency-hygiene
phase: inception
status: draft
unit_type: backend
default_bolt_type: simple-construction-bolt
created: 2026-06-05T09:30:00Z
updated: 2026-06-05T09:30:00Z
---

# Unit Brief: Dependency & Boot Hardening

## Purpose

Make the dependency tree auditable and the boot pipeline production-correct: zero known CVEs, one resolved version per package, automated upgrades, and a `/metrics` allow-list that works behind the reverse proxy. Ops/infra work — uses `simple-construction-bolt` (no domain modelling).

## Scope

### In Scope
- OTel suite bump to 1.15.x; Central Package Management; Stripe.net unification; Renovate config; ForwardedHeadersMiddleware.

### Out of Scope
- The global API rate limit + admin policy constant (intent 029 P08) — depends on this unit's P05 but is hardening, not dependency hygiene.
- Sentry/AWS/EF major upgrades (review flagged as Low/Medium; defer to Renovate cadence).

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-1 (P01) | Patch OpenTelemetry CVE GHSA-4625-4j76-fww9 (bump suite to 1.15.x) | Must |
| FR-2 (P02) | Unify Stripe.net + adopt Central Package Management | Must |
| FR-3 (P03) | Renovate config with grouped scheduled PRs | Should |
| FR-4 (P05) | Register ForwardedHeadersMiddleware for the /metrics allow-list | Must |

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes |
|--------|-------------|------------|
| Directory.Packages.props | Central version manifest | `<PackageVersion>` per package |
| renovate.json | Upgrade-automation config | package groups, schedule, dashboard |
| ForwardedHeadersOptions | Trusted-proxy config | KnownNetworks/KnownProxies (the proxy's own address) |

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| Restore with CPM | Pin one version per package | csproj refs | restore fails on version conflict |
| Forwarded-headers resolve | Compute real client IP | X-Forwarded-For + trusted CIDR | RemoteIpAddress = scraper IP |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 4 |
| Must Have | 3 |
| Should Have | 1 |
| Could Have | 0 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-patch-otel-cve | Patch OpenTelemetry CVE | Must | Planned |
| 002-central-package-management | Stripe.net unify + CPM | Must | Planned |
| 003-renovate-config | Renovate grouped upgrade PRs | Should | Planned |
| 004-forwarded-headers-metrics | ForwardedHeaders for /metrics allow-list | Must | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| None | — |

### Depended By
| Unit | Reason |
|------|--------|
| 029/001-access-hardening | P08 global rate limit keys on the real client IP (P05) |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| NuGet.org | Package restore | Low |
| GitHub + Renovate App | Automated upgrades | Low (one-time install) |
| Caddy reverse proxy | X-Forwarded-For source | Medium (CIDR must be correct) |

---

## Technical Context

### Suggested Technology
ASP.NET Core 8, `Microsoft.NET.Sdk` Central Package Management, OpenTelemetry 1.15.x, Renovate.

### Integration Points
| Integration | Type | Protocol |
|-------------|------|----------|
| Prometheus scraper | HTTP | GET /metrics (IP allow-listed) |
| OTel collector | OTLP | gRPC/HTTP |

---

## Constraints

- Sequential: P01 → P02 → P03 → P05.
- Stripe.net 46→47 may break; run full webhook integration suite.
- OTel `EntityFrameworkCore`/`Prometheus.AspNetCore` may stay on beta.

---

## Success Criteria

### Functional
- [ ] `dotnet list package --vulnerable` clean.
- [ ] One resolved version per package; restore fails on conflict.
- [ ] Renovate opens grouped PRs with a dependency dashboard.
- [ ] `/metrics` allow-list allows the scraper IP and denies others (integration test).

### Non-Functional
- [ ] No customer-facing behaviour change.
- [ ] No IP spoofing surface (trusted CIDR only).

### Quality
- [ ] Existing suite green (941/948 baseline).
- [ ] DEPLOYMENT.md §14 updated.

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 054-dependency-and-boot-hardening | simple | 001, 002, 003, 004 | Patch CVE, CPM, Renovate, forwarded headers |

---

## Notes

Pure ops/infra; pre-launch must-have group (P01 audit blocker, P05 day-1 ops blocker). P02 must precede P03 (Renovate needs the central manifest).
