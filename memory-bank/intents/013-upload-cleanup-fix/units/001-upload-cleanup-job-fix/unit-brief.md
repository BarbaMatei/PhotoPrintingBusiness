---
unit: 001-upload-cleanup-job-fix
intent: 013-upload-cleanup-fix
phase: inception
status: draft
created: 2026-05-25T10:00:00Z
updated: 2026-05-25T10:00:00Z
---

# Unit Brief: Upload Cleanup Job Fix

## Purpose

Repair the `UploadCleanupJob.CleanupAsync` LINQ query so that it never soft-deletes an upload referenced by `CartItem` or `OrderItem`. Add configurable retention windows and a regression-proof integration test.

## Scope

### In Scope
- `src/PhotoPrint.API/BackgroundJobs/UploadCleanupJob.cs` — query change + retention config wiring
- `src/PhotoPrint.API/Configuration/UploadCleanupSettings.cs` — new options class
- `appsettings.json` + `appsettings.Development.json` — new section
- Integration test in `src/PhotoPrint.Tests/Integration/BackgroundJobs/UploadCleanupJobTests.cs`

### Out of Scope
- New table or schema migration
- API or UI surface change
- Reconciler script for already-orphaned disk files (separate one-shot ops task)

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-1 | Skip referenced uploads in cleanup query | Must |
| FR-2 | Configurable retention windows | Must |
| FR-3 | Integration test guarding against regression | Must |

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes |
|--------|-------------|------------|
| Upload | Customer-uploaded source photo | Id, UploadedAt, DeletedAt, StoragePath |
| UploadCleanupSettings | Configuration object | OrphanRetentionHours (int), ReferencedRetentionDays (int) |

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| SelectCandidates | Choose uploads eligible for cleanup | now, OrphanRetentionHours, ReferencedRetentionDays | IReadOnlyList<Upload> |
| CleanupAsync | Soft-delete + remove file for each candidate | candidates | count deleted, count skipped (logged) |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 3 |
| Must Have | 3 |
| Should Have | 0 |
| Could Have | 0 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-skip-referenced-uploads | Cleanup query excludes cart/order-referenced uploads | Must | Planned |
| 002-retention-config | UploadCleanupSettings options class with two windows | Must | Planned |
| 003-cleanup-regression-test | Integration test: referenced upload survives cleanup | Must | Planned |

---

## Dependencies

### Depends On
- None (touches code already on `main`)

### Depended By
- None
