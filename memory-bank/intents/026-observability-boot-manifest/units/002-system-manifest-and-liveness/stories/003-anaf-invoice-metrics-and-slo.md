---
id: 003-anaf-invoice-metrics-and-slo
unit: 002-system-manifest-and-liveness
intent: 026-observability-boot-manifest
status: draft
priority: must
created: 2026-06-05T09:30:00Z
assigned_bolt: 056-system-manifest-and-liveness
implemented: false
---

# Story: 003-anaf-invoice-metrics-and-slo

## User Story

**As an** SRE responsible for the ANAF legal SLA
**I want** invoice-upload metrics and an SLO
**So that** ANAF submission lag is observable without deriving it from logs

## Acceptance Criteria

- [ ] **Given** `FotoMetrics`, **When** extended, **Then** `invoice_upload_total{result}` counter and `invoice_upload_lag_seconds` histogram exist (mirroring `payment_webhook_total`)
- [ ] **Given** `InvoiceUploadJob.ProcessOneAsync`, **When** it finishes, **Then** it stamps the counter with `result: accepted | rejected | failed | retried` and records lag
- [ ] **Given** `slos.md`, **When** updated, **Then** it contains an ANAF upload-lag SLO referencing the ADR-024 5-business-day SLA
- [ ] **Given** Prometheus, **When** scraping `/metrics`, **Then** the new series are present

## Technical Notes

- Lag = time from invoice ready/pending to ANAF acceptance.

## Dependencies

### Requires
- None

### Enables
- None

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| ANAF down / retrying | `result=retried`; lag keeps accruing until accepted |

## Out of Scope

- Alerting rules (ops config, not code).
