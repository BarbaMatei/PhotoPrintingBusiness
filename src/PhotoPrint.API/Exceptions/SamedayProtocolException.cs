namespace PhotoPrint.API.Exceptions;

/// <summary>
/// Sameday returned a 2xx response with a payload that does not match the
/// contract we expected (missing token, malformed JSON, missing AWB number, …).
/// Caller contract: log and surface to operators — retrying will not help; the
/// vendor API behaviour has changed or our parsing is wrong.
/// </summary>
public sealed class SamedayProtocolException : SamedayException
{
    public SamedayProtocolException(string endpoint, string reason, Exception? inner = null)
        : base(
            $"Sameday returned an unexpected response shape at '{endpoint}': {reason}",
            endpoint,
            httpStatus: 200,
            inner)
    {
    }
}
