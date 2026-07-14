---
intent: 019-thumbnail-cache-and-cloud-storage
phase: inception
status: complete
created: 2026-05-25T10:30:00Z
updated: 2026-05-25T10:30:00Z
source: docs/architecture-analysis-2026-05-25.md#7
priority_score: 18
---

# Requirements: Thumbnail Cache & Cloud Storage

## Intent Overview

`GET /api/uploads/{id}/preview` decodes the full 50 MB source image and re-encodes a JPEG **on every request**, with no on-disk cache. Combined with `LocalStorageService` binding the API to a single VM, the upload preview path is the system's first scaling wall. This intent persists thumbnails after first generation and introduces an `S3StorageService` (or Azure Blob) for both uploads and thumbnails, enabling horizontal scaling and CDN delivery.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Eliminate repeated thumbnail CPU work | Second preview request for an upload returns cached bytes; 0 ImageSharp invocations | Must |
| Unblock horizontal API scaling | Storage no longer bound to single VM | Must |
| Reduce bandwidth + latency | Thumbnails served via CDN redirect (pre-signed URL) | Should |

---

## Functional Requirements

### FR-1: Persistent thumbnail cache
- **Description**: On first `GET /api/uploads/{id}/preview`, generate the thumbnail, persist it via `IStorageService.SaveAsync(stream, $"{uploadId}_thumb")`, and record `Upload.ThumbnailPath`. Later requests stream the cached blob directly.
- **Acceptance Criteria**:
  - First request decodes + encodes; subsequent requests do not.
  - `Cache-Control: public, max-age=2592000, immutable` set on thumbnails.
  - Thumbnail regenerated automatically if the cached file is missing (defensive).
- **Priority**: Must
- **Related Stories**: US-019-1, US-019-2

### FR-2: S3StorageService implementation
- **Description**: `S3StorageService : IStorageService` using `AWSSDK.S3` (works against AWS S3, MinIO, R2, etc.). Config switch: `Storage:Provider = Local | S3 | AzureBlob`. Local stays default.
- **Acceptance Criteria**:
  - `Save`, `Load`, `Delete`, `Exists`, `GetPresignedUrl` implemented.
  - Bucket configurable; key prefix `uploads/` or `thumbs/` partitioned.
  - All ops wrapped in retry with exponential backoff (Polly).
- **Priority**: Must
- **Related Stories**: US-019-3

### FR-3: 302 redirect to pre-signed URL
- **Description**: When `Storage:Provider != Local`, `GET /api/uploads/{id}/preview` returns `302` with a pre-signed URL valid for 1 hour, instead of proxying bytes.
- **Acceptance Criteria**:
  - Local provider keeps current proxy behaviour.
  - S3 provider returns 302; the URL is signed via SDK with 1 h TTL.
  - The endpoint still applies the same authorization check before issuing the redirect.
- **Priority**: Must
- **Related Stories**: US-019-4

### FR-4: One-shot migration tool for existing local files
- **Description**: A `dotnet run -- migrate-storage --source local --target s3` console command iterates existing `Uploads`, uploads file + thumbnail (if present) to S3, and updates `StoragePath` / `ThumbnailPath`.
- **Acceptance Criteria**:
  - Resumable: re-runs skip already-migrated rows.
  - Logs per-file outcome with correlation id.
  - Reports total uploaded MB and elapsed time.
- **Priority**: Should
- **Related Stories**: US-019-5

### FR-5: Schema additions
- **Description**: `Uploads.ThumbnailPath varchar(512) NULL` added.
- **Acceptance Criteria**: Migration applies cleanly on Postgres and SQLite.
- **Priority**: Must
- **Related Stories**: US-019-6

---

## Non-Functional Requirements

### Performance
| Requirement | Metric | Target |
|-------------|--------|--------|
| Cached preview latency | p95 | < 100 ms (S3) / < 50 ms (local) |
| Thumbnail generation | p95 on 50 MB JPEG | < 2 s |

### Reliability
| Requirement | Metric | Target |
|-------------|--------|--------|
| Storage availability | S3 SLA | 99.99% |
| Cache hit rate | After 24 h of operation | > 90% |

### Security
| Requirement | Standard | Notes |
|-------------|----------|-------|
| Pre-signed URL TTL | ≤ 1 h | Avoid permanent links |
| ImageSharp max pixels | `MAX_PIXELS` cap | Decompression-bomb defence (also catches gap #6 from analysis) |

---

## Constraints

### Technical Constraints
- Must keep `LocalStorageService` working for development; production opts in via config.
- ImageSharp `Configuration.Default.MaxImageWidth/Height` must be set; cover gap from analysis.

### Business Constraints
- Ship after intent 017 (deploy pipeline) — needs CI to verify both providers.

---

## Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| MinIO acceptable for self-host dev | Some devs prefer AWS sandbox | Document both |
| Bucket created out-of-band | Boot fails on missing bucket | Provider verifies bucket on startup; fail-fast |

---

## Open Questions

| Question | Owner | Due Date | Resolution |
|----------|-------|----------|------------|
| Q1: AWS S3 vs. Azure Blob vs. R2 default | Ops | 2026-07-01 | Pending — recommend R2 for cost, S3 for ecosystem |
| Q2: Should we generate multiple thumbnail sizes (small/medium/full)? | Frontend | 2026-07-01 | Pending — keep single 800px for now; revisit when responsive image work begins |
