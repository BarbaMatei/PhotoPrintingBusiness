---
intent: 031-refund-return-flow
phase: inception
status: inception-complete
created: 2026-06-05T09:00:00Z
updated: 2026-06-05T09:00:00Z
source: docs/analysis/architect-review-2026-06-03.md (Group 7 — P09; P20 coupon excluded → covered by intent 022)
priority_score: 19
---

# Requirements: Refund / Return Flow

## Intent Overview

EU Directive 2011/83/EU grants consumers a 14-day right of withdrawal with refund. Today there is **no refund endpoint**, no `Order.RefundedAt`/`RefundAmountRon`/`RefundReason`, no `OrderStatus.Refunded`, and no credit-note in the `Invoices` table (Romanian fiscal law requires a credit-note for a refund — accountancy will flag its absence). The only current path is an admin manually refunding in the Stripe Dashboard, which leaves the FotoTipar DB and ANAF out of sync (order shows `Delivered` while Stripe shows refunded; ANAF never sees the credit-note). This intent wires the full server-side refund flow — order/invoice schema, the `OrderStatus.Refunded` terminal state, an admin-initiated refund endpoint, Stripe + EuPlatesc refund execution, and an ANAF credit-note (UBL `InvoiceTypeCode` 381) that the existing `InvoiceUploadJob` picks up. It is a **launch blocker for legal compliance if the launch market includes EU consumers** (otherwise post-launch, accepted in writing). Best landed after intent 027 so it sits in the layered shape; it intersects two regulated paths (ANAF invoicing, bolt 039) and photo-archive retention (bolt 052) — plan dedicated review.

> **Note — coupon engine (P09's Group-7 sibling P20):** the discount/coupon engine is **already an intent** ([[022-coupon-promo-codes]]) created from the 2026-05-25 review. It is intentionally NOT recreated here. The 2026-06-03 review's P20 proposes a slightly different shape (`Order.CouponId` FK + UBL `AllowanceCharge`) than `022` (`Order.CouponCode` + `DiscountRon`); reconciling those two schemas is logged as an Open Question against intent 022, not a new intent.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Legal: honour the 14-day right of withdrawal | Admin can fully/partially refund a paid order in-app | Must |
| Fiscal correctness | A credit-note invoice (type 381) is generated and submitted to ANAF | Must |
| System-of-record consistency | DB order status, payment gateway, and ANAF agree after a refund | Must |
| Auditability | Refund reason + amount + timestamp persisted on the order | Should |

---

## Functional Requirements

### FR-1 (P09): Order + Invoice refund schema and state machine
- **Description**: Add `Orders.RefundedAt`, `Orders.RefundAmountRon`, `Orders.RefundReason`; add `OrderStatus.Refunded` (terminal) and the transition in `OrderStatusMachine`. Add an explicit `Invoice.InvoiceType` enum (`Final` | `CreditNote`) and `Invoice.OriginalInvoiceId` FK (credit-note references the original, negative amounts), with an index on `OriginalInvoiceId`.
- **Acceptance Criteria**:
  - Migration adds the three `Orders` columns + two `Invoices` columns + partial index; `Add-Migration` diff reviewed.
  - `OrderStatusMachine` permits `Paid`/`Delivered` → `Refunded` and rejects illegal transitions; covered by unit tests.
  - Existing invoices default to `InvoiceType = 'Final'`.
- **Priority**: Must
- **Related Stories**: TBD

### FR-2 (P09): Refund service — full and partial refunds across gateways
- **Description**: Add `Services/Refunds/IRefundService.RefundOrderAsync(orderId, amount?, reason, ct)`. Stripe path calls refund-create against the `PaymentIntent`; EuPlatesc path uses its documented refund endpoint (or a flagged manual Z-report path in admin if unavailable). Partial refunds spread proportionally across line items (documented choice).
- **Acceptance Criteria**:
  - Full refund sets `Order.Status = Refunded`, stamps `RefundedAt`/`RefundAmountRon`/`RefundReason`, and refunds the gateway.
  - Partial refund records the partial amount; status policy for partial defined (Open Question Q2).
  - Idempotent: a duplicate refund request does not double-refund the gateway.
  - Refund is transactional with the credit-note creation (FR-3).
- **Priority**: Must
- **Related Stories**: TBD

### FR-3 (P09): ANAF credit-note generation and submission
- **Description**: On refund, generate a credit-note UBL invoice (`cbc:InvoiceTypeCode` 381) referencing the original via `OriginalInvoiceId`, with negative amounts. The existing `InvoiceUploadJob` (filters `Pending`+`Submitted` regardless of type) submits it to ANAF SPV.
- **Acceptance Criteria**:
  - Credit-note row created with negative totals and correct VAT reversal (reuses bolt 038 `VatCalculator` semantics).
  - `InvoiceUploadJob` picks up and submits the credit-note without type-specific changes; status lifecycle observable via intent 026 P17 metrics.
  - UBL validates against the e-Factura schema for type 381.
- **Priority**: Must
- **Related Stories**: TBD

### FR-4 (P09): Admin refund endpoint
- **Description**: `POST /api/admin/orders/{id}/refund { amount?, reason }` behind the admin policy. No customer-facing endpoint — refunds are admin-initiated.
- **Acceptance Criteria**:
  - Admin-only (uses `Policies.Admin`, intent 029 P08); validates amount ≤ refundable balance; returns the updated order + refund result.
  - Invalid states (already refunded, unpaid) → appropriate 409/422 with a `code:`.
  - Admin UI surfaces the action on the order detail view.
- **Priority**: Must
- **Related Stories**: TBD

---

## Non-Functional Requirements

### Compliance
| Requirement | Standard | Notes |
|-------------|----------|-------|
| Right of withdrawal | EU Directive 2011/83/EU | 14-day cooling-off; refund mechanism mandatory for EU launch |
| Credit-note | Romanian Fiscal Code / e-Factura | UBL `InvoiceTypeCode` 381, submitted to ANAF SPV |
| VAT reversal | Bolt 038 VAT rules / ADR-019 rounding | Credit-note reverses VAT on the refunded portion |

### Reliability
| Requirement | Metric | Target |
|-------------|--------|--------|
| No double-refund | Idempotent refund | Duplicate request is a no-op at the gateway |
| Cross-system consistency | DB / gateway / ANAF | Eventually consistent; reconcilable |

### Security
| Requirement | Standard | Notes |
|-------------|----------|-------|
| Refund authorization | Admin policy | `Policies.Admin` only |

---

## Constraints

### Technical Constraints
- Best landed **after intent 027** so new code sits in the layered shape (`Application/Refunds/`, `Infrastructure/Payments/`).
- Intersects **bolt 039 (ANAF)** and **bolt 052 (photo-archive retention)**: a refunded order must NOT auto-purge originals on the Shipped trigger, and SHOULD push the credit-note to ANAF. Both touch live regulated paths — dedicated review required.
- Reuses the bolt 038 `VatCalculator` (gold-standard) for VAT reversal — do not re-implement.
- EuPlatesc refund may lack a programmatic endpoint; a manual admin Z-report fallback is acceptable for v1 (flagged).

### Business Constraints
- **Pre-launch must-have IF the launch market includes EU consumers**; otherwise post-launch with written acceptance of the gap.
- Largest single feature in the review (7–10 dev-days).

---

## Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| `InvoiceUploadJob` submits credit-notes without type-specific changes | Credit-notes never reach ANAF | Verify the job's filter covers all `Pending`/`Submitted` regardless of `InvoiceType` |
| Proportional line-item split is acceptable for partial refunds | Accounting disputes the allocation | Document the rule; revisit if accountancy objects |
| EuPlatesc supports programmatic refunds | Manual-only refunds for that gateway | Ship manual Z-report fallback in admin; flag for v2 |
| Refund interacts safely with archive retention (bolt 052) | Originals purged before refund processed | Suppress auto-purge for orders eligible for / under refund |

---

## Open Questions

| Question | Owner | Due Date | Resolution |
|----------|-------|----------|------------|
| Q1: Is the launch market EU (making this a hard pre-launch blocker)? | Maintainer/Legal | 2026-06-19 | Pending — drives Must vs deferred |
| Q2: What status does a *partial* refund leave the order in (stays Paid/Delivered vs new PartiallyRefunded)? | Product | 2026-07-10 | Recommend keep current status + record partial amount; no new enum in v1 |
| Q3: Does EuPlatesc expose a refund API, or is manual Z-report the v1 path? | Dev | 2026-07-10 | Pending integration spike |
| Q4: How does refund interact with bolt 052 archive purge timing? | Dev | 2026-07-10 | Suppress purge for refund-eligible orders; document |
