namespace PhotoPrint.API.Exceptions;

/// <summary>
/// Sameday is unreachable or returned a transient failure that exhausted the
/// resilience-pipeline retry budget (5xx, 408, 429, network errors). Caller
/// contract: retry later — typically scheduled by a background job (bolt 037).
/// </summary>
public sealed class SamedayUnreachableException : SamedayException
{
    public SamedayUnreachableException(string endpoint, int? httpStatus = null, Exception? inner = null)
        : base(
            $"Sameday is unreachable at '{endpoint}'" +
            (httpStatus is not null ? $" (HTTP {httpStatus})." : "."),
            endpoint,
            httpStatus,
            inner)
    {
    }
}
