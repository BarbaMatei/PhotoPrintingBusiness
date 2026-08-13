---
id: 001-sentry-aspnet-integration
unit: 002-error-tracking-and-slos
intent: 020-observability-stack
status: complete
priority: must
created: 2026-05-25T10:35:00.000Z
assigned_bolt: 045-error-tracking-and-slos
implemented: true
---

# Story: 001-sentry-aspnet-integration

## User Story

**As** an oncall engineer
**I want** every unhandled exception to land in Sentry with correlation id and user id
**So that** I get an immediate page or notification instead of finding out from customers

## Acceptance Criteria

- [ ] `Sentry.AspNetCore` package added.
- [ ] `builder.WebHost.UseSentry(o => …)` wired; DSN from `Sentry:Dsn` config.
- [ ] Every Sentry event has tags: `correlation_id`, `user_id` (when authenticated), `environment`, `release` (image SHA).
- [ ] PII scrubbing: email, phone, full request body redacted; only structured metadata sent.
- [ ] Sample rate configurable; default 100 % errors / 10 % transactions.
- [ ] Integration test: a synthetic 500 endpoint produces a Sentry event in the in-memory transport.

## Technical Notes

- Release tag pulled from env var `GIT_COMMIT_SHA` (set by deploy workflow in intent 017).
- Correlation id pulled from existing `CorrelationContext`.
- Data scrubber list maintained in `Configuration/SentryDataScrubbers.cs`.

## Dependencies

### Requires
- intent 017 (release SHA env var)

### Enables
- 002-slo-documentation-and-dashboard

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Sentry DSN absent | SDK no-ops; no boot failure |
| Network partition to Sentry | SDK queues with bounded size; drops oldest on overflow |

## Out of Scope

- Frontend Sentry SDK (separate intent).
