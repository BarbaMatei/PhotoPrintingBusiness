using PhotoPrint.API.Exceptions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace PhotoPrint.API.Services;

public class ImageProcessor : IImageProcessor
{
    private const int ThumbnailMaxDimension = 300;
    private const int ThumbnailJpegQuality = 85;

    // ── Bolt 051 (intent 024) — large web preview tier ───────────────────────────
    // The customer-facing "full view" representation in the order history. Sized
    // to look crisp on a desktop monitor without serving the multi-MB original.
    private const int LargePreviewMaxDimension = 2000;
    private const int LargePreviewJpegQuality = 85;

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

    public async Task<MemoryStream> GenerateLargePreviewAsync(Stream source, CancellationToken ct = default)
    {
        // Same decompression-bomb defence as the thumbnail path — promotion happens on
        // user-supplied bytes that have already passed the upload-time check, but defence in
        // depth is cheap. Identify first; never decode an image we'd reject anyway.
        if (source.CanSeek)
            source.Position = 0;
        var info = await Image.IdentifyAsync(source, ct);
        if (info is not null && (info.Width > MaxDecodeDimension || info.Height > MaxDecodeDimension))
            throw new UnprocessableEntityException("Image dimensions exceed limits.");

        if (source.CanSeek)
            source.Position = 0;
        using var image = await Image.LoadAsync(source, ct);

        // Story 002: "never upscale — images already < 2000 px pass through at native size."
        // ImageSharp 3.x's ResizeMode.Max DOES upscale a smaller source to fit the bound,
        // so we gate the resize on at-least-one-dimension exceeding the target. A 1500×1000
        // input is re-encoded at native 1500×1000; a 4032×3024 phone photo comes out at
        // 2000×1500.
        if (image.Width > LargePreviewMaxDimension || image.Height > LargePreviewMaxDimension)
        {
            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(LargePreviewMaxDimension, LargePreviewMaxDimension),
                Mode = ResizeMode.Max,
            }));
        }

        var ms = new MemoryStream();
        var encoder = new JpegEncoder { Quality = LargePreviewJpegQuality };
        await image.SaveAsync(ms, encoder, ct);
        ms.Position = 0;
        return ms;
    }
}
