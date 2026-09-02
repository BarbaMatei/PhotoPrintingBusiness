---
id: 003-invoice-pdf-renderer-and-endpoint
unit: 002-efactura-generation-and-anaf
intent: 016-romanian-vat-efactura
status: complete
priority: must
created: 2026-05-25T10:15:00.000Z
assigned_bolt: 039-efactura-anaf
implemented: true
---

# Story: 003-invoice-pdf-renderer-and-endpoint

## User Story

**As** a customer
**I want** to download a PDF of my invoice and receive it by email
**So that** I have a copy without re-logging into the site

## Acceptance Criteria

- [ ] `IInvoicePdfRenderer.RenderAsync(Order, Invoice, Seller)` returns a PDF byte stream.
- [ ] PDF is rendered from a Razor template in `src/PhotoPrint.API/Templates/Invoices/Invoice.cshtml` and converted to PDF via QuestPDF (recommended) or PuppeteerSharp.
- [ ] PDF contains: invoice number, issue date, parties (seller + buyer), line items with quantity / unit price / line VAT, totals (`NetTotalRon`, `VatRon`, `TotalRon`), payment processor, AWB number when available, fiscal note.
- [ ] PDF stored via `IStorageService.SaveAsync(stream, "invoices/{yyyy}/{mm}/{invoiceNumber}.pdf")`; path persisted on `Invoice.PdfStoragePath`.
- [ ] `GET /api/orders/{id}/invoice` — JWT-only, ownership-checked, returns 200 PDF stream (or 404 if not yet rendered, 403 if not owner).
- [ ] Order confirmation email attaches the PDF when the invoice is already rendered at email-send time; otherwise a "Invoice will follow" line is included and a separate email fires once the PDF exists.

## Technical Notes

- PDF render kicks off after `001-ubl-xml-builder` completes; same job loop.
- Caching: PDF is immutable once generated; `Cache-Control: private, max-age=31536000, immutable`.

## Dependencies

### Requires
- 001-ubl-xml-builder (so the same data is used)

### Enables
- 004-admin-invoice-list-and-retry

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Customer requests invoice before render | 404 with `Retry-After: 30` |
| Storage write fails | Job retries; invoice stays without PDF; customer gets 404 until success |
| PDF key set but the blob is absent from the stamped tier | Falls back to the other tier once when cloud storage is on, logging `invoice.pdf.tier-mismatch` |
| Blob absent from every candidate tier | 404 `problem+json` with NO `Retry-After` (retrying cannot help), logging `invoice.pdf.blob-missing` |

## Out of Scope

- Credit-note PDFs.
