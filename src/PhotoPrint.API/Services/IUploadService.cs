using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

/// <summary>
/// Result of <see cref="IUploadService.GetPreviewAsync"/> — the upload's id, the storage tier
/// owning its bytes, and the thumbnail key. The controller dispatches stream-vs-302 based on
/// <see cref="Location"/>.
/// </summary>
public record PreviewLocation(Guid UploadId, StorageLocation Location, string ThumbnailKey);

public interface IUploadService
{
    /// <summary>
    /// Validates, stores, and persists metadata for a single photo upload.
    /// Throws UnsupportedMediaTypeException, RequestEntityTooLargeException,
    /// or TooManyRequestsException on constraint violations.
    /// </summary>
    Task<DTOs.Uploads.UploadDto> UploadAsync(
        Stream fileStream,
        string originalFileName,
        long declaredLength,
        Guid? userId,
        Guid? guestSessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Authorizes the caller, ensures the cached thumbnail exists in the upload's storage tier
    /// (generating + persisting it on first request — bolt 042), and returns the
    /// <see cref="PreviewLocation"/>. The controller chooses stream-vs-302 from the location.
    /// Throws <see cref="Exceptions.NotFoundException"/> if missing/deleted, or
    /// <see cref="Exceptions.ForbiddenException"/> if the caller does not own the upload.
    /// </summary>
    Task<PreviewLocation> GetPreviewAsync(
        Guid uploadId,
        Guid? userId,
        Guid? guestSessionId,
        CancellationToken ct = default);
}
