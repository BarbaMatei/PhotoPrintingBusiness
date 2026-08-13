using PhotoPrint.API.Exceptions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
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

    /// <summary>
    /// Reject images whose total pixel area exceeds this — decompression-bomb defence
    /// A per-axis cap misses the total-pixel bomb: a 25000×25000 image
    /// passes any 25000-per-axis check yet decodes to ~625 MP ≈ 2.5 GB. An area cap bounds
    /// the decode allocation instead. Sized to accept legitimate
    /// large-format prints (A1 @ 300 DPI ≈ 70 MP) and high-res camera originals: 100 MP
    /// decodes to ~400 MB RGBA, comfortably under the 512 MB allocator backstop
    /// (Program.cs), while a 625 MP+ bomb is still rejected here at Identify.
    /// </summary>
    public const long MaxDecodePixels = 100_000_000; // 100 MP

    /// <summary>Shared message so both decode sites report the rejection identically.</summary>
    public const string DimensionsExceededMessage = "Image dimensions exceed limits.";

    /// <summary>
    /// Single source of truth for the pixel-area limit, used at the upload-time check and
    /// both derived-image decode sites. Uses a <see langword="long"/> multiply so the
    /// product of two large <see langword="int"/> dimensions cannot overflow.
    /// </summary>
    public static bool ExceedsDecodeLimits(int widthPx, int heightPx)
        => (long)widthPx * heightPx > MaxDecodePixels;

    private readonly ILogger<ImageProcessor> _logger;
    private readonly ImageDecodeLimiter _decodeLimiter;

    // No IStorageService dependency. The caller routes via
    // IStorageRouter and hands the processor an open source stream. The decode limiter
    // is retained: it bounds total in-flight decode memory
    // process-wide regardless of which tier the source stream came from.
    public ImageProcessor(ILogger<ImageProcessor> logger, ImageDecodeLimiter decodeLimiter)
    {
        _logger = logger;
        _decodeLimiter = decodeLimiter;
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

    public Task<MemoryStream> GenerateThumbnailAsync(Stream source, CancellationToken ct = default)
        // Thumbnails always resize down to the bound (ResizeMode.Max never upscales a source
        // already smaller than 300 px, so it is safe to always request the resize).
        => ResizeToJpegAsync(source, ThumbnailMaxDimension, ThumbnailJpegQuality, neverUpscale: false, ct);

    public Task<MemoryStream> GenerateLargePreviewAsync(Stream source, CancellationToken ct = default)
        // Story 002: "never upscale — images already < 2000 px pass through at native size."
        => ResizeToJpegAsync(source, LargePreviewMaxDimension, LargePreviewJpegQuality, neverUpscale: true, ct);

    private async Task<MemoryStream> ResizeToJpegAsync(
        Stream source, int maxDimension, int jpegQuality, bool neverUpscale, CancellationToken ct)
    {
        // Bound concurrent decodes process-wide: hold a slot for the whole read+decode so total
        // in-flight decode memory is capped regardless of request rate.
        using var slot = await _decodeLimiter.AcquireAsync(ct);

        Image image;
        try
        {
            // Reject pixel bombs before the full decode allocates pixel buffers.
            // Identify reads only header metadata, so it needs no frame cap; the load below does.
            if (source.CanSeek)
                source.Position = 0;
            var info = await Image.IdentifyAsync(source, ct);
            if (info is not null && ExceedsDecodeLimits(info.Width, info.Height))
                throw new DecompressionBombException(info.Width, info.Height, DimensionsExceededMessage);

            if (source.CanSeek)
                source.Position = 0;
            image = await LoadSingleFrameAsync(source, ct);
        }
        catch (ImageFormatException ex)
        {
            // A file that passed the upload-time magic-byte check but was later corrupted or
            // replaced ops-side is unreadable here — IdentifyAsync/LoadAsync throw
            // UnknownImageFormatException (unrecognised) or InvalidImageContentException
            // (recognised but broken), both deriving from ImageFormatException. Surface it as a
            // clean 422, not a raw 500; mirror GetInfoAsync's logging.
            // DecompressionBombException is not an ImageFormatException, so the bomb path above
            // propagates uncaught.
            _logger.LogWarning(ex, "Failed to decode image stream.");
            throw new UnprocessableEntityException("The file could not be read as an image.", ex);
        }

        using (image)
        {
            // ResizeMode.Max in ImageSharp 3.x DOES upscale a smaller source to fit the bound.
            // The large-preview tier must not upscale (story 002), so gate the resize on the
            // source actually exceeding the target; the thumbnail tier always resizes down.
            if (!neverUpscale || image.Width > maxDimension || image.Height > maxDimension)
            {
                image.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Size = new Size(maxDimension, maxDimension),
                    Mode = ResizeMode.Max,
                }));
            }

            var ms = new MemoryStream();
            var encoder = new JpegEncoder { Quality = jpegQuality };
            await image.SaveAsync(ms, encoder, ct);
            ms.Position = 0;
            return ms;
        }
    }

    // MaxFrames = 1 caps a multi-frame (APNG/GIF/WebP) bomb: without it a small-canvas file with
    // thousands of near-identical frames materialises frames × canvas × 4 bytes on decode. This
    // app only ever needs one still frame. Extracted + internal so a test can
    // prove the cap holds — it can't be observed in the single-frame JPEG output.
    //
    // Decode is pinned to Rgba32 (4 B/px). The non-generic Image.LoadAsync auto-selects the source's
    // pixel type, so a 16-bit source decodes to Rgba64 (8 B/px) and a legitimate ~72 MP deep-colour
    // print (< the 100 MP cap) blows the 512 MB allocator backstop. Forcing Rgba32 bounds any
    // ≤100 MP decode to ≤400 MB and loses nothing the 8-bit JPEG output could carry
    // Returns Task<Image> (not Task<Image<Rgba32>>, which is not assignable
    // to it) via async/await so the reflection test's (Task<Image>) cast still holds.
    internal static async Task<Image> LoadSingleFrameAsync(Stream stream, CancellationToken ct = default)
        => await Image.LoadAsync<Rgba32>(new DecoderOptions { MaxFrames = 1 }, stream, ct);
}
