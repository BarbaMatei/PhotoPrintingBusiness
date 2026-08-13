namespace PhotoPrint.API.Services;

/// <summary>
/// Owns naming policy for object/file keys in storage. The adapter does byte persistence;
/// keys are generated here so the same scheme is reproducible from the same row data —
/// crucial for intent-024 promotion + backfill.
/// </summary>
/// <remarks>
/// Key conventions (Option 2):
/// <list type="bullet">
///   <item><c>uploads/{yyyy}/{MM}/{uploadId:N}{ext}</c> — original photo, partitioned by upload month.</item>
///   <item><c>thumbs/{uploadId:N}.jpg</c> — 300 px cached thumbnail.</item>
///   <item><c>previews/{uploadId:N}.jpg</c> — ~2000 px large web preview.</item>
/// </list>
/// Keys are deterministic from the Upload row, so the migrator/backfill never needs a
/// stored lookup to recompute them. No <c>ownerId</c> appears in the path — authorization
/// is enforced at the API layer; keys are opaque UUIDs.
/// </remarks>
public static class StorageKeys
{
    /// <summary>Original-photo key for a given upload + extension.</summary>
    public static string Original(Guid uploadId, DateTimeOffset createdAt, string extension)
    {
        var ext = NormalizeExtension(extension);
        return $"uploads/{createdAt:yyyy}/{createdAt:MM}/{uploadId:N}{ext}";
    }

    /// <summary>Thumbnail key (single 300 px JPEG per upload).</summary>
    public static string Thumbnail(Guid uploadId) => $"thumbs/{uploadId:N}.jpg";

    /// <summary>Large web preview key (~2000 px JPEG, intent 024 / bolt 051).</summary>
    public static string Preview(Guid uploadId) => $"previews/{uploadId:N}.jpg";

    /// <summary>
    /// Rejects keys that would escape the storage root, point at an absolute path,
    /// or sneak in backslash separators. All adapter methods that accept a key must
    /// call this; <see cref="LocalStorageService"/> in particular would otherwise be
    /// vulnerable to path traversal via a crafted key.
    /// </summary>
    public static void Validate(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Storage key must not be empty.", nameof(key));
        if (key.StartsWith('/') || key.StartsWith('\\'))
            throw new ArgumentException("Storage key must be relative (no leading separator).", nameof(key));
        if (key.Contains('\\'))
            throw new ArgumentException("Storage key must use forward slashes only.", nameof(key));
        if (key.Contains("..", StringComparison.Ordinal))
            throw new ArgumentException("Storage key must not contain '..' traversal.", nameof(key));
        if (key.Length > 512)
            throw new ArgumentException("Storage key must be ≤ 512 characters.", nameof(key));
    }

    private static string NormalizeExtension(string ext)
    {
        if (string.IsNullOrEmpty(ext)) return string.Empty;
        return ext.StartsWith('.') ? ext : "." + ext;
    }
}
