---
unit: 001-vat-calculation
intent: 016-romanian-vat-efactura
phase: inception
status: complete
created: 2026-05-25T10:15:00.000Z
updated: 2026-05-25T10:15:00.000Z
---

# Unit Brief: VAT Calculation

## Purpose

Compute Romanian VAT correctly on every order, persist the breakdown, and introduce strictly-sequential invoice numbering ready for the e-Factura pipeline.

## Scope

### In Scope
- Schema: `Orders.NetTotalRon`, `VatRon`, `VatRate`
- Schema: `Invoices` table (Id, OrderId, InvoiceNumber, Series, IssuedAt, NetTotalRon, VatRon, TotalRon, AnafStatus, …)
- Schema: Postgres `SEQUENCE` per series + helper for next number
- `OrderService.CreateFromCartAsync` — compute VAT and persist all three columns
- `IInvoiceNumberingService` — `NextNumber(seriesCode)` returns gap-free numbers

### Out of Scope
- UBL XML, PDF, ANAF (002)
- Reduced VAT rates per SKU
- Reverse-charge for B2B EU customers

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-1 | VAT computation on order creation | Must |
| FR-2 | Invoice entity + numbering sequence | Must |

---

## Story Summary

| Story ID | Title | Priority |
|----------|-------|----------|
| 001-vat-fields-and-computation | Schema + service computes VAT on `CreateFromCartAsync` | Must |
| 002-invoice-entity-and-numbering | `Invoice` entity, Postgres sequence per series, `IInvoiceNumberingService` | Must |

---

## Dependencies

### Depends On
- intent 014 (clean order totals)

### Depended By
- 002-efactura-generation-and-anaf
- intent 022 (coupons subtract from `NetTotalRon`)
