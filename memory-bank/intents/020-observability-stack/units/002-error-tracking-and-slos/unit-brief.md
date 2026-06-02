---
unit: 002-error-tracking-and-slos
intent: 020-observability-stack
phase: inception
status: complete
created: 2026-05-25T10:35:00.000Z
updated: 2026-05-25T10:35:00.000Z
---

# Unit Brief: Error Tracking & SLOs

## Purpose

Capture unhandled exceptions in Sentry with full context, scrub PII, and document operational SLOs the team will commit to.

## Scope

### In Scope
- Sentry SDK + DSN config
- Tag/context enrichment (correlation id, user id, release sha)
- PII scrubbing rules
- SLO documentation
- Sample Grafana dashboard JSON

### Out of Scope
- Auto-paging (PagerDuty etc.) — separate ops decision
- Frontend error tracking (consider later)

---

## Story Summary

| Story ID | Title | Priority |
|----------|-------|----------|
| 001-sentry-aspnet-integration | Sentry SDK with correlation + release tagging | Must |
| 002-slo-documentation-and-dashboard | SLO doc + sample Grafana dashboard JSON | Should |
