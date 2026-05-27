---
intent: 013-upload-cleanup-fix
phase: inception
status: units-decomposed
created: 2026-05-25T10:00:00Z
updated: 2026-05-25T10:00:00Z
---

# Units: Upload Cleanup Fix

## Decomposition

| Unit | Type | Stories | Default Bolt Type |
|------|------|---------|-------------------|
| 001-upload-cleanup-job-fix | backend | US-013-1, US-013-2, US-013-3 | simple-construction-bolt |

## Rationale

The fix is contained to a single class (`UploadCleanupJob`) plus configuration and one integration test. No new entities, no API changes, no UI work. A single backend unit at simple-construction-bolt complexity is correct.

## Unit Dependency Graph

```text
[001-upload-cleanup-job-fix]
```

## Execution Order

1. Day 1: Implement query change + config keys + tests (single bolt).
