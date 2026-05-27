---
unit: 001-coupon-domain-and-api
intent: 022-coupon-promo-codes
phase: inception
status: draft
created: 2026-05-25T10:45:00Z
updated: 2026-05-25T10:45:00Z
---

# Unit Brief: Coupon Domain & API

## Purpose

Schema, validation, redemption logic, and admin CRUD — everything needed to make a coupon system work server-side.

## Scope

### In Scope
- `Coupon` + `CouponRedemption` entities + migrations
- `Orders.CouponCode` + `Orders.DiscountRon` columns
- `ICouponService` — validate, apply, redeem
- Customer endpoints: `POST/DELETE /api/cart/coupon`
- `OrderService.CreateFromCartAsync` integration (atomic redemption)
- Admin endpoints: `GET/POST/PUT/DELETE /api/admin/coupons` + redemption stats

### Out of Scope
- Frontend (unit 002)
- Stacking / combinable coupons

---

## Story Summary

| Story ID | Title | Priority |
|----------|-------|----------|
| 001-coupon-schema | Tables + migrations + `Orders` additions | Must |
| 002-cart-coupon-endpoints | Customer apply/clear endpoints with validation | Must |
| 003-redemption-on-order-create | Atomic redemption with `RowVersion` + VAT order | Must |
| 004-admin-coupon-crud | Admin CRUD + redemption stats | Should |
