---
intent: 020-observability-stack
phase: inception
status: complete
created: 2026-05-25T10:35:00Z
updated: 2026-05-25T10:35:00Z
source: docs/architecture-analysis-2026-05-25.md#8
priority_score: 15
---

# Requirements: Observability Stack

## Intent Overview

Current observability is JSON file logs + correlation IDs (score 2/5). No metrics, no traces, no error aggregator. First production incident requires manual log grep. This intent wires OpenTelemetry traces, custom Prometheus metrics, and Sentry for unhandled exceptions.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Reduce MTTR for production incidents | p95 time from incident → root cause < 30 min | Should |
| Surface revenue-critical errors in real time | Payment / webhook failures alert in < 5 min | Must |
| Track business KPIs as first-class metrics | Orders / day, conversion, AWB success rate visible | Should |

---

## Functional Requirements

### FR-1: OpenTelemetry tracing + ASP.NET / HttpClient / EF Core instrumentation
- **Description**: Add OTel SDK packages and wire instrumentation. Export via OTLP to a configurable endpoint (Honeycomb, Grafana Tempo, SigNoz).
- **Acceptance Criteria**:
  - `Activity.Current.TraceId` flows correctly across `HttpClient` calls (Stripe, Sameday, ANAF).
  - EF Core spans show parameterised SQL.
  - Trace sampling rate configurable; default 5% on `/api/uploads/{id}/preview`, 100% elsewhere.
- **Priority**: Should
- **Related Stories**: US-020-1

### FR-2: Prometheus metrics endpoint + custom business metrics
- **Description**: Expose `/metrics` (Prometheus scrape format) and define custom metrics for orders, payments, uploads, AWB success.
- **Acceptance Criteria**:
  - `orders_created_total{processor,status}` counter increments per order.
  - `payment_webhook_total{processor,result}` counter increments per webhook.
  - `upload_size_bytes` histogram populated per upload.
  - `order_processing_duration_seconds` histogram tracks Paid→Shipped duration.
  - Endpoint protected by IP allowlist or basic auth (configurable).
- **Priority**: Should
- **Related Stories**: US-020-2

### FR-3: Sentry integration for unhandled exceptions
- **Description**: `Sentry.AspNetCore` captures unhandled exceptions; events tagged with `correlation_id`, `user_id`, environment.
- **Acceptance Criteria**:
  - Every `5xx ProblemDetails` corresponds to a Sentry event (correlation id matches).
  - PII scrubbed via Sentry data filters (no email, no full request body).
  - Release tagged with the deployed image SHA.
- **Priority**: Must
- **Related Stories**: US-020-3

### FR-4: SLO definitions and documentation
- **Description**: Document SLOs: availability ≥ 99.5%, p95 checkout latency ≤ 1.5 s, payment-webhook success ≥ 99.9%.
- **Acceptance Criteria**:
  - `memory-bank/operations/slos.md` lists each SLO with measurement source.
  - Sample Grafana dashboard JSON exported under `ops/dashboards/`.
- **Priority**: Should
- **Related Stories**: US-020-4

### FR-5: Sampling and high-volume endpoint tuning
- **Description**: Endpoints with predictable high RPS (`GET /api/uploads/{id}/preview`, `GET /api/products`) sample traces at 5%.
- **Acceptance Criteria**:
  - Sampler decision recorded; can be inspected in OTel processor logs.
  - Sampling rate configurable per route.
- **Priority**: Should
- **Related Stories**: US-020-5

---

## Non-Functional Requirements

### Performance
| Requirement | Metric | Target |
|-------------|--------|--------|
| Tracing overhead | CPU delta | < 7% |
| Metrics overhead | CPU delta | < 2% |

### Reliability
| Requirement | Metric | Target |
|-------------|--------|--------|
| Observability outage | OTel collector down | App continues; OTel SDK drops batches gracefully |

### Compliance
| Requirement | Standard | Notes |
|-------------|----------|-------|
| PII scrubbing | GDPR | Sentry data scrubbing + OTel processor filter |

---

## Constraints

### Technical Constraints
- Must coexist with existing Serilog logs; do not replace.
- Must depend on intent 017 (deploy artefacts) — staging needs an OTel collector.

### Business Constraints
- Lower priority (#8 in roadmap); ship after #3, #4, #5 land.

---

## Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| Self-hosted SigNoz / Grafana stack acceptable | Team wants SaaS only | Config picks endpoint per env |
| OTel SDK overhead acceptable in production | CPU bottleneck | Per-route sampling, lower default rate |

---

## Open Questions

| Question | Owner | Due Date | Resolution |
|----------|-------|----------|------------|
| Q1: Self-hosted SigNoz vs. SaaS (Honeycomb) | Ops | 2026-07-15 | Pending — SigNoz preferred for cost; Honeycomb if budget allows |
| Q2: Sentry free tier vs. self-host GlitchTip | Ops | 2026-07-15 | Pending |
