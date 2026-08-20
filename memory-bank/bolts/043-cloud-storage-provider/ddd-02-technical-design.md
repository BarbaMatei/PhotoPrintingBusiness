---
unit: 002-cloud-storage-provider
bolt: 043-cloud-storage-provider
stage: design
status: complete
updated: 2026-05-27T13:20:00Z
revision: 2 (two-tier model — intent 024)
---

# Technical Design - Cloud Storage Provider (stories 001 + 002)

> **Revision 2.** The "promote-on-payment" decision (intent 024) replaces the original *single-provider-per-deployment* design with a **two-tier** model. This bolt builds: the `S3StorageService` adapter, the `StorageKeys` helper, the `Upload.StorageLocation` flag, a **per-upload storage router**, and a **location-aware preview**. The promotion that actually moves files to cloud is intent 024 (bolt 051); the migration tool (old story 003 / bolt 050) is **retired**.
>
> Scope here = story 001 (S3 adapter + wiring) and story 002 (preview), re-interpreted for two tiers.

## Architecture Pattern

**Ports & Adapters (Hexagonal) + a two-tier router.** `IStorageService` is the port; `LocalStorageService` and `S3StorageService` are adapters. Unlike rev 1, **both adapters can be active at once**: every upload starts on the **local** tier; paid-order uploads are later promoted to the **cloud** tier (intent 024). A new `IStorageRouter` resolves, per upload, which adapter owns its bytes — driven by `Upload.StorageLocation`.

Naming policy stays in the application layer (`StorageKeys`), and `SaveAsync` takes a caller-supplied key (rev-1 decision, still in force).

## Layer Structure

```text
┌─────────────────────────────────────────────────────────┐
│ Presentation  UploadsController.GetPreview               │  per-upload: stream(local) | 302(cloud)
├─────────────────────────────────────────────────────────┤
│ Application   UploadService (authz + ensure-thumbnail)   │
│               StorageKeys (uploads/ thumbs/ [previews/])  │
│               IStorageRouter (location → adapter)         │
├─────────────────────────────────────────────────────────┤
│ Domain        IStorageService (port) + SupportsPresigned │
│               Upload.StorageLocation (Local|Cloud)        │
├─────────────────────────────────────────────────────────┤
│ Infrastructure LocalStorageService (always registered)   │
│                S3StorageService (registered iff S3 cfg'd) │
│                S3BucketVerifier (IHostedService, fail-fast)│
└─────────────────────────────────────────────────────────┘
```

## Two-tier routing (new)

- **`Upload.StorageLocation`** enum column (`Local` default | `Cloud`) — added by this bolt's migration. Tells the router where an upload's bytes live.
- **`IStorageRouter`**:
  ```text
  IStorageService For(StorageLocation loc)   // Local -> local adapter; Cloud -> cloud adapter
  IStorageService Local { get; }
  bool CloudEnabled { get; }                 // true when Storage:Provider == "S3"
  IStorageService Cloud { get; }             // throws if !CloudEnabled
  ```
- **Adapter registration** (composition root), gated by `Storage:Provider`:
  - Always: `AddKeyedSingleton<IStorageService, LocalStorageService>("local")`.
  - When `Provider == "S3"`: also `AddKeyedSingleton<IStorageService, S3StorageService>("cloud")` + `IAmazonS3` factory + `AddHostedService<S3BucketVerifier>()`.
  - `IStorageRouter` is registered as a singleton resolving the keyed adapters.
- **`Storage:Provider` is repurposed**: it no longer picks *one* store — it means *"is the cloud tier available."* `Local` (dev default) ⇒ cloud disabled, every upload stays `Local`, promotion is a no-op. `S3` (prod) ⇒ cloud tier wired, promotion (intent 024) can run.

## `IStorageService` — contract changes

| Member | Before | After | Why |
|--------|--------|-------|-----|
| `SaveAsync` | `Task<string> SaveAsync(Stream, Guid ownerId, string ext, ct, Guid? fileId)` | `Task SaveAsync(Stream content, string key, ct)` | naming → application concern; lets promoter/backfill reproduce date-correct keys |
| `GetPresignedUrlAsync` | — | `Task<string> GetPresignedUrlAsync(string key, TimeSpan ttl, ct)` *(new)* | story 001/002 |
| `SupportsPresignedUrls` | — | `bool SupportsPresignedUrls { get; }` *(new)* | local=false / cloud=true; keeps presign off the local path |
| `GetStreamAsync`/`DeleteAsync`/`ExistsAsync` | key-based | unchanged | already key-based |

- `LocalStorageService.SupportsPresignedUrls = false`; `GetPresignedUrlAsync` throws `NotSupportedException` (never reached — router + flag keep it off the local path).
- `S3StorageService.SupportsPresignedUrls = true`.
- **Blast radius of `SaveAsync`** (checkpoint item, unchanged from rev 1): both adapters, `FakeStorageService`, the two `UploadService` call-sites, and the `UploadServiceTests` Moq setups that **bolt 049 repaired**. Mechanical; strong ADR candidate.

## Key Scheme (Option 2)

`StorageKeys` static helper owns naming (this bolt adds `Original` + `Thumbnail`; `Preview` is added in bolt 051):

```text
StorageKeys.Original(Guid uploadId, DateTimeOffset createdAt, string ext)
    => $"uploads/{createdAt:yyyy}/{createdAt:MM}/{uploadId:N}{ext}"
StorageKeys.Thumbnail(Guid uploadId)
    => $"thumbs/{uploadId:N}.jpg"
// previews/{uploadId:N}.jpg  -> added by bolt 051
```

Keyed by `uploadId`; no `ownerId` in the path (authorization is enforced at the API layer; keys are opaque UUIDs). The **same key works on either tier** — local relative path or cloud object key — so promotion (051) reuses the key unchanged.

## API Design

| Endpoint | Method | Request | Response |
|----------|--------|---------|----------|
| `/api/uploads/{id}/preview` | GET | route `id`; Bearer JWT **or** `X-Guest-Token` | **`StorageLocation == Local`**: `200` `image/jpeg` + `Cache-Control: public, max-age=2592000, immutable` *(unchanged from 042)*. **`StorageLocation == Cloud`**: `302 Found`, `Location: <presigned URL, 1 h>`, `Cache-Control: private, max-age=3600`. `403`/`404` per existing rules. |

**Preview flow (per-upload):**

```text
UploadsController.GetPreview(id):
  (upload, key) = await _uploadService.GetPreviewThumbnailKeyAsync(id, caller, ct)  // authz + ensure-thumbnail
  store = _router.For(upload.StorageLocation)
  if (upload.StorageLocation == Cloud):                       // cloud tier
      url = await store.GetPresignedUrlAsync(key, 1h, ct)
      Response.Headers.CacheControl = "private, max-age=3600"
      return Redirect(url)                                    // 302
  else:                                                       // local tier (incl. all dev)
      stream = await store.GetStreamAsync(key, ct)
      Response.Headers.CacheControl = "public, max-age=2592000, immutable"
      return File(stream, "image/jpeg")
```

- `GetPreviewThumbnailKeyAsync` (in `UploadService`) does **authorization first**, then the bolt-042 ensure-thumbnail-exists logic against the upload's *current tier* (router-resolved), and returns `(upload, thumbKey)`. Authz-before-presign is structurally guaranteed.
- For a `Local` upload the thumbnail is generated + stored locally (exactly bolt 042). The cloud branch only fires once an upload has been promoted (intent 024) — fully testable here by seeding a `Cloud` upload + a MinIO object.

## Data Persistence

| Table | Column | Notes |
|-------|--------|-------|
| Uploads | `StorageLocation` (int/enum, default `0 = Local`, NOT NULL) | added by this bolt's migration; consumed by router + preview |

Migration authored Npgsql-typed (snapshot remains Npgsql-typed — see DEPLOYMENT.md §7 follow-up); valid on Postgres. `FilePath`/`ThumbnailPath` already exist (bolt 042); `LargePreviewPath` is added later by bolt 051.

## `StorageSettings` (bound from `Storage:`)

| Field | Default | Notes |
|-------|---------|-------|
| `Provider` | `Local` | `Local` (cloud tier off) \| `S3` (cloud tier on) |
| `Bucket` | — | required when `S3` |
| `Region` | `auto` | `auto` for R2; real region for AWS |
| `EndpointUrl` | null | set for R2/MinIO; null = AWS |
| `ForcePathStyle` | `false` | `true` for R2/MinIO |
| `AccessKey`/`SecretKey` | null | **ADR-006**: secret store / user-secrets / env; `.env.example` placeholders only |
| `PresignTtlMinutes` | `60` | preview presigned-URL lifetime |

`IValidateOptions<StorageSettings>` + `ValidateOnStart()`: when `Provider == "S3"`, `Bucket`/`AccessKey`/`SecretKey` required → else app fails to start. Credential rotation = restart baseline (IOptionsMonitor hot-reload deferred).

`IAmazonS3` factory (only built when `Provider == "S3"`): R2 ⇒ `Region="auto"`, `ServiceURL=EndpointUrl`, `ForcePathStyle=true`.

## `S3StorageService` — operation mapping

| Method | AWS SDK call | Notes |
|--------|-------------|-------|
| `SaveAsync(stream, key)` | `TransferUtility.UploadAsync` | streamed/multipart |
| `GetStreamAsync(key)` | `GetObjectAsync` → `ResponseStream` | |
| `DeleteAsync(key)` | `DeleteObjectAsync` | idempotent |
| `ExistsAsync(key)` | `GetObjectMetadataAsync` (HEAD) | 404 → false |
| `GetPresignedUrlAsync(key,ttl)` | `GetPreSignedURL(GET, UtcNow+ttl)` | wrapped in `Task.FromResult` |

All S3 ops wrapped in a **Polly** resilience pipeline (retry transient `AmazonS3Exception` HTTP ≥ 500 / throttling, exp. backoff + jitter, 3 attempts). Persistent failure → `502`.

## NFR / Security / Error Handling

| Area | Approach |
|------|----------|
| Performance/cost | R2 zero egress + `302` for cloud uploads → bytes bypass the API. `thumbs/` = one edge-cache rule. |
| Scalability | cloud tier is shared object storage → multiple replicas serve promoted uploads (basis for bolt 046). Pre-payment local uploads = single-node (acceptable now). |
| Reliability | Polly retries; `S3BucketVerifier` fail-fast at boot (only when `Provider=S3`). |
| Auth | dual auth unchanged; owner/admin check before any presign (`403`/`404` else). |
| Presigned URL | `Cache-Control: private`, 1 h TTL. |
| Credentials | ADR-006. |
| Persistent S3 failure | `502` (`BadGatewayException`). |
| Misconfigured S3 | `ValidateOnStart` fail-fast. |
| Decomp-bomb / oversize | `422`, unchanged from bolt 042. |

## External Dependencies

| Service | Purpose | Integration |
|---------|---------|-------------|
| Cloudflare R2 *(recommended)* / AWS S3 / MinIO | cloud tier | `AWSSDK.S3` |
| MinIO (CI) | real S3 backend for integration tests | Docker **service container** in `ci.yml`; `[SkippableFact]` when endpoint unset |

**New NuGet**: `AWSSDK.S3`. (Polly already present.)

## Test Plan (preview of Stage 5)

- **MinIO service in `ci.yml`**; integration tests gated to skip locally.
- Adapter integration: `SaveAsync`→`ExistsAsync`; `GetStreamAsync` round-trip; `DeleteAsync`; `GetPresignedUrlAsync` URL fetches `200`; `ForcePathStyle`/`Region=auto`.
- **Router**: `For(Local)`→local, `For(Cloud)`→cloud; `Cloud` throws when `!CloudEnabled`.
- **Location-aware preview**: seed `Local` upload → `200` stream + immutable cache (unchanged); seed `Cloud` upload (+ MinIO thumb) → `302` to endpoint with signature + `private, max-age=3600`; unauthorized → `403` (never a URL).
- Unit: `LocalStorageService.GetPresignedUrlAsync` throws `NotSupportedException`.

## Decisions to confirm at the Stage 2 checkpoint

1. **`SaveAsync` → caller-supplied key** + `StorageKeys` (re-touches bolt-049 tests). *Recommend yes — strong ADR candidate.*
2. **`IStorageRouter` + keyed DI** for per-upload tier resolution; `Storage:Provider` repurposed to "cloud tier on/off." *Recommend yes.*
3. **`Upload.StorageLocation` migration in this bolt** (consumed by intent-024 promotion). *Recommend yes — the preview needs it.*
4. **Credential rotation = restart**; **explicit Polly**. *Recommend yes.*
