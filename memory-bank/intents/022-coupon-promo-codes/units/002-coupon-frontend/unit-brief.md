---
unit: 002-coupon-frontend
intent: 022-coupon-promo-codes
phase: inception
status: draft
created: 2026-05-25T10:45:00Z
updated: 2026-05-25T10:45:00Z
---

# Unit Brief: Coupon Frontend

## Purpose

Wire the coupon input into the cart, surface the discount on cart / review / confirmation pages, and add the discount line to the rendered invoice.

## Scope

### In Scope
- Coupon input + Apply button on cart page
- Romanian copy mapping per `code:` value
- Discount line on cart summary, review summary, confirmation, and invoice PDF template

### Out of Scope
- Admin coupon management UI (a future intent — backend endpoints are ready)

---

## Story Summary

| Story ID | Title | Priority |
|----------|-------|----------|
| 001-cart-coupon-ux | Coupon input + apply/clear + RO copy + discount lines | Should |
