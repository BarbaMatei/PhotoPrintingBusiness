---
intent: 027-architectural-layering
phase: inception
status: units-decomposed
updated: 2026-06-05T09:30:00Z
---

# Architectural Layering - Unit Decomposition

## Units Overview

Decomposes into **3 backend units**, executed in sequence. All use `simple-construction-bolt` (pure refactor — no new domain modelling). Folded first-pass proposals (P06, P11, P16) live inside these units as stories.

### Unit 1: 001-layering-foundation
**Description**: Write the no-split ADR (P22), then establish Domain/ (folds P16), Infrastructure/, Web/, and Application/ layers (P21, folds P06).
**Stories**: 001-no-split-adr, 002-domain-layer-extraction, 003-infrastructure-layer, 004-web-layer, 005-application-feature-promotion
**Deliverables**: ADR; `Domain/`, `Infrastructure/`, `Web/`, `Application/<Feature>/` folder+namespace structure; layer-rule doc.
**Dependencies**: Depends on None · Depended by Unit 2, Unit 3
**Estimated Complexity**: XL

### Unit 2: 002-conventions-and-policy
**Description**: Introduce `Abstractions/` subfolders (P23) and the no-repository policy + IQueryable analyzer (P24).
**Stories**: 001-abstractions-subfolders, 002-no-repository-policy-and-analyzer
**Deliverables**: `Abstractions/` per feature; `data-access-conventions.md`; banned-API analyzer config.
**Dependencies**: Depends on Unit 1 · Depended by Unit 3
**Estimated Complexity**: M

### Unit 3: 003-handler-pattern
**Description**: Handler-per-use-case (P25) — abstractions + four target handlers, folding in OrderPaidEventDispatcher (P11).
**Stories**: 001-command-handler-abstractions, 002-create-order-handler, 003-order-paid-event-dispatcher, 004-retry-and-promote-handlers
**Deliverables**: `Application/Shared/Abstractions/` interfaces; CreateOrderHandler, OrderPaidEventDispatcher, RetryInvoiceUploadHandler, PromoteOrderPhotosHandler.
**Dependencies**: Depends on Unit 1 (layout), Unit 2 (Abstractions convention) · Depended by None
**Estimated Complexity**: L

## Requirement-to-Unit Mapping

- **FR-1 (P22)** → `001-layering-foundation`
- **FR-2 (P21)** → `001-layering-foundation` (PRs 2–5)
- **FR-3 (P06, folded)** → `001-layering-foundation` (005-application-feature-promotion)
- **FR-4 (P16, folded)** → `001-layering-foundation` (002-domain-layer-extraction)
- **FR-5 (P23)** → `002-conventions-and-policy`
- **FR-6 (P24)** → `002-conventions-and-policy`
- **FR-7 (P25)** → `003-handler-pattern`
- **FR-8 (P11, folded)** → `003-handler-pattern` (003-order-paid-event-dispatcher)

## Unit Dependency Graph

```text
[001-layering-foundation] ──> [002-conventions-and-policy] ──> [003-handler-pattern]
```

## Execution Order

1. Unit 1 (ADR + four layering PRs) — sequenced internally PR1→PR5
2. Unit 2 (Abstractions + no-repo policy)
3. Unit 3 (handlers)

**All in lockstep with intent 028 (tests).**
