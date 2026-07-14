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
        var id = fileId ?? Guid.NewGuid();
        var fileName = $"{id:N}.{extension}";

        // Storage keys always use '/' so they're OS-independent (NEW-4, review 042-v2): a key
        // written on a Windows dev box reads correctly on the Linux server, and it maps cleanly
        // to a cloud object key in bolt-043. Only the on-disk path uses OS separators.
        var key = prefix is null
            ? $"{ownerId}/{fileName}"
            : $"{prefix}/{ownerId}/{fileName}";

        var fullPath = ToFullPath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        // Write to a unique temp file then atomically move into place. Two concurrent writers of
        // the same deterministic key (e.g. two first-previews of one upload) would otherwise
        // collide on File.Create's exclusive FileShare.None handle → IOException → 500 (M2,
        // review 042-v4). The same key only ever holds the same logical artifact, so last-writer
        // overwrite is safe.
        var tempPath = $"{fullPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var fs = File.Create(tempPath))
            {
                stream.Position = 0;
                await stream.CopyToAsync(fs, ct);
            }
            File.Move(tempPath, fullPath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
            throw;
        }

        _logger.LogDebug("Saved upload to {Key}", key);
        return key;
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = ToFullPath(storagePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogDebug("Deleted upload {StoragePath}", storagePath);
        }
        return Task.CompletedTask;
    }

    public Task<Stream> GetStreamAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = ToFullPath(storagePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Stored upload not found.", fullPath);

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task<bool> ExistsAsync(string storagePath, CancellationToken ct = default)
        => Task.FromResult(File.Exists(ToFullPath(storagePath)));

    /// <summary>Maps an OS-independent '/'-separated storage key to an on-disk path.</summary>
    private string ToFullPath(string storageKey)
        => Path.Combine(_basePath, storageKey.Replace('/', Path.DirectorySeparatorChar));
}
