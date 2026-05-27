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

    private readonly IStorageService _storage;
    private readonly ILogger<ImageProcessor> _logger;

    public ImageProcessor(IStorageService storage, ILogger<ImageProcessor> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<ImageInfo?> GetInfoAsync(string storagePath, CancellationToken ct = default)
    {
        try
        {
            await using var stream = await _storage.GetStreamAsync(storagePath, ct);
            var info = await Image.IdentifyAsync(stream, ct);
            if (info is null) return null;
            return new ImageInfo(info.Width, info.Height);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to identify image at {StoragePath}", storagePath);
            return null;
        }
    }

    public async Task<MemoryStream> GenerateThumbnailAsync(string storagePath, CancellationToken ct = default)
    {
        await using var stream = await _storage.GetStreamAsync(storagePath, ct);

        // Reject pixel bombs before the full decode allocates pixel buffers (bolt 042).
        var info = await Image.IdentifyAsync(stream, ct);
        if (info is not null && (info.Width > MaxDecodeDimension || info.Height > MaxDecodeDimension))
            throw new UnprocessableEntityException("Image dimensions exceed limits.");
        stream.Position = 0;

        using var image = await Image.LoadAsync(stream, ct);

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
