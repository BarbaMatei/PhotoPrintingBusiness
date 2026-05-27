---
id: 036-sameday-api-client
unit: 001-sameday-api-client
intent: 015-sameday-shipping-integration
type: ddd-construction-bolt
status: planned
stories:
  - 001-sameday-settings-and-typed-client
  - 002-token-auth-and-refresh
  - 003-sameday-schema-additions
created: 2026-05-25T10:10:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [015-shipping-and-order-core]
enables_bolts: [037-awb-and-tracking-jobs]
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
