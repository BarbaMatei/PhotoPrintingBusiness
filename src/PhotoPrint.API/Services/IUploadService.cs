namespace PhotoPrint.API.Services;

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
    /// Returns the stored file stream and content type for the preview endpoint.
    /// Throws NotFoundException if the upload is not found or has been deleted.
    /// Throws ForbiddenException if the caller does not own the upload.
    /// </summary>
    Task<(Stream stream, string contentType)> GetPreviewAsync(
        Guid uploadId,
        Guid? userId,
        Guid? guestSessionId,
        CancellationToken ct = default);
}
