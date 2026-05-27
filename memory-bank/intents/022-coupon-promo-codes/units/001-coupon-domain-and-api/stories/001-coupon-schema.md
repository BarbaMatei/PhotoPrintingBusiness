---
id: 001-coupon-schema
unit: 001-coupon-domain-and-api
intent: 022-coupon-promo-codes
status: draft
priority: must
created: 2026-05-25T10:45:00Z
assigned_bolt: 047-coupon-domain-and-api
implemented: false
---

# Story: 001-coupon-schema

## User Story

**As** the application
**I want** durable storage for coupons, their redemptions, and per-order discount totals
**So that** offers can be defined once and accurately reflected on every order

## Acceptance Criteria

- [ ] EF migration creates `Coupons` table:
  ```sql
  CREATE TABLE "Coupons" (
      "Id"              uuid PRIMARY KEY,
      "Code"            varchar(50) NOT NULL UNIQUE,
      "Type"            varchar(20) NOT NULL,
      "Value"           numeric(10,2) NOT NULL,
      "MinSubtotalRon"  numeric(10,2) NOT NULL DEFAULT 0,
      "ValidFrom"       timestamptz NOT NULL,
      "ValidUntil"      timestamptz NOT NULL,
      "MaxRedemptions"  int NULL,
      "RedemptionsCount"int NOT NULL DEFAULT 0,
      "IsActive"        boolean NOT NULL DEFAULT true,
      "RowVersion"      bytea NOT NULL DEFAULT '\x00',
      "CreatedAt"       timestamptz NOT NULL,
      "UpdatedAt"       timestamptz NULL
  );
  ```
- [ ] EF migration creates `CouponRedemptions` table:
  ```sql
  CREATE TABLE "CouponRedemptions" (
      "Id"          uuid PRIMARY KEY,
      "CouponId"    uuid NOT NULL REFERENCES "Coupons"("Id"),
      "OrderId"     uuid NOT NULL REFERENCES "Orders"("Id"),
      "UserId"      uuid NULL REFERENCES "Users"("Id"),
      "DiscountRon" numeric(10,2) NOT NULL,
      "RedeemedAt"  timestamptz NOT NULL
  );
  ```
- [ ] `Orders` migration:
  ```sql
  ALTER TABLE "Orders"
      ADD COLUMN "CouponCode"  varchar(50) NULL,
      ADD COLUMN "DiscountRon" numeric(10,2) NOT NULL DEFAULT 0;
  ```
- [ ] `Coupon.Code` stored uppercase; `Coupon.Type` enum: `Percent | Fixed | FreeShipping`.

## Technical Notes

- `RowVersion` is the EF Core concurrency token; let EF map it to Postgres `xmin` system column via `IsRowVersion()` if preferred (less churn than `bytea` updates).
- Index `CouponRedemptions(CouponId)` and `CouponRedemptions(UserId, CouponId)` for future per-user limits.

## Dependencies

### Requires
- intent 016 (VAT) — discount must subtract from pre-VAT subtotal

### Enables
- 002-cart-coupon-endpoints, 003-redemption-on-order-create

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Existing rows in production | New columns default to zero / null; safe |
| Concurrent migrations | Standard EF lock |

## Out of Scope

- Per-user `MaxRedemptions` (separate column in a future intent).
