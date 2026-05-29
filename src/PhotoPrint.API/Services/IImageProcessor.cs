namespace PhotoPrint.API.Services;

public record ImageInfo(int WidthPx, int HeightPx);

public interface IImageProcessor
{
    /// <summary>
    /// Reads image dimensions from a source stream without loading the entire image into memory.
    /// Returns null if the stream is not a valid image.
    /// </summary>
    /// <remarks>
    /// Refactored in bolt 043 (ADR-008): the processor no longer holds an
    /// <see cref="IStorageService"/> reference. The caller routes via
    /// <see cref="IStorageRouter"/> and supplies the open source stream.
    /// </remarks>
    Task<ImageInfo?> GetInfoAsync(Stream source, CancellationToken ct = default);

    /// <summary>
    /// Generates a JPEG thumbnail (max 300 px on the longest dimension, quality 85) from
    /// the supplied source stream. Returns the thumbnail as a <see cref="MemoryStream"/>
    /// rewound to position 0.
    /// </summary>
    Task<MemoryStream> GenerateThumbnailAsync(Stream source, CancellationToken ct = default);
}
