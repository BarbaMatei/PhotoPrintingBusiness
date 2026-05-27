namespace PhotoPrint.API.DTOs.Uploads;

public record UploadDto(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    int WidthPx,
    int HeightPx,
    long FileSizeBytes,
    DateTimeOffset UploadedAt);
