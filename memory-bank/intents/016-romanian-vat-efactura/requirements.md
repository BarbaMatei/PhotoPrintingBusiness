---
intent: 016-romanian-vat-efactura
phase: inception
status: complete
created: 2026-05-25T10:15:00Z
updated: 2026-05-25T10:15:00Z
source: docs/architecture-analysis-2026-05-25.md#4
priority_score: 19
---

# Requirements: Romanian VAT + e-Factura

## Intent Overview

The codebase has zero references to TVA / VAT. Romania mandates 19 % VAT and e-Factura (ANAF SPV) submission. This intent adds VAT calculation to the order lifecycle and produces a UBL 2.1 e-Factura XML for every `Paid` order, uploaded to ANAF SPV and stored as PDF for customer download.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Comply with Romanian VAT law on all orders | 100% of `Paid` orders carry `VatRon` ≠ 0 and a generated invoice | Must |
| Comply with ANAF e-Factura mandate | 100% of `Paid` orders submitted to SPV within 5 business days | Must |
| Customers receive a downloadable, ANAF-valid invoice PDF | Email attachment + `GET /api/orders/{id}/invoice` | Must |
| Admins can audit and retry failed ANAF submissions | Admin list of pending/failed invoices with one-click retry | Should |

---

## Functional Requirements

### FR-1: VAT computation on order creation
- **Description**: Compute `VatRon = round(subtotal * VatRate / (1 + VatRate), 2)` on order creation. Persist `NetTotalRon`, `VatRon`, `VatRate` on every order. `TotalRon` is the gross amount the customer pays (unchanged).
- **Acceptance Criteria**:
  - For a 100.00 RON subtotal at 19 % VAT: `NetTotalRon = 84.03`, `VatRon = 15.97`, `TotalRon = 100.00`.
  - Rounding uses `MidpointRounding.AwayFromZero` to 2 decimal places.
  - `VatRate` is read from configuration (`Vat:Rate`, default 0.19).
- **Priority**: Must
- **Related Stories**: US-016-1

### FR-2: Invoice entity + numbering sequence
- **Description**: Introduce `Invoice` entity with strictly-sequential `InvoiceNumber` per fiscal year per series. Numbering uses a Postgres `SEQUENCE` per series for gap-free generation.
- **Acceptance Criteria**:
  - First invoice of series `FT` in 2026 is `FT-2026-00001`.
  - Numbering never has gaps within a fiscal year (legal requirement).
  - Concurrent `Paid` transitions never produce duplicate numbers.
- **Priority**: Must
- **Related Stories**: US-016-2

### FR-3: UBL 2.1 e-Factura XML generation
- **Description**: On `Paid` transition, build UBL 2.1-compliant XML per ANAF's CIUS-RO schema with seller, buyer, line items, VAT subtotal, totals.
- **Acceptance Criteria**:
  - XML validates against the ANAF XSD bundle (`UBL-Invoice-2.1.xsd` + CIUS-RO patch).
  - Includes the e-Factura mandatory fields: BT-1 (invoice number), BT-2 (issue date), BT-3 (invoice type 380), BT-22 (note), buyer CUI when present, line VAT category codes.
- **Priority**: Must
- **Related Stories**: US-016-3

### FR-4: ANAF SPV submission via background job
- **Description**: `InvoiceUploadJob : BackgroundService` submits queued invoices to ANAF SPV (OAuth-protected). On accepted upload, persists `AnafUploadId` and sets `AnafStatus = Submitted`; on validation rejection, sets `AnafStatus = Rejected` with the response message.
- **Acceptance Criteria**:
  - Job retries failed submissions with exponential backoff (1 h / 4 h / 16 h / 64 h).
  - Status polling separates submission acknowledgement from final ANAF validation.
- **Priority**: Must
- **Related Stories**: US-016-4

### FR-5: PDF rendering and storage
- **Description**: Render a human-readable PDF version of the invoice (Romanian + English labels). Store via existing `IStorageService` and surface via `GET /api/orders/{id}/invoice` and as an email attachment on the order-confirmation email.
- **Acceptance Criteria**:
  - PDF includes order details, parties (seller fiscal data, buyer name + address + CUI if available), itemised totals, VAT breakdown, payment method, AWB number (if available).
  - PDF accessible only by the order owner (JWT) or admin role.
- **Priority**: Must
- **Related Stories**: US-016-5

### FR-6: Admin invoice list + retry
- **Description**: `GET /api/admin/invoices?status=...` lists invoices with paging. `POST /api/admin/invoices/{id}/retry` re-queues a failed ANAF submission.
- **Acceptance Criteria**:
  - List shows `InvoiceNumber`, `OrderId`, `IssuedAt`, `AnafStatus`, `LastError`.
  - Retry only allowed for `Rejected` or `Failed` status.
- **Priority**: Should
- **Related Stories**: US-016-6

---

## Non-Functional Requirements

### Compliance
| Requirement | Standard | Notes |
|-------------|----------|-------|
| Invoice numbering | Romanian Fiscal Code | Strictly sequential per series per fiscal year |
| e-Factura submission | OUG 130/2021 + ANAF normative | Submission within 5 business days of issue |
| Data retention | 10 years for invoices | Aligned with Romanian retention rules |

### Performance
| Requirement | Metric | Target |
|-------------|--------|--------|
| Invoice generation | p95 from `Paid` transition to PDF stored | < 10 s |

### Security
| Requirement | Standard | Notes |
|-------------|----------|-------|
| ANAF certificate handling | PKCS#12 + passphrase from env | Never logged; never committed |
| Customer access | JWT + ownership check on `Invoice.OrderId.UserId` | Admin override via role |

---

## Constraints

### Technical Constraints
- Use `RazorLight` (already in use for emails) to render the PDF HTML, then convert via headless Chromium (e.g. `PuppeteerSharp`) — adds dependency.
- ANAF OAuth requires a real legal-entity digital certificate; staging without it relies on the SPV sandbox.

### Business Constraints
- Ship after intent 014 (payment hardening) — invoice math must not be undermined by client-tampered totals.
- Ship before intent 022 (coupons) — discount must subtract from the pre-VAT subtotal.

---

## Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| 19 % VAT applies to all SKUs (no reduced rates for photo prints) | Tax recategorisation breaks past invoices | Per-product VAT rate column kept on roadmap |
| ANAF SPV sandbox available for staging | Blocks staging tests | Mock SPV adapter behind interface |
| Seller fiscal data lives in config | Fiscal data change requires deploy | Acceptable; data changes rarely |

---

## Open Questions

| Question | Owner | Due Date | Resolution |
|----------|-------|----------|------------|
| Q1: Headless Chromium acceptable as deploy dependency? | DevOps | 2026-06-15 | Pending — alternative: `QuestPDF` library (smaller footprint) |
| Q2: Storage path: same bucket as photos, or separate `invoices/`? | Backend | 2026-06-01 | Recommend `invoices/{yyyy}/{mm}/{invoice-number}.pdf` |
| Q3: Buyer CUI optional at checkout or always? | Product | 2026-06-01 | Recommend optional, gated on `IsBusinessCustomer` flag |
