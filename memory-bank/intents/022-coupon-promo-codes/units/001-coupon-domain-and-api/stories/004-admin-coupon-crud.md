---
id: 004-admin-coupon-crud
unit: 001-coupon-domain-and-api
intent: 022-coupon-promo-codes
status: draft
priority: should
created: 2026-05-25T10:45:00Z
assigned_bolt: 047-coupon-domain-and-api
implemented: false
---

# Story: 004-admin-coupon-crud

## User Story

**As** an admin
**I want** to create, edit, deactivate, and inspect coupons
**So that** marketing can run campaigns without engineering changes

## Acceptance Criteria

- [ ] `GET /api/admin/coupons?status=active|inactive|expired&page&size` — paged list.
- [ ] `POST /api/admin/coupons` — create with FluentValidation (`Code` matches `[A-Z0-9]{4,20}`, `ValidUntil > ValidFrom`, `Value > 0`, `Type` valid).
- [ ] `PUT /api/admin/coupons/{id}` — update; `Code` immutable after first redemption.
- [ ] `DELETE /api/admin/coupons/{id}` — soft delete (`IsActive = false`); 409 if already inactive.
- [ ] `GET /api/admin/coupons/{id}/redemptions?page&size` — paged list of `CouponRedemption` rows joined to `Orders.OrderNumber`.
- [ ] All endpoints require `Admin` role; audit-logged with admin user id.

## Technical Notes

- `CouponValidator : AbstractValidator<CouponCreateRequest>` reuses FluentValidation pattern from existing admin endpoints.
- Soft-delete behaviour matches the broader convention (see intent 022 requirements assumption set).

## Dependencies

### Requires
- 003-redemption-on-order-create

### Enables
- Admin coupon UI in a future intent

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Code already exists | 409 with `code: "DUPLICATE_CODE"` |
| Editing a coupon mid-campaign | Allowed for non-code fields; existing redemptions unaffected |

## Out of Scope

- Bulk import via CSV.
