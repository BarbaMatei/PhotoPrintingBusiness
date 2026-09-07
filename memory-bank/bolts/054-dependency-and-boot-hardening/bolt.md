---
id: 054-dependency-and-boot-hardening
unit: 001-dependency-and-boot-hardening
intent: 025-security-dependency-hygiene
type: simple-construction-bolt
status: review-pending
stories:
  - 001-patch-otel-cve
  - 002-central-package-management
  - 003-renovate-config
  - 004-forwarded-headers-metrics
created: 2026-06-05T09:30:00Z
started: 2026-09-03T20:42:35Z
completed: null
current_stage: review
stages_completed:
  - name: plan
    completed: 2026-09-03T21:45:00Z
    artifact: implementation-plan.md
  - name: implement
    completed: 2026-09-04T12:13:50Z
    artifact: implementation-walkthrough.md
  - name: test
    completed: 2026-09-04T12:26:34Z
    artifact: test-walkthrough.md

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

- [x] **1. plan**: Complete → implementation-plan.md
- [x] **2. implement**: Complete → implementation-walkthrough.md
- [x] **3. test**: Complete → test-walkthrough.md (vulnerable-scan clean, webhook suite, metrics X-Forwarded-For)

## Dependencies

### Requires
- None (first bolt of this review cycle)

### Enables
- 063-access-hardening (P08 global rate limit keys on the real client IP from P05)

## Success Criteria

- [x] `dotnet list package --vulnerable` clean (direct and transitive, both projects)
- [x] One resolved version per package; restore fails on conflict (Stripe.net 47.0.0 everywhere; `NU1008`/`NU1603`/`NU1102` probed as errors)
- [x] Renovate dashboard + grouped PRs configured (`.github/renovate.json`; inert until the GitHub App is installed — owner action)
- [x] Real client IP resolved behind a trusted proxy, and `X-Forwarded-For` cannot open the `/metrics` scrape gate (integration test, mutation-proven)
- [x] Scoped suites green (140 + 74 + 4 cases, 0 failed); DEPLOYMENT.md §14.3 amended and §16 added

## Notes

Internal story order is strict: P01 → P02 → P03 → P05. Stripe.net 46→47 may break — keep a rollback PR ready.
