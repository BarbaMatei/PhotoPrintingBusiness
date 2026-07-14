using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace PhotoPrint.Tests.Unit.Services;

/// <summary>
/// Exercises the REAL <see cref="ImageProcessor"/> (bolt 042, TEST-2). Every other suite
/// replaces <see cref="IImageProcessor"/> with a fake, so the actual decompression-bomb
/// guard, the format-error mapping, and the resize/encode never ran under test. These pin
/// them against genuine ImageSharp decoding.
///
/// Bolt 043 (ADR-008): the processor no longer touches storage — the caller routes via
/// <see cref="IStorageRouter"/> and hands it an open source stream — so these feed streams
/// directly. The thumbnail long edge is 300 px (bolt 043 added a separate 2000 px large-preview
/// tier, see <see cref="ImageProcessorLargePreviewTests"/>).
/// </summary>
public class ImageProcessorTests
{
    private readonly ImageProcessor _sut =
        new(Mock.Of<ILogger<ImageProcessor>>(), new ImageDecodeLimiter(maxConcurrentDecodes: 8));

    private const int ThumbnailMaxDimension = 300;

    private static MemoryStream PngStream<TPixel>(Image<TPixel> image) where TPixel : unmanaged, IPixel<TPixel>
    {
        var ms = new MemoryStream();
        image.SaveAsPng(ms);
        ms.Position = 0;
        return ms;
    }

    // ── ExceedsDecodeLimits (BUG-1: total-pixel area cap) ──────────────────────

    [Fact]
    public void ExceedsDecodeLimits_AtCapAllowed_OverCapAndOverflowRejected()
    {
        ImageProcessor.ExceedsDecodeLimits(10_000, 10_000).Should().BeFalse();  // 100 MP exactly
        ImageProcessor.ExceedsDecodeLimits(10_000, 10_001).Should().BeTrue();   // one row over
        // A per-axis check would pass 25000×25000 (≈625 MP); the area cap rejects it.
        ImageProcessor.ExceedsDecodeLimits(25_000, 25_000).Should().BeTrue();
        // long multiply: this product overflows a 32-bit int (would wrap negative).
        ImageProcessor.ExceedsDecodeLimits(60_000, 60_000).Should().BeTrue();
    }

    // ── GenerateThumbnailAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GenerateThumbnailAsync_OversizedImage_ThrowsDecompressionBomb()
    {
        // A genuine image whose pixel area (110 MP) exceeds the 100 MP decode cap. L8 keeps the
        // test allocation ~110 MB; the guard rejects it at Identify, before the full decode.
        using var big = new Image<L8>(11_000, 10_000);
        await using var src = PngStream(big);

        var act = () => _sut.GenerateThumbnailAsync(src);

        var ex = (await act.Should().ThrowAsync<DecompressionBombException>()).Which;
        ex.WidthPx.Should().Be(11_000);
        ex.HeightPx.Should().Be(10_000);
    }

    [Fact]
    public async Task GenerateThumbnailAsync_LargeImage_ReturnsJpegThumbnailWithinCap()
    {
        // Bolt 043: the thumbnail long edge is 300 px; a 2000x1500 source is downscaled to fit.
        using var source = new Image<Rgba32>(2000, 1500);
        await using var src = PngStream(source);

        await using var thumb = await _sut.GenerateThumbnailAsync(src);

        thumb.Length.Should().BeGreaterThan(0);

        // JPEG magic bytes (FF D8 FF) — the thumbnail is a JPEG.
        var head = thumb.ToArray();
        head[0].Should().Be(0xFF);
        head[1].Should().Be(0xD8);
        head[2].Should().Be(0xFF);

        thumb.Position = 0;
        var info = await Image.IdentifyAsync(thumb);
        Math.Max(info.Width, info.Height).Should().BeLessThanOrEqualTo(ThumbnailMaxDimension);
        // Prove the source WAS downscaled (a 2000 px source must not pass through at native size).
        Math.Max(info.Width, info.Height).Should().Be(ThumbnailMaxDimension);
    }

    [Fact]
    public async Task GenerateThumbnailAsync_DecodeSlotUnavailable_WaitsOnGateBeforeDecoding()
    {
        // M3: the decode gate must precede the decode, so total in-flight decode memory stays
        // bounded. With the only slot held and the request cancelled, the call abandons while
        // waiting on the gate and never reads/decodes the source stream.
        using var limiter = new ImageDecodeLimiter(maxConcurrentDecodes: 1);
        using var _held = await limiter.AcquireAsync();

        var sut = new ImageProcessor(Mock.Of<ILogger<ImageProcessor>>(), limiter);

        using var source = new Image<Rgba32>(64, 64);
        await using var src = PngStream(source);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => sut.GenerateThumbnailAsync(src, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        // The gate blocked before any decode work, so the source stream was never read.
        src.Position.Should().Be(0);
    }

    [Fact]
    public async Task GenerateThumbnailAsync_UnreadableFile_ThrowsUnprocessableEntity()
    {
        // A source that is not a decodable image (corrupted/replaced ops-side). ImageSharp
        // throws an ImageFormatException; the processor must surface a clean 422, not a 500 (BUG-4).
        await using var src = new MemoryStream(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x11, 0x22, 0x33 });

        var act = () => _sut.GenerateThumbnailAsync(src);

        await act.Should().ThrowAsync<UnprocessableEntityException>();
    }

    [Fact]
    public async Task GenerateThumbnailAsync_UnreadableFile_LogsCause()
    {
        // M7 (review 042-v4): a source corrupted/replaced ops-side is unreadable at preview time.
        // The catch previously rethrew a bare 422 with no log; it must log the caught exception
        // (mirroring GetInfoAsync) before rethrowing so the incident is visible to ops.
        var logger = new Mock<ILogger<ImageProcessor>>();
        var sut = new ImageProcessor(logger.Object, new ImageDecodeLimiter(8));
        await using var src = new MemoryStream(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x11 });

        var act = () => sut.GenerateThumbnailAsync(src);
        await act.Should().ThrowAsync<UnprocessableEntityException>();

        logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.Is<Exception?>(e => e != null),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LoadSingleFrameAsync_MultiFrameGif_DecodesOnlyOneFrame()
    {
        // M11 (review 042-v4): the frame-bomb cap (MaxFrames=1) is invisible in
        // GenerateThumbnailAsync's JPEG output (JPEG is single-frame regardless), so nothing
        // pinned it — removing it kept the suite green while a thousands-of-frames file again
        // materialised frames x canvas x 4 bytes on decode. A genuine 3-frame GIF must decode to
        // exactly one frame. (internal method reached by reflection, matching the repo pattern.)
        using var f1 = new Image<Rgba32>(8, 8);
        using var f2 = new Image<Rgba32>(8, 8);
        using var animated = new Image<Rgba32>(8, 8);
        animated.Frames.AddFrame(f1.Frames.RootFrame);
        animated.Frames.AddFrame(f2.Frames.RootFrame);
        animated.Frames.Count.Should().Be(3);

        using var gif = new MemoryStream();
        await animated.SaveAsGifAsync(gif);
        gif.Position = 0;

        var method = typeof(ImageProcessor).GetMethod(
            "LoadSingleFrameAsync", BindingFlags.NonPublic | BindingFlags.Static);
        using var decoded = await (Task<Image>)method!.Invoke(null, new object[] { gif, CancellationToken.None })!;

        decoded.Frames.Count.Should().Be(1);
    }

    [Fact]
    public async Task LoadSingleFrameAsync_DeepColourSource_DecodesAs32BppNot64()
    {
        // F7 (review 042-v8): decode is pinned to Rgba32 (4 B/px). The non-generic Image.LoadAsync
        // auto-selects the source pixel type, so a 16-bit (Rgba64) source decodes to 8 B/px and a
        // legitimate ~72 MP deep-colour print trips the 512 MB backstop -> permanently un-previewable.
        // A 16-bit source must decode to 32 bpp; reverting to the non-generic load yields 64 bpp.
        using var deep = new Image<Rgba64>(32, 32);
        using var png = PngStream(deep);

        var method = typeof(ImageProcessor).GetMethod(
            "LoadSingleFrameAsync", BindingFlags.NonPublic | BindingFlags.Static);
        using var decoded = await (Task<Image>)method!.Invoke(null, new object[] { png, CancellationToken.None })!;

        decoded.PixelType.BitsPerPixel.Should().Be(32);
    }

    [Fact]
    public async Task GenerateThumbnailAsync_DeepColourSource_ReturnsValidJpegThumbnail()
    {
        // F7 (review 042-v8): a legitimate 16-bit deep-colour source must decode -> resize -> encode
        // end-to-end into a valid JPEG, not fail. Small canvas keeps the test fast; the memory bound
        // (≤400 MB at 100 MP with the 4 B/px pin) is arithmetic, guarded by the sibling loader test.
        using var deep = new Image<Rgba64>(1000, 800);
        await using var src = PngStream(deep);

        await using var thumb = await _sut.GenerateThumbnailAsync(src);

        var head = thumb.ToArray();
        head[0].Should().Be(0xFF);
        head[1].Should().Be(0xD8);
        head[2].Should().Be(0xFF);

        thumb.Position = 0;
        var info = await Image.IdentifyAsync(thumb);
        Math.Max(info.Width, info.Height).Should().BeLessThanOrEqualTo(ThumbnailMaxDimension);
    }

    [Fact]
    public async Task GenerateThumbnailAsync_TruncatedButRecognizedImage_ThrowsUnprocessableEntity()
    {
        // L14 (review 042-v4): the catch handles both UnknownImageFormatException (unrecognised)
        // and InvalidImageContentException (recognised header, broken body), but only the former
        // was tested. A valid PNG signature + IHDR with corrupt image data is RECOGNISED as PNG,
        // so a decode failure raises InvalidImageContentException — narrowing the catch to
        // UnknownImageFormatException would 500 here. Cover that branch.
        using var source = new Image<Rgba32>(64, 64);
        var png = new MemoryStream();
        source.SaveAsPng(png);
        var bytes = png.ToArray();
        // Scramble a window in the middle (the compressed IDAT data), leaving the 8-byte PNG
        // signature + IHDR (so Identify still reads dimensions) and the trailing IEND intact.
        for (int i = bytes.Length / 2; i < bytes.Length / 2 + 24 && i < bytes.Length; i++)
            bytes[i] ^= 0xFF;
        await using var src = new MemoryStream(bytes);

        var act = () => _sut.GenerateThumbnailAsync(src);

        await act.Should().ThrowAsync<UnprocessableEntityException>();
    }

    // ── GetInfoAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetInfoAsync_ValidImage_ReturnsDimensions()
    {
        using var source = new Image<Rgba32>(640, 480);
        await using var src = PngStream(source);

        var info = await _sut.GetInfoAsync(src);

        info.Should().NotBeNull();
        info!.WidthPx.Should().Be(640);
        info.HeightPx.Should().Be(480);
    }

    [Fact]
    public async Task GetInfoAsync_NonImage_ReturnsNull()
    {
        await using var src = new MemoryStream(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 });

        var info = await _sut.GetInfoAsync(src);

        info.Should().BeNull();
    }
}
