---
id: 043-cloud-storage-provider
unit: 002-cloud-storage-provider
intent: 019-thumbnail-cache-and-cloud-storage
type: ddd-construction-bolt
status: planned
stories:
  - 001-s3-storage-service
  - 002-preview-redirect-presigned-url
  - 003-local-to-cloud-migration-tool
created: 2026-05-25T10:30:00Z
started: null
completed: null
current_stage: null
stages_completed: []

requires_bolts: [042-thumbnail-cache]
enables_bolts: [046-distributed-state-redis]
requires_units: []
blocks: false

complexity:
  avg_complexity: 3
  avg_uncertainty: 3
  max_dependencies: 1
  testing_scope: 4
---

# Bolt: 043-cloud-storage-provider

## Overview

S3-compatible provider, pre-signed redirect, migration tool.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Domain Model | `IStorageService` contract; pre-signed URL semantics; key conventions |
| 2 | Technical Design | Provider switch wiring; bucket bootstrap; migration concurrency model |
| 3 | Implement | Service, controller change, migration command |
| 4 | Test | LocalStack/MinIO integration tests; dry-run + full migration smoke |

## Dependencies

- **Requires**: 042-thumbnail-cache (cache lives at portable keys).
- **Enables**: intent 021 (distributed state).

## Key Technical Notes

- Bucket bootstrap is one-shot ops; not handled in app boot.
- Cutover plan: deploy with `Storage:Provider=Local` first → run migration → swap to `Storage:Provider=S3` in a follow-up deploy.
