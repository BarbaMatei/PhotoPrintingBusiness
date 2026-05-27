using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace PhotoPrint.API.Services;

public class ImageProcessor : IImageProcessor
{
    private const int ThumbnailMaxDimension = 300;
    private const int ThumbnailJpegQuality = 85;

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
