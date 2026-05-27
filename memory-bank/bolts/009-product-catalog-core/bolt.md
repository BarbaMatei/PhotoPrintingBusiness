---
id: 009-product-catalog-core
unit: 001-product-catalog-core
intent: 003-product-catalog
type: ddd-construction-bolt
status: complete
started: 2026-05-21T09:15:00Z
completed: 2026-05-21T10:00:00Z
current_stage: complete
stages_completed: [domain-model, technical-design, implement, test]
stories:
  - 001-product-entity-schema
  - 002-quantity-tiered-pricing
  - 003-public-catalog-endpoint
  - 004-product-detail-endpoint
  - 005-price-calculation-endpoint
created: 2026-05-20T20:35:00Z

requires_bolts: []
enables_bolts: [010-product-catalog-admin, 011-product-catalog-ui]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 2
---

## Bolt: 009-product-catalog-core

**Intent**: 003-product-catalog
**Unit**: 001-product-catalog-core
**Type**: DDD Construction (5 stages)

### Stories Included

1. **001-product-entity-schema** — Product, ProductSize, ProductFinish, PricingTier EF Core schema + migration
2. **002-quantity-tiered-pricing** — Pricing tier storage, lookup logic, validation rules
3. **003-public-catalog-endpoint** — `GET /api/products` (public, cached)
4. **004-product-detail-endpoint** — `GET /api/products/{id}` (public)
5. **005-price-calculation-endpoint** — `POST /api/products/{id}/calculate-price`

### Dependency Analysis

- Requires: None — this is the foundational bolt for this intent
- Enables: **010-product-catalog-admin** (admin API needs domain model), **011-product-catalog-ui** (Angular needs public API)

### Stage Sequence

1. **Domain Model** → Entity graph, aggregates, invariants, DB schema design
2. **Technical Design** → API contract, service interfaces, EF Core config, DTO shapes
3. **ADR Analysis** → Evaluate: pricing tiers as rows vs JSON, caching strategy
4. **Implement** → Entities, migrations, services, controllers, validators
5. **Test** → Integration tests via WebApplicationFactory, unit tests for tier calculation
