namespace PhotoPrint.API.Services.Invoicing.Anaf;

/// <summary>
/// Thrown when ANAF returns 401 twice in a row (after the
/// <c>AnafAuthHandler</c> attempted a single token refresh + retry).
/// Per the ADR-014 pattern, this exits the auth pipeline and propagates
/// to the worker, which logs and leaves the invoice in <c>Pending</c>
/// for the next tick.
/// </summary>
public sealed class AnafAuthException : Exception
{
    public AnafAuthException(string endpoint)
        : base($"ANAF returned 401 twice for {endpoint}; check ClientId/ClientSecret and cert.")
    {
    }
}

/// <summary>
/// Thrown when ANAF returns a 200 OK whose body contains <c>&lt;Errors&gt;</c>
/// (the ANAF wire protocol uses 200 + body-encoded errors as a "your XML
/// was malformed" signal). The worker classifies this as a transient
/// failure: the invoice stays <c>Pending</c>, the error is recorded on
/// <c>LastError</c>, and the next tick retries.
/// </summary>
public sealed class AnafUploadException : Exception
{
    public AnafUploadException(string message) : base(message)
    {
    }
}

/// <summary>
/// Thrown when ANAF is unreachable (network failure, 5xx after the Polly
/// retry budget is exhausted, request timeout). The worker logs and exits
/// the per-row dispatch; the next tick retries on the natural schedule.
/// </summary>
public sealed class AnafUnreachableException : Exception
{
    public AnafUnreachableException(string endpoint, Exception? inner = null, int? httpStatus = null)
        : base(BuildMessage(endpoint, httpStatus), inner)
    {
        Endpoint   = endpoint;
        HttpStatus = httpStatus;
    }

    public string Endpoint   { get; }
    public int?   HttpStatus { get; }

    private static string BuildMessage(string endpoint, int? status)
        => status is null
            ? $"ANAF endpoint {endpoint} unreachable."
            : $"ANAF endpoint {endpoint} returned HTTP {status}.";
}
