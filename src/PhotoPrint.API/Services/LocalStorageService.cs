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

    public async Task<string> SaveAsync(Stream stream, Guid ownerId, string extension, CancellationToken ct = default, Guid? fileId = null)
    {
        var ownerDir = Path.Combine(_basePath, ownerId.ToString());
        Directory.CreateDirectory(ownerDir);

        var id = fileId ?? Guid.NewGuid();
        var fileName = $"{id:N}.{extension}";
        var fullPath = Path.Combine(ownerDir, fileName);

        await using var fs = File.Create(fullPath);
        stream.Position = 0;
        await stream.CopyToAsync(fs, ct);

        var relativePath = Path.Combine(ownerId.ToString(), fileName);
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
}
