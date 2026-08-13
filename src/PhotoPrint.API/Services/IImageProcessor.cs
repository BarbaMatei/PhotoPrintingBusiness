namespace PhotoPrint.API.Services;

public record ImageInfo(int WidthPx, int HeightPx);

public interface IImageProcessor
{
    /// <summary>
    /// Reads image dimensions from a source stream without loading the entire image into memory.
    /// Returns null if the stream is not a valid image.
    /// </summary>
    /// <remarks>
    /// Refactored in bolt 043: the processor no longer holds an
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

    /// <summary>
    /// Generates a JPEG large web preview (max 2000 px on the longest dimension, quality 85)
    /// from the supplied source stream. Aspect ratio preserved; never upscales — an image
    /// already smaller than 2000 px on its long edge is re-encoded at native dimensions.
    /// Subject to the same decompression-bomb guard as the thumbnail path. Used by the
    /// intent-024 promoter to generate the customer-facing full-view preview.
    /// Returns the preview as a <see cref="MemoryStream"/> rewound to position 0.
    /// </summary>
    Task<MemoryStream> GenerateLargePreviewAsync(Stream source, CancellationToken ct = default);
}
