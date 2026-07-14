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
/// </summary>
public class ImageProcessorTests
{
    private readonly Mock<IStorageService> _storage = new();
    private readonly ImageProcessor _sut;

    public ImageProcessorTests()
        => _sut = new ImageProcessor(_storage.Object, Mock.Of<ILogger<ImageProcessor>>(),
            new ImageDecodeLimiter(maxConcurrentDecodes: 8));

    private void StoreBytes(string path, byte[] bytes)
        => _storage.Setup(s => s.GetStreamAsync(path, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(() => new MemoryStream(bytes));

    private static byte[] EncodePng<TPixel>(Image<TPixel> image) where TPixel : unmanaged, IPixel<TPixel>
    {
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
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
        StoreBytes("big.png", EncodePng(big));

        var act = () => _sut.GenerateThumbnailAsync("big.png");

        var ex = (await act.Should().ThrowAsync<DecompressionBombException>()).Which;
        ex.WidthPx.Should().Be(11_000);
        ex.HeightPx.Should().Be(10_000);
    }

    [Fact]
    public async Task GenerateThumbnailAsync_SmallValidImage_ReturnsJpegThumbnailMax300px()
    {
        using var src = new Image<Rgba32>(800, 600);
        StoreBytes("photo.png", EncodePng(src));

        await using var thumb = await _sut.GenerateThumbnailAsync("photo.png");

        thumb.Length.Should().BeGreaterThan(0);

        // JPEG magic bytes (FF D8 FF) — the thumbnail is a JPEG.
        var head = thumb.ToArray();
        head[0].Should().Be(0xFF);
        head[1].Should().Be(0xD8);
        head[2].Should().Be(0xFF);

        thumb.Position = 0;
        var info = await Image.IdentifyAsync(thumb);
        Math.Max(info.Width, info.Height).Should().BeLessThanOrEqualTo(300);
    }

    [Fact]
    public async Task GenerateThumbnailAsync_DecodeSlotUnavailable_WaitsOnGateBeforeReadingStorage()
    {
        // M3: the decode gate must precede the storage read + decode, so total in-flight decode
        // memory stays bounded. With the only slot held and the request cancelled, the call
        // abandons while waiting on the gate and never opens the stored file.
        using var limiter = new ImageDecodeLimiter(maxConcurrentDecodes: 1);
        using var _held = await limiter.AcquireAsync();

        var storage = new Mock<IStorageService>(MockBehavior.Strict);
        var sut = new ImageProcessor(storage.Object, Mock.Of<ILogger<ImageProcessor>>(), limiter);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => sut.GenerateThumbnailAsync("held.png", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        storage.Verify(
            s => s.GetStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerateThumbnailAsync_UnreadableFile_ThrowsUnprocessableEntity()
    {
        // A stored file that is not a decodable image (corrupted/replaced ops-side). ImageSharp
        // throws an ImageFormatException; the processor must surface a clean 422, not a 500 (BUG-4).
        StoreBytes("corrupt.bin", new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x11, 0x22, 0x33 });

        var act = () => _sut.GenerateThumbnailAsync("corrupt.bin");

        await act.Should().ThrowAsync<UnprocessableEntityException>();
    }

    // ── GetInfoAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetInfoAsync_ValidImage_ReturnsDimensions()
    {
        using var src = new Image<Rgba32>(640, 480);
        StoreBytes("dims.png", EncodePng(src));

        var info = await _sut.GetInfoAsync("dims.png");

        info.Should().NotBeNull();
        info!.WidthPx.Should().Be(640);
        info.HeightPx.Should().Be(480);
    }

    [Fact]
    public async Task GetInfoAsync_NonImage_ReturnsNull()
    {
        StoreBytes("notimg.bin", new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 });

        var info = await _sut.GetInfoAsync("notimg.bin");

        info.Should().BeNull();
    }
}
