using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;

namespace PhotoPrint.API.Services;

public class LocalStorageService : IStorageService
{
    private readonly string _basePath;
    private readonly ILogger<LocalStorageService> _logger;

    public LocalStorageService(IOptions<StorageSettings> settings, ILogger<LocalStorageService> logger)
    {
        _basePath = settings.Value.BasePath;
        _logger = logger;
    }

    public async Task<string> SaveAsync(Stream stream, Guid ownerId, string extension, CancellationToken ct = default, Guid? fileId = null, string? prefix = null)
    {
        var relativeDir = prefix is null
            ? ownerId.ToString()
            : Path.Combine(prefix, ownerId.ToString());
        var ownerDir = Path.Combine(_basePath, relativeDir);
        Directory.CreateDirectory(ownerDir);

        var id = fileId ?? Guid.NewGuid();
        var fileName = $"{id:N}.{extension}";
        var fullPath = Path.Combine(ownerDir, fileName);

        await using var fs = File.Create(fullPath);
        stream.Position = 0;
        await stream.CopyToAsync(fs, ct);

        var relativePath = Path.Combine(relativeDir, fileName);
        _logger.LogDebug("Saved upload to {RelativePath}", relativePath);
        return relativePath;
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogDebug("Deleted upload {StoragePath}", storagePath);
        }
        return Task.CompletedTask;
    }

    public Task<Stream> GetStreamAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Stored upload not found.", fullPath);

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task<bool> ExistsAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        return Task.FromResult(File.Exists(fullPath));
    }
}
