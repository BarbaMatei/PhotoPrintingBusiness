---
id: 010-product-catalog-admin
unit: 001-product-catalog-core
intent: 003-product-catalog
type: ddd-construction-bolt
status: complete
started: 2026-05-21T10:05:00Z
completed: 2026-05-21T11:00:00Z
current_stage: complete
stages_completed: [domain-model, technical-design, implement, test]
stories:
  - 006-admin-product-management
  - 007-admin-pricing-management
created: 2026-05-20T20:35:00Z

requires_bolts: [009-product-catalog-core]
enables_bolts: [011-product-catalog-ui]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 2
---

## Bolt: 010-product-catalog-admin

**Intent**: 003-product-catalog
**Unit**: 001-product-catalog-core
**Type**: DDD Construction (5 stages)

### Stories Included

1. **006-admin-product-management** — `POST/PUT/PATCH/DELETE /api/admin/products/**`
2. **007-admin-pricing-management** — `PUT /api/admin/products/{id}/sizes/{sizeId}/pricing`

### Dependency Analysis

- Requires: **009-product-catalog-core** (needs Product/ProductSize/PricingTier entities and `[AdminOnly]` auth pattern)
- Enables: **011-product-catalog-ui** (Angular admin UI consumes these endpoints)

### Stage Sequence

1. **Domain Model** → Admin command model, validation rules (tier ordering, gap detection)
2. **Technical Design** → Request/response DTOs, atomic pricing replace strategy, auth integration
3. **ADR Analysis** → Soft-delete vs hard-delete for products
4. **Implement** → Admin controller, command handlers, FluentValidation rules
5. **Test** → Integration tests: 201/200/204 success cases, 401/403 auth, 422 validation
