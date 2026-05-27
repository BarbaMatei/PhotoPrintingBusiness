---
unit: 001-background-jobs
intent: 009-background-jobs
phase: inception
status: ready
created: 2026-05-22T12:00:00Z
updated: 2026-05-22T12:00:00Z
default_bolt_type: ddd-construction-bolt
---

# Unit Brief: 001-background-jobs

## Purpose

Implement 4 scheduled background services to keep the platform clean, GDPR-compliant, and reliable without blocking the request pipeline.

## Scope

### In Scope
- `UploadCleanupJob` — hourly; soft-deletes orphaned uploads + deletes physical files
- `GuestSessionCleanupJob` — daily 02:00; purges expired guest sessions with no orders
- `AccountDeletionJob` — daily 03:00; hard-deletes users where `DeletionRequestedAt` < 30 days ago
- `EmailRetryJob` — continuous; processes `Channel<FailedEmail>` with exponential backoff (30 s, 2 min, 10 min, max 3 retries)
- Registration in `Program.cs` via `AddHostedService<T>`
- `FailedEmailQueue` singleton channel service
- Unit tests for each job's core logic

### Out of Scope
- Job scheduling dashboard
- Hangfire or Quartz.NET
- Distributed locking

---

## Domain Concepts

### Key Operations
| Job | Schedule | Action |
|-----|----------|--------|
| `UploadCleanupJob` | Every 1 h | Find `Upload` records with no `OrderItem` and `CreatedAt` < now-24h; soft-delete in DB; delete physical file via `IStorageService.DeleteAsync` |
| `GuestSessionCleanupJob` | Daily 02:00 | Find `GuestSession` records with `ExpiresAt` < now and no linked `Order`; hard-delete |
| `AccountDeletionJob` | Daily 03:00 | Find `User` records with `DeletionRequestedAt` < now-30days; hard-delete (EF cascade) |
| `EmailRetryJob` | Continuous | Dequeue from `Channel<FailedEmail>`; retry `IEmailService.SendAsync`; exponential backoff; max 3 attempts |

## Technical Constraints

- All jobs: `IHostedService` with `PeriodicTimer` or `CancellationToken`-aware loops
- Scoped dependencies (DbContext, services) obtained via `IServiceScopeFactory`
- Each job in its own `try/catch`; exception logged, job continues
- `FailedEmail` record: `{ string To, string Subject, string HtmlBody, int Attempts }`
- `EmailRetryJob` uses `System.Threading.Channels.Channel<FailedEmail>` (bounded, capacity 100)
- `IStorageService.DeleteAsync(string path)` — already defined in bolt 012

## Story Summary

| # | Story | Priority |
|---|-------|----------|
| 001 | `001-upload-guest-cleanup-jobs` | Must |
| 002 | `002-account-deletion-job` | Must |
| 003 | `003-email-retry-job` | Must |
