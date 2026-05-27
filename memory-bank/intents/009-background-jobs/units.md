---
intent: 009-background-jobs
phase: inception
status: complete
created: 2026-05-22T12:00:00Z
---

# Units: Background Jobs

## Decomposition

| Unit | Type | Stories | Default Bolt Type |
|------|------|---------|-------------------|
| 001-background-jobs | backend | US-803 (all 4 jobs) | ddd-construction-bolt |

## Rationale

All 4 jobs are `IHostedService` implementations in the same backend project. They share the same pattern (timer + scoped service + logging) and the same dependencies (`PhotoPrintDbContext`, `IEmailService`, `IStorageService`). A single backend unit captures all maintenance jobs. No frontend work required.
