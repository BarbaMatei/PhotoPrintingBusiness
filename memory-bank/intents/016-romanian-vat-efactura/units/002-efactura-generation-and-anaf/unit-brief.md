---
unit: 002-efactura-generation-and-anaf
intent: 016-romanian-vat-efactura
phase: inception
status: draft
created: 2026-05-25T10:15:00Z
updated: 2026-05-25T10:15:00Z
---

# Unit Brief: e-Factura Generation & ANAF Submission

## Purpose

Generate compliant UBL 2.1 e-Factura XML, render a customer-facing PDF, upload to ANAF SPV, and expose admin tooling for the workflow.

## Scope

### In Scope
- `IInvoiceXmlBuilder` — produce CIUS-RO compliant XML
- `IInvoicePdfRenderer` — RazorLight HTML + QuestPDF (or PuppeteerSharp) PDF
- `IAnafSpvClient` — OAuth + upload + status polling
- `InvoiceUploadJob : BackgroundService` — retries with backoff
- `Controllers/InvoicesController` — `GET /api/orders/{id}/invoice` (customer)
- `Controllers/AdminInvoicesController` — list, retry, download
- Email change: attach PDF to order-confirmation email

### Out of Scope
- Credit notes (refund-invoice generation) — separate intent
- Multi-currency

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-3 | UBL 2.1 e-Factura XML generation | Must |
| FR-4 | ANAF SPV submission via background job | Must |
| FR-5 | PDF rendering and storage | Must |
| FR-6 | Admin invoice list + retry | Should |

---

## Story Summary

| Story ID | Title | Priority |
|----------|-------|----------|
| 001-ubl-xml-builder | Build UBL 2.1 + CIUS-RO compliant XML payload | Must |
| 002-anaf-spv-client | OAuth + upload + status-check client | Must |
| 003-invoice-pdf-renderer-and-endpoint | PDF render, customer endpoint, email attachment | Must |
| 004-admin-invoice-list-and-retry | Admin list/retry endpoints + UI hook | Should |

---

## Dependencies

### Depends On
- 001-vat-calculation

### Depended By
- intent 022 (coupons affect printed totals)
