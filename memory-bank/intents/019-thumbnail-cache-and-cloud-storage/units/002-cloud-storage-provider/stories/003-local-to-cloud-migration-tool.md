---
id: 003-local-to-cloud-migration-tool
unit: 002-cloud-storage-provider
intent: 019-thumbnail-cache-and-cloud-storage
status: draft
priority: should
created: 2026-05-25T10:30:00Z
assigned_bolt: 043-cloud-storage-provider
implemented: false
---

# Story: 003-local-to-cloud-migration-tool

## User Story

**As** the operator
**I want** a resumable command that copies all existing local files to the cloud bucket
**So that** the provider switchover is safe and incremental

## Acceptance Criteria

- [ ] `dotnet run --project src/PhotoPrint.API -- migrate-storage --source local --target s3 --dry-run` lists rows that would migrate.
- [ ] Without `--dry-run`, the command uploads each missing file + thumbnail and updates `StoragePath` / `ThumbnailPath` to the new key.
- [ ] Re-running skips already-migrated rows (idempotent on `StoragePath` prefix check).
- [ ] Per-file outcome logged at Information; errors logged at Error and counted.
- [ ] Summary at end: `migrated=N, skipped=M, failed=K, total_mb=...`.

## Technical Notes

- Use `IHost`/`IServiceProvider` from the existing API project; register a `MigrateStorageCommand` activated by a CLI verb.
- Concurrency: 4 parallel uploads via `Channel<Guid>` and 4 worker tasks.
- Stop on operator Ctrl+C; resume cleanly on next run.

## Dependencies

### Requires
- 001-s3-storage-service, 002-preview-redirect-presigned-url

### Enables
- Production cutover

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Source file missing on disk | Logged as Warning; row left as-is for manual investigation |
| Mid-run crash | Partial state safe (each row updated atomically after successful upload) |

## Out of Scope

- Mirror back from cloud → local (not needed).
