namespace PhotoPrint.API.DTOs.Payments;

/// <summary>
/// Documents the <b>409 Conflict</b> body the payment endpoints return, so generated API
/// clients see the <c>divergentFields</c> extension the runtime carries (OBS-2, review
/// 035-v8). Referenced from the endpoints' <c>ProducesResponseType(409)</c>; the concrete
/// wire type is still the RFC7807 <c>ProblemDetails</c> emitted by
/// <c>ExceptionHandlerMiddleware</c> — this record is the documentation/OpenAPI contract.
///
/// <para><see cref="DivergentFields"/> is present only for a <i>same-caller divergent-request</i>
/// conflict (<c>IdempotencyConflictException</c>) — it names the request fields that differ
/// from the existing order (names only, no values). It is <c>null</c>/absent for a
/// <i>cross-tenant</i> key collision (<c>IdempotencyKeyTakenException</c>), where the caller
/// owns no order to describe.</para>
/// </summary>
public sealed record IdempotencyConflictProblemDetails(
    string Type,
    string Title,
    int Status,
    string? Detail,
    string CorrelationId,
    IReadOnlyList<string>? DivergentFields);
