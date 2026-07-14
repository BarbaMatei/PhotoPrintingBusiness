namespace PhotoPrint.API.Services;

public interface IStorageService
{
    /// <summary>
    /// Saves the stream to storage under a UUID filename.
    /// Returns the relative storage path (never contains user-supplied data).
    /// <paramref name="fileId"/> makes the filename deterministic; <paramref name="prefix"/>
    /// places the file under a distinct top-level namespace (e.g. "thumbs") so a derived
    /// artifact cannot collide with the original's path (bolt 042, BUG-3/REQ-2).
    /// </summary>
    Task<string> SaveAsync(Stream stream, Guid ownerId, string extension, CancellationToken ct = default, Guid? fileId = null, string? prefix = null);

    /// <summary>Deletes a file by its storage path. No-op if the file does not exist.</summary>
    Task DeleteAsync(string storagePath, CancellationToken ct = default);

    /// <summary>Opens a read stream for a stored file.</summary>
    Task<Stream> GetStreamAsync(string storagePath, CancellationToken ct = default);

    /// <summary>Returns true if a file exists at the given storage path.</summary>
    Task<bool> ExistsAsync(string storagePath, CancellationToken ct = default);
}
