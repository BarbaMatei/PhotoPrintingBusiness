---
unit: 001-test-project-drift-repair
intent: 023-test-project-drift-repair
created: 2026-05-25T11:55:00Z
last_updated: 2026-05-25T12:20:00Z
---

# Construction Log: 001-test-project-drift-repair

## Original Plan

**From Inception**: 1 bolt planned
**Planned Date**: 2026-05-25T11:45:00Z

| Bolt ID | Stories | Type |
|---------|---------|------|
| 049-test-project-drift-repair | 4 stories | simple-construction-bolt |

## Replanning History

| Date | Action | Change | Reason | Approved |
|------|--------|--------|--------|----------|

*(None.)*

## Current Bolt Structure

| Bolt ID | Stories | Status | Changed |
|---------|---------|--------|---------|
| 049-test-project-drift-repair | 4 | ✅ completed | - |

## Execution History

| Date | Bolt | Event | Details |
|------|------|-------|---------|
| 2026-05-25T11:55:00Z | 049 | started | Stage 1: Plan |
| 2026-05-25T12:00:00Z | 049 | stage-complete | Plan → Implement |
| 2026-05-25T12:10:00Z | 049 | stage-complete | Implement → Test |
| 2026-05-25T12:20:00Z | 049 | stage-complete | Test (433/433 passed) |
| 2026-05-25T12:20:00Z | 049 | completed | All 3 stages done |

## Execution Summary

| Metric | Value |
|--------|-------|
| Original bolts planned | 1 |
| Current bolt count | 1 |
| Bolts completed | 1 |
| Bolts in progress | 0 |
| Bolts remaining | 0 |
| Replanning events | 0 |

## Notes

- One in-scope concession during Stage 3 review: the user authorised an inline one-line fix to `UploadServiceTests.UploadAsync_GuestAtUploadCap_ThrowsTooManyRequestsException` (seed loop bumped from 30 to 100 to match production's `MaxUploadsPerSession = 100`). This was hidden drift the bolt-049 build fix unmasked. Documented in `test-walkthrough.md`.
- Two intentional behaviour changes in test infrastructure (both flagged in the implementation walkthrough):
  - `SeedCartItemAsync` helpers (unit + integration) now require and persist a real `SizeId`.
  - `CartFactory.SeedProductAsync` now hydrates the `product.Sizes` navigation collection so callers can read it without a separate query.
- Full suite: **433 / 433 passed**, 0 skipped, 0 failed.
- Bolt-033 isolation trick (`Directory.Build.targets`) is no longer needed — verified by running `dotnet test` with no filter and no exclusions.
