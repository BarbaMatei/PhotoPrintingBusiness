namespace PhotoPrint.API.Services;

public record ImageInfo(int WidthPx, int HeightPx);

public interface IImageProcessor
{
    /// <summary>
    /// Reads image dimensions without loading the entire image into memory.
    /// Returns null if the stream is not a valid image.
    /// </summary>
    Task<ImageInfo?> GetInfoAsync(string storagePath, CancellationToken ct = default);

    /// <summary>
    /// Generates a JPEG thumbnail (max 800 px on longest dimension, quality 85).
    /// Returns the thumbnail as a MemoryStream.
    /// </summary>
    Task<MemoryStream> GenerateThumbnailAsync(string storagePath, CancellationToken ct = default);
}
