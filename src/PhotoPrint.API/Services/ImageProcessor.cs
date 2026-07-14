using PhotoPrint.API.Exceptions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace PhotoPrint.API.Services;

public class ImageProcessor : IImageProcessor
{
    private const int ThumbnailMaxDimension = 300;
    private const int ThumbnailJpegQuality = 85;

    /// <summary>
    /// Reject images whose total pixel area exceeds this — decompression-bomb defence
    /// (bolt 042, BUG-1). A per-axis cap misses the total-pixel bomb: a 25000×25000 image
    /// passes any 25000-per-axis check yet decodes to ~625 MP ≈ 2.5 GB. An area cap bounds
    /// the decode allocation instead. Sized (NEW-1, review 042-v2) to accept legitimate
    /// large-format prints (A1 @ 300 DPI ≈ 70 MP) and high-res camera originals: 100 MP
    /// decodes to ~400 MB RGBA, comfortably under the 512 MB allocator backstop
    /// (Program.cs), while a 625 MP+ bomb is still rejected here at Identify.
    /// </summary>
    public const long MaxDecodePixels = 100_000_000; // 100 MP

    /// <summary>Shared message so both decode sites report the rejection identically (QUAL-3).</summary>
    public const string DimensionsExceededMessage = "Image dimensions exceed limits.";

    /// <summary>
    /// Single source of truth for the pixel-area limit, used at both the upload-time and
    /// preview-time decode sites (QUAL-3). Uses a <see langword="long"/> multiply so the
    /// product of two large <see langword="int"/> dimensions cannot overflow.
    /// </summary>
    public static bool ExceedsDecodeLimits(int widthPx, int heightPx)
        => (long)widthPx * heightPx > MaxDecodePixels;

    private readonly IStorageService _storage;
    private readonly ILogger<ImageProcessor> _logger;
    private readonly ImageDecodeLimiter _decodeLimiter;

    public ImageProcessor(IStorageService storage, ILogger<ImageProcessor> logger, ImageDecodeLimiter decodeLimiter)
    {
        _storage = storage;
        _logger = logger;
        _decodeLimiter = decodeLimiter;
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
        // Bound concurrent decodes process-wide: hold a slot for the whole read+decode so total
        // in-flight decode memory is capped regardless of request rate (M3, review 042-v4).
        using var slot = await _decodeLimiter.AcquireAsync(ct);

        await using var stream = await _storage.GetStreamAsync(storagePath, ct);

        // MaxFrames = 1 caps a multi-frame (e.g. APNG) bomb: without it a small-canvas file
        // with thousands of near-identical frames materialises frames × canvas × 4 bytes on
        // decode. This app only ever needs one still frame (bolt 042, BUG-1).
        var decoderOptions = new DecoderOptions { MaxFrames = 1 };

        Image image;
        try
        {
            // Reject pixel bombs before the full decode allocates pixel buffers (bolt 042, BUG-1).
            var info = await Image.IdentifyAsync(decoderOptions, stream, ct);
            if (info is not null && ExceedsDecodeLimits(info.Width, info.Height))
                throw new DecompressionBombException(info.Width, info.Height, DimensionsExceededMessage);
            stream.Position = 0;

            image = await Image.LoadAsync(decoderOptions, stream, ct);
        }
        catch (ImageFormatException)
        {
            // A file that passed the upload-time magic-byte check but was later corrupted or
            // replaced ops-side is unreadable here — IdentifyAsync/LoadAsync throw
            // UnknownImageFormatException (unrecognised) or InvalidImageContentException
            // (recognised but broken), both deriving from ImageFormatException. Surface it as
            // a clean 422, not a raw 500 (BUG-4, review 042-v1).
            throw new UnprocessableEntityException("The file could not be read as an image.");
        }

        using (image)
        {
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
}
