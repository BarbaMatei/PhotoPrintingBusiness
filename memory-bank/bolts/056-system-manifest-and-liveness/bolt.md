---
id: 056-system-manifest-and-liveness
unit: 002-system-manifest-and-liveness
intent: 026-observability-boot-manifest
type: simple-construction-bolt
status: planned
stories:
  - 001-system-info-endpoint
  - 002-background-job-liveness-check
  - 003-anaf-invoice-metrics-and-slo
created: 2026-06-05T09:30:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [055-boot-composition-and-flags]
enables_bolts: [058-observability-boot-manifest-ui]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 2
---

# Bolt: 056-system-manifest-and-liveness

## Overview

Expose the system-info manifest (P04 backend) and add background-job liveness + ANAF invoice metrics/SLO (P17) — closing the hidden-functionality and silent-failure gaps.

## Objective

Make the system inspectable and detect dead jobs / ANAF lag against the legal SLA.

## Stories Included

- **001-system-info-endpoint**: /api/admin/system-info manifest (Should)
- **002-background-job-liveness-check**: Heartbeat + liveness check (Must)
- **003-anaf-invoice-metrics-and-slo**: invoice_upload metrics + SLO (Must)

## Bolt Type

**Type**: simple-construction-bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [ ] **1. plan**: Pending → implementation-plan.md
- [ ] **2. implement**: Pending → AdminSystemInfoController, SystemInfo/, IHeartbeat, liveness check, FotoMetrics, slos.md
- [ ] **3. test**: Pending → test-report (flag/job regression test; degraded-on-stale; metrics present)

## Dependencies

### Requires
- 055-boot-composition-and-flags (IFeatureGate)

### Enables
- 058-observability-boot-manifest-ui

## Success Criteria

- [ ] Manifest admin-only, cached, no secrets; flag/job regression test green
- [ ] Liveness degrades on stale heartbeat
- [ ] invoice_upload metrics + SLO present

## Notes

P17 is pre-launch must-have; P04 strong nice-to-have.
