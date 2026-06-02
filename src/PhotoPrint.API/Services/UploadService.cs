using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Uploads;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Observability;

namespace PhotoPrint.API.Services;

public class UploadService : IUploadService
{
    private const long MaxFileSizeBytes = 52_428_800L; // 50 MB
    private const int MaxUploadsPerSession = 100;

    private readonly IStorageRouter _router;
    private readonly IMimeValidator _mimeValidator;
    private readonly IImageProcessor _imageProcessor;
    private readonly PhotoPrintDbContext _db;
    private readonly ILogger<UploadService> _logger;

    // Bolt 043 (ADR-008): the router replaces the single IStorageService injection so a
    // promoted (Cloud) upload's bytes are read/written against the cloud adapter — every
    // path here routes by upload.StorageLocation.
    public UploadService(
        IStorageRouter router,
        IMimeValidator mimeValidator,
        IImageProcessor imageProcessor,
        PhotoPrintDbContext db,
        ILogger<UploadService> logger)
    {
        _router = router;
        _mimeValidator = mimeValidator;
        _imageProcessor = imageProcessor;
        _db = db;
        _logger = logger;
    }

    public async Task<UploadDto> UploadAsync(
        Stream fileStream,
        string originalFileName,
        long declaredLength,
        Guid? userId,
        Guid? guestSessionId,
        CancellationToken ct = default)
    {
        if (declaredLength > MaxFileSizeBytes)
            throw new RequestEntityTooLargeException(
                $"File size {declaredLength:N0} bytes exceeds the 50 MB limit.");

        var mimeType = _mimeValidator.DetectMimeType(fileStream);
        if (mimeType is null)
            throw new UnsupportedMediaTypeException(
                "Only JPEG, PNG, and HEIC files are accepted.");

        var ownerId = userId ?? guestSessionId
            ?? throw new BadRequestException("Request must be authenticated or carry a guest token.");

        if (guestSessionId.HasValue)
        {
            var sessionCount = await _db.Uploads
                .CountAsync(u => u.GuestSessionId == guestSessionId && u.DeletedAt == null, ct);

            if (sessionCount >= MaxUploadsPerSession)
                throw new TooManyRequestsException(
                    $"Guest sessions are limited to {MaxUploadsPerSession} active uploads.");
        }

        // Determine file extension from MIME type — never trust client filename extension.
        var extension = mimeType switch
        {
            "image/jpeg" => "jpg",
            "image/png"  => "png",
            "image/heic" => "heic",
            _            => "bin",
        };

        // Validate the image up-front, BEFORE writing it (ADR-007: caller owns key + I/O).
        // No need to save-then-delete on validation failure now.
        if (fileStream.CanSeek)
            fileStream.Position = 0;
        var imageInfo = await _imageProcessor.GetInfoAsync(fileStream, ct);
        if (imageInfo is null)
            throw new UnprocessableEntityException("The uploaded file could not be read as an image.");
        if (imageInfo.WidthPx > ImageProcessor.MaxDecodeDimension ||
            imageInfo.HeightPx > ImageProcessor.MaxDecodeDimension)
            throw new UnprocessableEntityException("Image dimensions exceed limits.");

        var uploadId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var key = StorageKeys.Original(uploadId, createdAt, extension);

        // New uploads always start on the Local tier — pre-payment bytes never go to cloud.
        // The intent-024 promoter flips StorageLocation to Cloud (and re-writes the bytes
        // there) when the order is paid.
        if (fileStream.CanSeek)
            fileStream.Position = 0;
        await _router.Local.SaveAsync(fileStream, key, ct);

        var actualLength = fileStream.Length;

        var upload = new Upload
        {
            Id              = uploadId,
            UserId          = userId,
            GuestSessionId  = guestSessionId,
            FilePath        = key,
            StorageLocation = StorageLocation.Local,
            // Strip any directory component OS-independently. Path.GetFileName only treats
            // '\' as a separator on Windows, so on the Linux server a crafted name like
            // "C:\evil\x.jpg" would pass through unsanitised — strip both '/' and '\'.
            OriginalFileName = string.IsNullOrEmpty(originalFileName)
                ? originalFileName
                : originalFileName[(originalFileName.LastIndexOfAny(new[] { '/', '\\' }) + 1)..],
            ContentType     = mimeType,
            WidthPx         = imageInfo.WidthPx,
            HeightPx        = imageInfo.HeightPx,
            FileSizeBytes   = actualLength,
            UploadedAt      = createdAt,
        };

        _db.Uploads.Add(upload);
        await _db.SaveChangesAsync(ct);

        // Observability (bolt 044): upload_size_bytes histogram.
        FotoMetrics.UploadSize.Record(actualLength);

        _logger.LogInformation(
            "Upload {UploadId} saved for owner {OwnerId} ({MimeType}, {W}x{H}, {Size:N0} bytes)",
            upload.Id, ownerId, mimeType, upload.WidthPx, upload.HeightPx, upload.FileSizeBytes);

        return MapToDto(upload);
    }

    public async Task<PreviewLocation> GetPreviewAsync(
        Guid uploadId,
        Guid? userId,
        Guid? guestSessionId,
        CancellationToken ct = default)
    {
        var upload = await _db.Uploads
            .FirstOrDefaultAsync(u => u.Id == uploadId && u.DeletedAt == null, ct);

        if (upload is null)
            throw new NotFoundException($"Upload {uploadId} not found.");

        var isOwner = (userId.HasValue && upload.UserId == userId) ||
                      (guestSessionId.HasValue && upload.GuestSessionId == guestSessionId);

        if (!isOwner)
            throw new ForbiddenException("You do not have access to this upload.");

        // Route to the adapter that owns this upload's bytes.
        var store = _router.For(upload.StorageLocation);

        // Cache miss: thumbnail never generated, or the cached file was removed (ops-side
        // deletion). Generate from the original (in the upload's current tier), store back
        // to that tier under the deterministic thumbnail key, and record it.
        if (upload.ThumbnailPath is null || !await store.ExistsAsync(upload.ThumbnailPath, ct))
        {
            // FilePath is nullable since bolt 052 (the original-purge nulls it). If the
            // original is gone, we cannot regenerate a missing thumbnail — surface as 404
            // ("your photos are no longer available"); unit 003 catches this for UI.
            if (upload.FilePath is null)
                throw new NotFoundException($"Upload {uploadId} is no longer available.");

            await using var src = await store.GetStreamAsync(upload.FilePath, ct);
            await using var generated = await _imageProcessor.GenerateThumbnailAsync(src, ct);
            var thumbKey = StorageKeys.Thumbnail(upload.Id);
            await store.SaveAsync(generated, thumbKey, ct);
            upload.ThumbnailPath = thumbKey;
            await _db.SaveChangesAsync(ct);
        }

        return new PreviewLocation(upload.Id, upload.StorageLocation, upload.ThumbnailPath!);
    }

    private static UploadDto MapToDto(Upload u) => new(
        u.Id,
        u.OriginalFileName,
        u.ContentType,
        u.WidthPx,
        u.HeightPx,
        u.FileSizeBytes,
        u.UploadedAt);
}
