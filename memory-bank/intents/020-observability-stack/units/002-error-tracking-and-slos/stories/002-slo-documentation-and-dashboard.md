---
id: 002-slo-documentation-and-dashboard
unit: 002-error-tracking-and-slos
intent: 020-observability-stack
status: complete
priority: should
created: 2026-05-25T10:35:00.000Z
assigned_bolt: 045-error-tracking-and-slos
implemented: true
---

# Story: 002-slo-documentation-and-dashboard

## User Story

**As** the team
**I want** SLOs and a starter dashboard in the repo
**So that** the metrics we just shipped have an obvious "good vs. bad" reading

## Acceptance Criteria

- [ ] `memory-bank/operations/slos.md` documents:
  - Availability ≥ 99.5% (rolling 30 d)
  - p95 checkout latency ≤ 1.5 s on `POST /api/payments/stripe/intent`
  - Payment-webhook success ≥ 99.9% (`payment_webhook_total{result="ok"}` / total)
  - AWB auto-creation ≥ 98% (intent 015)
  - ANAF submission success ≥ 99% (intent 016)
- [ ] `ops/dashboards/fototipar-overview.json` provides a Grafana dashboard JSON with: RPS, latency p50/p95/p99, error rate, orders/day, payment-webhook success, AWB success, ANAF status.
- [ ] README link added under Operations section.

## Technical Notes

- Dashboard rows correspond 1:1 to the metrics defined in unit 001 story 002.

## Dependencies

### Requires
- intent 020 unit 001 (metrics)

### Enables
- Future SRE practice (error budgets, burn alerts)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Team renames a metric | Dashboard breaks; doc cross-reference makes the fix obvious |

## Out of Scope

- Burn-rate alerts (next intent if needed).
