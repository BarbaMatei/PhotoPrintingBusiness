---
id: 001-ubl-xml-builder
unit: 002-efactura-generation-and-anaf
intent: 016-romanian-vat-efactura
status: draft
priority: must
created: 2026-05-25T10:15:00Z
assigned_bolt: 039-efactura-anaf
implemented: false
---

# Story: 001-ubl-xml-builder

## User Story

**As** the invoicing subsystem
**I want** to build a UBL 2.1 + CIUS-RO compliant XML for any paid order
**So that** the document is ready for ANAF SPV submission without manual touch-ups

## Acceptance Criteria

- [ ] `IInvoiceXmlBuilder.Build(Order, Invoice, Seller)` returns a UTF-8 XML byte stream.
- [ ] Output validates against bundled `UBL-Invoice-2.1.xsd` + CIUS-RO patch (validation invoked in unit tests).
- [ ] Required UBL Business Terms present and correct:
  - BT-1 `InvoiceNumber`
  - BT-2 `IssueDate` (yyyy-MM-dd)
  - BT-3 `InvoiceTypeCode` = 380
  - BT-22 `Note` (free-form, e.g. order reference)
  - BT-31 / 32 seller identification (CUI, registration number, address)
  - BT-44+ buyer identification (name + address; CUI when present)
  - BG-25 invoice lines with VAT category code
  - BG-22 document totals (net, tax, gross)
- [ ] Per-line VAT category defaults to `S` (standard rate 19%); reduced/exempt categories available behind config.

## Technical Notes

- Use `System.Xml.Linq` or `XmlSerializer` with hand-rolled types — XSD-generated code is bulky; a 200-line builder is easier to audit.
- Seller fiscal data read from `Seller:` config block.

## Dependencies

### Requires
- 001-vat-calculation (unit)

### Enables
- 002-anaf-spv-client, 003-invoice-pdf-renderer-and-endpoint

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Buyer is a guest (no CUI) | Omit BT-48 (buyer VAT identifier); BT-44 (buyer name) becomes "Persoană fizică" |
| Order with zero items (impossible) | Reject in builder with `InvalidOperationException` |

## Out of Scope

- E-signing the XML in our process (ANAF signs after submission).
