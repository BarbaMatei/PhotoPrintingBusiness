---
id: 036-sameday-api-client
unit: 001-sameday-api-client
intent: 015-sameday-shipping-integration
type: ddd-construction-bolt
status: complete
stories:
  - 001-sameday-settings-and-typed-client
  - 002-token-auth-and-refresh
  - 003-sameday-schema-additions
created: 2026-05-25T10:10:00.000Z
started: 2026-06-02T09:00:00.000Z
completed: "2026-06-02T14:29:35Z"
current_stage: null
stages_completed:
  - name: domain-model
    completed: 2026-06-02T09:30:00.000Z
    artifact: ddd-01-domain-model.md
  - name: technical-design
    completed: 2026-06-02T10:00:00.000Z
    artifact: ddd-02-technical-design.md
  - name: adr-analysis
    completed: 2026-06-02T10:20:00.000Z
    artifact: adr-013-in-process-sameday-token-cache.md, adr-014-401-retry-in-auth-handler-not-polly.md
  - name: implement
    completed: 2026-06-02T14:40:00.000Z
    artifact: implementation-walkthrough.md
  - name: test
    completed: 2026-06-02T15:30:00.000Z
    artifact: ddd-03-test-report.md
requires_bolts:
  - 015-shipping-and-order-core
enables_bolts:
  - 037-awb-and-tracking-jobs
requires_units: []
blocks: false
complexity:
  avg_complexity: 3
  avg_uncertainty: 3
  max_dependencies: 2
  testing_scope: 3
---

# Bolt: 036-sameday-api-client

## Overview

Stand up the Sameday HTTP client, settings, auth flow, and schema additions. Records fixtures during development so the AWB and tracking bolt that follows has a stable test surface.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Domain Model | `ddd-01-domain-model.md` — `SamedayToken`, `IShippingService.Sameday` mapping, error taxonomy |
| 2 | Technical Design | `ddd-02-technical-design.md` — Polly policies, HTTP client wiring, retry semantics |
| 3 | Implement | Code + recorded HTTP fixtures |
| 4 | Test | `ddd-03-test-report.md` — fixture-based integration tests, schema migration test |

## Dependencies

- **Requires**: 015-shipping-and-order-core.
- **Enables**: 037-awb-and-tracking-jobs.

## Key Technical Notes

- Sandbox credentials should live in `dotnet user-secrets` for dev, env vars for staging/prod.
- Production rollout: behind `Sameday:Enabled` flag; static service remains the fallback.
