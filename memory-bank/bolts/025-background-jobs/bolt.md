---
id: 025-background-jobs
unit: 001-background-jobs
intent: 009-background-jobs
type: ddd-construction-bolt
status: complete
stories:
  - 001-upload-guest-cleanup-jobs
  - 002-account-deletion-job
  - 003-email-retry-job
created: 2026-05-22T12:00:00Z
started: 2026-05-24T12:00:00Z
completed: 2026-05-24T12:00:00Z
current_stage: null
stages_completed: [1, 2, 3, 4]

requires_bolts: [003-email-infrastructure, 012-photo-upload-backend, 023-account-api]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 3
  testing_scope: 2
---

# Bolt: 025-background-jobs

## Overview

Implement the 4 `IHostedService` background jobs: UploadCleanupJob (hourly), GuestSessionCleanupJob (daily), AccountDeletionJob (daily), and EmailRetryJob (continuous channel consumer).

## Objective

By the end of this bolt the platform automatically keeps the database and disk clean, deletes accounts on schedule, and retries failed emails without operator intervention.

## Stories Included

- **001-upload-guest-cleanup-jobs**: `UploadCleanupJob` (hourly) + `GuestSessionCleanupJob` (daily 02:00) (Must)
- **002-account-deletion-job**: `AccountDeletionJob` (daily 03:00) — hard-deletes accounts where `DeletionRequestedAt` < now-30days (Must)
- **003-email-retry-job**: `EmailRetryJob` — `Channel<FailedEmail>` consumer with exponential backoff (Must)

## Bolt Type

`ddd-construction-bolt` — backend jobs with scoped service dependencies, timer patterns, and a channel-based queue.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Domain Model | `ddd-01-domain-model.md` — job schedules, queries, `FailedEmail` record shape |
| 2 | Technical Design | `ddd-02-technical-design.md` — `IHostedService` patterns, `IServiceScopeFactory`, `PeriodicTimer`, channel |
| 3 | Implement | Code: 4 job classes, `FailedEmailQueue`, `Program.cs` registrations |
| 4 | Test | `ddd-03-test-report.md` — unit tests using InMemory DbContext |

## Dependencies

- **Requires**: bolt `003-email-infrastructure` (`IEmailService` — ✅ complete)
- **Requires**: bolt `012-photo-upload-backend` (`IStorageService.DeleteAsync` — ✅ complete)
- **Requires**: bolt `023-account-api` (`DeletionRequestedAt` on User entity)
- **Enables**: nothing (final phase 8 deliverable)
