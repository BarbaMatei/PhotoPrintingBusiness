using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Authentication;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.DTOs.Uploads;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Extensions;
using PhotoPrint.API.Models;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Controllers;

[ApiController]
[Route("api/uploads")]
[Authorize(Policy = GuestSessionExtensions.DualAuthPolicy)]
public class UploadsController : ControllerBase
{
    private const long MaxFileSizeBytes = 52_428_800L;        // 50 MB per file
    private const long MaxBatchSizeBytes = 524_288_000L;       // 500 MB total batch

    // A preview is an ownership-checked, per-user resource, so it must never be shared-cacheable
    // (SEC-1 + QUAL-4, review 042-v1). `private` keeps it out of any shared proxy/CDN while still
    // allowing a per-user browser cache. `immutable` is intentionally dropped: a thumbnail can be
    // regenerated after an ops-side deletion, so the response is not immutable.
    private static readonly string PreviewCacheControl =
        $"private, max-age={(int)TimeSpan.FromDays(30).TotalSeconds}";

    private readonly IUploadService _uploadService;
    private readonly IStorageRouter _storageRouter;
    private readonly StorageSettings _storageSettings;
    private readonly ILogger<UploadsController> _logger;

    public UploadsController(
        IUploadService uploadService,
        IStorageRouter storageRouter,
        IOptions<StorageSettings> storageSettings,
        ILogger<UploadsController> logger)
    {
        _uploadService = uploadService;
        _storageRouter = storageRouter;
        _storageSettings = storageSettings.Value;
        _logger = logger;
    }

    // Client-controlled filenames must be neutralised before logging (L6, review 042-v4):
    // strip control chars (a newline forges a fake log line in plain-text sinks) and cap length
    // (an unbounded name is a log-volume amplification vector).
    private static string SanitizeForLog(string? name)
    {
        if (string.IsNullOrEmpty(name)) return "(none)";
        var cleaned = new string(name.Where(c => !char.IsControl(c)).ToArray());
        return cleaned.Length > 128 ? cleaned[..128] : cleaned;
    }

    // POST /api/uploads
    [HttpPost]
    [ProducesResponseType(typeof(UploadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<IActionResult> UploadPhotoAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrNull();
        var guestSessionId = User.GetGuestSessionIdOrNull();

        // Buffer the form file into a seekable MemoryStream.
        // IFormFile.OpenReadStream() may return a non-seekable stream in some
        // hosting contexts; MimeValidator and IStorageService both require seeking.
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var dto = await _uploadService.UploadAsync(
            buffer,
            file.FileName,
            file.Length,
            userId,
            guestSessionId,
            cancellationToken);

        return CreatedAtAction(
            // MvcOptions.SuppressAsyncSuffixInActionNames = true (ASP.NET Core default)
            // strips "Async" from action names, so the registered name is "GetPreview".
            actionName: "GetPreview",
            new { id = dto.Id },
            dto);
    }

    // POST /api/uploads/batch
    // Accepts multiple files in one multipart/form-data request (field name: "files").
    // Processes files sequentially to keep the per-session upload-count check consistent.
    // Returns one result per file; failed files carry an error message instead of a DTO.
    [HttpPost("batch")]
    [ProducesResponseType(typeof(IReadOnlyList<BatchUploadItemResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(MaxBatchSizeBytes)]
    public async Task<IActionResult> UploadPhotoBatchAsync(
        [FromForm] List<IFormFile> files,
        CancellationToken cancellationToken)
    {
        if (files is null || files.Count == 0)
            return BadRequest(new { message = "No files provided." });

        var userId = User.GetUserIdOrNull();
        var guestSessionId = User.GetGuestSessionIdOrNull();

        var results = new List<BatchUploadItemResult>(files.Count);

        foreach (var file in files)
        {
            try
            {
                using var buffer = new MemoryStream();
                await file.CopyToAsync(buffer, cancellationToken);
                buffer.Position = 0;

                var dto = await _uploadService.UploadAsync(
                    buffer, file.FileName, file.Length,
                    userId, guestSessionId, cancellationToken);

                results.Add(new BatchUploadItemResult(file.FileName, dto, null));
            }
            catch (Exception ex) when (
                ex is UnsupportedMediaTypeException or
                      UnprocessableEntityException or
                      TooManyRequestsException or
                      RequestEntityTooLargeException or
                      BadRequestException)
            {
                // A batch rejection is swallowed into a per-item result (200 overall), so it never
                // reaches ExceptionHandlerMiddleware. Log it here or bulk abuse is invisible to ops
                // (OBS-1, review 042-v1).
                var safeName = SanitizeForLog(file.FileName);
                if (ex is DecompressionBombException bomb)
                    // DecompressionBombException subclasses UnprocessableEntityException, so without
                    // this branch the reserved bomb event (with dimensions) that ops alerts key on
                    // would never fire for the /batch vector (M4, review 042-v4).
                    _logger.LogWarning(
                        "uploads.decompression_bomb.rejected file={File} width={Width} height={Height}",
                        safeName, bomb.WidthPx, bomb.HeightPx);
                else
                    _logger.LogWarning(
                        "uploads.batch.item_rejected file={File} reason={Reason}",
                        safeName, ex.GetType().Name);

                results.Add(new BatchUploadItemResult(file.FileName, null, ex.Message));
            }
        }

        return Ok(results);
    }

    // GET /api/uploads/{id}/preview
    //
    // Bolt 043 (ADR-008): the response shape depends on which tier owns the upload's bytes.
    //   Local upload  -> 200 image/jpeg + private 30-day cache (bolt 042 SEC-1 behaviour).
    //   Cloud upload  -> 302 Found to a 1 h presigned URL + Cache-Control: private,
    //                    max-age=3600 (so shared caches never leak a user's signed URL).
    // Authorization runs in the service BEFORE any presigned URL is generated.
    [HttpGet("{id:guid}/preview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreviewAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrNull();
        var guestSessionId = User.GetGuestSessionIdOrNull();

        var loc = await _uploadService.GetPreviewAsync(
            id, userId, guestSessionId, cancellationToken);

        if (loc.Location == StorageLocation.Cloud)
            return await CloudRedirectAsync(loc, cancellationToken);

        try
        {
            return await StreamLocalAsync(id, loc, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            // TOCTOU (F8, review 043-v1): GetPreviewAsync resolved Local, then a concurrent
            // promotion best-effort-deleted the local thumb before we opened it. Re-resolve
            // once — the upload is now Cloud (→ 302) or the thumb regenerated — rather than
            // letting the unmapped FileNotFoundException surface as a 500.
            var reResolved = await _uploadService.GetPreviewAsync(
                id, userId, guestSessionId, cancellationToken);

            if (reResolved.Location == StorageLocation.Cloud)
                return await CloudRedirectAsync(reResolved, cancellationToken);

            try
            {
                return await StreamLocalAsync(id, reResolved, cancellationToken);
            }
            catch (FileNotFoundException)
            {
                _logger.LogWarning(
                    "uploads.preview.local_thumb_vanished upload_id={UploadId}", id);
                return NotFound();
            }
        }
    }

    private async Task<IActionResult> CloudRedirectAsync(
        PreviewLocation loc, CancellationToken ct)
    {
        // Cloud tier → presigned 302. Bytes flow browser ↔ object store directly.
        var ttl = TimeSpan.FromMinutes(_storageSettings.PresignTtlMinutes);
        var url = await _storageRouter.Cloud.GetPresignedUrlAsync(loc.ThumbnailKey, ttl, ct);

        Response.Headers.CacheControl = "private, max-age=3600";
        return Redirect(url);
    }

    // Opens the local thumbnail and returns a 200 (or 304). Throws FileNotFoundException if the
    // thumb has been deleted since GetPreviewAsync resolved it — the caller handles that.
    private async Task<IActionResult> StreamLocalAsync(Guid id, PreviewLocation loc, CancellationToken ct)
    {
        var stream = await _storageRouter.Local.GetStreamAsync(loc.ThumbnailKey, ct);

        Response.Headers.CacheControl = PreviewCacheControl;
        var etag = $"\"{id}-{stream.Length}\"";
        Response.Headers.ETag = etag;

        if (Request.Headers.IfNoneMatch == etag)
        {
            stream.Dispose();
            return StatusCode(StatusCodes.Status304NotModified);
        }

        return File(stream, "image/jpeg");
    }
}
