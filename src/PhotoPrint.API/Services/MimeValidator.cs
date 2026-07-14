namespace PhotoPrint.API.Services;

/// <summary>
/// Validates file type by inspecting magic bytes — ignores client-supplied Content-Type.
/// Supported types: JPEG, PNG.
/// <para>
/// HEIC/HEIF is intentionally NOT accepted (M5, review 042-v4): the stack has no HEIF decoder
/// (ImageSharp 3.x ships none), so accepting it only buffered+wrote a file that then failed at
/// decode with a confusing "could not be read as an image" 422. Every ISO-BMFF container
/// (HEIC as well as MP4/MOV/M4A) starts with an "ftyp" box and is now rejected here by falling
/// through to null. Re-add a brand check here — and HEIC in the UI — once a decoder is integrated.
/// </para>
/// </summary>
public class MimeValidator : IMimeValidator
{
    // JPEG: FF D8 FF
    private static readonly byte[] JpegMagic = [0xFF, 0xD8, 0xFF];
    // PNG: 89 50 4E 47 0D 0A 1A 0A
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public string? DetectMimeType(Stream stream)
    {
        Span<byte> header = stackalloc byte[8];
        stream.Position = 0;
        int read = stream.Read(header);
        stream.Position = 0;

        if (read < 3)
            return null;

        if (StartsWith(header, JpegMagic))
            return "image/jpeg";

        if (read >= 8 && StartsWith(header, PngMagic))
            return "image/png";

        return null;
    }

    private static bool StartsWith(Span<byte> source, byte[] prefix)
    {
        if (source.Length < prefix.Length) return false;
        return source[..prefix.Length].SequenceEqual(prefix);
    }
}
