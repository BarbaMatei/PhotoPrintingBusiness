---
id: 002-anaf-spv-client
unit: 002-efactura-generation-and-anaf
intent: 016-romanian-vat-efactura
status: draft
priority: must
created: 2026-05-25T10:15:00Z
assigned_bolt: 039-efactura-anaf
implemented: false
---

# Story: 002-anaf-spv-client

## User Story

**As** the operator
**I want** invoices uploaded to ANAF SPV automatically with status polling
**So that** compliance is achieved without manual SPV portal use

## Acceptance Criteria

- [ ] `IAnafSpvClient.UploadAsync(invoiceXml)` POSTs to the SPV `/upload` endpoint with OAuth Bearer (from `AnafTokenProvider`).
- [ ] On 200 OK, returns `AnafUploadId`; the invoice row is updated to `AnafStatus = Submitted`.
- [ ] `IAnafSpvClient.GetStatusAsync(uploadId)` polls the `/stareMesaj` endpoint; maps response → `Submitted | Validated | Rejected`.
- [ ] `InvoiceUploadJob : BackgroundService` runs every 30 min (configurable). Picks up invoices in `AnafStatus IN (Pending, Submitted)` and advances them.
- [ ] On `Rejected`, `Invoice.LastError` records the ANAF error message and the next retry is scheduled with exponential backoff (1h, 4h, 16h, 64h then stop).
- [ ] All ANAF requests are logged at Information with correlation id; payload bodies redacted to avoid PII leakage in logs.

## Technical Notes

- ANAF OAuth client credentials + PKCS#12 cert path read from `Anaf:` config; never logged.
- HTTP client registered via `IHttpClientFactory` with retry policy (5xx only).
- Cert loaded once into `X509Certificate2` and re-used; reloaded on SIGHUP / startup only.

## Dependencies

### Requires
- 001-ubl-xml-builder

### Enables
- 003-invoice-pdf-renderer-and-endpoint, 004-admin-invoice-list-and-retry

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Cert expired | Boot fails with clear error; admin replaces cert via env var + restart |
| ANAF returns 503 | Polly retries; eventually leaves invoice `Pending`; next tick picks up |
| Job crashed mid-upload | Idempotent — `AnafUploadId` unique on retry via app-side dedupe key (invoice number) |

## Out of Scope

- Bulk re-upload tool (admin manual retry covers).
