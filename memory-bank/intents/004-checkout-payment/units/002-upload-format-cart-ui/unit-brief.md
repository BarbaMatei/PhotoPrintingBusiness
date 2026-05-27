---
id: 002-upload-format-cart-ui
intent: 004-checkout-payment
type: frontend
bolt_type: simple-construction-bolt
bolts: ["014-upload-format-cart-ui"]
status: draft
created: 2026-05-21T12:00:00Z
---

# Unit Brief: upload-format-cart-ui

## Purpose

All Angular work for the customer-facing upload experience — dragging photos, watching them upload with progress, selecting a global format and finish, adjusting quantities per photo, reviewing the summary, and managing the cart page.

## Why One Bolt?

These three pages (upload, format-selector, cart) form a tightly coupled user journey where shared state (`UploadService`, `CartService`, `ProductService`) flows through all of them. Bundling them in one bolt avoids partial delivery that leaves the user stuck mid-flow.

## Key Technical Challenges

- HEIC preview via `heic2any` (browser-only, async conversion)
- Quality badge recalculation on format change must be synchronous and reactive
- `CartService` must maintain a single source of truth between localStorage (guest) and server (logged-in) with merge-on-login

## Stories

| # | Story | FRs | Bolt |
|---|-------|-----|------|
| 001 | upload-page | FR-7 | 014 |
| 002 | format-finish-selector | FR-8 | 014 |
| 003 | order-summary-panel | FR-8 | 014 |
| 004 | cart-page | FR-9 | 014 |
| 005 | cart-service | FR-9 | 014 |

## Dependencies

- **Requires**: Bolt 013 (cart-api), Bolt 011 (product-catalog-ui — shared product models/service)
- **Enables**: Bolt 017 (checkout-ui — needs CartService for checkout state)
