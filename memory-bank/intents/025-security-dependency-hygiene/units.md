---
intent: 025-security-dependency-hygiene
phase: inception
status: units-decomposed
updated: 2026-06-05T09:30:00Z
---

# Security & Dependency Hygiene - Unit Decomposition

## Units Overview

This intent decomposes into **1 unit**. All four proposals touch the same two surfaces (the dependency manifests and the boot pipeline) and must ship in sequence, so a single cohesive ops/infra unit is correct — splitting would create artificial cross-unit ordering.

### Unit 1: 001-dependency-and-boot-hardening

**Description**: Patch the OTel CVE, adopt Central Package Management + unify Stripe.net, add Renovate, and register ForwardedHeadersMiddleware for the `/metrics` allow-list.

**Stories**:
- 001-patch-otel-cve (P01)
- 002-central-package-management (P02)
- 003-renovate-config (P03)
- 004-forwarded-headers-metrics (P05)

**Deliverables**:
- Updated `PhotoPrint.API.csproj` / `PhotoPrint.Tests.csproj`, new `Directory.Packages.props` + `Directory.Build.props`, new `.github/renovate.json`, `Program.cs` forwarded-headers registration, `MetricsEndpointIntegrationTests` X-Forwarded-For case, DEPLOYMENT.md §14 note.

**Dependencies**:
- Depends on: None
- Depended by: 029 (P08 global rate limit keys on the real client IP enabled here by P05)

**Estimated Complexity**: M

## Requirement-to-Unit Mapping

- **FR-1 (P01)** → `001-dependency-and-boot-hardening`
- **FR-2 (P02)** → `001-dependency-and-boot-hardening`
- **FR-3 (P03)** → `001-dependency-and-boot-hardening`
- **FR-4 (P05)** → `001-dependency-and-boot-hardening`

## Unit Dependency Graph

```text
(none) ──> [001-dependency-and-boot-hardening] ──> enables 029 (P08)
```

## Execution Order

1. Single unit; internal story order P01 → P02 → P03 → P05.
