---
id: 001-upload-and-cart-backend
intent: 004-checkout-payment
type: backend
bolt_type: ddd-construction-bolt
bolts: ["012-photo-upload-backend", "013-cart-api"]
status: draft
created: 2026-05-21T12:00:00Z
---

# Unit Brief: upload-and-cart-backend

## Purpose

All backend work to receive, validate, store, and serve uploaded photos; plus the server-side cart that ties uploads to a product selection with quantities and computes totals.

## Why Two Bolts?

The upload domain (file I/O, ImageSharp, IStorageService, background job) is structurally independent from the cart (relational join of Uploads + Products with quantity logic). Splitting them keeps each bolt focused and testable in isolation.

## Key Technical Challenges

- MIME magic byte validation (security boundary at upload)
- `IStorageService` abstraction must be clean enough for future S3 swap
- Cart merge atomicity (database transaction across guest and user records)

## Stories

| # | Story | FRs | Bolt |
|---|-------|-----|------|
| 001 | upload-entity-schema | FR-1, FR-2 | 012 |
| 002 | upload-endpoint | FR-1 | 012 |
| 003 | upload-preview-and-cleanup | FR-3, FR-4 | 012 |
| 004 | cart-item-entity | FR-5 | 013 |
| 005 | cart-crud-endpoints | FR-5 | 013 |
| 006 | cart-merge-endpoint | FR-6 | 013 |

## Dependencies

- **Requires**: Bolt 005 (auth-core — JWT + guest token), Bolt 007 (guest-sessions), Bolt 009 (product-catalog-core — Products table)
- **Enables**: Bolt 014 (upload-format-cart-ui), Bolt 015 (shipping-and-order-core via cart→order)
