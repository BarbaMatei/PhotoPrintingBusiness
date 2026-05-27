---
intent: 009-background-jobs
phase: inception
status: complete
created: 2026-05-22T12:00:00Z
updated: 2026-05-22T12:00:00Z
---

# Requirements: Background Jobs

## Intent Overview

Implement the 4 scheduled maintenance jobs that keep the platform healthy without blocking the request pipeline. All jobs run as `IHostedService` implementations (no Hangfire at MVP). The EmailRetryJob depends on the email infrastructure from bolt 003; the AccountDeletionJob depends on the `DeletionRequestedAt` field added by bolt 023 (account-api).

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Orphaned upload files are cleaned up automatically | Disk not cluttered with unused photos after 24 h | Must |
| Expired guest sessions are purged | DB doesn't accumulate stale guest records | Must |
| Deletion-requested accounts are hard-deleted | GDPR 30-day obligation met automatically | Must |
| Failed emails are retried with backoff | Email delivery rate ≥ 99% over time | Must |

---

## Functional Requirements

### FR-1: Upload Cleanup Job
- **Description**: Hourly job. Soft-deletes `Upload` records with no associated `OrderItem` and `CreatedAt` older than 24 h; deletes physical files.
- **Acceptance Criteria**: Runs every hour; only targets orphaned uploads; physical file deleted after DB soft-delete; logs count of cleaned uploads.
- **Priority**: Must
- **Related Stories**: US-803

### FR-2: Guest Session Cleanup Job
- **Description**: Daily job. Purges `GuestSession` records with no associated `Order` and `ExpiresAt` in the past.
- **Acceptance Criteria**: Runs daily at 02:00; logs count of purged sessions; does not delete sessions with linked orders.
- **Priority**: Must
- **Related Stories**: US-803

### FR-3: Account Deletion Job
- **Description**: Daily job. Hard-deletes `User` records where `DeletionRequestedAt` is older than 30 days, including all their associated data (cascade).
- **Acceptance Criteria**: Runs daily at 03:00; logs count of deleted accounts; EF Core cascade deletes associated data; does not delete accounts without `DeletionRequestedAt`.
- **Priority**: Must
- **Related Stories**: US-803

### FR-4: Email Retry Job
- **Description**: Background job that processes a failed-email queue (in-memory `Channel<T>`) with exponential backoff, retrying up to 3 times before logging as permanently failed.
- **Acceptance Criteria**: Retries on intervals: 30 s, 2 min, 10 min; after 3 failures logs `Error` with email details; does not block request pipeline.
- **Priority**: Must
- **Related Stories**: US-803

---

## Non-Functional Requirements

| Category | Requirement |
|----------|-------------|
| Simplicity | `IHostedService` only — no Hangfire, no Quartz |
| Observability | Each job logs start, count processed, duration at `Information` level |
| Safety | Jobs run in separate try/catch; one job failure does not crash the host |
| Dependencies | Upload cleanup depends on `IStorageService` (bolt 012); Email retry depends on `IEmailService` (bolt 003) |

---

## Out of Scope

- Job scheduling UI / dashboard
- Hangfire or other external schedulers
- Distributed locking (single-instance deployment at MVP)
