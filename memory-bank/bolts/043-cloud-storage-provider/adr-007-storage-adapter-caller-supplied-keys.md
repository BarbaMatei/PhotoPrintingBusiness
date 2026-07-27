---
bolt: 043-cloud-storage-provider
created: 2026-05-28T08:10:00Z
status: accepted
superseded_by: null
---

# ADR-007: Storage Adapter Persists Bytes at Caller-Supplied Keys (Naming is an Application Concern)

## Context

`IStorageService.SaveAsync` historically invented the storage key itself:

```csharp
Task<string> SaveAsync(Stream content, Guid ownerId, string ext, CancellationToken ct, Guid? fileId = null)
    // returns $"{ownerId}/{fileId:N}{ext}"
```

Two forces collided when planning the cloud storage work:

1. **Key policy is no longer adapter-local.** The Option-2 key scheme (`uploads/{yyyy}/{mm}/{uploadId}{ext}`, `thumbs/{uploadId}.jpg`) partitions originals by upload month. The relevant date is the upload's own `CreatedAt` — known to the caller, not the adapter. The intent-024 backfill must reproduce date-correct keys for orders paid weeks/months ago; an adapter computing the year/month from `UtcNow` would silently rewrite them.
2. **Bolt 049 just stabilized the 5-arg `SaveAsync` Moq setup** across `UploadServiceTests`. Any contract change re-touches those setups.

We had to decide whether to bolt extra parameters onto the existing signature (passing the date, kind, etc.) or to invert the contract.

## Decision

**`IStorageService.SaveAsync` becomes `Task SaveAsync(Stream content, string key, CancellationToken ct)`. The storage adapter persists bytes at a caller-supplied key. Key generation moves to an application-layer helper, `StorageKeys`, owned by domain/application code.**

```csharp
public static class StorageKeys
{
    public static string Original(Guid uploadId, DateTimeOffset createdAt, string ext)
        => $"uploads/{createdAt:yyyy}/{createdAt:MM}/{uploadId:N}{ext}";
    public static string Thumbnail(Guid uploadId)
        => $"thumbs/{uploadId:N}.jpg";
    // previews/{uploadId:N}.jpg added in bolt 051
}
```

The principle: **the storage adapter does byte persistence; key/naming policy is a domain/application concern.** All adapters (`LocalStorageService`, `S3StorageService`, `FakeStorageService`) implement only the byte-persistence side.

## Rationale

Naming policy intertwines with business concepts the adapter has no business knowing — the upload's own creation date, the *kind* of asset (original vs thumbnail vs preview), the partitioning scheme that pays off for CDN cache rules. Keeping that policy in one application-layer helper makes it:

- **Reproducible** by any caller (the intent-024 backfill needs the same keys as live promotion).
- **Auditable** in one place (one file owns naming, not three adapter implementations).
- **Independent** of which adapter is wired in — exactly the point of a port.

The cost is real but mechanical: re-touch the bolt-049-repaired `UploadServiceTests` Moq setups and the two call sites in `UploadService`. We pay it once.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|-------------|------|------|--------------|
| Caller-supplied key + `StorageKeys` (chosen) | Naming reproducible from the row's own data; adapters stay dumb/portable; intent-024 backfill becomes trivial | Re-touches the bolt-049 Moq fixtures; minor refactor at two call sites | **Accepted** |
| Keep the adapter-generates-key contract; add `kind` + `date` params | Smaller diff right now | Adapter still owns naming policy (purity loss); the param list keeps growing (kind, date, …); the date passed to the adapter would still need to be the upload's `CreatedAt`, not `UtcNow`, which is awkward to enforce | Rejected — the diff is small *now* but the design rot compounds |
| Two separate methods (`SaveOriginalAsync`, `SaveThumbnailAsync`, …) | Adapter can encode kind | Combinatorial explosion as new kinds appear (intent 024's `previews/`); still embeds policy in the adapter | Rejected — doesn't scale |

## Consequences

### Positive

- One source of truth for storage naming (`StorageKeys`).
- Adapters become trivially portable: `LocalStorageService`, `S3StorageService`, and `FakeStorageService` differ only in *how* they persist bytes.
- Intent-024's promoter and backfill can recompute any key from the row's `CreatedAt` — no adapter coupling.
- Future kinds (e.g. `previews/`) extend one helper, not every adapter.

### Negative

- Re-touches the `UploadServiceTests` Moq setups that bolt 049 just repaired — mechanical churn at the test layer.
- Two call sites in `UploadService` change (the original-save in `UploadAsync` and the bolt-042 thumbnail persist in the preview path).

### Risks

- **Risk**: a caller could pass a non-conforming key (e.g. a leading slash or a `..` traversal). **Mitigation**: route all production callers through `StorageKeys`; add a defensive normalization/validation in `LocalStorageService` (no `..`, no leading separator).

## Related

- **Stories**: 001-s3-storage-service, 002-preview-redirect-presigned-url; intent 024 stories 003-promote-on-paid + 004-backfill-paid-orders depend on this principle.
- **Standards**: should be referenced under coding-standards (storage adapter rules).
- **Previous ADRs**: complements ADR-002 (Fluent API for persistence config) by clarifying which side owns key naming.
