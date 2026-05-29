---
id: 001-s3-storage-service
unit: 002-cloud-storage-provider
intent: 019-thumbnail-cache-and-cloud-storage
status: complete
priority: must
created: 2026-05-25T10:30:00Z
completed: 2026-05-29T08:30:00Z
assigned_bolt: 043-cloud-storage-provider
implemented: true
---

# Story: 001-s3-storage-service

## User Story

**As** the platform
**I want** an S3-compatible storage backend behind `IStorageService`
**So that** API replicas can serve files from shared object storage

## Acceptance Criteria

- [ ] `S3StorageService : IStorageService` implementing `SaveAsync`, `LoadAsync`, `DeleteAsync`, `ExistsAsync`, `GetPresignedUrlAsync(path, ttl)`.
- [ ] `StorageSettings` exposes `Provider`, `Bucket`, `Region`, `AccessKey`, `SecretKey`, `EndpointUrl` (for MinIO/R2).
- [ ] `Provider == "S3"` registers `S3StorageService` over `LocalStorageService`.
- [ ] Polly retry policy on transient (5xx, throttling) errors with exponential backoff.
- [ ] Bucket existence verified at startup; boot fails fast if absent.
- [ ] Integration test using LocalStack or MinIO Docker service container.

## Technical Notes

- `AWSSDK.S3` NuGet.
- `EndpointUrl` non-null → use `AmazonS3Config.ServiceURL` + `ForcePathStyle = true` (MinIO/R2 compatibility).
- Keys: `uploads/{yyyy}/{mm}/{uploadId}{ext}` and `thumbs/{uploadId}.jpg`.

## Dependencies

### Requires
- intent 017 deploy pipeline (CI runs MinIO service)

### Enables
- 002-preview-redirect-presigned-url, 003-local-to-cloud-migration-tool

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Credential rotation | Reload settings on `IOptionsMonitor` change; no restart needed |
| Region mismatch | SDK throws; bucket-verification step catches it at boot |
| Very large object | `TransferUtility` streamed upload (no in-memory buffer) |

## Out of Scope

- Cross-region replication.
