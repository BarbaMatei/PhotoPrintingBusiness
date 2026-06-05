---
intent: 031-refund-return-flow
phase: inception
status: units-decomposed
updated: 2026-06-05T09:30:00Z
---

# Refund / Return Flow - Unit Decomposition

## Units Overview

Decomposes into **2 units**: a backend domain+API unit (genuine new domain → `ddd-construction-bolt`) and the admin UI (`{intent}-ui`, `simple-construction-bolt`).

### Unit 1: 001-refund-domain-and-api
**Description**: Refund schema + `OrderStatus.Refunded` + state machine (FR1), the refund service across gateways (FR2), the ANAF credit-note (FR3), and the admin refund endpoint (FR4 backend).
**Stories**: 001-refund-schema-and-status, 002-refund-service-stripe-euplatesc, 003-anaf-credit-note, 004-admin-refund-endpoint
**Deliverables**: migration; `OrderStatusMachine` update; `Services/Refunds/`; credit-note UBL generation; `AdminRefundsController`.
**Dependencies**: Depends on 027 (placement), 029 (`Policies.Admin`); intersects bolt 039 + 052 · Depended by Unit 2
**Estimated Complexity**: XL

### Unit 2: 002-refund-return-flow-ui
**Description**: Admin refund action on the order-detail view (FR4 UI).
**Unit Type**: frontend
**Stories**: 001-admin-refund-action
**Deliverables**: refund action + modal in the admin order-detail Angular page.
**Dependencies**: Depends on Unit 1 · Depended by None
**Estimated Complexity**: S

## Requirement-to-Unit Mapping

- **FR-1 (P09 schema/state)** → `001-refund-domain-and-api`
- **FR-2 (P09 refund service)** → `001-refund-domain-and-api`
- **FR-3 (P09 ANAF credit-note)** → `001-refund-domain-and-api`
- **FR-4 (P09 admin endpoint)** → `001-refund-domain-and-api` (backend) + `002-refund-return-flow-ui` (UI)

## Unit Dependency Graph

```text
[001-refund-domain-and-api] ──> [002-refund-return-flow-ui]
```

## Execution Order

1. Unit 1 (domain + API), internal order: schema → service → credit-note → endpoint
2. Unit 2 (admin UI) — after the endpoint exists
