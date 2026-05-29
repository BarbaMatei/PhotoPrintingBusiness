---
unit: 002-cloud-storage-provider
bolt: 043-cloud-storage-provider
stage: model
status: complete
updated: 2026-05-27T12:10:00Z
---

# Static Model - Cloud Storage Provider

## Bounded Context

**File Storage** — the boundary that owns *where the bytes physically live* and *how a caller obtains them*, independent of any business meaning. Everything above this boundary (`UploadService`, `ImageProcessor`, controllers) deals only in opaque **storage keys** and byte streams; it never knows whether those bytes sit on a local disk or in an S3 bucket.

This bolt does **not** introduce new business domain — it deepens an existing infrastructure abstraction (`IStorageService`) so a second, cloud-backed implementation can be swapped in by configuration. The "domain" here is the storage contract itself plus one operational capability (migration). Where this unit appears thin on classic DDD constructs (few entities, no events), that is expected and correct for an infrastructure adapter.

## Domain Entities

| Entity | Properties | Business Rules |
|--------|------------|----------------|
| **Upload** *(existing aggregate root — touched, not introduced)* | `Id`, `FilePath` (original-file key), `ThumbnailPath` (cached-thumb key, nullable), `UserId?`, `GuestSessionId?`, `OriginalFileName`, `ContentType` | Exactly one owner (`UserId` XOR `GuestSessionId`). `FilePath`/`ThumbnailPath` hold **provider-agnostic keys**, never absolute paths or URLs. The migration tool may rewrite these keys but must do so atomically per row. *(Stories 001/003 call these "StoragePath"; the persisted property is `FilePath` — treated as synonyms here, reconciled in Stage 2.)* |

## Value Objects

| Value Object | Properties | Constraints |
|--------------|------------|-------------|
| **StorageKey** | the relative key string (e.g. `uploads/2026/05/{uploadId}.jpg`, `thumbs/{uploadId}.jpg`) | Portable across providers; no leading slash, no scheme, no host; forward-slash separated. The *same* key must resolve under Local (relative to base dir) and S3 (object key in bucket). Existing keys use `{ownerId}/{fileId:N}.{ext}`; new key conventions per story 001 are layered in during design. |
| **PresignedUrl** | `Url` (absolute, signed), `ExpiresAt` | Time-limited (TTL = 1 h for previews). Grants read access to exactly one object. Treated as a secret-bearing capability — `Cache-Control: private` so shared caches never serve one user's signed URL to another. |
| **StorageProvider** | discriminator: `Local` \| `S3` | Exactly one provider is active per deployment (config `Storage:Provider`). Drives both DI registration and the preview-endpoint branch (proxy bytes vs. 302 redirect). `S3` is the **protocol family**, not a vendor — the recommended concrete cloud target is **Cloudflare R2** (configured via `EndpointUrl` + `Region=auto` + `ForcePathStyle`). |
| **MigrationOutcome** | `Migrated`, `Skipped`, `Failed`, `TotalMb` (per-run counters) | Monotonic within a run. `Skipped` = already-migrated (idempotent prefix match); `Failed` = upload error (counted, run continues). |

## Aggregates

| Aggregate Root | Members | Invariants |
|----------------|---------|------------|
| **Upload** | `FilePath` (StorageKey), `ThumbnailPath` (StorageKey?) | A key persisted on an Upload must reference an object that the *currently configured* provider can resolve, OR the migration that rewrites it must update the row only **after** the object is confirmed written to the target. No partial cutover within a single row. |

## Domain Events

| Event | Trigger | Payload |
|-------|---------|---------|
| *(none)* | — | This unit introduces no domain events. Storage is an infrastructure adapter; persistence and previews are synchronous request/response. Migration emits **operational log records** (per-file Information, errors Error), not domain events — deliberately, to keep the migrator a standalone ops tool with no coupling to the message/event surface. |

## Domain Services

| Service | Operations | Dependencies |
|---------|------------|--------------|
| **IStorageService** *(extended contract)* | `SaveAsync(stream, ownerId, ext, ct, fileId?)` → key · `GetStreamAsync(key, ct)` → stream · `DeleteAsync(key, ct)` · `ExistsAsync(key, ct)` → bool · **`GetPresignedUrlAsync(key, ttl, ct)` → absolute URL** *(new)* | none (each impl wraps its own backend) |
| **LocalStorageService** *(existing impl)* | implements all of the above; for `GetPresignedUrlAsync` it has no signing backend → capability is **not supported** (design decides: throw `NotSupportedException` vs. never-called because controller branches on provider) | filesystem |
| **S3StorageService** *(new impl)* | implements all; streamed upload via `TransferUtility` (no full-buffer); `GetPresignedUrlAsync` via SDK pre-sign; transient-error resilience via Polly (5xx + throttling, exponential backoff). One impl serves AWS S3, **Cloudflare R2 (recommended)**, and MinIO — vendor differences are pure config. | `AWSSDK.S3`, `StorageSettings`, Polly |
| **StorageMigrator** *(deferred to bolt 050 — same unit)* | enumerate Upload rows → for each missing-at-target key: copy bytes source→target, then update key on the row; idempotent (skip rows already on target prefix); bounded concurrency (4 workers via `Channel<Guid>`); cancellation-aware (Ctrl+C stops cleanly, resumable next run) | `IStorageService` (both source & target), `DbContext`, `ILogger` |

## Repository Interfaces

| Repository | Entity | Methods |
|------------|--------|---------|
| **IStorageService** | object bytes keyed by `StorageKey` | Save / GetStream / Delete / Exists / GetPresignedUrl *(this IS the storage "repository" — the byte-persistence contract)* |
| **ApplicationDbContext / Uploads set** *(existing)* | Upload | the migrator reads `Uploads` (streamed/paged) and updates `FilePath`/`ThumbnailPath` per row |

## Ubiquitous Language

| Term | Definition |
|------|------------|
| **Provider** | The active storage backend for a deployment: `Local` (disk) or `S3` (object storage). Selected by `Storage:Provider`. |
| **Storage key** | Provider-agnostic relative path identifying an object. Stored on `Upload.FilePath` / `Upload.ThumbnailPath`. |
| **Bucket** | The S3 container that holds all objects for a deployment. Verified to exist at boot; the app does **not** create it (one-shot ops task). |
| **Pre-signed URL** | A time-limited, signed S3 URL granting direct read of one object, so bytes flow client↔object-storage without proxying through the API. |
| **Path-style / ForcePathStyle** | Addressing mode (`endpoint/bucket/key` rather than `bucket.endpoint/key`) required for MinIO/R2 compatibility; enabled when `EndpointUrl` is set. |
| **Endpoint URL** | Optional custom S3 endpoint; non-null ⇒ talking to MinIO/Cloudflare R2 instead of AWS. For R2: `https://<account-id>.r2.cloudflarestorage.com`. |
| **Cloudflare R2** | The **recommended** concrete cloud target. S3-compatible object storage with **zero egress fees** (decisive for an image-serving workload) and native Cloudflare-CDN/edge proximity to the Romanian audience. Region must be `auto`. |
| **Egress** | Data-transfer-out cost. Dominates the bill for photo serving (vs. storage cost). R2 charges $0 egress; AWS S3 charges per-GB out — the core reason R2 is recommended. |
| **Cutover** | The deployment switch from `Provider=Local` to `Provider=S3`, performed *after* migration has copied existing files. |
| **Migration (migrate-storage)** | The resumable command that copies all existing local objects into the bucket and repoints the DB keys. |
| **Dry-run** | Migration mode that reports what *would* move without writing anything. |
| **Idempotent (migration)** | Re-running skips rows already at the target (prefix check), so a crashed/interrupted run resumes safely. |
| **LocalStack / MinIO** | S3-compatible servers used as the integration-test backend in CI (no real AWS). |

## Story Coverage

- ✅ **001-s3-storage-service** → `IStorageService.GetPresignedUrlAsync` added; `S3StorageService`, `StorageProvider`, `StorageSettings`, bucket-verification invariant, Polly resilience captured.
- ✅ **002-preview-redirect-presigned-url** → `PresignedUrl` value object + `Provider` discriminator drive the proxy-vs-302 branch; authorization-before-redirect invariant noted.
- ➡️ **003-local-to-cloud-migration-tool** → **moved to bolt 050** (same unit). `StorageMigrator`/`MigrationOutcome` modeled here for unit-level completeness; implemented in 050.

## Open Questions for Technical Design (Stage 2)

1. **`GetPresignedUrlAsync` on Local** — throw `NotSupportedException`, or rely solely on the controller's provider branch so it's never invoked on Local? (Affects interface honesty vs. caller simplicity.)
2. **Key naming reconciliation** — story 001 proposes `uploads/{yyyy}/{mm}/{uploadId}{ext}` + `thumbs/{uploadId}.jpg`; existing code uses `{ownerId}/{fileId:N}.{ext}`. Keep existing scheme for in-place objects and only apply the new scheme on migration? Or normalize? (Affects migrator key-rewrite logic.)
3. **`StorageSettings` secrets** — `AccessKey`/`SecretKey` (R2 API token) must follow the secrets approach from **ADR-006** (env/secret store, never committed; `.env.example` placeholder only).
4. **Boot-time bucket verification failure** — fail-fast at startup (story 001) vs. degrade. Confirm fail-fast and the exact exception/log surface. Verify `auto` region works for the R2 head-bucket probe.
5. **Recommended target = Cloudflare R2** — design `StorageSettings` so R2 is a clean config (`EndpointUrl`, `Region=auto`, `ForcePathStyle=true`); document AWS S3 / MinIO as equally-supported via the same code path.
6. **Future: drop per-request signing on R2** — serving thumbnails from a public/custom-domain bucket fronted by Cloudflare's own access control avoids presigned-URL CDN churn (story 002 edge case). Capture as a **documented future option in Stage 2, not implemented in this bolt.**
