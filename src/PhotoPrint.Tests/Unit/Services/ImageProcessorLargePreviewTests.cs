using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace PhotoPrint.Tests.Unit.Services;

/// <summary>
/// Unit tests for <see cref="ImageProcessor.GenerateLargePreviewAsync"/> (bolt 051).
/// Mirrors the bolt-042 thumbnail-tests pattern but with the 2000 px / q85 invariants.
/// </summary>
public class ImageProcessorLargePreviewTests
{
    private static ImageProcessor Create() => new(Mock.Of<ILogger<ImageProcessor>>());

    private static MemoryStream EncodeJpeg(int w, int h)
    {
        using var img = new Image<Rgba32>(w, h);
        var ms = new MemoryStream();
        img.Save(ms, new JpegEncoder());
        ms.Position = 0;
        return ms;
    }

    private static MemoryStream EncodePng(int w, int h)
    {
        using var img = new Image<Rgba32>(w, h);
        var ms = new MemoryStream();
        img.Save(ms, new PngEncoder());
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public async Task GenerateLargePreviewAsync_OversizedSource_LongEdge_Becomes2000Px()
    {
        var sut = Create();
        using var source = EncodeJpeg(4000, 3000);

        using var preview = await sut.GenerateLargePreviewAsync(source);
        using var decoded = await Image.LoadAsync(preview);

        decoded.Width.Should().Be(2000);
        decoded.Height.Should().Be(1500); // aspect 4:3 preserved
    }

    [Fact]
    public async Task GenerateLargePreviewAsync_SmallerSource_PassesThroughAtNativeSize()
    {
        // ResizeMode.Max never upscales — a 1500×1000 input must come out at 1500×1000.
        var sut = Create();
        using var source = EncodeJpeg(1500, 1000);

        using var preview = await sut.GenerateLargePreviewAsync(source);
        using var decoded = await Image.LoadAsync(preview);

        decoded.Width.Should().Be(1500);
        decoded.Height.Should().Be(1000);
    }

    [Fact]
    public async Task GenerateLargePreviewAsync_PortraitSource_LongEdgeRespected()
    {
        var sut = Create();
        using var source = EncodeJpeg(3000, 4000); // portrait

        using var preview = await sut.GenerateLargePreviewAsync(source);
        using var decoded = await Image.LoadAsync(preview);

        decoded.Height.Should().Be(2000);
        decoded.Width.Should().Be(1500);
    }

    [Fact]
    public async Task GenerateLargePreviewAsync_OutputIsJpeg()
    {
        var sut = Create();
        using var source = EncodePng(1000, 1000); // PNG in, JPEG out

        using var preview = await sut.GenerateLargePreviewAsync(source);
        var info = await Image.IdentifyAsync(preview);

        info!.Metadata.DecodedImageFormat!.DefaultMimeType.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task GenerateLargePreviewAsync_ReturnsStreamRewoundToZero()
    {
        var sut = Create();
        using var source = EncodeJpeg(2400, 1800);

        using var preview = await sut.GenerateLargePreviewAsync(source);

        preview.Position.Should().Be(0);
        preview.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateLargePreviewAsync_BombSizedSource_Throws()
    {
        // Defence-in-depth: the decompression-bomb guard must still kick in even on the
        // promoter's code path (the upload-time check was first; this is a second line).
        // Encoding 26000×26000 would be ~2.7 GB decoded — we don't actually allocate it;
        // ImageSharp's IdentifyAsync only reads the header and our code rejects before decode.
        var sut = Create();
        using var fake = BuildJpegHeaderClaimingDimensions(26_000, 26_000);

        var act = () => sut.GenerateLargePreviewAsync(fake);
        await act.Should().ThrowAsync<UnprocessableEntityException>()
            .WithMessage("*dimensions exceed limits*");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a tiny JPEG and rewrites the SOF0 marker to claim oversized dimensions —
    /// enough to make <c>Image.IdentifyAsync</c> report W×H above the bomb cap without
    /// actually allocating 2 GB of pixels. We never decode this; the guard rejects first.
    /// </summary>
    private static MemoryStream BuildJpegHeaderClaimingDimensions(int width, int height)
    {
        using var real = new Image<Rgba32>(8, 8);
        using var tmp = new MemoryStream();
        real.Save(tmp, new JpegEncoder());
        var bytes = tmp.ToArray();

        // Find the SOF0 marker (0xFFC0) and overwrite the height (2 bytes) + width (2 bytes).
        for (int i = 0; i < bytes.Length - 9; i++)
        {
            if (bytes[i] == 0xFF && bytes[i + 1] == 0xC0)
            {
                // i+2,3 = length; i+4 = precision; i+5,6 = height; i+7,8 = width
                bytes[i + 5] = (byte)((height >> 8) & 0xFF);
                bytes[i + 6] = (byte)(height & 0xFF);
                bytes[i + 7] = (byte)((width >> 8) & 0xFF);
                bytes[i + 8] = (byte)(width & 0xFF);
                break;
            }
        }
        return new MemoryStream(bytes);
    }
}
