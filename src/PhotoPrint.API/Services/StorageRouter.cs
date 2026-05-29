using Microsoft.Extensions.DependencyInjection;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

/// <summary>
/// Resolves the keyed <see cref="IStorageService"/> registrations ("local" / "cloud")
/// and exposes a per-upload routing API (ADR-008).
/// </summary>
public class StorageRouter : IStorageRouter
{
    private readonly IStorageService? _cloud;

    public StorageRouter(IServiceProvider services)
    {
        Local = services.GetRequiredKeyedService<IStorageService>("local");
        _cloud = services.GetKeyedService<IStorageService>("cloud");
    }

    public IStorageService Local { get; }

    public bool CloudEnabled => _cloud is not null;

    public IStorageService Cloud => _cloud
        ?? throw new InvalidOperationException(
            "Cloud storage is not enabled. Set Storage:Provider=S3 and configure Bucket/AccessKey/SecretKey.");

    public IStorageService For(StorageLocation location) => location switch
    {
        StorageLocation.Local => Local,
        StorageLocation.Cloud => Cloud,
        _ => throw new ArgumentOutOfRangeException(nameof(location), location, "Unknown storage location."),
    };
}
