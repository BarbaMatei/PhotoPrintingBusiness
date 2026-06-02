namespace PhotoPrint.API.Exceptions;

/// <summary>
/// Sameday rejected our credentials. Either a 401 returned by /api/authenticate,
/// or a second 401 returned by an operational call AFTER a fresh token was
/// attached (see ADR-014). Caller contract: stop retrying — the credentials
/// need to change, not the request.
/// </summary>
public sealed class SamedayAuthException : SamedayException
{
    public SamedayAuthException(string endpoint, Exception? inner = null)
        : base(
            $"Sameday rejected the request at '{endpoint}' with 401 even after a token refresh. " +
            "Verify Sameday:Username and Sameday:Password.",
            endpoint,
            httpStatus: 401,
            inner)
    {
    }
}
