using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoPrint.API.Authentication;
using PhotoPrint.API.DTOs.Uploads;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Extensions;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Controllers;

[ApiController]
[Route("api/uploads")]
[Authorize(Policy = GuestSessionExtensions.DualAuthPolicy)]
public class UploadsController : ControllerBase
{
    private const long MaxFileSizeBytes = 52_428_800L;        // 50 MB per file
    private const long MaxBatchSizeBytes = 524_288_000L;       // 500 MB total batch

    // A preview is an ownership-checked, per-user resource, so it must never be
    // shared-cacheable (SEC-1 + QUAL-4, review 042-v1). `private` keeps it out of
    // ASP.NET Core ResponseCaching and any shared proxy/CDN while still allowing a
    // per-user browser cache. `immutable` is intentionally dropped: a thumbnail can
    // be regenerated after an ops-side deletion, so the response is not immutable.
    private static readonly string PreviewCacheControl =
        $"private, max-age={(int)TimeSpan.FromDays(30).TotalSeconds}";

    private readonly IUploadService _uploadService;
    private readonly ILogger<UploadsController> _logger;

    public UploadsController(IUploadService uploadService, ILogger<UploadsController> logger)
    {
        _uploadService = uploadService;
        _logger = logger;
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
                // OBS-1 (review 042-v1): the batch endpoint turns each rejection into a
                // per-item result and returns 200, so without this the exception never
                // reaches ExceptionHandlerMiddleware and bulk abuse (the most likely bomb
                // vector) is invisible to ops. Log it — Warning, no file bytes / no PII.
                _logger.LogWarning(
                    "uploads.batch.item_rejected file={FileName} reason={Reason} correlation_id={CorrelationId}",
                    file.FileName, ex.GetType().Name, HttpContext.GetCorrelationId());
                results.Add(new BatchUploadItemResult(file.FileName, null, ex.Message));
            }
        }

        return Ok(results);
    }

    // GET /api/uploads/{id}/preview
    [HttpGet("{id:guid}/preview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreviewAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrNull();
        var guestSessionId = User.GetGuestSessionIdOrNull();

        var (stream, contentType) = await _uploadService.GetPreviewAsync(
            id, userId, guestSessionId, cancellationToken);

        Response.Headers.CacheControl = PreviewCacheControl;

        var etag = $"\"{id}-{stream.Length}\"";
        Response.Headers.ETag = etag;

        if (Request.Headers.IfNoneMatch == etag)
        {
            stream.Dispose();
            return StatusCode(StatusCodes.Status304NotModified);
        }

        return File(stream, contentType);
    }
}
