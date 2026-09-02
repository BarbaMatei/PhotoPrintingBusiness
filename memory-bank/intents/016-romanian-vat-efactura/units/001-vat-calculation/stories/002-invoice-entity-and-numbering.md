---
id: 002-invoice-entity-and-numbering
unit: 001-vat-calculation
intent: 016-romanian-vat-efactura
status: complete
priority: must
created: 2026-05-25T10:15:00.000Z
assigned_bolt: 038-vat-calculation
implemented: true
---

# Story: 002-invoice-entity-and-numbering

## User Story

**As** a Romanian fiscal authority
**I want** the platform's invoice numbers to be strictly sequential per series per year
**So that** the audit trail satisfies the Fiscal Code

## Acceptance Criteria

- [ ] EF migration creates `Invoices` table:
  ```sql
  CREATE TABLE "Invoices" (
      "Id"             uuid PRIMARY KEY,
      "OrderId"        uuid NOT NULL REFERENCES "Orders"("Id"),
      "InvoiceNumber"  varchar(50) NOT NULL UNIQUE,
      "Series"         varchar(10) NOT NULL,
      "IssuedAt"       timestamptz NOT NULL,
      "NetTotalRon"    numeric(18,2) NOT NULL,
      "VatRon"         numeric(18,2) NOT NULL,
      "TotalRon"       numeric(18,2) NOT NULL,
      "XmlPayload"     text          NULL,
      "PdfStoragePath" varchar(500)  NULL,
      "AnafUploadId"   varchar(100)  NULL,
      "AnafStatus"     varchar(30)   NOT NULL,
      "LastError"      text          NULL,
      "CreatedAt"      timestamptz   NOT NULL,
      "UpdatedAt"      timestamptz   NULL
  );
  ```
- [ ] Postgres `SEQUENCE invoice_seq_ft_2026 START 1 INCREMENT 1` created per `(series, year)` pair via a deterministic helper at startup (idempotent CREATE IF NOT EXISTS via raw SQL in migration).
- [ ] `IInvoiceNumberingService.NextNumberAsync("FT", year)` returns `FT-2026-00001`, `FT-2026-00002`, …
- [ ] No two concurrent transactions produce the same number (`nextval()` is atomic).
- [ ] Crossing into 2027 starts a new sequence `FT-2027-00001`.

## Technical Notes

- A separate sequence per series-year keeps numbers gap-free per the legal requirement and naturally resets on January 1.
- The sequence is created idempotently per `(series, year)` on first use, so a new year needs no migration.

## Dependencies

### Requires
- 001-vat-fields-and-computation

### Enables
- All of unit 002

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Migration runs mid-year | Sequence starts at 1; consider import script to backfill from existing `Orders` if needed (one-shot, manual) |
| Order rolled back after `nextval` | Number is consumed (gap acceptable only if it's the LAST number — Postgres sequences allow gaps on rollback). **Mitigate**: allocate the number in a separate, idempotent step right before `Paid`, and persist immediately. Document the constraint in `decision-index.md`. |

## Out of Scope

- Sequence reset migrations across legal-entity changes.
