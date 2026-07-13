namespace PhotoPrint.API.Exceptions;

/// <summary>
/// Thrown when an image's declared pixel area exceeds the decode limit — a
/// decompression-bomb defence (bolt 042, BUG-1/OBS-3). Subclasses
/// <see cref="UnprocessableEntityException"/> so it still maps to 422, but is a
/// distinct type so the exception handler can emit a dedicated structured event
/// (operators can alert on a pixel-bomb spike) and carry the offending dimensions.
/// </summary>
public class DecompressionBombException : UnprocessableEntityException
{
    public int WidthPx { get; }
    public int HeightPx { get; }

    public DecompressionBombException(int widthPx, int heightPx, string message)
        : base(message)
    {
        WidthPx = widthPx;
        HeightPx = heightPx;
    }
}
