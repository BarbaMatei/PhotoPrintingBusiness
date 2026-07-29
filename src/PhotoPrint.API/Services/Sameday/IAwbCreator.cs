namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Per-order workflow that turns a Paid order into a Sameday AWB and persists
/// the result. ADR-015's load-bearing re-check
/// (<c>Status == Paid AND AwbNumber IS NULL</c>) lives inside the
/// implementation; callers MUST NOT skip this entry point.
/// </summary>
public interface IAwbCreator
{
    Task<AwbCreationOutcome> CreateForOrderAsync(Guid orderId, int attempt, CancellationToken ct = default);
}
