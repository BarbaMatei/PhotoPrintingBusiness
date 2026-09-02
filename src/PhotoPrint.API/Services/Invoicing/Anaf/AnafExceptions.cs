namespace PhotoPrint.API.Services.Invoicing.Anaf;

/// <summary>
/// Thrown when ANAF returns 401 twice in a row (after the
/// <c>AnafAuthHandler</c> attempted a single token refresh + retry).
/// This exits the auth pipeline and propagates
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

/// <summary>An upload whose outcome is unknown: ANAF may have accepted the XML and merely answered too slowly.</summary>
public sealed class AnafUploadTimeoutException : Exception
{
    public AnafUploadTimeoutException(string endpoint, Exception? inner = null)
        : base($"ANAF endpoint {endpoint} did not answer before the client timeout; the upload outcome is unknown.", inner)
    {
        Endpoint = endpoint;
    }

    public string Endpoint { get; }
}

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

// A 4xx other than 408/429 is ANAF refusing this document, not an outage.
public sealed class AnafContentRejectedException : Exception
{
    public string Endpoint { get; }
    public int HttpStatus { get; }

    public AnafContentRejectedException(string endpoint, int httpStatus)
        : base($"ANAF rejected the document at {endpoint} with HTTP {httpStatus}.")
    {
        Endpoint = endpoint;
        HttpStatus = httpStatus;
    }
}
