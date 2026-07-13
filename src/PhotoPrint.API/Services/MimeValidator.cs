using System.Text;

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

    // HEIF/HEIC major brands carried in the ftyp box at bytes 8–11. Every ISO-BMFF container
    // (MP4/MOV/M4A) also starts with "ftyp", so the brand MUST be checked too — otherwise any
    // video/audio file is misclassified as image/heic, buffered, written to disk, and only
    // rejected later at decode (INPUT-1, review 042-v1). Generic MP4/MOV brands (isom, mp42,
    // qt) are intentionally excluded.
    private static readonly HashSet<string> HeifBrands = new(StringComparer.Ordinal)
    {
        "heic", "heix", "heim", "heis",
        "hevc", "hevx", "hevm", "hevs",
        "mif1", "msf1", "mif2",
    };

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

        // HEIC/HEIF check: bytes 4..7 == "ftyp" AND bytes 8..11 a HEIF brand (not just any
        // ISO-BMFF container). Requires the full 12-byte header.
        if (read >= 12 && header[4..8].SequenceEqual(HeicFtyp))
        {
            var brand = Encoding.ASCII.GetString(header[8..12]);
            if (HeifBrands.Contains(brand))
                return "image/heic";
        }

        return null;
    }

    private static bool StartsWith(Span<byte> source, byte[] prefix)
    {
        if (source.Length < prefix.Length) return false;
        return source[..prefix.Length].SequenceEqual(prefix);
    }
}
