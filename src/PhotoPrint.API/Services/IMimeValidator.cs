namespace PhotoPrint.API.Services;

public interface IMimeValidator
{
    /// <summary>
    /// Validates the file type by reading magic bytes from the stream.
    /// Resets stream position to 0 after reading.
    /// Returns the detected MIME type, or null if the type is not allowed.
    /// </summary>
    string? DetectMimeType(Stream stream);
}
