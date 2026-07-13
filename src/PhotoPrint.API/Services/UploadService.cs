using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;
using PhotoPrint.API.DTOs.Uploads;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

public class UploadService : IUploadService
{
    private const long MaxFileSizeBytes = 52_428_800L; // 50 MB
    private const int MaxUploadsPerSession = 100;

    private readonly IStorageService _storage;
    private readonly IMimeValidator _mimeValidator;
    private readonly IImageProcessor _imageProcessor;
    private readonly PhotoPrintDbContext _db;
    private readonly ILogger<UploadService> _logger;

    public UploadService(
        IStorageService storage,
        IMimeValidator mimeValidator,
        IImageProcessor imageProcessor,
        PhotoPrintDbContext db,
        ILogger<UploadService> logger)
    {
        _storage = storage;
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

        // Determine file extension from MIME type — never trust client filename extension
        var extension = mimeType switch
        {
            "image/jpeg" => "jpg",
            "image/png"  => "png",
            "image/heic" => "heic",
            _            => "bin",
        };

        var uploadId = Guid.NewGuid();
        var storagePath = await _storage.SaveAsync(fileStream, ownerId, extension, ct, fileId: uploadId);

        var imageInfo = await _imageProcessor.GetInfoAsync(storagePath, ct);
        if (imageInfo is null)
        {
            await _storage.DeleteAsync(storagePath, ct);
            throw new UnprocessableEntityException("The uploaded file could not be read as an image.");
        }

        var actualLength = fileStream.Length;

        var upload = new Upload
        {
            Id              = uploadId,
            UserId          = userId,
            GuestSessionId  = guestSessionId,
            FilePath        = storagePath,
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
            UploadedAt      = DateTimeOffset.UtcNow,
        };

        _db.Uploads.Add(upload);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Upload {UploadId} saved for owner {OwnerId} ({MimeType}, {W}x{H}, {Size:N0} bytes)",
            upload.Id, ownerId, mimeType, upload.WidthPx, upload.HeightPx, upload.FileSizeBytes);

        return MapToDto(upload);
    }

    public async Task<(Stream stream, string contentType)> GetPreviewAsync(
        Guid uploadId,
        Guid? userId,
        Guid? guestSessionId,
        CancellationToken ct = default)
    {
        var upload = await _db.Uploads
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == uploadId && u.DeletedAt == null, ct);

        if (upload is null)
            throw new NotFoundException($"Upload {uploadId} not found.");

        var isOwner = (userId.HasValue && upload.UserId == userId) ||
                      (guestSessionId.HasValue && upload.GuestSessionId == guestSessionId);

        if (!isOwner)
            throw new ForbiddenException("You do not have access to this upload.");

        var thumbnail = await _imageProcessor.GenerateThumbnailAsync(upload.FilePath, ct);
        return (thumbnail, "image/jpeg");
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
