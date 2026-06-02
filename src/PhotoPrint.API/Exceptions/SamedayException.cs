namespace PhotoPrint.API.Exceptions;

/// <summary>
/// Base exception for every fault that originates inside the Sameday integration
/// boundary. Carries the endpoint path the call was aimed at (never the request
/// body, never credentials) so log aggregators can trace failures without
/// exposing PII. Subclasses pick the operational meaning.
/// </summary>
public abstract class SamedayException : Exception
{
    public string Endpoint { get; }
    public int? HttpStatus { get; }

    protected SamedayException(string message, string endpoint, int? httpStatus = null, Exception? inner = null)
        : base(message, inner)
    {
        Endpoint = endpoint;
        HttpStatus = httpStatus;
    }
}
