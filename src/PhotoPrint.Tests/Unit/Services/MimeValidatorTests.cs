using FluentAssertions;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Unit.Services;

public class MimeValidatorTests
{
    private readonly IMimeValidator _sut = new MimeValidator();

    private static readonly byte[] JpegHeader  = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];
    private static readonly byte[] PngHeader   = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];
    // HEIC: bytes 0-3 are box size, bytes 4-7 are ASCII "ftyp"
    private static readonly byte[] HeicHeader  = [0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70, 0x68, 0x65, 0x69, 0x63];
    private static readonly byte[] PdfHeader   = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34, 0x00, 0x00, 0x00, 0x00];

    // ── JPEG ──────────────────────────────────────────────────────────────────

    [Fact]
    public void DetectMimeType_JpegMagicBytes_ReturnsImageJpeg()
    {
        using var stream = new MemoryStream(JpegHeader);

        _sut.DetectMimeType(stream).Should().Be("image/jpeg");
    }

    // ── PNG ───────────────────────────────────────────────────────────────────

    [Fact]
    public void DetectMimeType_PngMagicBytes_ReturnsImagePng()
    {
        using var stream = new MemoryStream(PngHeader);

        _sut.DetectMimeType(stream).Should().Be("image/png");
    }

    // ── HEIC/HEIF — no longer accepted ─────────────────────
    // The stack has no HEIF decoder, so accepting HEIC only buffered+wrote a file that then
    // failed at decode with a confusing 422. Reject it up front until a decoder is integrated.

    [Fact]
    public void DetectMimeType_HeicFtypBox_ReturnsNull()
    {
        using var stream = new MemoryStream(HeicHeader);

        _sut.DetectMimeType(stream).Should().BeNull();
    }

    [Theory]
    [InlineData("mif1")]
    [InlineData("heix")]
    [InlineData("msf1")]
    public void DetectMimeType_HeifBrands_ReturnsNull(string brand)
    {
        var header = FtypWithBrand(brand);
        using var stream = new MemoryStream(header);

        _sut.DetectMimeType(stream).Should().BeNull();
    }

    // A plain ISO-BMFF container (MP4/MOV/M4A) also starts with
    // "ftyp" but is NOT a HEIF image; it must be rejected by magic bytes, not accepted and
    // written to disk only to fail later at decode.
    [Theory]
    [InlineData("isom")]  // MP4
    [InlineData("mp42")]  // MP4
    [InlineData("qt  ")]  // QuickTime MOV
    public void DetectMimeType_NonHeifIsoBmffContainer_ReturnsNull(string brand)
    {
        var header = FtypWithBrand(brand);
        using var stream = new MemoryStream(header);

        _sut.DetectMimeType(stream).Should().BeNull();
    }

    private static byte[] FtypWithBrand(string brand)
    {
        var b = System.Text.Encoding.ASCII.GetBytes(brand);
        return [0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70, b[0], b[1], b[2], b[3]];
    }

    // ── Unknown / rejected ────────────────────────────────────────────────────

    [Fact]
    public void DetectMimeType_PdfMagicBytes_ReturnsNull()
    {
        using var stream = new MemoryStream(PdfHeader);

        _sut.DetectMimeType(stream).Should().BeNull();
    }

    [Fact]
    public void DetectMimeType_EmptyStream_ReturnsNull()
    {
        using var stream = new MemoryStream();

        _sut.DetectMimeType(stream).Should().BeNull();
    }

    [Fact]
    public void DetectMimeType_TwoByteStream_ReturnsNull()
    {
        // Less than 3 bytes — cannot match any magic pattern
        using var stream = new MemoryStream([0xFF, 0xD8]);

        _sut.DetectMimeType(stream).Should().BeNull();
    }

    // ── Stream position reset ─────────────────────────────────────────────────

    [Fact]
    public void DetectMimeType_AfterCall_StreamPositionIsZero()
    {
        using var stream = new MemoryStream(JpegHeader);

        _sut.DetectMimeType(stream);

        stream.Position.Should().Be(0);
    }

    [Fact]
    public void DetectMimeType_CalledTwice_ReturnsSameResult()
    {
        using var stream = new MemoryStream(PngHeader);

        var first  = _sut.DetectMimeType(stream);
        var second = _sut.DetectMimeType(stream);

        first.Should().Be(second);
    }
}
