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
    private const int MaxOriginalFileNameLength = 260; // UploadConfiguration HasMaxLength(260)

    private static string SanitizeFileName(string originalFileName)
    {
        if (string.IsNullOrEmpty(originalFileName))
            return originalFileName;
        var name = originalFileName[(originalFileName.LastIndexOfAny(new[] { '/', '\\' }) + 1)..];
        return name.Length <= MaxOriginalFileNameLength ? name : name[..MaxOriginalFileNameLength];
    }

    private readonly IStorageRouter _router;
    private readonly IMimeValidator _mimeValidator;
    private readonly IImageProcessor _imageProcessor;
    private readonly PhotoPrintDbContext _db;
    private readonly ILogger<UploadService> _logger;

    // The router replaces the single IStorageService injection so a
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
                "Only JPEG and PNG files are accepted.");

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
            _            => "bin",
        };

        // Validate the image up-front, BEFORE writing it (caller owns key + I/O).
        // No need to save-then-delete on validation failure now.
        if (fileStream.CanSeek)
            fileStream.Position = 0;
        var imageInfo = await _imageProcessor.GetInfoAsync(fileStream, ct);
        if (imageInfo is null)
            throw new UnprocessableEntityException("The uploaded file could not be read as an image.");
        // Total-pixel-area bomb check — a per-axis cap misses the square
        // bomb (25000×25000 passes 25000-per-axis yet decodes to ~625 MP). Reject up-front,
        // before the file is written (validate-then-save).
        if (ImageProcessor.ExceedsDecodeLimits(imageInfo.WidthPx, imageInfo.HeightPx))
            throw new DecompressionBombException(
                imageInfo.WidthPx, imageInfo.HeightPx, ImageProcessor.DimensionsExceededMessage);

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
            // Then cap to the column length: HasMaxLength(260) sizes the column but never
            // truncates, so an over-length name 201s on InMemory yet 22001-500s on Postgres.
            OriginalFileName = SanitizeFileName(originalFileName),
            ContentType     = mimeType,
            WidthPx         = imageInfo.WidthPx,
            HeightPx        = imageInfo.HeightPx,
            FileSizeBytes   = actualLength,
            UploadedAt      = createdAt,
        };

        _db.Uploads.Add(upload);
        await _db.SaveChangesAsync(ct);

        // Observability: upload_size_bytes histogram.
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

        // A Cloud-located upload with the cloud tier disabled is unroutable: For(Cloud) would throw
        // InvalidOperationException (unmapped -> 500) on the customer preview path. Degrade to the
        // same clean 404 as a missing original and signal the misconfiguration for ops. This is the
        // customer-preview sibling of the F2/F9 cleanup/ZIP guards.
        if (upload.StorageLocation == StorageLocation.Cloud && !_router.CloudEnabled)
        {
            _logger.LogWarning("uploads.preview.unroutable upload_id={UploadId} reason=cloud-tier-off", uploadId);
            throw new NotFoundException($"Upload {uploadId} is no longer available.");
        }

        // Route to the adapter that owns this upload's bytes.
        var store = _router.For(upload.StorageLocation);

        // Cache hit: recorded thumbnail still present in the tier. Return without any decode
        // work. Read-through Exists rather than trusting ThumbnailPath so an ops-side deletion
        // transparently regenerates below instead of handing the caller a dead key.
        if (upload.ThumbnailPath is not null && await store.ExistsAsync(upload.ThumbnailPath, ct))
            return new PreviewLocation(upload.Id, upload.StorageLocation, upload.ThumbnailPath);

        // Recorded-but-absent thumbnail: emit a distinct signal so a silently-broken cache is not
        // indistinguishable from a first-time miss, then regenerate below.
        if (upload.ThumbnailPath is not null)
            _logger.LogWarning("uploads.thumbnail.cache_miss_missing_file upload_id={UploadId}", uploadId);

        // Cache miss: regenerate from the original in the upload's current tier. FilePath is
        // nullable since bolt 052 (the original-purge nulls it). If it is gone, we cannot
        // regenerate — signal the storage-integrity incident distinctly and
        // surface a clean 404 ("your photos are no longer available"; unit 003 catches this).
        if (upload.FilePath is null)
        {
            _logger.LogWarning("uploads.original.missing_file upload_id={UploadId}", uploadId);
            throw new NotFoundException($"Upload {uploadId} is no longer available.");
        }

        var thumbKey = StorageKeys.Thumbnail(upload.Id);
        MemoryStream generated;
        try
        {
            await using var src = await store.GetStreamAsync(upload.FilePath, ct);
            generated = await _imageProcessor.GenerateThumbnailAsync(src, ct);
        }
        catch (FileNotFoundException)
        {
            // FilePath is recorded but the blob is physically gone (ops-side deletion / cleanup
            // race). Unmapped, GetStreamAsync's FileNotFoundException surfaces as a 500. Signal it and return a clean 404 instead.
            _logger.LogWarning("uploads.original.missing_file upload_id={UploadId}", uploadId);
            throw new NotFoundException($"Upload {uploadId} is no longer available.");
        }

        await using (generated)
        {
            await store.SaveAsync(generated, thumbKey, ct);
        }

        // Persist only ThumbnailPath. The entity is tracked, so setting the single property marks
        // just that column modified — a concurrent soft-delete's DeletedAt is not overwritten,
        // which is what lets the deleted-row-race check below observe it.
        upload.ThumbnailPath = thumbKey;
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // The thumbnail is stored but ThumbnailPath didn't persist, so the cleanup job (which
            // keys on ThumbnailPath) can never reclaim it. Signal + best-effort delete so it can't
            // leak silently, then rethrow.
            _logger.LogWarning(ex,
                "uploads.thumbnail.orphaned_on_commit_failure upload_id={UploadId} key={Key}",
                uploadId, thumbKey);
            try { await store.DeleteAsync(thumbKey, ct); } catch { /* best-effort */ }
            throw;
        }

        // The cleanup job may have soft-deleted this row between the live read above and this
        // write (the write keys only on Id — no DeletedAt guard, and Upload has no concurrency
        // token). A thumbnail written onto a now-dead row is never revisited by cleanup, so
        // delete it here to stop it leaking forever.
        var stillLive = await _db.Uploads
            .AsNoTracking()
            .AnyAsync(u => u.Id == uploadId && u.DeletedAt == null, ct);
        if (!stillLive)
        {
            _logger.LogWarning(
                "uploads.thumbnail.deleted_row_race upload_id={UploadId} key={Key}", uploadId, thumbKey);
            await store.DeleteAsync(thumbKey, ct);
        }

        return new PreviewLocation(upload.Id, upload.StorageLocation, thumbKey);
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
