namespace PhotoPrint.API.Services;

/// <summary>
/// Validates file type by inspecting magic bytes — ignores client-supplied Content-Type.
/// Supported types: JPEG, PNG, HEIC/HEIF.
/// </summary>
public class MimeValidator : IMimeValidator
{
    // JPEG: FF D8 FF
    private static readonly byte[] JpegMagic = [0xFF, 0xD8, 0xFF];
    // PNG: 89 50 4E 47 0D 0A 1A 0A
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    // HEIC/HEIF: bytes 4–7 == "ftyp" (66 74 79 70)
    private static readonly byte[] HeicFtyp = [0x66, 0x74, 0x79, 0x70];

    public string? DetectMimeType(Stream stream)
    {
        Span<byte> header = stackalloc byte[12];
        stream.Position = 0;
        int read = stream.Read(header);
        stream.Position = 0;

        if (read < 3)
            return null;

        if (StartsWith(header, JpegMagic))
            return "image/jpeg";

        if (read >= 8 && StartsWith(header, PngMagic))
            return "image/png";

        // HEIC/HEIF check: bytes 4..7 == "ftyp"
        if (read >= 8 && header[4..8].SequenceEqual(HeicFtyp))
            return "image/heic";

        return null;
    }

    private static bool StartsWith(Span<byte> source, byte[] prefix)
    {
        if (source.Length < prefix.Length) return false;
        return source[..prefix.Length].SequenceEqual(prefix);
    }
}
