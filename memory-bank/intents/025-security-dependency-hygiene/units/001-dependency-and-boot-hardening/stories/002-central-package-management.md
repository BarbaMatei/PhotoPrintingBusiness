---
id: 002-central-package-management
unit: 001-dependency-and-boot-hardening
intent: 025-security-dependency-hygiene
status: draft
priority: must
created: 2026-06-05T09:30:00Z
assigned_bolt: 054-dependency-and-boot-hardening
implemented: false
---

# Story: 002-central-package-management

## User Story

**As a** maintainer who wants to track every dependency in one place
**I want** Central Package Management with a single pinned Stripe.net version
**So that** the silent 46.3.0/47.0.0 split across projects can never recur

## Acceptance Criteria

- [ ] **Given** `Directory.Packages.props` at the solution root with `ManagePackageVersionsCentrally=true`, **When** restore runs, **Then** every package resolves from a single `<PackageVersion>`
- [ ] **Given** both csproj files, **When** CPM is adopted, **Then** no per-project `Version=` attributes remain
- [ ] **Given** `CentralPackageTransitivePinningEnabled=true`, **When** a transitive override would introduce a second version, **Then** `dotnet restore` fails (not warns)
- [ ] **Given** the pinned Stripe.net version, **When** `PaymentControllerIntegrationTests` runs, **Then** the full webhook/payment suite passes

## Technical Notes

- Create `Directory.Build.props` (if absent) to enable CPM solution-wide.
- Verify which Stripe.net version the Tests project actually needs (likely 47.0.0) before pinning; bump API to match.

## Dependencies

### Requires
- 001-patch-otel-cve (pin the new OTel versions in the central manifest)

### Enables
- 003-renovate-config (Renovate groups against the central manifest)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Stripe.net 46→47 breaking API | Run webhook suite; fix or rollback PR ready |
| A test package resists CPM pinning | Pin explicitly; document exception |

## Out of Scope

- Major upgrades of EF/Npgsql/Sentry/AWS (Renovate cadence).
