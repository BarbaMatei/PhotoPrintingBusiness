---
intent: 013-upload-cleanup-fix
phase: inception
status: complete
created: 2026-05-25T10:00:00Z
updated: 2026-05-25T10:00:00Z
source: docs/architecture-analysis-2026-05-25.md#1
priority_score: 23
---

# Requirements: Upload Cleanup Fix

## Intent Overview

`UploadCleanupJob.CleanupAsync` deletes every `Upload` older than 24 h with `DeletedAt IS NULL` — **without checking for `CartItem` or `OrderItem` references**, despite an inline comment claiming otherwise. A customer who uploads photos, leaves overnight, then pays loses their source files before the operator can print them.

This intent corrects the cleanup query so that uploads referenced by an active cart or any order are retained, and introduces explicit retention windows for orphans versus referenced uploads.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Stop silent destruction of paid customers' source photos | Zero deletions of uploads referenced by `CartItem` or `OrderItem` in 30-day production audit | Must |
| Preserve referenced uploads long enough for reprints / customer service | Referenced uploads kept ≥ 365 days from `UploadedAt` (configurable) | Must |
| Keep storage costs predictable | Orphan uploads still cleaned after 24 h (configurable) | Should |

---

## Functional Requirements

### FR-1: Skip Referenced Uploads in Cleanup Query
- **Description**: Extend `UploadCleanupJob.CleanupAsync` candidate selection to exclude any upload referenced by an existing `CartItem` or `OrderItem`, regardless of cart/order status.
- **Acceptance Criteria**:
  - Given an upload `U` added to a `CartItem` 25 h ago, when cleanup ticks, then `U.DeletedAt` remains null and the file remains on disk.
  - Given an upload `U` referenced by an `OrderItem` (any order status), when cleanup ticks at 48 h, then `U` is retained.
  - Given an upload `U` never referenced and older than `OrphanRetentionHours`, when cleanup ticks, then `U` is soft-deleted and its file removed.
- **Priority**: Must
- **Related Stories**: US-013-1

### FR-2: Configurable Retention Windows
- **Description**: Introduce two configurable retention windows: `UploadCleanup:OrphanRetentionHours` (default 24) and `UploadCleanup:ReferencedRetentionDays` (default 365).
- **Acceptance Criteria**:
  - `appsettings.json` exposes both keys with the documented defaults.
  - Referenced uploads older than `ReferencedRetentionDays` are still eligible for cleanup (covers truly stale data after long retention).
  - Job logs the effective retention windows at startup.
- **Priority**: Must
- **Related Stories**: US-013-2

### FR-3: Integration Test Guarding Against Regression
- **Description**: Add an integration test that uploads a photo, adds it to a cart and an order item, advances the clock past `OrphanRetentionHours`, runs `UploadCleanupJob.CleanupAsync` once, then asserts the upload remains.
- **Acceptance Criteria**:
  - Test runs against the real EF Core test database (no mocks of `IUploadRepository`).
  - Test fails with the pre-fix query.
  - Test passes after applying FR-1.
  - Test also covers the orphan-deleted branch as a positive control.
- **Priority**: Must
- **Related Stories**: US-013-3

---

## Non-Functional Requirements

### Performance
| Requirement | Metric | Target |
|-------------|--------|--------|
| Cleanup tick duration | p95 wall time on 1 M Uploads | < 60 s |
| DB load | Cleanup query memory | Stream results / take in batches of 500 |

### Reliability
| Requirement | Metric | Target |
|-------------|--------|--------|
| Data preservation | False-positive deletions per month | 0 |
| Observability | Skipped-vs-deleted counts logged | Per tick, structured Serilog |

### Security
No new surface — internal background job; no API changes.

### Compliance
| Requirement | Standard | Notes |
|-------------|----------|-------|
| GDPR | Soft-delete + file removal honoured for orphans | Existing behaviour preserved |

---

## Constraints

### Technical Constraints
- Must use existing `UploadCleanupJob` `BackgroundService`; no scheduler dependency change.
- Must use existing `IStorageService` for physical file deletion.

### Business Constraints
- Must ship in the next quick-wins drop (no waiting for Sameday / VAT work).

---

## Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| Soft-deleted uploads never re-enter the active set | Resurrection bug could mask references | Query filters on `DeletedAt IS NULL` only |
| Cart and order tables are authoritative reference sources | A new table referencing `Uploads` would still over-delete | One-shot audit script + schema note in `decision-index.md` |
| Existing orphan files on disk acceptable to leave for one-shot reconcile | Disk space growth until reconciler runs | Schedule reconciler in same release window |

---

## Open Questions

| Question | Owner | Due Date | Resolution |
|----------|-------|----------|------------|
| Should we emit a metric (`upload_cleanup_skipped_total`) now or wait for intent 020 (observability)? | Backend | 2026-06-01 | Pending — recommend yes, via existing Serilog only for this intent |
