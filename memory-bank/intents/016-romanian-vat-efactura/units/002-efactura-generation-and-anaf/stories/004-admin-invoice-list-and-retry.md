---
id: 004-admin-invoice-list-and-retry
unit: 002-efactura-generation-and-anaf
intent: 016-romanian-vat-efactura
status: draft
priority: should
created: 2026-05-25T10:15:00Z
assigned_bolt: 039-efactura-anaf
implemented: false
---

# Story: 004-admin-invoice-list-and-retry

## User Story

**As** an admin
**I want** to see all invoices with their ANAF status and retry failed ones with one click
**So that** I can resolve compliance issues without DB access

## Acceptance Criteria

- [ ] `GET /api/admin/invoices?status=Pending|Submitted|Validated|Rejected&page=N&size=M` returns paged list (offset envelope `{items, total, page, size}`).
- [ ] Each row: `InvoiceNumber`, `OrderId`, `OrderNumber`, `IssuedAt`, `AnafStatus`, `LastError`.
- [ ] `POST /api/admin/invoices/{id}/retry` re-queues a failed upload. Only allowed if `AnafStatus IN (Rejected, Failed)`.
- [ ] `GET /api/admin/invoices/{id}/xml` returns the raw UBL XML payload (Admin role).
- [ ] Angular admin UI receives a new "Invoices" panel — out of scope for this story (placeholder JSON consumer noted in bolt plan).

## Technical Notes

- Reuse existing admin authentication and pagination helpers.
- All operations logged with Information level + admin user id (audit trail).

## Dependencies

### Requires
- 002-anaf-spv-client, 003-invoice-pdf-renderer-and-endpoint

### Enables
- Admin UI follow-up intent

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Concurrent retries | Idempotent (`UPDATE Invoice SET AnafStatus = 'Pending' WHERE Status = 'Rejected'` returning the row count) |
| ANAF temporarily down at retry | Polly + standard retry job picks up |

## Out of Scope

- Customer-facing invoice list (`/api/account/invoices`) — covered by existing account intent in a follow-up.
