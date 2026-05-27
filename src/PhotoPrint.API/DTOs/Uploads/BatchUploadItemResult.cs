namespace PhotoPrint.API.DTOs.Uploads;

/// <summary>
/// Per-file outcome within a batch upload response.
/// Exactly one of <see cref="Upload"/> or <see cref="Error"/> is non-null.
/// </summary>
public record BatchUploadItemResult(
    string OriginalFileName,
    UploadDto? Upload,
    string? Error);
