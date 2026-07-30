namespace PhotoPrint.API.Exceptions;

/// <summary>
/// Thrown when an <c>Idempotency-Key</c> is already bound to an existing order
/// whose logical request differs from the current one. Maps to HTTP 409
/// (state conflicts are 409, distinct from validation's 422).
/// </summary>
public class IdempotencyConflictException : Exception
{
    /// <summary>Names (not values) of the request fields that diverged from the
    /// existing order. Surfaced to the client in the ProblemDetails body.</summary>
    public IReadOnlyList<string> DivergentFields { get; }

    public IdempotencyConflictException(IReadOnlyList<string> divergentFields)
        : base("The Idempotency-Key is already associated with a different request.")
    {
        DivergentFields = divergentFields;
    }
}
