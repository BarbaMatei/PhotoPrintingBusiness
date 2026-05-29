using PhotoPrint.API.Exceptions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace PhotoPrint.API.Services;

public class ImageProcessor : IImageProcessor
{
    private const int ThumbnailMaxDimension = 300;
    private const int ThumbnailJpegQuality = 85;

    /// <summary>Reject images whose width or height exceeds this — decompression-bomb defence (bolt 042).</summary>
    public const int MaxDecodeDimension = 25_000;

    private readonly ILogger<ImageProcessor> _logger;

    // Bolt 043 (ADR-008): no IStorageService dependency. The caller routes via
    // IStorageRouter and hands the processor an open source stream.
    public ImageProcessor(ILogger<ImageProcessor> logger)
    {
        _logger = logger;
    }

    public async Task<ImageInfo?> GetInfoAsync(Stream source, CancellationToken ct = default)
    {
        try
        {
            if (source.CanSeek)
                source.Position = 0;
            var info = await Image.IdentifyAsync(source, ct);
            if (info is null) return null;
            return new ImageInfo(info.Width, info.Height);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to identify image stream.");
            return null;
        }
    }

    public async Task<MemoryStream> GenerateThumbnailAsync(Stream source, CancellationToken ct = default)
    {
        // Defence in depth: even though UploadService validates dimensions before save, a
        // promoted/legacy upload's source could conceivably exceed the cap. Identify the
        // header first; reject before the full decode allocates pixel buffers (bolt 042).
        if (source.CanSeek)
            source.Position = 0;
        var info = await Image.IdentifyAsync(source, ct);
        if (info is not null && (info.Width > MaxDecodeDimension || info.Height > MaxDecodeDimension))
            throw new UnprocessableEntityException("Image dimensions exceed limits.");

        if (source.CanSeek)
            source.Position = 0;
        using var image = await Image.LoadAsync(source, ct);

        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(ThumbnailMaxDimension, ThumbnailMaxDimension),
            Mode = ResizeMode.Max,
        }));

        var ms = new MemoryStream();
        var encoder = new JpegEncoder { Quality = ThumbnailJpegQuality };
        await image.SaveAsync(ms, encoder, ct);
        ms.Position = 0;
        return ms;
    }
}
