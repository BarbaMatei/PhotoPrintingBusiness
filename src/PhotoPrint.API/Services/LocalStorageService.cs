using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;

namespace PhotoPrint.API.Services;

/// <summary>
/// Local-disk <see cref="IStorageService"/> adapter. Always registered; used for
/// pre-payment (and dev) bytes. Does NOT support presigned URLs — the router + the
/// per-upload <c>StorageLocation</c> keep <see cref="GetPresignedUrlAsync"/> off this path.
/// </summary>
public class LocalStorageService : IStorageService
{
    private readonly string _basePath;
    private readonly ILogger<LocalStorageService> _logger;

    public LocalStorageService(IOptions<StorageSettings> settings, ILogger<LocalStorageService> logger)
    {
        _basePath = settings.Value.BasePath;
        _logger = logger;
        // Eager directory creation surfaces misconfig at boot. Tolerate failures here —
        // tests that resolve this service transitively (via IStorageRouter from a hosted
        // service) but never actually write would otherwise crash on factories that don't
        // override BasePath. SaveAsync re-creates the directory tree per-file, so a real
        // production misconfig still surfaces — just on first upload rather than at boot.
        try
        {
            Directory.CreateDirectory(_basePath);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _logger.LogWarning(ex,
                "LocalStorageService: could not pre-create BasePath {BasePath} at boot. " +
                "SaveAsync will retry per-file. Verify the deployment user has write access " +
                "if uploads should land here.", _basePath);
        }
    }

    public bool SupportsPresignedUrls => false;

    public async Task SaveAsync(Stream content, string key, CancellationToken ct = default)
    {
        StorageKeys.Validate(key);

        var fullPath = ResolveFullPath(key);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await using var fs = File.Create(fullPath);
        if (content.CanSeek)
            content.Position = 0;
        await content.CopyToAsync(fs, ct);

        _logger.LogDebug("Saved upload to {Key}", key);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        StorageKeys.Validate(key);
        var fullPath = ResolveFullPath(key);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogDebug("Deleted upload {Key}", key);
        }
        return Task.CompletedTask;
    }

    public Task<Stream> GetStreamAsync(string key, CancellationToken ct = default)
    {
        StorageKeys.Validate(key);
        var fullPath = ResolveFullPath(key);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Stored upload not found.", fullPath);

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        StorageKeys.Validate(key);
        return Task.FromResult(File.Exists(ResolveFullPath(key)));
    }

    public Task<string> GetPresignedUrlAsync(string key, TimeSpan ttl, CancellationToken ct = default)
        => throw new NotSupportedException(
            "LocalStorageService cannot produce presigned URLs. " +
            "Callers must check IStorageService.SupportsPresignedUrls or route via IStorageRouter " +
            "and branch on Upload.StorageLocation.");

    private string ResolveFullPath(string key)
    {
        // StorageKeys.Validate has already rejected absolute paths and '..' — but we
        // still re-anchor to _basePath here so any future caller using a key that
        // bypassed validation can't escape the storage root.
        var combined = Path.GetFullPath(Path.Combine(_basePath, key));
        var root = Path.GetFullPath(_basePath);
        if (!combined.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Resolved path escapes storage root.");
        return combined;
    }
}
