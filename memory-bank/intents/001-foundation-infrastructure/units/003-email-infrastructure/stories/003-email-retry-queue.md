---
id: 003-email-retry-queue
unit: 003-email-infrastructure
intent: 001-foundation-infrastructure
status: draft
priority: must
created: 2026-05-05T15:26:00Z
assigned_bolt: null
implemented: false
---

# Story: 003-email-retry-queue

## User Story

**As a** system
**I want** failed email sends persisted to a database queue and retried with exponential backoff
**So that** transient email delivery failures don't result in lost notifications

## Acceptance Criteria

- [ ] **Given** an email send fails, **When** the failure is caught, **Then** the email is persisted to the `EmailQueue` table with status `Pending` and `NextRetryAt` set to now + 1 second
- [ ] **Given** a queued email with `NextRetryAt` in the past, **When** the `EmailRetryJob` runs, **Then** it attempts to send the email
- [ ] **Given** a retry fails, **When** the attempt count is < 3, **Then** `NextRetryAt` is updated with exponential backoff (1s, 4s, 16s) and `Attempts` is incremented
- [ ] **Given** a retry fails, **When** the attempt count reaches 3, **Then** status is set to `Failed` and the failure is logged with Serilog at Error level
- [ ] **Given** a retry succeeds, **When** the email is delivered, **Then** status is set to `Sent` and `SentAt` is recorded
- [ ] **Given** the application restarts, **When** the `EmailRetryJob` starts, **Then** it picks up any `Pending` emails from the database

## Technical Notes

- Create `EmailQueue` entity: Id (Guid), To, Subject, HtmlBody, Status (Pending/Sent/Failed), Attempts (int), NextRetryAt (DateTimeOffset), CreatedAt, SentAt (nullable), LastError (text, nullable)
- Create EF Core migration for `EmailQueue` table
- `EmailRetryJob` implements `IHostedService` / `BackgroundService`
- Polls database every 10 seconds for pending emails with `NextRetryAt <= now`
- Exponential backoff formula: `delay = 1s * 4^(attempt-1)` → 1s, 4s, 16s
- Use `IServiceScopeFactory` to create scoped DbContext in background service

## Dependencies

### Requires
- 001-email-service-abstraction (provides the actual send mechanism)

### Enables
- Reliable email delivery for all transactional emails

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| EmailRetryJob crashes | BackgroundService restarts automatically |
| Duplicate send (race condition) | Check status before sending; use optimistic concurrency |
| Very large queue backlog | Process in batches of 10; don't block other background work |
| Database unavailable during retry | Log error, skip cycle, retry on next poll |

## Out of Scope

- Dead letter queue / manual retry UI
- Email delivery webhooks (SendGrid events)
