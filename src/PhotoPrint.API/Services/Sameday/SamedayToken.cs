namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Opaque bearer token issued by Sameday's <c>/api/authenticate</c> endpoint.
/// Cached in-process for its lifetime minus a safety window.
///
/// <see cref="ToString"/> is overridden to exclude <see cref="Value"/>: the
/// project's structured-logger emits records via their <c>ToString</c>
/// representation, and a stray <c>logger.LogX("{Token}", token)</c> in a
/// future PR must never leak the bearer string.
/// </summary>
public sealed record SamedayToken(string Value, DateTimeOffset ExpiresAt)
{
    /// <summary>Default safety window: treat the token as expired 60 s before
    /// its real expiry to absorb clock skew and in-flight request latency.</summary>
    public static readonly TimeSpan DefaultSafetyWindow = TimeSpan.FromSeconds(60);

    public bool IsValid(DateTimeOffset now, TimeSpan? safetyWindow = null)
        => now + (safetyWindow ?? DefaultSafetyWindow) < ExpiresAt;

    public override string ToString()
        => $"SamedayToken(ExpiresAt={ExpiresAt:o})";
}
