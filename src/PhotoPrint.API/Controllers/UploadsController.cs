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

    private readonly IUploadService _uploadService;

    public UploadsController(IUploadService uploadService)
    {
        _uploadService = uploadService;
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

        // Thumbnails are UUID-keyed and immutable — allow long-lived shared caching.
        Response.Headers.CacheControl = "public, max-age=2592000, immutable";

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
