---
id: 033-upload-cleanup-fix
unit: 001-upload-cleanup-job-fix
intent: 013-upload-cleanup-fix
type: simple-construction-bolt
status: complete
stories:
  - 001-skip-referenced-uploads
  - 002-retention-config
  - 003-cleanup-regression-test
created: 2026-05-25T10:00:00Z
started: 2026-05-25T11:00:00Z
completed: 2026-05-25T11:35:00Z
current_stage: null
stages_completed:
  - name: plan
    completed: 2026-05-25T11:10:00Z
    artifact: implementation-plan.md
  - name: implement
    completed: 2026-05-25T11:20:00Z
    artifact: implementation-walkthrough.md
  - name: test
    completed: 2026-05-25T11:35:00Z
    artifact: test-walkthrough.md

requires_bolts: [025-background-jobs]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 2
---

# Bolt: 033-upload-cleanup-fix

## Overview

Single-file backend fix plus configuration and regression test. Restores the documented behaviour of `UploadCleanupJob`: never destroy uploads that a cart or order still points at.

## Objective

By the end of this bolt, `UploadCleanupJob.CleanupAsync` skips every upload referenced by `CartItem` or `OrderItem`, retains them up to a configurable long window, and an integration test fails without the fix.

## Stories Included

- **001-skip-referenced-uploads**: Add reference-filter sub-queries to the candidates LINQ expression and process in batches of 500 (Must).
- **002-retention-config**: `UploadCleanupSettings` options class with two windows, `ValidateOnStart` wiring, startup log line (Must).
- **003-cleanup-regression-test**: Three-case integration test against real DB + temp file storage, with `FakeClock` (Must).

## Bolt Type

`simple-construction-bolt` — narrow surface, well-understood query change, exhaustive test.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Plan | `implementation-plan.md` — exact LINQ shape, options binding, test layout, `FakeClock` wiring |
| 2 | Implement | Patches to `UploadCleanupJob`, new `UploadCleanupSettings`, `appsettings*.json` additions |
| 3 | Test | Three integration tests + run full suite locally |

## Dependencies

- **Requires**: bolt `025-background-jobs` (the job itself must exist — ✅ complete).
- **Enables**: nothing.

## Key Technical Notes

- Reuse `IClock` abstraction already present in `BackgroundJobs/`.
- Logging via existing `ILogger<UploadCleanupJob>` — no new sink.
- Bound batch size of 500 to keep memory predictable on hosts with millions of `Uploads`.
- Coordinate one-shot reconcile script with ops in a follow-up ticket; do **not** widen this bolt's scope.
