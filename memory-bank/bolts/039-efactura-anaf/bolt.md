---
id: 039-efactura-anaf
unit: 002-efactura-generation-and-anaf
intent: 016-romanian-vat-efactura
type: ddd-construction-bolt
status: complete
stories:
  - 001-ubl-xml-builder
  - 002-anaf-spv-client
  - 003-invoice-pdf-renderer-and-endpoint
  - 004-admin-invoice-list-and-retry
created: 2026-05-25T10:15:00.000Z
started: 2026-06-03T11:00:00.000Z
completed: "2026-06-03T11:27:24Z"
current_stage: null
stages_completed:
  - name: domain-model
    completed: 2026-06-03T11:30:00.000Z
    artifact: ddd-01-domain-model.md
  - name: technical-design
    completed: 2026-06-03T12:00:00.000Z
    artifact: ddd-02-technical-design.md
  - name: adr-analysis
    completed: 2026-06-03T12:30:00.000Z
    artifacts:
      - adr-021-pdf-library-questpdf-not-puppeteersharp.md
      - adr-022-dual-write-rollout-via-feature-flag.md
      - adr-023-worker-dispatch-db-polling-not-in-process-channel.md
      - adr-024-implicit-attempt-count-from-updatedat-no-persisted-counter.md
  - name: implement
    completed: 2026-06-03T13:30:00.000Z
    artifact: src code (no migration — bolt 038 staged the schema)
requires_bolts:
  - 038-vat-calculation
enables_bolts: []
requires_units: []
blocks: false
complexity:
  avg_complexity: 4
  avg_uncertainty: 4
  max_dependencies: 1
  testing_scope: 4
---

# Bolt: 039-efactura-anaf

## Overview

XML build, ANAF SPV transport, PDF rendering, and admin tools.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Domain Model | Invoice lifecycle states; ANAF status transitions |
| 2 | Technical Design | XML builder shape, OAuth + cert handling, PDF pipeline choice (QuestPDF vs PuppeteerSharp) |
| 3 | Implement | Builder, client, renderer, jobs, endpoints |
| 4 | Test | XSD validation tests, fixture-based ANAF tests, render snapshot tests |

## Dependencies

- **Requires**: 038-vat-calculation.
- **Enables**: intent 022 coupons can reference final invoice path.

## Key Technical Notes

- ANAF certificate path: env var only; never in repo.
- Production rollout: dual-write mode for one week (generate but don't email yet) to inspect outputs before customers see them.
