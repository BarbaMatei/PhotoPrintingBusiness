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
  - Payment-webhook success ≥ 99.9% (`ok` + `duplicate` over all results except `signature_invalid`
    — amended 2026-08-05: a correctly-answered duplicate is a success by the SLO's own definition,
    and an anonymous bad signature is not a request this app failed)
  - AWB auto-creation ≥ 98% (`ok` over all results except `skipped` and `retry_later` — amended
    2026-08-06: a `skipped` outcome means no label was needed at all, and `retry_later` is counted
    once per attempt, so keeping it would score an order that succeeded on its third try as 1 of 3
    and flag the retry loop the 2% budget exists to protect. `orphaned` — a billable label the
    order no longer references — stays in the denominator as the failure it is)
  - ANAF submission success ≥ 99% (`accepted` over all statuses except `pending` — amended
    2026-08-06: a submission still in flight is not yet a failure)
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
