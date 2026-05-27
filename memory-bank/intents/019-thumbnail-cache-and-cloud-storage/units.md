---
intent: 019-thumbnail-cache-and-cloud-storage
phase: inception
status: units-decomposed
created: 2026-05-25T10:30:00Z
updated: 2026-05-25T10:30:00Z
---

# Units: Thumbnail Cache & Cloud Storage

## Decomposition

| Unit | Type | Stories | Default Bolt Type |
|------|------|---------|-------------------|
| 001-thumbnail-cache | backend | US-019-1, US-019-2, US-019-6 | simple-construction-bolt |
| 002-cloud-storage-provider | backend / ops | US-019-3, US-019-4, US-019-5 | ddd-construction-bolt |

## Rationale

Thumbnail caching is a stand-alone perf win that lands without touching the storage backend. Cloud storage + redirect + migration is a separate, larger workstream that depends on the cache layer's persistence path.

## Unit Dependency Graph

```text
[001-thumbnail-cache] ──> [002-cloud-storage-provider]
```

## Execution Order

1. Days 1–2: 001-thumbnail-cache (file-system cache + schema).
2. Days 3–7: 002-cloud-storage-provider (S3 + redirect + migration).
