---
unit: 002-cloud-storage-provider
intent: 019-thumbnail-cache-and-cloud-storage
phase: inception
status: draft
created: 2026-05-25T10:30:00Z
updated: 2026-05-25T10:30:00Z
---

# Unit Brief: Cloud Storage Provider

## Purpose

Add `S3StorageService` (with MinIO/R2 compatibility), switch `IStorageService` via config, return 302 to pre-signed URLs from the preview endpoint, and ship a resumable migration tool for existing local files.

## Scope

### In Scope
- `S3StorageService` (`AWSSDK.S3`)
- Provider switch in `Program.cs`
- 302 redirect in `UploadsController.GetPreview` when non-Local
- Console migration `dotnet run -- migrate-storage --source local --target s3`

### Out of Scope
- Multi-cloud writes (single provider active at any time)
- Lifecycle rules / S3 IA tiering

---

## Story Summary

| Story ID | Title | Priority |
|----------|-------|----------|
| 001-s3-storage-service | Implementation against AWS SDK | Must |
| 002-preview-redirect-presigned-url | 302 redirect on cloud provider | Must |
| 003-local-to-cloud-migration-tool | Resumable bulk migration command | Should |
