namespace PhotoPrint.API.Services;

/// <summary>
/// Byte-persistence contract for upload storage adapters. Naming policy lives in
/// <see cref="StorageKeys"/> (ADR-007); the adapter persists bytes at a caller-supplied key.
/// Two implementations are wired by <see cref="IStorageRouter"/> per upload (ADR-008):
/// <see cref="LocalStorageService"/> (disk) and <see cref="S3StorageService"/> (cloud).
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Persists <paramref name="content"/> at the caller-supplied <paramref name="key"/>.
    /// The key is validated against <see cref="StorageKeys.Validate"/> by every adapter.
    /// </summary>
    Task SaveAsync(Stream content, string key, CancellationToken ct = default);

    /// <summary>Deletes the object at the given key. No-op if absent.</summary>
    Task DeleteAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Opens a read stream for a stored file. Callers must dispose. Every adapter throws
    /// <see cref="FileNotFoundException"/> when the key is absent (the S3 adapter translates its
    /// typed <c>NotFound</c> — F3, review 043-v1) so callers can catch one exception type across
    /// tiers to map a missing object to a 404.
    /// </summary>
    Task<Stream> GetStreamAsync(string key, CancellationToken ct = default);

    /// <summary>Returns true if an object exists at the given key.</summary>
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// True if this adapter can produce pre-signed URLs (cloud=true, local=false).
    /// Callers branch on this (or on <c>Upload.StorageLocation</c> via the router) to choose
    /// stream-vs-302 — they must never call <see cref="GetPresignedUrlAsync"/> when this is false.
    /// </summary>
    bool SupportsPresignedUrls { get; }

    /// <summary>
    /// Generates a time-limited URL granting direct read access to <paramref name="key"/>.
    /// Throws <see cref="NotSupportedException"/> on adapters where
    /// <see cref="SupportsPresignedUrls"/> is false.
    /// </summary>
    Task<string> GetPresignedUrlAsync(string key, TimeSpan ttl, CancellationToken ct = default);
}
