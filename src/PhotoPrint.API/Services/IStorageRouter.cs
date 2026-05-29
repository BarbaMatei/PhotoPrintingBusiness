using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

/// <summary>
/// Resolves which <see cref="IStorageService"/> owns a given upload's bytes (ADR-008).
/// The local adapter is always available; the cloud adapter is present only when
/// <c>Storage:Provider == "S3"</c>.
/// </summary>
public interface IStorageRouter
{
    /// <summary>The local-disk adapter — always registered, even when cloud is disabled.</summary>
    IStorageService Local { get; }

    /// <summary>
    /// True when the cloud tier is configured (<c>Storage:Provider == "S3"</c>) and an
    /// S3-compatible adapter has been registered.
    /// </summary>
    bool CloudEnabled { get; }

    /// <summary>The cloud adapter. Throws <see cref="InvalidOperationException"/> if !<see cref="CloudEnabled"/>.</summary>
    IStorageService Cloud { get; }

    /// <summary>Returns the adapter owning bytes for an upload in the given location.</summary>
    IStorageService For(StorageLocation location);
}
