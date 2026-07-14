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

    // Cached thumbnails live under a distinct top-level namespace, keyed deterministically by
    // the upload id, so a thumbnail can never collide with the original ({owner}/{id}.jpg) and
    // a racing/cancelled write simply overwrites the same key instead of leaking (bolt 042,
    // BUG-3/REQ-2). The cleanup job deletes this exact stored path (BUG-2).
    private const string ThumbnailPrefix = "thumbs";

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

        if (ImageProcessor.ExceedsDecodeLimits(imageInfo.WidthPx, imageInfo.HeightPx))
        {
            await _storage.DeleteAsync(storagePath, ct);
            throw new DecompressionBombException(
                imageInfo.WidthPx, imageInfo.HeightPx, ImageProcessor.DimensionsExceededMessage);
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
        // AsNoTracking keeps the steady-state cache-HIT path allocation-free (no identity-map
        // entry / original-values snapshot for an entity we never save) (QUAL-1). The miss
        // branch attaches + marks the single column below.
        var upload = await _db.Uploads
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == uploadId && u.DeletedAt == null, ct);

        if (upload is null)
            throw new NotFoundException($"Upload {uploadId} not found.");

        var isOwner = (userId.HasValue && upload.UserId == userId) ||
                      (guestSessionId.HasValue && upload.GuestSessionId == guestSessionId);

        if (!isOwner)
            throw new ForbiddenException("You do not have access to this upload.");

        // Cache hit: stream the stored thumbnail — no ImageSharp work on this path.
        if (upload.ThumbnailPath is not null && await _storage.ExistsAsync(upload.ThumbnailPath, ct))
            return (await _storage.GetStreamAsync(upload.ThumbnailPath, ct), "image/jpeg");

        // Cache miss: thumbnail never generated, or the cached file was removed (ops-side
        // deletion). Generate once and store under a DETERMINISTIC, id-keyed path in a distinct
        // namespace so a concurrent or cancelled write overwrites the same key rather than
        // minting a new random file that leaks, and the cleanup job can target it (BUG-2/BUG-3).
        var ownerId = upload.UserId ?? upload.GuestSessionId!.Value;
        MemoryStream generated;
        try
        {
            generated = await _imageProcessor.GenerateThumbnailAsync(upload.FilePath, ct);
        }
        catch (FileNotFoundException)
        {
            // The original blob is gone (ops-side deletion or the cleanup race) though the row
            // survives. There's nothing to regenerate from, so surface a clean 404 rather than
            // an unmapped FileNotFoundException -> 500 (M6, review 042-v4).
            throw new NotFoundException($"Upload {uploadId} is no longer available.");
        }
        var thumbnailPath = await _storage.SaveAsync(
            generated, ownerId, "jpg", ct, fileId: uploadId, prefix: ThumbnailPrefix);
        upload.ThumbnailPath = thumbnailPath;

        // Persist only ThumbnailPath. The entity was read AsNoTracking, so attach it and mark
        // the single column modified instead of tracking the whole graph.
        _db.Uploads.Attach(upload);
        _db.Entry(upload).Property(u => u.ThumbnailPath).IsModified = true;
        await _db.SaveChangesAsync(ct);

        // The cleanup job may have soft-deleted this row between the live read above and this
        // write (the write keys only on Id — no DeletedAt guard, and Upload has no concurrency
        // token). A thumbnail written onto a now-dead row is never revisited by cleanup, so
        // delete it here to stop it leaking forever (M1, review 042-v4).
        var stillLive = await _db.Uploads
            .AsNoTracking()
            .AnyAsync(u => u.Id == uploadId && u.DeletedAt == null, ct);
        if (!stillLive)
            await _storage.DeleteAsync(thumbnailPath, ct);

        // Return the just-generated stream directly rather than re-opening it from storage —
        // saves an open+read now and a billed round-trip once cloud storage lands (QUAL-2).
        generated.Position = 0;
        return (generated, "image/jpeg");
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
