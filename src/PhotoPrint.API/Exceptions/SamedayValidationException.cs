namespace PhotoPrint.API.Exceptions;

/// <summary>
/// Sameday rejected our request with a 4xx (other than 401 / 408 / 429) — i.e.
/// the request we sent was malformed (e.g. parcel weight over the courier
/// ceiling, missing pickup point, unknown locker code). Caller contract: do NOT
/// retry — the bug is on our side.
/// </summary>
public sealed class SamedayValidationException : SamedayException
{
    /// <summary>Raw vendor response body, if captured. Kept OFF the exception message
    /// so recipient PII the vendor echoes back does not reach retained Error logs.</summary>
    public string? ResponseBody { get; }

    public SamedayValidationException(string endpoint, int httpStatus, string? body = null, Exception? inner = null)
        : base(
            $"Sameday rejected the request at '{endpoint}' with HTTP {httpStatus}.",
            endpoint,
            httpStatus,
            inner)
    {
        ResponseBody = body;
    }
}
