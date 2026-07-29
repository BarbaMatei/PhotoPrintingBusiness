---
intent: 022-coupon-promo-codes
phase: inception
status: inception-complete
created: 2026-05-25T10:45:00Z
updated: 2026-06-05T10:30:00Z
source: docs/architecture-analysis-2026-05-25.md#10
priority_score: 15
---

# Requirements: Coupon / Promo Codes

## Intent Overview

No discount mechanism exists. Marketing-led promotions (first-order discount, "FREESHIP", seasonal codes) are effectively table-stakes for a Romanian e-commerce site. This intent introduces `Coupon` and `CouponRedemption` entities, server-side validation that runs **before** VAT calculation, an admin CRUD UI, and the customer-facing input on the cart page.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Enable marketing promotions | Marketing can issue + share codes without code change | Must |
| Preserve invoice correctness | Discount applies to pre-VAT subtotal; VAT computed on the discounted net | Must |
| Prevent abuse | `MaxRedemptions` and per-user limits enforced atomically | Must |
| Insight into campaign performance | Admin sees redemptions per coupon | Should |

---

## Functional Requirements

### FR-1: Coupon and CouponRedemption schema
- **Description**: Add `Coupons` and `CouponRedemptions` tables; add `Orders.CouponCode`, `Orders.DiscountRon`.
- **Acceptance Criteria**:
  - `Coupons`: Id, Code (unique), Type (Percent|Fixed|FreeShipping), Value, MinSubtotalRon, ValidFrom, ValidUntil, MaxRedemptions (nullable), RedemptionsCount (default 0), IsActive, RowVersion.
  - `CouponRedemptions`: Id, CouponId, OrderId, UserId (nullable), DiscountRon, RedeemedAt.
  - `Orders` gets `CouponCode varchar(50) NULL`, `DiscountRon numeric(10,2) NOT NULL DEFAULT 0`.
- **Priority**: Must
- **Related Stories**: US-022-1

### FR-2: Cart coupon endpoints
- **Description**: `POST /api/cart/coupon { code }` validates and previews the discount; `DELETE /api/cart/coupon` clears it. State stored on the cart row (server-side).
- **Acceptance Criteria**:
  - Apply: returns updated cart with `DiscountRon`, `NetTotalRon` preview.
  - Invalid / expired / inactive code → 422 with `code: "INVALID_COUPON"`.
  - Min-subtotal not met → 422 with `code: "MIN_SUBTOTAL_NOT_MET"`.
  - Duplicate apply replaces silently.
- **Priority**: Must
- **Related Stories**: US-022-2

### FR-3: Order creation applies coupon transactionally
- **Description**: `OrderService.CreateFromCartAsync` re-validates the coupon, computes discount, then VAT on the discounted subtotal, then creates `CouponRedemption` in the same transaction. Uses `RowVersion` optimistic concurrency to prevent over-redemption races.
- **Acceptance Criteria**:
  - Coupon `MaxRedemptions = 100` with 99 used; 10 concurrent orders → exactly 1 succeeds, others receive 409 with `code: "COUPON_EXHAUSTED"`.
  - VAT computed on `subtotal - discount` (post-discount).
  - Order persists `CouponCode` + `DiscountRon`.
- **Priority**: Must
- **Related Stories**: US-022-3

### FR-4: Admin coupon CRUD
- **Description**: `GET/POST/PUT/DELETE /api/admin/coupons` with paging and redemption stats endpoint.
- **Acceptance Criteria**:
  - All endpoints behind `Admin` role.
  - DELETE is soft (`IsActive = false`); never hard-delete.
  - `GET /api/admin/coupons/{id}/redemptions` lists who used the code.
- **Priority**: Should
- **Related Stories**: US-022-4

### FR-5: Frontend cart-page integration
- **Description**: Input + Apply button on the cart page. Discount line surfaces on cart, review, and confirmation pages. Discount + VAT shown on the generated PDF invoice.
- **Acceptance Criteria**:
  - Apply / clear UX matches existing cart interactions.
  - Error mapping presents Romanian copy for each `code:` value.
  - PDF invoice line `Reducere: -X.XX RON` rendered when present.
- **Priority**: Should
- **Related Stories**: US-022-5

---

## Non-Functional Requirements

### Compliance
| Requirement | Standard | Notes |
|-------------|----------|-------|
| Invoice line | Romanian Fiscal Code | Discount appears as separate line on invoice |
| VAT order of operations | Discount → VAT | Critical: VAT is computed on the post-discount net |

### Performance
| Requirement | Metric | Target |
|-------------|--------|--------|
| Apply coupon | p95 | < 100 ms |

---

## Constraints

### Technical Constraints
- Must depend on intent 016 (VAT) to ensure correct math.
- Must use `RowVersion` optimistic concurrency for atomic `MaxRedemptions`.

### Business Constraints
- Lower priority on roadmap; ship after VAT + ANAF in production.

---

## Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| Per-user limits sufficient as `MaxRedemptions` per code; no per-user-per-code cap initially | Abuse via multiple accounts | Add `MaxPerUser` column in follow-up |
| `FreeShipping` type can be implemented by setting `Order.ShippingCostRon = 0` after server-side resolution | Conflicts with future shipping per zone (intent 015) | Document interaction in `decision-index.md` |

---

## Open Questions

| Question | Owner | Due Date | Resolution |
|----------|-------|----------|------------|
| Q1: Combineable coupons or one-at-a-time? | Product | 2026-08-15 | Recommend one-at-a-time; multi-coupon is a separate intent |
| Q2: Coupon code format (case-sensitive, length)? | Product | 2026-08-15 | Recommend `[A-Z0-9]{4,20}`, stored uppercase |
| Q3: Reconcile schema with architect-review 2026-06-03 P20? | Dev/Architect | 2026-08-15 | This intent uses `Order.CouponCode`+`DiscountRon`; P20 proposes `Order.CouponId` FK + UBL `AllowanceCharge` (cbc:ChargeIndicator=false). Recommend keeping the simpler shipped shape; evaluate the UBL AllowanceCharge representation for ANAF correctness during construction. (Raised by 2026-06-03 review; see intent 031 log.) |
