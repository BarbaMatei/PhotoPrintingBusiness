---
id: 054-dependency-and-boot-hardening
unit: 001-dependency-and-boot-hardening
intent: 025-security-dependency-hygiene
type: simple-construction-bolt
status: planned
stories:
  - 001-patch-otel-cve
  - 002-central-package-management
  - 003-renovate-config
  - 004-forwarded-headers-metrics
created: 2026-06-05T09:30:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: []
enables_bolts: [063-access-hardening]
requires_units: []
blocks: false

complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 3
  testing_scope: 2
---

# Bolt: 054-dependency-and-boot-hardening

## Overview

Pre-launch dependency + boot hardening: patch the OTel CVE, adopt Central Package Management (unifying Stripe.net), add Renovate, and register ForwardedHeadersMiddleware so the `/metrics` allow-list is correct behind Caddy.

## Objective

Eliminate the known CVE and the silent multi-version Stripe.net load, automate future upgrades, and fix the day-1 `/metrics` allow-list defect — all without customer-facing behaviour change.

## Stories Included

- **001-patch-otel-cve**: Bump OTel suite to 1.15.x (Must)
- **002-central-package-management**: CPM + Stripe.net unification (Must)
- **003-renovate-config**: Grouped scheduled upgrade PRs (Should)
- **004-forwarded-headers-metrics**: ForwardedHeaders for the allow-list (Must)

## Bolt Type

**Type**: simple-construction-bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Pending → implementation-plan.md
- [ ] **2. implement**: Pending → csproj/props/renovate.json/Program.cs changes
- [ ] **3. test**: Pending → test-report (vulnerable-scan clean, webhook suite, metrics X-Forwarded-For)

## Dependencies

### Requires
- None (first bolt of this review cycle)

### Enables
- 063-access-hardening (P08 global rate limit keys on the real client IP from P05)

## Success Criteria

- [ ] `dotnet list package --vulnerable` clean
- [ ] One resolved version per package; restore fails on conflict
- [ ] Renovate dashboard + grouped PRs configured
- [ ] `/metrics` allow-list correct behind proxy (integration test)
- [ ] Existing suite green; DEPLOYMENT.md §14 updated

## Notes

Internal story order is strict: P01 → P02 → P03 → P05. Stripe.net 46→47 may break — keep a rollback PR ready.
