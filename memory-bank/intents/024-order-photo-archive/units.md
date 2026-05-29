---
intent: 024-order-photo-archive
phase: inception
status: draft
created: 2026-05-27T13:05:00Z
updated: 2026-05-27T13:05:00Z
---

# Units: Order Photo Archive & Lifecycle

Intent 024 decomposes into three units. The first two are backend (DDD); the third is
frontend (plus a thin read endpoint). All build on intent 019's `S3StorageService` and
bolt 043's `StorageLocation` flag + location-aware preview.

## Unit Decomposition

| Unit | Title | Type | Bolt | Stories | Depends on |
|------|-------|------|------|---------|------------|
| 001-order-photo-promotion | Promote paid-order photos to cloud | backend (ddd) | 051 | 4 | bolt 043 |
| 002-archive-retention | Purge originals + 12-month cleanup | backend (ddd) | 052 | 2 | bolt 051 |
| 003-order-history-photos | Account order-history photo viewing | frontend (simple) | 053 | 2 | bolt 051 |

## Dependency Flow

```text
043 (cloud adapter + StorageLocation + location-aware preview)
      │
      ▼
051 order-photo-promotion ──► 052 archive-retention
      │
      └──────────────────────► 053 order-history-photos
```

## Notes

- **Backfill (FR-7)** is a CLI verb on the promoter → folded into unit 001 / bolt 051 (story 004), not a separate bolt.
- This **supersedes** intent 019's story `003-local-to-cloud-migration-tool` (bolt 050): "migrate all local files" is invalid under the two-tier model. Bolt 050 is retired.
